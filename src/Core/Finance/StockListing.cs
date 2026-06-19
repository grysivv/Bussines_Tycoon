using System;
using System.Collections.Generic;

namespace Conglomerate.Finance
{
    /// <summary>
    /// Reprezentuje notowanie giełdowe jednej spółki.
    /// Cena akcji jest wyliczana dynamicznie na podstawie kondycji finansowej firmy.
    /// </summary>
    public class StockListing
    {
        public string CompanyName { get; set; }
        public decimal TotalShares { get; set; }          // Łączna liczba akcji w obiegu
        public decimal PlayerOwnedShares { get; set; }    // Ile akcji posiada gracz
        public decimal SharePrice { get; private set; }   // Aktualna cena akcji
        public List<decimal> PriceHistory { get; } = new List<decimal>(); // Historia cen (ostatnie 30 dni)

        // Wskaźniki fundamentalne (aktualizowane co dzień przez StockMarket)
        public decimal LastKnownCash { get; set; }
        public decimal LastKnownNetWorth { get; set; }
        public decimal LastMonthlyRevenue { get; set; }
        public decimal LastMonthlyProfit { get; set; }

        public StockListing(string companyName, decimal totalShares, decimal initialPrice)
        {
            CompanyName = companyName;
            TotalShares = totalShares;
            SharePrice = initialPrice;
            PriceHistory.Add(initialPrice);
        }

        /// <summary>Wartość rynkowa całej spółki (Market Cap).</summary>
        public decimal MarketCap => TotalShares * SharePrice;

        /// <summary>Udział gracza (0-100%).</summary>
        public decimal PlayerOwnershipPercent => TotalShares > 0 ? (PlayerOwnedShares / TotalShares) * 100m : 0m;

        /// <summary>
        /// Przelicza cenę akcji na podstawie wskaźników fundamentalnych.
        /// Model: Price = (Net Worth / TotalShares) * PEMultiplier * MarketSentiment
        /// </summary>
        public void RecalculateSharePrice(Random rng)
        {
            if (TotalShares <= 0) return;

            // Podstawowa wartość = majątek netto / liczba akcji
            decimal bookValue = LastKnownNetWorth / TotalShares;

            // Mnożnik P/E (1.5 - 3.0x) zależy od rentowności
            decimal peMultiplier = 1.5m;
            if (LastMonthlyProfit > 0 && LastMonthlyRevenue > 0)
            {
                decimal margin = LastMonthlyProfit / LastMonthlyRevenue;
                peMultiplier = 1.5m + Math.Min(margin * 5m, 2.5m); // max 4.0x przy 50% marży
            }

            // Losowa fluktuacja rynkowa (-3% do +3% dziennie)
            double noise = 1.0 + (rng.NextDouble() * 0.06 - 0.03);

            decimal newPrice = Math.Max(1m, Math.Round(bookValue * peMultiplier * (decimal)noise, 2));
            SharePrice = newPrice;

            // Historia (max 60 wpisów)
            PriceHistory.Add(newPrice);
            if (PriceHistory.Count > 60)
                PriceHistory.RemoveAt(0);
        }

        public void UpdateSharePrice(decimal price)
        {
            SharePrice = Math.Max(1m, price);
            PriceHistory.Add(SharePrice);
            if (PriceHistory.Count > 60)
                PriceHistory.RemoveAt(0);
        }
    }
}
