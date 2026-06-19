using System;
using System.Collections.Generic;
using System.Linq;

namespace Conglomerate.Finance
{
    /// <summary>
    /// Giełda Papierów Wartościowych — zarządza notowaniami wszystkich spółek,
    /// umożliwia graczowi kupno i sprzedaż akcji oraz wylicza przejęcia.
    /// </summary>
    public class StockMarket
    {
        private readonly Random _rng = new Random(123);
        private readonly Dictionary<string, StockListing> _listings = new Dictionary<string, StockListing>();

        public IReadOnlyDictionary<string, StockListing> Listings => _listings;

        public StockMarket()
        {
        }

        // ─────────────────────────────────────────────
        //  Rejestracja spółek
        // ─────────────────────────────────────────────

        /// <summary>Dodaje lub aktualizuje notowanie firmy.</summary>
        public void RegisterCompany(string companyName, decimal totalShares, decimal initialNetWorth)
        {
            if (!_listings.ContainsKey(companyName))
            {
                decimal initialPrice = totalShares > 0 ? Math.Max(1m, initialNetWorth / totalShares) : 10m;
                _listings[companyName] = new StockListing(companyName, totalShares, initialPrice);
            }
        }

        public StockListing? GetListing(string companyName)
        {
            _listings.TryGetValue(companyName, out var listing);
            return listing;
        }

        // ─────────────────────────────────────────────
        //  Aktualizacja cen (co dzień)
        // ─────────────────────────────────────────────

        /// <summary>Wywoływane przez GameManager raz na dobę — odświeża ceny wszystkich spółek.</summary>
        public void OnNewDay(IEnumerable<(string name, decimal netWorth, decimal monthlyRevenue, decimal monthlyProfit, decimal cash)> companyData)
        {
            foreach (var data in companyData)
            {
                if (_listings.TryGetValue(data.name, out var listing))
                {
                    listing.LastKnownCash = data.cash;
                    listing.LastKnownNetWorth = data.netWorth;
                    listing.LastMonthlyRevenue = data.monthlyRevenue;
                    listing.LastMonthlyProfit = data.monthlyProfit;
                    listing.RecalculateSharePrice(_rng);
                }
            }
        }

        // ─────────────────────────────────────────────
        //  Transakcje giełdowe
        // ─────────────────────────────────────────────

        /// <summary>
        /// Gracz kupuje akcje danej spółki.
        /// Zwraca true jeśli transakcja się powiodła.
        /// </summary>
        public bool BuyShares(string companyName, decimal shares, Company playerCompany, int day, int hour)
        {
            if (!_listings.TryGetValue(companyName, out var listing)) return false;
            if (shares <= 0) return false;

            decimal availableShares = listing.TotalShares - listing.PlayerOwnedShares;
            decimal sharesToBuy = Math.Min(shares, availableShares);
            if (sharesToBuy <= 0) return false;

            decimal totalCost = sharesToBuy * listing.SharePrice;
            if (playerCompany.Balance < totalCost) return false;

            playerCompany.Balance -= totalCost;
            listing.PlayerOwnedShares += sharesToBuy;

            // Dodaj do portfolio gracza
            if (!playerCompany.OwnedShares.ContainsKey(companyName))
                playerCompany.OwnedShares[companyName] = 0m;
            playerCompany.OwnedShares[companyName] += sharesToBuy;

            playerCompany.AddTransaction(day, hour,
                $"Zakup akcji: {sharesToBuy:F0}x {companyName} @ {listing.SharePrice:C}",
                -totalCost, "Inwestycje", "");

            return true;
        }

        /// <summary>
        /// Gracz sprzedaje akcje danej spółki.
        /// Zwraca true jeśli transakcja się powiodła.
        /// </summary>
        public bool SellShares(string companyName, decimal shares, Company playerCompany, int day, int hour)
        {
            if (!_listings.TryGetValue(companyName, out var listing)) return false;
            if (!playerCompany.OwnedShares.TryGetValue(companyName, out var owned) || owned <= 0) return false;

            decimal sharesToSell = Math.Min(shares, owned);
            if (sharesToSell <= 0) return false;

            decimal totalRevenue = sharesToSell * listing.SharePrice;
            playerCompany.Balance += totalRevenue;
            listing.PlayerOwnedShares -= sharesToSell;
            playerCompany.OwnedShares[companyName] -= sharesToSell;

            playerCompany.AddTransaction(day, hour,
                $"Sprzedaż akcji: {sharesToSell:F0}x {companyName} @ {listing.SharePrice:C}",
                totalRevenue, "Inwestycje", "");

            return true;
        }

        // ─────────────────────────────────────────────
        //  Kontrola przejęcia
        // ─────────────────────────────────────────────

        /// <summary>Czy gracz przejął kontrolę nad spółką (>50% udziałów)?</summary>
        public bool HasTakenOver(string companyName)
        {
            if (!_listings.TryGetValue(companyName, out var listing)) return false;
            return listing.PlayerOwnershipPercent > 50m;
        }

        // ─────────────────────────────────────────────
        //  Rankingi
        // ─────────────────────────────────────────────

        /// <summary>Zwraca wszystkie spółki posortowane wg kapitalizacji (malejąco).</summary>
        public List<StockListing> GetRanking()
        {
            return _listings.Values.OrderByDescending(l => l.MarketCap).ToList();
        }

        /// <summary>Wartość portfela akcji gracza (po aktualnych cenach).</summary>
        public decimal GetPlayerPortfolioValue(Company playerCompany)
        {
            decimal total = 0m;
            foreach (var kvp in playerCompany.OwnedShares)
            {
                if (_listings.TryGetValue(kvp.Key, out var listing))
                    total += kvp.Value * listing.SharePrice;
            }
            return total;
        }
    }
}
