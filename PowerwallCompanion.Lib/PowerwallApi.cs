using PowerwallCompanion.Lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace PowerwallCompanion.Lib
{
    public class PowerwallApi
    {
        private string siteId;
        private IPlatformAdapter platformAdapter;
        private IApiHelper apiHelper;
        private JsonObject productResponse;

        public PowerwallApi(string siteId, IPlatformAdapter platformAdapter, IApiHelper apiHelper)
        {
            this.siteId = siteId;
            this.platformAdapter = platformAdapter;
            this.apiHelper = apiHelper;
        }

        public PowerwallApi(string siteId, IPlatformAdapter platformAdapter)
        {
            this.siteId = siteId;
            this.platformAdapter = platformAdapter;
            if (platformAdapter.AccessToken == "DEMO")
            {
                this.apiHelper = new DemoApiHelper(platformAdapter);
            }
            else
            {
                this.apiHelper = new ApiHelper(platformAdapter);
            }
        }
   

        private async Task<JsonObject> GetProductResponse()
        {
            if (productResponse == null)
            {
                productResponse = await apiHelper.CallGetApiWithTokenRefresh("/api/1/products");
            }
            return productResponse;
        }
        public async Task<string> GetFirstSiteId()
        {
            var productsResponse = await GetProductResponse();
            var availableSites = new Dictionary<string, string>();
            foreach (var product in productsResponse["response"].AsArray())
            {
                if (product["resource_type"]?.GetValue<string>() == "battery" && product["energy_site_id"] != null)
                {
                    var id = product["energy_site_id"].GetValue<long>();
                    return id.ToString();
                }
            }

            throw new Exception("Powerwall site not found");
        }

        public async Task<Dictionary<string, string>> GetEnergySites()
        {
            var productsResponse = await GetProductResponse();
            var availableSites = new Dictionary<string, string>();
            foreach (var product in productsResponse["response"].AsArray())
            {
                if (product["resource_type"]?.GetValue<string>() == "battery" && product["energy_site_id"] != null)
                {
                    var siteName = product["site_name"].GetValue<string>();
                    var id = product["energy_site_id"].GetValue<long>();
                    availableSites.Add(id.ToString(), siteName);

                }
            }
            return availableSites;
        }

        public async Task<InstantaneousPower> GetInstantaneousPower()
        {
            var powerInfo = await apiHelper.CallGetApiWithTokenRefresh($"/api/1/energy_sites/{siteId}/live_status");
            var instantaneousPower = new InstantaneousPower();

            instantaneousPower.BatteryStoragePercent = Utils.GetValueOrDefault<double>(powerInfo["response"]["percentage_charged"]);
            instantaneousPower.HomePower = Utils.GetValueOrDefault<double>(powerInfo["response"]["load_power"]);
            instantaneousPower.SolarPower = Utils.GetValueOrDefault<double>(powerInfo["response"]["solar_power"]);
            instantaneousPower.BatteryPower = Utils.GetValueOrDefault<double>(powerInfo["response"]["battery_power"]);
            instantaneousPower.GridPower = Utils.GetValueOrDefault<double>(powerInfo["response"]["grid_power"]);
            instantaneousPower.GridActive = Utils.GetValueOrDefault<string>(powerInfo["response"]["grid_status"]) != "Inactive";
            instantaneousPower.StormWatchActive = Utils.GetValueOrDefault<bool>(powerInfo["response"]["storm_mode_active"]);
            return instantaneousPower;
        }

        public async Task<Tuple<double, double>> GetBatteryMinMaxToday()
        {
  
            var json = await apiHelper.CallGetApiWithTokenRefresh($"/api/1/energy_sites/{siteId}/calendar_history?kind=soe&period=day");
            int min = 100;
            int max = 0;
            foreach (var datapoint in (JsonArray)json["response"]["time_series"])
            {
                var timestamp = ConvertToPowerwallDate(datapoint["timestamp"].GetValue<DateTime>());
                if (timestamp.Date == (ConvertToPowerwallDate(DateTime.Now)).Date)
                {
                    var soe = datapoint["soe"].GetValue<int>();
                    if (soe < min) min = soe;
                    if (soe > max) max = soe;

                }
            }
            return new Tuple<double, double>((double)min, (double)max);
            
        }

        private TimeZoneInfo GetInstallationTimeZone()
        {
            return TZConvert.GetTimeZoneInfo(platformAdapter.InstallationTimeZone);
        }

        public async Task<EnergyTotals> GetEnergyTotalsForDay(int dateOffset, ITariffProvider tariffHelper)
        {
            var tzInfo = GetInstallationTimeZone();
            var nowDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzInfo);
            var offsetDate = nowDate.Date.AddDays(dateOffset);

            return await GetEnergyTotalsForPeriod(offsetDate, offsetDate.AddDays(1).AddSeconds(-1), "day", tariffHelper);
        }

        public async Task<EnergyTotals> GetEnergyTotalsForPeriod(DateTime startDate, DateTime endDate, string period, ITariffProvider tariffHelper)
        {
            var url = Utils.GetCalendarHistoryUrl(siteId, platformAdapter.InstallationTimeZone, "energy", period, startDate, endDate);

            var energyHistory = await apiHelper.CallGetApiWithTokenRefresh(url);
            double totalHomeFromGrid = 0;
            double totalHomeFromSolar = 0;
            double totalHomeFromBattery = 0;
            double totalBatteryFromSolar = 0;

            var energyTotals = new EnergyTotals();

            if (energyHistory["response"]["time_series"] == null)
            {
                return energyTotals;
            }

            foreach (var item in energyHistory["response"]["time_series"].AsArray())
            {
                energyTotals.HomeEnergy += Utils.GetValueOrDefault<double>(item["total_home_usage"]);
                energyTotals.SolarEnergy += Utils.GetValueOrDefault<double>(item["total_solar_generation"]);
                energyTotals.GridEnergyImported += Utils.GetValueOrDefault<double>(item["grid_energy_imported"]);
                energyTotals.GridEnergyExported += Utils.GetValueOrDefault<double>(item["grid_energy_exported_from_solar"]) + Utils.GetValueOrDefault<double>(item["grid_energy_exported_from_generator"]) + Utils.GetValueOrDefault<double>(item["grid_energy_exported_from_battery"]);
                energyTotals.BatteryEnergyCharged += Utils.GetValueOrDefault<double>(item["battery_energy_imported_from_grid"]) + Utils.GetValueOrDefault<double>(item["battery_energy_imported_from_solar"]) + Utils.GetValueOrDefault<double>(item["battery_energy_imported_from_generator"]);
                energyTotals.BatteryEnergyDischarged += Utils.GetValueOrDefault<double>(item["battery_energy_exported"]);

                // Totals for self consumption calcs 
                totalHomeFromGrid += Utils.GetValueOrDefault<double>(item["consumer_energy_imported_from_grid"]) + Utils.GetValueOrDefault<double>(item["consumer_energy_imported_from_generator"]);
                totalHomeFromSolar += Utils.GetValueOrDefault<double>(item["consumer_energy_imported_from_solar"]);
                totalHomeFromBattery += Utils.GetValueOrDefault<double>(item["consumer_energy_imported_from_battery"]);
                totalBatteryFromSolar += Utils.GetValueOrDefault<double>(item["battery_energy_imported_from_solar"]);
            }

            energyTotals.SolarUsePercent = (totalHomeFromSolar / energyTotals.HomeEnergy) * 100;
            energyTotals.BatteryUsePercent = (totalHomeFromBattery / energyTotals.HomeEnergy) * 100;
            energyTotals.GridUsePercent = (totalHomeFromGrid / energyTotals.HomeEnergy) * 100;
            energyTotals.SelfConsumption = energyTotals.HomeEnergy == 0 ? 0 : (1 - ((energyTotals.GridEnergyImported - energyTotals.GridEnergyExported) / energyTotals.HomeEnergy)) * 100;

            if (tariffHelper != null)
            {
                try
                {
                    var dailyCosts = await tariffHelper.GetEnergyCostAndFeedInFromEnergyHistory(energyHistory["response"]["time_series"].AsArray().ToList());
                    energyTotals.EnergyCost = dailyCosts.Item1;
                    energyTotals.EnergyFeedIn = dailyCosts.Item2;
                }
                catch
                { 
                    // TODO: Telemetry
                }
            }

            return energyTotals;
        }
        public async Task<JsonObject> GetRatePlan()
        {
            return await apiHelper.CallGetApiWithTokenRefresh($"/api/1/energy_sites/{siteId}/tariff_rate");
        }
                  
        public async Task<PowerChartSeries> GetPowerChartSeriesForLastTwoDays()
        {
            var json = await apiHelper.CallGetApiWithTokenRefresh($"/api/1/energy_sites/{siteId}/history?kind=power");

            var powerChartSeries = new PowerChartSeries();
            powerChartSeries.Home = new List<ChartDataPoint>();
            powerChartSeries.Solar = new List<ChartDataPoint>();
            powerChartSeries.Grid =  new List<ChartDataPoint>();
            powerChartSeries.Battery = new List<ChartDataPoint>();

            foreach (var datapoint in (JsonArray)json["response"]["time_series"])
            {
                var timestamp = Utils.GetUnspecifiedDateTime(datapoint["timestamp"]);
                var solarPower = datapoint["solar_power"].GetValue<double>() / 1000;
                var batteryPower = datapoint["battery_power"].GetValue<double>() / 1000;
                var gridPower = datapoint["grid_power"].GetValue<double>() / 1000;
                var homePower = solarPower + batteryPower + gridPower;
                powerChartSeries.Home.Add(new ChartDataPoint(timestamp, homePower));
                powerChartSeries.Solar.Add(new ChartDataPoint(timestamp, solarPower));
                powerChartSeries.Grid.Add(new ChartDataPoint(timestamp, gridPower));
                powerChartSeries.Battery.Add(new ChartDataPoint(timestamp, batteryPower));
            }
            return powerChartSeries;
        }

        public enum PowerChartType
        {
            AllData,
            Home,
            Solar,
            Grid, 
            Battery
        }
        public async Task<PowerChartSeries> GetPowerChartSeriesForPeriod(string period, DateTime startDate, DateTime endDate, PowerChartType chartType)
        {
            var url = Utils.GetCalendarHistoryUrl(siteId, platformAdapter.InstallationTimeZone, "power", period, startDate, endDate);
            var json = await apiHelper.CallGetApiWithTokenRefresh(url);

            var powerChartSeries = new PowerChartSeries();
            powerChartSeries.Solar = new List<ChartDataPoint>();
            powerChartSeries.Grid = new List<ChartDataPoint>();
            powerChartSeries.Battery = new List<ChartDataPoint>();
            powerChartSeries.Home = new List<ChartDataPoint>();

            if (json["response"].GetValueKind() == JsonValueKind.String || json["response"]["time_series"] == null)
            {
                return powerChartSeries;
            }

            foreach (var data in json["response"]["time_series"].AsArray())
            {
                var date = Utils.GetUnspecifiedDateTime(data["timestamp"]);

                var solarPower = Utils.GetValueOrDefault<double>(data["solar_power"]);
                var gridPower = Utils.GetValueOrDefault<double>(data["grid_power"]);
                var batteryPower = Utils.GetValueOrDefault<double>(data["battery_power"]);
                var homePower = solarPower + gridPower + batteryPower;

                if (solarPower == 0 && gridPower == 0 && batteryPower == 0 && homePower == 0)
                    continue; // Likely a future date, but checking dates is tricky due to potential time zone differences.

                // Calcs somewhat dervied from https://raw.githubusercontent.com/reptilex/tesla-style-solar-power-card/master/README.md
                var gridImport = gridPower >= 0 ? gridPower : 0;
                var gridExport = gridPower < 0 ? -gridPower : 0;
                var batteryDischarge = batteryPower >= 0 ? batteryPower : 0;
                var batteryCharge = batteryPower < 0 ? -batteryPower : 0;

                var gridToHome = gridImport > homePower ? homePower : gridImport;
                var gridToBattery = gridImport > homePower ? (gridImport - homePower) : 0;
                var batteryToHome = batteryDischarge > 0 ?
                    (batteryDischarge > homePower ? homePower : batteryDischarge) :
                    0;
                var batteryToGrid = batteryDischarge < 0 ?
                    (batteryDischarge > homePower ? (batteryDischarge - homePower) : 0) :
                    0;
                var solarToGrid = gridExport > batteryToGrid ? gridExport - batteryToGrid : 0;
                var solarToBattery = solarPower > 100 ? batteryCharge - gridToBattery : 0;
                var solarToHome = solarPower - gridExport - solarToBattery;

                if (chartType == PowerChartType.AllData)
                {
                    powerChartSeries.Solar.Add(new ChartDataPoint(date, solarPower / 1000));
                    powerChartSeries.Grid.Add(new ChartDataPoint(date, gridPower / 1000));
                    powerChartSeries.Battery.Add(new ChartDataPoint(date, batteryPower / 1000));
                    powerChartSeries.Home.Add(new ChartDataPoint(date, homePower / 1000));
                }
                else if (chartType == PowerChartType.Home)
                {
                    powerChartSeries.Solar.Add(new ChartDataPoint(date, solarToHome / 1000));
                    powerChartSeries.Grid.Add(new ChartDataPoint(date, gridToHome / 1000));
                    powerChartSeries.Battery.Add(new ChartDataPoint(date, batteryToHome / 1000));

                }
                else if (chartType == PowerChartType.Solar)
                {
                    powerChartSeries.Grid.Add(new ChartDataPoint(date, solarToGrid / 1000));
                    powerChartSeries.Battery.Add(new ChartDataPoint(date, solarToBattery / 1000));
                    powerChartSeries.Home.Add(new ChartDataPoint(date, solarToHome / 1000));
                }
                else if (chartType == PowerChartType.Grid)
                {
                    powerChartSeries.Solar.Add(new ChartDataPoint(date, -solarToGrid / 1000));
                    powerChartSeries.Battery.Add(new ChartDataPoint(date, gridToBattery / 1000));
                    powerChartSeries.Home.Add(new ChartDataPoint(date, (gridToHome) / 1000));
                }
                else if (chartType == PowerChartType.Battery)
                {
                    powerChartSeries.Solar.Add(new ChartDataPoint(date, -solarToBattery / 1000));
                    powerChartSeries.Grid.Add(new ChartDataPoint(date, -gridToBattery / 1000));
                    powerChartSeries.Home.Add(new ChartDataPoint(date, (batteryToHome) / 1000));
                }
            }
            return powerChartSeries;
        }

        public async Task<List<ChartDataPoint>> GetBatteryHistoricalChargeLevel(DateTime startDate, DateTime endDate)
        {
            var url = Utils.GetCalendarHistoryUrl(siteId, platformAdapter.InstallationTimeZone, "soe", "day", startDate, endDate);
            var json = await apiHelper.CallGetApiWithTokenRefresh(url);

            var batteryDailySoeGraphData = new List<ChartDataPoint>();

            if (json["response"].GetValueKind() == JsonValueKind.String || json["response"]["time_series"] == null)
            {
                return batteryDailySoeGraphData;
            }

            foreach (var data in json["response"]["time_series"].AsArray())
            {
                var date = Utils.GetUnspecifiedDateTime(data["timestamp"]);
                batteryDailySoeGraphData.Add(new ChartDataPoint(date, Utils.GetValueOrDefault<double>(data["soe"])));
            }
            return batteryDailySoeGraphData;
        }

        public async Task<EnergyChartSeries> GetEnergyChartSeriesForPeriod(string period, DateTime startDate, DateTime endDate, ITariffProvider tariffHelper)
        {
            var url = Utils.GetCalendarHistoryUrl(siteId, platformAdapter.InstallationTimeZone, "energy", period, startDate, endDate);
            var json = await apiHelper.CallGetApiWithTokenRefresh(url);

            double totalHomeEnergy = 0;
            double totalSolarEnergy = 0;
            double totalGridExportedEnergy = 0;
            double totalGridImportedEnergy = 0;
            double totalBatteryExportedEnergy = 0;
            double totalBatteryImportedEnergy = 0;
            double totalHomeFromGrid = 0;
            double totalHomeFromSolar = 0;
            double totalHomeFromBattery = 0;
            double totalBatteryFromSolar = 0;

            var homeEnergyGraphData = new List<ChartDataPoint>();
            var solarEnergyGraphData = new List<ChartDataPoint>();
            var gridExportedEnergyGraphData = new List<ChartDataPoint>();
            var gridImportedEnergyGraphData = new List<ChartDataPoint>();
            var batteryDischargedEnergyGraphData = new List<ChartDataPoint>();
            var batteryChargedEnergyGraphData = new List<ChartDataPoint>();

            foreach (var data in json["response"]["time_series"].AsArray())
            {
                var date = data["timestamp"].GetValue<DateTime>();
                var homeEnergy = Utils.GetValueOrDefault<double>(data["consumer_energy_imported_from_grid"]) +
                                    Utils.GetValueOrDefault<double>(data["consumer_energy_imported_from_solar"]) +
                                    Utils.GetValueOrDefault<double>(data["consumer_energy_imported_from_battery"]) +
                                    Utils.GetValueOrDefault<double>(data["consumer_energy_imported_from_generator"]);
                totalHomeEnergy += homeEnergy;
                homeEnergyGraphData.Add(new ChartDataPoint(date, homeEnergy / 1000));

                var solarEnergy = Utils.GetValueOrDefault<double>(data["solar_energy_exported"]);
                totalSolarEnergy += solarEnergy;
                solarEnergyGraphData.Add(new ChartDataPoint(date, solarEnergy / 1000));

                var gridExportedEnergy = Utils.GetValueOrDefault<double>(data["grid_energy_exported_from_solar"]) +
                                            Utils.GetValueOrDefault<double>(data["grid_energy_exported_from_generator"]) +
                                            Utils.GetValueOrDefault<double>(data["grid_energy_exported_from_battery"]);
                totalGridExportedEnergy += gridExportedEnergy;
                gridExportedEnergyGraphData.Add(new ChartDataPoint(date, -gridExportedEnergy / 1000));

                var gridImportedEnergy = Utils.GetValueOrDefault<double>(data["battery_energy_imported_from_grid"]) +
                                            Utils.GetValueOrDefault<double>(data["consumer_energy_imported_from_grid"]);
                totalGridImportedEnergy += gridImportedEnergy;
                gridImportedEnergyGraphData.Add(new ChartDataPoint(date, gridImportedEnergy / 1000));

                var batteryExportedEnergy = Utils.GetValueOrDefault<double>(data["battery_energy_exported"]);
                totalBatteryExportedEnergy += batteryExportedEnergy;
                batteryDischargedEnergyGraphData.Add(new ChartDataPoint(date, batteryExportedEnergy / 1000));

                var batteryImportedEnergy = Utils.GetValueOrDefault<double>(data["battery_energy_imported_from_grid"]) +
                                            Utils.GetValueOrDefault<double>(data["battery_energy_imported_from_solar"]) +
                                            Utils.GetValueOrDefault<double>(data["battery_energy_imported_from_generator"]);
                totalBatteryImportedEnergy += batteryImportedEnergy;
                batteryChargedEnergyGraphData.Add(new ChartDataPoint(date, -batteryImportedEnergy / 1000));

                // Totals for self consumption calcs
                totalHomeFromGrid += Utils.GetValueOrDefault<double>(data["consumer_energy_imported_from_grid"]) + Utils.GetValueOrDefault<double>(data["consumer_energy_imported_from_generator"]);
                totalHomeFromSolar += Utils.GetValueOrDefault<double>(data["consumer_energy_imported_from_solar"]);
                totalHomeFromBattery += Utils.GetValueOrDefault<double>(data["consumer_energy_imported_from_battery"]);
                totalBatteryFromSolar += Utils.GetValueOrDefault<double>(data["battery_energy_imported_from_solar"]);
            }

            EnergyChartSeries energyChartSeries = new EnergyChartSeries();
            energyChartSeries.Home = NormaliseEnergyData(homeEnergyGraphData, period);
            energyChartSeries.Solar = NormaliseEnergyData(solarEnergyGraphData, period);
            energyChartSeries.GridExport = NormaliseEnergyData(gridExportedEnergyGraphData, period);
            energyChartSeries.GridImport = NormaliseEnergyData(gridImportedEnergyGraphData, period);
            energyChartSeries.BatteryDischarge = NormaliseEnergyData(batteryDischargedEnergyGraphData, period);
            energyChartSeries.BatteryCharge = NormaliseEnergyData(batteryChargedEnergyGraphData, period);

            var homeFromBatterySolar = Math.Min(totalHomeFromBattery, totalBatteryFromSolar); // Don't count battery energy if it came from the grid
            energyChartSeries.EnergyTotals = new EnergyTotals()
            {
                HomeEnergy = totalHomeEnergy,
                SolarEnergy = totalSolarEnergy,
                GridEnergyImported = totalGridImportedEnergy,
                GridEnergyExported = totalGridExportedEnergy,
                BatteryEnergyDischarged = totalBatteryExportedEnergy,
                BatteryEnergyCharged = totalBatteryImportedEnergy,
                SolarUsePercent = (totalHomeFromSolar / totalHomeEnergy) * 100,
                BatteryUsePercent = (totalHomeFromBattery / totalHomeEnergy) * 100,
                GridUsePercent = (totalHomeFromGrid / totalHomeEnergy) * 100,
                SelfConsumption = ((totalHomeFromSolar + homeFromBatterySolar) / totalHomeEnergy) * 100
            };

            if (tariffHelper != null && (period == "Week" || period == "Month"))
            {
                await CalculateCostData((JsonArray)json["response"]["time_series"].AsArray(), tariffHelper, energyChartSeries);
            }

            return energyChartSeries;
        }

        private async Task CalculateCostData(JsonArray energyTimeSeries, ITariffProvider tariffHelper, EnergyChartSeries energyChartSeries)
        {
            try
            {

                energyChartSeries.EnergyCostGraphData = new List<ChartDataPoint>();
                energyChartSeries.EnergyFeedInGraphData = new List<ChartDataPoint>();
                energyChartSeries.EnergyNetCostGraphData = new List<ChartDataPoint>();

                var dailyData = new Dictionary<DateTime, List<JsonNode>>();
                // Split array by date
                foreach (var data in energyTimeSeries)
                {
                    var ts = data["timestamp"].GetValue<DateTime>();
                    if (!dailyData.ContainsKey(ts.Date))
                    {
                        dailyData[ts.Date] = new List<JsonNode>();
                    }
                    dailyData[ts.Date].Add(data.AsObject());
                }

                // Calculate costs per date  
                foreach (var date in dailyData.Keys)
                {
                    var energyCost = await tariffHelper.GetEnergyCostAndFeedInFromEnergyHistory(dailyData[date]);
                    energyChartSeries.EnergyCostGraphData.Add(new ChartDataPoint(date, (double)energyCost.Item1));
                    energyChartSeries.EnergyFeedInGraphData.Add(new ChartDataPoint(date, (double)-energyCost.Item2));
                    energyChartSeries.EnergyNetCostGraphData.Add(new ChartDataPoint(date, (double)(energyCost.Item1 - energyCost.Item2)));
                }

            }
            catch (Exception)
            {
                //Crashes.TrackError(ex);
            }

        }

        public async Task ExportPowerDataToCsv(Stream stream, DateTime startDate, DateTime endDate)
        {
            var url = Utils.GetCalendarHistoryUrl(siteId, platformAdapter.InstallationTimeZone, "power", "day", startDate, endDate);
            var json = await apiHelper.CallGetApiWithTokenRefresh(url);
            var data = json["response"]["time_series"].AsArray();

            if (data.Count > 0)
            {
                var tw = new StreamWriter(stream);
                var first = (JsonObject) data[0].AsObject();
                foreach (var prop in first)
                {
                    await tw.WriteAsync(prop.Key + ",");
                }
                await tw.WriteLineAsync("load_power");

                foreach (var entry in data.AsArray())
                {
                    foreach (var prop in (JsonObject)entry)
                    {
                        if (prop.Key == "timestamp")
                        {
                            await tw.WriteAsync($"{(Utils.GetUnspecifiedDateTime(prop.Value)):yyyy-MM-dd HH\\:mm\\:ss},");
                        }
                        else
                        {
                            await tw.WriteAsync(prop.Value + ",");
                        }
                    }
                    var solarPower = Utils.GetValueOrDefault<double>(entry["solar_power"]);
                    var gridPower = Utils.GetValueOrDefault<double>(entry["grid_power"]);
                    var batteryPower = Utils.GetValueOrDefault<double>(entry["battery_power"]);
                    var homePower = solarPower + gridPower + batteryPower;
                    await tw.WriteLineAsync(homePower.ToString());
                }
                await tw.FlushAsync();
            }            
        }
        public async Task ExportEnergyDataToCsv(Stream stream, DateTime startDate, DateTime endDate, string period, ITariffProvider tariffHelper)
        { 
            var url = Utils.GetCalendarHistoryUrl(siteId, platformAdapter.InstallationTimeZone, "energy", period, startDate, endDate);
            var json = await apiHelper.CallGetApiWithTokenRefresh(url);

            // Save data from API into dictionary
            var energyData = new Dictionary<DateTime, Dictionary<string, double>>();
            var keyNames = new List<string>();
            foreach (var data in json["response"]["time_series"].AsArray())
            {
                var date = data["timestamp"].GetValue<DateTime>();
                if (!energyData.ContainsKey(date)) // Apparently duplicates can occur
                {
                    var dict = new Dictionary<string, double>();
                    foreach (var prop in (JsonObject)data)
                    {
                        if (prop.Value.GetValueKind() == System.Text.Json.JsonValueKind.Number)
                        {
                            dict.Add(prop.Key, (double)prop.Value);
                            if (!keyNames.Contains(prop.Key))
                            {
                                keyNames.Add(prop.Key);
                            }
                        }
                    }
                    energyData.Add(date, dict);
                }
            }

            var normalisedExportData = NormaliseExportData(energyData, period);

            // Header line
            var tw = new StreamWriter(stream);
            await tw.WriteAsync("timestamp,");
            foreach (var key in keyNames)
            {
                await tw.WriteAsync(key + ",");
            }
            if (tariffHelper != null && (period == "Week" || period == "Month"))
            {
                await tw.WriteAsync("Cost,FeedIn,NetCost,");
            }
            await tw.WriteLineAsync();
   

            // Write out normalised data
            foreach (var data in normalisedExportData)
            {
                await tw.WriteAsync($"{data.Key:yyyy-MM-dd},");
                foreach (var key in keyNames)
                {
                    if (data.Value.ContainsKey(key))
                    {
                        await tw.WriteAsync($"{data.Value[key]},");
                    }
                    else
                    {
                        await tw.WriteAsync(",");
                    }
                }
                if (tariffHelper != null && (period == "Week" || period == "Month"))
                {
                    var dataForDay = json["response"]["time_series"].AsArray().Where(ts => ts["timestamp"].GetValue<DateTime>().Date == data.Key).ToList();
                    var energyCosts = await tariffHelper.GetEnergyCostAndFeedInFromEnergyHistory(dataForDay);
                    await tw.WriteAsync($"{energyCosts.Item1},{energyCosts.Item2},{energyCosts.Item1 - energyCosts.Item2}");
                }
                await tw.WriteLineAsync();
            }
            await tw.FlushAsync();
        }

        private Func<DateTime, DateTime, bool> GetNormalisationDateComparitor(string period)
        {
            Func<DateTime, DateTime, bool> dateComparitor;
            if (period == "Year")
            {
                dateComparitor = (DateTime d, DateTime c) => d.Year == c.Year && d.Month == c.Month;
            }
            else if (period == "Lifetime")
            {
                dateComparitor = (DateTime d, DateTime c) => d.Year == c.Year;
            }
            else // Day, Week, Month
            {
                dateComparitor = (DateTime d, DateTime c) => d.Date == c.Date;
            }
            return dateComparitor;
        }

        private List<ChartDataPoint> NormaliseEnergyData(List<ChartDataPoint> energyGraphData, string period)
        {
            // The API has started returning super granular data,. Let's normalise it to a more sensible granularity 
            var result = new List<ChartDataPoint>();
            ChartDataPoint lastPoint = null;

            var dateComparitor = GetNormalisationDateComparitor(period);

            foreach (var dataPoint in energyGraphData)
            {
                if (lastPoint == null || !dateComparitor(dataPoint.XValue, lastPoint.XValue))
                {
                    // New period
                    result.Add(dataPoint);
                    lastPoint = dataPoint;
                }
                else
                {
                    lastPoint.YValue += dataPoint.YValue;
                }
            }
            return result;
        }

        private Dictionary<DateTime, Dictionary<string, double>> NormaliseExportData(Dictionary<DateTime, Dictionary<string, double>> exportData, string period)
        {
            // The API has started returning super granular data,. Let's normalise it to a more sensible granularity 
            var result = new Dictionary<DateTime, Dictionary<string, double>>();
            DateTime lastDate = DateTime.MinValue;
            Dictionary<string, double> lastValue = null;

            var dateComparitor = GetNormalisationDateComparitor(period);

            foreach (var currentDate in exportData.Keys)
            {
                if (lastValue == null || !dateComparitor(currentDate, lastDate))
                {
                    // New period
                    result.Add(currentDate, exportData[currentDate]);
                    lastDate = currentDate;
                    lastValue = exportData[currentDate];
                }
                else
                {
                    // Add the values from the current point to the last one
                    foreach (var key in exportData[currentDate].Keys)
                    {
                        if (key == "timestamp")
                        {
                            continue;
                        }
                        if (exportData[currentDate][key].GetType() != typeof(DateTime))
                        {
                            try
                            {
                                if (!lastValue.ContainsKey(key))
                                {
                                    lastValue[key] = 0;
                                }
                                lastValue[key] = Convert.ToInt64(lastValue[key]) + Convert.ToInt64(exportData[currentDate][key]);
                            }
                            catch
                            {
                                // Unlikely but they could add string values...
                            }
                        }

                    }
                }
            }
            return result;
        }
        public DateTime ConvertToPowerwallDate(DateTime date)
        {
            try
            {
                var tzInfo = GetInstallationTimeZone();
                var offset = tzInfo.GetUtcOffset(date);
                var dto = new DateTimeOffset(date);
                return dto.ToOffset(offset).DateTime;
            }
            catch
            {
                // Unable to convert for some reason; assume local time 
                return date;
            }
        }

        public async Task<EnergySiteInfo> GetEnergySiteInfo()
        {
            var energySiteStatus = new EnergySiteInfo();
            var tasks = new List<Task<JsonObject>>();
            tasks.Add(apiHelper.CallGetApiWithTokenRefresh($"/api/1/energy_sites/{siteId}/site_status"));
            tasks.Add(apiHelper.CallGetApiWithTokenRefresh($"/api/1/energy_sites/{siteId}/site_info"));
            await Task.WhenAll(tasks);
            var siteStatusJson = tasks[0].Result;
            energySiteStatus.SiteName = Utils.GetValueOrDefault<string>(siteStatusJson["response"]["site_name"]);
            energySiteStatus.GatewayId = Utils.GetValueOrDefault<string>(siteStatusJson["response"]["gateway_id"]);
            var siteInfoJson = tasks[1].Result;
            energySiteStatus.NumberOfBatteries = Utils.GetValueOrDefault<int>(siteInfoJson["response"]["battery_count"]);
            energySiteStatus.InstallDate = Utils.GetValueOrDefault<DateTime>(siteInfoJson["response"]["installation_date"]);
            energySiteStatus.ReservePercent = Utils.GetValueOrDefault<int>(siteInfoJson["response"]["backup_reserve_percent"]);
            return energySiteStatus;
        }

        public async Task StoreInstallationTimeZone()
        {

            try
            {
                var siteInfoJson = await apiHelper.CallGetApiWithTokenRefresh($"/api/1/energy_sites/{siteId}/site_info");
                platformAdapter.InstallationTimeZone = siteInfoJson["response"]["installation_time_zone"].GetValue<string>();

            }
            catch
            {
                var systemTimeZone = TimeZoneInfo.Local.Id;
                if (systemTimeZone.Contains("/"))
                {
                    // On Android it will already be Iana
                    platformAdapter.InstallationTimeZone = systemTimeZone;
                }
                else
                {
                    platformAdapter.InstallationTimeZone = TZConvert.WindowsToIana(systemTimeZone);
                }

            }
        }

    }
}
