using System;
using System.Collections.Generic;

namespace Conglomerate.HR
{
    public interface IHRManager
    {
        IReadOnlyList<Employee> Employees { get; }
        IReadOnlyList<Employee> CandidatePool { get; }
        HRConfig Config { get; }

        event Action<Employee>? OnEmployeePlanningToQuit;

        void HireEmployee(Employee candidate);
        void FireEmployee(Guid employeeId);
        decimal CalculateTotalMonthlyPayroll();
        void Update(int currentDay, int currentHour);
        void RefreshCandidatePool();
    }
}
