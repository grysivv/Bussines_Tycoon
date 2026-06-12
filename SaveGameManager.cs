using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace Conglomerate
{
    public static class SaveGameManager
    {
        private static string GetSaveDirectory()
        {
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string savePath = Path.Combine(docPath, "ConglomerateTycoon", "Saves");
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
            return savePath;
        }

        public static void SaveGame(string saveName, Company company, Map map, GameManager gameManager)
        {
            var container = new GameSaveContainer();
            
            // Calculate Net Worth using the balance sheet
            var bs = company.Engine.CalculateCurrentBalanceSheet();
            
            // 1. Populate Metadata
            container.Metadata.CorporationName = company.Name;
            container.Metadata.CurrentDay = gameManager.CurrentDay;
            container.Metadata.CurrentHour = gameManager.CurrentHour;
            container.Metadata.NetWorth = bs.TotalEquity; // Net Worth = Assets - Liabilities = Equity
            container.Metadata.LogoIconName = "LogoStandard";
            container.Metadata.RealWorldSaveTime = DateTime.Now;

            // 2. Populate State
            container.State.CompanyName = company.Name;
            container.State.Cash = company.Engine.Cash;
            container.State.ShareCapital = company.Engine.ShareCapital;
            container.State.RetainedEarnings = company.Engine.RetainedEarnings;
            container.State.Loans = company.Engine.Loans;
            container.State.CurrentDay = gameManager.CurrentDay;
            container.State.CurrentHour = gameManager.CurrentHour;
            container.State.CurrentMonthIndex = company.Engine.CurrentMonthIndex;
            container.State.TaxRate = company.Engine.TaxRate;

            // Save Buildings
            foreach (var building in company.Buildings)
            {
                var bData = new BuildingSaveData
                {
                    FacilityId = building.FacilityId,
                    Type = building is Farm ? "Farm" : (building is CoalMine ? "CoalMine" : "Unknown"),
                    Name = building.Name,
                    AutoSell = building.AutoSell,
                    AccumulatedDepreciation = building.AccumulatedDepreciation
                };

                // Find building coordinates
                for (int x = 0; x < map.Width; x++)
                {
                    for (int y = 0; y < map.Height; y++)
                    {
                        if (map.GetTile(x, y).Building == building)
                        {
                            bData.X = x;
                            bData.Y = y;
                        }
                    }
                }

                // Save warehouse items
                foreach (var kvp in building.Warehouse)
                {
                    bData.Warehouse.Add(new WarehouseItem(kvp.Key, kvp.Value));
                }

                container.State.Buildings.Add(bData);
            }

            // Save Map Tiles
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    var tile = map.GetTile(x, y);
                    var tData = new TileSaveData
                    {
                        X = x,
                        Y = y,
                        BuildingFacilityId = tile.Building?.FacilityId ?? ""
                    };
                    container.State.Tiles.Add(tData);
                }
            }

            // Serialize and write to file
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(container, options);
            string safeFileName = string.Join("_", saveName.Split(Path.GetInvalidFileNameChars())) + ".json";
            string filePath = Path.Combine(GetSaveDirectory(), safeFileName);
            File.WriteAllText(filePath, json);
        }

        public static List<FileInfo> GetSaveFiles()
        {
            var dir = new DirectoryInfo(GetSaveDirectory());
            if (dir.Exists)
            {
                return new List<FileInfo>(dir.GetFiles("*.json"));
            }
            return new List<FileInfo>();
        }

        public static SaveGameMetadata GetSaveMetadata(string filePath)
        {
            string json = File.ReadAllText(filePath);
            
            // Deserialize only the metadata wrapper to avoid loading the whole game state
            var wrapper = JsonSerializer.Deserialize<SaveGameMetadataWrapper>(json);
            return wrapper?.Metadata ?? new SaveGameMetadata();
        }

        public static GameSaveContainer LoadGame(string filePath)
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<GameSaveContainer>(json);
        }
    }

    public class SaveGameMetadataWrapper
    {
        public SaveGameMetadata Metadata { get; set; } = new SaveGameMetadata();
    }
}
