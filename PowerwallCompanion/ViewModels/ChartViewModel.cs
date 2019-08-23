using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerwallCompanion.ViewModels
{
    public class ChartViewModel : INotifyPropertyChanged
    {
        public List<TimeSeriesPoint> Series { get; set; }

        public List<TimeSeriesPoint> TodaySeries 
        {
            get { return Series.Where(p => p.Timestamp >= DateTime.Now.Date).ToList(); }
        }

        public List<TimeSeriesPoint> YesterdaySeries
        {
            get { return Series.Where(p => p.Timestamp < DateTime.Now.Date).ToList(); }
        }

        private List<TimeSeriesPoint> _selectedSeries;
        public List<TimeSeriesPoint> SelectedSeries
        {
            get { return _selectedSeries; }
            set
            {
                _selectedSeries = value;
                NotifyPropertyChanged(nameof(SelectedSeries));
            }
        }

        private List<EnergyHistoryPoint> _energyHistory;
        public List<EnergyHistoryPoint> EnergyHistory
        {
            get { return _energyHistory; }
            set
            {
                _energyHistory = value;
                NotifyPropertyChanged(nameof(EnergyHistory));
            }
        }

        public List<EnergyHistoryPoint> EnergyHistoryWeek { get; set; }
        public List<EnergyHistoryPoint> EnergyHistoryMonth { get; set; }
        public List<EnergyHistoryPoint> EnergyHistoryYear { get; set; }

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

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public ConsumptionData SelfConsumptionToday { get; set; }
        public ConsumptionData SelfConsumptionYesterday { get; set; }
        public ConsumptionData SelfConsumptionWeek { get; set; }
        public ConsumptionData SelfConsumptionMonth { get; set; }
        public ConsumptionData SelfConsumptionYear { get; set; }

        private ConsumptionData _selfConsumption;
        public ConsumptionData SelfConsumption
        {
            get { return _selfConsumption; }
            set { _selfConsumption = value;
                NotifyPropertyChanged(nameof(SelfConsumption));
            }
        }

        public async Task LoadPowerGraphData()
        {
            try
            {
                this.StatusOK = true;
                this.Series = new List<TimeSeriesPoint>();

                var json = await ApiHelper.CallGetApiWithTokenRefresh($"{ApiHelper.BaseUrl}/api/1/energy_sites/{Settings.SiteId}/history?kind=power", "PowerHistory");

                this.Series = JsonConvert.DeserializeObject<List<TimeSeriesPoint>>(((JArray)(json["response"]["time_series"])).ToString());

                this.Series.Add(new TimeSeriesPoint() { Timestamp = DateTime.Now.AddMinutes(1), IsDummy = true }); // Dummy to force a full day
                this.Series.Add(new TimeSeriesPoint() { Timestamp = DateTime.Now.Date.AddDays(1), IsDummy = true }); // Dummy to force a full day

                NotifyPropertyChanged(nameof(Series));
                DataLastUpdated = DateTime.Now;
                
            }
            catch (Exception ex)
            {
                this.StatusOK = false;
                this.LastExceptionDate = DateTime.Now;
                this.LastExceptionMessage = ex.Message;

                DataLastUpdated = DateTime.MinValue;
                throw;
            }
        }

        public DateTime DataLastUpdated
        {
            get; set;
        }

        public async Task LoadTodaySelfConsumptionData()
        {
            try
            {
                this.StatusOK = true;

                var json = await ApiHelper.CallGetApiWithTokenRefresh($"{ApiHelper.BaseUrl}/api/1/energy_sites/{Settings.SiteId}/history?kind=self_consumption&period=day", "SelfConsumptionHistory");
                this.SelfConsumptionYesterday = new ConsumptionData()
                {
                    Solar = json["response"]["time_series"][0]["solar"].Value<Double>(),
                    Battery = json["response"]["time_series"][0]["battery"].Value<Double>()
                };
                this.SelfConsumptionToday = new ConsumptionData()
                {
                    Solar = json["response"]["time_series"][1]["solar"].Value<Double>(),
                    Battery = json["response"]["time_series"][1]["battery"].Value<Double>()
                };

                NotifyPropertyChanged(nameof(SelfConsumption));
            }
            catch (Exception ex)
            {
                this.StatusOK = false;
                this.LastExceptionDate = DateTime.Now;
                this.LastExceptionMessage = ex.Message;
                throw;
            }
        }


        public async Task<ConsumptionData> GetSelfConsumptionData(string period)
        {
            try
            {
                this.StatusOK = true;

                var json = await ApiHelper.CallGetApiWithTokenRefresh($"{ApiHelper.BaseUrl}/api/1/energy_sites/{Settings.SiteId}/history?kind=self_consumption&period={period}", "SelfConsumptionHistory");
                if (((JArray)json["response"]["time_series"]).Count > 0)
                {
                    return new ConsumptionData()
                    {
                        Solar = json["response"]["time_series"][0]["solar"].Value<Double>(),
                        Battery = json["response"]["time_series"][0]["battery"].Value<Double>()
                    };
                }
                return new ConsumptionData() { Solar = 0, Battery = 0 } ;
            }
            catch (Exception ex)
            {
                this.StatusOK = false;
                this.LastExceptionDate = DateTime.Now;
                this.LastExceptionMessage = ex.Message;
                throw;
            }
        }

        public async Task<List<EnergyHistoryPoint>> GetEnergyHistoryData(string period)
        {
            try
            {
                this.StatusOK = true;
       
                var json = await ApiHelper.CallGetApiWithTokenRefresh($"{ApiHelper.BaseUrl}/api/1/energy_sites/{Settings.SiteId}/history?kind=energy&period={period}", "EnergyHistory" + period);

                return JsonConvert.DeserializeObject<List<EnergyHistoryPoint>>(((JArray)(json["response"]["time_series"])).ToString());

            }
            catch (Exception ex)
            {
                this.StatusOK = false;
                this.LastExceptionDate = DateTime.Now;
                this.LastExceptionMessage = ex.Message;
                throw;
            }
        }
    }



    public class TimeSeriesPoint
    {
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("battery_power")]
        public double BatteryPower { get; set; }

        [JsonProperty("grid_power")]
        public double GridPower { get; set; }

        [JsonProperty("solar_power")]
        public double SolarPower { get; set; }

        public double LoadPower
        {
            get
            {
                return BatteryPower + GridPower + SolarPower;
            }
        }

        public bool IsDummy { get; set; }

    }

    public class ConsumptionData
    {
        public double Solar { get; set; }
        public double Battery { get; set; }
        public double Grid {  get { return 100 - Solar - Battery;  } }
        public double Self {  get { return Solar + Battery;  } }
    }

    public class EnergyHistoryPoint
    {
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        // Green positive bar
        [JsonProperty("battery_energy_exported")]
        public double BatteryEnergyExported { get; set; }

        [JsonProperty("battery_energy_imported_from_grid")]
        public double BatteryEnergyImportedFromGrid { get; set; }

        [JsonProperty("battery_energy_imported_from_solar")]
        public double BatteryEnergyImportedFromSolar { get; set; }

        [JsonProperty("consumer_energy_imported_from_battery")]
        public double ConsumerEnergyImportedFromBattery { get; set; }

        [JsonProperty("consumer_energy_imported_from_grid")]
        public double ConsumerEnergyImportedFromGrid { get; set; }

        [JsonProperty("consumer_energy_imported_from_solar")]
        public double ConsumerEnergyImportedFromSolar { get; set; }

        [JsonProperty("grid_energy_exported_from_battery")]
        public double GridEnergyExportedFromBattery { get; set; }

        [JsonProperty("grid_energy_exported_from_solar")]
        public double GridEnergyExportedFromSolar { get; set; }

        // Grey positive bar
        [JsonProperty("grid_energy_imported")]
        public double GridEnergyImported { get; set; }

        [JsonProperty("solar_energy_exported")]
        public double SolaryEnergyExported { get; set; }

        // Blue positive bar
        public double TotalHomeUse
        {
            get { return ConsumerEnergyImportedFromBattery + ConsumerEnergyImportedFromGrid + ConsumerEnergyImportedFromSolar; }
        }

        // Yellow positive bar
        public double TotalSolarGenerated
        {
            get { return BatteryEnergyImportedFromSolar + ConsumerEnergyImportedFromSolar + GridEnergyExportedFromSolar; }
        }

        // Green negative bar
        public double TotalBatteryImported
        {
            get { return -(BatteryEnergyImportedFromGrid + BatteryEnergyImportedFromSolar); }
        }

        // Grey negative bar
        public double TotalGridExported
        {
            get { return -(GridEnergyExportedFromSolar + GridEnergyExportedFromBattery); }
        }

    }
}
