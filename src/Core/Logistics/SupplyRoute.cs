using System;

namespace Conglomerate.Logistics
{
    /// <summary>Skąd pochodzi surowiec w trasie logistycznej.</summary>
    public enum RouteSourceType
    {
        /// <summary>Surowiec pochodzi z konkretnego budynku gracza (ekstraktor lub magazyn).</summary>
        Building,

        /// <summary>Surowiec jest kupowany każdorazowo na wolnym rynku.</summary>
        Market
    }

    /// <summary>
    /// Reprezentuje jedną automatyczną trasę logistyczną między dwoma punktami.
    /// 
    /// Przykłady:
    ///   Farma → Mleczarnia          (Building → Building, Mleko, 10 szt., co 24h)
    ///   Magazyn Żywności → Mleczarnia (Building → Building, Mleko, 20 szt., co 8h)
    ///   Rynek → Mleczarnia          (Market → Building, Mleko, 5 szt., co 24h)
    /// 
    /// Skalowalność: klasa jest prosta, rozszerzalna o priorytet, czas dostawy itp.
    /// </summary>
    public class SupplyRoute
    {
        // ──────────────────────────────────────────────
        //  Identyfikacja
        // ──────────────────────────────────────────────

        /// <summary>Unikalny identyfikator trasy (do serializacji i zarządzania).</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // ──────────────────────────────────────────────
        //  Definicja trasy
        // ──────────────────────────────────────────────

        /// <summary>Skąd pochodzi surowiec.</summary>
        public RouteSourceType SourceType { get; set; } = RouteSourceType.Building;

        /// <summary>
        /// FacilityId budynku źródłowego (gdy SourceType == Building).
        /// Pusty string gdy SourceType == Market.
        /// </summary>
        public string SourceFacilityId { get; set; } = string.Empty;

        /// <summary>FacilityId budynku docelowego (zawsze wymagany).</summary>
        public string TargetFacilityId { get; set; } = string.Empty;

        /// <summary>Nazwa surowca do transferu (np. "Mleko", "Węgiel").</summary>
        public string ResourceName { get; set; } = string.Empty;

        /// <summary>Ile jednostek surowca transferować przy każdym wyzwoleniu trasy.</summary>
        public int AmountPerTrip { get; set; } = 10;

        /// <summary>Co ile godzin gry trasa ma się wyzwalać (np. 24 = raz na dobę).</summary>
        public int IntervalHours { get; set; } = 24;

        /// <summary>
        /// Koszt transportu na jednostkę surowca (zł/szt.).
        /// Pobierany od firmy przy każdym wykonaniu trasy.
        /// </summary>
        public decimal TransportCostPerUnit { get; set; } = 10m;

        /// <summary>Czy trasa jest aktywna.</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>Typ pojazdu obsługującego trasę (np. "Van", "Truck").</summary>
        public string VehicleTypeName { get; set; } = "Van";

        /// <summary>Zasada wyzwalania i ładowania pojazdu.</summary>
        public LoadThresholdRule LoadRule { get; set; } = LoadThresholdRule.TimerOnly;

        /// <summary>Priorytet przypisania pojazdów w przypadku braku wolnej floty.</summary>
        public RoutePriority Priority { get; set; } = RoutePriority.Medium;

        // ──────────────────────────────────────────────
        //  Stan runtime (nie serializowany)
        // ──────────────────────────────────────────────

        /// <summary>Liczba godzin od ostatniego wyzwolenia trasy.</summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public int HoursSinceLastTrip { get; set; } = 0;

        /// <summary>Wynik ostatniego wykonania trasy (do wyświetlenia w UI).</summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public string LastTripResult { get; set; } = "Oczekiwanie...";

        /// <summary>Czytelna nazwa źródła (do wyświetlania w UI — uzupełniana przez LogisticsManager).</summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public string SourceDisplayName { get; set; } = string.Empty;

        // ──────────────────────────────────────────────
        //  Pomocnicze
        // ──────────────────────────────────────────────

        /// <summary>Całkowity koszt jednego tripu (transport + ewentualny zakup rynkowy).</summary>
        public decimal TotalTransportCost => TransportCostPerUnit * AmountPerTrip;

        public override string ToString() =>
            $"{(SourceType == RouteSourceType.Market ? "Rynek" : SourceDisplayName)} → {ResourceName} × {AmountPerTrip} (co {IntervalHours}h)";
    }
}
