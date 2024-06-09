using PowerwallCompanion.Lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;
using TimeZoneConverter;

namespace PowerwallCompanion.Lib
{
    public class PowerwallApi
    {
        private string siteId;
        private ITokenStore tokenStore;
        private ApiHelper apiHelper;
        private string installationTimeZone;
        private JsonObject productResponse;

        public PowerwallApi(string siteId, ITokenStore tokenStore)
        {
            this.siteId = siteId;
            this.tokenStore = tokenStore;
            this.apiHelper = new ApiHelper(tokenStore);
        }

        private async Task<JsonObject> GetProductResponse()
        {
            if (productResponse == null)
            {
                productResponse = await apiHelper.CallGetApiWithTokenRefresh("/api/1/products");
            }
            return productResponse;
        }
        public async Task<string> GetSiteId()
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
            bool foundSite = false;
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

            instantaneousPower.BatteryStoragePercent = GetValueOrDefault<double>(powerInfo["response"]["percentage_charged"]);
            instantaneousPower.HomePower = GetValueOrDefault<double>(powerInfo["response"]["load_power"]);
            instantaneousPower.SolarPower = GetValueOrDefault<double>(powerInfo["response"]["solar_power"]);
            instantaneousPower.BatteryPower = GetValueOrDefault<double>(powerInfo["response"]["battery_power"]);
            instantaneousPower.GridPower = GetValueOrDefault<double>(powerInfo["response"]["grid_power"]);
            instantaneousPower.GridActive = powerInfo["response"]["grid_status"].GetValue<string>() != "Inactive";
            return instantaneousPower;
        }

        public async Task<Tuple<double, double>> GetBatteryMinMaxToday()
        {
  
            var json = await apiHelper.CallGetApiWithTokenRefresh($"/api/1/energy_sites/{siteId}/calendar_history?kind=soe");
            int min = 100;
            int max = 0;
            foreach (var datapoint in (JsonArray)json["response"]["time_series"])
            {
                var timestamp = await ConvertToPowerwallDate(datapoint["timestamp"].GetValue<DateTime>());
                if (timestamp.Date == (await ConvertToPowerwallDate(DateTime.Now)).Date)
                {
                    var soe = datapoint["soe"].GetValue<int>();
                    if (soe < min) min = soe;
                    if (soe > max) max = soe;

                }
            }
            return new Tuple<double, double>((double)min, (double)max);
            
        }

        public async Task<EnergyTotals> GetEnergyTotalsForDay(DateTime date, TariffHelper tariffHelper)
        {
            return await GetEnergyTotalsForPeriod(date, date.AddDays(1).AddSeconds(-1), "day", tariffHelper);
        }

