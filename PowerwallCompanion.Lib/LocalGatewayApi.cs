using PowerwallCompanion.Lib.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PowerwallCompanion.Lib
{
    public class LocalGatewayApi
    {
        private GatewayApiHelper gatewayApiHelper;
        private IPlatformAdapter platformAdapter;

        public LocalGatewayApi(IPlatformAdapter platformAdapter)
        {
            this.platformAdapter = platformAdapter;
            gatewayApiHelper = new GatewayApiHelper();
        }

        public async Task<BatteryDetailsResponse> GetBatteryDetails(string localGatewayIP, string localGatewayPassword)
        {
            var response = new BatteryDetailsResponse();
            JsonObject json = null;
            try
            {
                json = await gatewayApiHelper.CallGetApi(localGatewayIP, localGatewayPassword, "/api/system_status");
                await platformAdapter.SaveGatewayDetailsToCache(json);
            }
            catch (Exception ex)
            {
                json = await platformAdapter.ReadGatewayDetailsFromCache();
                response.ErrorMessage = Utils.FormatException(ex);
            }

            if (json != null)
            {
                try
                {
                    response.BatteryDetails = new List<BatteryDetails>();
                    foreach (var batteryBlock in (JsonArray)json["battery_blocks"])
                    {
                        if (batteryBlock["nominal_full_pack_energy"] == null || batteryBlock["nominal_energy_remaining"] == null)
                        {
                            continue; // Can happen if battery is disabled
                        }
                        response.BatteryDetails.Add(new BatteryDetails
                        {
                            SerialNumber = batteryBlock["PackageSerialNumber"].GetValue<string>(),
                            FullCapacity = batteryBlock["nominal_full_pack_energy"].GetValue<double>(),
                            CurrentChargeLevel = batteryBlock["nominal_energy_remaining"].GetValue<double>()
                        });
                    }
                }
                catch (Exception ex)
                {
                    response.ErrorMessage = Utils.FormatException(ex);
                }
            }
            
            return response;
            
        }

        public async Task<Dictionary<string, List<ChartDataPoint>>> GetBatteryHistoryDataFromServer(string siteId, string gatewayId)
        {
            var batteryHistoryChartDictionary = new Dictionary<string, List<ChartDataPoint>>();

            var client = new HttpClient();
            var url = $"https://pwcfunctions.azurewebsites.net/api/getBatteryHistory?siteId={siteId}&gatewayId={gatewayId}&code={Keys.AzureFunctionsApiKey}";
            var response = await client.GetAsync(url);
            var responseMessage = await response.Content.ReadAsStringAsync();
            if (!String.IsNullOrEmpty(responseMessage) && responseMessage != "null")
            {
                var json = JsonObject.Parse(responseMessage);

                if (json["batteryGranularHistory"] != null)
                {
                    foreach (var property in json["batteryGranularHistory"].AsObject())
                    {
                        var batteryHistoryChartData = new List<ChartDataPoint>();
                        string serial = property.Key;
                        foreach (var history in property.Value.AsArray())
                        {
                            batteryHistoryChartData.Add(new ChartDataPoint(xValue: history["date"].GetValue<DateTime>(), yValue: history["capacity"].GetValue<double>() / 1000));
                        }
                        batteryHistoryChartDictionary.Add(serial, batteryHistoryChartData);
                    }
                }
            }
            return batteryHistoryChartDictionary;
        }

        public async Task SaveBatteryHistoryDataToServer(string siteId, string gatewayId, List<BatteryDetails> batteryDetails)
        {
            if (batteryDetails != null)
            {
                var client = new HttpClient();
                var url = $"https://pwcfunctions.azurewebsites.net/api/saveGranularBatteryHistory?code={Keys.AzureFunctionsApiKey}";
                var sb = new StringBuilder();
                foreach (var battery in batteryDetails)
                {
                    sb.Append("\"" + battery.SerialNumber + "\": ");
                    sb.Append(battery.FullCapacity);
                    sb.Append(", ");
                }
                if (sb.Length > 0)
                {
                    sb.Remove(sb.Length - 2, 2);
                }
                var body = $"{{\"siteId\": \"{siteId}\", \"gatewayId\": \"{gatewayId}\", \"batteryData\": {{ {sb.ToString()} }}}}";
                var response = await client.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));
            }
        }
    }
}
