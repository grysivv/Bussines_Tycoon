using System;
using System.Collections.Generic;
using System.Linq;
using Conglomerate.Logistics;
using Conglomerate.Simulation;
using Conglomerate.Finance;
using Conglomerate.Marketing;
using Conglomerate.HR;

namespace Conglomerate
{
    public class GameManager
    {
        public Company ActiveCompany { get; set; }
        public Map ActiveMap { get; }
        public int CurrentDay { get; private set; } = 1;
        public int CurrentHour { get; private set; } = 8; // Start o godzinie 08:00 rano

        // ── Systemy główne ──────────────────────────────────────
        public FreeMarket    Market     { get; } = new FreeMarket();
        public LogisticsManager Logistics { get; } = new LogisticsManager();
        public AIManager     AIManager  { get; } = new AIManager();
        public HR.HRManager  HR         { get; } = new HR.HRManager(new HR.HRConfig());

        // ── Nowe systemy (Capitalism Lab) ───────────────────────
        public StockMarket  StockMarket  { get; } = new StockMarket();
        public BankingSystem Banking     { get; } = new BankingSystem();

        public event Action? OnTickPerformed;
        public event Action<string>? OnNewsEvent; // Zdarzenia do News Tickera

        public GameManager(Company company, Map map)
        {
            ActiveCompany = company;
            ActiveMap = map;
            InitializeStockMarket();
        }

        // ─────────────────────────────────────────────────────────
        //  Inicjalizacja giełdy
        // ─────────────────────────────────────────────────────────

        private void InitializeStockMarket()
        {
            // Rejestracja firmy gracza
            StockMarket.RegisterCompany(ActiveCompany.Name, 10000m, 200000m);

            // Rejestracja AI firm
            foreach (var ai in AIManager.Competitors)
            {
                StockMarket.RegisterCompany(ai.Name, ai.TotalShares, ai.GetEstimatedNetWorth());
            }
        }

        public void RestoreState(int day, int hour)
        {
            CurrentDay  = day;
            CurrentHour = hour;
        }

        // ─────────────────────────────────────────────────────────
        //  Główna pętla gry
        // ─────────────────────────────────────────────────────────

