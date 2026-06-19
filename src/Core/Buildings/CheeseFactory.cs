using System.Collections.Generic;
using Conglomerate.Production;

namespace Conglomerate
{
    /// <summary>
    /// Ser fabryka — przetwarza Mleko w Ser.
    /// Przykładowy budynek przetwórczy demonstrujący system przepisów.
    /// 
    /// Dostępne przepisy:
    ///   1. Mleko → Ser (3 Mleko = 1 Ser, 6 godzin)
    ///   2. Mleko → Masło (4 Mleko = 2 Masło, 4 godziny) [przyszłe rozszerzenie, gotowy slot]
    /// 
    /// Surowce wejściowe (Mleko) muszą być dostarczone do Warehouse przed startem cyklu.
    /// Produkty wyjściowe trafiają do Warehouse po zakończeniu cyklu.
    /// </summary>
    public class CheeseFactory : FactoryBuilding
    {
        public override string ActivityType => "Mleczarnia / Ser";
        public override decimal BuildCost => 25000m;
        public override decimal MaintenanceCost => 300m;
        public override int WarehouseCapacity => 60;

        public CheeseFactory(string name) : base(name)
        {
            // ──────────────────────────────────────────────
            //  Definicje przepisów
            // ──────────────────────────────────────────────

            AvailableRecipes.Add(new RecipeDefinition
            {
                Id = "milk_to_cheese",
                DisplayName = "Mleko → Ser",
                Description = "Pasteryzacja i koagulacja mleka. 3 jednostki mleka dają 1 ser.",
                Inputs = new Dictionary<string, int> { { "Mleko", 3 } },
                Outputs = new Dictionary<string, int> { { "Ser", 1 } },
                CycleDurationHours = 6,
                OperationalCostPerCycle = 50m,
                OutputPrices = new Dictionary<string, decimal> { { "Ser", 220m } }
            });

            AvailableRecipes.Add(new RecipeDefinition
            {
                Id = "milk_to_butter",
                DisplayName = "Mleko → Masło",
                Description = "Odwirowywanie śmietany i ubijanie masła. 4 jednostki mleka dają 2 masła.",
                Inputs = new Dictionary<string, int> { { "Mleko", 4 } },
                Outputs = new Dictionary<string, int> { { "Masło", 2 } },
                CycleDurationHours = 4,
                OperationalCostPerCycle = 40m,
                OutputPrices = new Dictionary<string, decimal> { { "Masło", 130m } }
            });

            // Inicjalizacja slotów w magazynie
            Warehouse["Mleko"] = new Economy.ProductBatch("Mleko", 0);   // wejście
            Warehouse["Ser"] = new Economy.ProductBatch("Ser", 0);     // wyjście
            Warehouse["Masło"] = new Economy.ProductBatch("Masło", 0);   // wyjście drugiego przepisu
        }
    }
}
