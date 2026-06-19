using System;
using System.Diagnostics;
using System.Linq;
using Conglomerate.HR;

namespace Conglomerate.Tests
{
    public static class HRSystemTests
    {
        public static void RunTests()
        {
            Console.WriteLine("=== URUCHAMIANIE TESTÓW SYSTEMU HR & PRACOWNIKÓW ===");

            TestEmployeeStateMutation();
            TestEmployeeEfficiencyFormula();
            TestEmployeeQuittingLogic();
            TestHRManagerHiringFiring();
            TestHRManagerPayrollAndUpdates();

            Console.WriteLine("=== WSZYSTKIE TESTY HR ZAKOŃCZONE SUKCESEM! ===");
        }

        private static void TestEmployeeStateMutation()
        {
            Console.Write("Test 1: Zmiana stanu pracownika (Fatigue i Satisfaction)... ");

            var config = new HRConfig
            {
                FatigueIncreaseWorkday = 10f,
                FatigueDecreaseWeekend = 20f,
                SatisfactionAdjustmentRate = 5f
            };

            var role = new JobRole("Programista", 5000m, Department.Production);
            // Employee paid 4000 (market is 5000). Target satisfaction is 80%.
            var employee = new Employee(Guid.NewGuid(), "Adam Nowak", role, 4000m, satisfaction: 100f, fatigue: 10f);

            // 1. Workday update
            employee.UpdateState(isWeekend: false, config);
            // Fatigue increases by 10 (10 -> 20)
            Debug.Assert(Math.Abs(employee.Fatigue - 20f) < 0.001f, $"Fatigue should be 20, got {employee.Fatigue}");
            // Satisfaction decreases towards 80% (100 -> 95)
            Debug.Assert(Math.Abs(employee.Satisfaction - 95f) < 0.001f, $"Satisfaction should be 95, got {employee.Satisfaction}");

            // 2. Weekend update
            employee.UpdateState(isWeekend: true, config);
            // Fatigue decreases by 20 (20 -> 0)
            Debug.Assert(Math.Abs(employee.Fatigue - 0f) < 0.001f, $"Fatigue should be 0, got {employee.Fatigue}");
            // Satisfaction decreases towards 80% (95 -> 90)
            Debug.Assert(Math.Abs(employee.Satisfaction - 90f) < 0.001f, $"Satisfaction should be 90, got {employee.Satisfaction}");

            Console.WriteLine("OK");
        }

        private static void TestEmployeeEfficiencyFormula()
        {
            Console.Write("Test 2: Wzór na efektywność pracownika... ");

            var role = new JobRole("Księgowy", 4000m, Department.Finance);
            
            // Case 1: Satisfaction 80, Fatigue 20
            // Efficiency = (80 / 100) * (1.0 - 20 / 100) = 0.8 * 0.8 = 0.64
            var emp1 = new Employee(Guid.NewGuid(), "Jan Kowalski", role, 4000m, satisfaction: 80f, fatigue: 20f);
            Debug.Assert(Math.Abs(emp1.Efficiency - 0.64f) < 0.001f, $"Efficiency should be 0.64, got {emp1.Efficiency}");

            // Case 2: Satisfaction 50, Fatigue 50
            // Efficiency = (50 / 100) * (1.0 - 50 / 100) = 0.5 * 0.5 = 0.25
            var emp2 = new Employee(Guid.NewGuid(), "Anna Nowak", role, 4000m, satisfaction: 50f, fatigue: 50f);
            Debug.Assert(Math.Abs(emp2.Efficiency - 0.25f) < 0.001f, $"Efficiency should be 0.25, got {emp2.Efficiency}");

            Console.WriteLine("OK");
        }

