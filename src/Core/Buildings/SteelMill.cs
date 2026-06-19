using System.Collections.Generic;
using Conglomerate.Production;

namespace Conglomerate
{
    /// <summary>
    /// Huta Stali — przetwarza Rudę Żelaza + Węgiel → Stal.
    /// Kluczowy element łańcucha produkcji elektroniki i mebli.
    /// </summary>
    public class SteelMill : FactoryBuilding
    {
        public override string ActivityType => "Huta Stali";
        public override decimal BuildCost => 350000m;
        public override decimal MaintenanceCost => 1800m;
        public override int WarehouseCapacity => 200;

        public SteelMill(string name) : base(name)
        {
            AvailableRecipes = new System.Collections.Generic.List<RecipeDefinition>
            {
                new RecipeDefinition
                {
                    Id = "steel_basic",
                    DisplayName = "Produkcja Stali",
                    Inputs = new System.Collections.Generic.Dictionary<string, int>
                    {
                        { "Ruda Żelaza", 3 },
                        { "Węgiel",      2 }
                    },
                    Outputs = new System.Collections.Generic.Dictionary<string, int>
                    {
                        { "Stal", 2 }
                    },
                    CycleDurationHours = 6,
                    OperationalCostPerCycle = 400m,
                    OutputPrices = new System.Collections.Generic.Dictionary<string, decimal>
                    {
                        { "Stal", 800 }
                    }
                },
                new RecipeDefinition
                {
                    Id = "steel_quality",
                    DisplayName = "Stal Premium",
                    Inputs = new System.Collections.Generic.Dictionary<string, int>
                    {
                        { "Ruda Żelaza", 4 },
                        { "Węgiel",      2 }
                    },
                    Outputs = new System.Collections.Generic.Dictionary<string, int>
                    {
                        { "Stal Premium", 2 }
                    },
                    CycleDurationHours = 8,
                    OperationalCostPerCycle = 700m,
                    OutputPrices = new System.Collections.Generic.Dictionary<string, decimal>
                    {
                        { "Stal Premium", 1400 }
                    }
                }
            };
        }
    }
}
