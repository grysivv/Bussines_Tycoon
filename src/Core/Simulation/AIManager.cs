using System;
using System.Collections.Generic;
using System.Linq;
using Conglomerate;
using Conglomerate.Finance;

namespace Conglomerate.Simulation
{
    /// <summary>
    /// Menedżer AI firm — zarządza wszystkimi konkurentami.
    /// Capitalism Lab style: AI buduje wiele budynków, reaguje na ceny gracza,
    /// ekspanduje do nowych segmentów rynku.
    /// </summary>
    public class AIManager
    {
        private readonly Random _rng = new Random(42);
        public List<AICompany> Competitors { get; } = new List<AICompany>();

        public AIManager()
        {
            // 5 zróżnicowanych konkurentów (jak w Capitalism Lab)
            Competitors.Add(new AICompany("Global Extracts",     120000m, AIType.Extractor,     200000m, AIStrategy.Balanced));
            Competitors.Add(new AICompany("Retail Kings Corp",   200000m, AIType.Retailer,      250000m, AIStrategy.Aggressive));
            Competitors.Add(new AICompany("Heavy Industries",    300000m, AIType.Manufacturer,  350000m, AIStrategy.Conservative));
            Competitors.Add(new AICompany("TechVision Ltd",      500000m, AIType.Conglomerate,  600000m, AIStrategy.Aggressive));
            Competitors.Add(new AICompany("EcoFarm Holdings",    80000m,  AIType.Extractor,     150000m, AIStrategy.Conservative));
        }

        // ──────────────────────────────────────────────────────────
        //  Główny tick (raz na dobę)
        // ──────────────────────────────────────────────────────────

        public void TickDaily(Map activeMap, int currentDay, int currentHour)
        {
            foreach (var ai in Competitors)
            {
                // Faza 1: Symulacja rynkowa jeśli jeszcze bez budynków
                if (!ai.IsOnMap)
                {
                    ai.UpdateMarketSimulation(currentDay);
                    if (ai.Balance >= ai.TargetThreshold)
                        TryPlaceBuildingOnMap(ai, activeMap, currentDay, currentHour);
                }
                else
                {
                    // Faza 2: Ekspansja jeśli AI ma kapitał i slot budynkowy
                    if (ai.BuildingsOnMap < ai.MaxBuildingsAllowed && ai.Balance >= GetNextBuildingCost(ai))
                    {
                        TryPlaceBuildingOnMap(ai, activeMap, currentDay, currentHour);
                    }

                    // Faza 3: AI zarabia na swoich budynkach (uproszczona symulacja)
                    SimulateAIRevenue(ai, currentDay);
                }

                // Naturalne wypadanie Brand Awareness AI
                ai.DecayBrandAwareness(0.2f);

                // Co miesiąc: reset miesięcznych danych dla giełdy
                if (currentDay > 1 && (currentDay - 1) % 30 == 0)
                    ai.CloseMonthForStock();
            }
        }

        // ──────────────────────────────────────────────────────────
        //  Stawianie budynków na mapie
        // ──────────────────────────────────────────────────────────

        private void TryPlaceBuildingOnMap(AICompany ai, Map map, int day, int hour)
        {
            Building? buildingToBuild = GetNextBuildingForAI(ai);
            if (buildingToBuild == null) return;

            // Szukamy wolnego miejsca na mapie (AI preferuje różne lokalizacje)
            int startX = _rng.Next(0, map.Width);
            int startY = _rng.Next(0, map.Height);

            for (int dx = 0; dx < map.Width; dx++)
            {
                for (int dy = 0; dy < map.Height; dy++)
                {
                    int x = (startX + dx) % map.Width;
                    int y = (startY + dy) % map.Height;
                    var tile = map.GetTile(x, y);

                    if (tile != null && tile.Type == TileType.Grass && tile.Building == null)
                    {
                        if (ai.BuyBuilding(buildingToBuild, map, x, y, day, hour))
                        {
                            ai.IsOnMap = true;
                            ai.BuildingsOnMap++;

                            // Konfiguracja automatyczna budynku AI
                            ConfigureAIBuilding(buildingToBuild);
                            return;
                        }
                    }
                }
            }
        }

        private static void ConfigureAIBuilding(Building building)
        {
            building.AutoSell = true;

            if (building is FactoryBuilding fb && fb.AvailableRecipes.Any())
            {
                fb.SetRecipe(fb.AvailableRecipes.First());
                fb.AutoSell = true;
            }

            if (building is RetailBuilding rb)
            {
                // AI automatycznie wypełnia sloty produktami
                for (int i = 0; i < Math.Min(rb.MaxSlots, 3); i++)
                {
                    string product = GetAIRetailProduct(rb, i);
                    if (!string.IsNullOrEmpty(product))
                        rb.AssignProduct(i, product, 1.4m);
                }
                rb.AutoSell = true;
            }
        }

