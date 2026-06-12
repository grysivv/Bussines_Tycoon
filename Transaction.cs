namespace Conglomerate
{
    public class Transaction
    {
        public int Day { get; }
        public int Hour { get; }
        public string Description { get; }
        public decimal Amount { get; } // Ujemna dla wydatków, dodatnia dla przychodów
        public string Category { get; } // "Budowa", "Utrzymanie", "Sprzedaż"

        public Transaction(int day, int hour, string description, decimal amount, string category)
        {
            Day = day;
            Hour = hour;
            Description = description;
            Amount = amount;
            Category = category;
        }
    }
}
