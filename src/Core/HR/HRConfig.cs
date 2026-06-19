using System.Collections.Generic;

namespace Conglomerate.HR
{
    public class HRConfig
    {
        // Fatigue parameters
        public float FatigueIncreaseWorkday { get; set; } = 5.0f;
        public float FatigueDecreaseWeekend { get; set; } = 15.0f;

        // Satisfaction and efficiency parameters
        public float SatisfactionAdjustmentRate { get; set; } = 5.0f;
        public float LowSatisfactionThreshold { get; set; } = 20.0f;
        public int MonthsBeforeQuitting { get; set; } = 3;

        // Candidate pool configuration
        public int CandidatePoolRefreshDays { get; set; } = 7;
        public int MaxCandidatesCount { get; set; } = 5;

        // Market salaries configuration
        public Dictionary<string, decimal> CustomMarketSalaries { get; set; } = new Dictionary<string, decimal>();

        public decimal GetMarketSalary(string roleTitle, decimal defaultSalary)
        {
            if (CustomMarketSalaries.TryGetValue(roleTitle, out var customSalary))
            {
                return customSalary;
            }
            return defaultSalary;
        }
    }
}
