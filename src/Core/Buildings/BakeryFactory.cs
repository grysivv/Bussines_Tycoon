using System.Collections.Generic;
using Conglomerate.Production;

namespace Conglomerate
{
    /// <summary>Piekarnia / Zakład Spożywczy — produkuje produkty spożywcze premium.</summary>
    public class BakeryFactory : FactoryBuilding
    {
        public override string ActivityType => "Zakład Spożywczy";
        public override decimal BuildCost => 180000m;
        public override decimal MaintenanceCost => 900m;
        public override int WarehouseCapacity => 300;

        public BakeryFactory(string name) : base(name)
        {
            AvailableRecipes = new List<RecipeDefinition>
            {
                new RecipeDefinition
                {
                    Id = "bread",
                    DisplayName = "Chleb",
                    Inputs = new Dictionary<string, int>(),
                    Outputs = new Dictionary<string, int> { { "Chleb", 10 } },
                    CycleDurationHours = 2,
                    OperationalCostPerCycle = 150m,
                    OutputPrices = new Dictionary<string, decimal> { { "Chleb", 50 } }
                },
                new RecipeDefinition
                {
                    Id = "pastry",
                    DisplayName = "Wyroby Cukiernicze",
                    Inputs = new Dictionary<string, int>
                    {
                        { "Mleko", 2 }
                    },
                    Outputs = new Dictionary<string, int> { { "Wyroby Cukiernicze", 5 } },
                    CycleDurationHours = 4,
                    OperationalCostPerCycle = 250m,
                    OutputPrices = new Dictionary<string, decimal> { { "Wyroby Cukiernicze", 180 } }
                },
                new RecipeDefinition
                {
                    Id = "packaged_food",
                    DisplayName = "Żywność Pakowana",
                    Inputs = new Dictionary<string, int>
                    {
                        { "Mleko", 1 },
                        { "Mięso", 1 }
                    },
                    Outputs = new Dictionary<string, int> { { "Żywność Pakowana", 4 } },
                    CycleDurationHours = 6,
                    OperationalCostPerCycle = 400m,
                    OutputPrices = new Dictionary<string, decimal> { { "Żywność Pakowana", 350 } }
                }
            };
        }
    }
}
