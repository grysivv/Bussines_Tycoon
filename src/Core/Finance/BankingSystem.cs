using System;
using System.Collections.Generic;
using System.Linq;

namespace Conglomerate.Finance
{
    /// <summary>
    /// System Bankowy — zarządza kredytami gracza.
    /// Capitalism Lab style: możliwość zaciągania pożyczek, spłaty, 
    /// automatyczne naliczanie odsetek co miesiąc.
    /// </summary>
    public class BankingSystem
    {
        private readonly List<Loan> _loans = new List<Loan>();

        public IReadOnlyList<Loan> Loans => _loans;

        /// <summary>Łączne zadłużenie gracza.</summary>
        public decimal TotalDebt => _loans.Sum(l => l.OutstandingBalance);

        /// <summary>Łączne miesięczne zobowiązania (raty).</summary>
        public decimal TotalMonthlyPayments => _loans.Sum(l => l.MonthlyPayment);

        // ─────────────────────────────────────────────
        //  Parametry banku
        // ─────────────────────────────────────────────

        private static readonly Dictionary<LoanType, (decimal rate, int months)> LoanParameters = new()
        {
            [LoanType.ShortTerm]  = (0.10m, 12),   // 10% rocznie, 12 miesięcy
            [LoanType.MediumTerm] = (0.08m, 36),   // 8% rocznie, 36 miesięcy
            [LoanType.LongTerm]   = (0.06m, 60),   // 6% rocznie, 60 miesięcy
        };

        // ─────────────────────────────────────────────
        //  Zaciąganie kredytu
        // ─────────────────────────────────────────────

        /// <summary>
        /// Oblicza maksymalną zdolność kredytową firmy.
        /// Model: max pożyczka = 2x majątek netto firmy (bez długu).
        /// </summary>
        public decimal CalculateCreditLimit(decimal netWorth, decimal existingDebt)
        {
            decimal maxTotal = Math.Max(0m, netWorth * 2m);
            return Math.Max(0m, maxTotal - existingDebt);
        }

        /// <summary>
        /// Gracz zaciąga nowy kredyt.
        /// Zwraca true jeśli transakcja się powiodła.
        /// </summary>
        public bool TakeLoan(
            LoanType type,
            decimal amount,
            Company company,
            int day, int hour)
        {
            if (amount <= 0) return false;

            var (rate, months) = LoanParameters[type];

            var loan = new Loan
            {
                Type = type,
                Principal = amount,
                OutstandingBalance = amount,
                AnnualInterestRate = rate,
                TotalMonths = months,
                MonthsRemaining = months,
                DayTaken = day,
                Description = $"{type} Loan – {amount:C}"
            };

            _loans.Add(loan);
            company.Balance += amount;
            company.Engine.AdjustLoans(amount);

            company.AddTransaction(day, hour,
                $"Kredyt bankowy ({type}): +{amount:C}",
                amount, "Kredyty", "");

            return true;
        }

        /// <summary>
        /// Gracz spłaca część lub całość kredytu.
        /// </summary>
        public bool RepayLoan(Guid loanId, decimal amount, Company company, int day, int hour)
        {
            var loan = _loans.FirstOrDefault(l => l.LoanId == loanId);
            if (loan == null || loan.IsFullyRepaid) return false;
            if (company.Balance < amount) return false;

            decimal repayment = Math.Min(amount, loan.OutstandingBalance);
            loan.OutstandingBalance -= repayment;
            company.Balance -= repayment;
            company.Engine.AdjustLoans(-repayment);

            company.AddTransaction(day, hour,
                $"Spłata kredytu: -{repayment:C}",
                -repayment, "Kredyty", "");

            if (loan.IsFullyRepaid)
                _loans.Remove(loan);

            return true;
        }

        // ─────────────────────────────────────────────
        //  Miesięczny tick (odsetki + raty)
        // ─────────────────────────────────────────────

        /// <summary>
        /// Wywoływane przez GameManager na koniec każdego miesiąca.
        /// Pobiera odsetki i ratę kapitałową od firmy gracza.
        /// </summary>
        public void ProcessMonthlyPayments(Company company, int day, int hour)
        {
            var toRemove = new List<Loan>();

            foreach (var loan in _loans)
            {
                if (loan.IsFullyRepaid) { toRemove.Add(loan); continue; }

                decimal interest = loan.MonthlyInterest;
                decimal capital = Math.Min(loan.MonthlyCapitalRepayment, loan.OutstandingBalance);
                decimal totalPayment = interest + capital;

                if (company.Balance >= totalPayment)
                {
                    company.Balance -= totalPayment;
                    loan.OutstandingBalance -= capital;
                    loan.MonthsRemaining--;
                    company.Engine.AdjustLoans(-capital);

                    company.AddTransaction(day, hour,
                        $"Rata kredytu (odsetki {interest:C} + kapitał {capital:C})",
                        -totalPayment, "Kredyty", "");
                }
                else
                {
                    // Brak środków — pobierz tylko odsetki (w trudnej sytuacji)
                    if (company.Balance >= interest)
                    {
                        company.Balance -= interest;
                        company.AddTransaction(day, hour,
                            $"Odsetki kredytu (brak środków na ratę): -{interest:C}",
                            -interest, "Kredyty", "");
                    }
                }

                if (loan.IsFullyRepaid || loan.MonthsRemaining <= 0)
                    toRemove.Add(loan);
            }

            foreach (var l in toRemove) _loans.Remove(l);
        }

        /// <summary>Dostępne typy kredytów z parametrami.</summary>
        public static IEnumerable<(LoanType type, decimal rate, int months)> GetAvailableLoanTypes()
        {
            foreach (var kvp in LoanParameters)
                yield return (kvp.Key, kvp.Value.rate, kvp.Value.months);
        }
    }
}
