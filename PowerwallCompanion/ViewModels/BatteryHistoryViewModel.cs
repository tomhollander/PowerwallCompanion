using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PowerwallCompanion.ViewModels
{
    public class BatteryHistoryViewModel : INotifyPropertyChanged
    {
        public List<BatteryHistoryPoint> BatteryHistory { get; set; }

        public static double TotalPackEnergy { get;  set; }

        public async Task RefreshData()
        {
            try
            {
                var powerInfo = await ApiHelper.CallGetApiWithTokenRefresh($"{ApiHelper.BaseUrl}/api/1/energy_sites/{Settings.SiteId}/live_status", "LiveStatus");
                TotalPackEnergy = powerInfo["response"]["total_pack_energy"].Value<double>();

                BatteryHistory = new List<BatteryHistoryPoint>();
                BatteryHistory.Add(new BatteryHistoryPoint()
                {
                    Timestamp = powerInfo["response"]["timestamp"].Value<DateTime>(),
                    EnergyLeft = powerInfo["response"]["energy_left"].Value<double>()
                });

                var powerHistoryJson = await ApiHelper.CallGetApiWithTokenRefresh($"{ApiHelper.BaseUrl}/api/1/energy_sites/{Settings.SiteId}/history?kind=power", "PowerHistory");
                var powerHistory = JsonConvert.DeserializeObject<List<TimeSeriesPoint>>(((JArray)(powerHistoryJson["response"]["time_series"])).ToString());

                for (int i = powerHistory.Count - 1; i >= 0; i--)
                {
                    var mostRecentPoint = BatteryHistory.First();
                    double energyForLastPoint = mostRecentPoint.EnergyLeft + (powerHistory[i].BatteryPower / 12); // 5 min intervals
                    if (powerHistory[i].BatteryPower > 0)
                    {
                        energyForLastPoint = energyForLastPoint + 8; // fix??
                    }
                    if (energyForLastPoint > TotalPackEnergy)
                    {
                        energyForLastPoint = TotalPackEnergy;
                    }
                    else if (energyForLastPoint < 0)
                    {
                        energyForLastPoint = 0;
                    }

                    var newPoint = new BatteryHistoryPoint()
                    {
                        Timestamp = powerHistory[i].Timestamp,
                        EnergyLeft = energyForLastPoint
                    };
                    BatteryHistory.Insert(0, newPoint);

                }
                DataLastUpdated = DateTime.Now;
                this.StatusOK = true;
            }
            catch (Exception ex)
            {
                this.LastExceptionDate = DateTime.Now;
                this.LastExceptionMessage = ex.Message;
                this.StatusOK = false;
            }
            NotifyPropertyChanged(nameof(BatteryHistory));
        }

        private bool _statusOK;
        public bool StatusOK
        {
            get { return _statusOK; }
            set
            {
                _statusOK = value;
                NotifyPropertyChanged(nameof(StatusOK));
            }
        }

        public string LastExceptionMessage { get; set; }
        public DateTime LastExceptionDate { get; set; }

        public DateTime DataLastUpdated
        {
            get; set;
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

   

    public class BatteryHistoryPoint
    {
        public DateTime Timestamp { get; set; }
        public double EnergyLeft { get; set; }
        public double EnergyPercent
        {
            get
            {
                return (EnergyLeft / BatteryHistoryViewModel.TotalPackEnergy) * 100;
            }
        }
    }
}