        public async Task<EnergyTotals> GetEnergyTotalsForPeriod(DateTime startDate, DateTime endDate, string period, TariffHelper tariffHelper)
        {
            var timeZone = await GetInstallationTimeZone();
            var url = Utils.GetCalendarHistoryUrl(siteId, timeZone, "energy", period, startDate, endDate);

            var energyHistory = await apiHelper.CallGetApiWithTokenRefresh(url);
            double totalHomeFromGrid = 0;
            double totalHomeFromSolar = 0;
            double totalHomeFromBattery = 0;

            var energyTotals = new EnergyTotals();

            foreach (var item in energyHistory["response"]["time_series"].AsArray())
            {
                energyTotals.HomeEnergy += GetValueOrDefault<double>(item["total_home_usage"]);
                energyTotals.SolarEnergy += GetValueOrDefault<double>(item["total_solar_generation"]);
                energyTotals.GridEnergyImported += GetValueOrDefault<double>(item["grid_energy_imported"]);
                energyTotals.GridEnergyExported += GetValueOrDefault<double>(item["grid_energy_exported_from_solar"]) + GetValueOrDefault<double>(item["grid_energy_exported_from_generator"]) + GetValueOrDefault<double>(item["grid_energy_exported_from_battery"]);
                energyTotals.BatteryEnergyCharged += GetValueOrDefault<double>(item["battery_energy_imported_from_grid"]) + GetValueOrDefault<double>(item["battery_energy_imported_from_solar"]) + GetValueOrDefault<double>(item["battery_energy_imported_from_generator"]);
                energyTotals.BatteryEnergyDischarged += GetValueOrDefault<double>(item["battery_energy_exported"]);

                // Totals for self consumption calcs
                totalHomeFromGrid += GetValueOrDefault<double>(item["consumer_energy_imported_from_grid"]) + GetValueOrDefault<double>(item["consumer_energy_imported_from_generator"]);
                totalHomeFromSolar += GetValueOrDefault<double>(item["consumer_energy_imported_from_solar"]);
                totalHomeFromBattery += GetValueOrDefault<double>(item["consumer_energy_imported_from_battery"]);
            }

            energyTotals.SolarUsePercent = (totalHomeFromSolar / energyTotals.HomeEnergy) * 100;
            energyTotals.BatteryUsePercent = (totalHomeFromBattery / energyTotals.HomeEnergy) * 100;
            energyTotals.GridUsePercent = (totalHomeFromGrid / energyTotals.HomeEnergy) * 100;
            energyTotals.SelfConsumption = ((totalHomeFromSolar + totalHomeFromBattery) / energyTotals.HomeEnergy) * 100;

            if (tariffHelper != null)
            {
                var dailyCosts = tariffHelper.GetEnergyCostAndFeedInFromEnergyHistory(energyHistory["response"]["time_series"].AsArray().ToList());
                energyTotals.EnergyCost = dailyCosts.Item1;
                energyTotals.EnergyFeedIn = dailyCosts.Item2;
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
                var timestamp = await ConvertToPowerwallDate(datapoint["timestamp"].GetValue<DateTime>());
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
            string timeZone = await GetInstallationTimeZone();
            var url = Utils.GetCalendarHistoryUrl(siteId, timeZone, "power", period, startDate, endDate);
            var json = await apiHelper.CallGetApiWithTokenRefresh(url);

            var powerChartSeries = new PowerChartSeries();
            powerChartSeries.Solar = new List<ChartDataPoint>();
            powerChartSeries.Grid = new List<ChartDataPoint>();
            powerChartSeries.Battery = new List<ChartDataPoint>();
            powerChartSeries.Home = new List<ChartDataPoint>();

            foreach (var data in json["response"]["time_series"].AsArray())
            {
                var date = data["timestamp"].GetValue<DateTime>();
                if (date > DateTime.Now) continue;

                // The date may be in a different time zone to the local time, we want to use the install time
                date = await ConvertToPowerwallDate(date);

                var solarPower = GetValueOrDefault<double>(data["solar_power"]);
                var gridPower = GetValueOrDefault<double>(data["grid_power"]);
                var batteryPower = GetValueOrDefault<double>(data["battery_power"]);
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
            string timeZone = await GetInstallationTimeZone();
            var url = Utils.GetCalendarHistoryUrl(siteId, timeZone, "soe", "day", startDate, endDate);
            var json = await apiHelper.CallGetApiWithTokenRefresh(url);

            var batteryDailySoeGraphData = new List<ChartDataPoint>();

            foreach (var data in json["response"]["time_series"].AsArray())
            {
                var date = data["timestamp"].GetValue<DateTime>();
                if (date <= DateTime.Now)
                {
                    // The date may be in a different time zone to the local time, we want to use the install time
                    date = await ConvertToPowerwallDate(date);

                    batteryDailySoeGraphData.Add(new ChartDataPoint(date, GetValueOrDefault<double>(data["soe"])));
                }
            }
            return batteryDailySoeGraphData;
        }

        public async Task<EnergyChartSeries> GetEnergyChartSeriesForPeriod(string period, DateTime startDate, DateTime endDate, TariffHelper tariffHelper)
        {
            string timeZone = await GetInstallationTimeZone();
            var url = Utils.GetCalendarHistoryUrl(siteId, timeZone, "energy", period, startDate, endDate);
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

            var homeEnergyGraphData = new List<ChartDataPoint>();
            var solarEnergyGraphData = new List<ChartDataPoint>();
            var gridExportedEnergyGraphData = new List<ChartDataPoint>();
            var gridImportedEnergyGraphData = new List<ChartDataPoint>();
            var batteryDischargedEnergyGraphData = new List<ChartDataPoint>();
            var batteryChargedEnergyGraphData = new List<ChartDataPoint>();

            foreach (var data in json["response"]["time_series"].AsArray())
            {
                var date = data["timestamp"].GetValue<DateTime>();
                var homeEnergy = GetValueOrDefault<double>(data["consumer_energy_imported_from_grid"]) +
                                    GetValueOrDefault<double>(data["consumer_energy_imported_from_solar"]) +
                                    GetValueOrDefault<double>(data["consumer_energy_imported_from_battery"]) +
                                    GetValueOrDefault<double>(data["consumer_energy_imported_from_generator"]);
                totalHomeEnergy += homeEnergy;
                homeEnergyGraphData.Add(new ChartDataPoint(date, homeEnergy / 1000));

                var solarEnergy = GetValueOrDefault<double>(data["solar_energy_exported"]);
                totalSolarEnergy += solarEnergy;
                solarEnergyGraphData.Add(new ChartDataPoint(date, solarEnergy / 1000));

                var gridExportedEnergy = GetValueOrDefault<double>(data["grid_energy_exported_from_solar"]) +
                                            GetValueOrDefault<double>(data["grid_energy_exported_from_generator"]) +
                                            GetValueOrDefault<double>(data["grid_energy_exported_from_battery"]);
                totalGridExportedEnergy += gridExportedEnergy;
                gridExportedEnergyGraphData.Add(new ChartDataPoint(date, -gridExportedEnergy / 1000));

                var gridImportedEnergy = GetValueOrDefault<double>(data["battery_energy_imported_from_grid"]) +
                                            GetValueOrDefault<double>(data["consumer_energy_imported_from_grid"]);
                totalGridImportedEnergy += gridImportedEnergy;
                gridImportedEnergyGraphData.Add(new ChartDataPoint(date, gridImportedEnergy / 1000));

                var batteryExportedEnergy = GetValueOrDefault<double>(data["battery_energy_exported"]);
                totalBatteryExportedEnergy += batteryExportedEnergy;
                batteryDischargedEnergyGraphData.Add(new ChartDataPoint(date, batteryExportedEnergy / 1000));

                var batteryImportedEnergy = GetValueOrDefault<double>(data["battery_energy_imported_from_grid"]) +
                                            GetValueOrDefault<double>(data["battery_energy_imported_from_solar"]) +
                                            GetValueOrDefault<double>(data["battery_energy_imported_from_generator"]);
                totalBatteryImportedEnergy += batteryImportedEnergy;
                batteryChargedEnergyGraphData.Add(new ChartDataPoint(date, -batteryImportedEnergy / 1000));

                // Totals for self consumption calcs
                totalHomeFromGrid += GetValueOrDefault<double>(data["consumer_energy_imported_from_grid"]) + GetValueOrDefault<double>(data["consumer_energy_imported_from_generator"]);
                totalHomeFromSolar += GetValueOrDefault<double>(data["consumer_energy_imported_from_solar"]);
                totalHomeFromBattery += GetValueOrDefault<double>(data["consumer_energy_imported_from_battery"]);
            }

            EnergyChartSeries energyChartSeries = new EnergyChartSeries();
            energyChartSeries.Home = NormaliseEnergyData(homeEnergyGraphData, period);
            energyChartSeries.Solar = NormaliseEnergyData(solarEnergyGraphData, period);
            energyChartSeries.GridExport = NormaliseEnergyData(gridExportedEnergyGraphData, period);
            energyChartSeries.GridImport = NormaliseEnergyData(gridImportedEnergyGraphData, period);
            energyChartSeries.BatteryDischarge = NormaliseEnergyData(batteryDischargedEnergyGraphData, period);
            energyChartSeries.BatteryCharge = NormaliseEnergyData(batteryChargedEnergyGraphData, period);


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
                SelfConsumption = ((totalHomeFromSolar + totalHomeFromBattery) / totalHomeEnergy) * 100
            };

            if (tariffHelper != null && (period == "Week" || period == "Month"))
            {
                CalculateCostData((JsonArray)json["response"]["time_series"].AsArray(), tariffHelper, energyChartSeries);
            }

            return energyChartSeries;
        }

        private void CalculateCostData(JsonArray energyTimeSeries, TariffHelper tariffHelper, EnergyChartSeries energyChartSeries)
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

                // Calculate costs per date  // TODO: FIX
                foreach (var date in dailyData.Keys)
                {
                    var energyCost = tariffHelper.GetEnergyCostAndFeedInFromEnergyHistory(dailyData[date]);
                    energyChartSeries.EnergyCostGraphData.Add(new ChartDataPoint(date, (double)energyCost.Item1));
                    energyChartSeries.EnergyFeedInGraphData.Add(new ChartDataPoint(date, (double)-energyCost.Item2));
                    energyChartSeries.EnergyNetCostGraphData.Add(new ChartDataPoint(date, (double)(energyCost.Item1 - energyCost.Item2)));
                }

            }
            catch (Exception ex)
            {
                //Crashes.TrackError(ex);
            }

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

