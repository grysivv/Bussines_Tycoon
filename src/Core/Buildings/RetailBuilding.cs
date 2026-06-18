using System;
using System.Collections.Generic;
using System.Linq;
using Conglomerate.Retail;

namespace Conglomerate
{
    /// <summary>
    /// Abstrakcyjna klasa bazowa dla wszystkich budynków detalicznych (sklepy).
    ///
    /// Architektura:
    ///   RetailBuilding (tu) — zarządza slotami i finansami
    ///   RetailDemandEngine  — oblicza popyt (całkowicie oddzielony)
    ///   SalesSlot           — stan pojedynczej półki
    ///
    /// Skalowalność: wystarczy stworzyć nową klasę dziedziczącą (np. ElectronicsStore)
    /// i nadpisać MaxSlots, LocationFactor, BuildCost — logika sprzedaży działa automatycznie.
    /// </summary>
    public abstract class RetailBuilding : Building
    {
        private readonly Random _rng;

        // ──────────────────────────────────────────────
        //  Konfiguracja (nadpisywana w podklasach)
        // ──────────────────────────────────────────────

        /// <summary>Maksymalna liczba aktywnych slotów sprzedażowych.</summary>
        public abstract int MaxSlots { get; }

        /// <summary>
        /// Czynnik lokalizacji (0.0–2.0).
        /// 1.0 = standardowa lokalizacja, 2.0 = centrum, 0.5 = peryferia.
        /// W przyszłości może być wyliczany na podstawie pozycji na mapie.
        /// </summary>
        public virtual float LocationFactor { get; set; } = 1.0f;

        // ──────────────────────────────────────────────
        //  Półki (Slots)
        // ──────────────────────────────────────────────

        /// <summary>Aktywne sloty sprzedażowe. Indeksy 0 do MaxSlots-1.</summary>
        public List<SalesSlot> Slots { get; } = new List<SalesSlot>();

        // ──────────────────────────────────────────────
        //  Statystyki 24h dla całego sklepu
        // ──────────────────────────────────────────────

        public int TotalUnitsSoldLast24h  => Slots.Sum(s => s.UnitsSoldLast24h);
        public decimal TotalRevenueLast24h => Slots.Sum(s => s.RevenueLast24h);

        // ──────────────────────────────────────────────
        //  Konstruktor
        // ──────────────────────────────────────────────

        protected RetailBuilding(string name, int seed = 0) : base(name)
        {
            _rng = seed != 0 ? new Random(seed) : new Random();

            // Inicjalizuj puste sloty
            for (int i = 0; i < MaxSlots; i++)
            {
                Slots.Add(new SalesSlot
                {
                    SlotIndex      = i,
                    ProductName    = string.Empty,
                    CurrentStock   = 0,
                    ShelfCapacity  = 50,
                    PriceMultiplier = 1.5m
                });
            }
        }

        // ──────────────────────────────────────────────
        //  Zarządzanie slotami
        // ──────────────────────────────────────────────

        /// <summary>
        /// Przypisuje produkt do slotu. Zeruje stock i historię.
        /// Zwraca false jeśli slot jest poza zakresem lub już zajęty innym produktem.
        /// </summary>
        public bool AssignProduct(int slotIndex, string productName, decimal priceMultiplier = 1.5m)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlots) return false;

            var slot = Slots[slotIndex];
            
            if (slot.ProductName != productName)
            {
                if (!string.IsNullOrEmpty(slot.ProductName) && slot.CurrentStock > 0)
                {
                    if (!Warehouse.ContainsKey(slot.ProductName)) Warehouse[slot.ProductName] = 0;
                    Warehouse[slot.ProductName] += slot.CurrentStock;
                }
                slot.CurrentStock = 0;
                slot.LastAttractiveness = 0f;
                slot.DirectRetailPrice = 0m;
            }
            
            slot.ProductName = productName;
            slot.PriceMultiplier = priceMultiplier;

