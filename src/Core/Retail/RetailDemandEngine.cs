using System;
using System.Collections.Generic;

namespace Conglomerate.Retail
{
    /// <summary>
    /// Silnik popytu konsumenckiego — CAŁKOWICIE ODDZIELONY od logiki budynku.
    ///
    /// Wzorzec Strategy: RetailBuilding wywołuje RetailDemandEngine.CalculateHourlySales()
    /// zamiast implementować logikę sprzedaży wewnątrz siebie.
    ///
    /// Skalowalność: w przyszłości można:
    ///   - dodać CityPopulationFactor (więcej mieszkańców → wyższa sprzedaż)
    ///   - dodać SeasonalModifier (Święta → +30% sprzedaży żywności)
    ///   - dodać CompetitionPenalty (sklep obok → -15% sprzedaży)
    ///   - wstrzyknąć różne implementacje IRetailDemandStrategy
    /// </summary>
    public static class RetailDemandEngine
    {
        // ──────────────────────────────────────────────
        //  Wagi formuły atrakcyjności
        // ──────────────────────────────────────────────

        /// <summary>Waga jakości produktu (wyższa jakość → wyższy popyt).</summary>
        private const float QualityWeight = 0.25f;

        /// <summary>Waga ceny — im wyższa cena względem bazy, tym mniejszy popyt.</summary>
        private const float PriceWeight = 0.55f;

        /// <summary>Waga lokalizacji (centralny sklep → wyższy ruch).</summary>
        private const float ConvenienceWeight = 0.20f;

        /// <summary>Maksymalna liczba jednostek sprzedawanych na godzinę przy pełnej atrakcyjności.</summary>
        private const int MaxUnitsPerHour = 8;

        /// <summary>Minimalna atrakcyjność wymagana do dokonania jakiejkolwiek sprzedaży.</summary>
        private const float MinAttractivenessThreshold = 0.1f;

        // ──────────────────────────────────────────────
        //  Jakości produktów (skalowalne przez słownik)
        // ──────────────────────────────────────────────

        /// <summary>
        /// Mapa jakości bazowej produktów (0.0–1.0).
        /// W przyszłości może być zasilana przez system technologii lub jakości produkcji.
        /// </summary>
        private static readonly Dictionary<string, float> ProductQualityMap =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
            {
                { "Mleko",  0.70f },
                { "Mięso",  0.80f },
                { "Ser",    0.85f },
                { "Masło",  0.75f },
                { "Węgiel", 0.50f }, // Mniejszy popyt detaliczny — surowiec przemysłowy
            };

        // ──────────────────────────────────────────────
        //  Publiczne API
        // ──────────────────────────────────────────────

        /// <summary>
        /// Oblicza atrakcyjność produktu na danym slocie.
        ///
        /// Wzór: Attractiveness = (QualityWeight * Quality)
        ///                      - (PriceWeight * PriceRatio - 1)  ← kara za zbyt wysoką cenę
        ///                      + (ConvenienceWeight * LocationFactor)
        ///
        /// Wynik: 0.0 (brak popytu) do ~1.0 (maksymalny popyt).
        /// </summary>
        public static float CalculateAttractiveness(
            string productName,
            decimal retailPrice,
            decimal baseMarketPrice,
            float locationFactor)
        {
            float quality = ProductQualityMap.TryGetValue(productName, out var q) ? q : 0.5f;

            // PriceRatio: 1.0 = cena = baza, 2.0 = cena = 2x baza
            float priceRatio = baseMarketPrice > 0
                ? (float)(retailPrice / baseMarketPrice)
                : 1.5f;

            // Kara cenowa — cenę powyżej 1.0x bazy traktujemy jako negatywną
            float pricePenalty = Math.Max(0f, priceRatio - 1.0f);

            float attractiveness =
                QualityWeight * quality
                - PriceWeight * pricePenalty
                + ConvenienceWeight * locationFactor;

            return Math.Max(0f, attractiveness);
        }

        /// <summary>
        /// Na podstawie atrakcyjności oblicza ile jednostek zostanie sprzedanych w tej godzinie.
        ///
        /// Algorytm:
        ///   1. Atrakcyjność przekłada się liniowo na bazową sprzedaż (0–MaxUnitsPerHour).
        ///   2. Dodawana jest losowość ±20% (symulacja naturalnych wahań popytu).
        ///   3. Wynik ograniczony do dostępnego stanu magazynowego.
        /// </summary>
        public static int CalculateHourlySales(
            SalesSlot slot,
            float attractiveness,
            Random rng)
        {
            if (!slot.IsActive || slot.IsStockout) return 0;
            if (attractiveness < MinAttractivenessThreshold) return 0;

            // Baza sprzedaży proporcjonalna do atrakcyjności
            float baseUnits = attractiveness * MaxUnitsPerHour;

            // Losowość ±20%
            float randomFactor = 0.80f + (float)rng.NextDouble() * 0.40f; // 0.8 – 1.2
            int units = (int)Math.Round(baseUnits * randomFactor);

            // Ograniczenie do stanu na półce
            return Math.Min(units, slot.CurrentStock);
        }

        /// <summary>
        /// Zwraca bazową jakość produktu (używane przez UI do podglądu).
        /// </summary>
        public static float GetProductQuality(string productName) =>
            ProductQualityMap.TryGetValue(productName, out var q) ? q : 0.5f;

        /// <summary>
        /// Rejestruje nowy produkt z własną jakością (np. przy dodawaniu nowych kategorii).
        /// </summary>
        public static void RegisterProductQuality(string productName, float quality) =>
            ProductQualityMap[productName] = Math.Clamp(quality, 0f, 1f);
    }
}
