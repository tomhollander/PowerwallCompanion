using System;
using System.Collections.Generic;
using System.Text;

namespace PowerwallCompanion.Lib.Models
{
    public class VehicleData
    {
        public string VehicleId { get; set; }
        public string VehicleName { get; set; }
        public int BatteryLevel { get; set; }
        public bool IsAwake { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime LastWoken { get; set; }
    }
}
