namespace Conglomerate.Financials
{
    public class PeriodSnapshot
    {
        public int PeriodIndex { get; }
        public string PeriodName { get; }
        public PnLStatement PnL { get; }
        public BalanceSheet BalanceSheet { get; }

        public PeriodSnapshot(int periodIndex, string periodName, PnLStatement pnl, BalanceSheet balanceSheet)
        {
            PeriodIndex = periodIndex;
            PeriodName = periodName ?? $"Okres {periodIndex}";
            PnL = pnl;
            BalanceSheet = balanceSheet;
        }
    }
}
