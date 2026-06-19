using System;

namespace Conglomerate.HR
{
    public class Employee
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public JobRole Role { get; private set; }
        public decimal MonthlySalary { get; private set; }
        public float Efficiency { get; private set; }
        public float Satisfaction { get; private set; } // 0 to 100
        public float Fatigue { get; private set; }      // 0 to 100
        public bool IsPlanningToQuit { get; private set; }
        public int MonthsWithLowSatisfaction { get; private set; }

        public Employee(Guid id, string name, JobRole role, decimal monthlySalary, float satisfaction = 80f, float fatigue = 0f)
        {
            Id = id;
            Name = name;
            Role = role;
            MonthlySalary = monthlySalary;
            Satisfaction = Math.Clamp(satisfaction, 0f, 100f);
            Fatigue = Math.Clamp(fatigue, 0f, 100f);
            RecalculateEfficiency();
        }

        public void AdjustSalary(decimal newSalary)
        {
            if (newSalary < 0) throw new ArgumentException("Salary cannot be negative.");
            MonthlySalary = newSalary;
        }

        public void RecalculateEfficiency()
        {
            // Efficiency = (Satisfaction / 100) * (1.0 - Fatigue / 100)
            Efficiency = (Satisfaction / 100f) * (1.0f - (Fatigue / 100f));
            Efficiency = Math.Clamp(Efficiency, 0f, 1f);
        }

        public void UpdateState(bool isWeekend, HRConfig config)
        {
            // Increase fatigue on workdays, decrease on weekends
            if (isWeekend)
            {
                Fatigue = Math.Max(0f, Fatigue - config.FatigueDecreaseWeekend);
            }
            else
            {
                Fatigue = Math.Min(100f, Fatigue + config.FatigueIncreaseWorkday);
            }

            // Adjust satisfaction based on how close MonthlySalary is to market salary
            decimal marketSalary = config.GetMarketSalary(Role.Title, Role.BaseMarketSalary);
            float salaryRatio = marketSalary > 0 ? (float)(MonthlySalary / marketSalary) : 1f;

            // Target satisfaction based on salary compared to market
            float targetSatisfaction = Math.Clamp(salaryRatio * 100f, 0f, 100f);

            if (Satisfaction < targetSatisfaction)
            {
                Satisfaction = Math.Min(targetSatisfaction, Satisfaction + config.SatisfactionAdjustmentRate);
            }
            else if (Satisfaction > targetSatisfaction)
            {
                Satisfaction = Math.Max(targetSatisfaction, Satisfaction - config.SatisfactionAdjustmentRate);
            }

            RecalculateEfficiency();
        }

        public void CheckQuittingStatus(HRConfig config)
        {
            if (Satisfaction < config.LowSatisfactionThreshold)
            {
                MonthsWithLowSatisfaction++;
                if (MonthsWithLowSatisfaction >= config.MonthsBeforeQuitting)
                {
                    IsPlanningToQuit = true;
                }
            }
            else
            {
                MonthsWithLowSatisfaction = 0;
                IsPlanningToQuit = false; // Reset if they become satisfied
            }
        }
    }
}
