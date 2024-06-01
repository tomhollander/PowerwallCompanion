using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json.Linq;
using PowerwallCompanion.CustomEnergySourceProviders;
using PowerwallCompanion.ViewModels;
using System;
using System.Collections.Generic;
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
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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
        private TariffHelper tariffHelper;

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
            await RefreshTariffData(); // Refresh tariff data first, as it's used in other data refreshes
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

                var tasks = new List<Task<JObject>>()
                {
                    GetCalendarHistoryData(DateTime.Now.Date.AddDays(-1)),
                    GetCalendarHistoryData(DateTime.Now.Date)
                };
                var results = await Task.WhenAll(tasks);
                var yesterdayEnergy = results[0];
                var todayEnergy = results[1];

                viewModel.HomeEnergyYesterday = 0;
                viewModel.SolarEnergyYesterday = 0;
                viewModel.GridEnergyImportedYesterday = 0;
                viewModel.GridEnergyExportedYesterday = 0;
                viewModel.BatteryEnergyImportedYesterday = 0;
                viewModel.BatteryEnergyExportedYesterday = 0;
                foreach (var period in yesterdayEnergy["response"]["time_series"])
                {
                    viewModel.HomeEnergyYesterday += GetJsonDoubleValue(period["total_home_usage"]);
                    viewModel.SolarEnergyYesterday += GetJsonDoubleValue(period["total_solar_generation"]);
                    viewModel.GridEnergyImportedYesterday += GetJsonDoubleValue(period["grid_energy_imported"]);
                    viewModel.GridEnergyExportedYesterday += GetJsonDoubleValue(period["grid_energy_exported_from_solar"]) + GetJsonDoubleValue(period["grid_energy_exported_from_generator"]) + GetJsonDoubleValue(period["grid_energy_exported_from_battery"]);
                    viewModel.BatteryEnergyImportedYesterday += GetJsonDoubleValue(period["battery_energy_imported_from_grid"]) + GetJsonDoubleValue(period["battery_energy_imported_from_solar"]) + GetJsonDoubleValue(period["battery_energy_imported_from_generator"]);
                    viewModel.BatteryEnergyExportedYesterday += GetJsonDoubleValue(period["battery_energy_exported"]);
                }

                viewModel.HomeEnergyToday = 0;
                viewModel.SolarEnergyToday = 0;
                viewModel.GridEnergyImportedToday = 0;
                viewModel.GridEnergyExportedToday = 0;
                viewModel.BatteryEnergyImportedToday = 0;
                viewModel.BatteryEnergyExportedToday = 0;
                foreach (var period in todayEnergy["response"]["time_series"])
                {
                    viewModel.HomeEnergyToday += GetJsonDoubleValue(period["total_home_usage"]);
                    viewModel.SolarEnergyToday += GetJsonDoubleValue(period["total_solar_generation"]);
                    viewModel.GridEnergyImportedToday += GetJsonDoubleValue(period["grid_energy_imported"]);
                    viewModel.GridEnergyExportedToday += GetJsonDoubleValue(period["grid_energy_exported_from_solar"]) + GetJsonDoubleValue(period["grid_energy_exported_from_generator"]) + GetJsonDoubleValue(period["grid_energy_exported_from_battery"]);
                    viewModel.BatteryEnergyImportedToday += GetJsonDoubleValue(period["battery_energy_imported_from_grid"]) + GetJsonDoubleValue(period["battery_energy_imported_from_solar"]) + GetJsonDoubleValue(period["battery_energy_imported_from_generator"]);
                    viewModel.BatteryEnergyExportedToday += GetJsonDoubleValue(period["battery_energy_exported"]);
                }

                viewModel.EnergyHistoryLastRefreshed = DateTime.Now;
                viewModel.NotifyDailyEnergyProperties();

                if (Settings.ShowEnergyRates)
                {
                    RefreshEnergyCostData(yesterdayEnergy, todayEnergy);
                }

            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
                viewModel.LastExceptionMessage = ex.Message;
                viewModel.LastExceptionDate = DateTime.Now;
                viewModel.NotifyDailyEnergyProperties();
            }
        }

        private void RefreshEnergyCostData(JObject yesterdayEnergy, JObject todayEnergy)
        {
            try
            {   
                var yesterdayCost = tariffHelper.GetEnergyCostAndFeedInFromEnergyHistory((JArray)yesterdayEnergy["response"]["time_series"]);
                viewModel.EnergyCostYesterday = yesterdayCost.Item1;
                viewModel.EnergyFeedInYesterday = yesterdayCost.Item2;

                var todayCost = tariffHelper.GetEnergyCostAndFeedInFromEnergyHistory((JArray)todayEnergy["response"]["time_series"]);
                viewModel.EnergyCostToday = todayCost.Item1;
                viewModel.EnergyFeedInToday = todayCost.Item2;

                Analytics.TrackEvent("Energy cost data refreshed");
                viewModel.NotifyEnergyCostProperties();

            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
            }
        }

        private async Task<JObject> GetCalendarHistoryData(DateTime date)
        {
            var url = Utils.GetCalendarHistoryUrl("energy", "day", date, date.AddDays(1).AddSeconds(-1));
            return await ApiHelper.CallGetApiWithTokenRefresh(url, "CalendarHistory");
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

        private async Task RefreshTariffData()
        {
            if (tariffHelper == null && viewModel.TariffName == null && Settings.ShowEnergyRates)
            {
                try
                {
                    var ratePlan = await ApiHelper.CallGetApiWithTokenRefresh($"/api/1/energy_sites/{Settings.SiteId}/tariff_rate", "TariffRate");
                    tariffHelper = new TariffHelper(ratePlan);
                    viewModel.TariffBadgeVisibility = tariffHelper.IsSingleRatePlan ? Visibility.Collapsed : Visibility.Visible;
                }
                catch (Exception ex)
                {
                    Crashes.TrackError(ex);
                    viewModel.TariffColor = new SolidColorBrush(Windows.UI.Colors.DimGray);
                    viewModel.TariffName = "Rates unavailable";
                    viewModel.TariffBadgeVisibility = Visibility.Visible;
                }
                
            }
            if (tariffHelper != null)
            {
                try
                {
                    var tariff = tariffHelper.GetTariffForInstant(DateTime.Now);
                    var prices = tariffHelper.GetRatesForTariff(tariff);
                    viewModel.TariffName = tariff.DisplayName;
                    viewModel.TariffSellRate = prices.Item1;
                    viewModel.TariffBuyRate = prices.Item2;
                    viewModel.TariffColor = tariff.Color;
                    Analytics.TrackEvent("Tariff data refreshed");
                }
                catch (Exception ex)
                {
                    Crashes.TrackError(ex);
                    viewModel.TariffColor = new SolidColorBrush(Windows.UI.Colors.DimGray);
                    viewModel.TariffName = "Rates unavailable";
                    viewModel.TariffBadgeVisibility = Visibility.Visible;
                }

                
            }
            viewModel.NotifyTariffProperties();
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
