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

            powerwallApi = new PowerwallApi(Settings.SiteId, new UwpPlatformAdapter());
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
            if (tariffHelper != null && tariffHelper.ProviderName != Settings.TariffProvider)
            {
                await CreateTariffProvider();
                await RefreshDataAndCharts();
            }
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
                dailyCost.Visibility = Settings.ShowEnergyRates ? Visibility.Visible : Visibility.Collapsed;
                DateTime date = powerwallApi.ConvertToPowerwallDate(ViewModel.PeriodStart);
                await GetTariffsForDay(date.Date);
            }
            else
            {
                dailyChart.Visibility = Visibility.Collapsed;
                batteryChart.Visibility = Visibility.Collapsed;
                energyChart.Visibility = Visibility.Visible;
                powerGraphOptionsCombo.Visibility = Visibility.Collapsed;
                dailyCost.Visibility = Visibility.Collapsed;

                if (Settings.ShowEnergyRates && (ViewModel.Period == "Week" || ViewModel.Period == "Month"))
                {
                    Grid.SetRowSpan(energyChart, 1);
                    energyCostChart.Visibility = Visibility.Visible;
                }
                else
                {
                    Grid.SetRowSpan(energyChart, 2);
                    energyCostChart.Visibility = Visibility.Collapsed;
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
                ((DateTimeAxis)dailyChart.PrimaryAxis).Maximum = null;

                ViewModel.NotifyPropertyChanged(nameof(ViewModel.PowerChartSeries));
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.PowerChartStackingSeries));

                if (Settings.AccessToken != "DEMO")
                {
                   ((DateTimeAxis)dailyChart.PrimaryAxis).Maximum = ViewModel.PeriodEnd;
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
            }
            catch (Exception ex)
            {
                Telemetry.TrackException(ex);
                ViewModel.Status = StatusViewModel.StatusEnum.Error;
                ViewModel.LastExceptionMessage = ex.Message;
                ViewModel.LastExceptionDate = DateTime.Now;
            }
        }
        private void ConfigureLegend(StackingAreaSeries seriesToHide)
        {
            if (seriesToHide == null)
            {

                // Show non-stacking series on legend
                homeSeries.VisibilityOnLegend = Visibility.Visible;
                solarSeries.VisibilityOnLegend = Visibility.Visible;
                batterySeries.VisibilityOnLegend = Visibility.Visible;
                gridSeries.VisibilityOnLegend = Visibility.Visible;
                homeStackingSeries.VisibilityOnLegend = Visibility.Collapsed;
                solarStackingSeries.VisibilityOnLegend = Visibility.Collapsed;
                batteryStackingSeries.VisibilityOnLegend = Visibility.Collapsed;
                gridStackingSeries.VisibilityOnLegend = Visibility.Collapsed;
                //((ChartLegend)dailyChart.Legend).CheckBoxVisibility = Visibility.Visible;
                //((ChartLegend)dailyChart.Legend).ToggleSeriesVisibility = true;
            }
            else
            {
                // Set legend
                homeSeries.VisibilityOnLegend = Visibility.Collapsed;
                solarSeries.VisibilityOnLegend = Visibility.Collapsed;
                batterySeries.VisibilityOnLegend = Visibility.Collapsed;
                gridSeries.VisibilityOnLegend = Visibility.Collapsed;
                homeStackingSeries.VisibilityOnLegend = Visibility.Visible;
                solarStackingSeries.VisibilityOnLegend = Visibility.Visible;
                batteryStackingSeries.VisibilityOnLegend = Visibility.Visible;
                gridStackingSeries.VisibilityOnLegend = Visibility.Visible;
                seriesToHide.VisibilityOnLegend = Visibility.Collapsed;
                //((ChartLegend)dailyChart.Legend).CheckBoxVisibility = Visibility.Collapsed;
                //((ChartLegend)dailyChart.Legend).ToggleSeriesVisibility = false;

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
                ((DateTimeAxis)batteryChart.PrimaryAxis).Maximum = null;
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.BatteryDailySoeGraphData));
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.PeriodEnd));

                if (Settings.AccessToken != "DEMO")
                {
                    ((DateTimeAxis)batteryChart.PrimaryAxis).Maximum = ViewModel.PeriodEnd;
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
                var md = new MessageDialog("Error while saving data: " + ex.Message);
                await md.ShowAsync();
            }
            exportButton.Content = "Export Data";
            exportButton.IsEnabled = true;
        }


        private async void errorIndicator_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (ViewModel.LastExceptionMessage != null)
            {
                var md = new MessageDialog($"Last error occurred at {ViewModel.LastExceptionDate.ToString("g")}:\r\n{ViewModel.LastExceptionMessage}");
                await md.ShowAsync();
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

                dailyChart.PrimaryAxis.MultiLevelLabels.Clear();
                ChartMultiLevelLabel lastMultiLabel = null;
                foreach (var tariff in tariffs.OrderBy(t => t.StartDate).AsEnumerable())
                {
                    if (lastMultiLabel != null && lastMultiLabel.Text == tariff.DisplayName)
                    {
                        lastMultiLabel.End = tariff.EndDate;
                        continue;
                    }
                    var multiLabel = new ChartMultiLevelLabel()
                    {
                        Start = tariff.StartDate,
                        End = tariff.EndDate,
                        Text = tariff.DisplayName,
                        Foreground = new SolidColorBrush(WindowsColorFromDrawingColor(tariff.Color)),
                        FontSize = 14,
                    };
                    lastMultiLabel = multiLabel; 
                    dailyChart.PrimaryAxis.MultiLevelLabels.Add(multiLabel);
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

