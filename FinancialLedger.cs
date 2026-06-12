using System;
using System.Collections.Generic;
using System.Linq;

namespace Conglomerate
{
    public class FinancialLedger
    {
        private readonly List<Transaction> _transactions = new List<Transaction>();

        public void Record(int day, int hour, string description, decimal amount, string category)
        {
            _transactions.Add(new Transaction(day, hour, description, amount, category));
        }

        public List<Transaction> GetTransactionsForPeriod(int currentDay, int currentHour, int hoursLimit)
        {
            return _transactions.Where(t => 
                (currentDay - t.Day) * 24 + (currentHour - t.Hour) <= hoursLimit
            ).ToList();
        }

        public decimal GetSumByCategory(List<Transaction> periodTransactions, string category)
        {
            return periodTransactions.Where(t => t.Category == category).Sum(t => t.Amount);
        }

        public List<Transaction> GetAllTransactions()
        {
            return _transactions;
        }
    }
}
