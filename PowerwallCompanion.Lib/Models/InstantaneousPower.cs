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
            get { return GridPower > 0D ? GridPower : 0D; }
        }

        public double HomeFromBattery
        {
            get { return BatteryPower > 0D ? BatteryPower : 0D; }
        }

        public double HomeFromSolar
        {
            get { return HomePower - HomeFromGrid - HomeFromBattery; }
        }

        public double SolarToGrid
        {
            get { return GridPower < 0D ? -GridPower : 0D; }
        }

        public double SolarToBattery
        {
            get { return BatteryPower < 0D ? -BatteryPower : 0D; }
        }

        public double SolarToHome
        {
            get { return SolarPower - SolarToGrid - SolarToBattery; }
        }
    }
}
