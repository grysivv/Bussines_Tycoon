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
                        ActiveCompany.AddTransaction(CurrentDay, CurrentHour, $"Utrzymanie: {building.Name}", -maintenanceCharged, "Utrzymanie");
                    }

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
            }

            // Powiadomienie interfejsu graficznego do odświeżenia zegara i statystyk
            OnTickPerformed?.Invoke();
        }
    }
}
