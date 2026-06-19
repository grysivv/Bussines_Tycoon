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
            AddProduct("Mleko", 0);
            AddProduct("Mięso", 0);
        }

        public override bool Produce(Company company)
        {
            decimal totalStock = GetTotalStock();
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

            // Wyznacz produkcję (Mleko +3, Mięso +2)
            int milkProd = 3;
            int meatProd = 2;

            decimal freeSpace = WarehouseCapacity - totalStock;

            // Częściowa produkcja do zapełnienia magazynu
            decimal addedMilk = Math.Min(milkProd, freeSpace);
            AddProduct("Mleko", addedMilk);
            freeSpace -= addedMilk;

            decimal addedMeat = Math.Min(meatProd, freeSpace);
            AddProduct("Mięso", addedMeat);

            return true;
        }
    }
}
