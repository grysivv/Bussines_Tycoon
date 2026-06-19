using System;
using System.Diagnostics;
using Conglomerate;
using Conglomerate.HR;

namespace Conglomerate.Tests
{
    public static class CoalMineTests
    {
        public static void RunTests()
        {
            Console.WriteLine("=== URUCHAMIANIE TESTÓW SYSTEMU PRODUKCJI WĘGLA (COALMINE) ===");

            TestHourlyProductionRate();
            TestWeeklyStorageBottleneck();
            TestCoalSalesPrice();
            TestEmployeeAndLevelScaling();

            Console.WriteLine("=== WSZYSTKIE TESTY PRODUKCJI WĘGLA ZAKOŃCZONE SUKCESEM! ===");
        }

        private static void TestHourlyProductionRate()
        {
            Console.Write("Test 1: Cogodzinna skala produkcji (~228.31 t/h zaadaptowane do poziomu i zatrudnienia)... ");

            var company = new Company("TestCorp", 1000000m);
            var mine = new CoalMine("Kopalnia A");
            company.Buildings.Add(mine);

            // Domyślnie poziom 1 i 1200 pracowników: 1200 * 0.085 = 102 tony/godzinę
            bool produced = mine.ProduceHourly(company, day: 1, hour: 9);

            Debug.Assert(produced, "Production should succeed.");
            decimal coalStock = mine.GetProductQuantity("Węgiel");
            Debug.Assert(coalStock == 102, $"Expected 102 tons after 1 hour, got {coalStock}");
            Debug.Assert(Math.Abs(mine.PreciseStorage - 102.0) < 0.0001, $"Expected precise storage ~102.0, got {mine.PreciseStorage}");

            Console.WriteLine("OK");
        }

        private static void TestWeeklyStorageBottleneck()
        {
            Console.Write("Test 2: Wąskie gardło magazynu (zatrzymanie po 1 tygodniu / 168h)... ");

            // Zwiększamy budżet firmy, by opłacić 168h pensji górników
            var company = new Company("TestCorp", 40000000m);
            
            // Konfigurujemy poziom 2 i 2239 pracowników, by uzyskać ~228.378 t/h, co zapełni magazyn (38356.0) w 168h
            var mine = new CoalMine("Kopalnia A")
            {
                Level = 2,
                CurrentEmployees = 2239
            };
            company.Buildings.Add(mine);

            bool haltEventTriggered = false;
            mine.OnProductionHalted += (sender, e) =>
            {
                haltEventTriggered = true;
            };

            // Simulate 168 hours of production
            for (int h = 0; h < 168; h++)
            {
                mine.ProduceHourly(company, day: 1 + (h / 24), hour: h % 24);
            }

            // At 168 hours: 228.378 * 168 = 38367.504 -> capped at MaxStorageCapacity (38356.0)
            Debug.Assert(Math.Abs(mine.PreciseStorage - 38356.0) < 0.001, $"Expected capped storage at 38356.0, got {mine.PreciseStorage}");
            decimal coalStock = mine.GetProductQuantity("Węgiel");
            Debug.Assert(coalStock == 38356m, $"Expected integer warehouse stock 38356, got {coalStock}");

            // The 169th hour should trigger halt
            bool producedAfterHalt = mine.ProduceHourly(company, day: 8, hour: 1);
            Debug.Assert(!producedAfterHalt, "Production should halt when storage is full.");
            Debug.Assert(haltEventTriggered, "Halt event should have been triggered.");

            Console.WriteLine("OK");
        }

        private static void TestCoalSalesPrice()
        {
            Console.Write("Test 3: Bilans finansowy i cena sprzedaży (400 PLN / t)... ");

            var company = new Company("TestCorp", 2000000m);
            var mine = new CoalMine("Kopalnia A");
            company.Buildings.Add(mine);

            // Produce 1 hour to get 102 tons (Level 1, 1200 employees)
            mine.ProduceHourly(company, day: 1, hour: 9);

            decimal initialBalance = company.Balance;
            // Sell 50 tons of coal at 400 PLN / ton
            bool sold = mine.SellResource("Węgiel", 50, company, day: 1, hour: 10);

            Debug.Assert(sold, "Selling coal should succeed.");
            decimal expectedRevenue = 50 * 400m;
            decimal balanceDiff = company.Balance - initialBalance;
            Debug.Assert(balanceDiff == expectedRevenue, $"Expected revenue {expectedRevenue}, got {balanceDiff}");
            Debug.Assert(mine.GetProductQuantity("Węgiel") == 52m, $"Expected remaining warehouse stock 52, got {mine.GetProductQuantity("Węgiel")}");

            // Verify precise storage synced down on next production tick
            mine.ProduceHourly(company, day: 1, hour: 11);
            // Before tick: synced down to 52. After tick: 52 + 102 = 154
            Debug.Assert(Math.Abs(mine.PreciseStorage - 154.0) < 0.001, $"Expected precise storage synced and increased to ~154.0, got {mine.PreciseStorage}");

            Console.WriteLine("OK");
        }

        private static void TestEmployeeAndLevelScaling()
        {
            Console.Write("Test 4: Skalowanie poziomów, pracowników i technologii... ");

            var company = new Company("TestCorp", 1000000m);
            var mine = new CoalMine("Kopalnia A");
            company.Buildings.Add(mine);

            // Level 1: Max employees = 1200
            Debug.Assert(mine.MaxEmployees == 1200, "Level 1 max employees should be 1200");
            mine.CurrentEmployees = 1500; // clamp to 1200
            Debug.Assert(mine.CurrentEmployees == 1200, "Current employees should clamp to max");

            // Level 2: Max employees = 3000
            mine.Level = 2;
            Debug.Assert(mine.MaxEmployees == 3000, "Level 2 max employees should be 3000");
            mine.CurrentEmployees = 2500;
            Debug.Assert(mine.CurrentEmployees == 2500, "Current employees should be 2500");

            // Technology multiplier
            mine.TechnologyMultiplier = 1.5f;
            // Production: 2500 * 0.085 * 1.2 * 1.5 = 382.5 tons/hour
            mine.ProduceHourly(company, 1, 9);
            Debug.Assert(Math.Abs(mine.PreciseStorage - 382.5) < 0.001, $"Expected 382.5, got {mine.PreciseStorage}");

            Console.WriteLine("OK");
        }
    }
}
