using System;
using System.Collections.Generic;
using System.Linq;

namespace Conglomerate.HR
{
    public class HRManager : IHRManager
    {
        private readonly List<Employee> _employees = new List<Employee>();
        private readonly List<Employee> _candidatePool = new List<Employee>();
        private readonly Random _random = new Random();
        private int _lastProcessedDay = -1;
        private int _lastProcessedMonthDay = -1;

        public IReadOnlyList<Employee> Employees => _employees;
        public IReadOnlyList<Employee> CandidatePool => _candidatePool;
        public HRConfig Config { get; }

        public event Action<Employee>? OnEmployeePlanningToQuit;

        private static readonly string[] FirstNames = { "Jan", "Anna", "Piotr", "Maria", "Krzysztof", "Katarzyna", "Tomasz", "Małgorzata", "Paweł", "Agnieszka", "John", "Jane", "Alice", "Bob", "Charlie", "Diana" };
        private static readonly string[] LastNames = { "Kowalski", "Nowak", "Wiśniewski", "Wójcik", "Kowalczyk", "Kamiński", "Smith", "Johnson", "Williams", "Brown", "Jones", "Miller" };

        public static readonly List<JobRole> DefaultJobRoles = new List<JobRole>
        {
            new JobRole("Junior Developer", 3500m, Department.Production),
            new JobRole("Senior Developer", 7500m, Department.Production),
            new JobRole("Accountant", 4500m, Department.Finance),
            new JobRole("Marketing Specialist", 4000m, Department.Marketing),
            new JobRole("HR Specialist", 4200m, Department.HR),
            new JobRole("Manager", 6500m, Department.Production)
        };

        public HRManager(HRConfig config)
        {
            Config = config ?? new HRConfig();
            RefreshCandidatePool();
        }

        public void HireEmployee(Employee candidate)
        {
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));
            
            if (_candidatePool.Contains(candidate))
            {
                _candidatePool.Remove(candidate);
            }
            
            if (!_employees.Any(e => e.Id == candidate.Id))
            {
                _employees.Add(candidate);
            }
        }

        public void FireEmployee(Guid employeeId)
        {
            var emp = _employees.FirstOrDefault(e => e.Id == employeeId);
            if (emp != null)
            {
                _employees.Remove(emp);
            }
        }

        public decimal CalculateTotalMonthlyPayroll()
        {
            return _employees.Sum(e => e.MonthlySalary);
        }

        public void RefreshCandidatePool()
        {
            _candidatePool.Clear();
            int candidatesCount = Config.MaxCandidatesCount;
            for (int i = 0; i < candidatesCount; i++)
            {
                _candidatePool.Add(GenerateRandomCandidate());
            }
        }

        public void Update(int currentDay, int currentHour)
        {
            // Only execute day ticks once per day
            if (currentDay != _lastProcessedDay)
            {
                bool isWeekend = IsWeekend(currentDay);
                
                foreach (var employee in _employees)
                {
                    employee.UpdateState(isWeekend, Config);
                }

                // Periodic candidate refresh
                if (currentDay > 1 && currentDay % Config.CandidatePoolRefreshDays == 0)
                {
                    RefreshCandidatePool();
                }

                _lastProcessedDay = currentDay;
            }

            // Monthly tick (every 30 days)
            if (currentDay > 1 && (currentDay - 1) % 30 == 0 && currentDay != _lastProcessedMonthDay)
            {
                foreach (var employee in _employees)
                {
                    bool wasPlanningToQuit = employee.IsPlanningToQuit;
                    employee.CheckQuittingStatus(Config);
                    
                    if (!wasPlanningToQuit && employee.IsPlanningToQuit)
                    {
                        OnEmployeePlanningToQuit?.Invoke(employee);
                    }
                }
                _lastProcessedMonthDay = currentDay;
            }
        }

        private bool IsWeekend(int day)
        {
            // Assuming Day 1 is Monday.
            // Day 6 (Saturday) and Day 7 (Sunday) are weekends.
            int dayOfWeek = (day - 1) % 7;
            return dayOfWeek == 5 || dayOfWeek == 6;
        }

        private Employee GenerateRandomCandidate()
        {
            string firstName = FirstNames[_random.Next(FirstNames.Length)];
            string lastName = LastNames[_random.Next(LastNames.Length)];
            string fullName = $"{firstName} {lastName}";

            JobRole role = DefaultJobRoles[_random.Next(DefaultJobRoles.Count)];
            
            // Random salary starting slightly above or below market base (e.g. 90% to 110%)
            double multiplier = 0.9 + (_random.NextDouble() * 0.2);
            decimal startingSalary = Math.Round(role.BaseMarketSalary * (decimal)multiplier, 0);

            // Starting satisfaction 70 to 90
            float satisfaction = (float)(70.0 + (_random.NextDouble() * 20.0));

            return new Employee(Guid.NewGuid(), fullName, role, startingSalary, satisfaction, 0f);
        }
    }
}
