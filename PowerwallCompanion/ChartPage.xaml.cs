using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Newtonsoft.Json.Linq;
using PowerwallCompanion.ViewModels;
using Syncfusion.UI.Xaml.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeZoneConverter;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

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
        public ChartPage()
        {
            this.InitializeComponent();
            Analytics.TrackEvent("ChartPage opened");

            this.ViewModel = new ChartViewModel();
            ViewModel.Period = "Day";
            ViewModel.CalendarDate = DateTime.Now;

            ratePlanTask = FetchRatePlan();

            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMinutes(5);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            RefreshDataAndCharts();
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
                powerGraphOptionsCombo.Visibility = Visibility.Visible;
                await GetTariffsForDay(DateUtils.ConvertToPowerwallDate(ViewModel.PeriodStart).Date);
            }
            else
            {
                dailyChart.Visibility = Visibility.Collapsed;
                batteryChart.Visibility = Visibility.Collapsed;
                energyChart.Visibility = Visibility.Visible;
                powerGraphOptionsCombo.Visibility = Visibility.Collapsed;
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
                tasks.Add(FetchPowerData());
                tasks.Add(FetchBatterySoeData());
            }
            await Task.WhenAll(tasks);
        }

        private string GetCalendarHistoryUrl(string kind)
        {
            var sb = new StringBuilder();
            var siteId = Settings.SiteId;

            var timeZone = Settings.InstallationTimeZone;
            var windowsTimeZone = TZConvert.IanaToWindows(timeZone);
            var startOffset = TimeZoneInfo.FindSystemTimeZoneById(windowsTimeZone).GetUtcOffset(ViewModel.PeriodStart);
            var endOffset = TimeZoneInfo.FindSystemTimeZoneById(windowsTimeZone).GetUtcOffset(ViewModel.PeriodEnd);
            var startDate = new DateTimeOffset(ViewModel.PeriodStart, startOffset);
            var endDate = new DateTimeOffset(ViewModel.PeriodEnd, endOffset).AddSeconds(-1);

            sb.Append($"/api/1/energy_sites/{siteId}/calendar_history?");
            sb.Append("kind=" + kind);
            sb.Append("&period=" + ViewModel.Period.ToLowerInvariant());
            if (ViewModel.Period != "Lifetime")
            {
                sb.Append("&start_date=" + Uri.EscapeDataString(startDate.ToString("o")));
                sb.Append("&end_date=" + Uri.EscapeDataString(endDate.ToString("o")));
            }
            sb.Append("&time_zone=" + Uri.EscapeDataString(timeZone));
            sb.Append("&fill_telemetry=0");
            return sb.ToString();
        }

        private async Task FetchEnergyData()
        {
            try
            {
                var url = GetCalendarHistoryUrl("energy");
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
                    }

                }
                ViewModel.HomeEnergyGraphData = homeEnergyGraphData;
                ViewModel.SolarEnergyGraphData = solarEnergyGraphData;
                ViewModel.GridExportedEnergyGraphData = gridExportedEnergyGraphData;
                ViewModel.GridImportedEnergyGraphData = gridImportedEnergyGraphData;
                ViewModel.BatteryExportedEnergyGraphData = batteryExportedEnergyGraphData;
                ViewModel.BatteryImportedEnergyGraphData = batteryImportedEnergyGraphData;

                ViewModel.NotifyPropertyChanged(nameof(ViewModel.HomeEnergyGraphData));
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.SolarEnergyGraphData));
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.GridExportedEnergyGraphData));
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.GridImportedEnergyGraphData));
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.BatteryExportedEnergyGraphData));
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.BatteryImportedEnergyGraphData));

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

        private async Task FetchPowerData()
        {
            try
            {
                var url = GetCalendarHistoryUrl("power");
                var json = await ApiHelper.CallGetApiWithTokenRefresh(url, "PowerHistory");

                ViewModel.PowerDataForExport = new Dictionary<DateTime, Dictionary<string, object>>();

                foreach (var data in json["response"]["time_series"])
                {
                    var date = data["timestamp"].Value<DateTime>();
                    if (date > DateTime.Now) continue;

                    // The date may be in a different time zone to the local time, we want to use the install time
                    date = DateUtils.ConvertToPowerwallDate(date);
                    
                    var solarPower = GetJsonDoubleValue(data["solar_power"]);
                    var gridPower = GetJsonDoubleValue(data["grid_power"]);
                    var batteryPower = GetJsonDoubleValue(data["battery_power"]);
                    var homePower = solarPower + gridPower + batteryPower;

                    // Save for export and charts
                    data["load_power"] = homePower;
                    ViewModel.PowerDataForExport.Add(date, data.ToObject<Dictionary<string, object>>());
                    ViewModel.PowerDataForExport[date].Remove("timestamp");
                }

                UpdatePowerGraph();
            }

            catch (Exception ex)
            {
                Crashes.TrackError(ex);
                ViewModel.Status = StatusViewModel.StatusEnum.Error;
                ViewModel.LastExceptionMessage = ex.Message;
                ViewModel.LastExceptionDate = DateTime.Now;
            }
        }

        private void UpdatePowerGraph()
        {
            var solarDailyGraphData = new List<ChartDataPoint>();
            var gridDailyGraphData = new List<ChartDataPoint>();
            var batteryDailyGraphData = new List<ChartDataPoint>();
            var homeDailyGraphData = new List<ChartDataPoint>();

            foreach (var data in ViewModel.PowerDataForExport)
            {
                var date = data.Key;

                var solarPower = Convert.ToDouble(data.Value["solar_power"]);
                var gridPower = Convert.ToDouble(data.Value["grid_power"]);
                var batteryPower = Convert.ToDouble(data.Value["battery_power"]);
                var homePower = Convert.ToDouble(data.Value["load_power"]);

                if (solarPower == 0 && gridPower == 0 && batteryPower == 0 && homePower == 0)
                    continue; // Likely a future date, but checking dates is tricky due to potential time zone differences.

                // Calcs somewhat dervied from https://raw.githubusercontent.com/reptilex/tesla-style-solar-power-card/master/README.md
                var gridImport = gridPower >= 0 ? gridPower: 0;
                var gridExport = gridPower < 0 ? -gridPower : 0;
                var batteryDischarge = batteryPower >= 0 ? batteryPower : 0;
                var batteryCharge = batteryPower < 0 ? -batteryPower : 0;

                var gridToHome = gridImport > homePower ? homePower : gridImport;
                var gridToBattery = gridImport > homePower ? (gridImport - homePower) : 0; 
                var batteryToHome = batteryDischarge > 0 ?
                    (batteryDischarge > homePower ? homePower : batteryDischarge) :
                    0;
                var batteryToGrid = batteryDischarge < 0 ?
                    (batteryDischarge > homePower ? (batteryDischarge - homePower) : 0) :
                    0;
                var solarToGrid = gridExport  > batteryToGrid ? gridExport - batteryToGrid : 0;
                var solarToBattery = solarPower > 100 ? batteryCharge - gridToBattery : 0; 
                var solarToHome = solarPower - gridExport - solarToBattery;

                // Clear old chart data
                ViewModel.SolarDailyGraphData = null;
                ViewModel.GridDailyGraphData = null;
                ViewModel.BatteryDailyGraphData = null;
                ViewModel.HomeDailyGraphData = null;
                ViewModel.SolarStackedDailyGraphData = null;
                ViewModel.GridStackedDailyGraphData = null;
                ViewModel.BatteryStackedDailyGraphData = null;
                ViewModel.HomeStackedDailyGraphData = null;

                if (powerGraphOptionsCombo.SelectedIndex == 0) // All data
                {
                    solarDailyGraphData.Add(new ChartDataPoint(date, solarPower / 1000));
                    gridDailyGraphData.Add(new ChartDataPoint(date, gridPower / 1000));
                    batteryDailyGraphData.Add(new ChartDataPoint(date, batteryPower / 1000));
                    homeDailyGraphData.Add(new ChartDataPoint(date, homePower / 1000));

                    ViewModel.SolarDailyGraphData = solarDailyGraphData;
                    ViewModel.GridDailyGraphData = gridDailyGraphData;
                    ViewModel.BatteryDailyGraphData = batteryDailyGraphData;
                    ViewModel.HomeDailyGraphData = homeDailyGraphData;

                    ConfigureLegend(null);

                }
                else if (powerGraphOptionsCombo.SelectedIndex == 1) // Home
                {
                    solarDailyGraphData.Add(new ChartDataPoint(date, solarToHome / 1000));
                    gridDailyGraphData.Add(new ChartDataPoint(date, gridToHome / 1000));
                    batteryDailyGraphData.Add(new ChartDataPoint(date, batteryToHome / 1000));

                    ViewModel.SolarStackedDailyGraphData = solarDailyGraphData;
                    ViewModel.GridStackedDailyGraphData = gridDailyGraphData;
                    ViewModel.BatteryStackedDailyGraphData = batteryDailyGraphData;

                    ConfigureLegend(homeStackingSeries);
                }
                else if (powerGraphOptionsCombo.SelectedIndex == 2) // Solar
                {
                    gridDailyGraphData.Add(new ChartDataPoint(date, solarToGrid / 1000));
                    batteryDailyGraphData.Add(new ChartDataPoint(date, solarToBattery / 1000));
                    homeDailyGraphData.Add(new ChartDataPoint(date, solarToHome / 1000));

                    ViewModel.GridStackedDailyGraphData = gridDailyGraphData;
                    ViewModel.BatteryStackedDailyGraphData = batteryDailyGraphData;
                    ViewModel.HomeStackedDailyGraphData = homeDailyGraphData;

                    ConfigureLegend(solarStackingSeries);
                }
                else if (powerGraphOptionsCombo.SelectedIndex == 3) // Grid
                {
                    solarDailyGraphData.Add(new ChartDataPoint(date, -solarToGrid / 1000));
                    batteryDailyGraphData.Add(new ChartDataPoint(date, gridToBattery / 1000));
                    homeDailyGraphData.Add(new ChartDataPoint(date, (gridToHome) / 1000));

                    ViewModel.SolarStackedDailyGraphData = solarDailyGraphData;
                    ViewModel.BatteryStackedDailyGraphData = batteryDailyGraphData;
                    ViewModel.HomeStackedDailyGraphData = homeDailyGraphData;

                    ConfigureLegend(gridStackingSeries);
                }
                else if (powerGraphOptionsCombo.SelectedIndex == 4) // Battery
                {
                    solarDailyGraphData.Add(new ChartDataPoint(date, -solarToBattery / 1000));
                    gridDailyGraphData.Add(new ChartDataPoint(date, -gridToBattery / 1000));
                    homeDailyGraphData.Add(new ChartDataPoint(date, (batteryToHome) / 1000));

                    ViewModel.SolarStackedDailyGraphData = solarDailyGraphData;
                    ViewModel.GridStackedDailyGraphData = gridDailyGraphData;
                    ViewModel.HomeStackedDailyGraphData = homeDailyGraphData;

                    ConfigureLegend(batteryStackingSeries);
                }

            }

            ViewModel.NotifyPropertyChanged(nameof(ViewModel.PeriodEnd));
            ((DateTimeAxis)dailyChart.PrimaryAxis).Maximum = null;
            ViewModel.NotifyPropertyChanged(nameof(ViewModel.SolarDailyGraphData));
            ViewModel.NotifyPropertyChanged(nameof(ViewModel.GridDailyGraphData));
            ViewModel.NotifyPropertyChanged(nameof(ViewModel.BatteryDailyGraphData));
            ViewModel.NotifyPropertyChanged(nameof(ViewModel.HomeDailyGraphData));
            ViewModel.NotifyPropertyChanged(nameof(ViewModel.SolarStackedDailyGraphData));
            ViewModel.NotifyPropertyChanged(nameof(ViewModel.GridStackedDailyGraphData));
            ViewModel.NotifyPropertyChanged(nameof(ViewModel.BatteryStackedDailyGraphData));
            ViewModel.NotifyPropertyChanged(nameof(ViewModel.HomeStackedDailyGraphData));

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
                ViewModel.RatePlan = await ApiHelper.CallGetApiWithTokenRefresh($"/api/1/energy_sites/{Settings.SiteId}/tariff_rate", "TariffRate");
            }
            catch
            {

            }
        }

        private async Task FetchBatterySoeData()
        {
            try
            {
                var url = GetCalendarHistoryUrl("soe");
                var json = await ApiHelper.CallGetApiWithTokenRefresh(url, "SoeHistory");

                var batteryDailySoeGraphData = new List<ChartDataPoint>();

                var windowsTimeZone = TZConvert.IanaToWindows(Settings.InstallationTimeZone);
                var offset = TimeZoneInfo.FindSystemTimeZoneById(windowsTimeZone).GetUtcOffset(ViewModel.PeriodStart);

                foreach (var data in json["response"]["time_series"])
                {
                    var date = data["timestamp"].Value<DateTime>();
                    if (date <= DateTime.Now)
                    {
                        // The date may be in a different time zone to the local time, we want to use the install time
                        date = DateUtils.ConvertToPowerwallDate(date);

                        batteryDailySoeGraphData.Add(new ChartDataPoint(date, GetJsonDoubleValue(data["soe"])));
                    }
                }
                ViewModel.BatteryDailySoeGraphData = batteryDailySoeGraphData;
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
            if (ViewModel.EnergyDataForExport.Count > 0)
            {
                sb.Append("timestamp,");
                foreach (var key in ViewModel.EnergyDataForExport.First().Value.Keys)
                {
                    sb.Append($"{key},");
                }
            }
            sb.Append("\r\n");


            foreach (var kvp in ViewModel.EnergyDataForExport)
            {
                sb.Append($"{(kvp.Key):yyyy-MM-dd},");
                foreach (var v in kvp.Value.Values)
                {
                    sb.Append($"{v},");
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
            if (ViewModel.RatePlan == null)
            {
                return;
            }
            var tariffs = new List<Tariff>();

            try
            {

                var seasons = ViewModel.RatePlan["response"]["seasons"];
                foreach (var season in seasons)
                {
                    var seasonData = season.First;
                    int fromMonth = seasonData["fromMonth"].Value<int>();
                    int toMonth = seasonData["toMonth"].Value<int>();
                    int fromDay = seasonData["fromDay"].Value<int>();
                    int toDay = seasonData["toDay"].Value<int>();
                    DateTime fromDate = new DateTime(date.Year, fromMonth, fromDay);
                    DateTime toDate = new DateTime(date.Year, toMonth, toDay);

                    bool dateIsInSeason = false;
                    if (toDate > fromDate)
                    {
                        dateIsInSeason = (date >= fromDate && date <= toDate);
                    }
                    else
                    {
                        dateIsInSeason = (date >= fromDate || date <= toDate);
                    }
                    if (dateIsInSeason)
                    {
                        int currentWeekDay = ((int)date.DayOfWeek - 1); // Tesla uses 0 for Monday
                        if (currentWeekDay == -1)
                        {
                            currentWeekDay = 6;
                        }

                        foreach (var rate in seasonData["tou_periods"])
                        {
                            var rateName = ((JProperty)rate).Name;
                            foreach (var period in rate.First.Value<JArray>())
                            {
                                var fromWeekDay = period["fromDayOfWeek"].Value<int>();
                                var toWeekDay = period["toDayOfWeek"].Value<int>();
                                if (currentWeekDay >= fromWeekDay && currentWeekDay <= toWeekDay)
                                {
                                    var fromHour = period["fromHour"].Value<int>();
                                    var fromMinute = period["fromMinute"].Value<int>();
                                    var toHour = period["toHour"].Value<int>();
                                    var toMinute = period["toMinute"].Value<int>();
                                    if (fromHour < toHour)
                                    {
                                        var tariff = new Tariff
                                        {
                                            Name = rateName,
                                            StartDate = date.AddHours(fromHour).AddMinutes(fromMinute),
                                            EndDate = date.AddHours(toHour).AddMinutes(toMinute),
                                        };
                                        tariffs.Add(tariff);
                                    }
                                    else
                                    {
                                        var tariff1 = new Tariff
                                        {
                                            Name = rateName,
                                            StartDate = date,
                                            EndDate = date.AddHours(toHour).AddMinutes(toMinute),
                                        };
                                        var tariff2 = new Tariff
                                        {
                                            Name = rateName,
                                            StartDate = date.AddHours(fromHour).AddMinutes(fromMinute),
                                            EndDate = date.AddDays(1)
                                        };
                                        tariffs.Add(tariff1);
                                        tariffs.Add(tariff2);
                                    }
                                }
                            }

                        }
                        break; // Don't need to look for more seasons
                    }
                }
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
            }

            dailyChart.PrimaryAxis.MultiLevelLabels.Clear();
            foreach (var tariff in tariffs)
            {
                var multiLabel = new ChartMultiLevelLabel()
                {
                    Start = tariff.StartDate,
                    End = tariff.EndDate,
                    Text = tariff.DisplayName,
                    Foreground = tariff.Color,
                    
                };
                dailyChart.PrimaryAxis.MultiLevelLabels.Add(multiLabel);
            }
        }

        private void powerGraphOptionsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel?.PowerDataForExport != null)
            {
                UpdatePowerGraph();
            }
        }


    }


}

