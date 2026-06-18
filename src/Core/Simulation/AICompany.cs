using System;

namespace Conglomerate
{
    public enum AIType
    {
        Extractor,
        Manufacturer,
        Retailer
    }

    public class AICompany : Company
    {
        public AIType Specialization { get; set; }
        public bool IsOnMap { get; set; }
        public decimal TargetThreshold { get; set; }
        
        // This is a simplified market simulation variable
        // Determines how much money they make/lose per day while off-map
        public decimal DailyProfitPotential { get; set; }

        public AICompany(string name, decimal startingBalance, AIType specialization, decimal targetThreshold) 
            : base(name, startingBalance)
        {
            Specialization = specialization;
            IsOnMap = false;
            TargetThreshold = targetThreshold;
            
            // Randomize daily profit potential between 100 and 1000 roughly
            var rand = new Random();
            DailyProfitPotential = (decimal)(rand.NextDouble() * 900 + 100);
        }

        public void UpdateMarketSimulation(int currentDay)
        {
            if (IsOnMap) return;

            // Simple simulated profit/loss while they are purely a market force
            var rand = new Random();
            // Fluctuate around potential
            decimal dailyResult = DailyProfitPotential * (decimal)(0.5 + rand.NextDouble());
            
            if (dailyResult > 0)
            {
                AddTransaction(currentDay, 0, "Zyski z wolnego rynku (Symulacja AI)", dailyResult, "Sprzedaż");
            }
            else if (dailyResult < 0)
            {
                AddTransaction(currentDay, 0, "Straty z wolnego rynku (Symulacja AI)", dailyResult, "Utrzymanie");
            }
        }
    }
}
