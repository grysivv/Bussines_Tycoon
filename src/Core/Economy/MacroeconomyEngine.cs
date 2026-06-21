using System;
using System.Collections.Generic;
using Conglomerate.Finance;

namespace Conglomerate.Economy
{
    public enum EconomicPhase { Recovery, Expansion, Boom, Slowdown, Recession }

    /// <summary>
    /// Silnik makroekonomiczny — symuluje cykl koniunkturalny, inflację,
    /// stopy procentowe i wskaźnik zaufania konsumentów (CCI).
    /// </summary>
    public class MacroeconomyEngine
    {
        private readonly Random _rng = new Random(42);

        public EconomicPhase Phase { get; private set; } = EconomicPhase.Expansion;
        public float InflationRate { get; private set; } = 2.5f;       // %
        public float BaseInterestRate { get; private set; } = 3.5f;    // %
        public float ConsumerConfidenceIndex { get; private set; } = 65f; // 0–100

        private int _daysInPhase = 0;
        private int _phaseDuration = 120;

        public List<float> CCIHistory { get; } = new List<float>();
        public List<float> InflationHistory { get; } = new List<float>();
        public List<float> RateHistory { get; } = new List<float>();

        public string PhaseLabel => Phase switch
        {
            EconomicPhase.Boom => "BOOM",
            EconomicPhase.Expansion => "Ekspansja",
            EconomicPhase.Slowdown => "Spowolnienie",
            EconomicPhase.Recession => "Recesja",
            EconomicPhase.Recovery => "Ożywienie",
            _ => "Ekspansja"
        };

        public System.Drawing.Color PhaseColor => Phase switch
        {
            EconomicPhase.Boom => System.Drawing.Color.FromArgb(255, 200, 60),
            EconomicPhase.Expansion => System.Drawing.Color.FromArgb(40, 210, 110),
            EconomicPhase.Slowdown => System.Drawing.Color.FromArgb(255, 165, 0),
            EconomicPhase.Recession => System.Drawing.Color.FromArgb(240, 70, 60),
            EconomicPhase.Recovery => System.Drawing.Color.FromArgb(60, 140, 220),
            _ => System.Drawing.Color.White
        };

        /// <summary>Dynamiczna stawka kredytu (baza + spread per typ).</summary>
        public decimal GetLoanRate(LoanType type)
        {
            float spread = type switch
            {
                LoanType.ShortTerm => 4.0f,
                LoanType.MediumTerm => 2.0f,
                LoanType.LongTerm => 0.5f,
                _ => 2.0f
            };
            return Math.Round((decimal)((BaseInterestRate + spread) / 100f), 4);
        }

        /// <summary>Mnożnik popytu detalicznego na podstawie CCI (0.5 – 1.5).</summary>
        public float GetRetailDemandMultiplier() =>
            0.5f + (ConsumerConfidenceIndex / 100f);

        /// <summary>Wywołaj raz na dobę przez GameManager.</summary>
        public void OnNewDay()
        {
            _daysInPhase++;

            var (tCCI, tInfl, tRate) = Phase switch
            {
                EconomicPhase.Boom => (85f, 7.0f, 7.0f),
                EconomicPhase.Expansion => (68f, 3.0f, 4.0f),
                EconomicPhase.Slowdown => (52f, 5.0f, 5.5f),
                EconomicPhase.Recession => (30f, 1.5f, 2.0f),
                EconomicPhase.Recovery => (50f, 2.5f, 3.0f),
                _ => (65f, 3.0f, 3.5f)
            };

            float noise = (float)(_rng.NextDouble() * 2 - 1);
            ConsumerConfidenceIndex = Clamp(Lerp(ConsumerConfidenceIndex, tCCI + noise * 6, 0.025f), 5f, 98f);
            InflationRate = Clamp(Lerp(InflationRate, tInfl + noise * 0.6f, 0.012f), 0f, 20f);
            BaseInterestRate = Clamp(Lerp(BaseInterestRate, tRate + noise * 0.4f, 0.010f), 0.5f, 18f);

            // Historia (ostatnie 60 dni)
            AppendHistory(CCIHistory, ConsumerConfidenceIndex);
            AppendHistory(InflationHistory, InflationRate);
            AppendHistory(RateHistory, BaseInterestRate);

            if (_daysInPhase >= _phaseDuration)
                TransitionPhase();
        }

        private void TransitionPhase()
        {
            _daysInPhase = 0;
            _phaseDuration = 90 + _rng.Next(0, 91);

            Phase = Phase switch
            {
                EconomicPhase.Recovery => EconomicPhase.Expansion,
                EconomicPhase.Expansion => _rng.NextDouble() < 0.55 ? EconomicPhase.Boom : EconomicPhase.Slowdown,
                EconomicPhase.Boom => EconomicPhase.Slowdown,
                EconomicPhase.Slowdown => _rng.NextDouble() < 0.65 ? EconomicPhase.Recession : EconomicPhase.Recovery,
                EconomicPhase.Recession => EconomicPhase.Recovery,
                _ => EconomicPhase.Expansion
            };
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
        private static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;
        private static void AppendHistory(List<float> list, float val)
        {
            list.Add(val);
            if (list.Count > 60) list.RemoveAt(0);
        }
    }
}
