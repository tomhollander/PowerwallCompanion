﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
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
        public string SiteName { get; set; }
        public int NumberOfBatteries { get; set; }
        public DateTime InstallDate { get; set; }
        public string InstallDateString { get { return InstallDate.ToString("d"); } }
        public double WarrantedCapacity { 
            get 
            {
                int capacity = 13500;
                return capacity * NumberOfBatteries; 
            } 
        }

        public double WarrantedCapacityKWh
        {
            get => WarrantedCapacity / 1000;
        }

        public double MinimumWarrantedCapacityKWh
        {
            get
            {
                double warrantedProportion = 0.7; // True in most countries
                return WarrantedCapacityKWh * warrantedProportion;
            }
        }

        public double TotalPackEnergy { get; set; }

        public double CurrentCapacityPercent { get { return TotalPackEnergy / WarrantedCapacity * 100; } }
        public double Degradation { get { return CurrentCapacityPercent > 100 ? 0 : 100.0 - CurrentCapacityPercent; } }
        public string GatewayId { get; set; }

    
        public List<ChartDataPoint> BatteryHistoryChartData
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
        public void NotifyAllProperties()
        {
            NotifyPropertyChanged(nameof(SiteName));
            NotifyPropertyChanged(nameof(InstallDateString));
            NotifyPropertyChanged(nameof(NumberOfBatteries));
            NotifyPropertyChanged(nameof(WarrantedCapacity));
            NotifyPropertyChanged(nameof(TotalPackEnergy));
            NotifyPropertyChanged(nameof(CurrentCapacityPercent));
            NotifyPropertyChanged(nameof(Degradation));
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
