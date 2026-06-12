using System;
using System.Collections.Generic;
using Conglomerate.Logistics;

namespace Conglomerate
{
    public class GameSaveContainer
    {
        public SaveGameMetadata Metadata { get; set; } = new SaveGameMetadata();
        public GameStateData State { get; set; } = new GameStateData();
    }

    public class GameStateData
    {
        public string CompanyName { get; set; } = "";
        public decimal Cash { get; set; }
        public decimal ShareCapital { get; set; }
        public decimal RetainedEarnings { get; set; }
        public decimal Loans { get; set; }
        public int CurrentDay { get; set; }
        public int CurrentHour { get; set; }
        public int CurrentMonthIndex { get; set; }
        public decimal TaxRate { get; set; }

        public List<BuildingSaveData> Buildings { get; set; } = new List<BuildingSaveData>();
        public List<TileSaveData> Tiles { get; set; } = new List<TileSaveData>();

        /// <summary>Trasy logistyczne — zapisywane razem ze stanem gry.</summary>
        public List<SupplyRoute> SupplyRoutes { get; set; } = new List<SupplyRoute>();
    }

    public class BuildingSaveData
    {
        public string FacilityId { get; set; } = "";
        public string Type { get; set; } = ""; // "Farm", "CoalMine", "CheeseFactory", itp.
        public string Name { get; set; } = "";
        public int X { get; set; }
        public int Y { get; set; }
        public bool AutoSell { get; set; }
        public decimal AccumulatedDepreciation { get; set; }
        /// <summary>ID aktywnego przepisu dla FactoryBuilding (null = Idle).</summary>
        public string? ActiveRecipeId { get; set; } = null;
        public List<WarehouseItem> Warehouse { get; set; } = new List<WarehouseItem>();
    }

    public class WarehouseItem
    {
        public string Key { get; set; } = "";
        public int Value { get; set; }

        public WarehouseItem() { }
        public WarehouseItem(string key, int value)
        {
            Key = key;
            Value = value;
        }
    }

    public class TileSaveData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string BuildingFacilityId { get; set; } = ""; // Empty if none
    }
}
