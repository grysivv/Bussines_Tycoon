using System;
using System.Collections.Generic;

namespace Conglomerate
{
    public class WarehouseBuilding : Building
    {
        public ResourceCategory AllowedCategory { get; }

        public override string ActivityType => AllowedCategory == ResourceCategory.Food ? "Magazyn żywności" : "Magazyn kopalniany";
        public override decimal BuildCost => AllowedCategory == ResourceCategory.Food ? 8000m : 12000m;
        public override decimal MaintenanceCost => AllowedCategory == ResourceCategory.Food ? 50m : 80m;
        public override int WarehouseCapacity => AllowedCategory == ResourceCategory.Food ? 150 : 250;

        public override Dictionary<string, decimal> ResourcePrices
        {
            get
            {
                var dict = new Dictionary<string, decimal>();
                foreach (var resName in ResourceRegistry.GetResourcesByCategory(AllowedCategory))
                {
                    dict[resName] = ResourceRegistry.GetPrice(resName);
                }
                return dict;
            }
        }

        public WarehouseBuilding(string name, ResourceCategory category) : base(name)
        {
            AllowedCategory = category;

            // Initialize warehouse slots for matching category resources
            foreach (var resName in ResourceRegistry.GetResourcesByCategory(category))
            {
                AddProduct(resName, 0);
            }
        }

        public override bool Produce(Company company)
        {
            // Warehouses do not produce resources themselves. They only consume maintenance cost.
            if (company.Balance >= MaintenanceCost)
            {
                company.Balance -= MaintenanceCost;
                return true;
            }
            return false;
        }

        public bool CanStoreResource(string resourceName)
        {
            return ResourceRegistry.GetCategory(resourceName) == AllowedCategory;
        }
    }
}
