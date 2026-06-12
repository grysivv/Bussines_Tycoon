namespace Conglomerate.Production
{
    /// <summary>
    /// Reprezentuje aktualny stan operacyjny fabryki/zakładu przetwórczego.
    /// Pozwala UI i silnikowi rozróżnić co się dzieje z danym budynkiem.
    /// </summary>
    public enum FacilityState
    {
        /// <summary>Zakład jest wyłączony — nie produkuje, nie pobiera kosztów operacyjnych.</summary>
        Idle,

        /// <summary>Zakład aktywnie przetwarza surowce według wybranego przepisu.</summary>
        Producing,

        /// <summary>Zakład jest wstrzymany z powodu braku surowców wejściowych.</summary>
        WaitingForInputs,

        /// <summary>Zakład jest wstrzymany z powodu pełnego magazynu wyjściowego.</summary>
        OutputStorageFull,

        /// <summary>Zakład jest wstrzymany z powodu braku środków na koszty operacyjne.</summary>
        InsufficientFunds,

        /// <summary>Zakład jest w trybie konserwacji — tymczasowo wyłączony przez gracza.</summary>
        Maintenance
    }
}
