﻿using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Toolkit.Uwp.Notifications;
using PowerwallCompanion.CustomEnergySourceProviders;
using PowerwallCompanion.Lib;
using PowerwallCompanion.Lib.Models;
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
        private readonly TimeSpan energySiteInfoRefreshInterval = new TimeSpan(1, 0, 0);

        private double minPercentSinceNotification = 0D;
        private double maxPercentSinceNotification = 100D;
        private DispatcherTimer timer;
        private PowerwallApi powerwallApi;
        private TariffHelper tariffHelper;


        public StatusPage()
        {
            this.InitializeComponent();
            Analytics.TrackEvent("StatusPage opened");
            powerwallApi = new PowerwallApi(Settings.SiteId, new UwpPlatformAdapter());
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

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await RefreshDataFromTeslaOwnerApi();
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            timer.Stop();
            base.OnNavigatedFrom(e);
        }

        private async void Timer_Tick(object sender, object e)
        {
            await RefreshDataFromTeslaOwnerApi();
        }


        private async Task RefreshDataFromTeslaOwnerApi()
        {
            await RefreshTariffData(); // Refresh tariff data first, as it's used in other data refreshes
            var tasks = new List<Task>()
            {
                GetCurrentPowerData(),
                GetEnergyHistoryData(),
                GetPowerHistoryData(),
                RefreshGridEnergyUsageData(),
                GetEnergySiteInfo(),
            };
            await Task.WhenAll(tasks);
        }

        private async Task GetEnergySiteInfo()
        {
            try
            {
                if (ViewModel.EnergySiteInfo == null ||  (DateTime.Now - viewModel.EnergySiteInfoLastRefreshed > energySiteInfoRefreshInterval))
                {
                    ViewModel.EnergySiteInfo = await powerwallApi.GetEnergySiteInfo();
                    ViewModel.EnergySiteInfoLastRefreshed = DateTime.Now;
                    ViewModel.NotifyPropertyChanged(nameof(ViewModel.EnergySiteInfo));
                }
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
            }
        }

        private async Task GetCurrentPowerData()
        {
            try
            {
                if (DateTime.Now - viewModel.LiveStatusLastRefreshed < liveStatusRefreshInterval)
                {
                    return;
                }

                viewModel.InstantaneousPower = await powerwallApi.GetInstantaneousPower();
                await UpdateMinMaxPercentToday(); 
                viewModel.LiveStatusLastRefreshed = DateTime.Now;
                viewModel.Status = viewModel.InstantaneousPower.GridActive ? StatusViewModel.StatusEnum.Online : StatusViewModel.StatusEnum.GridOutage;
                
                viewModel.NotifyPowerProperties();


                SendNotificationsOnBatteryStatus(viewModel.InstantaneousPower.BatteryStoragePercent);
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

        private async Task UpdateMinMaxPercentToday()
        {
            if (viewModel.InstantaneousPower == null)
            {
                return;
            }
            if (viewModel.BatteryDay == DateTime.MinValue)
            {
                var minMax = await powerwallApi.GetBatteryMinMaxToday();
                viewModel.MinBatteryPercentToday = minMax.Item1;
                viewModel.MaxBatteryPercentToday = minMax.Item2;
            }
            else if (viewModel.BatteryDay != (await powerwallApi.ConvertToPowerwallDate(DateTime.Now)).Date) 
            {
                viewModel.BatteryDay = DateTime.Today;
                viewModel.MinBatteryPercentToday = viewModel.InstantaneousPower.BatteryStoragePercent;
                viewModel.MaxBatteryPercentToday = viewModel.InstantaneousPower.BatteryStoragePercent;
            }
            else if (viewModel.InstantaneousPower.BatteryStoragePercent < viewModel.MinBatteryPercentToday)
            {
                viewModel.MaxBatteryPercentToday = viewModel.InstantaneousPower.BatteryStoragePercent;
            }
            else if (viewModel.InstantaneousPower.BatteryStoragePercent > viewModel.MaxBatteryPercentToday)
            {
                viewModel.MaxBatteryPercentToday = viewModel.InstantaneousPower.BatteryStoragePercent;
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

                var tasks = new List<Task<EnergyTotals>>()
                {
                    powerwallApi.GetEnergyTotalsForDay(-1, tariffHelper),
                    powerwallApi.GetEnergyTotalsForDay(0, tariffHelper)
                };
                var results = await Task.WhenAll(tasks);
                viewModel.EnergyTotalsYesterday = results[0];
                viewModel.EnergyTotalsToday = results[1];

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

                viewModel.PowerChartSeries = await powerwallApi.GetPowerChartSeriesForLastTwoDays();

                DateTime d = await powerwallApi.ConvertToPowerwallDate(DateTime.Now);
                if (Settings.AccessToken == "DEMO")
                {
                    viewModel.ChartMaxDate = new DateTime(2018, 3, 1);
                }
                else
                {
                    viewModel.ChartMaxDate = d.Date.AddDays(1);
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
                    var ratePlan = await powerwallApi.GetRatePlan();
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
                    viewModel.TariffColor = new SolidColorBrush(WindowsColorFromDrawingColor(tariff.Color));
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

        private Windows.UI.Color WindowsColorFromDrawingColor(System.Drawing.Color c)
        {
            return new Windows.UI.Color() { A = c.A, R = c.R, G = c.G, B = c.B };
        }

      
        private async void errorIndicator_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (ViewModel.LastExceptionMessage != null)
            {
                var md = new MessageDialog($"Last error occurred at {ViewModel.LastExceptionDate.ToString("g")}:\r\n{ViewModel.LastExceptionMessage}");
                await md.ShowAsync();
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
                client.DefaultRequestHeaders.Add("auth-token", Keys.ElectricityMapsApiKey);
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
