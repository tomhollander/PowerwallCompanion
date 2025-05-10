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
        public string PowerwallPartNumber { get; set; }

        public string PowerwallVersion
        {
            get
            {
                if (PowerwallPartNumber == null)
                {
                    return "Unknown";
                }
                // https://service.tesla.com/docs/Public/Energy/Powerwall/Powerwall-2-Owners-Manual-NA-EN/GUID-9ACA2015-05B4-41A0-B8BC-1D9AD658B307.html
                // 1457844 appears on some refurbished batteries
                string[] powerwall2PartNumbers = { "1092170", "2012170", "3012170", "1457844" };
                foreach (var pw2PartNumber in powerwall2PartNumbers)
                {
                    if (PowerwallPartNumber.StartsWith(pw2PartNumber))
                    {
                        return "Powerwall 2";
                    }
                }
                return "Unknown"; // Don't know the PW3 part numbers
            }
        }
    }
}
