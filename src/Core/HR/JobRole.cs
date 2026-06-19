namespace Conglomerate.HR
{
    public class JobRole
    {
        public string Title { get; private set; }
        public decimal BaseMarketSalary { get; private set; }
        public Department Type { get; private set; }

        public JobRole(string title, decimal baseMarketSalary, Department type)
        {
            Title = title;
            BaseMarketSalary = baseMarketSalary;
            Type = type;
        }
    }
}
