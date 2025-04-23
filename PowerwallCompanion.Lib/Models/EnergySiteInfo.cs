using System;
using System.Collections.Generic;
using System.Text;

namespace PowerwallCompanion.Lib.Models
{
    public class EnergySiteInfo
    {
        public string SiteName { get; set; }
        public string GatewayId { get; set; }
        public int NumberOfBatteries { get; set; }
        public DateTime InstallDate { get; set; }
        public string InstallDateString { get { return InstallDate.ToString("d"); } }
        public int ReservePercent { get; set; }
        public string PowerwallVersion { get; set; }
    }
}
