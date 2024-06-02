using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Newtonsoft.Json.Linq;
using PowerwallCompanion.Lib;
using PowerwallCompanion.Lib.Models;
using PowerwallCompanion.ViewModels;
using Syncfusion.UI.Xaml.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using TimeZoneConverter;
using Windows.Storage;
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
        private TariffHelper tariffHelper;

        public ChartPage()
        {
            this.InitializeComponent();
            Analytics.TrackEvent("ChartPage opened");

            this.ViewModel = new ChartViewModel();
            ViewModel.Period = "Day";
            ViewModel.CalendarDate = DateTime.Now;

            powerwallApi = new PowerwallApi(Settings.SiteId, new TokenStore());
            ratePlanTask = FetchRatePlan();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMinutes(5);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            RefreshDataAndCharts();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
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
            if (args.NewDate > DateUtils.ConvertToPowerwallDate(DateTime.Now).Date)
            {
                datePicker.Date = DateUtils.ConvertToPowerwallDate(DateTime.Now).Date;
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

            if (ViewModel.Period == "Day")
            {
                dailyChart.Visibility = Visibility.Visible;
                batteryChart.Visibility = Visibility.Visible;
                energyChart.Visibility = Visibility.Collapsed;
                energyCostChart.Visibility = Visibility.Collapsed;
                powerGraphOptionsCombo.Visibility = Visibility.Visible;
                await GetTariffsForDay(DateUtils.ConvertToPowerwallDate(ViewModel.PeriodStart).Date);
            }
            else
            {
                dailyChart.Visibility = Visibility.Collapsed;
                batteryChart.Visibility = Visibility.Collapsed;
                energyChart.Visibility = Visibility.Visible;
                powerGraphOptionsCombo.Visibility = Visibility.Collapsed;

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
    
        }

        private async Task FetchData()
        {
            ViewModel.Status = StatusViewModel.StatusEnum.Online;
            
            var tasks = new List<Task>();
            tasks.Add(FetchEnergyData());
            if (ViewModel.Period == "Day")
            {
                tasks.Add(UpdatePowerGraph());
                tasks.Add(FetchBatterySoeData());
            }
            await Task.WhenAll(tasks);
        }

        private async Task FetchEnergyData()
        {
            try
            {
                var url = Utils.GetCalendarHistoryUrl("energy", ViewModel.Period, ViewModel.PeriodStart, ViewModel.PeriodEnd);
                var json = await ApiHelper.CallGetApiWithTokenRefresh(url, "EnergyHistory");
                
                double totalHomeEnergy = 0;
                double totalSolarEnergy = 0;
                double totalGridExportedEnergy = 0;
                double totalGridImportedEnergy = 0;
                double totalBatteryExportedEnergy = 0;
                double totalBatteryImportedEnergy = 0;
                double totalHomeFromGrid = 0;
                double totalHomeFromSolar = 0;
                double totalHomeFromBattery = 0;

                var homeEnergyGraphData = new List<ChartDataPoint>();
                var solarEnergyGraphData = new List<ChartDataPoint>();
                var gridExportedEnergyGraphData = new List<ChartDataPoint>();
                var gridImportedEnergyGraphData = new List<ChartDataPoint>();
                var batteryExportedEnergyGraphData = new List<ChartDataPoint>();
                var batteryImportedEnergyGraphData = new List<ChartDataPoint>();
                ViewModel.EnergyDataForExport = new Dictionary<DateTime, Dictionary<string, object>>();

                foreach (var data in json["response"]["time_series"])
                {
                    var date = data["timestamp"].Value<DateTime>();
                    var homeEnergy = GetJsonDoubleValue(data["consumer_energy_imported_from_grid"]) +
                                       GetJsonDoubleValue(data["consumer_energy_imported_from_solar"]) +
                                       GetJsonDoubleValue(data["consumer_energy_imported_from_battery"]) +
                                       GetJsonDoubleValue(data["consumer_energy_imported_from_generator"]);
                    totalHomeEnergy += homeEnergy;
                    homeEnergyGraphData.Add(new ChartDataPoint(date, homeEnergy / 1000));
 
                    var solarEnergy = GetJsonDoubleValue(data["solar_energy_exported"]);
                    totalSolarEnergy += solarEnergy;
                    solarEnergyGraphData.Add(new ChartDataPoint(date, solarEnergy / 1000));

                    var gridExportedEnergy = GetJsonDoubleValue(data["grid_energy_exported_from_solar"]) +
                                             GetJsonDoubleValue(data["grid_energy_exported_from_generator"]) +
                                             GetJsonDoubleValue(data["grid_energy_exported_from_battery"]);
                    totalGridExportedEnergy += gridExportedEnergy;
                    gridExportedEnergyGraphData.Add(new ChartDataPoint(date, -gridExportedEnergy / 1000));

                    var gridImportedEnergy = GetJsonDoubleValue(data["battery_energy_imported_from_grid"]) +
                                             GetJsonDoubleValue(data["consumer_energy_imported_from_grid"]);
                    totalGridImportedEnergy += gridImportedEnergy;
                    gridImportedEnergyGraphData.Add(new ChartDataPoint(date, gridImportedEnergy / 1000));

                    var batteryExportedEnergy = GetJsonDoubleValue(data["battery_energy_exported"]);
                    totalBatteryExportedEnergy += batteryExportedEnergy;
                    batteryExportedEnergyGraphData.Add(new ChartDataPoint(date, batteryExportedEnergy / 1000));

                    var batteryImportedEnergy = GetJsonDoubleValue(data["battery_energy_imported_from_grid"]) +
                                             GetJsonDoubleValue(data["battery_energy_imported_from_solar"]) +
                                             GetJsonDoubleValue(data["battery_energy_imported_from_generator"]);
                    totalBatteryImportedEnergy += batteryImportedEnergy;
                    batteryImportedEnergyGraphData.Add(new ChartDataPoint(date, -batteryImportedEnergy / 1000));

                    // Totals for self consumption calcs
                    totalHomeFromGrid += GetJsonDoubleValue(data["consumer_energy_imported_from_grid"]) + GetJsonDoubleValue(data["consumer_energy_imported_from_generator"]);
                    totalHomeFromSolar += GetJsonDoubleValue(data["consumer_energy_imported_from_solar"]);
                    totalHomeFromBattery += GetJsonDoubleValue(data["consumer_energy_imported_from_battery"]);

                    // Save for export
                    if (!ViewModel.EnergyDataForExport.ContainsKey(date)) // Apparently duplicates can occur
                    {
                        ViewModel.EnergyDataForExport.Add(date, data.ToObject<Dictionary<string, object>>());
                        ViewModel.EnergyDataForExport[date].Remove("timestamp");
                        ViewModel.EnergyDataForExport[date].Remove("raw_timestamp");
                        ViewModel.EnergyDataForExport[date].Remove("test");
                    }

                }

                if (ViewModel.Period != "Day")
                {
                    ViewModel.HomeEnergyGraphData = NormaliseEnergyData(homeEnergyGraphData, ViewModel.Period);
                    ViewModel.SolarEnergyGraphData = NormaliseEnergyData(solarEnergyGraphData, ViewModel.Period);
                    ViewModel.GridExportedEnergyGraphData = NormaliseEnergyData(gridExportedEnergyGraphData, ViewModel.Period);
                    ViewModel.GridImportedEnergyGraphData = NormaliseEnergyData(gridImportedEnergyGraphData, ViewModel.Period);
                    ViewModel.BatteryExportedEnergyGraphData = NormaliseEnergyData(batteryExportedEnergyGraphData, ViewModel.Period);
                    ViewModel.BatteryImportedEnergyGraphData = NormaliseEnergyData(batteryImportedEnergyGraphData, ViewModel.Period);

                    ViewModel.NotifyPropertyChanged(nameof(ViewModel.HomeEnergyGraphData));
                    ViewModel.NotifyPropertyChanged(nameof(ViewModel.SolarEnergyGraphData));
                    ViewModel.NotifyPropertyChanged(nameof(ViewModel.GridExportedEnergyGraphData));
                    ViewModel.NotifyPropertyChanged(nameof(ViewModel.GridImportedEnergyGraphData));
                    ViewModel.NotifyPropertyChanged(nameof(ViewModel.BatteryExportedEnergyGraphData));
                    ViewModel.NotifyPropertyChanged(nameof(ViewModel.BatteryImportedEnergyGraphData));
                }

                if (ViewModel.Period == "Week" || ViewModel.Period == "Month")
                {
                    CalculateCostData((JArray)json["response"]["time_series"]);
                }

                ViewModel.HomeEnergy = totalHomeEnergy;
                ViewModel.SolarEnergy = totalSolarEnergy;
                ViewModel.GridExportedEnergy = totalGridExportedEnergy;
                ViewModel.GridImportedEnergy = totalGridImportedEnergy;
                ViewModel.BatteryExportedEnergy = totalBatteryExportedEnergy;
                ViewModel.BatteryImportedEnergy = totalBatteryImportedEnergy;

                ViewModel.SolarUsePercent = (totalHomeFromSolar / totalHomeEnergy) * 100;
                ViewModel.BatteryUsePercent = (totalHomeFromBattery / totalHomeEnergy) * 100;
                ViewModel.GridUsePercent = (totalHomeFromGrid / totalHomeEnergy) * 100;
                ViewModel.SelfConsumption = ((totalHomeFromSolar + totalHomeFromBattery) / totalHomeEnergy) * 100;
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
                ViewModel.Status = StatusViewModel.StatusEnum.Error;
                ViewModel.LastExceptionMessage = ex.Message;
                ViewModel.LastExceptionDate = DateTime.Now;
            }
        }
        
        private void CalculateCostData(JArray energyTimeSeries)
        {
            try
            {
                if (!Settings.ShowEnergyRates)
                {
                    return;
                }

                ViewModel.EnergyCostGraphData = new List<ChartDataPoint>();
                ViewModel.EnergyFeedInGraphData = new List<ChartDataPoint>();
                ViewModel.EnergyNetCostGraphData = new List<ChartDataPoint>();

                var dailyData = new Dictionary<DateTime, List<JObject>>();
                // Split array by date
                foreach (var data in energyTimeSeries)
                {
                    var ts = data["timestamp"].Value<DateTime>();
                    if (!dailyData.ContainsKey(ts.Date))
                    {
                        dailyData[ts.Date] = new List<JObject>();
                    }
                    dailyData[ts.Date].Add(data as JObject);
                }

                // Calculate costs per date  // TODO: FIX
                //foreach (var date in dailyData.Keys)
                //{
                //    var energyCost = tariffHelper.GetEnergyCostAndFeedInFromEnergyHistory(dailyData[date]);
                //    ViewModel.EnergyCostGraphData.Add(new ChartDataPoint(date, (double)energyCost.Item1));
                //    ViewModel.EnergyFeedInGraphData.Add(new ChartDataPoint(date, (double)-energyCost.Item2));
                //    ViewModel.EnergyNetCostGraphData.Add(new ChartDataPoint(date, (double)(energyCost.Item1 - energyCost.Item2)));
                //}

                ViewModel.NotifyPropertyChanged(nameof(ViewModel.EnergyCostGraphData));
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.EnergyFeedInGraphData));
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.EnergyNetCostGraphData));
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
            }

        }

        private Func<DateTime, DateTime, bool> GetNormalisationDateComparitor(string period)
        {
            Func<DateTime, DateTime, bool> dateComparitor;
            if (period == "Year")
            {
                dateComparitor = (DateTime d, DateTime c) => d.Year == c.Year && d.Month == c.Month;
            }
            else if (period == "Lifetime")
            {
                dateComparitor = (DateTime d, DateTime c) => d.Year == c.Year;
            }
            else // Day, Week, Month
            {
                dateComparitor = (DateTime d, DateTime c) => d.Date == c.Date;
            }
            return dateComparitor;
        }

        private List<ChartDataPoint> NormaliseEnergyData(List<ChartDataPoint> energyGraphData, string period)
        {
            // The API has started returning super granular data,. Let's normalise it to a more sensible granularity 
            var result = new List<ChartDataPoint>();
            ChartDataPoint lastPoint = null;

            var dateComparitor = GetNormalisationDateComparitor(period);

            foreach (var dataPoint in energyGraphData)
            {
                if (lastPoint == null || !dateComparitor(dataPoint.XValue, lastPoint.XValue))
                {
                    // New period
                    result.Add(dataPoint);
                    lastPoint = dataPoint;
                }
                else
                {
                    lastPoint.YValue += dataPoint.YValue;
                }
            }
            return result;
        }

        private Dictionary<DateTime, Dictionary<string, object>> NormaliseExportData(Dictionary<DateTime, Dictionary<string, object>> exportData, string period)
        {
            // The API has started returning super granular data,. Let's normalise it to a more sensible granularity 
            var result = new Dictionary <DateTime, Dictionary< string, object>>();
            DateTime lastDate = DateTime.MinValue;
            Dictionary<string, object> lastValue = null;

            var dateComparitor = GetNormalisationDateComparitor(period);

            foreach (var currentDate in exportData.Keys)
            {
                if (lastValue == null || !dateComparitor(currentDate, lastDate))
                {
                    // New period
                    result.Add(currentDate, exportData[currentDate]);
                    lastDate = currentDate;
                    lastValue = exportData[currentDate];
                }
                else
                {
                    // Add the values from the current point to the last one
                    foreach (var key in exportData[currentDate].Keys)
                    {
                        if (key == "timestamp")
                        {
                            continue;
                        }
                        if (exportData[currentDate][key].GetType() != typeof(DateTime))
                        {
                            try
                            {
                                if (!lastValue.ContainsKey(key))
                                {
                                    lastValue[key] = 0;
                                }
                                lastValue[key] = Convert.ToInt64(lastValue[key]) + Convert.ToInt64(exportData[currentDate][key]);
                            }
                            catch
                            {
                                // Unlikely but they could add string values...
                            }
                        }

                    }
                }
            }
            return result;
        }

      

        private async Task UpdatePowerGraph()
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

            ((DateTimeAxis)dailyChart.PrimaryAxis).Maximum = ViewModel.PeriodEnd;
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
                ((ChartLegend)dailyChart.Legend).CheckBoxVisibility = Visibility.Visible;
                ((ChartLegend)dailyChart.Legend).ToggleSeriesVisibility = true;
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
                ((ChartLegend)dailyChart.Legend).CheckBoxVisibility = Visibility.Collapsed;
                ((ChartLegend)dailyChart.Legend).ToggleSeriesVisibility = false;

            }
        }

        private async Task FetchRatePlan()
        {
            try
            {
                var ratePlan = await powerwallApi.GetRatePlan();
                tariffHelper = new TariffHelper(ratePlan);
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
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

                ((DateTimeAxis)batteryChart.PrimaryAxis).Maximum = ViewModel.PeriodEnd;

            }

            catch (Exception ex)
            {
                Crashes.TrackError(ex);
                ViewModel.Status = StatusViewModel.StatusEnum.Error;
                ViewModel.LastExceptionMessage = ex.Message;
                ViewModel.LastExceptionDate = DateTime.Now;
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

       

        private async void exportButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                Analytics.TrackEvent("Chart data exported", new Dictionary<string, string> { { "Period", ViewModel.Period} });

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

                    if (ViewModel.Period == "Day")
                    {
                        await SavePowerInfo(file);
                    }
                    else
                    {
                        await SaveEnergyInfo(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
                var md = new MessageDialog("Error while saving data: " + ex.Message);
                await md.ShowAsync();
            }
            exportButton.Content = "Export Data";
            exportButton.IsEnabled = true;
        }



        private async Task SavePowerInfo(StorageFile file)
        {
            var sb = new StringBuilder();
            if (ViewModel.PowerDataForExport.Count > 0)
            {
                sb.Append("timestamp,");
                foreach (var key in ViewModel.PowerDataForExport.First().Value.Keys)
                {
                    sb.Append( $"{key},");
                }
            }
            sb.Append("\r\n");


            foreach (var kvp in ViewModel.PowerDataForExport)
            {
                sb.Append($"{(kvp.Key):yyyy-MM-dd HH\\:mm\\:ss},");
                foreach (var v in kvp.Value.Values)
                {
                    sb.Append($"{v},");
                }
                sb.Append("\r\n");
            }
            await Windows.Storage.FileIO.WriteTextAsync(file, sb.ToString());
        }


        private async Task SaveEnergyInfo(StorageFile file)
        {
            var sb = new StringBuilder();
            var keys = new List<string>();
            var normalisedExportData = NormaliseExportData(ViewModel.EnergyDataForExport, ViewModel.Period);
            if (normalisedExportData.Count > 0)
            {
                
                sb.Append("timestamp,");
                foreach (var key in normalisedExportData.First().Value.Keys)
                {
                    keys.Add(key);
                    sb.Append($"{key},");
                }
                if (Settings.ShowEnergyRates && (ViewModel.Period == "Week" || ViewModel.Period == "Month"))
                {
                    sb.Append("Cost,");
                    sb.Append("FeedIn,");
                    sb.Append("NetCost,");
                }
            }
            sb.Append("\r\n");


            foreach (var kvp in normalisedExportData)
            {
                sb.Append($"{(kvp.Key):yyyy-MM-dd},");
                foreach (var key in keys)
                {
                    if (kvp.Value.ContainsKey(key))
                    {
                        sb.Append($"{kvp.Value[key]},");
                    }
                    else
                    {
                        sb.Append(",");
                    }
                }
                if (Settings.ShowEnergyRates && ViewModel.EnergyCostGraphData != null && (ViewModel.Period == "Week" || ViewModel.Period == "Month"))
                {
                    sb.Append($"{ViewModel.EnergyCostGraphData.Where(x => x.XValue == kvp.Key).FirstOrDefault()?.YValue},");
                    sb.Append($"{ViewModel.EnergyFeedInGraphData.Where(x => x.XValue == kvp.Key).FirstOrDefault()?.YValue},");
                    sb.Append($"{ViewModel.EnergyNetCostGraphData.Where(x => x.XValue == kvp.Key).FirstOrDefault()?.YValue},");
                }
                sb.Append("\r\n");
            }
            await Windows.Storage.FileIO.WriteTextAsync(file, sb.ToString());
        }


        private void errorIndicator_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (ViewModel.LastExceptionMessage != null)
            {
                var md = new MessageDialog($"Last error occurred at {ViewModel.LastExceptionDate.ToString("g")}:\r\n{ViewModel.LastExceptionMessage}");
                md.ShowAsync();
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
                var tariffs = tariffHelper.GetTariffsForDay(date);

                dailyChart.PrimaryAxis.MultiLevelLabels.Clear();
                foreach (var tariff in tariffs)
                {
                    var multiLabel = new ChartMultiLevelLabel()
                    {
                        Start = tariff.StartDate,
                        End = tariff.EndDate,
                        Text = tariff.DisplayName,
                        Foreground = new SolidColorBrush(WindowsColorFromDrawingColor(tariff.Color)),
                        FontSize = 14,
                    };
                    dailyChart.PrimaryAxis.MultiLevelLabels.Add(multiLabel);
                }
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
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
            }
            
        }


    }


}

