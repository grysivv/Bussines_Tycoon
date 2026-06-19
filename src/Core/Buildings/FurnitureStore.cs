namespace Conglomerate
{
    /// <summary>
    /// Salon Meblowy — sprzedaje Meble i Meble Luksusowe.
    /// 3 sloty sprzedażowe (produkty zajmują dużo miejsca).
    /// </summary>
    public class FurnitureStore : RetailBuilding
    {
        public override int MaxSlots => 3;
        public override decimal BuildCost => 220000m;
        public override decimal MaintenanceCost => 2000m;
        public override string ActivityType => "Salon Meblowy";
        public override int WarehouseCapacity => 80;

        public FurnitureStore(string name) : base(name)
        {
            LocationFactor = 0.95f; // Lepiej poza centrum (tańszy wynajem)
        }
    }
}
