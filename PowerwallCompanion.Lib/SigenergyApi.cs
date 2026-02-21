using PowerwallCompanion.Lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PowerwallCompanion.Lib
{
    public class SigenergyApi : IEnergyAPI
    {
        private string siteId;
        private IPlatformAdapter platformAdapter;
        private SigenergyApiHelper apiHelper;

        public SigenergyApi(string siteId, IPlatformAdapter platformAdapter)
        {
            this.siteId = siteId;
            this.platformAdapter = platformAdapter;
            this.apiHelper = new SigenergyApiHelper("aus", platformAdapter);
        }


        public DateTime ConvertToPowerwallDate(DateTime date)
        {
            return date; // TODO: check if timezone conversion is needed here
        }

        public Task ExportEnergyDataToCsv(Stream stream, DateTime startDate, DateTime endDate, string period, ITariffProvider tariffHelper)
        {
            throw new NotImplementedException();
        }

        public Task ExportPowerDataToCsv(Stream stream, DateTime startDate, DateTime endDate)
        {
            throw new NotImplementedException();
        }

        public Task<List<ChartDataPoint>> GetBatteryHistoricalChargeLevel(DateTime startDate, DateTime endDate)
        {
            throw new NotImplementedException();
        }

        public async Task<Tuple<double, double>> GetBatteryMinMaxToday()
        {
            var url = $"/openapi/systems/{siteId}/history?level=day&date={DateTime.Now.ToString("yyyy-MM-dd")}";
            var response = await apiHelper.CallGetApiWithTokenRefresh(url, true);
            var timeSeries = response["itemList"].AsArray();
            double min = double.MaxValue;
            double max = double.MinValue;
            foreach (var t in timeSeries)
            {
                var chargeLevel = t["batSoc"].GetValue<double>();
                if (chargeLevel < min)
                {
                    min = chargeLevel;
                }
                if (chargeLevel > max)
                {
                    max = chargeLevel;
                }
            }
            return new Tuple<double, double>(min, max);
        }

        public Task<EnergyChartSeries> GetEnergyChartSeriesForPeriod(string period, DateTime startDate, DateTime endDate, ITariffProvider tariffHelper)
        {
            throw new NotImplementedException();
        }

        public async Task<EnergySiteInfo> GetEnergySiteInfo()
        {
            return new EnergySiteInfo(); // TODO, need to subclass this per provider
        }

        public async Task<EnergyTotals> GetEnergyTotalsForDay(int dateOffset, ITariffProvider tariffHelper)
        {
            var nowDate = DateTime.Now.Date;
            var offsetDate = nowDate.Date.AddDays(dateOffset);

            return await GetEnergyTotalsForPeriod(offsetDate, offsetDate.AddDays(1).AddSeconds(-1), "day", tariffHelper);
        }

        public async Task<EnergyTotals> GetEnergyTotalsForPeriod(DateTime startDate, DateTime endDate, string period, ITariffProvider tariffHelper)
        {
            var url = $"/openapi/systems/{siteId}/history?level={period}&date={startDate.ToString("yyyy-MM-dd")}";
            bool cache = startDate.Date != DateTime.Now.Date; // Don't cache today's data, historical data won't change
            var response = await apiHelper.CallGetApiWithTokenRefresh(url, cache);
            return new EnergyTotals
            {
                HomeEnergy = response["powerUseKwh"].GetValue<double>() * 1000,
                SolarEnergy = response["powerGenerationKwh"].GetValue<double>() * 1000,
                GridEnergyImported = response["powerFromGridKwh"].GetValue<double>() * 1000,
                GridEnergyExported = response["powerToGridKwh"].GetValue<double>() * 1000,
                BatteryEnergyCharged = response["esChargingKwh"].GetValue<double>() * 1000,
                BatteryEnergyDischarged = response["esDischargingKwh"].GetValue<double>() * 1000,
            };
        }

        public async Task<InstantaneousPower> GetInstantaneousPower()
        {
            var url = $"/openapi/systems/{siteId}/energyFlow";
            var response = await apiHelper.CallGetApiWithTokenRefresh(url, false);
            var power =  new InstantaneousPower
            {
                BatteryStoragePercent = response["batterySoc"].GetValue<double>(),
                GridActive = true, // TODO: find a way to determine this 
                HomePower = (response["loadPower"].GetValue<double>() + response["evPower"].GetValue<double>() + response["heatPumpPower"].GetValue<double>()) * 1000,
                SolarPower = response["pvPower"].GetValue<double>() * 1000,
                GridPower = response["gridPower"].GetValue<double>() * 1000,
                BatteryPower = response["batteryPower"].GetValue<double>() * -1000,
                StormWatchActive = false,
            };
            if (power.GridPower == 0)
            {
                // API doesn't show feed-in, need to calculate it
                power.GridPower = power.SolarPower - power.HomePower + power.BatteryPower;
            }
            return power;
        }

        public async Task<PowerChartSeries> GetPowerChartSeriesForLastTwoDays()
        {
            // TODO: This uses the same API response as the GetEnergyTotalsForPeriod, so we should optimize this to only call the API once if both methods are called in the same period
            var yesterdayUrl = $"/openapi/systems/{siteId}/history?level=day&date={DateTime.Now.Date.AddDays(-1).ToString("yyyy-MM-dd")}";
            var todayUrl = $"/openapi/systems/{siteId}/history?level=day&date={DateTime.Now.Date.ToString("yyyy-MM-dd")}";

            var yesterdayTask = apiHelper.CallGetApiWithTokenRefresh(yesterdayUrl, true);
            var todayTask = apiHelper.CallGetApiWithTokenRefresh(todayUrl, false);

            var tasks = new List<Task<JsonObject>>()
            {
                yesterdayTask, todayTask
            };
            await Task.WhenAll(tasks);
            // Combine results
            // Get them in order so graph isn't screwy
            var results = new List<JsonObject>()
            {
                yesterdayTask.Result,
                todayTask.Result
            };
            var combinedTimeSeries = new JsonArray();
            foreach (var result in results)
            {
                foreach (var item in result["itemList"].AsArray())
                {
                    combinedTimeSeries.Add(item.DeepClone());
                }
            }

            var powerChartSeries = new PowerChartSeries();
            powerChartSeries.Home = new List<ChartDataPoint>();
            powerChartSeries.Solar = new List<ChartDataPoint>();
            powerChartSeries.Grid = new List<ChartDataPoint>();
            powerChartSeries.Battery = new List<ChartDataPoint>();

            foreach (var datapoint in combinedTimeSeries)
            {
                var dateString = datapoint["dataTime"].GetValue<string>();
                var timestamp = DateTime.ParseExact(dateString, "yyyyMMdd HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                var solarPower = datapoint["pvTotalPower"].GetValue<double>();
                var batteryPower = datapoint["esChargePower"].GetValue<double>() + datapoint["esDischargePower"].GetValue<double>();
                var gridPower = datapoint["fromGridPower"].GetValue<double>() - datapoint["toGridPower"].GetValue<double>();
                var homePower = datapoint["loadPower"].GetValue<double>(); 
                powerChartSeries.Home.Add(new ChartDataPoint(timestamp, homePower));
                powerChartSeries.Solar.Add(new ChartDataPoint(timestamp, solarPower));
                powerChartSeries.Grid.Add(new ChartDataPoint(timestamp, gridPower));
                powerChartSeries.Battery.Add(new ChartDataPoint(timestamp, batteryPower));
            }
            return powerChartSeries;
        }

        public Task<PowerChartSeries> GetPowerChartSeriesForPeriod(string period, DateTime startDate, DateTime endDate, PowerChartType chartType)
        {
            throw new NotImplementedException();
        }

        public Task<JsonObject> GetRatePlan()
        {
            throw new NotImplementedException();
        }

        public async Task StoreInstallationTimeZone()
        {
            // No need to store timezone as the API returns all times in local time, so we can just treat them as local times without needing to convert
        }
    }
}
