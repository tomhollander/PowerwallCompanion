using PowerwallCompanion.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace PowerwallCompanion.CustomEnergySourceProviders
{
    internal class AustraliaNemEnergySourceProvider
    {
        private const string openNemBaseUrl = "https://api.openelectricity.org.au";
        private const string network = "NEM";
        private List<Tuple<string, string>> networkRegions = new List<Tuple<string, string>>()
        {
            new Tuple<string, string>("NEM", null),
            new Tuple<string, string>("NEM", "NSW1"),
            new Tuple<string, string>("NEM", "QLD1"),
            new Tuple<string, string>("NEM", "SA1"),
            new Tuple<string, string>("NEM", "TAS1"),
            new Tuple<string, string>("NEM", "VIC1"),
            new Tuple<string, string>("WEM", "WEM"),
        };

        private Dictionary<string, FuelTech> fuelTechs;
        private Dictionary<string, double> fuelTypeGeneration;
        private string selectedRegion;
        public AustraliaNemEnergySourceProvider(string selectedRegion)
        {
            this.selectedRegion = selectedRegion;
            GetFuelTechs();
        }


        public async Task Refresh()
        {
            fuelTypeGeneration = new Dictionary<string, double>();

            Tuple<string, string> region = null;
            if (selectedRegion == null)
            {
                region = networkRegions.First();
            }
            else
            {
                region = networkRegions.Find(r => r.Item2 != null && r.Item2.StartsWith(selectedRegion));
            }
            var results = await GetLatestFuelTechGenerationForRegion(region);

   
            foreach (var key in results.Keys)
            {
                if (fuelTypeGeneration.ContainsKey(key))
                {
                    fuelTypeGeneration[key] += results[key];
                }
                else
                {
                    fuelTypeGeneration[key] = results[key];
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
                    else if (fuelType.Contains("bioenergy"))
                    {
                        results.Biomass += (int)fuelTypeGeneration[fuelType];
                    }
                    else if (fuelType.Contains("battery_discharging"))
                    {
                        results.BatteryStorage += (int)fuelTypeGeneration[fuelType];
                    }
                    else if (fuelType.Contains("distillate"))
                    {
                        results.Oil += (int)fuelTypeGeneration[fuelType];
                    }
                    else if (fuelType.Contains("battery_charging") || fuelType.Contains("pumps"))
                    {
                        // Skip, we don't want to count battery charge as generation
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

        private async Task<Dictionary<string, double>> GetLatestFuelTechGenerationForRegion(Tuple<string, string> region)
        {
            var results = new Dictionary<string, double>();
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Keys.OpenElectricityApiKey);
            string networkRegionQuery = region.Item2 == null ? "" : $"&network_region={region.Item2}";
            var response = await client.GetStringAsync($"{openNemBaseUrl}/v4/data/network/{region.Item1}?metrics=power{networkRegionQuery}&secondary_grouping=fueltech_group");
            var json = JsonNode.Parse(response);
            UpdatedDate = json["created_at"].GetValue<DateTime>();
            var dataArray = (JsonArray)json["data"];
            if (dataArray == null || dataArray.Count == 0)
            {
                return results;
            }
            var resultsArray = (JsonArray)dataArray[0]["results"];
            if (resultsArray == null || resultsArray.Count == 0)
            {
                return results;
            }


            foreach (var fuelTech in resultsArray)
            {
                string groupName = fuelTech["name"]?.GetValue<string>();
                string fuelTechCode = null;

                if (groupName.Contains("|"))
                {
                    fuelTechCode = groupName.Substring(groupName.IndexOf('|') + 1); // Format is "power_NSW1|battery_charging",
                }
                else if(groupName.StartsWith("power_"))
                {
                    fuelTechCode = groupName.Substring(6); // Format is "power_battery_charging",
                }
                else
                {
                    continue;
                }

                var historyData = (JsonArray)fuelTech["data"];
                var latestPower = GetLastNonZeroValue(historyData);
                results.Add(fuelTechCode, latestPower);
     
            }
            return results;
        }

        private double GetLastNonZeroValue(JsonArray array)
        {
            int valuesChecked = 0;
            int maxValuesToCheck =  5; 
            for (int i = array.Count - 1; i >= 0; i--)
            {
                if (valuesChecked++ > maxValuesToCheck) // It's probably really 0
                {
                    return 0;
                }
                double val = array[i][1].GetValue<double>();
                if (val != 0)
                {
                    return val;
                }
            }
            return 0;
        }


        private void GetFuelTechs()
        {
            fuelTechs = new Dictionary<string, FuelTech>();
            fuelTechs.Add("coal", new FuelTech() { Code = "coal", Label = "Coal", IsRenewable = false });
            fuelTechs.Add("gas", new FuelTech() { Code = "gas", Label = "Gas", IsRenewable = false });
            fuelTechs.Add("wind", new FuelTech() { Code = "wind", Label = "Wind", IsRenewable = true });
            fuelTechs.Add("solar", new FuelTech() { Code = "solar", Label = "Solar", IsRenewable = true });
            fuelTechs.Add("battery_charging", new FuelTech() { Code = "battery_charging", Label = "Battery (Charging)", IsRenewable = false });
            fuelTechs.Add("battery_discharging", new FuelTech() { Code = "battery_discharging", Label = "Battery (Charging)", IsRenewable = true });
            fuelTechs.Add("hydro", new FuelTech() { Code = "hydro", Label = "Hydro", IsRenewable = true });
            fuelTechs.Add("distillate", new FuelTech() { Code = "distillate", Label = "Distillate", IsRenewable = false });
            fuelTechs.Add("bioenergy", new FuelTech() { Code = "bioenergy", Label = "Bioenergy", IsRenewable = true });
            fuelTechs.Add("pumps", new FuelTech() { Code = "pumps", Label = "Pumps", IsRenewable = false });
        }

        class FuelTech
        {
            public string Code { get; set; }
            public string Label { get; set; }
            public bool IsRenewable { get; set; }
        }
    }
}