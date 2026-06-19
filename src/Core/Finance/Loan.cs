using System;

namespace Conglomerate.Finance
{
    /// <summary>Typy kredytów bankowych.</summary>
    public enum LoanType
    {
        ShortTerm,   // Krótkoterminowy (12 miesięcy)
        MediumTerm,  // Średnioterminowy (36 miesięcy)
        LongTerm     // Długoterminowy (60 miesięcy)
    }

    /// <summary>
    /// Reprezentuje jeden kredyt bankowy zaciągnięty przez gracza.
    /// </summary>
    public class Loan
    {
        public Guid LoanId { get; } = Guid.NewGuid();
        public LoanType Type { get; set; }
        public decimal Principal { get; set; }          // Kwota główna
        public decimal OutstandingBalance { get; set; } // Pozostałe do spłaty
        public decimal AnnualInterestRate { get; set; } // Oprocentowanie roczne (np. 0.08 = 8%)
        public int TotalMonths { get; set; }            // Całkowity czas kredytu w miesiącach
        public int MonthsRemaining { get; set; }        // Pozostałe miesiące

        public int DayTaken { get; set; }
        public string Description { get; set; } = string.Empty;

        /// <summary>Miesięczna rata odsetkowa.</summary>
        public decimal MonthlyInterest => OutstandingBalance * (AnnualInterestRate / 12m);

        /// <summary>Miesięczna rata kapitałowa.</summary>
        public decimal MonthlyCapitalRepayment => TotalMonths > 0 ? Principal / TotalMonths : 0m;

        /// <summary>Łączna miesięczna rata (kapitał + odsetki).</summary>
        public decimal MonthlyPayment => MonthlyCapitalRepayment + MonthlyInterest;

        public bool IsFullyRepaid => OutstandingBalance <= 0m;
    }
}
