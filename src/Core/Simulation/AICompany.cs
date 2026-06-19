using System;
using System.Collections.Generic;
using System.Linq;

namespace Conglomerate
{
    public enum AIType
    {
        Extractor,
        Manufacturer,
        Retailer,
        Conglomerate  // Pełny łańcuch: wydobycie → fabryka → sklep
    }

    public enum AIStrategy
    {
        Aggressive,    // Szybka ekspansja, wysokie ryzyko
        Conservative,  // Powolna ekspansja, niskie ryzyko
        Balanced       // Równowaga
    }

    /// <summary>
    /// Firma AI — rozszerzona o strategię, dane giełdowe i pełny łańcuch dostaw.
    /// Wzorowana na Capitalism Lab: AI buduje wiele budynków i reaguje na gracza.
    /// </summary>
    public class AICompany : Company
    {
        private readonly Random _rnd = new Random();

        public AIType Specialization { get; set; }
        public AIStrategy Strategy { get; set; } = AIStrategy.Balanced;
        public bool IsOnMap { get; set; }
        public decimal TargetThreshold { get; set; }
        public decimal DailyProfitPotential { get; set; }

        // ── Dane giełdowe ──────────────────────────────────────
        public decimal TotalShares { get; set; } = 10000m;  // 10 000 akcji w obiegu

        // ── Kontrola ekspansji ─────────────────────────────────
        public int BuildingsOnMap { get; set; } = 0;
        public int MaxBuildingsAllowed => Strategy switch
        {
            AIStrategy.Aggressive    => 8,
            AIStrategy.Conservative  => 3,
            _                        => 5
        };

        // ── Historia wyników ───────────────────────────────────
        public List<decimal> DailyRevenueHistory { get; } = new List<decimal>();
        public decimal LastMonthRevenue { get; private set; }
        public decimal LastMonthProfit  { get; private set; }

        public AICompany(string name, decimal startingBalance, AIType specialization, decimal targetThreshold,
                         AIStrategy strategy = AIStrategy.Balanced)
            : base(name, startingBalance)
        {
            Specialization = specialization;
            Strategy = strategy;
            IsOnMap = false;
            TargetThreshold = targetThreshold;
            DailyProfitPotential = (decimal)(_rnd.NextDouble() * 900 + 100);
        }

        /// <summary>
        /// Symulacja rynkowa gdy AI nie ma budynków na mapie.
        /// Uproszczony model — AI akumuluje kapitał.
        /// </summary>
        public void UpdateMarketSimulation(int currentDay)
        {
            if (IsOnMap) return;

            decimal dailyResult = DailyProfitPotential * (decimal)(0.5 + _rnd.NextDouble());
            decimal strategyMultiplier = Strategy switch
            {
                AIStrategy.Aggressive   => 1.5m,
                AIStrategy.Conservative => 0.7m,
                _                       => 1.0m
            };
            dailyResult *= strategyMultiplier;

            if (dailyResult > 0)
            {
                Balance += dailyResult;
                LastMonthRevenue += dailyResult;
                LastMonthProfit += dailyResult * 0.3m;
                AddTransaction(currentDay, 0, "Zyski z wolnego rynku (Symulacja AI)", dailyResult, "Sprzedaż");
            }
        }

        /// <summary>Aktualizuje dane miesięczne dla giełdy.</summary>
        public void CloseMonthForStock()
        {
            LastMonthRevenue = 0m;
            LastMonthProfit = 0m;
        }

        /// <summary>Wylicza bazową wartość netto AI (używana do wyceny akcji).</summary>
        public decimal GetEstimatedNetWorth()
        {
            decimal buildingValue = Buildings.Count * 150000m; // Uproszczone
            return Balance + buildingValue - Engine.Loans;
        }
    }
}
