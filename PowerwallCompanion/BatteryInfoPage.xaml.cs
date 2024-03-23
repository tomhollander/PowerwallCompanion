using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Newtonsoft.Json.Linq;
using PowerwallCompanion.ViewModels;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace PowerwallCompanion
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BatteryInfoPage : Page
    {
        public BatteryInfoPage()
        {
            this.InitializeComponent();
            Analytics.TrackEvent("BatteryInfoPage opened");
            this.ViewModel = new BatteryInfoViewModel();
            ViewModel.NumberOfBatteries = 1;

            GetData();
        }

        public BatteryInfoViewModel ViewModel { get; set; }

        private async Task GetData()
        {
            try
            {
                var tasks = new List<Task> { GetSiteStatus(), GetBatteryInfo() };
                if (String.IsNullOrEmpty(Settings.LocalGatewayIP) || String.IsNullOrEmpty(Settings.LocalGatewayPassword))
                {
                    noGatewayBanner.Visibility = Visibility.Visible;
                }
                else
                {
                    tasks.Add(GetBatteryDetailsFromLocalGateway());
                }

                await Task.WhenAll(tasks);
                ViewModel.NotifyAllProperties();

                await ProcessBatteryHistoryData();
            }
            catch (System.Exception ex) 
            {
                Crashes.TrackError(ex);
            }
        }
        
        private async Task GetBatteryDetailsFromLocalGateway()
        {
            try
            {
                JsonObject json = null;
                try
                {
                    json = await GatewayApiHelper.CallGetApi("/api/system_status");
                    await SaveGatewayDetailsToCache(json);
                    Settings.CachedGatewayDetailsUpdated = DateTime.Now;
                }
                catch
                {
                    var cachedData = await ReadGatewayDetailsFromCache();
                    if (cachedData == null)
                    {
                        gatewayErrorBanner.Visibility = Visibility.Visible;
                        return;
                    }
                    else
                    {
                        staleDataBannerTextBlock.Text += " " + Settings.CachedGatewayDetailsUpdated.ToString("g");
                        staleDataBanner.Visibility = Visibility.Visible;
                        json = cachedData;
                    }
                }
                ViewModel.BatteryDetails = new List<BatteryDetails>();
                foreach (var batteryBlock in (JsonArray)json["battery_blocks"])
                {
                    ViewModel.BatteryDetails.Add(new BatteryDetails
                    {
                        SerialNumber = batteryBlock["PackageSerialNumber"].GetValue<string>(),
                        FullCapacity = batteryBlock["nominal_full_pack_energy"].GetValue<double>(),
                        CurrentChargeLevel = batteryBlock["nominal_energy_remaining"].GetValue<double>()
                    });
                }
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
            }
        }

        private async Task ProcessBatteryHistoryData()
        {
            if (ViewModel.StoreBatteryHistory)
            {
                await GetBatteryHistoryData();
                if (ViewModel.BatteryHistoryChartData?.Count > 1) // Last record is today's data, just for show
                {
                    var lastSaved = ViewModel.BatteryHistoryChartData[ViewModel.BatteryHistoryChartData.Count - 2].XValue;
                    if ((DateTime.Now - lastSaved).TotalDays > 30)
                    {
                        await SaveBatteryHistoryData();
                    }
                }
                else
                {
                    // First time here!
                    await SaveBatteryHistoryData();
                }
            }
        }

        private async Task GetSiteStatus()
        {
            try
            {
                var siteStatusJson = await ApiHelper.CallGetApiWithTokenRefresh($"/api/1/energy_sites/{Settings.SiteId}/site_status", "SiteStatus");
                ViewModel.SiteName = siteStatusJson["response"]["site_name"].Value<string>();
                ViewModel.GatewayId = siteStatusJson["response"]["gateway_id"].Value<string>();
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
            }

        }

        private async Task GetBatteryInfo()
        {
            var siteInfoJson = await ApiHelper.CallGetApiWithTokenRefresh($"/api/1/energy_sites/{Settings.SiteId}/site_info", "SiteInfo");
            ViewModel.NumberOfBatteries = siteInfoJson["response"]["battery_count"].Value<int>();
            ViewModel.InstallDate = siteInfoJson["response"]["installation_date"].Value<DateTime>();
        }

        private async Task SaveBatteryHistoryData()
        {
            //try
            //{
            //    if (ViewModel.TotalPackEnergy == 0)
            //    {
            //        return; // Don't save invalid data
            //    }
            //    var client = new HttpClient();
            //    var url = "https://us-east-1.aws.data.mongodb-api.com/app/powerwallcompanion-prter/endpoint/batteryHistory";
            //    client.DefaultRequestHeaders.Add("apiKey", Licenses.AppServicesKey);
            //    var body = $"{{\"siteId\": \"{Settings.SiteId}\", \"gatewayId\": \"{ViewModel.GatewayId}\", \"capacity\":{ViewModel.TotalPackEnergy}}}";
            //    await client.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));
            //}
            //catch (Exception ex) 
            //{
            //    Crashes.TrackError(ex);
            //}
        }

        public async Task GetBatteryHistoryData()
        {
            var batteryHistoryChartData = new List<ChartDataPoint>();
            DateTime mostRecentDate = DateTime.Now;
            try
            {
                var client = new HttpClient();
                var url = $"https://us-east-1.aws.data.mongodb-api.com/app/powerwallcompanion-prter/endpoint/batteryHistory?siteId={Settings.SiteId}&gatewayId={ViewModel.GatewayId}";
                client.DefaultRequestHeaders.Add("apiKey", Licenses.AppServicesKey);
                var response = await client.GetAsync(url);
                var responseMessage = await response.Content.ReadAsStringAsync();
                if (!String.IsNullOrEmpty(responseMessage) && responseMessage != "null")
                {
                    var json = JObject.Parse(responseMessage);
                    foreach (var history in json["batteryHistory"])
                    {
                        batteryHistoryChartData.Add(new ChartDataPoint(xValue: history["date"].Value<DateTime>(), yValue: history["capacity"].Value<double>()/1000));
                        mostRecentDate = history["date"].Value<DateTime>();
                    }
                }
            }
            catch (Exception ex) 
            {
                Crashes.TrackError(ex);
            }
            //if (ViewModel.TotalPackEnergy > 0)
            //{
            //    batteryHistoryChartData.Add(new ChartDataPoint(DateTime.Now, ViewModel.TotalPackEnergy / 1000));
            //}
            
            ViewModel.EnoughDataToShowChart = batteryHistoryChartData.Count > 2 || ((DateTime.Now - mostRecentDate).TotalDays >= 7);
            ViewModel.BatteryHistoryChartData = batteryHistoryChartData;
            ViewModel.NotifyChartProperties();

        }

        private async void enableBatteryHistory_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            Analytics.TrackEvent("Battery history enabled");
            ViewModel.StoreBatteryHistory = true;
            await ProcessBatteryHistoryData();
        }

        private async Task SaveGatewayDetailsToCache(JsonObject json)
        {
            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            Windows.Storage.StorageFile cacheFile = await storageFolder.CreateFileAsync("gateway_system_status.json", Windows.Storage.CreationCollisionOption.ReplaceExisting);
            await Windows.Storage.FileIO.WriteTextAsync(cacheFile, json.ToString());
        }

        private async Task<JsonObject> ReadGatewayDetailsFromCache()
        {
            try
            {
                Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                Windows.Storage.StorageFile cacheFile = await storageFolder.GetFileAsync("gateway_system_status.json");
                string text = await Windows.Storage.FileIO.ReadTextAsync(cacheFile);
                return (JsonObject)JsonObject.Parse(text);
            }
            catch
            {
                return null;
            }
        }

    }
}
