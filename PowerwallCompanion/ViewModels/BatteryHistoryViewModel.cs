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
                var soeInfo = await ApiHelper.CallGetApiWithTokenRefresh($"{ApiHelper.BaseUrl}/api/1/energy_sites/{Settings.SiteId}/calendar_history?kind=soe", "BatteryHistory");
                var soeSeries = (JArray)soeInfo["response"]["time_series"];

                var batteryHistory = new List<BatteryHistoryPoint>();
               
                for (int i=0; i< soeSeries.Count; i++)
                {
                    batteryHistory.Add(new BatteryHistoryPoint()
                    {
                        Timestamp = soeSeries[i]["timestamp"].Value<DateTime>(),
                        EnergyPercent = soeSeries[i]["soe"].Value<double>(),
                    }); 

                }
                BatteryHistory = batteryHistory;
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

        public double EnergyPercent
        {
            get; set; 
        }
    }
}
