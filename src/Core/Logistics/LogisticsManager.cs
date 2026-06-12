using System;
using System.Collections.Generic;
using System.Linq;

namespace Conglomerate.Logistics
{
    /// <summary>
    /// Zarządza wszystkimi trasami logistycznymi w grze.
    /// Wywoływany przez GameManager co każdy tick (1 godzina).
    /// 
    /// Odpowiedzialności:
    ///   - Przechowywanie listy tras
    ///   - Naliczanie czasu od ostatniego tripu
    ///   - Wykonanie transferu surowców gdy interwał minął
    ///   - Rejestracja transakcji finansowych
    /// </summary>
    public class LogisticsManager
    {
        // ──────────────────────────────────────────────
        //  Stan
        // ──────────────────────────────────────────────

        /// <summary>Wszystkie aktywne trasy logistyczne.</summary>
        public List<SupplyRoute> Routes { get; } = new List<SupplyRoute>();

        // ──────────────────────────────────────────────
        //  API dla UI
        // ──────────────────────────────────────────────

        public void AddRoute(SupplyRoute route) => Routes.Add(route);

        public void RemoveRoute(string routeId) =>
            Routes.RemoveAll(r => r.Id == routeId);

        /// <summary>Zwraca trasy, których celem jest dany budynek (FacilityId).</summary>
        public List<SupplyRoute> GetRoutesForTarget(string targetFacilityId) =>
            Routes.Where(r => r.TargetFacilityId == targetFacilityId).ToList();

        /// <summary>Zwraca trasy, których źródłem jest dany budynek (FacilityId).</summary>
        public List<SupplyRoute> GetRoutesFromSource(string sourceFacilityId) =>
            Routes.Where(r => r.SourceFacilityId == sourceFacilityId).ToList();

        // ──────────────────────────────────────────────
        //  Silnik logistyczny — wywoływany co godzinę
        // ──────────────────────────────────────────────

        /// <summary>
        /// Główna metoda ticku — sprawdza i wykonuje trasy, których interwał minął.
        /// </summary>
        public void Tick(
            Company company,
            FreeMarket market,
            int day, int hour)
        {
            // Odbuduj mapę FacilityId → Building dla szybkiego wyszukiwania
            var buildingMap = company.Buildings.ToDictionary(b => b.FacilityId, b => b);

            foreach (var route in Routes)
            {
                if (!route.IsEnabled) continue;

                route.HoursSinceLastTrip++;

                if (route.HoursSinceLastTrip < route.IntervalHours)
                    continue;

                // Czas na wyzwolenie trasy
                ExecuteRoute(route, company, market, buildingMap, day, hour);
                route.HoursSinceLastTrip = 0;
            }
        }

        // ──────────────────────────────────────────────
        //  Wykonanie pojedynczej trasy
        // ──────────────────────────────────────────────

        private void ExecuteRoute(
            SupplyRoute route,
            Company company,
            FreeMarket market,
            Dictionary<string, Building> buildingMap,
            int day, int hour)
        {
            // 1. Znajdź budynek docelowy
            if (!buildingMap.TryGetValue(route.TargetFacilityId, out var target))
            {
                route.LastTripResult = "❌ Budynek docelowy nie istnieje";
                return;
            }

            // 2. Sprawdź miejsce w magazynie celu
            int freeSpace = target.WarehouseCapacity - target.GetTotalStock();
            if (freeSpace <= 0)
            {
                route.LastTripResult = "⏸ Magazyn docelowy pełny";
                return;
            }

            int amountToTransfer = Math.Min(route.AmountPerTrip, freeSpace);

            // 3. Pobierz surowce ze źródła
            if (route.SourceType == RouteSourceType.Market)
            {
                ExecuteMarketRoute(route, company, market, target, amountToTransfer, day, hour);
            }
            else
            {
                ExecuteBuildingRoute(route, company, buildingMap, target, amountToTransfer, day, hour);
            }
        }

