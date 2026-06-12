using System;
using System.Collections.Generic;

namespace Conglomerate
{
    public class Company
    {
        public string Name { get; set; }
        public decimal Balance { get; set; }
        public List<Building> Buildings { get; } = new List<Building>();
        public FinancialLedger Ledger { get; } = new FinancialLedger();

        public Company(string name, decimal startingBalance)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "BezNazwy Corp" : name;
            Balance = startingBalance;
        }

        public void AddTransaction(int day, int hour, string description, decimal amount, string category)
        {
            Ledger.Record(day, hour, description, amount, category);
        }

        public bool BuyBuilding(Building building, Map map, int x, int y, int day, int hour)
        {
            if (Balance >= building.BuildCost)
            {
                if (map.BuildBuildingOnTile(x, y, building))
                {
                    Balance -= building.BuildCost;
                    Buildings.Add(building);
                    AddTransaction(day, hour, $"Zakup: {building.Name}", -building.BuildCost, "Budowa");
                    return true;
                }
            }
            return false;
        }
    }
}
