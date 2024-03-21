using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json.Linq;
using PowerwallCompanion.CustomEnergySourceProviders;
using PowerwallCompanion.ViewModels;
using Syncfusion.UI.Xaml.Charts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using WinRTXamlToolkit.IO.Serialization;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace PowerwallCompanion
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public sealed partial class StatusPage : Page
    {
        private StatusViewModel viewModel;
        private readonly TimeSpan liveStatusRefreshInterval = new TimeSpan(0, 0, 30);
        private readonly TimeSpan energyHistoryRefreshInterval = new TimeSpan(0, 5, 0);
        private readonly TimeSpan powerHistoryRefreshInterval = new TimeSpan(0, 5, 0);

        private double minPercentSinceNotification = 0D;
        private double maxPercentSinceNotification = 100D;
        private DispatcherTimer timer;

        public StatusPage()
        {
            this.InitializeComponent();
            Analytics.TrackEvent("StatusPage opened");
            viewModel = new StatusViewModel();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(30);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        public StatusViewModel ViewModel
        {
            get => viewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            RefreshDataFromTeslaOwnerApi();
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            timer.Stop();
            base.OnNavigatedFrom(e);
        }

        private void Timer_Tick(object sender, object e)
        {
            RefreshDataFromTeslaOwnerApi();
        }


        private async Task RefreshDataFromTeslaOwnerApi()
        {
            if (Settings.InstallationTimeZone == null)
            {
                await DateUtils.GetInstallationTimeZone();
            }
            var tasks = new List<Task>()
            {
                GetCurrentPowerData(),
                GetEnergyHistoryData(),
                GetPowerHistoryData(),
                RefreshGridEnergyUsageData(),
            };
            await Task.WhenAll(tasks);
        }

        private async Task GetCurrentPowerData()
        {
            try
            {
                if (DateTime.Now - viewModel.LiveStatusLastRefreshed < liveStatusRefreshInterval)
                {
                    return;
                }
#if FAKE
                viewModel.BatteryPercent = 72;
                viewModel.HomeValue = 1900D;
                viewModel.SolarValue = 1900D;
                viewModel.BatteryValue = -1000D;
                viewModel.GridValue = 100D;
                viewModel.GridActive = true;
#else
                var siteId = Settings.SiteId;

                var powerInfo = await ApiHelper.CallGetApiWithTokenRefresh($"/api/1/energy_sites/{siteId}/live_status", "LiveStatus");

                viewModel.BatteryPercent = GetJsonDoubleValue(powerInfo["response"]["percentage_charged"]);
                viewModel.HomeValue = GetJsonDoubleValue(powerInfo["response"]["load_power"]);
                viewModel.SolarValue = GetJsonDoubleValue(powerInfo["response"]["solar_power"]);
                viewModel.BatteryValue = GetJsonDoubleValue(powerInfo["response"]["battery_power"]);
                viewModel.GridValue = GetJsonDoubleValue(powerInfo["response"]["grid_power"]);
                viewModel.GridActive = powerInfo["response"]["grid_status"].Value<string>() != "Inactive";
                viewModel.TotalPackEnergy = GetJsonDoubleValue(powerInfo["response"]["total_pack_energy"]);
                viewModel.Status = viewModel.GridActive ? StatusViewModel.StatusEnum.Online : StatusViewModel.StatusEnum.GridOutage;
#endif
                viewModel.LiveStatusLastRefreshed = DateTime.Now;
                viewModel.NotifyPowerProperties();


                SendNotificationsOnBatteryStatus(viewModel.BatteryPercent);
            }
            catch (UnauthorizedAccessException ex)
            {
                if (this.Frame?.Content.GetType() != typeof(LoginPage))
                {
                    this.Frame?.Navigate(typeof(LoginPage));
                }

                viewModel.LastExceptionMessage = ex.Message;
                viewModel.LastExceptionDate = DateTime.Now;
                viewModel.NotifyPowerProperties();
                viewModel.Status = StatusViewModel.StatusEnum.Error;

            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
                viewModel.LastExceptionMessage = ex.Message;
                viewModel.LastExceptionDate = DateTime.Now;
                viewModel.NotifyPowerProperties();
                viewModel.Status = StatusViewModel.StatusEnum.Error;
            }
        }

        private async Task GetEnergyHistoryData()
        {
            try
            {
                if (DateTime.Now - viewModel.EnergyHistoryLastRefreshed < energyHistoryRefreshInterval)
                {
                    return;
                }

                string period = "day";
                var json = await ApiHelper.CallGetApiWithTokenRefresh($"/api/1/energy_sites/{Settings.SiteId}/history?kind=energy&period={period}", "EnergyHistory");

                var yesterday = json["response"]["time_series"][0];
                viewModel.HomeEnergyYesterday = GetJsonDoubleValue(yesterday["consumer_energy_imported_from_grid"]) + GetJsonDoubleValue(yesterday["consumer_energy_imported_from_solar"]) + GetJsonDoubleValue(yesterday["consumer_energy_imported_from_battery"]) + GetJsonDoubleValue(yesterday["consumer_energy_imported_from_generator"]);
                viewModel.SolarEnergyYesterday = GetJsonDoubleValue(yesterday["solar_energy_exported"]);
                viewModel.GridEnergyImportedYesterday = GetJsonDoubleValue(yesterday["grid_energy_imported"]);
                viewModel.GridEnergyExportedYesterday = GetJsonDoubleValue(yesterday["grid_energy_exported_from_solar"]) + GetJsonDoubleValue(yesterday["grid_energy_exported_from_battery"]);
                viewModel.BatteryEnergyImportedYesterday = GetJsonDoubleValue(yesterday["battery_energy_imported_from_grid"]) + GetJsonDoubleValue(yesterday["battery_energy_imported_from_solar"]);
                viewModel.BatteryEnergyExportedYesterday = GetJsonDoubleValue(yesterday["battery_energy_exported"]);

                var today = json["response"]["time_series"][1];
                viewModel.HomeEnergyToday = GetJsonDoubleValue(today["consumer_energy_imported_from_grid"]) + GetJsonDoubleValue(today["consumer_energy_imported_from_solar"]) + GetJsonDoubleValue(today["consumer_energy_imported_from_battery"]) + GetJsonDoubleValue(today["consumer_energy_imported_from_generator"]);
                viewModel.SolarEnergyToday = GetJsonDoubleValue(today["solar_energy_exported"]);
                viewModel.GridEnergyImportedToday = GetJsonDoubleValue(today["grid_energy_imported"]);
                viewModel.GridEnergyExportedToday = GetJsonDoubleValue(today["grid_energy_exported_from_solar"]) + GetJsonDoubleValue(today["grid_energy_exported_from_battery"]);
                viewModel.BatteryEnergyImportedToday = GetJsonDoubleValue(today["battery_energy_imported_from_grid"]) + GetJsonDoubleValue(today["battery_energy_imported_from_solar"]);
                viewModel.BatteryEnergyExportedToday = GetJsonDoubleValue(today["battery_energy_exported"]);

                viewModel.EnergyHistoryLastRefreshed = DateTime.Now;
                viewModel.NotifyDailyEnergyProperties();

            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
                viewModel.LastExceptionMessage = ex.Message;
                viewModel.LastExceptionDate = DateTime.Now;
                viewModel.NotifyDailyEnergyProperties();
            }
        }

        public async Task GetPowerHistoryData()
        {
            try
            {
                if (DateTime.Now - viewModel.PowerHistoryLastRefreshed < powerHistoryRefreshInterval)
                {
                    return;
                }

                var json = await ApiHelper.CallGetApiWithTokenRefresh($"/api/1/energy_sites/{Settings.SiteId}/history?kind=power", "PowerHistory");

                var homeGraphData = new List<ChartDataPoint>();
                var solarGraphData = new List<ChartDataPoint>();
                var gridGraphData = new List<ChartDataPoint>();
                var batteryGraphData = new List<ChartDataPoint>();

                foreach (var datapoint in (JArray)json["response"]["time_series"])
                {
                    var timestamp = DateUtils.ConvertToPowerwallDate(datapoint["timestamp"].Value<DateTime>());
                    var solarPower = datapoint["solar_power"].Value<double>() / 1000;
                    var batteryPower = datapoint["battery_power"].Value<double>() / 1000;
                    var gridPower = datapoint["grid_power"].Value<double>() / 1000;
                    var homePower = solarPower + batteryPower + gridPower;
                    homeGraphData.Add(new ChartDataPoint(timestamp, homePower));
                    solarGraphData.Add(new ChartDataPoint(timestamp, solarPower));
                    gridGraphData.Add(new ChartDataPoint(timestamp, gridPower));
                    batteryGraphData.Add(new ChartDataPoint(timestamp, batteryPower));

                    viewModel.HomeGraphData = homeGraphData;
                    viewModel.SolarGraphData = solarGraphData;
                    viewModel.BatteryGraphData = batteryGraphData;
                    viewModel.GridGraphData = gridGraphData;
                }

                viewModel.PowerHistoryLastRefreshed = DateTime.Now;
                viewModel.NotifyGraphProperties();
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
                viewModel.LastExceptionDate = DateTime.Now;
                viewModel.LastExceptionMessage = ex.Message;
            }
        }


        private static double GetJsonDoubleValue(JToken jtoken)
        {
            if (jtoken == null)
            {
                return 0;
            }
            try
            {
                return jtoken.Value<double>();
            }
            catch
            {
                return 0;
            }
        }

        private void errorIndicator_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (ViewModel.LastExceptionMessage != null)
            {
                var md = new MessageDialog($"Last error occurred at {ViewModel.LastExceptionDate.ToString("g")}:\r\n{ViewModel.LastExceptionMessage}");
                md.ShowAsync();
            }
        }

        bool notifyCheckRunBefore;
        private void SendNotificationsOnBatteryStatus(double newPercent)
        {
            minPercentSinceNotification = Math.Min(minPercentSinceNotification, newPercent);
            maxPercentSinceNotification = Math.Max(maxPercentSinceNotification, newPercent);
            if (newPercent >= 99.6D && minPercentSinceNotification < 80D)
            {
                if (notifyCheckRunBefore) // Don't send notifications on the very first run
                {
                    new ToastContentBuilder()
                        .AddText("Powerwall is now fully charged")
                        .Show();
                    if (Settings.PlaySounds)
                    {
                        var mediaPlayer = new MediaPlayer();
                        mediaPlayer.Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/battery-full.wav"));
                        mediaPlayer.Play();
                    }
                }

                minPercentSinceNotification = 100D;
            }
            else if (newPercent <= 0.5D && maxPercentSinceNotification > 20D)
            {
                if (notifyCheckRunBefore) // Don't send notifications on the very first run
                {
                    new ToastContentBuilder()
                    .AddText("Powerwall is now fully discharged")
                    .Show();

                    if (Settings.PlaySounds)
                    {
                        var mediaPlayer = new MediaPlayer();
                        mediaPlayer.Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/battery-empty.wav"));
                        mediaPlayer.Play();
                    }
                }
                maxPercentSinceNotification = 0D;
            }
            notifyCheckRunBefore = true;
        }

        DateTime _energyUsageDataLastUpdated;
        private async Task RefreshGridEnergyUsageData()
        {
            if (Settings.ShowEnergySources)
            {
                if ((DateTime.Now - _energyUsageDataLastUpdated).TotalMinutes > 15)
                {
                    if (IsNemRegion(Settings.EnergySourcesZoneOverride))
                    {
                        await RefreshGridEnergyUsageDataFromOpenNem();
                    }
                    else
                    {
                        await RefreshGridEnergyUsageDataFromElectricityMaps();
                    }
                    _energyUsageDataLastUpdated = DateTime.Now;
                }
            }
        }

        private bool IsNemRegion(string energySourcesZoneOverride)
        {
            return (energySourcesZoneOverride == "AU" ||
                energySourcesZoneOverride == "AU-NSW" ||
                energySourcesZoneOverride == "AU-QLD" ||
                energySourcesZoneOverride == "AU-VIC" ||
                energySourcesZoneOverride == "AU-SA" ||
                energySourcesZoneOverride == "AU-TAS");
        }

        private async Task RefreshGridEnergyUsageDataFromOpenNem()
        { 
            try
            {
                string zone = Settings.EnergySourcesZoneOverride == "AU" ? null : 
                    Settings.EnergySourcesZoneOverride.Substring(3);
                var provider = new AustraliaNemEnergySourceProvider(zone);
                await provider.Refresh();
                Analytics.TrackEvent("GridEnergyUsage Refreshed", new Dictionary<string, string> { { "Zone", Settings.EnergySourcesZoneOverride }, { "Provider", "OpenNEM" } });
                ViewModel.GridEnergySources = provider.CurrentGenerationMix;
                ViewModel.GridLowCarbonPercent = provider.RenewablePercent;
                viewModel.GridEnergySourcesStatusMessage = $"Energy sources for zone '{Settings.EnergySourcesZoneOverride}', OpenNEM data from {provider.UpdatedDate.ToString("g")}";
                return;
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
            }

        }


        private async Task RefreshGridEnergyUsageDataFromElectricityMaps()
        { 
            try
            {

                string locationQueryString = "";
                if (Settings.EnergySourcesZoneOverride != null)
                {
                    locationQueryString = $"zone={Settings.EnergySourcesZoneOverride}";
                }
                else
                {
                    var accessStatus = await Geolocator.RequestAccessAsync();
                    if (accessStatus == GeolocationAccessStatus.Allowed)
                    {
                        var geolocator = new Geolocator();
                        var pos = await geolocator.GetGeopositionAsync();
                        locationQueryString = $"lat={pos.Coordinate.Point.Position.Latitude}&lon={pos.Coordinate.Point.Position.Longitude}";
                    }
                }

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("auth-token", Licenses.ElectricityMapsApiKey);
                var url = $"https://api.electricitymap.org/v3/power-breakdown/latest?{locationQueryString}&xx={new Random().Next()}";
                var response = await client.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();
                var json = JsonNode.Parse(responseContent);
                if (json["error"]?.GetValue<string>() != null)
                {
                    viewModel.GridEnergySourcesStatusMessage = $"No energy source data available for selected zone.";
                    _energyUsageDataLastUpdated = DateTime.Now;
                    return; // No data available
                }
                var zone = json["zone"].GetValue<string>();
                if (IsNemRegion(zone)) // Save geolocated zone for NEM
                {
                    Settings.EnergySourcesZoneOverride = zone;
                    await RefreshGridEnergyUsageDataFromOpenNem();
                    return;
                }
                var date = DateTime.Parse(json["datetime"].GetValue<string>());
                
                var jsonEnergy = json["powerConsumptionBreakdown"];
                var energyUsage = new GridEnergySources()
                {
                    Solar = jsonEnergy["solar"].GetValue<int>(),
                    Wind = jsonEnergy["wind"].GetValue<int>(),
                    Nuclear = jsonEnergy["nuclear"].GetValue<int>(),
                    Geothermal = jsonEnergy["geothermal"].GetValue<int>(),
                    Biomass = jsonEnergy["biomass"].GetValue<int>(),
                    Coal = jsonEnergy["coal"].GetValue<int>(),
                    Hydro = jsonEnergy["hydro"].GetValue<int>(),
                    HydroStorage = jsonEnergy["hydro discharge"].GetValue<int>(),
                    BatteryStorage = jsonEnergy["battery discharge"].GetValue<int>(),
                    Oil = jsonEnergy["oil"].GetValue<int>(),
                    Gas = jsonEnergy["gas"].GetValue<int>(),
                    Unknown = jsonEnergy["unknown"].GetValue<int>(),

                };
                viewModel.GridEnergySourcesStatusMessage = $"Energy sources for zone '{zone}', data from {date.ToString("g")}";
                ViewModel.GridEnergySources = energyUsage;
                ViewModel.GridLowCarbonPercent = json["fossilFreePercentage"].GetValue<int>();
                Analytics.TrackEvent("GridEnergyUsage Refreshed", new Dictionary<string, string> { { "Zone", zone }, { "Provider", "ElectricityMaps.com" } });
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
            }

        }

    }
}