        public void NextTick()
        {
            CurrentHour++;

            if (CurrentHour >= 24)
            {
                CurrentHour = 0;
                CurrentDay++;

                // ── Aktualizacje dobowe ──────────────────────────
                Market.OnNewDay(CurrentDay);
                AIManager.TickDaily(ActiveMap, CurrentDay, CurrentHour);

                // Kampanie reklamowe gracza
                ProcessAdvertisingCampaigns(ActiveCompany);

                // Brand Awareness Decay
                ActiveCompany.DecayBrandAwareness(0.3f);

                // Aktualizacja giełdy
                UpdateStockMarket();

                var allCompanies = new List<Company> { ActiveCompany };
                allCompanies.AddRange(AIManager.Competitors);

                foreach (var company in allCompanies)
                {
                    // Ekstraktorzy produkują raz na dobę
                    foreach (var building in company.Buildings)
                    {
                        if (building is FactoryBuilding) continue;
                        if (building is RetailBuilding)  continue;

                        decimal oldBalance = company.Balance;
                        building.Produce(company);
                        decimal maintenanceCharged = oldBalance - company.Balance;
                        if (maintenanceCharged > 0)
                        {
                            company.AddTransaction(CurrentDay, CurrentHour,
                                $"Utrzymanie: {building.Name}", -maintenanceCharged, "Utrzymanie", building.FacilityId);
                        }

                        TransferResourcesToWarehouses(building, company);

                        foreach (var key in building.Warehouse.Keys.ToList())
                        {
                            if (building.AutoSellResources.Contains(key) || (building.AutoSell && building.AutoSellResources.Count == 0))
                            {
                                decimal qty = building.GetProductQuantity(key);
                                if (qty > 0)
                                    building.SellResource(key, qty, company, CurrentDay, CurrentHour);
                            }
                        }
                    }

                    // Opłata dzienna za fabryki
                    foreach (var factory in company.Buildings.OfType<FactoryBuilding>())
                    {
                        if (company.Balance >= factory.MaintenanceCost)
                        {
                            company.Balance -= factory.MaintenanceCost;
                            company.AddTransaction(CurrentDay, CurrentHour,
                                $"Utrzymanie: {factory.Name}", -factory.MaintenanceCost, "Utrzymanie", factory.FacilityId);
                        }
                    }

                    // Opłata dzienna za sklepy
                    foreach (var store in company.Buildings.OfType<RetailBuilding>())
                    {
                        if (company.Balance >= store.MaintenanceCost)
                        {
                            company.Balance -= store.MaintenanceCost;
                            company.AddTransaction(CurrentDay, CurrentHour,
                                $"Utrzymanie: {store.Name}", -store.MaintenanceCost, "Utrzymanie", store.FacilityId);
                        }
                    }

                    // Wynagrodzenia dyrektorów (gracza)
                    if (company == ActiveCompany)
                    {
                        ProcessExecutiveSalaries(company);
                    }

                    // Zamknięcie miesiąca
                    if (CurrentDay > 1 && (CurrentDay - 1) % 30 == 0)
                    {
                        company.Engine.CloseMonth(CurrentDay, CurrentHour);

                        // Banking: miesięczne raty kredytów (tylko gracz)
                        if (company == ActiveCompany)
                        {
                            Banking.ProcessMonthlyPayments(company, CurrentDay, CurrentHour);
                            FireMonthlyNewsEvent();
                        }
                    }
                }
            }

            // ── Tick godzinowy ────────────────────────────────────

            // Logistyka
            Logistics.Tick(ActiveCompany, Market, CurrentDay, CurrentHour);

            var hourlyCompanies = new List<Company> { ActiveCompany };
            hourlyCompanies.AddRange(AIManager.Competitors);

            foreach (var company in hourlyCompanies)
            {
                // Fabryki — tick produkcji
                foreach (var factory in company.Buildings.OfType<FactoryBuilding>())
                {
                    decimal oldBalance = company.Balance;
                    bool cycleCompleted = factory.TryAdvanceProduction(company);
                    decimal costCharged = oldBalance - company.Balance;

                    if (costCharged > 0)
                    {
                        company.AddTransaction(CurrentDay, CurrentHour,
                            $"Produkcja: {factory.Name} ({(factory.ActiveRecipe != null ? factory.ActiveRecipe.DisplayName : "")})",
                            -costCharged, "Koszty produkcji", factory.FacilityId);
                    }

                    if (cycleCompleted)
                    {
                        if (factory.ActiveRecipe != null)
                        {
                            foreach (var output in factory.ActiveRecipe.Outputs)
                            {
                                if (factory.AutoSellResources.Contains(output.Key) || (factory.AutoSell && factory.AutoSellResources.Count == 0))
                                {
                                    decimal qty = factory.GetProductQuantity(output.Key);
                                    if (qty > 0)
                                        factory.SellResource(output.Key, qty, company, CurrentDay, CurrentHour);
                                }
                            }
                        }
                        else
                        {
                            TransferResourcesToWarehouses(factory, company);
                        }
                    }
                }

                // Sklepy detaliczne — tick sprzedaży
                foreach (var store in company.Buildings.OfType<RetailBuilding>())
                {
                    // Przekaż Brand Awareness gracza do tick sprzedaży (tylko gracz)
                    if (company == ActiveCompany)
                        store.TickHourlySalesWithBrand(company, CurrentDay, CurrentHour);
                    else
                        store.TickHourlySales(company, CurrentDay, CurrentHour);
                }

                // Centra R&D
                foreach (var rnd in company.Buildings.OfType<RNDCenter>())
                {
                    decimal oldBalance = company.Balance;
                    bool completed = rnd.TryAdvanceResearch(company);
                    decimal costCharged = oldBalance - company.Balance;

                    if (costCharged > 0)
                    {
                        company.AddTransaction(CurrentDay, CurrentHour,
                            $"Badania: {rnd.Name}",
                            -costCharged, "Badania i Rozwój", rnd.FacilityId);
                    }

                    if (completed && company == ActiveCompany)
                    {
                        OnNewsEvent?.Invoke($"🔬 BADANIA: {rnd.Name} ukończyło projekt! Poziom technologii wzrósł.");
                    }
                }

                // Kopalnie węgla (CoalMine) — tick godzinowy
                foreach (var mine in company.Buildings.OfType<CoalMine>())
                {
                    decimal oldBalance = company.Balance;
                    mine.ProduceHourly(company, CurrentDay, CurrentHour);
                    decimal costCharged = oldBalance - company.Balance;
                    if (costCharged > 0)
                    {
                        company.AddTransaction(CurrentDay, CurrentHour,
                            $"Koszty operacyjne: {mine.Name}", -costCharged, "Koszty produkcji", mine.FacilityId);
                    }
                }
            }

            OnTickPerformed?.Invoke();
        }

        // ─────────────────────────────────────────────────────────
        //  Marketing / Reklama
        // ─────────────────────────────────────────────────────────

        private void ProcessAdvertisingCampaigns(Company company)
        {
            // Bonus od CMO
            float cmoMultiplier = 1f;
            var cmo = company.HiredExecutives.FirstOrDefault(e => e.Type == ExecutiveType.CMO);
            if (cmo != null) cmoMultiplier = cmo.BrandAwarenessMultiplier;

            var completedCampaigns = new List<AdvertisingCampaign>();
            foreach (var campaign in company.ActiveCampaigns)
            {
                if (campaign.Tick(company, CurrentDay, CurrentHour))
                {
                    // Zastosuj Brand Awareness growth z bonusem CMO
                    float gain = campaign.DailyAwarenessGain * cmoMultiplier;
                    company.IncreaseBrandAwareness(campaign.ProductName, gain);
                }

                if (!campaign.IsActive)
                    completedCampaigns.Add(campaign);
            }

            foreach (var c in completedCampaigns)
            {
                company.ActiveCampaigns.Remove(c);
                OnNewsEvent?.Invoke($"📢 Kampania reklamowa dla {c.ProductName} zakończona!");
            }
        }

