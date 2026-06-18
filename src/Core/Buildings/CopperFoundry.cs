using System.Collections.Generic;
using Conglomerate.Production;

namespace Conglomerate
{
    /// <summary>
    /// Miedziana Huta — przetwarza Ruda Żelaza w Miedź.
    /// Przykładowy budynek przetwórczy dla surowców kopalnianych.
    /// 
    /// Dostępne przepisy:
    ///   1. Ruda Żelaza → Miedź (2 Ruda Żelaza = 1 Miedź, 8 godzin)
    /// 
    /// Surowce wejściowe (Ruda Żelaza) muszą być dostarczone do Warehouse przed startem cyklu.
    /// Produkty wyjściowe trafiają do Warehouse po zakończeniu cyklu.
    /// </summary>
    public class CopperFoundry : FactoryBuilding
    {
        public override string ActivityType => "Huta Miedzi";
        public override decimal BuildCost => 30000m;
        public override decimal MaintenanceCost => 350m;
        public override int WarehouseCapacity => 50;

        public CopperFoundry(string name) : base(name)
        {
            // ──────────────────────────────────────────────
            //  Definicje przepisów
            // ──────────────────────────────────────────────

            AvailableRecipes.Add(new RecipeDefinition
            {
                Id = "iron_ore_to_copper",
                DisplayName = "Ruda Żelaza → Miedź",
                Description = "Proces hutniczy przetwarzający rudę żelaza w miedź. 2 jednostki rudy dają 1 miedź.",
                Inputs = new Dictionary<string, int> { { "Ruda Miedzi", 2 } , {"Węgiel", 1 } },
                Outputs = new Dictionary<string, int> { { "Miedź", 1 } },
                CycleDurationHours = 4,
                OperationalCostPerCycle = 70m,
                OutputPrices = new Dictionary<string, decimal> { { "Miedź", 300m } }
            });

            // Inicjalizacja slotów w magazynie
            Warehouse["Ruda Miedzi"] = 0;   // wejście
            Warehouse["Węgiel"] = 0;         // wejście
            Warehouse["Miedź"] = 0;         // wyjście
        }
    }
}