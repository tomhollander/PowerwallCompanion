using Microsoft.AppCenter.Crashes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace PowerwallCompanion.ViewModels
{
    public class StatusViewModel : INotifyPropertyChanged
    {
        public enum StatusEnum
        {
            Online,
            GridOutage,
            Error
        }

        private double _batteryPercent;
        private double _minPercentToday;
        private double _maxPercentToday;
        private DateTime _batteryDay = DateTime.MinValue;
        private double _homeValue;
        private double _solarValue;
        private double _batteryValue;
        private double _gridValue;
        private bool _gridActive;
        private StatusEnum _status;

        public StatusViewModel()
        {
        }

        public void Reset()
        {
            EnergyHistoryLastRefreshed = DateTime.MinValue;
            PowerHistoryLastRefreshed = DateTime.MinValue;
            LiveStatusLastRefreshed = DateTime.MinValue;
            _batteryDay = DateTime.MinValue;

        }
 
        public void NotifyPowerProperties()
        {
            NotifyPropertyChanged(nameof(BatteryPercent));
            NotifyPropertyChanged(nameof(BatteryStatus));
            NotifyPropertyChanged(nameof(BatteryValue));
            NotifyPropertyChanged(nameof(HomeValue));
            NotifyPropertyChanged(nameof(HomeFromBattery));
            NotifyPropertyChanged(nameof(HomeFromGrid));
            NotifyPropertyChanged(nameof(HomeFromSolar));
            NotifyPropertyChanged(nameof(SolarValue));
            NotifyPropertyChanged(nameof(SolarToBattery));
            NotifyPropertyChanged(nameof(SolarToGrid));
            NotifyPropertyChanged(nameof(SolarToHome));
            NotifyPropertyChanged(nameof(GridValue));
            NotifyPropertyChanged(nameof(GridActive));
            NotifyPropertyChanged(nameof(Time));
        }

        public void NotifyDailyEnergyProperties()
        {
            NotifyPropertyChanged(nameof(HomeEnergyYesterday));
            NotifyPropertyChanged(nameof(HomeEnergyToday));
            NotifyPropertyChanged(nameof(SolarEnergyYesterday));
            NotifyPropertyChanged(nameof(SolarEnergyToday));
            NotifyPropertyChanged(nameof(GridEnergyImportedYesterday));
            NotifyPropertyChanged(nameof(GridEnergyImportedToday));
            NotifyPropertyChanged(nameof(GridEnergyExportedYesterday));
            NotifyPropertyChanged(nameof(GridEnergyExportedToday));
            NotifyPropertyChanged(nameof(BatteryEnergyImportedYesterday));
            NotifyPropertyChanged(nameof(BatteryEnergyImportedToday));
            NotifyPropertyChanged(nameof(BatteryEnergyExportedYesterday));
            NotifyPropertyChanged(nameof(BatteryEnergyExportedToday));
            NotifyPropertyChanged(nameof(ShowBothGridSettingsToday));
            NotifyPropertyChanged(nameof(ShowBothGridSettingsYesterday));
            NotifyPropertyChanged(nameof(Time));
        }

        public void NotifyGraphProperties()
        {
            NotifyPropertyChanged(nameof(HomeGraphData));
            NotifyPropertyChanged(nameof(SolarGraphData));
            NotifyPropertyChanged(nameof(BatteryGraphData));
            NotifyPropertyChanged(nameof(GridGraphData));
            NotifyPropertyChanged(nameof(GraphDayBoundary));
            NotifyPropertyChanged(nameof(ChartMaxDate));
        }
        public void NotifyChangedSettings()
        {
            NotifyPropertyChanged(nameof(ShowClock));
        }

        public double BatteryPercent
        {
            get { return _batteryPercent; }
            set
            {
                _batteryPercent = value;
                UpdateMinMaxPercentToday();
            }
        }

        public double MinBatteryPercentToday
        {
            get { return _minPercentToday; }
        }

        public double MaxBatteryPercentToday
        {
            get { return _maxPercentToday; }
        }
        public double HomeValue
        {
            get { return _homeValue; }
            set
            {
                _homeValue = value;
            }
        }

        public double SolarValue
        {
            get { return _solarValue; }
            set
            {
                _solarValue = value;

            }
        }

        public double BatteryValue
        {
            get { return _batteryValue; }
            set
            {
                _batteryValue = value;
            }
        }

        public double GridValue
        {
            get { return _gridValue; }
            set
            {
                _gridValue = value;
            }
        }

        public double TotalPackEnergy
        {
            get; set;
        }
        public double HomeEnergyToday
        {
            get; set;
        }

        public double HomeEnergyYesterday
        {
            get; set;
        }

        public double SolarEnergyToday
        {
            get; set;
        }

        public double SolarEnergyYesterday
        {
            get; set;
        }

        public double GridEnergyImportedToday
        {
            get; set;
        }

        public double GridEnergyImportedYesterday
        {
            get; set;
        }

        public double GridEnergyExportedToday
        {
            get; set;
        }

        public double GridEnergyExportedYesterday
        {
            get; set;
        }

        public Visibility ShowBothGridSettingsToday
        {
            get { return GridEnergyExportedToday > 500 ? Visibility.Visible : Visibility.Collapsed; }
        }

        public Visibility ShowBothGridSettingsYesterday
        {
            get { return GridEnergyExportedYesterday > 500 ? Visibility.Visible : Visibility.Collapsed; }
        }

        public double BatteryEnergyImportedToday
        {
            get; set;
        }

        public double BatteryEnergyImportedYesterday
        {
            get; set;
        }

        public double BatteryEnergyExportedToday
        {
            get; set;
        }

        public double BatteryEnergyExportedYesterday
        {
            get; set;
        }

        public double HomeFromGrid
        {
            get { return GridValue > 0D ? GridValue : 0D; }
        }

        public double HomeFromBattery
        {
            get { return BatteryValue > 0D ? BatteryValue : 0D; }
        }

        public double HomeFromSolar
        {
            get { return HomeValue - HomeFromGrid - HomeFromBattery; }
        }

        public double SolarToGrid
        {
            get { return GridValue < 0D ? -GridValue : 0D; }
        }

        public double SolarToBattery
        {
            get { return BatteryValue < 0D ? -BatteryValue : 0D; }
        }

        public double SolarToHome
        {
            get { return SolarValue - SolarToGrid - SolarToBattery; }
        }

        public List<ChartDataPoint> HomeGraphData
        {
            get; set;
        }
        public List<ChartDataPoint> SolarGraphData
        {
            get; set;
        }
        public List<ChartDataPoint> GridGraphData
        {
            get; set;
        }
        public List<ChartDataPoint> BatteryGraphData
        {
            get; set;
        }

        public DateTime ChartMaxDate
        {
            get
            {
                if (Settings.AccessToken == "DEMO")
                {
                    return new DateTime(2021, 04, 17); // Match the dummy data
                }
                return DateUtils.ConvertToPowerwallDate(DateTime.Now).Date.AddDays(1);
            }
        }
        public StatusEnum Status
        {
            get { return _status; }
            set
            {
                _status = value;
                NotifyPropertyChanged(nameof(Status));
            }
        }


        public string BatteryStatus
        {
            get
            {
                if (BatteryValue < -20)
                {
                    return "Charging";
                }
                else if (BatteryValue > 20)
                {
                    return "Discharging";
                }
                else
                {
                    return "Standby";
                }
            }
        }

        public bool GridActive
        {
            get { return _gridActive; }
            set
            {
                _gridActive = value;
            }
        }

  

        public string Time
        {
            get
            {
                string pattern = DateTimeFormatInfo.CurrentInfo.ShortTimePattern;
                string patternWithoutAmPm = pattern.Replace("tt", "");
                return DateTime.Now.ToString(patternWithoutAmPm);
            }
        }

        public bool ShowClock
        {
            get
            {
                return Settings.ShowClock;
            }
        }

       

        public string LastExceptionMessage { get; set; }
        public DateTime LastExceptionDate { get; set; }
        public DateTime LiveStatusLastRefreshed { get; set; }
        public DateTime EnergyHistoryLastRefreshed { get; set; }

        public DateTime PowerHistoryLastRefreshed { get; set; }
        public DateTime GraphDayBoundary
        {
            get { return DateTime.Today; }
        }

        private async void UpdateMinMaxPercentToday()
        {
            if (_batteryDay == DateTime.MinValue)
            {
                await GetInitialBatteryMinMaxToday();
                NotifyPropertyChanged(nameof(MinBatteryPercentToday));
                NotifyPropertyChanged(nameof(MaxBatteryPercentToday));
            }
            else if (_batteryDay != DateUtils.ConvertToPowerwallDate(DateTime.Now).Date)
            {
                _batteryDay = DateTime.Today;
                _minPercentToday = BatteryPercent;
                _maxPercentToday = BatteryPercent;
                NotifyPropertyChanged(nameof(MinBatteryPercentToday));
                NotifyPropertyChanged(nameof(MaxBatteryPercentToday));
            }
            else if (BatteryPercent < _minPercentToday)
            {
                _minPercentToday = BatteryPercent;
                NotifyPropertyChanged(nameof(MinBatteryPercentToday));
            }
            else if (BatteryPercent > _maxPercentToday)
            {
                _maxPercentToday = BatteryPercent;
                NotifyPropertyChanged(nameof(MaxBatteryPercentToday));
            }
        }

        private async Task GetInitialBatteryMinMaxToday()
        {
            try
            {
                var json = await ApiHelper.CallGetApiWithTokenRefresh($"/api/1/energy_sites/{Settings.SiteId}/calendar_history?kind=soe", "SOE");
                int min = 100;
                int max = 0;
                foreach (var datapoint in (JArray)json["response"]["time_series"])
                {
                    var timestamp = DateUtils.ConvertToPowerwallDate(datapoint["timestamp"].Value<DateTime>());
                    if (timestamp.Date == DateUtils.ConvertToPowerwallDate(DateTime.Now).Date)
                    {
                        var soe = datapoint["soe"].Value<int>();
                        if (soe < min) min = soe;
                        if (soe > max) max = soe;

                    }
                }
                _batteryDay = DateTime.Now.Date;
                _minPercentToday = (double)min;
                _maxPercentToday = (double)max;
            }
            catch (Exception ex)
            {
                // Don't worry, NBD
                Crashes.TrackError(ex);
            }
        }

        private double _gridLowCarbonPercent;
        public double GridLowCarbonPercent
        {
            get { return _gridLowCarbonPercent; }
            set
            {
                _gridLowCarbonPercent = value;
                NotifyPropertyChanged(nameof(GridLowCarbonPercent));
            }
        }
        private GridEnergySources _gridEnergySources;
        public GridEnergySources GridEnergySources
        {
            get { return _gridEnergySources; }
            set
            {
                _gridEnergySources = value;
                NotifyPropertyChanged(nameof(GridEnergySources));
                NotifyPropertyChanged(nameof(GridEnergySourcesList));
            }
        }
        public List<GridEnergySources> GridEnergySourcesList
        {
            get { return _gridEnergySources == null ? null : new List<GridEnergySources>() { _gridEnergySources }; }
        }

        private string _gridEnergySourcesStatusMessage;
        public string GridEnergySourcesStatusMessage
        {
            get { return _gridEnergySourcesStatusMessage; }
            set
            {
                _gridEnergySourcesStatusMessage = value;
                NotifyPropertyChanged(nameof(GridEnergySourcesStatusMessage));
            }
        }

        public Visibility EnergySourcesVisibility
        {
            get 
            {  
                if (Settings.ShowEnergySources)
                {
                    return Visibility.Visible;
                }
                else
                { 
                    return Visibility.Collapsed; 
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
