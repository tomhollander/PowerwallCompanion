using System;
using System.Collections.Generic;
using System.Text;

namespace PowerwallCompanion.Lib.Models
{
    public class BatteryDetailsResponse
    {
        public List<BatteryDetails> BatteryDetails { get; set; }
        public string ErrorMessage { get; set; }
    }
}
