namespace Conglomerate
{
    /// <summary>
    /// Sklep Odzieżowy — sprzedaje Odzież i Odzież Premium.
    /// 5 slotów sprzedażowych (duża powierzchnia ekspozycji).
    /// </summary>
    public class ClothingStore : RetailBuilding
    {
        public override int MaxSlots => 5;
        public override decimal BuildCost => 180000m;
        public override decimal MaintenanceCost => 1800m;
        public override string ActivityType => "Sklep Odzieżowy";
        public override int WarehouseCapacity => 120;

        public ClothingStore(string name) : base(name)
        {
            LocationFactor = 1.1f;
        }
    }
}
