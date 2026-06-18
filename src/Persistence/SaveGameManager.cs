using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using Conglomerate.Logistics;

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
            container.Metadata.NetWorth = bs.TotalEquity;
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
                string type = building switch
                {
                    Farm             => "Farm",
                    CoalMine         => "CoalMine",
                    CopperMine       => "CopperMine",
                    CheeseFactory    => "CheeseFactory",
                    CopperFoundry     => "CopperFoundry",
                    GeneralStore     => "GeneralStore",
                    WarehouseBuilding wh => wh.AllowedCategory == ResourceCategory.Food
                                            ? "FoodWarehouse"
                                            : wh.AllowedCategory == ResourceCategory.ProcessedFood
                                                ? "ProcessedFoodWarehouse"
                                                : "MiningWarehouse",
                    _                => "Unknown"
                };

                var bData = new BuildingSaveData
                {
                    FacilityId               = building.FacilityId,
                    Type                     = type,
                    Name                     = building.Name,
                    AutoSell                 = building.AutoSell,
                    AutoSellResources        = building.AutoSellResources.ToList(),
                    AccumulatedDepreciation  = building.AccumulatedDepreciation,
                    // Zapisz aktywny przepis fabryki
                    ActiveRecipeId           = (building is FactoryBuilding fb) ? fb.ActiveRecipe?.Id : null
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

                // Save retail slots (for GeneralStore and future retail buildings)
                if (building is RetailBuilding retailBuilding)
                {
                    foreach (var slot in retailBuilding.Slots)
                    {
                        bData.RetailSlots.Add(new RetailSlotSaveData
                        {
                            SlotIndex        = slot.SlotIndex,
                            ProductName      = slot.ProductName,
                            CurrentStock     = slot.CurrentStock,
                            ShelfCapacity    = slot.ShelfCapacity,
                            PriceMultiplier  = slot.PriceMultiplier,
                            DirectRetailPrice = slot.DirectRetailPrice
                        });
                    }
                }

                container.State.Buildings.Add(bData);
            }

            // Save Map Tiles
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    var tile = map.GetTile(x, y);
                    container.State.Tiles.Add(new TileSaveData
                    {
                        X = x,
                        Y = y,
                        BuildingFacilityId = tile.Building?.FacilityId ?? ""
                    });
                }
            }

            // Save Supply Routes (Logistics)
            container.State.SupplyRoutes.AddRange(gameManager.Logistics.Routes);

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
            var wrapper = JsonSerializer.Deserialize<SaveGameMetadataWrapper>(json);
            return wrapper?.Metadata ?? new SaveGameMetadata();
        }

        public static GameSaveContainer LoadGame(string filePath)
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<GameSaveContainer>(json)!;
        }

        /// <summary>
        /// Przywraca budynki z danych zapisu, uwzględniając nowe typy (CheeseFactory itp.).
        /// </summary>
        public static Building? RestoreBuilding(BuildingSaveData bData)
        {
            Building? building = bData.Type switch
            {
                "Farm"                   => new Farm(bData.Name),
                "CoalMine"               => new CoalMine(bData.Name),
                "CopperMine"             => new CopperMine(bData.Name),
                "CheeseFactory"          => new CheeseFactory(bData.Name),
                "CopperFoundry"          => new CopperFoundry(bData.Name),
                "GeneralStore"           => new GeneralStore(bData.Name),
                "FoodWarehouse"          => new WarehouseBuilding(bData.Name, ResourceCategory.Food),
                "ProcessedFoodWarehouse" => new WarehouseBuilding(bData.Name, ResourceCategory.ProcessedFood),
                "MiningWarehouse"        => new WarehouseBuilding(bData.Name, ResourceCategory.Mining),
                _                        => null
            };

            if (building == null) return null;

            building.FacilityId             = bData.FacilityId;
            building.AutoSell               = bData.AutoSell;
            building.AccumulatedDepreciation = bData.AccumulatedDepreciation;

            building.AutoSellResources.Clear();
            if (bData.AutoSellResources != null)
            {
                foreach (var res in bData.AutoSellResources)
                {
                    building.AutoSellResources.Add(res);
                }
            }

            // Przywróć stan magazynu
            foreach (var item in bData.Warehouse)
            {
                building.Warehouse[item.Key] = item.Value;
            }

            // Przywróć aktywny przepis fabryki
            if (building is FactoryBuilding factory && bData.ActiveRecipeId != null)
            {
                var recipe = factory.AvailableRecipes.Find(r => r.Id == bData.ActiveRecipeId);
                if (recipe != null)
                    factory.SetRecipe(recipe);
            }

            // Przywróć sloty detaliczne
            if (building is RetailBuilding retailBuilding && bData.RetailSlots.Count > 0)
            {
                foreach (var slotData in bData.RetailSlots)
                {
                    if (!string.IsNullOrEmpty(slotData.ProductName))
                    {
                        retailBuilding.AssignProduct(slotData.SlotIndex, slotData.ProductName, slotData.PriceMultiplier);
                        var slot = retailBuilding.Slots[slotData.SlotIndex];
                        slot.CurrentStock      = slotData.CurrentStock;
                        slot.ShelfCapacity     = slotData.ShelfCapacity;
                        slot.DirectRetailPrice  = slotData.DirectRetailPrice;
                    }
                }
            }

            return building;
        }
    }

    public class SaveGameMetadataWrapper
    {
        public SaveGameMetadata Metadata { get; set; } = new SaveGameMetadata();
    }
}
