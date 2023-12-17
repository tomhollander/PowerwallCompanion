using Newtonsoft.Json.Linq;
using PowerwallCompanion.ViewModels;
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
        public ChartPage()
        {
            this.InitializeComponent();

            this.ViewModel = new ChartViewModel();
            ViewModel.Period = "Day";
            ViewModel.CalendarDate = DateTime.Now;

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
            }
            await RefreshDataAndCharts();

        }

        private async void periodCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await RefreshDataAndCharts();
        }

        private void CalendarDatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (args.NewDate > DateTime.Now.Date)
            {
                datePicker.Date = DateTime.Now.Date;
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
            }
            else
            {
                dailyChart.Visibility = Visibility.Collapsed;
                batteryChart.Visibility = Visibility.Collapsed;
                energyChart.Visibility = Visibility.Visible;
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
            var startDate = new DateTimeOffset(ViewModel.PeriodStart);
            var endDate = new DateTimeOffset(ViewModel.PeriodEnd).AddSeconds(-1);
            var timeZone = TZConvert.WindowsToIana(TimeZoneInfo.Local.Id);

            sb.Append($"/api/1/energy_sites/{siteId}/calendar_history?");
            sb.Append("kind=" + kind);
            sb.Append("&period=" + ViewModel.Period.ToLowerInvariant());
            sb.Append("&start_date=" + Uri.EscapeDataString(startDate.ToString("o")));
            sb.Append("&end_date=" + Uri.EscapeDataString(endDate.ToString("o")));
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
                    ViewModel.EnergyDataForExport.Add(date, data.ToObject<Dictionary<string, object>>());
                    ViewModel.EnergyDataForExport[date].Remove("timestamp");

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

                var solarDailyGraphData = new List<ChartDataPoint>();
                var gridDailyGraphData = new List<ChartDataPoint>();
                var batteryDailyGraphData = new List<ChartDataPoint>();
                var homeDailyGraphData = new List<ChartDataPoint>();
                ViewModel.PowerDataForExport = new Dictionary<DateTime, Dictionary<string, object>>();

                foreach (var data in json["response"]["time_series"])
                {
                    var date = data["timestamp"].Value<DateTime>();

                    var solarPower = GetJsonDoubleValue(data["solar_power"]);
                    var gridPower = GetJsonDoubleValue(data["grid_power"]);
                    var batteryPower = GetJsonDoubleValue(data["battery_power"]);
                    var homePower = solarPower + gridPower + batteryPower;

                    solarDailyGraphData.Add(new ChartDataPoint(date, solarPower  / 1000));
                    gridDailyGraphData.Add(new ChartDataPoint(date, gridPower / 1000));
                    batteryDailyGraphData.Add(new ChartDataPoint(date, batteryPower / 1000));
                    homeDailyGraphData.Add(new ChartDataPoint(date, homePower / 1000));

                    // Save for export
                    ViewModel.PowerDataForExport.Add(date, data.ToObject<Dictionary<string, object>>());
                    ViewModel.PowerDataForExport[date].Remove("timestamp");
                }
                ViewModel.SolarDailyGraphData = solarDailyGraphData;
                ViewModel.GridDailyGraphData = gridDailyGraphData;
                ViewModel.BatteryDailyGraphData = batteryDailyGraphData;
                ViewModel.HomeDailyGraphData = homeDailyGraphData;

                ViewModel.NotifyPropertyChanged(nameof(ViewModel.PeriodEnd));
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.SolarDailyGraphData));
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.GridDailyGraphData));
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.BatteryDailyGraphData));
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.HomeDailyGraphData));
            }

            catch (Exception ex)
            {
                ViewModel.Status = StatusViewModel.StatusEnum.Error;
                ViewModel.LastExceptionMessage = ex.Message;
                ViewModel.LastExceptionDate = DateTime.Now;
            }
        }

        private async Task FetchBatterySoeData()
        {
            try
            {
                var url = GetCalendarHistoryUrl("soe");
                var json = await ApiHelper.CallGetApiWithTokenRefresh(url, "SoeHistory");

                var batteryDailySoeGraphData = new List<ChartDataPoint>();

                foreach (var data in json["response"]["time_series"])
                {
                    var date = data["timestamp"].Value<DateTime>();
                    batteryDailySoeGraphData.Add(new ChartDataPoint(date, GetJsonDoubleValue(data["soe"])));

                }
                ViewModel.BatteryDailySoeGraphData = batteryDailySoeGraphData;
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.BatteryDailySoeGraphData));
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.PeriodEnd));
            }

            catch (Exception ex)
            {
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
            await Windows.Storage.FileIO.AppendTextAsync(file, sb.ToString());
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
            await Windows.Storage.FileIO.AppendTextAsync(file, sb.ToString());
        }


        private void errorIndicator_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (ViewModel.LastExceptionMessage != null)
            {
                var md = new MessageDialog($"Last error occurred at {ViewModel.LastExceptionDate.ToString("g")}:\r\n{ViewModel.LastExceptionMessage}");
                md.ShowAsync();
            }
        }


    }


}

