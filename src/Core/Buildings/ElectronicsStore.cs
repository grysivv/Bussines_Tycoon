namespace Conglomerate
{
    /// <summary>
    /// Sklep Elektroniczny — sprzedaje Smartfony, Laptopy i Komponenty.
    /// 4 sloty sprzedażowe, wysoki Location Factor w centrum.
    /// </summary>
    public class ElectronicsStore : RetailBuilding
    {
        public override int MaxSlots => 4;
        public override decimal BuildCost => 250000m;
        public override decimal MaintenanceCost => 2500m;
        public override string ActivityType => "Sklep Elektroniczny";
        public override int WarehouseCapacity => 100;

        public ElectronicsStore(string name) : base(name)
        {
            LocationFactor = 1.2f; // Dobrze sprawdza się w centrum
        }
    }
}
