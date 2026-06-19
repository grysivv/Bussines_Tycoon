using System.Collections.Generic;
using Conglomerate.Production;

namespace Conglomerate
{
    /// <summary>
    /// Fabryka Elektroniki — przetwarza Stal + Miedź → Komponenty Elektroniczne → Smartfon / Laptop.
    /// 3-etapowy łańcuch jak w Capitalism Lab.
    /// </summary>
    public class ElectronicsFactory : FactoryBuilding
    {
        public override string ActivityType => "Fabryka Elektroniki";
        public override decimal BuildCost => 600000m;
        public override decimal MaintenanceCost => 3000m;
        public override int WarehouseCapacity => 150;

        public ElectronicsFactory(string name) : base(name)
        {
            AvailableRecipes = new List<RecipeDefinition>
            {
                new RecipeDefinition
                {
                    Id = "components",
                    DisplayName = "Komponenty Elektroniczne",
                    Inputs = new Dictionary<string, int>
                    {
                        { "Miedź",  2 },
                        { "Stal",   1 }
                    },
                    Outputs = new Dictionary<string, int>
                    {
                        { "Komponenty Elektroniczne", 3 }
                    },
                    CycleDurationHours = 4,
                    OperationalCostPerCycle = 600m,
                    OutputPrices = new Dictionary<string, decimal>
                    {
                        { "Komponenty Elektroniczne", 800 }
                    }
                },
                new RecipeDefinition
                {
                    Id = "smartphone",
                    DisplayName = "Smartfon",
                    Inputs = new Dictionary<string, int>
                    {
                        { "Komponenty Elektroniczne", 3 }
                    },
                    Outputs = new Dictionary<string, int>
                    {
                        { "Smartfon", 2 }
                    },
                    CycleDurationHours = 8,
                    OperationalCostPerCycle = 1200m,
                    OutputPrices = new Dictionary<string, decimal>
                    {
                        { "Smartfon", 2500 }
                    }
                },
                new RecipeDefinition
                {
                    Id = "laptop",
                    DisplayName = "Laptop",
                    Inputs = new Dictionary<string, int>
                    {
                        { "Komponenty Elektroniczne", 5 },
                        { "Stal", 1 }
                    },
                    Outputs = new Dictionary<string, int>
                    {
                        { "Laptop", 2 }
                    },
                    CycleDurationHours = 12,
                    OperationalCostPerCycle = 2000m,
                    OutputPrices = new Dictionary<string, decimal>
                    {
                        { "Laptop", 5000 }
                    }
                }
            };
        }
    }
}
