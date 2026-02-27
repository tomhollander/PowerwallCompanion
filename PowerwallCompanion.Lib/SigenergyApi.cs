using PowerwallCompanion.Lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
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

        public async Task<EnergyChartSeries> GetEnergyChartSeriesForPeriod(string period, DateTime startDate, DateTime endDate, ITariffProvider tariffHelper)
        {
            var url = $"/openapi/systems/{siteId}/history?level={period}&date={startDate.ToString("yyyy-MM-dd")}";
            bool cache = startDate.Date != DateTime.Now.Date; // Don't cache today's data, historical data won't change
            var response = await apiHelper.CallGetApiWithTokenRefresh(url, cache);

            var energyChartSeries = new EnergyChartSeries();
            energyChartSeries.Home = new List<ChartDataPoint>();
            energyChartSeries.Solar = new List<ChartDataPoint>();
            energyChartSeries.GridImport = new List<ChartDataPoint>();
            energyChartSeries.GridExport = new List<ChartDataPoint>();
            energyChartSeries.BatteryCharge = new List<ChartDataPoint>();
            energyChartSeries.BatteryDischarge = new List<ChartDataPoint>();

            foreach (var datapoint in response["itemList"].AsArray())
            {
                var dateString = datapoint["dataTime"].GetValue<string>();
                var date = DateTime.ParseExact(dateString, "yyyyMMdd HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                energyChartSeries.Home.Add(new ChartDataPoint(date, datapoint["powerUse"].GetValue<double>()));
                energyChartSeries.Solar.Add(new ChartDataPoint(date, datapoint["powerGeneration"].GetValue<double>()));
                energyChartSeries.GridImport.Add(new ChartDataPoint(date, datapoint["powerFromGrid"].GetValue<double>()));
                energyChartSeries.GridExport.Add(new ChartDataPoint(date, -datapoint["powerToGrid"].GetValue<double>()));
                energyChartSeries.BatteryCharge.Add(new ChartDataPoint(date, datapoint["esCharging"].GetValue<double>()));
                energyChartSeries.BatteryDischarge.Add(new ChartDataPoint(date, -datapoint["esDischarging"].GetValue<double>()));

            }
            var totalHomeEnergy = response["powerUseKwh"].GetValue<double>();
            energyChartSeries.EnergyTotals = new EnergyTotals
            {
                HomeEnergy = totalHomeEnergy * 1000,
                SolarEnergy = response["powerGenerationKwh"].GetValue<double>() * 1000,
                GridEnergyImported = response["powerFromGridKwh"].GetValue<double>() * 1000,
                GridEnergyExported = response["powerToGridKwh"].GetValue<double>() * 1000,
                BatteryEnergyCharged = response["esChargingKwh"].GetValue<double>() * 1000,
                BatteryEnergyDischarged = response["esDischargingKwh"].GetValue<double>() * 1000,
                SolarUsePercent = (response["powerOneselfKwh"].GetValue<double>() / totalHomeEnergy) * 100,
                BatteryUsePercent = ((totalHomeEnergy - response["powerOneselfKwh"].GetValue<double>() - response["powerFromGridKwh"].GetValue<double>()) / totalHomeEnergy) * 100,
                GridUsePercent = (response["powerFromGridKwh"].GetValue<double>() / totalHomeEnergy) * 100,
                SelfConsumption = (response["powerSelfConsumptionKwh"].GetValue<double>() / totalHomeEnergy) * 100,
            };

            return energyChartSeries;
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
                SolarUsePercent = (response["powerOneselfKwh"].GetValue<double>() / response["powerUseKwh"].GetValue<double>()) * 100,
                BatteryUsePercent = ((response["powerUseKwh"].GetValue<double>() - response["powerOneselfKwh"].GetValue<double>() - response["powerFromGridKwh"].GetValue<double>()) / response["powerUseKwh"].GetValue<double>()) * 100,
                GridUsePercent = (response["powerFromGridKwh"].GetValue<double>() / response["powerUseKwh"].GetValue<double>()) * 100,
                SelfConsumption = (response["powerSelfConsumptionKwh"].GetValue<double>() / response["powerUseKwh"].GetValue<double>()) * 100,
            };
        }

        public async Task<InstantaneousPower> GetInstantaneousPower()
        {
            var url = $"/openapi/systems/{siteId}/energyFlow";
            var response = await apiHelper.CallGetApiWithTokenRefresh(url, false);
            var power =  new InstantaneousPower
            {
                BatteryStoragePercent = Utils.GetValueOrDefault<double>(response["batterySoc"]),
                GridActive = true, // TODO: find a way to determine this 
                HomePower = Utils.GetValueOrDefault<double>(response["loadPower"]) * 1000,
                SolarPower = Utils.GetValueOrDefault<double>(response["pvPower"]) * 1000,
                GridPower = Utils.GetValueOrDefault<double>(response["gridPower"]) * 1000,
                BatteryPower = Utils.GetValueOrDefault<double>(response["batteryPower"]) * -1000,
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
                var batteryPower = -datapoint["esChargeDischargePower"].GetValue<double>();
                var gridPower = datapoint["fromGridPower"].GetValue<double>() - datapoint["toGridPower"].GetValue<double>();
                var homePower = datapoint["loadPower"].GetValue<double>(); 
                powerChartSeries.Home.Add(new ChartDataPoint(timestamp, homePower));
                powerChartSeries.Solar.Add(new ChartDataPoint(timestamp, solarPower));
                powerChartSeries.Grid.Add(new ChartDataPoint(timestamp, gridPower));
                powerChartSeries.Battery.Add(new ChartDataPoint(timestamp, batteryPower));
            }
            return powerChartSeries;
        }

        public async Task<PowerChartSeries> GetPowerChartSeriesForPeriod(string period, DateTime startDate, DateTime endDate, PowerChartType chartType)
        {
            var url = $"/openapi/systems/{siteId}/history?level={period}&date={startDate.ToString("yyyy-MM-dd")}";
            bool cache = startDate.Date != DateTime.Now.Date; // Don't cache today's data, historical data won't change
            var response = await apiHelper.CallGetApiWithTokenRefresh(url, cache);

            var powerChartSeries = new PowerChartSeries();
            powerChartSeries.Solar = new List<ChartDataPoint>();
            powerChartSeries.Grid = new List<ChartDataPoint>();
            powerChartSeries.Battery = new List<ChartDataPoint>();
            powerChartSeries.Home = new List<ChartDataPoint>();


            foreach (var datapoint in response["itemList"].AsArray())
            {
                var dateString = datapoint["dataTime"].GetValue<string>();
                var date = DateTime.ParseExact(dateString, "yyyyMMdd HH:mm", System.Globalization.CultureInfo.InvariantCulture);

                var solarPower = datapoint["pvTotalPower"].GetValue<double>();
                var batteryPower = -datapoint["esChargeDischargePower"].GetValue<double>();
                var gridPower = datapoint["fromGridPower"].GetValue<double>() - datapoint["toGridPower"].GetValue<double>();
                var homePower = datapoint["loadPower"].GetValue<double>();

                if (solarPower == 0 && gridPower == 0 && batteryPower == 0 && homePower == 0)
                    continue; // Likely a future date, but checking dates is tricky due to potential time zone differences.

                var gridImport = datapoint["fromGridPower"].GetValue<double>();
                var gridExport = datapoint["toGridPower"].GetValue<double>();
                var batteryDischarge = datapoint["esDischargePower"].GetValue<double>();
                var batteryCharge = datapoint["esChargePower"].GetValue<double>();

                var gridToHome = gridImport > homePower ? homePower : gridImport;
                var gridToBattery = gridImport > homePower ? (gridImport - homePower) : 0;
                var batteryToHome = batteryDischarge > 0 ?
                    (batteryDischarge > homePower ? homePower : batteryDischarge) :
                    0;
                var batteryToGrid = batteryDischarge < 0 ?
                    (batteryDischarge > homePower ? (batteryDischarge - homePower) : 0) :
                    0;
                var solarToGrid = gridExport > batteryToGrid ? gridExport - batteryToGrid : 0;
                var solarToBattery = batteryCharge - gridToBattery;
                var solarToHome = solarPower - gridExport - solarToBattery;

                if (chartType == PowerChartType.AllData)
                {
                    powerChartSeries.Solar.Add(new ChartDataPoint(date, solarPower));
                    powerChartSeries.Grid.Add(new ChartDataPoint(date, gridPower));
                    powerChartSeries.Battery.Add(new ChartDataPoint(date, batteryPower));
                    powerChartSeries.Home.Add(new ChartDataPoint(date, homePower));
                }
                else if (chartType == PowerChartType.Home)
                {
                    powerChartSeries.Solar.Add(new ChartDataPoint(date, solarToHome));
                    powerChartSeries.Grid.Add(new ChartDataPoint(date, gridToHome));
                    powerChartSeries.Battery.Add(new ChartDataPoint(date, batteryToHome));

                }
                else if (chartType == PowerChartType.Solar)
                {
                    powerChartSeries.Grid.Add(new ChartDataPoint(date, solarToGrid ));
                    powerChartSeries.Battery.Add(new ChartDataPoint(date, solarToBattery));
                    powerChartSeries.Home.Add(new ChartDataPoint(date, solarToHome));
                }
                else if (chartType == PowerChartType.Grid)
                {
                    powerChartSeries.Solar.Add(new ChartDataPoint(date, -solarToGrid ));
                    powerChartSeries.Battery.Add(new ChartDataPoint(date, gridToBattery));
                    powerChartSeries.Home.Add(new ChartDataPoint(date, (gridToHome) ));
                }
                else if (chartType == PowerChartType.Battery)
                {
                    powerChartSeries.Solar.Add(new ChartDataPoint(date, -solarToBattery));
                    powerChartSeries.Grid.Add(new ChartDataPoint(date, -gridToBattery));
                    powerChartSeries.Home.Add(new ChartDataPoint(date, (batteryToHome)));
                }
            }
            return powerChartSeries;
        }

        public async Task<List<ChartDataPoint>> GetBatteryHistoricalChargeLevel(DateTime startDate, DateTime endDate)
        {
            var url = $"/openapi/systems/{siteId}/history?level=day&date={startDate.ToString("yyyy-MM-dd")}";
            var response = await apiHelper.CallGetApiWithTokenRefresh(url, true);

            var socHistory = new List<ChartDataPoint>();
            foreach (var datapoint in response["itemList"].AsArray())
            {
                var dateString = datapoint["dataTime"].GetValue<string>();
                var date = DateTime.ParseExact(dateString, "yyyyMMdd HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                socHistory.Add(new ChartDataPoint(date, datapoint["batSoc"].GetValue<double>()));
            }
            return socHistory;
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
