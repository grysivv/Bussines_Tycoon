using System;
using System.Collections.Generic;

namespace Conglomerate.Retail
{
    /// <summary>
    /// Silnik popytu konsumenckiego — CAŁKOWICIE ODDZIELONY od logiki budynku.
    /// Capitalism Lab style: cena, jakość, lokalizacja i Brand Awareness wpływają na popyt.
    /// </summary>
    public static class RetailDemandEngine
    {
        // ──────────────────────────────────────────────
        //  Wagi formuły atrakcyjności
        // ──────────────────────────────────────────────

        private const float QualityWeight      = 0.20f;
        private const float PriceWeight        = 0.45f;
        private const float ConvenienceWeight  = 0.15f;
        private const float BrandWeight        = 0.20f; // Brand Awareness (Capitalism Lab style)

        private const int   MaxUnitsPerHour    = 8;
        private const float MinAttractivenessThreshold = 0.05f;

        // ──────────────────────────────────────────────
        //  Jakości bazowe produktów (0.0–1.0)
        // ──────────────────────────────────────────────

        private static readonly Dictionary<string, float> ProductQualityMap =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
            {
                // Spożywcze
                { "Mleko",                0.70f },
                { "Mięso",                0.80f },
                { "Ser",                  0.85f },
                { "Masło",                0.75f },
                { "Chleb",                0.70f },
                { "Wyroby Cukiernicze",   0.80f },
                { "Żywność Pakowana",     0.72f },

                // Surowce przemysłowe (mały popyt detaliczny)
                { "Węgiel",               0.40f },
                { "Ruda Miedzi",          0.35f },
                { "Ruda Żelaza",          0.35f },
                { "Miedź",                0.55f },
                { "Stal",                 0.60f },
                { "Stal Premium",         0.80f },

                // Elektronika
                { "Komponenty Elektroniczne", 0.65f },
                { "Smartfon",             0.85f },
                { "Laptop",               0.88f },

                // Tekstylia
                { "Tkanina",              0.60f },
                { "Odzież",               0.75f },
                { "Odzież Premium",       0.90f },

                // Drewno i meble
                { "Drewno Przetarte",     0.55f },
                { "Meble",                0.78f },
                { "Meble Luksusowe",      0.93f },
            };

        // ──────────────────────────────────────────────
        //  Publiczne API
        // ──────────────────────────────────────────────

        /// <summary>
        /// Oblicza atrakcyjność produktu — uwzględnia cenę, jakość, lokalizację i Brand Awareness.
        /// </summary>
        public static float CalculateAttractiveness(
            string productName,
            decimal retailPrice,
            decimal baseMarketPrice,
            float locationFactor,
            float brandAwareness)
        {
            float quality = ProductQualityMap.TryGetValue(productName, out var q) ? q : 0.5f;

            float priceRatio = baseMarketPrice > 0
                ? (float)(retailPrice / baseMarketPrice)
                : 1.5f;

            float pricePenalty = Math.Max(0f, priceRatio - 1.0f);

            // Brand Awareness: 0-100 → 0.0-1.0
            float brandFactor = Math.Clamp(brandAwareness / 100f, 0f, 1f);

            float attractiveness =
                QualityWeight      * quality
                - PriceWeight      * pricePenalty
                + ConvenienceWeight * locationFactor
                + BrandWeight      * brandFactor;

            return Math.Max(0f, attractiveness);
        }

        /// <summary>Overload bez brand awareness (kompatybilność wsteczna).</summary>
        public static float CalculateAttractiveness(
            string productName,
            decimal retailPrice,
            decimal baseMarketPrice,
            float locationFactor)
            => CalculateAttractiveness(productName, retailPrice, baseMarketPrice, locationFactor, 0f);

        /// <summary>
        /// Na podstawie atrakcyjności oblicza ile jednostek zostanie sprzedanych w tej godzinie.
        /// </summary>
        public static int CalculateHourlySales(
            SalesSlot slot,
            float attractiveness,
            Random rng)
        {
            if (!slot.IsActive || slot.IsStockout) return 0;
            if (attractiveness < MinAttractivenessThreshold) return 0;

            float baseUnits = attractiveness * MaxUnitsPerHour;
            float randomFactor = 0.80f + (float)rng.NextDouble() * 0.40f;
            int units = (int)Math.Round(baseUnits * randomFactor);

            return Math.Min(units, slot.CurrentStock);
        }

        public static float GetProductQuality(string productName) =>
            ProductQualityMap.TryGetValue(productName, out var q) ? q : 0.5f;

        public static void RegisterProductQuality(string productName, float quality) =>
            ProductQualityMap[productName] = Math.Clamp(quality, 0f, 1f);
    }
}
