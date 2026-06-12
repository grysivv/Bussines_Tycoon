using System;
using System.Collections.Generic;
using System.Linq;

namespace Conglomerate.Financials
{
    public class FinancialEngine
    {
        private readonly List<FinancialTransaction> _currentMonthTransactions = new List<FinancialTransaction>();

        public List<IFacilitySegment> Facilities { get; } = new List<IFacilitySegment>();
        public decimal Cash { get; private set; }
        public decimal Loans { get; private set; }
        public decimal ShareCapital { get; }
        public decimal RetainedEarnings { get; private set; }
        public decimal TaxRate { get; set; } = 0.19m; // Default 19% corporate tax
        public int CurrentMonthIndex { get; private set; } = 1;

        public List<PeriodSnapshot> MonthlyHistory { get; } = new List<PeriodSnapshot>();
        public List<PeriodSnapshot> QuarterlyHistory { get; } = new List<PeriodSnapshot>();

        public FinancialEngine(decimal startingCash, decimal startingShareCapital)
        {
            Cash = startingCash;
            ShareCapital = startingShareCapital;
            RetainedEarnings = 0m;
        }

        public void RegisterFacility(IFacilitySegment facility)
        {
            if (facility != null && !Facilities.Any(f => f.FacilityId == facility.FacilityId))
            {
                Facilities.Add(facility);
            }
        }

        public void BuyFacility(IFacilitySegment facility)
        {
            if (facility != null)
            {
                RegisterFacility(facility);
                Cash -= facility.PropertyPurchasePrice;
            }
        }

        public void RecordTransaction(int day, int hour, decimal amount, FinancialCategory category, string description, string facilityId = "")
        {
            var tx = new FinancialTransaction(day, hour, amount, category, description, facilityId);
            _currentMonthTransactions.Add(tx);

            // Only actual cash-flow categories modify cash reserves
            if (category != FinancialCategory.Depreciation)
            {
                Cash += amount; // Note: Expenses are passed as negative numbers
            }
        }

        public void RecordTransactionWithoutCashImpact(int day, int hour, decimal amount, FinancialCategory category, string description, string facilityId = "")
        {
            var tx = new FinancialTransaction(day, hour, amount, category, description, facilityId);
            _currentMonthTransactions.Add(tx);
        }

        public void ModifyCashDirectly(decimal delta)
        {
            Cash += delta;
        }

        public void AdjustLoans(decimal deltaAmount)
        {
            Loans += deltaAmount;
            Cash += deltaAmount; // Borrowing adds cash, repaying subtracts cash
        }

        public PnLStatement CalculateCurrentPnL()
        {
            var pnl = new PnLStatement();
            foreach (var tx in _currentMonthTransactions)
            {
                switch (tx.Category)
                {
                    case FinancialCategory.Revenue: pnl.Revenue += tx.Amount; break;
                    case FinancialCategory.RawMaterials: pnl.RawMaterials += tx.Amount; break;
                    case FinancialCategory.Logistics: pnl.Logistics += tx.Amount; break;
                    case FinancialCategory.Marketing: pnl.Marketing += tx.Amount; break;
                    case FinancialCategory.Salaries: pnl.Salaries += tx.Amount; break;
                    case FinancialCategory.Depreciation: pnl.Depreciation += tx.Amount; break;
                    case FinancialCategory.CorporateTax: pnl.CorporateTax += tx.Amount; break;
                    case FinancialCategory.Interest: pnl.Interest += tx.Amount; break;
                }
            }

            // Estimate depreciation if it hasn't been posted yet for this month
            if (!HasDepreciationPosted())
            {
                decimal totalEstDep = 0m;
                foreach (var f in Facilities)
                {
                    totalEstDep += (f.PropertyPurchasePrice * f.DepreciationRate) / 12m;
                }
                pnl.Depreciation = -totalEstDep;
            }

            return pnl;
        }