        public async Task<DateTime> ConvertToPowerwallDate(DateTime date)
        {
            try
            {
                string timeZone = await GetInstallationTimeZone();
                var windowsTimeZone = TZConvert.IanaToWindows(timeZone);
                var powerwallTimeZone = TimeZoneInfo.FindSystemTimeZoneById(windowsTimeZone);
                var offset = powerwallTimeZone.GetUtcOffset(date);
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
            energySiteStatus.SiteName = siteStatusJson["response"]["site_name"].GetValue<string>();
            energySiteStatus.GatewayId = siteStatusJson["response"]["gateway_id"].GetValue<string>();
            var siteInfoJson = tasks[1].Result;
            energySiteStatus.NumberOfBatteries = siteInfoJson["response"]["battery_count"].GetValue<int>();
            energySiteStatus.InstallDate = siteInfoJson["response"]["installation_date"].GetValue<DateTime>();
            return energySiteStatus;
        }

        private async Task<string> GetInstallationTimeZone()
        {
            if (installationTimeZone == null)
            {
                try
                {
                    var siteInfoJson = await apiHelper.CallGetApiWithTokenRefresh($"/api/1/energy_sites/{siteId}/site_info");
                    installationTimeZone = siteInfoJson["response"]["installation_time_zone"].GetValue<string>();
                }
                catch
                {
                    installationTimeZone = TZConvert.WindowsToIana(TimeZoneInfo.Local.Id);
                }
            }
            return installationTimeZone;
        }

        private T GetValueOrDefault<T>(JsonNode obj)
        {
            if (obj == null)
            {
                return default(T);
            }
            try
            {
                return obj.GetValue<T>();
            }
            catch
            {
                return default(T);
            }
        }
    }
}
