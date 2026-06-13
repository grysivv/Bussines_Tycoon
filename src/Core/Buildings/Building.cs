using System;
using System.Collections.Generic;
using System.Linq;
using Conglomerate.Financials;

namespace Conglomerate
{
    public abstract class Building : IFacilitySegment
    {
        public int X { get; set; } = 0;
        public int Y { get; set; } = 0;
        public string FacilityId { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public abstract string ActivityType { get; }
        public abstract decimal BuildCost { get; }
        public abstract decimal MaintenanceCost { get; }
        public abstract int WarehouseCapacity { get; }
        public Dictionary<string, int> Warehouse { get; } = new Dictionary<string, int>();
        public abstract Dictionary<string, decimal> ResourcePrices { get; }
        public bool AutoSell { get; set; } = false;
        public HashSet<string> AutoSellResources { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // IFacilitySegment implementation
        public decimal PropertyPurchasePrice => BuildCost;
        public decimal DepreciationRate => 0.05m; // 5% depreciation per year
        public decimal AccumulatedDepreciation { get; set; } = 0m;
        public decimal PropertyBookValue => PropertyPurchasePrice - AccumulatedDepreciation;
        public decimal InventoryValue => Warehouse.Sum(kvp => kvp.Value * (ResourcePrices.ContainsKey(kvp.Key) ? ResourcePrices[kvp.Key] : 0m));

        protected Building(string name)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Budynek" : name;
        }

        public int GetTotalStock()
        {
            return Warehouse.Values.Sum();
        }

        public abstract bool Produce(Company company);

        public bool SellResource(string resource, int amount, Company company, int day, int hour)
        {
            if (Warehouse.ContainsKey(resource) && Warehouse[resource] >= amount && amount > 0)
            {
                if (ResourcePrices.ContainsKey(resource))
                {
                    decimal price = ResourcePrices[resource];
                    Warehouse[resource] -= amount;
                    decimal revenue = price * amount;
                    company.Balance += revenue;
                    company.AddTransaction(day, hour, $"Sprzedaż: {amount}x {resource}", revenue, "Sprzedaż", FacilityId);
                    return true;
                }
            }
            return false;
        }
    }
}