        public BalanceSheet CalculateCurrentBalanceSheet()
        {
            decimal inventoryVal = Facilities.Sum(f => f.InventoryValue);
            decimal propertyBookVal = Facilities.Sum(f => f.PropertyBookValue);

            return new BalanceSheet
            {
                Cash = this.Cash,
                InventoryValue = inventoryVal,
                PropertyBookValue = propertyBookVal,
                Loans = this.Loans,
                ShareCapital = this.ShareCapital,
                RetainedEarnings = this.RetainedEarnings
            };
        }

        public void CloseMonth(int day, int hour)
        {
            // 1. Post depreciation expenses for all facilities
            foreach (var f in Facilities)
            {
                decimal monthlyDep = (f.PropertyPurchasePrice * f.DepreciationRate) / 12m;
                f.AccumulatedDepreciation += monthlyDep;

                // Log a non-cash transaction for P&L tracking
                RecordTransaction(
                    day, hour, 
                    -monthlyDep, 
                    FinancialCategory.Depreciation, 
                    $"Amortyzacja: {f.Name}", 
                    f.FacilityId
                );
            }

            // 2. Calculate corporate tax based on EBT
            var initialPnL = CalculateCurrentPnL();
            if (initialPnL.EBT > 0m)
            {
                decimal taxAmount = initialPnL.EBT * TaxRate;
                RecordTransaction(
                    day, hour, 
                    -taxAmount, 
                    FinancialCategory.CorporateTax, 
                    "Naliczony podatek dochodowy (CIT)"
                );
            }

            // 3. Finalize monthly P&L and Balance Sheet
            var finalPnL = CalculateCurrentPnL();
            var finalBS = CalculateCurrentBalanceSheet();

            // 4. Update Retained Earnings with Net Income
            RetainedEarnings += finalPnL.NetIncome;

            // Update Balance Sheet to reflect updated Retained Earnings
            var archivedBS = CalculateCurrentBalanceSheet();
            if (!archivedBS.IsBalanced)
            {
                throw new InvalidOperationException("Balance Sheet does not balance after EOM retained earnings adjustment.");
            }

            // 5. Save Monthly Snapshot
            MonthlyHistory.Add(new PeriodSnapshot(CurrentMonthIndex, $"Miesiąc {CurrentMonthIndex}", finalPnL, archivedBS));
            if (MonthlyHistory.Count > 24)
            {
                MonthlyHistory.RemoveAt(0); // Maintain 24 months limit (FIFO)
            }

            // 6. Quarterly aggregation if applicable
            if (CurrentMonthIndex % 3 == 0)
            {
                int qIndex = CurrentMonthIndex / 3;
                var last3Snaps = MonthlyHistory.Skip(Math.Max(0, MonthlyHistory.Count - 3)).Take(3).ToList();
                
                var qPnL = new PnLStatement();
                foreach (var snap in last3Snaps)
                {
                    qPnL.Revenue += snap.PnL.Revenue;
                    qPnL.RawMaterials += snap.PnL.RawMaterials;
                    qPnL.Logistics += snap.PnL.Logistics;
                    qPnL.Marketing += snap.PnL.Marketing;
                    qPnL.Salaries += snap.PnL.Salaries;
                    qPnL.Depreciation += snap.PnL.Depreciation;
                    qPnL.CorporateTax += snap.PnL.CorporateTax;
                    qPnL.Interest += snap.PnL.Interest;
                }

                QuarterlyHistory.Add(new PeriodSnapshot(qIndex, $"Kwartał {qIndex}", qPnL, archivedBS.Clone()));
                if (QuarterlyHistory.Count > 8)
                {
                    QuarterlyHistory.RemoveAt(0); // Maintain 8 quarters limit (FIFO)
                }
            }

            // 7. Transition to the next month
            _currentMonthTransactions.Clear();
            CurrentMonthIndex++;
        }

        private bool HasDepreciationPosted()
        {
            return _currentMonthTransactions.Any(tx => tx.Category == FinancialCategory.Depreciation);
        }

        public List<FinancialTransaction> GetCurrentMonthTransactions()
        {
            return new List<FinancialTransaction>(_currentMonthTransactions);
        }
    }
}
