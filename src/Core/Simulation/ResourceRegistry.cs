using System;
using System.Collections.Generic;

namespace Conglomerate
{
    public enum ResourceCategory
    {
        Food,           // Surowce spożywcze (Mleko, Mięso)
        ProcessedFood,  // Produkty przetworzone (Ser, Masło, itp.)
        Mining,         // Surowce kopalniane (Węgiel, Ruda Żelaza, itp.)
        Other
    }

    public static class ResourceRegistry
    {
        private static readonly Dictionary<string, (ResourceCategory Category, decimal DefaultPrice)> Registry = 
            new Dictionary<string, (ResourceCategory, decimal)>
            {
                // Surowce rolne (surowe)
                { "Mleko",   (ResourceCategory.Food, 50m) },
                { "Mięso",   (ResourceCategory.Food, 150m) },

                // Produkty przetworzone (wyjścia fabryk)
                { "Ser",     (ResourceCategory.ProcessedFood, 220m) },
                { "Masło",   (ResourceCategory.ProcessedFood, 130m) },

                // Surowce kopalniane
                { "Węgiel",  (ResourceCategory.Mining, 100m) },
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
