using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerwallCompanion.ViewModels
{
    public class BatteryDetails
    {
        private const double warrantedCapacity = 13500;
        public string SerialNumber { get; set; }
        public string ShortSerialNumber
        {
            get => SerialNumber.Substring(0, 5) + "***" + SerialNumber.Substring(SerialNumber.Length - 2, 2);
        }
        public double FullCapacity { get; set; }
        public double CurrentChargeLevel { get; set; }
        public double CurrentChargePercent
        {
            get => CurrentChargeLevel / FullCapacity * 100;
        }
        public double WarrantedPercent
        {
            get => FullCapacity / warrantedCapacity * 100;
        }
        public double DegradationPercent
        {
            get => WarrantedPercent > 100 ? 0 : 100.0 - WarrantedPercent;
        }
    }
}
