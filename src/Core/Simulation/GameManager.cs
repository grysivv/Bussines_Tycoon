using System;
using System.Linq;

namespace Conglomerate
{
    public class GameManager
    {
        public Company ActiveCompany { get; set; }
        public Map ActiveMap { get; }
        public int CurrentDay { get; private set; } = 1;
        public int CurrentHour { get; private set; } = 8; // Start o godzinie 08:00 rano

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

                // Ekstraktorzy (Farm, CoalMine itp.) produkują raz na dobę (o północy)
                foreach (var building in ActiveCompany.Buildings)
                {
                    if (building is FactoryBuilding) continue; // fabryki tickowane co godzinę poniżej

                    decimal oldBalance = ActiveCompany.Balance;
                    building.Produce(ActiveCompany);
                    decimal maintenanceCharged = oldBalance - ActiveCompany.Balance;
                    if (maintenanceCharged > 0)
                    {
                        ActiveCompany.AddTransaction(CurrentDay, CurrentHour,
                            $"Utrzymanie: {building.Name}", -maintenanceCharged, "Utrzymanie", building.FacilityId);
                    }

                    // Przeniesienie nadmiaru surowców do dedykowanych magazynów
                    TransferResourcesToWarehouses(building, ActiveCompany);

                    if (building.AutoSell)
                    {
                        foreach (var key in building.Warehouse.Keys.ToList())
                        {
                            int qty = building.Warehouse[key];
                            if (qty > 0)
                                building.SellResource(key, qty, ActiveCompany, CurrentDay, CurrentHour);
                        }
                    }
                }

                // Opłata dzienna za utrzymanie fabryk (niezależna od produkcji)
                foreach (var factory in ActiveCompany.Buildings.OfType<FactoryBuilding>())
                {
                    if (ActiveCompany.Balance >= factory.MaintenanceCost)
                    {
                        ActiveCompany.Balance -= factory.MaintenanceCost;
                        ActiveCompany.AddTransaction(CurrentDay, CurrentHour,
                            $"Utrzymanie: {factory.Name}", -factory.MaintenanceCost, "Utrzymanie", factory.FacilityId);
                    }
                }

                // Zamknięcie miesiąca w silniku finansowym co 30 dni
                if (CurrentDay > 1 && (CurrentDay - 1) % 30 == 0)
                {
                    ActiveCompany.Engine.CloseMonth(CurrentDay, CurrentHour);
                }
            }

            // Fabryki przetwórcze — tick produkcji co każdą godzinę
            foreach (var factory in ActiveCompany.Buildings.OfType<FactoryBuilding>())
            {
                bool cycleCompleted = factory.TryAdvanceProduction(ActiveCompany);

                if (cycleCompleted)
                {
                    // Zaksięguj przychód za ukończony cykl (AutoSell lub do magazynu)
                    if (factory.AutoSell && factory.ActiveRecipe != null)
                    {
                        foreach (var output in factory.ActiveRecipe.Outputs)
                        {
                            int qty = factory.Warehouse.ContainsKey(output.Key) ? factory.Warehouse[output.Key] : 0;
                            if (qty > 0)
                                factory.SellResource(output.Key, qty, ActiveCompany, CurrentDay, CurrentHour);
                        }
                    }
                    else
                    {
                        // Przenieś do magazynów jeśli są dostępne
                        TransferResourcesToWarehouses(factory, ActiveCompany);
                    }

                    // Zaksięguj koszt operacyjny cyklu
                    if (factory.ActiveRecipe != null && factory.ActiveRecipe.OperationalCostPerCycle > 0)
                    {
                        ActiveCompany.AddTransaction(CurrentDay, CurrentHour,
                            $"Produkcja: {factory.Name} ({factory.ActiveRecipe.DisplayName})",
                            -factory.ActiveRecipe.OperationalCostPerCycle, "Koszty produkcji", factory.FacilityId);
                    }
                }
            }

            // Powiadomienie interfejsu graficznego do odświeżenia zegara i statystyk
            OnTickPerformed?.Invoke();
        }

        private void TransferResourcesToWarehouses(Building source, Company company)
        {
            if (source is WarehouseBuilding) return;

            foreach (var resourceKey in source.Warehouse.Keys.ToList())
            {
                int quantity = source.Warehouse[resourceKey];
                if (quantity <= 0) continue;

                // Find matching warehouses with capacity
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
                        // Transfer
                        source.Warehouse[resourceKey] -= amountToTransfer;
                        if (!wh.Warehouse.ContainsKey(resourceKey))
                        {
                            wh.Warehouse[resourceKey] = 0;
                        }
                        wh.Warehouse[resourceKey] += amountToTransfer;

                        quantity -= amountToTransfer;
                        if (quantity <= 0) break;
                    }
                }
            }
        }
    }
}
