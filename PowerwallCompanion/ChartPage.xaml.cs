using PowerwallCompanion.Lib;
using PowerwallCompanion.Lib.Models;
using PowerwallCompanion.ViewModels;
using Syncfusion.UI.Xaml.Charts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage.Pickers;
using WinRT.Interop;
using System.Formats.Tar;
using Microsoft.UI.Xaml.Shapes;
using System.Globalization;
// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace PowerwallCompanion
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ChartPage : Page
    {
        public ChartViewModel ViewModel { get; set; }
        private Task ratePlanTask;
        private DispatcherTimer timer;
        private PowerwallApi powerwallApi;
        private ITariffProvider tariffHelper;

        public ChartPage()
        {
            this.InitializeComponent();
            Telemetry.TrackEvent("ChartPage opened");
            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            this.ViewModel = new ChartViewModel();
            ViewModel.Period = "Day";
            ViewModel.CalendarDate = DateTime.Now;

            powerwallApi = new PowerwallApi(Settings.SiteId, new WindowsPlatformAdapter());
            ratePlanTask = CreateTariffProvider();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMinutes(5);
            timer.Tick += Timer_Tick;
        }

        private async void Timer_Tick(object sender, object e)
        {
            await RefreshDataAndCharts();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Reset the tariffProvider if it was changed in settings
            if (tariffHelper != null && (tariffHelper.ProviderName != Settings.TariffProvider || tariffHelper.DailySupplyCharge != Settings.TariffDailySupplyCharge))
            {
                await CreateTariffProvider();
                await RefreshDataAndCharts();
            }

            // Reset the API helper in case we've signed out and back in
            powerwallApi = new PowerwallApi(Settings.SiteId, new WindowsPlatformAdapter());
            timer.Start();
            base.OnNavigatedTo(e);
        }
        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            timer.Stop();
            base.OnNavigatedFrom(e);
        }


        private async void prevPeriodButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            switch(ViewModel.Period)
            {
                case "Day":
                    ViewModel.CalendarDate = ViewModel.CalendarDate.Value.AddDays(-1);
                    break;
                case "Week":
                    ViewModel.CalendarDate = ViewModel.CalendarDate.Value.AddDays(-7);
                    break;
                case "Month":
                    ViewModel.CalendarDate = ViewModel.CalendarDate.Value.AddMonths(-1);
                    break;
                case "Year":
                    ViewModel.CalendarDate =ViewModel.CalendarDate.Value.AddYears(-1);
                    break;
                default:
                    break;
            }
            await RefreshDataAndCharts();
        }

        private async void nextPeriodButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            switch (ViewModel.Period)
            {
                case "Day":
                    ViewModel.CalendarDate = ViewModel.CalendarDate.Value.AddDays(1);
                    break;
                case "Week":
                    ViewModel.CalendarDate = ViewModel.CalendarDate.Value.AddDays(7);
                    break;
                case "Month":
                    ViewModel.CalendarDate = ViewModel.CalendarDate.Value.AddMonths(1);
                    break;
                case "Year":
                    ViewModel.CalendarDate = ViewModel.CalendarDate.Value.AddYears(1);
                    break;
                default:
                    break;
            }
            await RefreshDataAndCharts();

        }

        private async void periodCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await RefreshDataAndCharts();
        }

        private void CalendarDatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            var date = powerwallApi.ConvertToPowerwallDate(DateTime.Now);
            if (args.NewDate > date.Date)
            {
                datePicker.Date = date.Date;
            }
        }
        private async void CalendarDatePicker_Closed(object sender, object e)
        {
            await RefreshDataAndCharts();
        }

        private async void todayButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ViewModel.CalendarDate = DateTime.Today;
            await RefreshDataAndCharts();
        }

    

        private async Task RefreshDataAndCharts()
        {

            progressRing.IsActive = true;
            
            if (ViewModel.Period == "Day")
            {
                dailyChart.Visibility = Visibility.Visible;
                batteryChart.Visibility = Visibility.Visible;
                energyChart.Visibility = Visibility.Collapsed;
                energyCostChart.Visibility = Visibility.Collapsed;
                powerGraphOptionsCombo.Visibility = Visibility.Visible;
                periodNetCost.Visibility = Settings.ShowEnergyRates ? Visibility.Visible : Visibility.Collapsed;
                tariffBar.Visibility = Settings.ShowEnergyRates ? Visibility.Visible : Visibility.Collapsed;

                await GetTariffsForDay(ViewModel.PeriodStart);
            }
            else
            {
                dailyChart.Visibility = Visibility.Collapsed;
                batteryChart.Visibility = Visibility.Collapsed;
                energyChart.Visibility = Visibility.Visible;
                powerGraphOptionsCombo.Visibility = Visibility.Collapsed;
                tariffBar.Visibility = Visibility.Collapsed;

                if (Settings.ShowEnergyRates && (ViewModel.Period == "Week" || ViewModel.Period == "Month"))
                {
                    Grid.SetRowSpan(energyChart, 1);
                    dailySupplyChargeSeries.IsVisibleOnLegend = Settings.TariffDailySupplyCharge > 0;
                    dailySupplyChargeSeries.IsSeriesVisible = Settings.TariffDailySupplyCharge > 0;
                    energyCostChart.Visibility = Visibility.Visible;
                    periodNetCost.Visibility = Visibility.Visible;
                }
                else
                {
                    Grid.SetRowSpan(energyChart, 2);
                    energyCostChart.Visibility = Visibility.Collapsed;
                    periodNetCost.Visibility = Visibility.Collapsed;
                }


            }
            await FetchData();
            ViewModel.ChartsLastUpdated = DateTime.Now;
            progressRing.IsActive = false;
    
        }

        private async Task FetchData()
        {
            try
            {
                ViewModel.Status = StatusViewModel.StatusEnum.Online;

                var tasks = new List<Task>();
                if (ViewModel.Period == "Day")
                {
                    tasks.Add(UpdatePowerGraph());
                    tasks.Add(FetchBatterySoeData());
                    tasks.Add(FetchDailyEnergyData());
                }
                else
                {
                    tasks.Add(UpdateEnergyGraph());
                }
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Telemetry.TrackException(ex);
                ViewModel.Status = StatusViewModel.StatusEnum.Error;
                ViewModel.LastExceptionMessage = ex.Message;
                ViewModel.LastExceptionDate = DateTime.Now;
            }
        }



        private async Task UpdatePowerGraph()
        {
            try
            {
                PowerwallApi.PowerChartType powerChartType = (PowerwallApi.PowerChartType)powerGraphOptionsCombo.SelectedIndex;
                var powerChartSeries = await powerwallApi.GetPowerChartSeriesForPeriod(ViewModel.Period, ViewModel.PeriodStart, ViewModel.PeriodEnd, powerChartType);

                ViewModel.PowerChartSeries = new PowerChartSeries();
                ViewModel.PowerChartStackingSeries = new PowerChartSeries();

                if (powerGraphOptionsCombo.SelectedIndex == 0) // All data
                {
                    ViewModel.PowerChartSeries = powerChartSeries;
                    ConfigureLegend(null);
                }
                else if (powerGraphOptionsCombo.SelectedIndex == 1) // Home
                {
                    ViewModel.PowerChartStackingSeries = powerChartSeries;
                    ConfigureLegend(homeStackingSeries);
                }
                else if (powerGraphOptionsCombo.SelectedIndex == 2) // Solar
                {
                    ViewModel.PowerChartStackingSeries = powerChartSeries;
                    ConfigureLegend(solarStackingSeries);
                }
                else if (powerGraphOptionsCombo.SelectedIndex == 3) // Grid
                {
                    ViewModel.PowerChartStackingSeries = powerChartSeries;
                    ConfigureLegend(gridStackingSeries);
                }
                else if (powerGraphOptionsCombo.SelectedIndex == 4) // Battery
                {
                    ViewModel.PowerChartStackingSeries = powerChartSeries;
                    ConfigureLegend(batteryStackingSeries);
                }


                ViewModel.NotifyPropertyChanged(nameof(ViewModel.PeriodEnd));
                ((DateTimeAxis)dailyChart.XAxes[0]).Maximum = DateTime.MaxValue;

                ViewModel.NotifyPropertyChanged(nameof(ViewModel.PowerChartSeries));
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.PowerChartStackingSeries));

                if (Settings.AccessToken != "DEMO")
                {
                    ((DateTimeAxis)dailyChart.XAxes[0]).Minimum = ViewModel.PeriodStart;
                    ((DateTimeAxis)dailyChart.XAxes[0]).Maximum = ViewModel.PeriodEnd;
                }
            }
            catch (Exception ex)
            {
                Telemetry.TrackException(ex);
            }          
            
        }

        private async Task FetchDailyEnergyData()
        {
            try
            {
                ViewModel.EnergyTotals = await powerwallApi.GetEnergyTotalsForPeriod(ViewModel.PeriodStart, ViewModel.PeriodEnd, ViewModel.Period, tariffHelper);
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.EnergyTotals));
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.CurrentPeriodNetCost));
            }
            catch (Exception ex)
            {
                Telemetry.TrackException(ex);
                ViewModel.Status = StatusViewModel.StatusEnum.Error;
                ViewModel.LastExceptionMessage = ex.Message;
                ViewModel.LastExceptionDate = DateTime.Now;
            }
        }
        private void ConfigureLegend(StackedAreaSeries seriesToHide)
        {
            homeSeries.IsVisibleOnLegend = (seriesToHide == null);
            solarSeries.IsVisibleOnLegend = (seriesToHide == null);
            batterySeries.IsVisibleOnLegend = (seriesToHide == null);
            gridSeries.IsVisibleOnLegend = (seriesToHide == null);
            homeStackingSeries.IsVisibleOnLegend = (seriesToHide != null);
            solarStackingSeries.IsVisibleOnLegend = (seriesToHide != null);
            batteryStackingSeries.IsVisibleOnLegend = (seriesToHide != null);
            gridStackingSeries.IsVisibleOnLegend = (seriesToHide != null);
            if (seriesToHide != null)
            {
                seriesToHide.IsVisibleOnLegend = false;
            }
        }

        public async Task UpdateEnergyGraph()
        {
            try
            {
                ViewModel.EnergyChartSeries = await powerwallApi.GetEnergyChartSeriesForPeriod(ViewModel.Period, ViewModel.PeriodStart, ViewModel.PeriodEnd, Settings.ShowEnergyRates ? tariffHelper : null);
                ViewModel.EnergyTotals = ViewModel.EnergyChartSeries.EnergyTotals;
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.EnergyChartSeries));
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.EnergyTotals));
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.ChartPeriodInterval));
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.CurrentPeriodNetCost));
            }
            catch (Exception ex)
            {
                Telemetry.TrackException(ex);
            }
        }
        private async Task CreateTariffProvider()
        {
            try
            {
                tariffHelper = await TariffProviderFactory.Create(powerwallApi);
            }
            catch (Exception ex)
            {
                Telemetry.TrackException(ex);
            }
        }


        private async Task FetchBatterySoeData()
        {
            try
            {
                ViewModel.BatteryDailySoeGraphData = await powerwallApi.GetBatteryHistoricalChargeLevel(ViewModel.PeriodStart, ViewModel.PeriodEnd);
                ((DateTimeAxis)batteryChart.XAxes[0]).Maximum = DateTime.MaxValue;
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.BatteryDailySoeGraphData));
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.PeriodEnd));

                if (Settings.AccessToken != "DEMO")
                {
                    ((DateTimeAxis)batteryChart.XAxes[0]).Minimum = ViewModel.PeriodStart;
                    ((DateTimeAxis)batteryChart.XAxes[0]).Maximum = ViewModel.PeriodEnd;
                }

            }

            catch (Exception ex)
            {
                Telemetry.TrackException(ex);
                ViewModel.Status = StatusViewModel.StatusEnum.Error;
                ViewModel.LastExceptionMessage = ex.Message;
                ViewModel.LastExceptionDate = DateTime.Now;
            }
        }

        private async void exportButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                Telemetry.TrackEvent("Chart data exported", new Dictionary<string, string> { { "Period", ViewModel.Period} });

                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                var hwnd = WindowNative.GetWindowHandle(App.Window);
                InitializeWithWindow.Initialize(savePicker, hwnd);
                //WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

                savePicker.SuggestedStartLocation =
                    Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                // Dropdown of file types the user can save the file as
                savePicker.FileTypeChoices.Add("Comma Separated Value (CSV) file", new List<string>() { ".csv" });
                // Default file name if the user does not type one in or select a file to replace
                savePicker.SuggestedFileName = String.Format("Powerwall-{0:yyyy-MM-dd}-{1}.csv", ViewModel.PeriodStart, ViewModel.Period);

                Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    exportButton.Content = "Saving...";
                    exportButton.IsEnabled = false;
                    var stream = await file.OpenStreamForWriteAsync();

                    if (ViewModel.Period == "Day")
                    {
                        await powerwallApi.ExportPowerDataToCsv(stream, ViewModel.PeriodStart, ViewModel.PeriodEnd);
                    }
                    else
                    {
                        await powerwallApi.ExportEnergyDataToCsv(stream, ViewModel.PeriodStart, ViewModel.PeriodEnd, ViewModel.Period, 
                            Settings.ShowEnergyRates ? tariffHelper : null);
                    }

                    await stream.FlushAsync();
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                Telemetry.TrackException(ex);
                var dialog = new ContentDialog()
                {
                    Title = "Error",
                    Content = "Error while saving data: " + ex.Message,
                    CloseButtonText = "Ok"
                };

                dialog.XamlRoot = this.Content.XamlRoot;
                await dialog.ShowAsync();
            }
            exportButton.Content = "Export Data";
            exportButton.IsEnabled = true;
        }


        private async void errorIndicator_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (ViewModel.LastExceptionMessage != null)
            {
                var dialog = new ContentDialog()
                {
                    Title = "Error",
                    Content = $"Last error occurred at {ViewModel.LastExceptionDate.ToString("g")}:\r\n{ViewModel.LastExceptionMessage}",
                    CloseButtonText = "Ok"
                };

                dialog.XamlRoot = this.Content.XamlRoot;
                await dialog.ShowAsync();
            }
        }

        private async Task GetTariffsForDay(DateTime date)
        {
            await Task.WhenAll(ratePlanTask);
            if (tariffHelper == null)
            {
                return;
            }
            try
            {
                var tariffs = await tariffHelper.GetTariffsForDay(date);
                if (tariffs == null || tariffs.Count == 0)
                {
                    return;
                }
                tariffs.Sort((x, y) => x.StartDate.CompareTo(y.StartDate));
                tariffBar.ColumnDefinitions.Clear();
                tariffBar.Children.Clear();
                int columnNumber = 0;
                TextBlock lastTextBlock = null;
                foreach ( var tariff in tariffs)
                { 
                    var rates = tariffHelper.GetRatesForTariff(tariff);
                    // Create column
                    var column = new ColumnDefinition();
                    var tariffDuration = (tariff.EndDate - tariff.StartDate).TotalHours;
                    column.Width = new GridLength(tariffDuration, GridUnitType.Star);
                    tariffBar.ColumnDefinitions.Add(column);

                    // Create rectangle
                    var rect = new Rectangle();
                    rect.Fill = new SolidColorBrush(WindowsColorFromDrawingColor(tariff.Color));
                    rect.Opacity = 0.2;
                    rect.Stroke = new SolidColorBrush(WindowsColorFromDrawingColor(tariff.Color));
                    tariffBar.Children.Add(rect);
                    Grid.SetColumn(rect, columnNumber);

                    // Set tooltips
                    string rateMessage = $"Buy at {rates.Item1.ToString("C", CultureInfo.CurrentCulture)} / Sell at {rates.Item2.ToString("C", CultureInfo.CurrentCulture)}";
                    ToolTipService.SetToolTip(rect, rateMessage);

                    // Create textblock (unless the previous tariff has the same name, for dynamic tariffs)
                    var previousTariff = tariffs.ElementAtOrDefault(columnNumber - 1);
                    if (previousTariff == null || previousTariff.DisplayName != tariff.DisplayName)
                    {
                        var textBlock = new TextBlock();
                        lastTextBlock = textBlock;
                        textBlock.Text = tariff.DisplayName;
                        textBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.LightGray);
                        textBlock.HorizontalAlignment = HorizontalAlignment.Center;
                        textBlock.VerticalAlignment = VerticalAlignment.Center;
                        textBlock.FontSize = 12;
                        tariffBar.Children.Add(textBlock);
                        Grid.SetColumn(textBlock, columnNumber);

                        ToolTipService.SetToolTip(textBlock, rateMessage);
                    }
                    else
                    {
                        // Span the previous text block over this column
                        if (lastTextBlock != null)
                        {
                            Grid.SetColumnSpan(lastTextBlock, Grid.GetColumnSpan(lastTextBlock) + 1);
                        }
                    }

                    columnNumber++;
                }

            }
            catch (Exception ex)
            {
                Telemetry.TrackException(ex);
            }

        }

        private Windows.UI.Color WindowsColorFromDrawingColor(System.Drawing.Color c)
        {
            return new Windows.UI.Color() { A = c.A, R = c.R, G = c.G, B = c.B };
        }

        private async void powerGraphOptionsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                await UpdatePowerGraph();
                ViewModel.ChartsLastUpdated = DateTime.Now;
            }
            
        }


    }


}

