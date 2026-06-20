using System;
using System.Linq;
using Conglomerate.HR;
using Xunit;

namespace Conglomerate.Tests
{
    public class HRSystemTests
    {
        [Fact]
        public void TestEmployeeStateMutation()
        {
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
            Assert.True(Math.Abs(employee.Fatigue - 20f) < 0.001f, $"Fatigue should be 20, got {employee.Fatigue}");
            // Satisfaction decreases towards 80% (100 -> 95)
            Assert.True(Math.Abs(employee.Satisfaction - 95f) < 0.001f, $"Satisfaction should be 95, got {employee.Satisfaction}");

            // 2. Weekend update
            employee.UpdateState(isWeekend: true, config);
            // Fatigue decreases by 20 (20 -> 0)
            Assert.True(Math.Abs(employee.Fatigue - 0f) < 0.001f, $"Fatigue should be 0, got {employee.Fatigue}");
            // Satisfaction decreases towards 80% (95 -> 90)
            Assert.True(Math.Abs(employee.Satisfaction - 90f) < 0.001f, $"Satisfaction should be 90, got {employee.Satisfaction}");
        }

        [Fact]
        public void TestEmployeeEfficiencyFormula()
        {
            var role = new JobRole("Księgowy", 4000m, Department.Finance);

            // Case 1: Satisfaction 80, Fatigue 20
            // Efficiency = (80 / 100) * (1.0 - 20 / 100) = 0.8 * 0.8 = 0.64
            var emp1 = new Employee(Guid.NewGuid(), "Jan Kowalski", role, 4000m, satisfaction: 80f, fatigue: 20f);
            Assert.True(Math.Abs(emp1.Efficiency - 0.64f) < 0.001f, $"Efficiency should be 0.64, got {emp1.Efficiency}");

            // Case 2: Satisfaction 50, Fatigue 50
            // Efficiency = (50 / 100) * (1.0 - 50 / 100) = 0.5 * 0.5 = 0.25
            var emp2 = new Employee(Guid.NewGuid(), "Anna Nowak", role, 4000m, satisfaction: 50f, fatigue: 50f);
            Assert.True(Math.Abs(emp2.Efficiency - 0.25f) < 0.001f, $"Efficiency should be 0.25, got {emp2.Efficiency}");
        }

        [Fact]
        public void TestEmployeeQuittingLogic()
        {
            var config = new HRConfig
            {
                LowSatisfactionThreshold = 20f,
                MonthsBeforeQuitting = 3
            };

            var role = new JobRole("Księgowy", 4000m, Department.Finance);
            var employee = new Employee(Guid.NewGuid(), "Jan Kowalski", role, 4000m, satisfaction: 15f);

            // Month 1
            employee.CheckQuittingStatus(config);
            Assert.True(employee.MonthsWithLowSatisfaction == 1);
            Assert.False(employee.IsPlanningToQuit);

            // Month 2
            employee.CheckQuittingStatus(config);
            Assert.True(employee.MonthsWithLowSatisfaction == 2);
            Assert.False(employee.IsPlanningToQuit);

            // Month 3
            employee.CheckQuittingStatus(config);
            Assert.True(employee.MonthsWithLowSatisfaction == 3);
            Assert.True(employee.IsPlanningToQuit);

            // Salary adjustment, satisfaction raises, check reset
            employee.AdjustSalary(6000m);
            // Simulate state update that pushes satisfaction up above 20
            var tempConfig = new HRConfig { SatisfactionAdjustmentRate = 50f };
            employee.UpdateState(isWeekend: true, tempConfig);
            Assert.True(employee.Satisfaction > 20f);

            employee.CheckQuittingStatus(config);
            Assert.True(employee.MonthsWithLowSatisfaction == 0);
            Assert.False(employee.IsPlanningToQuit);
        }

        [Fact]
        public void TestHRManagerHiringFiring()
        {
            var config = new HRConfig();
            var manager = new HRManager(config);

            Assert.True(manager.CandidatePool.Count == config.MaxCandidatesCount, $"Candidate pool should have {config.MaxCandidatesCount} items.");

            var candidate = manager.CandidatePool[0];
            manager.HireEmployee(candidate);

            Assert.True(manager.Employees.Contains(candidate), "Employee should be hired.");
            Assert.False(manager.CandidatePool.Contains(candidate), "Hired candidate should be removed from candidate pool.");

            manager.FireEmployee(candidate.Id);
            Assert.False(manager.Employees.Contains(candidate), "Employee should be fired.");
        }

        [Fact]
        public void TestHRManagerPayrollAndUpdates()
        {
            var config = new HRConfig { CandidatePoolRefreshDays = 3, MaxCandidatesCount = 4 };
            var manager = new HRManager(config);

            var r1 = new JobRole("R1", 1000m, Department.Production);
            var emp1 = new Employee(Guid.NewGuid(), "E1", r1, 1000m);
            var emp2 = new Employee(Guid.NewGuid(), "E2", r1, 2000m);

            manager.HireEmployee(emp1);
            manager.HireEmployee(emp2);

            // 1. Total payroll calculation
            decimal totalPayroll = manager.CalculateTotalMonthlyPayroll();
            Assert.True(totalPayroll == 3000m, $"Payroll should be 3000, got {totalPayroll}");

            // 2. Day updates and candidate pool refresh
            manager.Update(1, 0); // Day 1
            manager.Update(2, 0); // Day 2

            // Check candidate refresh at day 3 (refresh days is 3)
            manager.Update(3, 0);
            Assert.True(manager.CandidatePool.Count == 4);

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
            Assert.True(lowSatEmp.MonthsWithLowSatisfaction == 1);
            Assert.False(eventTriggered);

            manager.Update(61, 0); // Month 2
            Assert.True(lowSatEmp.MonthsWithLowSatisfaction == 2);
            Assert.False(eventTriggered);

            manager.Update(91, 0); // Month 3
            Assert.True(lowSatEmp.MonthsWithLowSatisfaction == 3);
            Assert.True(lowSatEmp.IsPlanningToQuit);
            Assert.True(eventTriggered);
        }
    }
}
