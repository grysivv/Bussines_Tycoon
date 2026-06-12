using System.Collections.Generic;

namespace Conglomerate.Production
{
    /// <summary>
    /// Definiuje jeden przepis produkcji: jakie surowce wejściowe są potrzebne,
    /// co powstaje na wyjściu, ile czasu zajmuje jeden cykl i jaki jest koszt operacyjny.
    /// Struktura jest w pełni data-driven — w przyszłości może być ładowana z JSON.
    /// </summary>
    public class RecipeDefinition
    {
        /// <summary>Unikalna nazwa/identyfikator przepisu (np. "Mleko->Ser").</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Czytelna dla gracza nazwa przepisu wyświetlana w UI.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Krótki opis przepisu (np. "Przetwarzanie mleka w ser").</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Słownik surowców wejściowych: klucz = nazwa surowca, wartość = ilość wymagana na cykl.
        /// Przykład: { "Mleko", 3 }
        /// </summary>
        public Dictionary<string, int> Inputs { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Słownik produktów wyjściowych: klucz = nazwa produktu, wartość = ilość wyprodukowana na cykl.
        /// Przykład: { "Ser", 1 }
        /// </summary>
        public Dictionary<string, int> Outputs { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Liczba godzin (ticków) potrzebnych do ukończenia jednego cyklu produkcyjnego.
        /// Allows fine-grained balancing — np. prosta obróbka = 1h, złożona = 8h.
        /// </summary>
        public int CycleDurationHours { get; set; } = 8;

        /// <summary>Koszt operacyjny (energia, robocizna) pobierany po każdym ukończonym cyklu.</summary>
        public decimal OperationalCostPerCycle { get; set; } = 0m;

        /// <summary>Cena sprzedaży każdego produktu wyjściowego (klucz = nazwa produktu).</summary>
        public Dictionary<string, decimal> OutputPrices { get; set; } = new Dictionary<string, decimal>();
    }
}
