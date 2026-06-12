using System;

namespace Conglomerate.Financials
{
    public class PnLStatement
    {
        public decimal Revenue { get; set; }        // Positive revenue
        public decimal RawMaterials { get; set; }   // Negative expense (COGS)
        public decimal Logistics { get; set; }      // Negative expense
        public decimal Marketing { get; set; }      // Negative expense
        public decimal Salaries { get; set; }       // Negative expense
        public decimal Depreciation { get; set; }   // Negative expense (non-cash)
        public decimal CorporateTax { get; set; }   // Negative expense
        public decimal Interest { get; set; }       // Negative expense

        // Gross Profit (Revenue - COGS)
        public decimal GrossProfit => Revenue + RawMaterials;

        // EBITDA (Earnings Before Interest, Tax, Depreciation, and Amortization)
        public decimal EBITDA => GrossProfit + Logistics + Marketing + Salaries;

        // EBIT (Operating Profit)
        public decimal EBIT => EBITDA + Depreciation;

        // EBT (Earnings Before Tax)
        public decimal EBT => EBIT + Interest;

        // Net Income (Net Profit)
        public decimal NetIncome => EBT + CorporateTax;

        public PnLStatement Clone()
        {
            return new PnLStatement
            {
                Revenue = this.Revenue,
                RawMaterials = this.RawMaterials,
                Logistics = this.Logistics,
                Marketing = this.Marketing,
                Salaries = this.Salaries,
                Depreciation = this.Depreciation,
                CorporateTax = this.CorporateTax,
                Interest = this.Interest
            };
        }
    }
}
