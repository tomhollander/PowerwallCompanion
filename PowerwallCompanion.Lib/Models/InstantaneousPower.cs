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
        
        // Grid power allocation: Home first, then battery
        public double GridToHome
        {
            get
            {
                if (GridPower <= 0) return 0; // Grid not importing
                return Math.Min(GridPower, HomePower);
            }
        }

        public double GridToBattery
        {
            get
            {
                if (GridPower <= 0 || BatteryPower >= 0) return 0; // Grid not importing or battery not charging
                
                double gridSurplus = Math.Max(0, GridPower - GridToHome);
                double batteryCharge = Math.Abs(BatteryPower);
                return Math.Min(gridSurplus, batteryCharge);
            }
        }

        // Solar power allocation
        public double SolarToHome
        {
            get
            {
                double remainingHomePower = Math.Max(0, HomePower - GridToHome - BatteryToHome);
                return Math.Min(SolarPower, remainingHomePower);
            }
        }

        public double SolarToBattery
        {
            get
            {
                if (BatteryPower >= 0) return 0; // Battery not charging

                double batteryCharge = Math.Abs(BatteryPower);
                double remainingBatteryCharge = Math.Max(0, batteryCharge - GridToBattery);
                double solarSurplus = Math.Max(0, SolarPower - SolarToHome);
                return Math.Min(solarSurplus, remainingBatteryCharge);
            }
        }

        public double SolarToGrid =>
            Math.Max(0, SolarPower - SolarToHome - SolarToBattery);

        // Battery power allocation
        public double BatteryToHome
        {
            get
            {
                if (BatteryPower <= 0) return 0; // Battery not discharging
                
                double remainingHomePower = Math.Max(0, HomePower - GridToHome);
                return Math.Min(BatteryPower, remainingHomePower);
            }
        }

        public double BatteryToGrid =>
            BatteryPower > 0 ? Math.Max(0, BatteryPower - BatteryToHome) : 0;

        // Combined power values for bar chart
        public double AllGridImport => GridToHome + GridToBattery;

        public double AllBatteryCharge => SolarToBattery + GridToBattery;
    }
}
