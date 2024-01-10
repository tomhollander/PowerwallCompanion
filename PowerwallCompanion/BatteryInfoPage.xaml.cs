using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Newtonsoft.Json.Linq;
using PowerwallCompanion.ViewModels;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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
                var tasks = new List<Task> { GetBatteryCapacity(), GetBatteryInfo() };
                await Task.WhenAll(tasks);
                await ProcessBatteryHistoryData();

                ViewModel.NotifyAllProperties();
            }
            catch (System.Exception ex) 
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

        private async Task GetBatteryCapacity()
        {
            var siteStatusJson = await ApiHelper.CallGetApiWithTokenRefresh($"/api/1/energy_sites/{Settings.SiteId}/site_status", "SiteStatus");
            ViewModel.SiteName = siteStatusJson["response"]["site_name"].Value<string>();
            ViewModel.TotalPackEnergy = siteStatusJson["response"]["total_pack_energy"].Value<double>();
            ViewModel.GatewayId = siteStatusJson["response"]["gateway_id"].Value<string>();
        }

        private async Task GetBatteryInfo()
        {
            var siteInfoJson = await ApiHelper.CallGetApiWithTokenRefresh($"/api/1/energy_sites/{Settings.SiteId}/site_info", "SiteInfo");
            ViewModel.NumberOfBatteries = siteInfoJson["response"]["battery_count"].Value<int>();
            ViewModel.InstallDate = siteInfoJson["response"]["installation_date"].Value<DateTime>();
        }

        private async Task SaveBatteryHistoryData()
        {
            try
            {
                var client = new HttpClient();
                var url = "https://us-east-1.aws.data.mongodb-api.com/app/powerwallcompanion-prter/endpoint/batteryHistory";
                client.DefaultRequestHeaders.Add("apiKey", Licenses.AppServicesKey);
                var body = $"{{\"siteId\": \"{Settings.SiteId}\", \"gatewayId\": \"{ViewModel.GatewayId}\", \"capacity\":{ViewModel.TotalPackEnergy}}}";
                await client.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));
            }
            catch (Exception ex) 
            {
                Crashes.TrackError(ex);
            }
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
            batteryHistoryChartData.Add(new ChartDataPoint(DateTime.Now, ViewModel.TotalPackEnergy/1000));
            ViewModel.EnoughDataToShowChart = batteryHistoryChartData.Count > 2 || ((DateTime.Now - mostRecentDate).TotalDays >= 30);
            ViewModel.BatteryHistoryChartData = batteryHistoryChartData;
            ViewModel.NotifyChartProperties();

        }

        private void enableBatteryHistory_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            ViewModel.StoreBatteryHistory = true;
            ViewModel.NotifyChartProperties();
        }
    }
}
