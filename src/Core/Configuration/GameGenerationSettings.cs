using System;

namespace Conglomerate
{
    public class GameGenerationSettings
    {
        // --- COMPANY (FIRMA GRACZA) ---
        public string CompanyName { get; set; } = "My Tycoon Company";

        // --- MAP SIZE (ROZMIAR MAPY) ---
        public int MapWidth  { get; set; } = 10;
        public int MapHeight { get; set; } = 10;

        // --- MACRO PARAMETERS (PARAMETRY MAKRO) ---
        public int StartYear { get; set; } = 2026;
        public decimal BaseInflation { get; set; } = 0.02m; // 2% inflation rate
        public string AICompetitionAggressiveness { get; set; } = "Normalna"; // Normalna, Niska, Agresywna
        public decimal GlobalCorporateTax { get; set; } = 0.19m; // 19% corporate tax

        // --- MAP PARAMETERS (PARAMETRY MAPY) ---
        public int CitiesCount { get; set; } = 1;
        public string PopulationDensity { get; set; } = "Średnia"; // Niska, Średnia, Wysoka
        public string NaturalResourcesRichness { get; set; } = "Standardowa"; // Uboga, Standardowa, Obfita

        // --- STARTING CAPITAL (KAPITAŁ STARTOWY) ---
        public decimal StartingCash { get; set; } = 50000m;
        public decimal StartingDebt { get; set; } = 0m;
        public decimal OwnedTreasuryShares { get; set; } = 1000m; // Owned treasury shares
    }
}
