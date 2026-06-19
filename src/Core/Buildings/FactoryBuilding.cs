using System;
using System.Collections.Generic;
using System.Linq;
using Conglomerate.Production;

namespace Conglomerate
{
    /// <summary>
    /// Klasa bazowa dla wszystkich budynków przetwórczych (fabryk).
    /// Dziedziczy po Building i dodaje system przepisów produkcji (RecipeDefinition).
    /// 
    /// Architektura:
    ///  - Fabryka przechowuje listę dostępnych przepisów (AvailableRecipes).
    ///  - Gracz może wybrać aktywny przepis (ActiveRecipe).
    ///  - Silnik gry w każdym ticku wywołuje TryAdvanceProduction().
    ///  - Po ukończeniu cyklu produkty trafiają do Warehouse.
    /// 
    /// Skalowalność: dodanie nowego produktu = dodanie nowego RecipeDefinition.
    /// Nie wymaga zmian w kodzie tej klasy.
    /// </summary>
    public abstract class FactoryBuilding : Building
    {
        // ──────────────────────────────────────────────
        //  Przepisy
        // ──────────────────────────────────────────────

        /// <summary>Lista wszystkich przepisów dostępnych w tym zakładzie.</summary>
        public List<RecipeDefinition> AvailableRecipes { get; protected set; } = new List<RecipeDefinition>();

        /// <summary>Aktualnie wybrany przez gracza przepis. Null = zakład na Idle.</summary>
        public RecipeDefinition? ActiveRecipe { get; private set; }

        // ──────────────────────────────────────────────
        //  Stan produkcji
        // ──────────────────────────────────────────────

        /// <summary>Aktualny stan operacyjny zakładu.</summary>
        public FacilityState State { get; private set; } = FacilityState.Idle;

        /// <summary>
        /// Liczba godzin przepracowanych w bieżącym cyklu produkcyjnym.
        /// Od 0 do ActiveRecipe.CycleDurationHours.
        /// </summary>
        public int ProductionProgressHours { get; private set; } = 0;

        /// <summary>Postęp bieżącego cyklu jako wartość od 0.0 do 1.0 (dla paska postępu w UI).</summary>
        public float ProductionProgressNormalized =>
            ActiveRecipe == null || ActiveRecipe.CycleDurationHours <= 0
                ? 0f
                : (float)ProductionProgressHours / ActiveRecipe.CycleDurationHours;

        /// <summary>Liczba ukończonych cykli produkcyjnych od początku gry.</summary>
        public int TotalCyclesCompleted { get; private set; } = 0;

        // ──────────────────────────────────────────────
        //  Konstruktor
        // ──────────────────────────────────────────────

        protected FactoryBuilding(string name) : base(name)
        {
        }

        // ──────────────────────────────────────────────
        //  API dla gracza
        // ──────────────────────────────────────────────

        /// <summary>
        /// Ustawia aktywny przepis produkcji.
        /// Jeśli zmieniony, resetuje postęp bieżącego cyklu.
        /// </summary>
        public void SetRecipe(RecipeDefinition? recipe)
        {
            if (ActiveRecipe?.Id != recipe?.Id)
            {
                ActiveRecipe = recipe;
                ProductionProgressHours = 0;
                State = recipe == null ? FacilityState.Idle : FacilityState.WaitingForInputs;
            }
        }

        // ──────────────────────────────────────────────
        //  Silnik produkcji — wywoływany co godzinę
        // ──────────────────────────────────────────────

        /// <summary>
        /// Główna metoda silnika gry — wywoływana przez GameManager co każdy tick (1h).
        /// Próbuje wykonać jeden krok produkcji zgodnie z aktywnym przepisem.
        /// </summary>
        public override bool Produce(Company company)
        {
            // Pobierz koszty utrzymania (płacone raz na dobę — zachowane ze starego systemu)
            // Uwaga: ta metoda jest teraz wywoływana co tick, ale opłata dzienna powinna być
            // kontrolowana przez GameManager. Zostawiamy tu flagę dla klarowności.
            return TryAdvanceProduction(company);
        }

        /// <summary>
        /// Jeden krok (1 godzina) silnika produkcji.
        /// Zwraca true jeśli cykl został ukończony w tej godzinie.
        /// </summary>
        public bool TryAdvanceProduction(Company company)
        {
            if (ActiveRecipe == null)
            {
                State = FacilityState.Idle;
                return false;
            }

            // Sprawdź czy mamy surowce na początku nowego cyklu
            if (ProductionProgressHours == 0)
            {
                if (!HasRequiredInputs())
                {
                    State = FacilityState.WaitingForInputs;
                    return false;
                }

                if (IsOutputStorageFull())
                {
                    State = FacilityState.OutputStorageFull;
                    return false;
                }

                if (company.Balance < ActiveRecipe.OperationalCostPerCycle)
                {
                    State = FacilityState.InsufficientFunds;
                    return false;
                }

                // Pobierz surowce i koszty na początku cyklu
                ConsumeInputs();
                company.Balance -= ActiveRecipe.OperationalCostPerCycle;
            }

            // Postęp o jedną godzinę
            State = FacilityState.Producing;
            ProductionProgressHours++;

            // Sprawdź czy cykl ukończony
            if (ProductionProgressHours >= ActiveRecipe.CycleDurationHours)
            {
                // Wyprodukuj wyjście
                ProduceOutputs();
                TotalCyclesCompleted++;
                ProductionProgressHours = 0;
                return true;
            }

            return false;
        }

        // ──────────────────────────────────────────────
        //  Pomocnicze
        // ──────────────────────────────────────────────

        private bool HasRequiredInputs()
        {
            if (ActiveRecipe == null) return false;
            foreach (var input in ActiveRecipe.Inputs)
            {
                if (!Warehouse.ContainsKey(input.Key) || Warehouse[input.Key].Quantity < input.Value)
                    return false;
            }
            return true;
        }

        private bool IsOutputStorageFull()
        {
            return GetTotalStock() >= WarehouseCapacity;
        }

        private void ConsumeInputs()
        {
            if (ActiveRecipe == null) return;
            foreach (var input in ActiveRecipe.Inputs)
            {
                RemoveProduct(input.Key, input.Value);
            }
        }

        private void ProduceOutputs()
        {
            if (ActiveRecipe == null) return;
            decimal freeSpace = WarehouseCapacity - GetTotalStock();
            foreach (var output in ActiveRecipe.Outputs)
            {
                decimal toAdd = Math.Min(output.Value, freeSpace);
                AddProduct(output.Key, toAdd, 10m * (decimal)WorkerExperience);
                freeSpace -= toAdd;
            }
        }

        // ──────────────────────────────────────────────
        //  ResourcePrices — z aktywnego przepisu lub pusto
        // ──────────────────────────────────────────────

        public override Dictionary<string, decimal> ResourcePrices
        {
            get
            {
                var prices = new Dictionary<string, decimal>();
                // Zbierz ceny ze wszystkich przepisów (dla AutoSell)
                foreach (var recipe in AvailableRecipes)
                {
                    foreach (var kvp in recipe.OutputPrices)
                    {
                        if (!prices.ContainsKey(kvp.Key))
                            prices[kvp.Key] = kvp.Value;
                    }
                }
                return prices;
            }
        }
    }
}
