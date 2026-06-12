using System;
using System.Collections.Generic;

namespace Conglomerate
{
    public class CoalMine : Building
    {
        public override string ActivityType => "Kopalnia węgla";
        public override decimal BuildCost => 15000m;
        public override decimal MaintenanceCost => 250m;
        public override int WarehouseCapacity => 50;
        public override Dictionary<string, decimal> ResourcePrices { get; } = new Dictionary<string, decimal>
        {
            { "Węgiel", 100m }
        };

        public CoalMine(string name) : base(name)
        {
            Warehouse["Węgiel"] = 0;
        }

        public override bool Produce(Company company)
        {
            int totalStock = GetTotalStock();
            if (totalStock >= WarehouseCapacity)
            {
                // Magazyn pełny, produkcja wstrzymana
                return false;
            }

            // Sprawdź czy firma ma środki na utrzymanie
            if (company.Balance < MaintenanceCost)
            {
                return false;
            }

            // Pobierz koszty utrzymania
            company.Balance -= MaintenanceCost;

            // Produkcja węgla (+4 jednostki)
            int coalProd = 4;
            int freeSpace = WarehouseCapacity - totalStock;

            int addedCoal = Math.Min(coalProd, freeSpace);
            Warehouse["Węgiel"] += addedCoal;

            return true;
        }
    }
}