        private static void TestEmployeeQuittingLogic()
        {
            Console.Write("Test 3: Logika odejścia pracownika (Low Satisfaction)... ");

            var config = new HRConfig
            {
                LowSatisfactionThreshold = 20f,
                MonthsBeforeQuitting = 3
            };

            var role = new JobRole("Księgowy", 4000m, Department.Finance);
            var employee = new Employee(Guid.NewGuid(), "Jan Kowalski", role, 4000m, satisfaction: 15f);

            // Month 1
            employee.CheckQuittingStatus(config);
            Debug.Assert(employee.MonthsWithLowSatisfaction == 1);
            Debug.Assert(!employee.IsPlanningToQuit);

            // Month 2
            employee.CheckQuittingStatus(config);
            Debug.Assert(employee.MonthsWithLowSatisfaction == 2);
            Debug.Assert(!employee.IsPlanningToQuit);

            // Month 3
            employee.CheckQuittingStatus(config);
            Debug.Assert(employee.MonthsWithLowSatisfaction == 3);
            Debug.Assert(employee.IsPlanningToQuit);

            // Salary adjustment, satisfaction raises, check reset
            employee.AdjustSalary(6000m);
            // Simulate state update that pushes satisfaction up above 20
            var tempConfig = new HRConfig { SatisfactionAdjustmentRate = 50f };
            employee.UpdateState(isWeekend: true, tempConfig);
            Debug.Assert(employee.Satisfaction > 20f);

            employee.CheckQuittingStatus(config);
            Debug.Assert(employee.MonthsWithLowSatisfaction == 0);
            Debug.Assert(!employee.IsPlanningToQuit);

            Console.WriteLine("OK");
        }

        private static void TestHRManagerHiringFiring()
        {
            Console.Write("Test 4: Hiring / Firing / Candidate Pool... ");

            var config = new HRConfig();
            var manager = new HRManager(config);

            Debug.Assert(manager.CandidatePool.Count == config.MaxCandidatesCount, $"Candidate pool should have {config.MaxCandidatesCount} items.");

            var candidate = manager.CandidatePool[0];
            manager.HireEmployee(candidate);

            Debug.Assert(manager.Employees.Contains(candidate), "Employee should be hired.");
            Debug.Assert(!manager.CandidatePool.Contains(candidate), "Hired candidate should be removed from candidate pool.");

            manager.FireEmployee(candidate.Id);
            Debug.Assert(!manager.Employees.Contains(candidate), "Employee should be fired.");

            Console.WriteLine("OK");
        }

        private static void TestHRManagerPayrollAndUpdates()
        {
            Console.Write("Test 5: Płatności i Update (Obieg Dni i Miesięcy)... ");

            var config = new HRConfig { CandidatePoolRefreshDays = 3, MaxCandidatesCount = 4 };
            var manager = new HRManager(config);

            var r1 = new JobRole("R1", 1000m, Department.Production);
            var emp1 = new Employee(Guid.NewGuid(), "E1", r1, 1000m);
            var emp2 = new Employee(Guid.NewGuid(), "E2", r1, 2000m);

            manager.HireEmployee(emp1);
            manager.HireEmployee(emp2);

            // 1. Total payroll calculation
            decimal totalPayroll = manager.CalculateTotalMonthlyPayroll();
            Debug.Assert(totalPayroll == 3000m, $"Payroll should be 3000, got {totalPayroll}");

            // 2. Day updates and candidate pool refresh
            manager.Update(1, 0); // Day 1
            manager.Update(2, 0); // Day 2
            
            // Check candidate refresh at day 3 (refresh days is 3)
            manager.Update(3, 0); 
            Debug.Assert(manager.CandidatePool.Count == 4);

            // 3. Event planning to quit
            var quitRole = new JobRole("R2", 10000m, Department.HR);
            var lowSatEmp = new Employee(Guid.NewGuid(), "Low Sat", quitRole, 1000m, satisfaction: 10f);
            manager.HireEmployee(lowSatEmp);

            bool eventTriggered = false;
            manager.OnEmployeePlanningToQuit += (e) =>
            {
                if (e.Id == lowSatEmp.Id) eventTriggered = true;
            };

            // Trigger month end at day 31 (day-1 % 30 == 0)
            manager.Update(31, 0); // Month 1
            Debug.Assert(lowSatEmp.MonthsWithLowSatisfaction == 1);
            Debug.Assert(!eventTriggered);

            manager.Update(61, 0); // Month 2
            Debug.Assert(lowSatEmp.MonthsWithLowSatisfaction == 2);
            Debug.Assert(!eventTriggered);

            manager.Update(91, 0); // Month 3
            Debug.Assert(lowSatEmp.MonthsWithLowSatisfaction == 3);
            Debug.Assert(lowSatEmp.IsPlanningToQuit);
            Debug.Assert(eventTriggered);

            Console.WriteLine("OK");
        }
    }
}