            return true;
        }

        /// <summary>
        /// Usuwa przypisanie produktu ze slotu i zwraca pozostały stock na półce do magazynu budynku.
        /// </summary>
        public bool ClearSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlots) return false;

            var slot = Slots[slotIndex];
            if (slot.IsActive)
            {
                if (slot.CurrentStock > 0)
                {
                    if (!Warehouse.ContainsKey(slot.ProductName)) Warehouse[slot.ProductName] = 0;
                    Warehouse[slot.ProductName] += slot.CurrentStock;
                }
                slot.ProductName = string.Empty;
                slot.CurrentStock = 0;
                slot.LastAttractiveness = 0f;
                slot.DirectRetailPrice = 0m;
            }
            return true;
        }

        /// <summary>
        /// Uzupełnia towary na półkach (Slots) z magazynu wewnętrznego (Warehouse).
        /// </summary>
        public void RestockAllSlots()
        {
            foreach (var slot in Slots)
            {
                if (!slot.IsActive) continue;

                int needed = slot.ShelfCapacity - slot.CurrentStock;
                if (needed <= 0) continue;

                if (Warehouse.TryGetValue(slot.ProductName, out int inWarehouse) && inWarehouse > 0)
                {
                    int toTransfer = Math.Min(needed, inWarehouse);
                    if (toTransfer > 0)
                    {
                        slot.CurrentStock += toTransfer;
                        Warehouse[slot.ProductName] -= toTransfer;
                    }
                }
            }
        }



        // ──────────────────────────────────────────────
        //  Główny tick sprzedaży — wywoływany co godzinę
        // ──────────────────────────────────────────────

        /// <summary>
        /// Wykonuje godzinowy cykl sprzedaży dla wszystkich aktywnych slotów.
        /// Deducts stock, records revenue, registers transactions in Company.
        /// </summary>
        public void TickHourlySales(Company company, int day, int hour)
        {
            // Najpierw uzupełnij półki z wewnętrznego magazynu
            RestockAllSlots();

            foreach (var slot in Slots)
            {
                if (!slot.IsActive) continue;

                decimal baseMarketPrice = ResourceRegistry.GetPrice(slot.ProductName);
                decimal retailPrice     = slot.GetEffectivePrice(baseMarketPrice);

                // Oblicz atrakcyjność (oddzielony silnik)
                float attractiveness = RetailDemandEngine.CalculateAttractiveness(
                    slot.ProductName,
                    retailPrice,
                    baseMarketPrice,
                    LocationFactor);

                slot.LastAttractiveness = attractiveness;

                // Oblicz sprzedaż godzinową
                int unitsSold = RetailDemandEngine.CalculateHourlySales(slot, attractiveness, _rng);
                bool wasStockout = slot.IsStockout;

                if (unitsSold > 0)
                {
                    decimal revenue = retailPrice * unitsSold;

                    // Odejmij ze stanu na półce
                    slot.CurrentStock -= unitsSold;

                    // Zarejestruj transakcję finansową
                    company.Balance += revenue;
                    company.AddTransaction(day, hour,
                        $"Sprzedaż: {unitsSold}x {slot.ProductName} @ {retailPrice:C} [{Name}]",
                        revenue,
                        "Sprzedaż detaliczna",
                        FacilityId);
                }

                // Zarejestruj w historii 24h
                slot.RecordHourlySale(unitsSold, unitsSold > 0 ? retailPrice * unitsSold : 0m, wasStockout);
            }
        }

        // ──────────────────────────────────────────────
        //  Building overrides (sklep nie produkuje — brak Produce)
        // ──────────────────────────────────────────────

        public override string ActivityType => "Handel Detaliczny";

        public override Dictionary<string, decimal> ResourcePrices =>
            Slots
                .Where(s => s.IsActive)
                .ToDictionary(s => s.ProductName, s => s.GetEffectivePrice(ResourceRegistry.GetPrice(s.ProductName)));

        /// <summary>
        /// Sklep detaliczny nie produkuje surowców — ta metoda pobiera tylko koszty utrzymania.
        /// Właściwa sprzedaż odbywa się w TickHourlySales().
        /// </summary>
        public override bool Produce(Company company)
        {
            // Koszty utrzymania obsługiwane są przez GameManager w pętli dziennej
            return false;
        }
    }
}