        // ─────────────────────────────────────────────────────────
        //  Giełda
        // ─────────────────────────────────────────────────────────

        private void UpdateStockMarket()
        {
            // Dane gracza
            var playerData = (ActiveCompany.Name, ActiveCompany.GetNetWorth(),
                ActiveCompany.Engine.CalculateCurrentPnL().Revenue,
                ActiveCompany.Engine.CalculateCurrentPnL().NetIncome,
                ActiveCompany.Balance);

            // Dane AI firm
            var allData = new List<(string, decimal, decimal, decimal, decimal)> { playerData };
            allData.AddRange(AIManager.GetStockMarketData());

            StockMarket.OnNewDay(allData);

            // Sprawdź przejęcia
            foreach (var ai in AIManager.Competitors)
            {
                if (StockMarket.HasTakenOver(ai.Name))
                    OnNewsEvent?.Invoke($"🏛️ PRZEJĘCIE: Kontrolujesz {ai.Name}! (>50% udziałów)");
            }
        }

        // ─────────────────────────────────────────────────────────
        //  Wynagrodzenia C-Suite
        // ─────────────────────────────────────────────────────────

        private void ProcessExecutiveSalaries(Company company)
        {
            // Wynagrodzenia płacone raz na miesiąc
            if (CurrentDay > 1 && (CurrentDay - 1) % 30 == 0)
            {
                foreach (var exec in company.HiredExecutives)
                {
                    if (company.Balance >= exec.MonthlySalary)
                    {
                        company.Balance -= exec.MonthlySalary;
                        company.AddTransaction(CurrentDay, CurrentHour,
                            $"Wynagrodzenie: {exec.Name} ({exec.Type})",
                            -exec.MonthlySalary, "Utrzymanie", "");
                    }
                }

                // Bonus CFO: obniżenie stawki podatkowej
                var cfo = company.HiredExecutives.FirstOrDefault(e => e.Type == ExecutiveType.CFO);
                if (cfo != null)
                {
                    decimal reducedTax = Math.Max(0.05m, company.Engine.TaxRate - (decimal)cfo.TaxRateReduction);
                    company.Engine.TaxRate = reducedTax;
                }
            }
        }

        // ─────────────────────────────────────────────────────────
        //  Eventy newsowe
        // ─────────────────────────────────────────────────────────

        private void FireMonthlyNewsEvent()
        {
            string[] templates = {
                "📈 RYNEK: Analitycy prognozują wzrost popytu na elektronikę o 8% w Q4.",
                "💰 FINANSE: Stopy procentowe NBP pozostają bez zmian. Kredyty tańsze!",
                "🏭 PRZEMYSŁ: Ceny stali rosną w związku z ograniczeniami dostaw rudy żelaza.",
                "🛒 HANDEL: Sezon świąteczny napędza sprzedaż detaliczną. Zwiększ zapasy!",
                "⚙️ TECHNOLOGIA: Nowe innowacje w elektronice obniżają koszty produkcji.",
                "🌾 ROLNICTWO: Rekordowe plony mleka wpłyną na ceny produktów mlecznych."
            };
            var rng = new Random(CurrentDay);
            OnNewsEvent?.Invoke(templates[rng.Next(templates.Length)]);
        }

        // ─────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────

        private void TransferResourcesToWarehouses(Building source, Company company)
        {
            if (source is WarehouseBuilding) return;
            if (source is RetailBuilding)    return;

            foreach (var resourceKey in source.Warehouse.Keys.ToList())
            {
                decimal quantity = source.GetProductQuantity(resourceKey);
                if (quantity <= 0) continue;

                var category = ResourceRegistry.GetCategory(resourceKey);
                var warehouses = company.Buildings
                    .OfType<WarehouseBuilding>()
                    .Where(w => w.AllowedCategory == category)
                    .ToList();

                foreach (var wh in warehouses)
                {
                    decimal freeSpace = wh.WarehouseCapacity - wh.GetTotalStock();
                    if (freeSpace <= 0) continue;

                    decimal amountToTransfer = Math.Min(quantity, freeSpace);
                    if (amountToTransfer > 0)
                    {
                        source.RemoveProduct(resourceKey, amountToTransfer);
                        wh.AddProduct(resourceKey, amountToTransfer);
                        quantity -= amountToTransfer;
                        if (quantity <= 0) break;
                    }
                }
            }
        }
    }
}
