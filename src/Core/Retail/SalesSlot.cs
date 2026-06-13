using System;
using System.Collections.Generic;
using System.Linq;

namespace Conglomerate.Retail
{
    /// <summary>
    /// Reprezentuje jeden slot półkowy (shelf slot) w sklepie detalicznym.
    /// Każdy slot może sprzedawać jeden typ towaru po ustalonej cenie.
    ///
    /// Skalowalność: dodanie atrybutu jakości produktu, sezonowości
    /// czy bufferów promocyjnych to modyfikacja tylko tej klasy.
    /// </summary>
    public class SalesSlot
    {
        // ──────────────────────────────────────────────
        //  Identyfikacja slotu
        // ──────────────────────────────────────────────

        public int SlotIndex { get; set; }

        /// <summary>Nazwa produktu wystawionego na tym slocie (np. "Ser", "Mleko").</summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>Czy slot jest aktywny (posiada przypisany produkt).</summary>
        public bool IsActive => !string.IsNullOrEmpty(ProductName);

        // ──────────────────────────────────────────────
        //  Stan magazynowy
        // ──────────────────────────────────────────────

        /// <summary>Aktualny stan zapasów na tym slocie (szt.).</summary>
        public int CurrentStock { get; set; } = 0;

        /// <summary>Pojemność półki dla tego slotu (szt.).</summary>
        public int ShelfCapacity { get; set; } = 50;

        /// <summary>Czy zapasy są wyczerpane (stan zerowy → brak sprzedaży).</summary>
        public bool IsStockout => IsActive && CurrentStock <= 0;

        // ──────────────────────────────────────────────
        //  Cena i wycena
        // ──────────────────────────────────────────────

        /// <summary>
        /// Mnożnik ceny względem bazowej ceny rynkowej.
        /// Np. 1.5 = sprzedajesz 50% drożej niż cena referencyjna.
        /// </summary>
        public decimal PriceMultiplier { get; set; } = 1.5m;

        /// <summary>
        /// Bezpośrednio ustawiona cena detaliczna (zł/szt.).
        /// Jeśli > 0, nadpisuje wyliczenie z PriceMultiplier.
        /// </summary>
        public decimal DirectRetailPrice { get; set; } = 0m;

        /// <summary>
        /// Efektywna cena sprzedaży. Priorytet: DirectRetailPrice > PriceMultiplier * BasePrice.
        /// </summary>
        public decimal GetEffectivePrice(decimal baseMarketPrice)
        {
            if (DirectRetailPrice > 0)
                return DirectRetailPrice;
            return Math.Round(baseMarketPrice * PriceMultiplier, 2);
        }

        // ──────────────────────────────────────────────
        //  Statystyki 24h (rolling window)
        // ──────────────────────────────────────────────

        /// <summary>Historia sprzedaży godzinowej (ostatnie 24h). Klucz = numer godziny historycznej.</summary>
        private readonly Queue<HourlySaleRecord> _hourlyHistory = new Queue<HourlySaleRecord>();

        /// <summary>Łączna sprzedaż w ostatnich 24 godzinach.</summary>
        public int UnitsSoldLast24h => _hourlyHistory.Sum(r => r.UnitsSold);

        /// <summary>Łączny przychód w ostatnich 24 godzinach.</summary>
        public decimal RevenueLast24h => _hourlyHistory.Sum(r => r.Revenue);

        /// <summary>Ostatnia znana atrakcyjność (do debugowania/wyświetlenia).</summary>
        public float LastAttractiveness { get; set; } = 0f;

        /// <summary>Ile godzin slot był w stanie stockout w ostatnich 24h.</summary>
        public int StockoutHoursLast24h { get; private set; } = 0;

        // ──────────────────────────────────────────────
        //  API historii
        // ──────────────────────────────────────────────

        /// <summary>
        /// Rejestruje sprzedaż z danej godziny i przycinuje okno do 24h.
        /// </summary>
        public void RecordHourlySale(int unitsSold, decimal revenue, bool wasStockout)
        {
            _hourlyHistory.Enqueue(new HourlySaleRecord
            {
                UnitsSold   = unitsSold,
                Revenue     = revenue,
                WasStockout = wasStockout
            });

            // Utrzymuj okno 24 godzin
            while (_hourlyHistory.Count > 24)
                _hourlyHistory.Dequeue();

            // Przelicz stockout hours
            StockoutHoursLast24h = _hourlyHistory.Count(r => r.WasStockout);
        }

        public override string ToString() =>
            IsActive
                ? $"Slot {SlotIndex}: {ProductName} | {CurrentStock}/{ShelfCapacity} szt. | ${GetEffectivePrice(0):C}"
                : $"Slot {SlotIndex}: [Pusty]";
    }

    /// <summary>Rekord sprzedaży z jednej godziny gry.</summary>
    public class HourlySaleRecord
    {
        public int     UnitsSold   { get; set; }
        public decimal Revenue     { get; set; }
        public bool    WasStockout { get; set; }
    }
}
