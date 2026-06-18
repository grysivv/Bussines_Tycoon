using System;
using System.Collections.Generic;

namespace Conglomerate
{
    public class CopperMine : Building
    {
        public override string ActivityType => "Kopalnia miedzi";
        public override decimal BuildCost => 15000m;
        public override decimal MaintenanceCost => 250m;
        public override int WarehouseCapacity => 50;
        public override Dictionary<string, decimal> ResourcePrices { get; } = new Dictionary<string, decimal>
        {
            { "Ruda Miedzi", 100m }
        };

        public CopperMine(string name) : base(name)
        {
            Warehouse["Ruda Miedzi"] = 0;
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

            // Produkcja miedzi (+4 jednostki)
            int copperProd = 4;
            int freeSpace = WarehouseCapacity - totalStock;

            int addedCopper = Math.Min(copperProd, freeSpace);
            Warehouse["Ruda Miedzi"] += addedCopper;

            return true;
        }
    }
}
