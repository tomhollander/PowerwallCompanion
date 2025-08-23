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
        public double SolarToHome => Math.Min(SolarPower, HomePower);

        public double BatteryToHome =>
            BatteryPower > 0D ? Math.Min(BatteryPower, Math.Max(0D, HomePower - SolarToHome)) : 0D;

        public double GridToHome =>
            Math.Max(0D, HomePower - SolarToHome - BatteryToHome);

        // Battery charging attribution
        public double SolarToBattery
        {
            get
            {
                if (BatteryPower >= 0D) return 0D;

                double batteryCharge = Math.Abs(BatteryPower);
                double solarSurplus = Math.Max(0D, SolarPower - SolarToHome);
                return Math.Min(solarSurplus, batteryCharge);
            }
        }

        public double GridToBattery
        {
            get
            {
                if (BatteryPower >= 0D) return 0D;

                double batteryCharge = Math.Abs(BatteryPower);
                double solarToBattery = SolarToBattery;
                return batteryCharge - solarToBattery;
            }
        }

        public double SolarToGrid =>
            Math.Max(0D, SolarPower - SolarToHome - SolarToBattery);

        public double BatteryToGrid =>
            BatteryPower > 0D ? Math.Max(0D, BatteryPower - BatteryToHome) : 0D;

        // Combined power values for bar chart
        public double AllGridImport => GridToHome + GridToBattery;

        public double AllBatteryCharge => SolarToBattery + GridToBattery;

    }
}
