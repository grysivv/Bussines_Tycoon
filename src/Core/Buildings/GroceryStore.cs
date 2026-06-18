namespace Conglomerate
{
    /// <summary>
    /// Sklep spożywczy — pierwsza implementacja RetailBuilding.
    ///
    /// Parametry:
    ///   - 5 aktywnych slotów sprzedażowych (możne sprzedawać 5 różnych produktów)
    ///   - Lokalizacja standardowa (LocationFactor = 1.0)
    ///   - Pojemność magazynu wewnętrznego: 1000 szt. łącznie
    ///
    /// Przykłady sprzedaży: Mleko, Ser, Masło, Mięso, Warzywa, Owoce
    ///
    /// Skalowanie (przyszłe sklepy):
    ///   class ElectronicsStore : RetailBuilding { MaxSlots = 5; ... }
    ///   class GroceryChain     : RetailBuilding { MaxSlots = 10; LocationFactor = 1.5f; ... }
    /// </summary>
    public class GroceryStore : RetailBuilding
    {
        public override int MaxSlots => 5;

        // Sklep w standardowej lokalizacji
        public override float LocationFactor { get; set; } = 1.0f;

        public override int WarehouseCapacity => 1000;
        public override decimal BuildCost => 25_000m;
        public override decimal MaintenanceCost => 500m; // 500 zł/dzień

        public GroceryStore(string name) : base(name) { }
    }
}