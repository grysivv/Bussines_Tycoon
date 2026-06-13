namespace Conglomerate
{
    /// <summary>
    /// Sklep Ogólny — pierwsza implementacja RetailBuilding.
    ///
    /// Parametry:
    ///   - 3 aktywne sloty sprzedażowe (możne sprzedawać 3 różne produkty)
    ///   - Lokalizacja standardowa (LocationFactor = 1.0)
    ///   - Pojemność magazynu wewnętrznego: 500 szt. łącznie
    ///
    /// Przykłady sprzedaży: Mleko, Ser, Masło, Mięso
    ///
    /// Skalowanie (przyszłe sklepy):
    ///   class ElectronicsStore : RetailBuilding { MaxSlots = 5; ... }
    ///   class GroceryChain     : RetailBuilding { MaxSlots = 10; LocationFactor = 1.5f; ... }
    /// </summary>
    public class GeneralStore : RetailBuilding
    {
        public override int MaxSlots => 3;

        // Sklep w standardowej lokalizacji
        public override float LocationFactor { get; set; } = 1.0f;

        public override int WarehouseCapacity => 500;
        public override decimal BuildCost => 25_000m;
        public override decimal MaintenanceCost => 400m; // 400 zł/dzień

        public GeneralStore(string name) : base(name) { }
    }
}
