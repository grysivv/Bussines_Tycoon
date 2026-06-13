using System;
using System.Collections.Generic;
using Conglomerate.Financials;

namespace Conglomerate
{
    public class Company
    {
        public string Name { get; set; }
        public List<Building> Buildings { get; } = new List<Building>();
        public FinancialLedger Ledger { get; } = new FinancialLedger();
        public FinancialEngine Engine { get; }

        public decimal Balance
        {
            get => Engine.Cash;
            set => Engine.ModifyCashDirectly(value - Engine.Cash);
        }

        public Company(string name, decimal startingBalance)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "BezNazwy Corp" : name;
            Engine = new FinancialEngine(startingBalance, startingBalance);
        }

        public void AddTransaction(int day, int hour, string description, decimal amount, string category, string facilityId = "")
        {
            // Support legacy ledger
            Ledger.Record(day, hour, description, amount, category);

            // Support new financial engine
            FinancialCategory cat = FinancialCategory.Marketing;
            if (category == "Sprzedaż")           cat = FinancialCategory.Revenue;
            else if (category == "Sprzedaż detaliczna") cat = FinancialCategory.Revenue;
            else if (category == "Utrzymanie")    cat = FinancialCategory.Salaries;
            else if (category == "Budowa")         cat = FinancialCategory.RawMaterials;
            else if (category == "Zakup surowców") cat = FinancialCategory.RawMaterials;
            else if (category == "Koszty produkcji") cat = FinancialCategory.RawMaterials;
            else if (category == "Transport")      cat = FinancialCategory.RawMaterials;

            Engine.RecordTransactionWithoutCashImpact(day, hour, amount, cat, description, facilityId);
        }

        public bool BuyBuilding(Building building, Map map, int x, int y, int day, int hour)
        {
            if (Balance >= building.BuildCost)
            {
                if (map.BuildBuildingOnTile(x, y, building))
                {
                    Engine.BuyFacility(building);
                    Buildings.Add(building);
                    
                    Ledger.Record(day, hour, $"Zakup: {building.Name}", -building.BuildCost, "Budowa");
                    Engine.RecordTransactionWithoutCashImpact(day, hour, -building.BuildCost, FinancialCategory.RawMaterials, $"Zakup: {building.Name}", building.FacilityId);
                    
                    return true;
                }
            }
            return false;
        }
    }
}
