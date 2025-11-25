using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using PowerwallCompanion.Lib;
using PowerwallCompanion.Lib.Models;
using PowerwallCompanion.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Popups;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace PowerwallCompanion
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BatteryInfoPage : Page, INotifyPropertyChanged
    {
        private PowerwallApi powerwallApi;
        private LocalGatewayApi localGatewayApi;
        private BatteryCapacityEstimator batteryCapacityEstimator;
        private BatteryInfoViewModel _viewModel;
        public event PropertyChangedEventHandler PropertyChanged;

        public BatteryInfoPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            Telemetry.TrackEvent("BatteryInfoPage opened");
            this.ViewModel = new BatteryInfoViewModel();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (((DateTime.Now - ViewModel.LastUpdate).TotalDays > 2) || ViewModel.SettingsHaveChanged())
            {
                if (ViewModel.LoadingStateVisibility != Visibility.Visible)
                {
                    this.ViewModel = new BatteryInfoViewModel();
                    await GetData();
                }
            }
                
            base.OnNavigatedTo(e);
        }

        public BatteryInfoViewModel ViewModel
        {
            get => _viewModel;
            set
            {
                if (_viewModel != value)
                {
                    _viewModel = value;
                    OnPropertyChanged();
                }
            }
        }


        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private async Task GetData()
        {
            ViewModel.LoadingStateVisibility = Visibility.Visible;
            powerwallApi = new PowerwallApi(Settings.SiteId, new WindowsPlatformAdapter());
            localGatewayApi = new LocalGatewayApi(new WindowsPlatformAdapter());
            try
            {
                // Get Powerwall data first
                await GetPowerwallData();

                // Race to get various data sources
                var tasksToRace = new List<Task>();
                if (Settings.EstimateBatteryCapacity)
                {
                    batteryCapacityEstimator = new BatteryCapacityEstimator(powerwallApi);
                    tasksToRace.Add(GetEstimatedData());
                }
                if (Settings.UseLocalGatewayForBatteryCapacity)
                {
                    tasksToRace.Add(GetLocalGatewayData());
                }
                if (Settings.StoreBatteryHistory)
                {
                    tasksToRace.Add(LoadBatteryHistoryData());
                }
                await Task.WhenAll(tasksToRace);

                // Tasks to be done after initial data fetch
                var finalTasks = new List<Task>();
                finalTasks.Add(SendShadowCapacityEstimateTelemetryEvent());
                if (Settings.UseLocalGatewayForBatteryCapacity)
                {
                    finalTasks.Add(ProcessGatewayBatteryHistoryData());
                }

                if (Settings.EstimateBatteryCapacity)
                {
                    finalTasks.Add(ProcessEstimatedBatteryHistoryData());
                }
                await Task.WhenAll(finalTasks);
                ViewModel.LastUpdate = DateTime.Now;

            }
            catch (System.Exception ex)
            {
                Telemetry.TrackException(ex);
            }
            finally
            {
                ViewModel.NotifyAllProperties();
                ViewModel.LoadingStateVisibility = Visibility.Collapsed;
            }
            

        }

        private async Task GetPowerwallData()
        {
            try
            {
                ViewModel.EnergySiteInfo = await powerwallApi.GetEnergySiteInfo();
            }
            catch (System.Exception ex)
            {
                Telemetry.TrackException(ex);
            }
        }

        private async Task GetEstimatedData()
        {
            try
            {
                Telemetry.TrackEvent("Battery Capacity Estimate");
                ViewModel.EstimatedCapacity = await batteryCapacityEstimator.GetEstimatedBatteryCapacity(DateTime.Today);
                ViewModel.NotifyEstimatedCapacityProperties();
            }
            catch (System.Exception ex)
            {
                noEstimatesBanner.Visibility = Visibility.Visible;
                Telemetry.TrackException(ex);
            }
        }

        private async Task GetLocalGatewayData()
        { 
            try
            {
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
            }
            catch (System.Exception ex)
            {
                Telemetry.TrackException(ex);
            }
        }

        private async Task GetBatteryDetailsFromLocalGateway()
        {
            try
            {
                var response = await localGatewayApi.GetBatteryDetails(Settings.LocalGatewayIP, Settings.LocalGatewayPassword);
                ViewModel.BatteryDetails = response.BatteryDetails;

                if (response.ErrorMessage != null)
                {
                    ViewModel.GatewayError = response.ErrorMessage;
                    if (response.BatteryDetails == null)
                    {
                        gatewayErrorBanner.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        staleDataBannerTextBlock.Text += " " + Settings.CachedGatewayDetailsUpdated.ToString("g");
                        staleDataBanner.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    gatewayErrorBanner.Visibility = Visibility.Collapsed;
                    staleDataBanner.Visibility = Visibility.Collapsed;
                    Settings.CachedGatewayDetailsUpdated = DateTime.Now;
                }
                Telemetry.TrackEvent("Battery Capacity from Gateway", new Dictionary<string, string> { { "Success", (response.ErrorMessage == null).ToString() }});

            }
            catch (Exception ex)
            {
                // Shouldn't happen as we catch all exceptions in the LocalGatewayApi
                Telemetry.TrackException(ex);
            }
        }

        private async Task ProcessEstimatedBatteryHistoryData()
        {
            try
            {
                // If we have no data, start from install date
                var maxDate = new DateTime(ViewModel.EnergySiteInfo.InstallDate.Year, ViewModel.EnergySiteInfo.InstallDate.Month, 1);

                if (ViewModel.BatteryHistoryChartData.Keys.Contains("Estimated"))
                {
                    maxDate = ViewModel.BatteryHistoryChartData["Estimated"].Max(x => x.XValue);
                }

                if (maxDate < DateTime.Now.AddMonths(-2))
                {
                    loadingHistoricalEstimatesBanner.Visibility = Visibility.Visible;
                }

                // Retrieve SOE data for any months after the max date we have (always 1st of the month)
                if (maxDate < DateTime.Now.AddMonths(1))
                {
                    DateTime dateToGet = maxDate.AddMonths(1);
                    // Retrieve missing months, starting from the install date
                    while (dateToGet <= DateTime.Today)
                    {
                        try
                        {
                            var estimatedCapacity = await batteryCapacityEstimator.GetEstimatedBatteryCapacity(dateToGet);
                            await localGatewayApi.SaveBatteryHistoryDataToServer(Settings.SiteId, ViewModel.EnergySiteInfo.GatewayId, new List<BatteryDetails>()
                        {
                            new BatteryDetails()
                            {
                                SerialNumber = "Estimated",
                                FullCapacity = estimatedCapacity,
                            }
                        }, dateToGet);

                            // Add to chart data
                            if (!ViewModel.BatteryHistoryChartData.ContainsKey("Estimated"))
                            {
                                ViewModel.BatteryHistoryChartData.Add("Estimated", new ObservableCollection<ChartDataPoint>());
                                AddEstimatedSeriesToChart();
                            }
                            ViewModel.BatteryHistoryChartData["Estimated"].Add(new ChartDataPoint(dateToGet, estimatedCapacity / 1000));
                            ViewModel.NotifyChartProperties();
                        }
                        catch (Exception)
                        {
                            // Ignore for now
                        }
                        dateToGet = dateToGet.AddMonths(1);
                    }
                }
            }
            catch (Exception ex)
            {
                Telemetry.TrackException(ex);
            }
            finally
            {
                loadingHistoricalEstimatesBanner.Visibility = Visibility.Collapsed;
            }
            
        }

        private void AddEstimatedSeriesToChart()
        {
            var series = new Syncfusion.UI.Xaml.Charts.LineSeries();
            series.StrokeWidth = 2;
            series.ItemsSource = Settings.UseMovingAveragesForBatteryCapacity ?
                   ViewModel.BatteryHistoryChartDataMovingAverage["Estimated"] : ViewModel.BatteryHistoryChartData["Estimated"];
            series.Label = "Estimated";
            series.XBindingPath = nameof(ChartDataPoint.XValue);
            series.YBindingPath = nameof(ChartDataPoint.YValue);
            batteryHistoryChart.Series.Add(series);
        }

        private async Task ProcessGatewayBatteryHistoryData()
        {
            try
            {
                AddCurrentDataPointToBatteryChartData();
                await localGatewayApi.SaveBatteryHistoryDataToServer(Settings.SiteId, ViewModel.EnergySiteInfo.GatewayId, ViewModel.BatteryDetails, null);
            }
            catch (Exception ex)
            {
                Telemetry.TrackUnhandledException(ex);
            }


        }

        private async Task LoadBatteryHistoryData()
        {
            try
            {
                if (ViewModel.StoreBatteryHistory)
                {
                    ViewModel.BatteryHistoryChartData = await localGatewayApi.GetBatteryHistoryDataFromServer(Settings.SiteId, ViewModel.EnergySiteInfo.GatewayId);
                    
                    double maxValue = 0;
                    double minValue = 20;
                    // Plot series on chart
                    foreach (var serial in ViewModel.BatteryHistoryChartData.Keys)
                    {
                        string label = (serial == "Estimated") ? "Estimated" : serial.Substring(0, 5) + "***" + serial.Substring(serial.Length - 2, 2);
                        double maxValueInSeries = ViewModel.BatteryHistoryChartData[serial].Max(x => x.YValue);
                        double minValueInSeries = ViewModel.BatteryHistoryChartData[serial].Min(x => x.YValue);
                        maxValue = Math.Max(maxValue, maxValueInSeries);
                        minValue = Math.Min(minValue, minValueInSeries);

                        var existingSeries = batteryHistoryChart.Series.Where(s => s.Label == label).FirstOrDefault();
                        if (existingSeries != null)
                        {
                            batteryHistoryChart.Series.Remove(existingSeries);
                        }

                        var series = new Syncfusion.UI.Xaml.Charts.LineSeries();
                        series.StrokeWidth = 2;
                        series.ItemsSource = Settings.UseMovingAveragesForBatteryCapacity ?
                                ViewModel.BatteryHistoryChartDataMovingAverage[serial] : ViewModel.BatteryHistoryChartData[serial];
                        series.Label = label;
                        series.XBindingPath = nameof(ChartDataPoint.XValue);
                        series.YBindingPath = nameof(ChartDataPoint.YValue);

                        batteryHistoryChart.Series.Add(series);

                    }
                    int multiplier = (Settings.EstimateBatteryCapacity && ViewModel.EnergySiteInfo != null) ? ViewModel.EnergySiteInfo.NumberOfBatteries : 1;

                    ((Syncfusion.UI.Xaml.Charts.NumericalAxis)batteryHistoryChart.YAxes[0]).Maximum = Math.Max(maxValue, multiplier * 14);
                    ((Syncfusion.UI.Xaml.Charts.NumericalAxis)batteryHistoryChart.YAxes[0]).Minimum = Math.Min(minValue, multiplier * 9);
                    ViewModel.NotifyChartProperties();
                }
            }
            catch (Exception ex)
            {
                Telemetry.TrackException(ex);
            }
            
        }

        private async Task SendShadowCapacityEstimateTelemetryEvent()
        {
            try
            {
                if (ViewModel.BatteryDetails == null || ViewModel.BatteryDetails.Count == 0 || staleDataBanner.Visibility == Visibility.Visible)
                {
                    return;
                }
                var estimatedCapacity = ViewModel.EstimatedCapacity;

                double totalCapacityFromGateway = 0;
                foreach (var battery in ViewModel.BatteryDetails)
                {
                    totalCapacityFromGateway += battery.FullCapacity;
                }
                var capacityDeltaPercent = Math.Abs((estimatedCapacity - totalCapacityFromGateway) / totalCapacityFromGateway * 100);

                var dict = new Dictionary<string, string>()
                {
                    { "GatewayCapacity", totalCapacityFromGateway.ToString() },
                    { "EstimatedCapacity", estimatedCapacity.ToString() },
                    { "DeltaPercent", capacityDeltaPercent.ToString() }
                };

                Telemetry.TrackEvent("BatteryCapacity", dict);

            }
            catch (Exception ex)
            {
                Telemetry.TrackException(ex);
            }

        }

        private void AddCurrentDataPointToBatteryChartData()
        {
            if (ViewModel.BatteryDetails != null)
            {
                foreach (var battery in ViewModel.BatteryDetails)
                {
                    if (!ViewModel.BatteryHistoryChartData.ContainsKey(battery.SerialNumber))
                    {
                        ViewModel.BatteryHistoryChartData.Add(battery.SerialNumber, new ObservableCollection<ChartDataPoint>());
                    }
                    else if (ViewModel.BatteryHistoryChartData[battery.SerialNumber] == null)
                    {
                        ViewModel.BatteryHistoryChartData[battery.SerialNumber] = new ObservableCollection<ChartDataPoint>();
                    }

                    ViewModel.BatteryHistoryChartData[battery.SerialNumber].Add(new ChartDataPoint(Settings.CachedGatewayDetailsUpdated, battery.FullCapacity / 1000));
                }

            }
        }

        
        private async void enableBatteryHistory_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Telemetry.TrackEvent("Battery history enabled");
            ViewModel.StoreBatteryHistory = true;
            await ProcessGatewayBatteryHistoryData();
        }

       

        private async void HyperlinkButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var dialog = new ContentDialog()
            {
                Title = "Unable to connect to Powerwall Gateway",
                Content = ViewModel.GatewayError,
                CloseButtonText = "Ok"
            };

            dialog.XamlRoot = this.Content.XamlRoot;
            await dialog.ShowAsync();
        }
    }
}
