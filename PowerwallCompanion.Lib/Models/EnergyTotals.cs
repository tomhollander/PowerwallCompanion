using System;
using System.Collections.Generic;
using System.Text;

namespace PowerwallCompanion.Lib.Models
{
    public class EnergyTotals
    {
        public double HomeEnergy { get; set; }
        public double SolarEnergy { get; set; }
        public double GridEnergyImported { get; set; }
        public double GridEnergyExported { get; set; }
        public double BatteryEnergyCharged { get; set; }
        public double BatteryEnergyDischarged { get; set; }
        public decimal EnergyCost { get; set; }
        public decimal EnergyFeedIn { get; set; }
        public decimal EnergyNetCost { get => EnergyCost - EnergyFeedIn; }
        public double SelfConsumption { get; set; }
        public double SolarUsePercent { get; set; }
        public double GridUsePercent { get; set; }
        public double BatteryUsePercent { get; set; }
    }
}
