using System;
using System.Diagnostics;
using System.Linq;

namespace Conglomerate.Financials.Tests
{
    public class MockFacility : IFacilitySegment
    {
        public string FacilityId { get; }
        public string Name { get; }
        public decimal PropertyPurchasePrice { get; }
        public decimal DepreciationRate { get; }
        public decimal AccumulatedDepreciation { get; set; }
        public decimal PropertyBookValue => PropertyPurchasePrice - AccumulatedDepreciation;
        public decimal InventoryValue { get; set; }

        public MockFacility(string id, string name, decimal purchasePrice, decimal depRate, decimal inventoryVal = 0m)
        {
            FacilityId = id;
            Name = name;
            PropertyPurchasePrice = purchasePrice;
            DepreciationRate = depRate;
            AccumulatedDepreciation = 0m;
            InventoryValue = inventoryVal;
        }
    }

    public static class FinancialSystemTests
    {
        public static void RunTests()
        {
            Console.WriteLine("=== URUCHAMIANIE TESTÓW SYSTEMU FINANSOWEGO ===");

            TestPnLCalculations();
            TestBalanceSheetEquation();
            TestHistoryAndQuarterlyRoll();

            Console.WriteLine("=== WSZYSTKIE TESTY ZAKOŃCZONE SUKCESEM! ===");
        }

        private static void TestPnLCalculations()
        {
            Console.Write("Test 1: Kalkulacja P&L i Amortyzacja... ");

            // Inicjalizacja silnika finansowego
            var engine = new FinancialEngine(startingCash: 100000m, startingShareCapital: 100000m);

            // Zakup i rejestracja placówki z amortyzacją 10% rocznie (120,000 / 10% = 12,000/rok = 1,000/miesiąc)
            var factory = new MockFacility("FAC_01", "Fabryka A", 120000m, 0.10m);
            engine.BuyFacility(factory);

            // Transakcje bieżącego miesiąca
            engine.RecordTransaction(1, 12, 50000m, FinancialCategory.Revenue, "Przychód ze sprzedaży");
            engine.RecordTransaction(5, 8, -10000m, FinancialCategory.RawMaterials, "Zakup stali (COGS)");
            engine.RecordTransaction(10, 16, -8000m, FinancialCategory.Salaries, "Płace pracowników");
            engine.RecordTransaction(15, 10, -3000m, FinancialCategory.Logistics, "Transport towarów");
            engine.RecordTransaction(20, 14, -4000m, FinancialCategory.Marketing, "Kampania reklamowa");

            // Przed zamknięciem miesiąca - kalkulacja P&L (z szacowaną amortyzacją)
            var pnlMid = engine.CalculateCurrentPnL();
            
            Debug.Assert(pnlMid.Revenue == 50000m, $"Revenue: oczekiwano 50000, otrzymano {pnlMid.Revenue}");
            Debug.Assert(pnlMid.RawMaterials == -10000m, $"COGS: oczekiwano -10000, otrzymano {pnlMid.RawMaterials}");
            Debug.Assert(pnlMid.GrossProfit == 40000m, $"GrossProfit: oczekiwano 40000, otrzymano {pnlMid.GrossProfit}");
            Debug.Assert(pnlMid.EBITDA == 25000m, $"EBITDA: oczekiwano 25000, otrzymano {pnlMid.EBITDA}");
            Debug.Assert(pnlMid.Depreciation == -1000m, $"Depreciation: oczekiwano -1000, otrzymano {pnlMid.Depreciation}");
            Debug.Assert(pnlMid.EBIT == 24000m, $"EBIT: oczekiwano 24000, otrzymano {pnlMid.EBIT}");

            // Zamknięcie miesiąca (powinno naliczyć CIT 19% z 24000 = 4560 i amortyzację)
            engine.CloseMonth(30, 23);

            // Po zamknięciu miesiąca (poczatek nowego miesiąca 2)
            Debug.Assert(engine.CurrentMonthIndex == 2, "CurrentMonthIndex powinien wynosić 2");
            Debug.Assert(engine.MonthlyHistory.Count == 1, "Historia powinna zawierać 1 wpis");

            var archivedSnap = engine.MonthlyHistory[0];
            Debug.Assert(archivedSnap.PnL.CorporateTax == -4560m, $"Tax: oczekiwano -4560, otrzymano {archivedSnap.PnL.CorporateTax}");
            Debug.Assert(archivedSnap.PnL.NetIncome == 19440m, $"NetIncome: oczekiwano 19440, otrzymano {archivedSnap.PnL.NetIncome}");
            Debug.Assert(engine.RetainedEarnings == 19440m, $"RetainedEarnings: oczekiwano 19440, otrzymano {engine.RetainedEarnings}");

            Console.WriteLine("OK");
        }

