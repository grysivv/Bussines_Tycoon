using System;
using System.Collections.Generic;

namespace Conglomerate
{
    public enum ResourceCategory
    {
        Food,            // Surowce spożywcze (Mleko, Mięso)
        ProcessedFood,   // Produkty przetworzone (Ser, Masło, Żywność Pakowana)
        Mining,          // Surowce kopalniane (Węgiel, Rudy)
        Metal,           // Metale przetworzone (Miedź, Stal, Stal Premium)
        Electronics,     // Elektronika (Komponenty, Smartfon, Laptop)
        Textile,         // Tekstylia (Tkanina, Odzież)
        Wood,            // Drewno i meble
        Bakery,          // Wyroby piekarnicze
        Other
    }

    public static class ResourceRegistry
    {
        private static readonly Dictionary<string, (ResourceCategory Category, decimal DefaultPrice)> Registry = 
            new Dictionary<string, (ResourceCategory, decimal)>
            {
                // ── Surowce rolne ──────────────────────────────────
                { "Mleko",                (ResourceCategory.Food,          50m)    },
                { "Mięso",                (ResourceCategory.Food,          150m)   },

                // ── Przetworzone spożywcze ─────────────────────────
                { "Ser",                  (ResourceCategory.ProcessedFood,  220m)  },
                { "Masło",                (ResourceCategory.ProcessedFood,  130m)  },
                { "Chleb",                (ResourceCategory.Bakery,         50m)   },
                { "Wyroby Cukiernicze",   (ResourceCategory.Bakery,         180m)  },
                { "Żywność Pakowana",     (ResourceCategory.ProcessedFood,  350m)  },

                // ── Surowce kopalniane ─────────────────────────────
                { "Węgiel",               (ResourceCategory.Mining,         400m)  },
                { "Ruda Miedzi",          (ResourceCategory.Mining,         100m)  },
                { "Ruda Żelaza",          (ResourceCategory.Mining,         120m)  },

                // ── Metale przetworzone ────────────────────────────
                { "Miedź",                (ResourceCategory.Metal,          350m)  },
                { "Stal",                 (ResourceCategory.Metal,          800m)  },
                { "Stal Premium",         (ResourceCategory.Metal,          1400m) },

                // ── Elektronika ────────────────────────────────────
                { "Komponenty Elektroniczne", (ResourceCategory.Electronics, 800m) },
                { "Smartfon",             (ResourceCategory.Electronics,    2500m) },
                { "Laptop",               (ResourceCategory.Electronics,    5000m) },

                // ── Tekstylia ──────────────────────────────────────
                { "Tkanina",              (ResourceCategory.Textile,        200m)  },
                { "Odzież",               (ResourceCategory.Textile,        600m)  },
                { "Odzież Premium",       (ResourceCategory.Textile,        1800m) },

                // ── Drewno i meble ─────────────────────────────────
                { "Drewno Przetarte",     (ResourceCategory.Wood,           250m)  },
                { "Meble",                (ResourceCategory.Wood,           1500m) },
                { "Meble Luksusowe",      (ResourceCategory.Wood,           4000m) },
            };

        public static ResourceCategory GetCategory(string resourceName)
        {
            if (Registry.ContainsKey(resourceName))
            {
                return Registry[resourceName].Category;
            }
            return ResourceCategory.Other;
        }

        public static decimal GetPrice(string resourceName)
        {
            if (Registry.ContainsKey(resourceName))
            {
                return Registry[resourceName].DefaultPrice;
            }
            return 0m;
        }

        public static List<string> GetResourcesByCategory(ResourceCategory category)
        {
            var list = new List<string>();
            foreach (var kvp in Registry)
            {
                if (kvp.Value.Category == category)
                {
                    list.Add(kvp.Key);
                }
            }
            return list;
        }
    }
}
