using System;
using System.Collections.Generic;

namespace Conglomerate.Logistics
{
    public enum LoadThresholdRule
    {
        /// <summary>Wysyłaj co określony czas bez względu na ilość (depart on timer).</summary>
        TimerOnly,

        /// <summary>Czekaj aż uzbiera się co najmniej 50% pojemności pojazdu.</summary>
        MinCargo50,

        /// <summary>Wysyłaj tylko przy pełnym załadowaniu (100% pojemności).</summary>
        FullOnly
    }

    public enum RoutePriority
    {
        Low,
        Medium,
        High
    }

    public class VehicleTypeConfig
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public decimal BaseCostPerTrip { get; set; }
        public int TravelTimeHours { get; set; }
    }

    public static class VehicleRegistry
    {
        public static readonly List<VehicleTypeConfig> VehicleTypes = new List<VehicleTypeConfig>
        {
            new VehicleTypeConfig 
            { 
                Name = "Van", 
                DisplayName = "Mały dostawca (Van) [15 szt, 2h, 40 zł]", 
                Capacity = 15, 
                BaseCostPerTrip = 40m, 
                TravelTimeHours = 2 
            },
            new VehicleTypeConfig 
            { 
                Name = "Truck", 
                DisplayName = "Ciężki transport (Truck) [60 szt, 6h, 120 zł]", 
                Capacity = 60, 
                BaseCostPerTrip = 120m, 
                TravelTimeHours = 6 
            }
        };

        public static VehicleTypeConfig Get(string name)
        {
            var found = VehicleTypes.Find(v => v.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return found ?? VehicleTypes[0];
        }
    }
}
