using PowerwallCompanion.Lib;
using PowerwallCompanion.Lib.Models;
using PowerwallCompanion.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Input;

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
            Telemetry.TrackEvent("BatteryInfoPage opened");
            this.ViewModel = new BatteryInfoViewModel();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            await GetData();
            base.OnNavigatedTo(e);
        }

        public BatteryInfoViewModel ViewModel { get; set; }

        private async Task GetData()
        {
            try
            {
                var powerwallApi = new PowerwallApi(Settings.SiteId, new WindowsPlatformAdapter());
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
                Telemetry.TrackException(ex);
            }
        }

        private async Task GetBatteryDetailsFromLocalGateway()
        {
            try
            {
                var gatewayApi = new LocalGatewayApi(new WindowsPlatformAdapter());
                var response = await gatewayApi.GetBatteryDetails(Settings.LocalGatewayIP, Settings.LocalGatewayPassword);
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

            }
            catch (Exception ex)
            {
                // Shouldn't happen as we catch all exceptions in the LocalGatewayApi
                Telemetry.TrackException(ex);
            }
        }

        private async Task ProcessBatteryHistoryData()
        {
            try
            {
                var localGatewayApi = new LocalGatewayApi(new WindowsPlatformAdapter());
                if (ViewModel.StoreBatteryHistory)
                {
                    ViewModel.BatteryHistoryChartData = await localGatewayApi.GetBatteryHistoryDataFromServer(Settings.SiteId, ViewModel.EnergySiteInfo.GatewayId);
                    AddCurrentDataPointToBatteryChartData();
                    await localGatewayApi.SaveBatteryHistoryDataToServer(Settings.SiteId, ViewModel.EnergySiteInfo.GatewayId, ViewModel.BatteryDetails);

                    double maxValue = 0;
                    double minValue = 20;
                    // Plot series on chart
                    foreach (var serial in ViewModel.BatteryHistoryChartData.Keys)
                    {
                        var series = new Syncfusion.UI.Xaml.Charts.LineSeries();
                        series.StrokeWidth = 1;
                        series.ItemsSource = ViewModel.BatteryHistoryChartData[serial];
                        series.Label = serial.Substring(0, 5) + "***" + serial.Substring(serial.Length - 2, 2); ;
                        series.XBindingPath = nameof(ChartDataPoint.XValue);
                        series.YBindingPath = nameof(ChartDataPoint.YValue);
                        //series.AdornmentsInfo = new Syncfusion.UI.Xaml.Charts.ChartAdornmentInfo()
                        //{
                        //    SymbolStroke = new SolidColorBrush(Colors.Black),
                        //    SymbolInterior = series.Stroke,
                        //    SymbolWidth = 10,
                        //    SymbolHeight = 10,
                        //    Symbol = Syncfusion.UI.Xaml.Charts.ChartSymbol.Ellipse,
                        //};
                        batteryHistoryChart.Series.Add(series);
                        double maxValueInSeries = ViewModel.BatteryHistoryChartData[serial].Max(x => x.YValue);
                        double minValueInSeries = ViewModel.BatteryHistoryChartData[serial].Min(x => x.YValue);
                        maxValue = Math.Max(maxValue, maxValueInSeries);
                        minValue = Math.Min(minValue, minValueInSeries);
                    }
                    ((Syncfusion.UI.Xaml.Charts.NumericalAxis)batteryHistoryChart.YAxes[0]).Maximum = Math.Max(maxValue, 14);
                    ((Syncfusion.UI.Xaml.Charts.NumericalAxis)batteryHistoryChart.YAxes[0]).Minimum = Math.Min(minValue, 9);
                    ViewModel.NotifyChartProperties();
                }
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
                        ViewModel.BatteryHistoryChartData.Add(battery.SerialNumber, new List<ChartDataPoint>());
                    }
                    else if (ViewModel.BatteryHistoryChartData[battery.SerialNumber] == null)
                    {
                        ViewModel.BatteryHistoryChartData[battery.SerialNumber] = new List<ChartDataPoint>();
                    }

                    ViewModel.BatteryHistoryChartData[battery.SerialNumber].Add(new ChartDataPoint(Settings.CachedGatewayDetailsUpdated, battery.FullCapacity / 1000));
                }

            }
        }

        
        private async void enableBatteryHistory_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Telemetry.TrackEvent("Battery history enabled");
            ViewModel.StoreBatteryHistory = true;
            await ProcessBatteryHistoryData();
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
