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

                // Produkcja i opłaty za utrzymanie naliczane są raz dziennie (o północy) dla wszystkich budynków
                foreach (var building in ActiveCompany.Buildings)
                {
                    decimal oldBalance = ActiveCompany.Balance;
                    building.Produce(ActiveCompany);
                    decimal maintenanceCharged = oldBalance - ActiveCompany.Balance;
                    if (maintenanceCharged > 0)
                    {
                        ActiveCompany.AddTransaction(CurrentDay, CurrentHour, $"Utrzymanie: {building.Name}", -maintenanceCharged, "Utrzymanie", building.FacilityId);
                    }

                    // Przeniesienie nadmiaru wyprodukowanych surowców do dedykowanych magazynów
                    TransferResourcesToWarehouses(building, ActiveCompany);

                    if (building.AutoSell)
                    {
                        foreach (var key in building.Warehouse.Keys.ToList())
                        {
                            int qty = building.Warehouse[key];
                            if (qty > 0)
                            {
                                building.SellResource(key, qty, ActiveCompany, CurrentDay, CurrentHour);
                            }
                        }
                    }
                }

                // Zamknięcie miesiąca w silniku finansowym co 30 dni
                if (CurrentDay > 1 && (CurrentDay - 1) % 30 == 0)
                {
                    ActiveCompany.Engine.CloseMonth(CurrentDay, CurrentHour);
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
