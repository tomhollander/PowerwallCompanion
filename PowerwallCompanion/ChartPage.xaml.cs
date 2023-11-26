using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PowerwallCompanion.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using TimeZoneConverter;

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
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            RefreshDataAndCharts();
        }


        private void prevPeriodButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            switch(ViewModel.Period)
            {
                case "Day":
                    ViewModel.CalendarDate = ViewModel.CalendarDate.Value.AddDays(-1);
                    break;
                case "Month":
                    ViewModel.CalendarDate = ViewModel.CalendarDate.Value.AddMonths(-1);
                    break;
                case "Year":
                    ViewModel.CalendarDate =ViewModel.CalendarDate.Value.AddYears(-1);
                    break;
            }
            RefreshDataAndCharts();
        }

        private void nextPeriodButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            switch (ViewModel.Period)
            {
                case "Day":
                    ViewModel.CalendarDate = ViewModel.CalendarDate.Value.AddDays(1);
                    break;
                case "Month":
                    ViewModel.CalendarDate = ViewModel.CalendarDate.Value.AddMonths(1);
                    break;
                case "Year":
                    ViewModel.CalendarDate = ViewModel.CalendarDate.Value.AddYears(1);
                    break;
            }
            RefreshDataAndCharts();

        }

        private void periodCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshDataAndCharts();
        }

        private void CalendarDatePicker_Closed(object sender, object e)
        {
            RefreshDataAndCharts();
        }

        private void todayButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ViewModel.CalendarDate = DateTime.Today;
            RefreshDataAndCharts();
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

            sb.Append($"{ApiHelper.BaseUrl}/api/1/energy_sites/{siteId}/calendar_history?");
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

                ViewModel.HomeEnergyGraphData.Clear();
                ViewModel.SolarEnergyGraphData.Clear();
                ViewModel.GridExportedEnergyGraphData.Clear();
                ViewModel.GridImportedEnergyGraphData.Clear();
                ViewModel.BatteryExportedEnergyGraphData.Clear();
                ViewModel.BatteryImportedEnergyGraphData.Clear();

                foreach (var data in json["response"]["time_series"])
                {
                    var date = data["timestamp"].Value<DateTime>();
                    var homeEnergy = GetJsonDoubleValue(data["consumer_energy_imported_from_grid"]) +
                                       GetJsonDoubleValue(data["consumer_energy_imported_from_solar"]) +
                                       GetJsonDoubleValue(data["consumer_energy_imported_from_battery"]) +
                                       GetJsonDoubleValue(data["consumer_energy_imported_from_generator"]);
                    totalHomeEnergy += homeEnergy;
                    ViewModel.HomeEnergyGraphData.Add(new ChartDataPoint(date, homeEnergy / 1000));
 
                    var solarEnergy = GetJsonDoubleValue(data["solar_energy_exported"]);
                    totalSolarEnergy += solarEnergy;
                    ViewModel.SolarEnergyGraphData.Add(new ChartDataPoint(date, solarEnergy / 1000));

                    var gridExportedEnergy = GetJsonDoubleValue(data["grid_energy_exported_from_solar"]) +
                                             GetJsonDoubleValue(data["grid_energy_exported_from_generator"]) +
                                             GetJsonDoubleValue(data["grid_energy_exported_from_battery"]);
                    totalGridExportedEnergy += gridExportedEnergy;
                    ViewModel.GridExportedEnergyGraphData.Add(new ChartDataPoint(date, -gridExportedEnergy / 1000));

                    var gridImportedEnergy = GetJsonDoubleValue(data["battery_energy_imported_from_grid"]) +
                                             GetJsonDoubleValue(data["consumer_energy_imported_from_grid"]);
                    totalGridImportedEnergy += gridImportedEnergy;
                    ViewModel.GridImportedEnergyGraphData.Add(new ChartDataPoint(date, gridImportedEnergy / 1000));

                    var batteryExportedEnergy = GetJsonDoubleValue(data["battery_energy_exported"]);
                    totalBatteryExportedEnergy += batteryExportedEnergy;
                    ViewModel.BatteryExportedEnergyGraphData.Add(new ChartDataPoint(date, batteryExportedEnergy / 1000));

                    var batteryImportedEnergy = GetJsonDoubleValue(data["battery_energy_imported_from_grid"]) +
                                             GetJsonDoubleValue(data["battery_energy_imported_from_solar"]) +
                                             GetJsonDoubleValue(data["battery_energy_imported_from_generator"]);
                    totalBatteryImportedEnergy += batteryImportedEnergy;
                    ViewModel.BatteryImportedEnergyGraphData.Add(new ChartDataPoint(date, -batteryImportedEnergy / 1000));

                }
                ViewModel.HomeEnergy = totalHomeEnergy;
                ViewModel.SolarEnergy = totalSolarEnergy;
                ViewModel.GridExportedEnergy = totalGridExportedEnergy;
                ViewModel.GridImportedEnergy = totalGridImportedEnergy;
                ViewModel.BatteryExportedEnergy = totalBatteryExportedEnergy;
                ViewModel.BatteryImportedEnergy = totalBatteryImportedEnergy;
            }
            catch
            {

            }
        }

        private async Task FetchPowerData()
        {
            try
            {
                var url = GetCalendarHistoryUrl("power");
                var json = await ApiHelper.CallGetApiWithTokenRefresh(url, "PowerHistory");

                ViewModel.SolarDailyGraphData.Clear();
                ViewModel.GridDailyGraphData.Clear();
                ViewModel.BatteryDailyGraphData.Clear();

                foreach (var data in json["response"]["time_series"])
                {
                    var date = data["timestamp"].Value<DateTime>();

                    //Not sure how to get load data? 
                    ViewModel.SolarDailyGraphData.Add(new ChartDataPoint(date, GetJsonDoubleValue(data["solar_power"]) / 1000));
                    ViewModel.GridDailyGraphData.Add(new ChartDataPoint(date, GetJsonDoubleValue(data["grid_power"]) / 1000));
                    ViewModel.BatteryDailyGraphData.Add(new ChartDataPoint(date, GetJsonDoubleValue(data["battery_power"]) / 1000));
                }
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.PeriodEnd));
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

                ViewModel.BatteryDailySoeGraphData.Clear();

                foreach (var data in json["response"]["time_series"])
                {
                    var date = data["timestamp"].Value<DateTime>();
                    ViewModel.BatteryDailySoeGraphData.Add(new ChartDataPoint(date, GetJsonDoubleValue(data["soe"])));

                }
                ViewModel.NotifyPropertyChanged(nameof(ViewModel.PeriodEnd));
            }

            catch
            {

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
        //    try
        //    {
        //        var savePicker = new Windows.Storage.Pickers.FileSavePicker();
        //        savePicker.SuggestedStartLocation =
        //            Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
        //        // Dropdown of file types the user can save the file as
        //        savePicker.FileTypeChoices.Add("Comma Separated Value (CSV) file", new List<string>() { ".csv" });
        //        // Default file name if the user does not type one in or select a file to replace
        //        savePicker.SuggestedFileName = String.Format("Powerwall-{0:yyyy-MM-dd}-{1}.csv", DateTime.Now, periodCombo.SelectedValue);

        //        Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();
        //        if (file != null)
        //        {
        //            exportButton.Content = "Saving...";
        //            exportButton.IsEnabled = false;

        //            if (periodCombo.SelectedIndex <= 1)
        //            {
        //                await SavePowerInfo(file);
        //            }
        //            else
        //            {
        //                await SaveEnergyInfo(file);
        //            }


        //            exportButton.Content = "Export Data";
        //            exportButton.IsEnabled = true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        var md = new MessageDialog("Error while saving data: " + ex.Message);
        //        await md.ShowAsync();
        //    }
        }






        //private async Task SavePowerInfo(StorageFile file)
        //{
        //    //Windows.Storage.CachedFileManager.DeferUpdates(file);
        //    await Windows.Storage.FileIO.WriteTextAsync(file, "timestamp,solar_power,battery_power,grid_power,load_power\r\n");
        //    foreach (var record in ViewModel.SelectedSeries)
        //    {
        //        if (!record.IsDummy)
        //        {
        //            await Windows.Storage.FileIO.AppendTextAsync(file, $"{record.Timestamp:yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz},{record.SolarPower},{record.BatteryPower},{record.GridPower},{record.LoadPower}\r\n");
        //        }
        //    }
        //    //await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
        //}


        //private async Task SaveEnergyInfo(StorageFile file)
        //{
        //    //Windows.Storage.CachedFileManager.DeferUpdates(file);
        //    await Windows.Storage.FileIO.WriteTextAsync(file, "timestamp,battery_energy_output,battery_energy_imported_from_grid,battery_energy_imported_from_solar,consumer_energy_imported_from_battery,consumer_energy_imported_from_grid,consumer_energy_imported_from_solar,battery_energy_exported_to_grid,solar_energy_exported_to_grid,grid_energy_imported,solar_energy_generated\r\n");
        //    foreach (var record in ViewModel.EnergyHistory)
        //    {

        //        await Windows.Storage.FileIO.AppendTextAsync(file, $"{record.Timestamp:yyyy-MM-dd},{record.BatteryEnergyExported},{record.BatteryEnergyImportedFromGrid},{record.BatteryEnergyImportedFromSolar},{record.ConsumerEnergyImportedFromBattery},{record.ConsumerEnergyImportedFromGrid},{record.ConsumerEnergyImportedFromSolar},{record.GridEnergyExportedFromBattery},{record.GridEnergyExportedFromSolar},{record.GridEnergyImported},{record.SolaryEnergyExported}\r\n");

        //    }
        //    //await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
        //}





    }


}

