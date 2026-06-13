using System;
using System.Collections.Generic;
using System.Linq;

namespace Conglomerate.Logistics
{
    public class ActiveTrip
    {
        public string RouteId { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public int CargoAmount { get; set; }
        public int RemainingHours { get; set; }
        public decimal TripCost { get; set; }
        public string DestinationBuildingId { get; set; } = string.Empty;
        public string VehicleTypeName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Zarządza wszystkimi trasami logistycznymi w grze.
    /// Wywoływany przez GameManager co każdy tick (1 godzina).
    /// </summary>
    public class LogisticsManager
    {
        // ──────────────────────────────────────────────
        //  Stan
        // ──────────────────────────────────────────────

        /// <summary>Wszystkie aktywne trasy logistyczne.</summary>
        public List<SupplyRoute> Routes { get; } = new List<SupplyRoute>();

        /// <summary>Pojazdy aktualnie w drodze (real-time transport).</summary>
        public List<ActiveTrip> ActiveTrips { get; } = new List<ActiveTrip>();

        /// <summary>Całkowity rozmiar floty pojazdów przedsiębiorstwa.</summary>
        public int TotalFleetSize { get; set; } = 8;

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
        /// Główna metoda ticku — aktualizuje przejazdy oraz planuje kolejne wysyłki.
        /// </summary>
        public void Tick(
            Company company,
            FreeMarket market,
            int day, int hour)
        {
            // Odbuduj mapę FacilityId → Building dla szybkiego wyszukiwania
            var buildingMap = company.Buildings.ToDictionary(b => b.FacilityId, b => b);

            // 1. Aktualizacja przejazdów w drodze
            for (int i = ActiveTrips.Count - 1; i >= 0; i--)
            {
                var trip = ActiveTrips[i];
                trip.RemainingHours--;

                if (trip.RemainingHours <= 0)
                {
                    // Dostawa na miejsce
                    if (buildingMap.TryGetValue(trip.DestinationBuildingId, out var target))
                    {
                        if (!target.Warehouse.ContainsKey(trip.ResourceName))
                            target.Warehouse[trip.ResourceName] = 0;
                        target.Warehouse[trip.ResourceName] += trip.CargoAmount;

                        var r = Routes.Find(x => x.Id == trip.RouteId);
                        if (r != null)
                        {
                            r.LastTripResult = $"✅ Dostarczono {trip.CargoAmount}x {trip.ResourceName} ({trip.VehicleTypeName})";
                        }
                    }
                    ActiveTrips.RemoveAt(i);
                }
            }

            // 2. Przygotowanie kolejki wysyłek
            var pendingDispatches = new List<(SupplyRoute Route, int CargoToLoad, decimal TotalTripCost)>();

            foreach (var route in Routes)
            {
                if (!route.IsEnabled) continue;

                route.HoursSinceLastTrip++;

                if (!buildingMap.TryGetValue(route.TargetFacilityId, out var target))
                {
                    route.LastTripResult = "❌ Budynek docelowy nie istnieje";
                    continue;
                }

                // Wolne miejsce w magazynie docelowym (z uwzględnieniem pojazdów w drodze)
                int freeSpace = target.WarehouseCapacity - target.GetTotalStock();
                int inTransit = ActiveTrips
                    .Where(t => t.DestinationBuildingId == route.TargetFacilityId && t.ResourceName == route.ResourceName)
                    .Sum(t => t.CargoAmount);
                freeSpace -= inTransit;

                if (freeSpace <= 0)
                {
                    route.LastTripResult = "⏸ Magazyn docelowy pełny";
                    continue;
                }

                var vehicle = VehicleRegistry.Get(route.VehicleTypeName);
                int maxToLoad = route.LoadRule == LoadThresholdRule.FullOnly ? vehicle.Capacity : Math.Min(route.AmountPerTrip, vehicle.Capacity);
                int capacity = Math.Min(maxToLoad, freeSpace);

                // Dostępność towaru u dostawcy
                int availableCargo = 0;
                decimal marketPrice = 0m;

                if (route.SourceType == RouteSourceType.Market)
                {
                    if (market.IsAvailable(route.ResourceName))
                    {
                        market.Listings.TryGetValue(route.ResourceName, out var listing);
                        int marketAvail = listing != null ? listing.RemainingToday : 0;
                        availableCargo = Math.Min(capacity, marketAvail);
                        marketPrice = market.GetCurrentPrice(route.ResourceName);
                    }
                }
                else
                {
                    if (buildingMap.TryGetValue(route.SourceFacilityId, out var source))
                    {
                        route.SourceDisplayName = source.Name;
                        int stock = 0;
                        source.Warehouse.TryGetValue(route.ResourceName, out stock);
                        availableCargo = Math.Min(capacity, stock);
                    }
                    else
                    {
                        route.LastTripResult = "❌ Budynek źródłowy nie istnieje";
                        continue;
                    }
                }

                // Sprawdzenie reguł progów ładowania
                bool shouldDispatch = false;
                int cargoToLoad = 0;

                switch (route.LoadRule)
                {
                    case LoadThresholdRule.FullOnly:
                        // Wysyłka tylko przy 100% pojemności pojazdu
                        shouldDispatch = availableCargo >= vehicle.Capacity;
                        cargoToLoad = vehicle.Capacity;
                        break;

                    case LoadThresholdRule.MinCargo50:
                        // Wysyłka przy minimum 50% pojemności pojazdu
                        int minRequired = (int)Math.Ceiling(vehicle.Capacity * 0.5f);
                        shouldDispatch = availableCargo >= minRequired;
                        cargoToLoad = availableCargo;
                        break;

                    case LoadThresholdRule.TimerOnly:
                        // Wysyłka na podstawie zegara co IntervalHours
                        if (route.HoursSinceLastTrip >= route.IntervalHours)
                        {
                            if (availableCargo > 0)
                            {
                                shouldDispatch = true;
                                cargoToLoad = availableCargo;
                            }
                            else
                            {
                                route.HoursSinceLastTrip = 0;
                                route.LastTripResult = "⏸ Oczekiwanie na towar (Timer)";
                            }
                        }
                        break;
                }

                if (shouldDispatch && cargoToLoad > 0)
                {
                    decimal totalTripCost = vehicle.BaseCostPerTrip;
                    if (route.SourceType == RouteSourceType.Market)
                    {
                        totalTripCost += marketPrice * cargoToLoad;
                    }

                    pendingDispatches.Add((route, cargoToLoad, totalTripCost));
                }
            }

            // 3. Sortowanie według priorytetu (High > Medium > Low)
            var sortedPending = pendingDispatches
                .OrderByDescending(p => p.Route.Priority)
                .ThenBy(p => p.Route.Id)
                .ToList();

            // 4. Realizacja wysyłek na podstawie dostępności pojazdów we flocie
            foreach (var dispatch in sortedPending)
            {
                var route = dispatch.Route;
                var vehicle = VehicleRegistry.Get(route.VehicleTypeName);

                // Sprawdzenie wolnych pojazdów we flocie
                if (ActiveTrips.Count >= TotalFleetSize)
                {
                    route.LastTripResult = "⏸ Oczekiwanie na wolną flotę (Kolejka)";
                    continue;
                }

                // Sprawdzenie funduszy firmy
                if (company.Balance < dispatch.TotalTripCost)
                {
                    route.LastTripResult = $"❌ Brak środków (potrzeba {dispatch.TotalTripCost:C})";
                    continue;
                }

                // Wykonaj wysyłkę
                if (route.SourceType == RouteSourceType.Market)
                {
                    bool bought = market.BuyResource(route.ResourceName, dispatch.CargoToLoad, buildingMap[route.TargetFacilityId], company, day, hour);
                    if (!bought)
                    {
                        route.LastTripResult = "❌ Błąd zakupu rynkowego";
                        continue;
                    }
                }
                else
                {
                    var source = buildingMap[route.SourceFacilityId];
                    source.Warehouse[route.ResourceName] -= dispatch.CargoToLoad;
                }

                // Pobierz koszty transportu (Logistics OPEX)
                if (vehicle.BaseCostPerTrip > 0)
                {
                    company.Balance -= vehicle.BaseCostPerTrip;
                    company.AddTransaction(day, hour,
                        $"Transport: {dispatch.CargoToLoad}x {route.ResourceName} z {(route.SourceType == RouteSourceType.Market ? "rynku" : buildingMap[route.SourceFacilityId].Name)} → {buildingMap[route.TargetFacilityId].Name} ({vehicle.DisplayName})",
                        -vehicle.BaseCostPerTrip, "Transport", route.TargetFacilityId);
                }

                // Utwórz podróż aktywną
                ActiveTrips.Add(new ActiveTrip
                {
                    RouteId = route.Id,
                    ResourceName = route.ResourceName,
                    CargoAmount = dispatch.CargoToLoad,
                    RemainingHours = vehicle.TravelTimeHours,
                    TripCost = vehicle.BaseCostPerTrip,
                    DestinationBuildingId = route.TargetFacilityId,
                    VehicleTypeName = vehicle.Name
                });

                route.HoursSinceLastTrip = 0;
                route.LastTripResult = $"🚚 W drodze ({vehicle.TravelTimeHours}h)";
            }
        }

        // ──────────────────────────────────────────────
        //  Serializacja — eksport/import listy tras
        // ──────────────────────────────────────────────

        public void RestoreRoutes(List<SupplyRoute> savedRoutes)
        {
            Routes.Clear();
            ActiveTrips.Clear();
            foreach (var r in savedRoutes)
            {
                r.HoursSinceLastTrip = 0;
                r.LastTripResult = "Wczytano z zapisu";
                Routes.Add(r);
            }
        }
    }
}
