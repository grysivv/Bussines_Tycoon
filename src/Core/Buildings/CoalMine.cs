using System;
using System.Collections.Generic;

namespace Conglomerate
{
    public class ProductionHaltedEventArgs : EventArgs
    {
        public string Message { get; }
        public ProductionHaltedEventArgs(string message)
        {
            Message = message;
        }
    }

    public class CoalMine : Building
    {
        public override string ActivityType => "Kopalnia węgla";
        public override decimal BuildCost => 15000m;
        public override decimal MaintenanceCost => 250m;
        public override int WarehouseCapacity => 38356;
        public override Dictionary<string, decimal> ResourcePrices { get; } = new Dictionary<string, decimal>
        {
            { "Węgiel", 400m }
        };

        public int Level { get; set; } = 1;
        public int MaxEmployees => Level == 2 ? 3000 : 1200;
        public float LevelMultiplier => Level == 2 ? 1.2f : 1.0f;
        public float TechnologyMultiplier { get; set; } = 1.0f;

        private int _currentEmployees;
        public int CurrentEmployees
        {
            get => _currentEmployees;
            set => _currentEmployees = Math.Clamp(value, 0, MaxEmployees);
        }

        public double MaxStorageCapacity => 38356.0;
        private double _preciseStorage = 0.0;
        public double PreciseStorage => _preciseStorage;

        public event EventHandler<ProductionHaltedEventArgs>? OnProductionHalted;

        public CoalMine(string name) : base(name)
        {
            Warehouse["Węgiel"] = new Economy.ProductBatch("Węgiel", 0);
            CurrentEmployees = 1200; // Poziom 1 domyślna liczba pracowników
        }

        public override bool Produce(Company company)
        {
            // CoalMine now uses hourly updates: ProduceHourly
            return false;
        }

        public bool ProduceHourly(Company company, int day, int hour)
        {
            // Sync with base Warehouse dictionary if altered externally by sales or logistics
            decimal currentWarehouseCoal = GetProductQuantity("Węgiel");
            if (currentWarehouseCoal != (decimal)_preciseStorage)
            {
                _preciseStorage = (double)currentWarehouseCoal;
            }

            // Koszt płac (81.25 PLN za godzinę za pracownika) oraz bazowy koszt utrzymania kopalni
            decimal hourlyLaborCost = CurrentEmployees * 35.25m;
            decimal hourlyMaintenance = MaintenanceCost / 24m;
            decimal totalHourlyCost = hourlyLaborCost + hourlyMaintenance;

            if (company.Balance < totalHourlyCost)
            {
                return false;
            }

            // Pensje i koszty utrzymania muszą być opłacone bez względu na to, czy magazyn jest pełny
            company.Balance -= totalHourlyCost;

            if (_preciseStorage >= MaxStorageCapacity)
            {
                OnProductionHalted?.Invoke(this, new ProductionHaltedEventArgs($"Kopalnia {Name}: Produkcja wstrzymana - brak miejsca na hałdzie (magazyn pełny)."));
                return false;
            }

            // Produkcja węgla (ProductionPerHour = CurrentEmployees * 0.085 * LevelMultiplier * TechnologyMultiplier)
            double producedAmount = CurrentEmployees * 0.085 * LevelMultiplier * TechnologyMultiplier;
            double freeSpace = MaxStorageCapacity - _preciseStorage;
            double addedAmount = Math.Min(producedAmount, freeSpace);

            _preciseStorage += addedAmount;
            Warehouse["Węgiel"].Quantity = (decimal)_preciseStorage;

            return true;
        }

        public void ForceSetPreciseStorage(double value)
        {
            _preciseStorage = Math.Clamp(value, 0.0, MaxStorageCapacity);
            if (!Warehouse.ContainsKey("Węgiel")) Warehouse["Węgiel"] = new Economy.ProductBatch("Węgiel", 0);
            Warehouse["Węgiel"].Quantity = (decimal)_preciseStorage;
        }
    }
}
