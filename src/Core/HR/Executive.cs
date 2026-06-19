using System;
using System.Collections.Generic;

namespace Conglomerate.HR
{
    /// <summary>Typ menedżera (C-Suite Executive).</summary>
    public enum ExecutiveType
    {
        CEO,    // Ogólny bonus: +10% do Net Income, -5% koszty
        COO,    // Operacyjny: +15% efektywność fabryk, -10% maintenance
        CMO,    // Marketing: +25% Brand Awareness growth, +10% popyt sklepów
        CTO,    // Technologia: +20% szybkość R&D, +15% jakość produktów
        CFO,    // Finanse: -2pp podatek, lepsza zdolność kredytowa
        CSO     // Sprzedaż: +20% przychody sklepów detalicznych
    }

    /// <summary>
    /// Menedżer wyższego szczebla (C-Suite Executive).
    /// Wzorowany na Capitalism Lab: każdy exec daje konkretny bonus do danej sfery.
    /// </summary>
    public class Executive
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; set; }
        public ExecutiveType Type { get; set; }
        public decimal MonthlySalary { get; set; }
        public float SkillLevel { get; set; }         // 1-10 (wpływa na siłę bonusu)
        public string BonusDescription { get; set; }

        // Mnożniki bonusów (zależą od SkillLevel)
        public float FactoryEfficiencyBonus    => Type == ExecutiveType.COO ? 0.05f + SkillLevel * 0.015f : 0f;
        public float MaintenanceCostReduction  => Type == ExecutiveType.COO ? 0.02f + SkillLevel * 0.008f : 0f;
        public float BrandAwarenessMultiplier  => Type == ExecutiveType.CMO ? 1f + 0.1f + SkillLevel * 0.025f : 1f;
        public float RetailDemandBonus         => Type is ExecutiveType.CMO or ExecutiveType.CSO ? 0.05f + SkillLevel * 0.015f : 0f;
        public float RnDSpeedBonus             => Type == ExecutiveType.CTO ? 0.1f + SkillLevel * 0.02f : 0f;
        public float ProductQualityBonus       => Type == ExecutiveType.CTO ? 0.05f + SkillLevel * 0.01f : 0f;
        public float TaxRateReduction          => Type == ExecutiveType.CFO ? 0.005f + SkillLevel * 0.002f : 0f;
        public float NetIncomeBonus            => Type == ExecutiveType.CEO ? 0.05f + SkillLevel * 0.01f : 0f;

        public Executive(string name, ExecutiveType type, decimal salary, float skillLevel)
        {
            Name = name;
            Type = type;
            MonthlySalary = salary;
            SkillLevel = Math.Clamp(skillLevel, 1f, 10f);
            BonusDescription = GenerateBonusDescription();
        }

        private string GenerateBonusDescription()
        {
            return Type switch
            {
                ExecutiveType.CEO => $"+{NetIncomeBonus*100:F0}% Net Income, ogólne zarządzanie",
                ExecutiveType.COO => $"+{FactoryEfficiencyBonus*100:F0}% wydajność fabryk, -{MaintenanceCostReduction*100:F0}% utrzymanie",
                ExecutiveType.CMO => $"+{(BrandAwarenessMultiplier-1)*100:F0}% Brand Awareness, +{RetailDemandBonus*100:F0}% popyt",
                ExecutiveType.CTO => $"+{RnDSpeedBonus*100:F0}% szybkość R&D, +{ProductQualityBonus*100:F0}% jakość",
                ExecutiveType.CFO => $"-{TaxRateReduction*100:F1}pp podatek CIT, lepsza zdolność kredytowa",
                ExecutiveType.CSO => $"+{RetailDemandBonus*100:F0}% przychody detaliczne",
                _                 => "Brak bonusu"
            };
        }

        /// <summary>Generuje losowego kandydata na daną pozycję.</summary>
        public static Executive GenerateCandidate(ExecutiveType type, Random rng)
        {
            string[] firstNames = { "Marek", "Anna", "Piotr", "Katarzyna", "Tomasz", "Agnieszka", "Jan", "Monika", "Robert", "Ewa" };
            string[] lastNames  = { "Kowalski", "Nowak", "Wiśniewski", "Zieliński", "Woźniak", "Kozłowski", "Jankowski", "Mazur" };

            string name = $"{firstNames[rng.Next(firstNames.Length)]} {lastNames[rng.Next(lastNames.Length)]}";
            float skill = (float)(3.0 + rng.NextDouble() * 7.0); // 3-10
            decimal baseSalary = type switch
            {
                ExecutiveType.CEO => 25000m,
                ExecutiveType.COO => 18000m,
                ExecutiveType.CMO => 16000m,
                ExecutiveType.CTO => 17000m,
                ExecutiveType.CFO => 15000m,
                ExecutiveType.CSO => 14000m,
                _                 => 12000m
            };
            decimal salary = Math.Round(baseSalary * (0.8m + (decimal)rng.NextDouble() * 0.4m), 0);
            return new Executive(name, type, salary, skill);
        }
    }
}
