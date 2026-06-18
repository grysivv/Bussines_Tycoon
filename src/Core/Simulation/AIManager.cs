using System;
using System.Collections.Generic;
using System.Linq;
using Conglomerate;

namespace Conglomerate.Simulation
{
    public class AIManager
    {
        public List<AICompany> Competitors { get; } = new List<AICompany>();

        public AIManager()
        {
            // Initialize some default AI competitors
            Competitors.Add(new AICompany("Global Extracts", 100000m, AIType.Extractor, 250000m));
            Competitors.Add(new AICompany("Retail Kings", 150000m, AIType.Retailer, 300000m));
            Competitors.Add(new AICompany("Heavy Industries", 200000m, AIType.Manufacturer, 400000m));
        }

        public void TickDaily(Map activeMap, int currentDay, int currentHour)
        {
            foreach (var ai in Competitors)
            {
                if (!ai.IsOnMap)
                {
                    // Accumulate virtual wealth
                    ai.UpdateMarketSimulation(currentDay);

                    // Check threshold
                    if (ai.Balance >= ai.TargetThreshold)
                    {
                        TryPlaceBuildingOnMap(ai, activeMap, currentDay, currentHour);
                    }
                }
            }
        }

        private void TryPlaceBuildingOnMap(AICompany ai, Map map, int day, int hour)
        {
            Building? buildingToBuild = GetBuildingForSpecialization(ai.Specialization);
            if (buildingToBuild == null) return;

            // Find an empty tile
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    var tile = map.GetTile(x, y);
                    if (tile != null && tile.Type == TileType.Grass && tile.Building == null)
                    {
                        if (ai.BuyBuilding(buildingToBuild, map, x, y, day, hour))
                        {
                            ai.IsOnMap = true;
                            // Optionally configure the building
                            if (buildingToBuild is FactoryBuilding fb && fb.AvailableRecipes.Any())
                            {
                                fb.SetRecipe(fb.AvailableRecipes.First());
                                fb.AutoSell = true;
                            }
                            if (buildingToBuild is RetailBuilding rb)
                            {
                                rb.AutoSell = true;
                            }
                            buildingToBuild.AutoSell = true;
                            return; // Successfully placed
                        }
                    }
                }
            }
        }

        private Building? GetBuildingForSpecialization(AIType type)
        {
            return type switch
            {
                AIType.Extractor => new CoalMine("Kopalnia AI"),
                AIType.Manufacturer => new CheeseFactory("Fabryka AI"),
                AIType.Retailer => new GroceryStore("Sklep AI"),
                _ => null
            };
        }
    }
}
