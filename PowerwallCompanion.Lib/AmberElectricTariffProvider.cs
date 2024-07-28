using PowerwallCompanion.Lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PowerwallCompanion.Lib
{
    public class AmberElectricTariffProvider : ITariffProvider
    {
        private string apiKey;
        private string siteId;
        private Dictionary<DateTime, List<Tariff>> tariffCache = new Dictionary<DateTime, List<Tariff>>();
        public AmberElectricTariffProvider(string apiKey)
        {
            this.apiKey = apiKey;
        }

        private async Task<string> GetSiteId()
        { 
            if (siteId == null)
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);
                var response = await client.GetAsync("https://api.amber.com.au/v1/sites");
                var responseMessage = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var json = (JsonArray)JsonNode.Parse(responseMessage);
                    siteId = json[0]["id"].GetValue<string>();
                }
            }
            return siteId;
        }


        public bool IsSingleRatePlan => false;

        public async Task<Tuple<decimal, decimal>> GetEnergyCostAndFeedInFromEnergyHistory(List<JsonNode> energyHistoryTimeSeries)
        {
            var tariffs = await GetTariffsForDay(Utils.GetUnspecifiedDateTime(energyHistoryTimeSeries.First()["timestamp"]));
            decimal totalCost = 0M;
            decimal totalFeedIn = 0M;
            foreach (var energyHistory in energyHistoryTimeSeries)
            {
                var timestamp = Utils.GetUnspecifiedDateTime(energyHistory["timestamp"]);
                var energyImported = Utils.GetValueOrDefault<double>(energyHistory["grid_energy_imported"]) / 1000; // Convert to kWh
                var energyExported = (Utils.GetValueOrDefault<double>(energyHistory["grid_energy_exported_from_solar"]) +
                    Utils.GetValueOrDefault<double>(energyHistory["grid_energy_exported_from_battery"]) +
                    Utils.GetValueOrDefault<double>(energyHistory["grid_energy_exported_from_generator"])) / 1000; // Convert to kWh
                var tariff = tariffs.Where(t => t.StartDate >= timestamp).FirstOrDefault();
                if (tariff == null)
                {
                    continue;
                }
                totalCost += (decimal)energyImported * tariff.DynamicSellAndFeedInRate.Item1;
                totalFeedIn += (decimal)energyExported * tariff.DynamicSellAndFeedInRate.Item2;
            }
            return new Tuple<decimal, decimal>(totalCost, totalFeedIn);
        }

        public Tuple<decimal, decimal> GetRatesForTariff(Tariff tariff)
        {
            return tariff.DynamicSellAndFeedInRate;
        }

        public async Task<Tariff> GetInstantaneousTariff()
        {
            var siteId = await GetSiteId();
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);
            var response = await client.GetAsync($"https://api.amber.com.au/v1/sites/{siteId}/prices/current?resolution=30");
            var responseMessage = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var json = (JsonArray)JsonNode.Parse(responseMessage);
                var tariff = new Tariff();
                tariff.StartDate = json[0]["startTime"].GetValue<DateTime>();
                tariff.EndDate = json[0]["endTime"].GetValue<DateTime>();
                tariff.IsDynamic = true;
                tariff.DynamicSellAndFeedInRate = new Tuple<decimal, decimal>(json[0]["perKwh"].GetValue<decimal>() / 100, json[1]["perKwh"].GetValue<decimal>() / -100);
                tariff.Name = "Dynamic";
                return tariff;
            }
            return null;
        }

        public async Task<List<Tariff>> GetTariffsForDay(DateTime date)
        {
            var siteId = await GetSiteId();

            if (!tariffCache.ContainsKey(date) || date.Date == DateTime.Now.Date)
            {

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);
                var response = await client.GetAsync($"https://api.amber.com.au/v1/sites/{siteId}/prices/?format=date&resolution=30&startDate={date.ToString("yyyy-MM-dd")}&endDate={date.ToString("yyyy-MM-dd")}");
                var responseMessage = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                
                var tariffs = new List<Tariff>();
                var json = (JsonArray)JsonNode.Parse(responseMessage);
                foreach (var item in json)
                {
                    if (item["channelType"].GetValue<string>() == "general")
                    {
                        var tariff = new Tariff();
                        // TODO: Use Powerwall time not local
                        tariff.StartDate = item["startTime"].GetValue<DateTimeOffset>().LocalDateTime;
                        tariff.EndDate = item["endTime"].GetValue<DateTimeOffset>().LocalDateTime;
                        tariff.IsDynamic = true;
                        tariff.DynamicSellAndFeedInRate = new Tuple<decimal, decimal>(item["perKwh"].GetValue<decimal>() / 100, 0);
                        tariff.Name = "Dynamic";
                        tariffs.Add(tariff);
                    }
                    else if (item["channelType"].GetValue<string>() == "feedIn")
                    {
                        var tariff = tariffs.Where(t => t.StartDate == item["startTime"].GetValue<DateTime>()).FirstOrDefault();
                        if (tariff != null)
                        {
                            tariff.DynamicSellAndFeedInRate = new Tuple<decimal, decimal>(tariff.DynamicSellAndFeedInRate.Item1, item["perKwh"].GetValue<decimal>() / -100);
                        }
                    }
                }
                tariffCache[date] = tariffs;

            }
            return tariffCache[date];
        }


    }
}
