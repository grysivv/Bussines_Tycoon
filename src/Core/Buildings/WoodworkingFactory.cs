using System.Collections.Generic;
using Conglomerate.Production;

namespace Conglomerate
{
    /// <summary>Tartak / Meblownia — produkuje Drewno → Meble.</summary>
    public class WoodworkingFactory : FactoryBuilding
    {
        public override string ActivityType => "Tartarnia i Meblownia";
        public override decimal BuildCost => 220000m;
        public override decimal MaintenanceCost => 1200m;
        public override int WarehouseCapacity => 200;

        public WoodworkingFactory(string name) : base(name)
        {
            AvailableRecipes = new List<RecipeDefinition>
            {
                new RecipeDefinition
                {
                    Id = "lumber",
                    DisplayName = "Drewno Przetarte",
                    Inputs = new Dictionary<string, int>(),
                    Outputs = new Dictionary<string, int> { { "Drewno Przetarte", 6 } },
                    CycleDurationHours = 4,
                    OperationalCostPerCycle = 350m,
                    OutputPrices = new Dictionary<string, decimal> { { "Drewno Przetarte", 250 } }
                },
                new RecipeDefinition
                {
                    Id = "furniture",
                    DisplayName = "Meble",
                    Inputs = new Dictionary<string, int>
                    {
                        { "Drewno Przetarte", 4 },
                        { "Stal", 1 }
                    },
                    Outputs = new Dictionary<string, int> { { "Meble", 2 } },
                    CycleDurationHours = 8,
                    OperationalCostPerCycle = 700m,
                    OutputPrices = new Dictionary<string, decimal> { { "Meble", 1500 } }
                },
                new RecipeDefinition
                {
                    Id = "luxury_furniture",
                    DisplayName = "Meble Luksusowe",
                    Inputs = new Dictionary<string, int>
                    {
                        { "Drewno Przetarte", 6 },
                        { "Stal Premium",     1 }
                    },
                    Outputs = new Dictionary<string, int> { { "Meble Luksusowe", 2 } },
                    CycleDurationHours = 14,
                    OperationalCostPerCycle = 1400m,
                    OutputPrices = new Dictionary<string, decimal> { { "Meble Luksusowe", 4000 } }
                }
            };
        }
    }
}
