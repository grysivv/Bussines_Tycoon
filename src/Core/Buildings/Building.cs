using System;
using System.Collections.Generic;
using System.Linq;
using Conglomerate.Financials;
using Conglomerate.Economy;

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
        public decimal EffectiveBuildCost { get; set; } = 0m;
        public abstract decimal MaintenanceCost { get; }
        public abstract int WarehouseCapacity { get; }
        public Dictionary<string, ProductBatch> Warehouse { get; } = new Dictionary<string, ProductBatch>();
        public abstract Dictionary<string, decimal> ResourcePrices { get; }
        public bool AutoSell { get; set; } = false;
        public HashSet<string> AutoSellResources { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Capitalism Lab: Szkolenia (Training)
        public decimal TrainingBudget { get; set; } = 0m; // Miesięczny budżet na szkolenia w tym budynku
        public float WorkerExperience { get; set; } = 1.0f; // Poziom doświadczenia: 1.0f (bazowy) do np. 2.0f (max)


        // IFacilitySegment implementation
        public decimal PropertyPurchasePrice => EffectiveBuildCost > 0 ? EffectiveBuildCost : BuildCost;
        public decimal DepreciationRate => 0.05m; // 5% depreciation per year
        public decimal AccumulatedDepreciation { get; set; } = 0m;
        public decimal PropertyBookValue => PropertyPurchasePrice - AccumulatedDepreciation;
        public decimal InventoryValue => Warehouse.Sum(kvp => kvp.Value.Quantity * (ResourcePrices.ContainsKey(kvp.Key) ? ResourcePrices[kvp.Key] : 0m));

        protected Building(string name)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Budynek" : name;
        }

        public decimal GetTotalStock()
        {
            return Warehouse.Values.Sum(b => b.Quantity);
        }

        public decimal GetProductQuantity(string name)
        {
            return Warehouse.ContainsKey(name) ? Warehouse[name].Quantity : 0m;
        }

        public void AddProduct(string name, decimal amount, decimal quality = 10m, decimal brand = 0m)
        {
            if (amount <= 0) return;
            if (!Warehouse.ContainsKey(name))
                Warehouse[name] = new ProductBatch(name, 0, quality, brand);
            
            Warehouse[name].MergeWith(new ProductBatch(name, amount, quality, brand));
        }

        public void RemoveProduct(string name, decimal amount)
        {
            if (Warehouse.ContainsKey(name))
            {
                Warehouse[name].Quantity -= amount;
                if (Warehouse[name].Quantity < 0) Warehouse[name].Quantity = 0;
            }
        }

        public abstract bool Produce(Company company);

        public virtual void TickHourly(int currentDay, int currentHour, Company company)
        {
            // Szkolenia: budżet miesięczny dzielony na 720 godzin (30 dni * 24h)
            if (TrainingBudget > 0 && company.Balance >= (TrainingBudget / 720m))
            {
                decimal hourlyCost = TrainingBudget / 720m;
                company.Balance -= hourlyCost;
                
                // Doświadczenie rośnie, max to 2.0f
                if (WorkerExperience < 2.0f)
                {
                    // 100k budżetu miesięcznie daje około 0.05 wzrostu na miesiąc
                    WorkerExperience += (float)(hourlyCost / 2000000m); 
                    if (WorkerExperience > 2.0f) WorkerExperience = 2.0f;
                }
            }

            Produce(company);
        }

        public bool SellResource(string resource, decimal amount, Company company, int day, int hour)
        {
            if (Warehouse.ContainsKey(resource) && Warehouse[resource].Quantity >= amount && amount > 0)
            {
                if (ResourcePrices.ContainsKey(resource))
                {
                    decimal price = ResourcePrices[resource];
                    Warehouse[resource].Quantity -= amount;
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
