using System;

namespace Conglomerate.Financials
{
    public enum FinancialCategory
    {
        Revenue,        // Sales revenues
        RawMaterials,   // Direct materials cost (COGS)
        Logistics,      // Distribution & shipping
        Marketing,      // Ad campaigns, promos
        Salaries,       // Employees payroll
        Depreciation,   // Property depreciation (non-cash)
        CorporateTax,   // Corporation income tax
        Interest,       // Bank loans debt service (interest only)
        Capex           // Capital expenditures (non-P&L)
    }

    public struct FinancialTransaction
    {
        public int Day { get; }
        public int Hour { get; }
        public decimal Amount { get; } // Positive for revenues, negative for expenses
        public FinancialCategory Category { get; }
        public string Description { get; }
        public string FacilityId { get; } // Reference to segment (facility)

        public FinancialTransaction(int day, int hour, decimal amount, FinancialCategory category, string description, string facilityId)
        {
            Day = day;
            Hour = hour;
            Amount = amount;
            Category = category;
            Description = description ?? "";
            FacilityId = facilityId ?? "";
        }
    }

    public interface IFacilitySegment
    {
        string FacilityId { get; }
        string Name { get; }
        decimal PropertyPurchasePrice { get; }
        decimal DepreciationRate { get; } // Yearly depreciation rate (e.g., 0.04m for 4% per year)
        decimal AccumulatedDepreciation { get; set; }
        decimal PropertyBookValue { get; } // Price minus accumulated depreciation
        decimal InventoryValue { get; } // Current valuation of resources stored in warehouse
    }
}