        private static string GetAIRetailProduct(RetailBuilding rb, int slotIndex)
        {
            return rb switch
            {
                GroceryStore => slotIndex == 0 ? "Mleko" : slotIndex == 1 ? "Ser" : "Masło",
                GeneralStore => slotIndex == 0 ? "Węgiel" : slotIndex == 1 ? "Miedź" : string.Empty,
                ElectronicsStore => slotIndex == 0 ? "Smartfon" : slotIndex == 1 ? "Laptop" : "Komponenty Elektroniczne",
                ClothingStore => slotIndex == 0 ? "Odzież" : slotIndex == 1 ? "Odzież Premium" : "Tkanina",
                FurnitureStore => slotIndex == 0 ? "Meble" : slotIndex == 1 ? "Meble Luksusowe" : string.Empty,
                _ => string.Empty
            };
        }

        // ──────────────────────────────────────────────────────────
        //  Dobór budynku AI
        // ──────────────────────────────────────────────────────────

        private Building? GetNextBuildingForAI(AICompany ai)
        {
            // Strategia: AI buduje zróżnicowany portfel
            int count = ai.BuildingsOnMap;

            return ai.Specialization switch
            {
                AIType.Extractor => count switch
                {
                    0 => new CoalMine($"{ai.Name} Kopalnia"),
                    1 => new Farm($"{ai.Name} Farma"),
                    2 => new IronMine($"{ai.Name} Kopalnia Żelaza"),
                    _ => new Farm($"{ai.Name} Farma {count}")
                },
                AIType.Manufacturer => count switch
                {
                    0 => new CheeseFactory($"{ai.Name} Fabryka Sera"),
                    1 => new CopperFoundry($"{ai.Name} Odlewnia"),
                    2 => new SteelMill($"{ai.Name} Huta"),
                    3 => new TextileFactory($"{ai.Name} Tekstylia"),
                    _ => new BakeryFactory($"{ai.Name} Piekarnia {count}")
                },
                AIType.Retailer => count switch
                {
                    0 => new GroceryStore($"{ai.Name} Sklep Spożywczy"),
                    1 => new GeneralStore($"{ai.Name} Sklep Ogólny"),
                    2 => new ElectronicsStore($"{ai.Name} Elektronika"),
                    3 => new ClothingStore($"{ai.Name} Odzież"),
                    _ => new GroceryStore($"{ai.Name} Sklep {count}")
                },
                AIType.Conglomerate => count switch
                {
                    0 => new Farm($"{ai.Name} Farma"),
                    1 => new CheeseFactory($"{ai.Name} Fabryka"),
                    2 => new GroceryStore($"{ai.Name} Sklep"),
                    3 => new IronMine($"{ai.Name} Kopalnia"),
                    4 => new SteelMill($"{ai.Name} Huta"),
                    5 => new ElectronicsFactory($"{ai.Name} Elektronika"),
                    6 => new ElectronicsStore($"{ai.Name} Sklep Tech"),
                    _ => new GeneralStore($"{ai.Name} Sklep {count}")
                },
                _ => null
            };
        }

        private decimal GetNextBuildingCost(AICompany ai)
        {
            return ai.Specialization switch
            {
                AIType.Extractor     => 100000m,
                AIType.Manufacturer  => 250000m,
                AIType.Retailer      => 150000m,
                AIType.Conglomerate  => 300000m,
                _                    => 150000m
            };
        }

        // ──────────────────────────────────────────────────────────
        //  Symulacja przychodów AI na mapie
        // ──────────────────────────────────────────────────────────

        private void SimulateAIRevenue(AICompany ai, int day)
        {
            // Uproszczona symulacja: każdy budynek AI zarabia pewną kwotę
            decimal dailyRevenue = 0m;
            foreach (var building in ai.Buildings)
            {
                decimal buildingRevenue = building switch
                {
                    RetailBuilding  => (decimal)(_rng.NextDouble() * 2000 + 500),
                    FactoryBuilding => (decimal)(_rng.NextDouble() * 1500 + 300),
                    _               => (decimal)(_rng.NextDouble() * 800  + 100)
                };

                // Strategia wpływa na efektywność
                buildingRevenue *= ai.Strategy switch
                {
                    AIStrategy.Aggressive   => 1.3m,
                    AIStrategy.Conservative => 0.8m,
                    _                       => 1.0m
                };

                dailyRevenue += buildingRevenue;
            }

            if (dailyRevenue > 0)
            {
                // Koszty utrzymania
                decimal costs = ai.Buildings.Sum(b => b.MaintenanceCost);
                decimal profit = dailyRevenue - costs;

                if (profit > 0)
                    ai.Balance += profit;
                else if (ai.Balance > Math.Abs(profit))
                    ai.Balance += profit;

                ai.AddTransaction(day, 0, $"Przychody z operacji AI ({ai.Buildings.Count} budynków)", dailyRevenue, "Sprzedaż");
            }
        }

        // ──────────────────────────────────────────────────────────
        //  Dane dla giełdy
        // ──────────────────────────────────────────────────────────

        /// <summary>
        /// Zbiera dane dla StockMarket.OnNewDay() — netWorth, revenue, profit.
        /// </summary>
        public IEnumerable<(string name, decimal netWorth, decimal monthlyRevenue, decimal monthlyProfit, decimal cash)> GetStockMarketData()
        {
            foreach (var ai in Competitors)
            {
                yield return (ai.Name, ai.GetEstimatedNetWorth(), ai.LastMonthRevenue, ai.LastMonthProfit, ai.Balance);
            }
        }
    }
}
