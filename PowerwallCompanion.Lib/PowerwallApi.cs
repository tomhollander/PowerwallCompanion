using PowerwallCompanion.Lib.Models;
using System;
using System.Net.Http.Headers;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace PowerwallCompanion.Lib
{
    public class PowerwallApi
    {
        private string siteId;
        private ITokenStore tokenStore;
        private ApiHelper apiHelper;
        private string installationTimeZone;

        public PowerwallApi(string siteId, ITokenStore tokenStore)
        {
            this.siteId = siteId;
            this.tokenStore = tokenStore;
            this.apiHelper = new ApiHelper(tokenStore);
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

            var energyTotals = new EnergyTotals();

            foreach (var item in energyHistory["response"]["time_series"].AsArray())
            {
                energyTotals.HomeEnergy += GetValueOrDefault<double>(item["total_home_usage"]);
                energyTotals.SolarEnergy += GetValueOrDefault<double>(item["total_solar_generation"]);
                energyTotals.GridEnergyImported += GetValueOrDefault<double>(item["grid_energy_imported"]);
                energyTotals.GridEnergyExported += GetValueOrDefault<double>(item["grid_energy_exported_from_solar"]) + GetValueOrDefault<double>(item["grid_energy_exported_from_generator"]) + GetValueOrDefault<double>(item["grid_energy_exported_from_battery"]);
                energyTotals.BatteryEnergyCharged += GetValueOrDefault<double>(item["battery_energy_imported_from_grid"]) + GetValueOrDefault<double>(item["battery_energy_imported_from_solar"]) + GetValueOrDefault<double>(item["battery_energy_imported_from_generator"]);
                energyTotals.BatteryEnergyDischarged += GetValueOrDefault<double>(item["battery_energy_exported"]);
            }

            if (tariffHelper != null)
            {
                var dailyCosts = tariffHelper.GetEnergyCostAndFeedInFromEnergyHistory(energyHistory["response"]["time_series"].AsArray());
                energyTotals.EnergyCost = dailyCosts.Item1;
                energyTotals.EnergyFeedIn = dailyCosts.Item2;
            }

            return energyTotals;
        }
        public async Task<JsonObject> GetRatePlan()
        {
            return await apiHelper.CallGetApiWithTokenRefresh($"/api/1/energy_sites/{siteId}/tariff_rate");
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
