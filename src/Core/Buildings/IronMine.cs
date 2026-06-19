using System.Collections.Generic;
using Conglomerate.Production;

namespace Conglomerate
{
    /// <summary>Kopalnia Żelaza — wydobywa Rudę Żelaza (surowiec do huty).</summary>
    public class IronMine : Building
    {
        public override string ActivityType => "Kopalnia Żelaza";
        public override decimal BuildCost => 200000m;
        public override decimal MaintenanceCost => 600m;
        public override int WarehouseCapacity => 300;
        public override Dictionary<string, decimal> ResourcePrices => new Dictionary<string, decimal>
        {
            { "Ruda Żelaza", 120m }
        };

        public IronMine(string name) : base(name) { }

        public override bool Produce(Company company)
        {
            if (company.Balance < MaintenanceCost) return false;
            company.Balance -= MaintenanceCost;
            AddProduct("Ruda Żelaza", 15m);
            return true;
        }
    }
}
