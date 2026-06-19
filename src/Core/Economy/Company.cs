using System;
using System.Collections.Generic;
using Conglomerate.Financials;
using Conglomerate.Marketing;
using Conglomerate.HR;

namespace Conglomerate
{
    public class Company
    {
        public string Name { get; set; }
        public List<Building> Buildings { get; } = new List<Building>();
        public FinancialLedger Ledger { get; } = new FinancialLedger();
        public FinancialEngine Engine { get; }

        /// <summary>
        /// Poziom technologii dla poszczególnych produktów/surowców. Wpływa na jakość.
        /// Domyślnie 0.
        /// </summary>
        public Dictionary<string, float> TechLevels { get; } = new Dictionary<string, float>();

        /// <summary>Portfolio akcji gracza: nazwa firmy → liczba akcji.</summary>
        public Dictionary<string, decimal> OwnedShares { get; } = new Dictionary<string, decimal>();

        /// <summary>Rozpoznawalność marki per produkt (0-100). Wpływa na popyt detaliczny.</summary>
        public Dictionary<string, float> BrandAwareness { get; } = new Dictionary<string, float>();

        /// <summary>Aktywne kampanie reklamowe.</summary>
        public List<AdvertisingCampaign> ActiveCampaigns { get; } = new List<AdvertisingCampaign>();

        /// <summary>Zatrudnieni dyrektorzy (C-Suite).</summary>
        public List<Executive> HiredExecutives { get; } = new List<Executive>();

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
            else if (category == "Budowa")         cat = FinancialCategory.Capex;
            else if (category == "Zakup surowców") cat = FinancialCategory.RawMaterials;
            else if (category == "Koszty produkcji") cat = FinancialCategory.RawMaterials;
            else if (category == "Transport")      cat = FinancialCategory.Logistics;

            Engine.RecordTransactionWithoutCashImpact(day, hour, amount, cat, description, facilityId);
        }

        public bool BuyBuilding(Building building, Map map, int x, int y, int day, int hour)
        {
            var tile = map.GetTile(x, y);
            decimal cost = building.BuildCost * (decimal)tile.LandValue;
            building.EffectiveBuildCost = cost;

            if (Balance >= cost)
            {
                if (map.BuildBuildingOnTile(x, y, building))
                {
                    if (building is RetailBuilding rb)
                    {
                        rb.LocationFactor = tile.LandValue;
                    }

                    building.X = x;
                    building.Y = y;
                    Engine.BuyFacility(building);
                    Buildings.Add(building);
                    
                    Ledger.Record(day, hour, $"Zakup: {building.Name}", -cost, "Budowa");
                    Engine.RecordTransactionWithoutCashImpact(day, hour, -cost, FinancialCategory.Capex, $"Zakup: {building.Name}", building.FacilityId);
                    
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Oblicza wartość netto firmy (majątek - dług).
        /// Używana przez giełdę do wyceny akcji.
        /// </summary>
        public decimal GetNetWorth()
        {
            decimal inventoryVal = Engine.Facilities.Sum(f => f.InventoryValue);
            decimal propertyVal  = Engine.Facilities.Sum(f => f.PropertyBookValue);
            return Engine.Cash + inventoryVal + propertyVal - Engine.Loans;
        }

        /// <summary>Pobiera poziom Brand Awareness dla produktu (0-100).</summary>
        public float GetBrandAwareness(string productName)
        {
            return BrandAwareness.TryGetValue(productName, out float val) ? val : 0f;
        }

        /// <summary>Zwiększa Brand Awareness, nie przekraczając 100.</summary>
        public void IncreaseBrandAwareness(string productName, float delta)
        {
            if (!BrandAwareness.ContainsKey(productName))
                BrandAwareness[productName] = 0f;
            BrandAwareness[productName] = Math.Min(100f, BrandAwareness[productName] + delta);
        }

        /// <summary>Naturalny spadek Brand Awareness (co dzień bez reklamy).</summary>
        public void DecayBrandAwareness(float decayRate = 0.5f)
        {
            foreach (var key in new List<string>(BrandAwareness.Keys))
            {
                BrandAwareness[key] = Math.Max(0f, BrandAwareness[key] - decayRate);
            }
        }
    }
}
