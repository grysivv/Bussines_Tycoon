using System.Collections.Generic;
using Conglomerate.Production;

namespace Conglomerate
{
    /// <summary>Fabryka Tekstylna — produkuje Tkaninę → Ubrania.</summary>
    public class TextileFactory : FactoryBuilding
    {
        public override string ActivityType => "Fabryka Tekstylna";
        public override decimal BuildCost => 280000m;
        public override decimal MaintenanceCost => 1500m;
        public override int WarehouseCapacity => 250;

        public TextileFactory(string name) : base(name)
        {
            AvailableRecipes = new List<RecipeDefinition>
            {
                new RecipeDefinition
                {
                    Id = "fabric",
                    DisplayName = "Tkanina",
                    Inputs = new Dictionary<string, int>(),
                    Outputs = new Dictionary<string, int> { { "Tkanina", 5 } },
                    CycleDurationHours = 3,
                    OperationalCostPerCycle = 300m,
                    OutputPrices = new Dictionary<string, decimal> { { "Tkanina", 200 } }
                },
                new RecipeDefinition
                {
                    Id = "clothing",
                    DisplayName = "Odzież",
                    Inputs = new Dictionary<string, int> { { "Tkanina", 3 } },
                    Outputs = new Dictionary<string, int> { { "Odzież", 2 } },
                    CycleDurationHours = 6,
                    OperationalCostPerCycle = 500m,
                    OutputPrices = new Dictionary<string, decimal> { { "Odzież", 600 } }
                },
                new RecipeDefinition
                {
                    Id = "premium_clothing",
                    DisplayName = "Odzież Premium",
                    Inputs = new Dictionary<string, int> { { "Tkanina", 4 } },
                    Outputs = new Dictionary<string, int> { { "Odzież Premium", 2 } },
                    CycleDurationHours = 10,
                    OperationalCostPerCycle = 900m,
                    OutputPrices = new Dictionary<string, decimal> { { "Odzież Premium", 1800 } }
                }
            };
        }
    }
}
