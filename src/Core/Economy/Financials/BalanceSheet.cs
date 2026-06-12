using System;

namespace Conglomerate.Financials
{
    public class BalanceSheet
    {
        // --- ASSETS (AKTYWA) ---
        public decimal Cash { get; set; }
        public decimal InventoryValue { get; set; }
        public decimal PropertyBookValue { get; set; }

        public decimal TotalAssets => Cash + InventoryValue + PropertyBookValue;

        // --- LIABILITIES (ZOBOWIĄZANIA / PASYWA OBCE) ---
        public decimal Loans { get; set; }

        public decimal TotalLiabilities => Loans;

        // --- EQUITY (KAPITAŁ WŁASNY) ---
        public decimal ShareCapital { get; set; }
        public decimal RetainedEarnings { get; set; }

        public decimal TotalEquity => ShareCapital + RetainedEarnings;

        // --- TOTAL LIABILITIES & EQUITY ---
        public decimal TotalLiabilitiesAndEquity => TotalLiabilities + TotalEquity;

        // Accounting Equation check: Assets = Liabilities + Equity
        public bool IsBalanced => Math.Abs(TotalAssets - TotalLiabilitiesAndEquity) < 0.01m;

        public BalanceSheet Clone()
        {
            return new BalanceSheet
            {
                Cash = this.Cash,
                InventoryValue = this.InventoryValue,
                PropertyBookValue = this.PropertyBookValue,
                Loans = this.Loans,
                ShareCapital = this.ShareCapital,
                RetainedEarnings = this.RetainedEarnings
            };
        }
    }
}
