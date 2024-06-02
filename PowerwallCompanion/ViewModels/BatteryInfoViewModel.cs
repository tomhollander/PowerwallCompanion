using PowerwallCompanion.Lib.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.System.UserProfile;

namespace PowerwallCompanion.ViewModels
{
    public class BatteryInfoViewModel : INotifyPropertyChanged
    {
        public BatteryInfoViewModel()
        {
            EnoughDataToShowChart = true; // Prevent flicker
        }

        public bool CachedData { get; set; }
        public List<BatteryDetails> BatteryDetails { get; set; }

        public string GatewayError { get; set; }

        public double WarrantedCapacity
        {
            get
            {
                if (EnergySiteInfo == null)
                {
                    return 0;
                }
                int capacity = 13500;
                return capacity * EnergySiteInfo.NumberOfBatteries;
            }
        }

        public double WarrantedCapacityKWh
        {
            get => WarrantedCapacity / 1000;
        }

        private readonly string[] regionsWith80PercentWarranty = { "AT", "BE", "FR", "DE", "IE", "LU", "NL", "CH", "GB" };

        private double WarrantyProportion
        {
            get
            {
                double warrantedProportion = 0.7; // True in most countries
                var region = Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion;
                if (regionsWith80PercentWarranty.Contains(region))
                {
                    warrantedProportion = 0.8;
                }
                return warrantedProportion;
            }

        }

        public double MinimumWarrantedCapacityKWh
        {
            get
            {
                return WarrantedCapacityKWh * WarrantyProportion;
            }
        }

        public string MinimumWarrantedCapacityPercentageMessage
        {
            get
            {
                return $"{WarrantyProportion * 100}% baseline capacity";
            }
        }

        public Dictionary<string, List<ChartDataPoint>> BatteryHistoryChartData
        {
            get; set;
        }

        public bool EnoughDataToShowChart
        {
            get; set;
        }

        public bool ShowChart
        {
            get { return EnoughDataToShowChart && StoreBatteryHistory; }
        }

        public bool ShowNotEnoughDataMessage
        {
            get { return !EnoughDataToShowChart && StoreBatteryHistory; }
        }

        public bool StoreBatteryHistory
        {
            get
            {
                return Settings.StoreBatteryHistory;
            }
            set
            {
                Settings.StoreBatteryHistory = value;
            }
        }

        public EnergySiteInfo EnergySiteInfo { get; set; }
        public void NotifyAllProperties()
        {
            NotifyPropertyChanged(nameof(EnergySiteInfo));
            NotifyPropertyChanged(nameof(BatteryDetails));
        }

        public void NotifyChartProperties()
        {
            NotifyPropertyChanged(nameof(StoreBatteryHistory));
            NotifyPropertyChanged(nameof(ShowChart));
            NotifyPropertyChanged(nameof(EnoughDataToShowChart));
            NotifyPropertyChanged(nameof(BatteryHistoryChartData));
            NotifyPropertyChanged(nameof(ShowNotEnoughDataMessage));
            NotifyPropertyChanged(nameof(WarrantedCapacityKWh));
            NotifyPropertyChanged(nameof(MinimumWarrantedCapacityKWh));
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }


}
