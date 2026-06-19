using System;
using System.Collections.Generic;
using System.Linq;
using Conglomerate.Economy;

namespace Conglomerate
{
    public class RNDCenter : Building
    {
        public override string ActivityType => "Centrum Badań i Rozwoju";
        public override decimal BuildCost => 500000m;
        public override decimal MaintenanceCost => 1000m;
        public override int WarehouseCapacity => 0; // R&D doesn't store items
        public override Dictionary<string, decimal> ResourcePrices => new Dictionary<string, decimal>();

        public string ActiveResearchProject { get; private set; }
        public int ResearchProgressHours { get; private set; }
        public int RequiredResearchHours { get; private set; }

        public float ProgressNormalized => RequiredResearchHours > 0 ? (float)ResearchProgressHours / RequiredResearchHours : 0f;

        public RNDCenter(string name) : base(name)
        {
        }

        public void SetResearchProject(string productName, int requiredHours)
        {
            ActiveResearchProject = productName;
            RequiredResearchHours = requiredHours;
            ResearchProgressHours = 0;
        }

        public override bool Produce(Company company)
        {
            // TryAdvanceResearch is called from GameManager similarly to FactoryBuilding.
            return true;
        }

        public bool TryAdvanceResearch(Company company)
        {
            if (string.IsNullOrEmpty(ActiveResearchProject)) return false;

            // R&D also consumes OPEX per hour on top of maintenance, to simulate research cost
            decimal hourlyCost = 50m;
            if (company.Balance < hourlyCost) return false;

            company.Balance -= hourlyCost;

            ResearchProgressHours++;
            if (ResearchProgressHours >= RequiredResearchHours)
            {
                // Research completed
                if (!company.TechLevels.ContainsKey(ActiveResearchProject))
                    company.TechLevels[ActiveResearchProject] = 0f;
                
                // Increase tech level by 10 points
                company.TechLevels[ActiveResearchProject] += 10f;

                // Reset
                ActiveResearchProject = null;
                ResearchProgressHours = 0;
                RequiredResearchHours = 0;

                return true; // Completed
            }

            return false;
        }
    }
}

