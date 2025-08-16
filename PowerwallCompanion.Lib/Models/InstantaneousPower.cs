using System;
using System.Collections.Generic;
using System.Text;

namespace PowerwallCompanion.Lib.Models
{
    public class InstantaneousPower
    {
        public double BatteryStoragePercent { get; set; }
        public bool GridActive { get; set; }
        public double HomePower { get; set; }
        public double SolarPower { get; set; }
        public double GridPower { get; set; }
        public double BatteryPower { get; set; }

        public bool StormWatchActive { get; set; }

        // Derived values
        public double HomeFromGrid
        {
            get { return Math.Max(0, HomePower - HomeFromSolar - HomeFromBattery); }
        }

        public double HomeFromBattery
        {
            get { return BatteryPower > 0D ? Math.Min(BatteryPower, Math.Max(0, HomePower - HomeFromSolar)) : 0D; }
        }

        public double HomeFromSolar
        {
            get { return Math.Min(SolarPower, HomePower); }
        }

        public double SolarToGrid
        {
            get { return Math.Max(0, SolarPower - SolarToHome - SolarToBattery); }
        }

        public double SolarToBattery
        {
            get { return BatteryPower > 0D ? Math.Min(SolarPower - SolarToHome, Math.Abs(BatteryPower)): 0D; }
        }

        public double SolarToHome
        {
            get { return HomeFromSolar; }
        }

        public double BatteryToGrid
        {
            get { return BatteryPower > 0D ? Math.Max(0, BatteryPower - HomePower) : 0D; }
        }

        public double BatteryFromGrid
        {
            get { return BatteryPower < 0 && GridPower > HomeFromGrid ? Math.Abs(BatteryPower) : Math.Max(0, GridPower - HomeFromGrid);  }
        }
    }
}