        private static void TestBalanceSheetEquation()
        {
            Console.Write("Test 2: Równanie Bilansowe (Assets == Liabilities + Equity)... ");

            // 1. Inicjalizacja
            var engine = new FinancialEngine(startingCash: 100000m, startingShareCapital: 100000m);
            var bs1 = engine.CalculateCurrentBalanceSheet();
            Debug.Assert(bs1.IsBalanced, "Początkowy bilans musi być zrównoważony");
            Debug.Assert(bs1.TotalAssets == 100000m);

            // 2. Zaciągnięcie kredytu (Liabilities +50k)
            engine.AdjustLoans(50000m);
            var bs2 = engine.CalculateCurrentBalanceSheet();
            Debug.Assert(bs2.IsBalanced, "Bilans po kredycie musi być zrównoważony");
            Debug.Assert(bs2.Loans == 50000m);
            Debug.Assert(bs2.Cash == 150000m);
            Debug.Assert(bs2.TotalAssets == 150000m);

            // 3. Zakup fabryki za gotówkę (Cash -120k, PropertyBookValue +120k)
            var factory = new MockFacility("FAC_02", "Fabryka B", 120000m, 0.10m);
            engine.BuyFacility(factory);
            
            var bs3 = engine.CalculateCurrentBalanceSheet();
            Debug.Assert(bs3.IsBalanced, "Bilans po zakupie fabryki musi być zrównoważony");
            Debug.Assert(bs3.Cash == 30000m);
            Debug.Assert(bs3.PropertyBookValue == 120000m);
            Debug.Assert(bs3.TotalAssets == 150000m);

            // 4. Cykl operacyjny i zamknięcie miesiąca
            // Przychód
            engine.RecordTransaction(10, 12, 50000m, FinancialCategory.Revenue, "Sprzedaż");
            // Koszty operacyjne
            engine.RecordTransaction(15, 8, -5000m, FinancialCategory.Salaries, "Płace");
            
            // Zamknięcie (Depreciation = 120000 * 10% / 12 = 1000)
            // EBITDA = 50000 - 5000 = 45000
            // EBIT = EBITDA + Depreciation = 45000 - 1000 = 44000. CIT = 44000 * 0.19 = 8360. NetIncome = 35640
            engine.CloseMonth(30, 23);

            var bs4 = engine.CalculateCurrentBalanceSheet();
            Debug.Assert(bs4.IsBalanced, "Bilans po zamknięciu miesiąca musi być zrównoważony");
            Debug.Assert(bs4.RetainedEarnings == 35640m, $"RetainedEarnings: oczekiwano 35640, otrzymano {bs4.RetainedEarnings}");
            Debug.Assert(bs4.PropertyBookValue == 119000m, $"PropertyBookValue: oczekiwano 119000, otrzymano {bs4.PropertyBookValue}");
            Debug.Assert(bs4.Cash == 66640m, $"Cash: oczekiwano 66640, otrzymano {bs4.Cash}"); // 30k starting + 50k sales - 5k salaries - 8.36k tax = 66.64k

            Console.WriteLine("OK");
        }

        private static void TestHistoryAndQuarterlyRoll()
        {
            Console.Write("Test 3: System Historyczny i Agregacja Kwartalna... ");

            var engine = new FinancialEngine(startingCash: 100000m, startingShareCapital: 100000m);
            var factory = new MockFacility("FAC_03", "Fabryka C", 60000m, 0.10m);
            engine.BuyFacility(factory);

            // Symulacja 6 miesięcy stałych operacji (miesiące 1 - 6)
            for (int m = 1; m <= 6; m++)
            {
                engine.RecordTransaction(15, 12, 20000m, FinancialCategory.Revenue, $"Sprzedaż M{m}");
                engine.RecordTransaction(20, 10, -5000m, FinancialCategory.Salaries, $"Płace M{m}");
                
                // EBITDA = 20000 - 5000 = 15000
                // Monthly Depreciation = 60000 * 0.10 / 12 = 500
                // EBIT = 14500. Corporate Tax (19%) = 2755. Net Income = 11745.
                engine.CloseMonth(30, 23);
            }

            Debug.Assert(engine.MonthlyHistory.Count == 6, $"Miesięczna historia: {engine.MonthlyHistory.Count}");
            Debug.Assert(engine.QuarterlyHistory.Count == 2, $"Kwartalna historia: {engine.QuarterlyHistory.Count}");

            // Sprawdzenie wartości pierwszego kwartału (Miesiące 1, 2, 3)
            var q1 = engine.QuarterlyHistory[0];
            Debug.Assert(q1.PeriodName == "Kwartał 1");
            Debug.Assert(q1.PnL.Revenue == 60000m, $"Q1 Revenue: {q1.PnL.Revenue}");
            Debug.Assert(q1.PnL.Salaries == -15000m, $"Q1 Salaries: {q1.PnL.Salaries}");
            Debug.Assert(q1.PnL.Depreciation == -1500m, $"Q1 Depreciation: {q1.PnL.Depreciation}");
            Debug.Assert(q1.PnL.CorporateTax == -8265m, $"Q1 Tax: {q1.PnL.CorporateTax}"); // 2755 * 3 = 8265
            Debug.Assert(q1.PnL.NetIncome == 35235m, $"Q1 NetIncome: {q1.PnL.NetIncome}"); // 11745 * 3 = 35235

            // Sprawdzenie FIFO (np. gdybyśmy mieli ponad limit 24 miesięcy)
            // Symulacja dodatkowych 20 miesięcy (razem 26 miesięcy)
            for (int m = 7; m <= 26; m++)
            {
                engine.RecordTransaction(15, 12, 1000m, FinancialCategory.Revenue, "Minimalny obrót");
                engine.CloseMonth(30, 23);
            }

            Debug.Assert(engine.MonthlyHistory.Count == 24, "Miesięczna historia powinna być obcięta do maksymalnie 24 miesięcy");
            // Pierwszy zachowany element to miesiąc 3 (indeks okresu = 3) bo miesiące 1 i 2 zostały usunięte
            Debug.Assert(engine.MonthlyHistory.First().PeriodIndex == 3, $"Pierwszy okres w historii: {engine.MonthlyHistory.First().PeriodIndex}");

            Console.WriteLine("OK");
        }
    }
}
