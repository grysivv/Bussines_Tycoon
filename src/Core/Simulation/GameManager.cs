using System;
using System.Collections.Generic;
using System.Linq;
using Conglomerate.Logistics;
using Conglomerate.Simulation;

namespace Conglomerate
{
    public class GameManager
    {
        public Company ActiveCompany { get; set; }
        public Map ActiveMap { get; }
        public int CurrentDay { get; private set; } = 1;
        public int CurrentHour { get; private set; } = 8; // Start o godzinie 08:00 rano

        // ── System Logistyczny ──
        public FreeMarket Market { get; } = new FreeMarket();
        public LogisticsManager Logistics { get; } = new LogisticsManager();
        public AIManager AIManager { get; } = new AIManager();

        public event Action? OnTickPerformed;

        public GameManager(Company company, Map map)
        {
            ActiveCompany = company;
            ActiveMap = map;
        }

        public void RestoreState(int day, int hour)
        {
            CurrentDay = day;
            CurrentHour = hour;
        }

        public void NextTick()
        {
            CurrentHour++;

            if (CurrentHour >= 24)
            {
                CurrentHour = 0;
                CurrentDay++;

                // Aktualizacja cen rynkowych (raz na dobę)
                Market.OnNewDay(CurrentDay);
                AIManager.TickDaily(ActiveMap, CurrentDay, CurrentHour);

                var allCompanies = new List<Company> { ActiveCompany };
                allCompanies.AddRange(AIManager.Competitors);

                // Ekstraktorzy (Farm, CoalMine itp.) produkują raz na dobę (o północy)
                // UWAGA: RetailBuilding i FactoryBuilding są pominięte — mają własne pętle poniżej
                foreach (var company in allCompanies)
                {
                    foreach (var building in company.Buildings)
                    {
                        if (building is FactoryBuilding) continue;
                        if (building is RetailBuilding)  continue; // sklep ma własny tick poniżej

                        decimal oldBalance = company.Balance;
                        building.Produce(company);
                        decimal maintenanceCharged = oldBalance - company.Balance;
                        if (maintenanceCharged > 0)
                        {
                            company.AddTransaction(CurrentDay, CurrentHour,
                                $"Utrzymanie: {building.Name}", -maintenanceCharged, "Utrzymanie", building.FacilityId);
                        }

                        // Przeniesienie nadmiaru surowców do dedykowanych magazynów
                        TransferResourcesToWarehouses(building, company);

                        foreach (var key in building.Warehouse.Keys.ToList())
                        {
                            if (building.AutoSellResources.Contains(key) || (building.AutoSell && building.AutoSellResources.Count == 0))
                            {
                                int qty = building.Warehouse[key];
                                if (qty > 0)
                                    building.SellResource(key, qty, company, CurrentDay, CurrentHour);
                            }
                        }
                    }

                    // Opłata dzienna za utrzymanie fabryk (niezależna od produkcji)
                    foreach (var factory in company.Buildings.OfType<FactoryBuilding>())
                    {
                        if (company.Balance >= factory.MaintenanceCost)
                        {
                            company.Balance -= factory.MaintenanceCost;
                            company.AddTransaction(CurrentDay, CurrentHour,
                                $"Utrzymanie: {factory.Name}", -factory.MaintenanceCost, "Utrzymanie", factory.FacilityId);
                        }
                    }

                    // Opłata dzienna za utrzymanie sklepów detalicznych
                    foreach (var store in company.Buildings.OfType<RetailBuilding>())
                    {
                        if (company.Balance >= store.MaintenanceCost)
                        {
                            company.Balance -= store.MaintenanceCost;
                            company.AddTransaction(CurrentDay, CurrentHour,
                                $"Utrzymanie: {store.Name}", -store.MaintenanceCost, "Utrzymanie", store.FacilityId);
                        }
                    }

                    // Zamknięcie miesiąca w silniku finansowym co 30 dni
                    if (CurrentDay > 1 && (CurrentDay - 1) % 30 == 0)
                    {
                        company.Engine.CloseMonth(CurrentDay, CurrentHour);
                    }
                }
            }

            // Trasy logistyczne — tick co każdą godzinę
            Logistics.Tick(ActiveCompany, Market, CurrentDay, CurrentHour);

            var hourlyCompanies = new List<Company> { ActiveCompany };
            hourlyCompanies.AddRange(AIManager.Competitors);

            foreach (var company in hourlyCompanies)
            {
                // Fabryki przetwórcze — tick produkcji co każdą godzinę
                foreach (var factory in company.Buildings.OfType<FactoryBuilding>())
                {
                    decimal oldBalance = company.Balance;
                    bool cycleCompleted = factory.TryAdvanceProduction(company);
                    decimal costCharged = oldBalance - company.Balance;
                    
                    if (costCharged > 0)
                    {
                        company.AddTransaction(CurrentDay, CurrentHour,
                            $"Produkcja: {factory.Name} ({(factory.ActiveRecipe != null ? factory.ActiveRecipe.DisplayName : "")})",
                            -costCharged, "Koszty produkcji", factory.FacilityId);
                    }

                    if (cycleCompleted)
                    {
                        if (factory.ActiveRecipe != null)
                        {
                            foreach (var output in factory.ActiveRecipe.Outputs)
                            {
                                if (factory.AutoSellResources.Contains(output.Key) || (factory.AutoSell && factory.AutoSellResources.Count == 0))
                                {
                                    int qty = factory.Warehouse.ContainsKey(output.Key) ? factory.Warehouse[output.Key] : 0;
                                    if (qty > 0)
                                        factory.SellResource(output.Key, qty, company, CurrentDay, CurrentHour);
                                }
                            }
                        }
                        else
                        {
                            TransferResourcesToWarehouses(factory, company);
                        }
                    }
                }

                // ── Sklepy detaliczne — tick sprzedaży co każdą godzinę ──
                foreach (var store in company.Buildings.OfType<RetailBuilding>())
                {
                    store.TickHourlySales(company, CurrentDay, CurrentHour);
                }
            }

            // Powiadomienie interfejsu graficznego do odświeżenia zegara i statystyk
            OnTickPerformed?.Invoke();
        }

        private void TransferResourcesToWarehouses(Building source, Company company)
        {
            if (source is WarehouseBuilding) return;
            if (source is RetailBuilding)    return; // sklep zarządza własnym magazynem

            foreach (var resourceKey in source.Warehouse.Keys.ToList())
            {
                int quantity = source.Warehouse[resourceKey];
                if (quantity <= 0) continue;

                var category = ResourceRegistry.GetCategory(resourceKey);
                var warehouses = company.Buildings
                    .OfType<WarehouseBuilding>()
                    .Where(w => w.AllowedCategory == category)
                    .ToList();

                foreach (var wh in warehouses)
                {
                    int freeSpace = wh.WarehouseCapacity - wh.GetTotalStock();
                    if (freeSpace <= 0) continue;

                    int amountToTransfer = Math.Min(quantity, freeSpace);
                    if (amountToTransfer > 0)
                    {
                        source.Warehouse[resourceKey] -= amountToTransfer;
                        if (!wh.Warehouse.ContainsKey(resourceKey))
                            wh.Warehouse[resourceKey] = 0;
                        wh.Warehouse[resourceKey] += amountToTransfer;

                        quantity -= amountToTransfer;
                        if (quantity <= 0) break;
                    }
                }
            }
        }
    }
}
