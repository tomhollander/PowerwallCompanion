using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PowerwallCompanion.Lib.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public double EstimatedCapacity
        {
            get; set;
        }

        public double EstimatedCapacityPercentOfBaseline
        {
            get => WarrantedCapacity == 0 ? 0 : (EstimatedCapacity / WarrantedCapacity) * 100;
        }

        public double EstimatedDegradationPercent
        {
            get => WarrantedCapacity == 0 ? 0 : 100 - EstimatedCapacityPercentOfBaseline;
        }

        public double WarrantedCapacityKWh
        {
            get => WarrantedCapacity / 1000;
        }

        public List<ChartDataPoint> WarrantedCapacityKWhSeries
        {
            get
            {
                if (BatteryHistoryChartData == null || BatteryHistoryChartData.Values.Count == 0 || BatteryHistoryChartData.First().Value == null)
                {
                    return new List<ChartDataPoint> // Default values so the chart doesn't look crap
                    {
                        new ChartDataPoint( new DateTime(2025, 1, 1), 0),
                        new ChartDataPoint( new DateTime(2025, 6, 1), 0)
                    };
                }
                var series = new List<ChartDataPoint>();
                var data = BatteryHistoryChartData.ContainsKey("Estimated") ? BatteryHistoryChartData["Estimated"] : BatteryHistoryChartData.First().Value;
                for (int i = 0; i < data.Count; i++)
                {
                    series.Add(new ChartDataPoint(data[i].XValue, WarrantedCapacityKWh));
                }
                return series;
            }
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

        public List<ChartDataPoint> MinimumWarrantedCapacityKWhSeries
        {
            get
            {
                if (BatteryHistoryChartData == null || BatteryHistoryChartData.Values.Count == 0 || BatteryHistoryChartData.First().Value == null)
                {
                    return new List<ChartDataPoint>();
                }
                var series = new List<ChartDataPoint>();
                var data = BatteryHistoryChartData.ContainsKey("Estimated") ? BatteryHistoryChartData["Estimated"] : BatteryHistoryChartData.First().Value;
                for (int i = 0; i < data.Count; i++)
                {
                    series.Add(new ChartDataPoint(data[i].XValue, MinimumWarrantedCapacityKWh));
                }
                return series;
            }
        }

        public string MinimumWarrantedCapacityPercentageMessage
        {
            get
            {
                return $"{WarrantyProportion * 100}% baseline capacity";
            }
        }

        private Dictionary<string, ObservableCollection<ChartDataPoint>> _batteryHistoryChartData;
        public Dictionary<string, ObservableCollection<ChartDataPoint>> BatteryHistoryChartData
        {
            get => _batteryHistoryChartData;
            set
            {
                if (_batteryHistoryChartData == value)
                {
                    return;
                }
                UnsubscribeFromSourceCollections();
                _batteryHistoryChartData = value;
                SubscribeToSourceCollections();
                RecalculateAllMovingAverages();
                NotifyPropertyChanged(nameof(BatteryHistoryChartData));
                NotifyPropertyChanged(nameof(BatteryHistoryChartDataMovingAverage));
            }
        }

        private readonly Dictionary<string, ObservableCollection<ChartDataPoint>> _batteryHistoryChartDataMovingAverage = new Dictionary<string, ObservableCollection<ChartDataPoint>>();
        private const int MovingAverageWindowSize = 4;

        public Dictionary<string, ObservableCollection<ChartDataPoint>> BatteryHistoryChartDataMovingAverage
        {
            get
            {
                if (BatteryHistoryChartData == null)
                {
                    return null;
                }

                // Ensure any newly added source series are tracked and have a moving average calculated
                foreach (var kvp in BatteryHistoryChartData)
                {
                    if (!_batteryHistoryChartDataMovingAverage.ContainsKey(kvp.Key))
                    {
                        kvp.Value.CollectionChanged += SourceCollectionChanged;
                        RecalculateMovingAverageForKey(kvp.Key, kvp.Value);
                    }
                    else
                    {
                        // Re-subscribe defensively in case the collection instance was replaced
                        kvp.Value.CollectionChanged -= SourceCollectionChanged;
                        kvp.Value.CollectionChanged += SourceCollectionChanged;
                    }
                }

                return _batteryHistoryChartDataMovingAverage;
            }
        }

        private void SubscribeToSourceCollections()
        {
            if (BatteryHistoryChartData == null)
            {
                return;
            }
            foreach (var kvp in BatteryHistoryChartData)
            {
                kvp.Value.CollectionChanged += SourceCollectionChanged;
            }
        }

        private void UnsubscribeFromSourceCollections()
        {
            if (_batteryHistoryChartData == null)
            {
                return;
            }
            foreach (var kvp in _batteryHistoryChartData)
            {
                kvp.Value.CollectionChanged -= SourceCollectionChanged;
            }
        }

        private void SourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Identify key
            if (BatteryHistoryChartData == null)
            {
                return;
            }
            foreach (var kvp in BatteryHistoryChartData)
            {
                if (kvp.Value == sender)
                {
                    RecalculateMovingAverageForKey(kvp.Key, kvp.Value);
                    NotifyPropertyChanged(nameof(BatteryHistoryChartDataMovingAverage));
                    break;
                }
            }
        }

        private void RecalculateAllMovingAverages()
        {
            _batteryHistoryChartDataMovingAverage.Clear();
            if (BatteryHistoryChartData == null)
            {
                return;
            }
            foreach (var kvp in BatteryHistoryChartData)
            {
                RecalculateMovingAverageForKey(kvp.Key, kvp.Value);
            }
        }

        private void RecalculateMovingAverageForKey(string key, ObservableCollection<ChartDataPoint> source)
        {
            if (!_batteryHistoryChartDataMovingAverage.TryGetValue(key, out var target))
            {
                target = new ObservableCollection<ChartDataPoint>();
                _batteryHistoryChartDataMovingAverage[key] = target;
            }

            // Ensure target has same length as source
            while (target.Count > source.Count)
            {
                target.RemoveAt(target.Count - 1);
            }

            for (int i = 0; i < source.Count; i++)
            {
                int start = Math.Max(0, i - MovingAverageWindowSize + 1);
                double sum = 0;
                int count = 0;
                for (int j = start; j <= i; j++)
                {
                    sum += source[j].YValue;
                    count++;
                }
                double average = count == 0 ? 0 : sum / count;

                if (i < target.Count)
                {
                    // Update existing item to preserve bindings
                    var item = target[i];
                    // Replace to ensure X/Y updates are reflected even if ChartDataPoint does not implement INotifyPropertyChanged
                    target[i] = new ChartDataPoint(source[i].XValue, average);
                }
                else
                {
                    target.Add(new ChartDataPoint(source[i].XValue, average));
                }
            }
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

        public Visibility ShowEstimatedData
        {
            get => Settings.EstimateBatteryCapacity? Visibility.Visible : Visibility.Collapsed;
        }

        public Visibility ShowGatewayData
        {
            get => Settings.UseLocalGatewayForBatteryCapacity ? Visibility.Visible : Visibility.Collapsed;
        }

        private EnergySiteInfo _energySiteInfo;
        public EnergySiteInfo EnergySiteInfo
        {
            get => _energySiteInfo;
            set
            {
                _energySiteInfo = value;
                NotifyPropertyChanged(nameof(EnergySiteInfo));
            }
        }

        private Visibility _loadingStateVisibility = Visibility.Collapsed;
        public Visibility LoadingStateVisibility
        {
            get => _loadingStateVisibility;
            set
            {
                _loadingStateVisibility = value;
                NotifyPropertyChanged(nameof(LoadingStateVisibility));
            }
        }

        public DateTime LastUpdate { get; set; } = DateTime.MinValue;

        private bool _previousSettingsUseLocalGatewayForBatteryCapacity = Settings.UseLocalGatewayForBatteryCapacity;
        private bool _previousSettingsEstimateBatteryCapacity = Settings.EstimateBatteryCapacity;
        private bool _previousSettingsStoreBatteryHistory = Settings.StoreBatteryHistory;
        private bool _previousSettingsUseMovingAveragesForBatteryCapacity = Settings.UseMovingAveragesForBatteryCapacity;
        private string _previousSettingsGatewayIP = Settings.LocalGatewayIP;
        private string _previousSettingsGatewayPassword = Settings.LocalGatewayPassword;

        public bool SettingsHaveChanged()
        {
            bool changed = _previousSettingsUseLocalGatewayForBatteryCapacity != Settings.UseLocalGatewayForBatteryCapacity ||
                           _previousSettingsEstimateBatteryCapacity != Settings.EstimateBatteryCapacity ||
                           _previousSettingsStoreBatteryHistory != Settings.StoreBatteryHistory ||
                           _previousSettingsGatewayIP != Settings.LocalGatewayIP ||
                           _previousSettingsGatewayPassword != Settings.LocalGatewayPassword ||
                           _previousSettingsUseMovingAveragesForBatteryCapacity != Settings.UseMovingAveragesForBatteryCapacity;
            return changed;
        }


        public void NotifyAllProperties()
        {
            NotifyPropertyChanged(nameof(EnergySiteInfo));
            NotifyPropertyChanged(nameof(BatteryDetails));
            NotifyPropertyChanged(nameof(ShowGatewayData));
            NotifyPropertyChanged(nameof(ShowEstimatedData));
        }

        public void NotifyChartProperties()
        {
            NotifyPropertyChanged(nameof(StoreBatteryHistory));
            NotifyPropertyChanged(nameof(ShowChart));
            NotifyPropertyChanged(nameof(EnoughDataToShowChart));
            NotifyPropertyChanged(nameof(BatteryHistoryChartData));
            NotifyPropertyChanged(nameof(BatteryHistoryChartDataMovingAverage));
            NotifyPropertyChanged(nameof(ShowNotEnoughDataMessage));
            NotifyPropertyChanged(nameof(WarrantedCapacityKWh));
            NotifyPropertyChanged(nameof(MinimumWarrantedCapacityKWh));
            NotifyPropertyChanged(nameof(WarrantedCapacityKWhSeries));
            NotifyPropertyChanged(nameof(MinimumWarrantedCapacityKWhSeries));
        }

        public void NotifyEstimatedCapacityProperties()
        {
            NotifyPropertyChanged(nameof(EstimatedCapacity));
            NotifyPropertyChanged(nameof(EstimatedCapacityPercentOfBaseline));
            NotifyPropertyChanged(nameof(EstimatedDegradationPercent));
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
