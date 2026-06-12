using System;
using System.Collections.Generic;

namespace Conglomerate
{
    public enum ResourceCategory
    {
        Food,
        Mining,
        Other
    }

    public static class ResourceRegistry
    {
        private static readonly Dictionary<string, (ResourceCategory Category, decimal DefaultPrice)> Registry = 
            new Dictionary<string, (ResourceCategory, decimal)>
            {
                { "Mleko", (ResourceCategory.Food, 50m) },
                { "Mięso", (ResourceCategory.Food, 150m) },
                { "Węgiel", (ResourceCategory.Mining, 100m) }
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
