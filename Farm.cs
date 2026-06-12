using System;
using System.Collections.Generic;

namespace Conglomerate
{
    public class Farm : Building
    {
        public override string ActivityType => "Hodowla krów";
        public override decimal BuildCost => 10000m;
        public override decimal MaintenanceCost => 150m;
        public override int WarehouseCapacity => 30;
        public override Dictionary<string, decimal> ResourcePrices { get; } = new Dictionary<string, decimal>
        {
            { "Mleko", 50m },
            { "Mięso", 150m }
        };

        public Farm(string name) : base(name)
        {
            Warehouse["Mleko"] = 0;
            Warehouse["Mięso"] = 0;
        }

        public override bool Produce(Company company)
        {
            int totalStock = GetTotalStock();
            if (totalStock >= WarehouseCapacity)
            {
                return false;
            }

            // Sprawdź czy firma ma środki na utrzymanie
            if (company.Balance < MaintenanceCost)
            {
                return false;
            }

            // Pobierz koszty utrzymania
            company.Balance -= MaintenanceCost;

            // Wyznacz produkcję (Mleko +2, Mięso +1)
            int milkProd = 2;
            int meatProd = 1;

            int freeSpace = WarehouseCapacity - totalStock;

            // Częściowa produkcja do zapełnienia magazynu
            int addedMilk = Math.Min(milkProd, freeSpace);
            Warehouse["Mleko"] += addedMilk;
            freeSpace -= addedMilk;

            int addedMeat = Math.Min(meatProd, freeSpace);
            Warehouse["Mięso"] += addedMeat;

            return true;
        }
    }
}
