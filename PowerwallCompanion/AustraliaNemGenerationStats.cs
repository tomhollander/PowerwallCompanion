using Microsoft.IdentityModel.Tokens;
using Syncfusion.UI.Xaml.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Windows.Media.Audio;
using Windows.Networking.Connectivity;

namespace PowerwallCompanion
{
    internal class AustraliaNemGenerationStats
    {
        private const string openNemBaseUrl = "https://api.opennem.org.au";
        private const string network = "NEM";
        private List<string> networkRegions;
        private Dictionary<string, FuelTech> fuelTechs;
        private Dictionary<string, double> fuelTypeGeneration;
        public AustraliaNemGenerationStats()
        {

        }

        private async Task Init()
        {
            Task[] tasks = { GetNetworkRegions(), GetFuelTechs() };
            await Task.WhenAll(tasks);
        }

        public async Task Refresh()
        {
            if (networkRegions == null)
            {
                await Init();
            }

            fuelTypeGeneration = new Dictionary<string, double>();
            var tasks = new List<Task<Dictionary<string, double>>>();
            foreach (var region in networkRegions)
            {
                tasks.Add(GetLatestFuelTechGenerationForRegion(region));
            }
            var results = await Task.WhenAll(tasks);
            foreach (var result in results)
            {
                foreach(var key in result.Keys)
                {
                    if (fuelTypeGeneration.ContainsKey(key))
                    {
                        fuelTypeGeneration[key] += result[key];
                    }
                    else
                    {
                        fuelTypeGeneration[key] = result[key];
                    }
                }
                
            }
        }

        public int RenewablePercent
        {
            get
            {
                double total = 0;
                double renewable = 0;
                foreach (var fuelType in fuelTypeGeneration.Keys)
                {
                    total += fuelTypeGeneration[fuelType];
                    var fuelTech = fuelTechs[fuelType];
                    if (fuelTech.IsRenewable)
                    {
                        renewable += fuelTypeGeneration[fuelType]; 
                    }
                }
                return (int) (renewable / total * 100);
            }
        }

        public Dictionary<string, double> CurrentGenerationMix
        {
            get
            {
                var results = new Dictionary<string, double>
                { { "Solar", 0 }, { "Wind", 0 }, { "Hydro", 0 }, { "Coal", 0 }, { "Gas", 0 }, { "Other", 0 }
                };
                foreach (var fuelType in fuelTypeGeneration.Keys)
                {
                    if (fuelType.Contains("solar"))
                    {
                        results["Solar"] += fuelTypeGeneration[fuelType];
                    }
                    else if (fuelType.Contains("wind"))
                    {
                        results["Wind"] += fuelTypeGeneration[fuelType];
                    }
                    else if (fuelType.Contains("hydro"))
                    {
                        results["Hydro"] += fuelTypeGeneration[fuelType];
                    }
                    else if (fuelType.Contains("coal"))
                    {
                        results["Coal"] += fuelTypeGeneration[fuelType];
                    }
                    else if (fuelType.Contains("gas"))
                    {
                        results["Gas"] += fuelTypeGeneration[fuelType];
                    }
                    else
                    {
                        results["Other"] += fuelTypeGeneration[fuelType];
                    }
                }
                return results;
            }

        }

        private async Task<Dictionary<string, double>> GetLatestFuelTechGenerationForRegion(string region)
        {
            var results = new Dictionary<string, double>();
            var client = new HttpClient();
            var response = await client.GetStringAsync($"{openNemBaseUrl}/stats/power/network/fueltech/{network}/{region}");
            var json = JsonNode.Parse(response);
            var array = (JsonArray)json["data"];


            foreach (var fuelTech in array)
            {
                string type = fuelTech["type"]?.GetValue<string>();
                string fuelTechCode = fuelTech["fuel_tech"]?.GetValue<string>();

                if (type == "power" && fuelTechCode != null && fuelTechCode != "imports" && fuelTechCode != "exports")
                {
                    var historyData = (JsonArray)fuelTech["history"]["data"];
                    var latestPower = GetLastNonZeroValue(historyData, fuelTechCode);
                    results.Add(fuelTechCode, latestPower);
                }
            }
            return results;
        }

        private double GetLastNonZeroValue(JsonArray array, string fuelTypeCode)
        {
            int valuesChecked = 0;
            int maxValuesToCheck = (fuelTypeCode == "solar_rooftop") ? 2 : 5; // Rooftop solar has a 30 minute interval so don't look back so far
            for (int i = array.Count - 1; i >= 0; i--)
            {
                if (valuesChecked++ > maxValuesToCheck) // It's probably really 0
                {
                    return 0;
                }
                if (array[i] == null) // Happens occasionally 
                {
                    return 0;
                }
                double val = array[i].GetValue<double>();
                if (val != 0)
                {
                    return val;
                }
            }
            return 0;
        }

        private async Task GetNetworkRegions()
        {
            networkRegions = new List<string>();
            var client = new HttpClient();
            var response = await client.GetStringAsync($"{openNemBaseUrl}/networks/regions?network_code={network}");
            var json = (JsonArray) JsonArray.Parse(response);
            foreach (var item in json)
            {
                networkRegions.Add(item["code"].GetValue<string>());
            }
        }

        private async Task GetFuelTechs()
        {
            fuelTechs = new Dictionary<string, FuelTech>();
            var client = new HttpClient();
            var response = await client.GetStringAsync($"{openNemBaseUrl}/fueltechs");
            var json = (JsonArray)JsonArray.Parse(response);
            foreach (var item in json)
            {
                string code = item["code"].GetValue<string>();
                var fuelTech = new FuelTech()
                {
                    Code = code,
                    Label = item["label"].GetValue<string>(),
                    IsRenewable = item["renewable"].GetValue<bool>(),
                };
                fuelTechs.Add(code, fuelTech);
            }
        }

        class FuelTech
        {
            public string Code { get; set; }
            public string Label { get; set; }
            public bool IsRenewable { get; set; }
        }
    }
}
