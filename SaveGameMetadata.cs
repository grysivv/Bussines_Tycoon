using System;

namespace Conglomerate
{
    public class SaveGameMetadata
    {
        public string CorporationName { get; set; } = "BezNazwy Corp";
        public int CurrentDay { get; set; } = 1;
        public int CurrentHour { get; set; } = 8;
        public decimal NetWorth { get; set; } = 0m;
        public string LogoIconName { get; set; } = "LogoStandard";
        public DateTime RealWorldSaveTime { get; set; } = DateTime.Now;
    }
}
