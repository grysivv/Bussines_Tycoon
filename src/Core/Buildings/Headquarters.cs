using System;
using System.Collections.Generic;

namespace Conglomerate
{
    /// <summary>
    /// Kwatera Główna (Headquarters) — odblokowuje dostęp do zaawansowanych funkcji (Marketing, HR).
    /// </summary>
    public class Headquarters : Building
    {
        public override string ActivityType => "Kwatera Główna";
        public override decimal BuildCost => 1000000m; // 1M cost
        public override decimal MaintenanceCost => 10000m; // 10k/month
        public override int WarehouseCapacity => 0;

        public override Dictionary<string, decimal> ResourcePrices => new Dictionary<string, decimal>();

        public Headquarters(string name) : base(name)
        {
        }

        public override bool Produce(Company company)
        {
            // Headquarters nie produkuje dóbr.
            return false;
        }
    }
}
