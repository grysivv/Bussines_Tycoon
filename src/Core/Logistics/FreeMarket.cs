using System;
using System.Collections.Generic;
using Conglomerate;

namespace Conglomerate.Logistics
{
    /// <summary>
    /// Reprezentuje ofertę rynkową dla jednego surowca.
    /// Cena zmienia się codziennie w zakresie PremiumMin–PremiumMax względem ceny bazowej.
    /// </summary>
    public class MarketListing
    {
        public string ResourceName { get; set; } = string.Empty;

        /// <summary>Bazowa cena referencyjna (z ResourceRegistry).</summary>
        public decimal BasePrice { get; set; }

        /// <summary>Aktualnie obowiązująca cena rynkowa (fluktuuje codziennie).</summary>
        public decimal CurrentPrice { get; set; }

        /// <summary>Minimalny mnożnik premii (np. 1.4 = 40% drożej niż produkcja).</summary>
        public decimal PremiumMin { get; set; } = 1.4m;

        /// <summary>Maksymalny mnożnik premii (np. 2.2 = 2.2x ceny bazowej).</summary>
        public decimal PremiumMax { get; set; } = 2.2m;

        /// <summary>Dostępna ilość na rynku na ten dzień (odnawialna co dzień).</summary>
        public int DailyAvailability { get; set; } = 500;

        /// <summary>Ile już kupiono dzisiaj.</summary>
        public int TodayPurchased { get; set; } = 0;

        public int RemainingToday => Math.Max(0, DailyAvailability - TodayPurchased);
    }

    /// <summary>
    /// Wolny Rynek — globalna giełda surowców.
    /// Pozwala graczowi kupić dowolny surowiec natychmiastowo za gotówkę,
    /// po cenie wyższej niż koszt własnej produkcji.
    /// Ceny fluktuują każdego dnia gry.
    /// </summary>
    public class FreeMarket
    {
        private readonly Random _rng;
        private int _lastUpdateDay = -1;

        /// <summary>Wszystkie dostępne surowce na rynku (klucz = nazwa surowca).</summary>
        public Dictionary<string, MarketListing> Listings { get; } = new Dictionary<string, MarketListing>();

        public FreeMarket(int seed = 42)
        {
            _rng = new Random(seed);
            InitializeListings();
        }

        // ──────────────────────────────────────────────
        //  Inicjalizacja katalogu rynkowego
        // ──────────────────────────────────────────────

        private void InitializeListings()
        {
            // Surowce rolne
            AddListing("Mleko",   basePrice: 50m,  premiumMin: 1.5m, premiumMax: 2.0m, dailyAvail: 300);
            AddListing("Mięso",   basePrice: 150m, premiumMin: 1.4m, premiumMax: 1.9m, dailyAvail: 150);

            // Produkty przetworzone (rzadziej dostępne, wyższa premia)
            AddListing("Ser",     basePrice: 220m, premiumMin: 1.3m, premiumMax: 1.7m, dailyAvail: 100);
            AddListing("Masło",   basePrice: 130m, premiumMin: 1.3m, premiumMax: 1.7m, dailyAvail: 120);

            // Surowce kopalniane
            AddListing("Węgiel",  basePrice: 100m, premiumMin: 1.4m, premiumMax: 2.0m, dailyAvail: 500);
            AddListing("Ruda Miedzi", basePrice: 150m, premiumMin: 1.4m, premiumMax: 2.0m, dailyAvail: 400);

            // Ustal ceny na start
            foreach (var listing in Listings.Values)
                listing.CurrentPrice = RollNewPrice(listing);
        }

        private void AddListing(string name, decimal basePrice, decimal premiumMin, decimal premiumMax, int dailyAvail)
        {
            Listings[name] = new MarketListing
            {
                ResourceName = name,
                BasePrice    = basePrice,
                CurrentPrice = basePrice * premiumMin, // startowa cena
                PremiumMin   = premiumMin,
                PremiumMax   = premiumMax,
                DailyAvailability = dailyAvail,
                TodayPurchased    = 0
            };
        }

        // ──────────────────────────────────────────────
        //  Aktualizacja cen (raz na dzień)
        // ──────────────────────────────────────────────

        /// <summary>
        /// Wywoływana przez GameManager o północy każdego dnia.
        /// Odnawia dostępność i fluktuuje ceny.
        /// </summary>
        public void OnNewDay(int day)
        {
            if (day == _lastUpdateDay) return;
            _lastUpdateDay = day;

            foreach (var listing in Listings.Values)
            {
                listing.CurrentPrice   = RollNewPrice(listing);
                listing.TodayPurchased = 0;
            }
        }

        private decimal RollNewPrice(MarketListing listing)
        {
            double t = _rng.NextDouble(); // 0.0–1.0
            decimal premium = listing.PremiumMin + (decimal)t * (listing.PremiumMax - listing.PremiumMin);
            // Zaokrąglij do pełnych złotych
            return Math.Round(listing.BasePrice * premium, 0);
        }

        // ──────────────────────────────────────────────
        //  Zakup surowców
        // ──────────────────────────────────────────────

        /// <summary>
        /// Kupuje <paramref name="amount"/> jednostek surowca z rynku i wkłada do magazynu <paramref name="targetBuilding"/>.
        /// Zwraca true jeśli transakcja się powiodła.
        /// </summary>
        public bool BuyResource(
            string resourceName,
            int amount,
            Building targetBuilding,
            Company company,
            int day, int hour)
        {
            if (!Listings.TryGetValue(resourceName, out var listing))
                return false;

            if (amount <= 0) return false;
            if (amount > listing.RemainingToday) return false;

            // Sprawdź pojemność magazynu celu
            int freeSpace = targetBuilding.WarehouseCapacity - targetBuilding.GetTotalStock();
            if (freeSpace <= 0) return false;

            int actualAmount = Math.Min(amount, freeSpace);
            decimal totalCost = listing.CurrentPrice * actualAmount;

            if (company.Balance < totalCost) return false;

            // Wykonaj zakup
            company.Balance -= totalCost;

            if (!targetBuilding.Warehouse.ContainsKey(resourceName))
                targetBuilding.Warehouse[resourceName] = 0;
            targetBuilding.Warehouse[resourceName] += actualAmount;

            listing.TodayPurchased += actualAmount;

            company.AddTransaction(day, hour,
                $"Zakup rynkowy: {actualAmount}x {resourceName} → {targetBuilding.Name}",
                -totalCost, "Zakup surowców", targetBuilding.FacilityId);

            return true;
        }

        /// <summary>Zwraca aktualną cenę rynkową surowca lub 0 jeśli nie ma go w ofercie.</summary>
        public decimal GetCurrentPrice(string resourceName) =>
            Listings.TryGetValue(resourceName, out var l) ? l.CurrentPrice : 0m;

        /// <summary>Czy dany surowiec jest dostępny na rynku?</summary>
        public bool IsAvailable(string resourceName) =>
            Listings.ContainsKey(resourceName) && Listings[resourceName].RemainingToday > 0;
    }
}
