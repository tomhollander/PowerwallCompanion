using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Newtonsoft.Json.Linq;
using PowerwallCompanion.Lib;
using PowerwallCompanion.Lib.Models;
using PowerwallCompanion.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;

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
            GetData();
        }

        public BatteryInfoViewModel ViewModel { get; set; }

        private async Task GetData()
        {
            try
            {
                var powerwallApi = new PowerwallApi(Settings.SiteId, new UwpPlatformAdapter());
                ViewModel.EnergySiteInfo = await powerwallApi.GetEnergySiteInfo();
                if (String.IsNullOrEmpty(Settings.LocalGatewayIP) || String.IsNullOrEmpty(Settings.LocalGatewayPassword))
                {
                    noGatewayBanner.Visibility = Visibility.Visible;
                }
                else
                {
                    await GetBatteryDetailsFromLocalGateway();
                }

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
                    Analytics.TrackEvent("Gateway data retrieved");
                }
                catch (Exception ex)
                {
                    Crashes.TrackError(ex);
                    ViewModel.GatewayError = FormatException(ex);
                    var cachedData = await ReadGatewayDetailsFromCache();
                    if (cachedData == null)
                    {
                        gatewayErrorBanner.Visibility = Visibility.Visible;
                        Analytics.TrackEvent("Gateway data not retrieved", new Dictionary<string, string> { { "CacheAvailable", "false" } });
                        return;
                    }
                    else
                    {
                        Analytics.TrackEvent("Gateway data not retrieved", new Dictionary<string, string> { { "CacheAvailable", "true" } });
                        staleDataBannerTextBlock.Text += " " + Settings.CachedGatewayDetailsUpdated.ToString("g");
                        staleDataBanner.Visibility = Visibility.Visible;
                        json = cachedData;
                        ViewModel.CachedData = true;
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

        private string FormatException(Exception ex)
        {
            string message = ex.GetType() + ": " + ex.Message;
            if (ex.InnerException != null)
            {
                message += "\n" + FormatException(ex.InnerException);
            }
            return message;
        }

        private async Task ProcessBatteryHistoryData()
        {
            try
            {
                if (ViewModel.StoreBatteryHistory)
                {
                    await GetBatteryHistoryData();
                    double maxValue = 0;
                    double minValue = 20;
                    // Plot series on chart
                    foreach (var serial in ViewModel.BatteryHistoryChartData.Keys)
                    {
                        var series = new Syncfusion.UI.Xaml.Charts.LineSeries();
                        series.StrokeThickness = 1;
                        series.ItemsSource = ViewModel.BatteryHistoryChartData[serial];
                        series.Label = serial.Substring(0, 5) + "***" + serial.Substring(serial.Length - 2, 2); ;
                        series.XBindingPath = nameof(ChartDataPoint.XValue);
                        series.YBindingPath = nameof(ChartDataPoint.YValue);
                        series.AdornmentsInfo = new Syncfusion.UI.Xaml.Charts.ChartAdornmentInfo()
                        {
                            SymbolStroke = new SolidColorBrush(Colors.Black),
                            SymbolInterior = series.Stroke,
                            SymbolWidth = 10,
                            SymbolHeight = 10,
                            Symbol = Syncfusion.UI.Xaml.Charts.ChartSymbol.Ellipse,
                        };
                        batteryHistoryChart.Series.Add(series);
                        double maxValueInSeries = ViewModel.BatteryHistoryChartData[serial].Max(x => x.YValue);
                        double minValueInSeries = ViewModel.BatteryHistoryChartData[serial].Min(x => x.YValue);
                        maxValue = Math.Max(maxValue, maxValueInSeries);
                        minValue = Math.Min(minValue, minValueInSeries);
                    }
                    ((Syncfusion.UI.Xaml.Charts.NumericalAxis)batteryHistoryChart.SecondaryAxis).Maximum = Math.Max(maxValue, 14);
                    ((Syncfusion.UI.Xaml.Charts.NumericalAxis)batteryHistoryChart.SecondaryAxis).Minimum = Math.Min(minValue, 9);


                    if (ViewModel.BatteryHistoryChartData?.Count > 1) // Last record is today's data, just for show
                    {
                        var lastSaved = ViewModel.BatteryHistoryChartData.Values.First()[ViewModel.BatteryHistoryChartData.Count - 2].XValue;
                        SaveBatteryHistoryData();

                    }
                    else
                    {
                        // First time here!
                        await SaveBatteryHistoryData();
                    }
                }
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
            }
            
        }


        private async Task SaveBatteryHistoryData()
        {
            try
            {
                if (!ViewModel.CachedData && ViewModel.BatteryDetails != null)
                {
                    var client = new HttpClient();
                    var url = "https://us-east-1.aws.data.mongodb-api.com/app/powerwallcompanion-prter/endpoint/granularBatteryHistory";
                    client.DefaultRequestHeaders.Add("apiKey", Keys.AppServicesKey);
                    var sb = new StringBuilder();
                    foreach (var battery in ViewModel.BatteryDetails)
                    {
                        sb.Append("\"" + battery.SerialNumber + "\": ");
                        sb.Append(battery.FullCapacity);
                        sb.Append(", ");
                    }
                    if (sb.Length > 0)
                    {
                        sb.Remove(sb.Length - 2, 2);
                    }
                    var body = $"{{\"siteId\": \"{Settings.SiteId}\", \"gatewayId\": \"{ViewModel.EnergySiteInfo.GatewayId}\", \"batteryData\": {{ {sb.ToString()} }}}}";
                    var response = await client.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));
                }

            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
            }
        }

        public async Task GetBatteryHistoryData()
        {
            var batteryHistoryChartDictionary = new Dictionary<string, List<ChartDataPoint>>();

            DateTime mostRecentDate = DateTime.MinValue;
            try
            {
                var client = new HttpClient();
                var url = $"https://us-east-1.aws.data.mongodb-api.com/app/powerwallcompanion-prter/endpoint/batteryHistory?siteId={Settings.SiteId}&gatewayId={ViewModel.EnergySiteInfo.GatewayId}";
                client.DefaultRequestHeaders.Add("apiKey", Keys.AppServicesKey);
                var response = await client.GetAsync(url);
                var responseMessage = await response.Content.ReadAsStringAsync();
                if (!String.IsNullOrEmpty(responseMessage) && responseMessage != "null")
                {
                    var json = JObject.Parse(responseMessage);

                    if (json["batteryGranularHistory"] != null)
                    {
                        foreach (var property in (JObject)json["batteryGranularHistory"])
                        {
                            var batteryHistoryChartData = new List<ChartDataPoint>();
                            string serial = property.Key;
                            foreach (var history in (JArray)property.Value)
                            {
                                batteryHistoryChartData.Add(new ChartDataPoint(xValue: history["date"].Value<DateTime>(), yValue: history["capacity"].Value<double>() / 1000));
                                var currentDate = history["date"].Value<DateTime>();
                                mostRecentDate = currentDate > mostRecentDate ? currentDate : mostRecentDate;
                            }
                            batteryHistoryChartDictionary.Add(serial, batteryHistoryChartData);
                        }
                    }
                    else
                    {
                        // Legacy data before we could break it down by battery
                        var batteryHistoryChartData = new List<ChartDataPoint>();
                        foreach (var history in json["batteryHistory"])
                        {
                            if (history["capacity"].Value<double>() < 15000)
                            {
                                batteryHistoryChartData.Add(new ChartDataPoint(xValue: history["date"].Value<DateTime>(), yValue: history["capacity"].Value<double>() / 1000));
                                var currentDate = history["date"].Value<DateTime>();
                                mostRecentDate = currentDate > mostRecentDate ? currentDate : mostRecentDate;
                            }
                        }
  
                        if (ViewModel.BatteryDetails != null)
                        {
                            // Replace with individual battery data
                            batteryHistoryChartDictionary.Add(ViewModel.BatteryDetails.First().SerialNumber, batteryHistoryChartData);
                        }   
                    }

                }

                // Add current data point
                if (ViewModel.BatteryDetails != null)
                {
                    foreach (var battery in ViewModel.BatteryDetails)
                    {
                        if (!batteryHistoryChartDictionary.ContainsKey(battery.SerialNumber))
                        {
                            batteryHistoryChartDictionary.Add(battery.SerialNumber, new List<ChartDataPoint>());
                        }

                        batteryHistoryChartDictionary[battery.SerialNumber].Add(new ChartDataPoint(Settings.CachedGatewayDetailsUpdated, battery.FullCapacity / 1000));
                    }

                }
                ViewModel.BatteryHistoryChartData = batteryHistoryChartDictionary;
                ViewModel.EnoughDataToShowChart = (batteryHistoryChartDictionary.Count > 0 && batteryHistoryChartDictionary[batteryHistoryChartDictionary.Keys.First()].Count > 2) ||
                    ((mostRecentDate > DateTime.MinValue) && ((DateTime.Now - mostRecentDate).TotalDays >= 7));


            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
            }
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

        private void HyperlinkButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var md = new MessageDialog(ViewModel.GatewayError);
            md.Title = "Unable to connect to Powerwall Gateway";
            md.ShowAsync();
        }
    }
}