        private void ExecuteMarketRoute(
            SupplyRoute route,
            Company company,
            FreeMarket market,
            Building target,
            int amount,
            int day, int hour)
        {
            if (!market.IsAvailable(route.ResourceName))
            {
                route.LastTripResult = "⏸ Brak towaru na rynku dzisiaj";
                return;
            }

            int marketAvail = market.Listings.TryGetValue(route.ResourceName, out var listing)
                ? listing.RemainingToday : 0;

            int actualAmount = Math.Min(amount, marketAvail);
            if (actualAmount <= 0)
            {
                route.LastTripResult = "⏸ Wyczerpany limit dzienny na rynku";
                return;
            }

            decimal marketCost = market.GetCurrentPrice(route.ResourceName) * actualAmount;
            decimal transportCost = route.TransportCostPerUnit * actualAmount;
            decimal totalCost = marketCost + transportCost;

            if (company.Balance < totalCost)
            {
                route.LastTripResult = $"❌ Brak środków (potrzeba {totalCost:C})";
                return;
            }

            // Kup z rynku
            bool bought = market.BuyResource(route.ResourceName, actualAmount, target, company, day, hour);
            if (!bought)
            {
                route.LastTripResult = "❌ Błąd zakupu rynkowego";
                return;
            }

            // Pobierz koszt transportu
            if (transportCost > 0)
            {
                company.Balance -= transportCost;
                company.AddTransaction(day, hour,
                    $"Transport: {actualAmount}x {route.ResourceName} → {target.Name}",
                    -transportCost, "Transport", target.FacilityId);
            }

            route.LastTripResult = $"✅ {actualAmount}x {route.ResourceName} z rynku ({market.GetCurrentPrice(route.ResourceName):C}/szt.)";
            route.SourceDisplayName = "Wolny Rynek";
        }

        private void ExecuteBuildingRoute(
            SupplyRoute route,
            Company company,
            Dictionary<string, Building> buildingMap,
            Building target,
            int amount,
            int day, int hour)
        {
            if (!buildingMap.TryGetValue(route.SourceFacilityId, out var source))
            {
                route.LastTripResult = "❌ Budynek źródłowy nie istnieje";
                return;
            }

            route.SourceDisplayName = source.Name;

            // Sprawdź dostępność w magazynie źródłowym
            if (!source.Warehouse.TryGetValue(route.ResourceName, out int available) || available <= 0)
            {
                route.LastTripResult = $"⏸ Brak {route.ResourceName} w {source.Name}";
                return;
            }

            int actualAmount = Math.Min(amount, available);

            // Sprawdź koszty transportu
            decimal transportCost = route.TransportCostPerUnit * actualAmount;
            if (company.Balance < transportCost)
            {
                route.LastTripResult = $"❌ Brak środków na transport ({transportCost:C})";
                return;
            }

            // Wykonaj transfer
            source.Warehouse[route.ResourceName] -= actualAmount;

            if (!target.Warehouse.ContainsKey(route.ResourceName))
                target.Warehouse[route.ResourceName] = 0;
            target.Warehouse[route.ResourceName] += actualAmount;

            // Koszt transportu
            if (transportCost > 0)
            {
                company.Balance -= transportCost;
                company.AddTransaction(day, hour,
                    $"Transport: {actualAmount}x {route.ResourceName} {source.Name} → {target.Name}",
                    -transportCost, "Transport", target.FacilityId);
            }

            route.LastTripResult = $"✅ {actualAmount}x {route.ResourceName} z {source.Name}";
        }

        // ──────────────────────────────────────────────
        //  Serializacja — eksport/import listy tras
        // ──────────────────────────────────────────────

        /// <summary>Przywraca stan tras z danych zapisu (zeruje liczniki runtime).</summary>
        public void RestoreRoutes(List<SupplyRoute> savedRoutes)
        {
            Routes.Clear();
            foreach (var r in savedRoutes)
            {
                r.HoursSinceLastTrip = 0;
                r.LastTripResult     = "Wczytano z zapisu";
                Routes.Add(r);
            }
        }
    }
}
