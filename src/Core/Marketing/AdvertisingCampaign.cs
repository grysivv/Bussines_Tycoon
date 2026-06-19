using System;

namespace Conglomerate.Marketing
{
    /// <summary>Typ kampanii reklamowej.</summary>
    public enum CampaignType
    {
        TV,           // Telewizja — szeroki zasięg, drogie
        Social,       // Media społecznościowe — szybki wzrost, tańsze
        Outdoor,      // Billboardy — wolny wzrost, stabilny efekt
        Influencer    // Influencerzy — mocny krótkoterminowy spike
    }

    /// <summary>
    /// Kampania reklamowa — zwiększa Brand Awareness dla danego produktu,
    /// co przekłada się na wyższy popyt w sklepach detalicznych.
    /// Wzorowana na Capitalism Lab: każda kampania ma budżet dzienny, typ i czas trwania.
    /// </summary>
    public class AdvertisingCampaign
    {
        public Guid CampaignId { get; } = Guid.NewGuid();
        public string ProductName { get; set; }       // Promowany produkt
        public CampaignType Type { get; set; }
        public decimal DailyBudget { get; set; }      // Koszt dzienny w złotych
        public int DurationDays { get; set; }         // Całkowity czas kampanii
        public int DaysRemaining { get; set; }        // Pozostałe dni
        public int DayStarted { get; set; }

        // Efektywność kampanii (ile Brand Awareness rośnie dziennie)
        public float DailyAwarenessGain { get; private set; }

        public bool IsActive => DaysRemaining > 0;

        public AdvertisingCampaign(string productName, CampaignType type, decimal dailyBudget, int durationDays, int dayStarted)
        {
            ProductName = productName;
            Type = type;
            DailyBudget = dailyBudget;
            DurationDays = durationDays;
            DaysRemaining = durationDays;
            DayStarted = dayStarted;
            DailyAwarenessGain = CalculateDailyGain(type, dailyBudget);
        }

        /// <summary>
        /// Oblicza dzienny przyrost Brand Awareness na podstawie typu kampanii i budżetu.
        /// </summary>
        private static float CalculateDailyGain(CampaignType type, decimal dailyBudget)
        {
            // Bazowy gain per 1000 zł budżetu
            float baseGainPer1000 = type switch
            {
                CampaignType.TV          => 2.5f,   // Mocny ale drogi
                CampaignType.Social      => 4.0f,   // Dobry ROI
                CampaignType.Outdoor     => 1.5f,   // Wolny ale stabilny
                CampaignType.Influencer  => 6.0f,   // Spike ale krótki
                _                        => 2.0f
            };

            // Skalowanie: logarytmiczne (diminishing returns)
            float budgetK = (float)(dailyBudget / 1000m);
            float scale = budgetK > 0 ? (float)Math.Log(1 + budgetK) / (float)Math.Log(2) : 0f;

            return baseGainPer1000 * scale;
        }

        /// <summary>Wywoływane każdego dnia — zmniejsza licznik i pobiera koszt.</summary>
        public bool Tick(Company company, int day, int hour)
        {
            if (!IsActive) return false;

            if (company.Balance >= DailyBudget)
            {
                company.Balance -= DailyBudget;
                company.AddTransaction(day, hour,
                    $"Reklama: {Type} → {ProductName}",
                    -DailyBudget, "Marketing", "");
            }

            DaysRemaining--;
            return true;
        }
    }
}
