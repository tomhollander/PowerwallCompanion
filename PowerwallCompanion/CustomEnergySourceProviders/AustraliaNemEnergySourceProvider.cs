using PowerwallCompanion.ViewModels;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PowerwallCompanion.CustomEnergySourceProviders
{
    internal class AustraliaNemEnergySourceProvider
    {
        private const string openNemBaseUrl = "https://api.opennem.org.au";
        private const string network = "NEM";
        private List<string> networkRegions;
        private Dictionary<string, FuelTech> fuelTechs;
        private Dictionary<string, double> fuelTypeGeneration;
        private string selectedRegion;
        public AustraliaNemEnergySourceProvider(string selectedRegion)
        {
            this.selectedRegion = selectedRegion;
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
                if (selectedRegion == null || region.StartsWith(selectedRegion))
                {
                    tasks.Add(GetLatestFuelTechGenerationForRegion(region));
                }
            }
            var results = await Task.WhenAll(tasks);
            foreach (var result in results)
            {
                foreach (var key in result.Keys)
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
                if (total == 0)
                {
                    return 0;
                }
                return (int)(renewable / total * 100);
            }
        }

        public DateTime UpdatedDate
        {
            get; private set;
        }

        public GridEnergySources CurrentGenerationMix
        {
            get
            {
                var results = new GridEnergySources();

                foreach (var fuelType in fuelTypeGeneration.Keys)
                {
                    if (fuelType.Contains("solar"))
                    {
                        results.Solar += (int)fuelTypeGeneration[fuelType];
                    }
                    else if (fuelType.Contains("wind"))
                    {
                         results.Wind += (int)fuelTypeGeneration[fuelType];
                    }
                    else if (fuelType.Contains("hydro"))
                    {
                        results.Hydro += (int)fuelTypeGeneration[fuelType];
                    }
                    else if (fuelType.Contains("coal"))
                    {
                        results.Coal += (int) fuelTypeGeneration[fuelType];
                    }
                    else if (fuelType.Contains("gas"))
                    {
                        results.Gas += (int) fuelTypeGeneration[fuelType];
                    }
                    else if (fuelType.Contains("biomass"))
                    {
                        results.Biomass += (int)fuelTypeGeneration[fuelType];
                    }
                    else if (fuelType.Contains("battery"))
                    {
                        results.BatteryStorage += (int)fuelTypeGeneration[fuelType];
                    }
                    else if (fuelType.Contains("distillate"))
                    {
                        results.Oil += (int)fuelTypeGeneration[fuelType];
                    }
                    else
                    {
                        results.Unknown += (int)fuelTypeGeneration[fuelType];
                    }
                }
                // Remove any negative values that can creep in
                results.Wind = results.Wind < 0 ? 0 : results.Wind;
                results.Solar = results.Solar < 0 ? 0 : results.Solar;
                results.Coal = results.Coal < 0 ? 0 : results.Coal;
                results.Hydro = results.Hydro < 0 ? 0 : results.Hydro;
                results.Gas = results.Gas < 0 ? 0 : results.Gas;
                results.Biomass = results.Biomass < 0 ? 0 : results.Biomass;
                results.Oil = results.Oil < 0 ? 0 : results.Oil;
                results.BatteryStorage = results.BatteryStorage < 0 ? 0 : results.BatteryStorage;
                results.Unknown = results.Unknown < 0 ? 0 : results.Unknown;
                return results;
            }

        }

        private async Task<Dictionary<string, double>> GetLatestFuelTechGenerationForRegion(string region)
        {
            var results = new Dictionary<string, double>();
            var client = new HttpClient();
            var response = await client.GetStringAsync($"{openNemBaseUrl}/stats/power/network/fueltech/{network}/{region}");
            var json = JsonNode.Parse(response);
            UpdatedDate = json["created_at"].GetValue<DateTime>();
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
            var json = (JsonArray)JsonArray.Parse(response);
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