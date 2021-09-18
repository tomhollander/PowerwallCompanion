using Newtonsoft.Json.Linq;
using PowerwallCompanion.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace PowerwallCompanion
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EnergyPage : Page
    {
        private readonly TimeSpan energyHistoryRefreshInterval = new TimeSpan(0, 4, 0);
        public EnergyPage()
        {
            this.InitializeComponent();
            this.ViewModel = new EnergyViewModel();

            RefreshData();

            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMinutes(5);
            timer.Tick += Timer_Tick;
            timer.Start();

        }

        private async Task RefreshData()
        {
            GetEnergyHistoryData();
        }

        private void Timer_Tick(object sender, object e)
        {
            RefreshData();
        }

        public EnergyViewModel ViewModel
        {
            get; set;
        }

        private async Task GetEnergyHistoryData()
        {
            try
            {
                if (DateTime.Now - ViewModel.EnergyHistoryLastRefreshed < energyHistoryRefreshInterval)
                {
                    return;
                }

                string period = "day";
                var json = await ApiHelper.CallGetApiWithTokenRefresh($"{ApiHelper.BaseUrl}/api/1/energy_sites/{Settings.SiteId}/history?kind=energy&period={period}", "EnergyHistory");

                var yesterday = json["response"]["time_series"][0];
                ViewModel.HomeEnergyYesterday = GetJsonDoubleValue(yesterday["consumer_energy_imported_from_grid"]) + GetJsonDoubleValue(yesterday["consumer_energy_imported_from_solar"]) + GetJsonDoubleValue(yesterday["consumer_energy_imported_from_battery"]) + GetJsonDoubleValue(yesterday["consumer_energy_imported_from_generator"]);
                ViewModel.SolarEnergyYesterday = GetJsonDoubleValue(yesterday["solar_energy_exported"]);
                ViewModel.GridEnergyImportedYesterday = GetJsonDoubleValue(yesterday["grid_energy_imported"]);
                ViewModel.GridEnergyExportedYesterday = GetJsonDoubleValue(yesterday["grid_energy_exported_from_solar"]) + GetJsonDoubleValue(yesterday["grid_energy_exported_from_battery"]);
                ViewModel.BatteryEnergyImportedYesterday = GetJsonDoubleValue(yesterday["battery_energy_imported_from_grid"]) + GetJsonDoubleValue(yesterday["battery_energy_imported_from_solar"]);
                ViewModel.BatteryEnergyExportedYesterday = GetJsonDoubleValue(yesterday["battery_energy_exported"]);

                var today = json["response"]["time_series"][1];
                ViewModel.HomeEnergyToday = GetJsonDoubleValue(today["consumer_energy_imported_from_grid"]) + GetJsonDoubleValue(today["consumer_energy_imported_from_solar"]) + GetJsonDoubleValue(today["consumer_energy_imported_from_battery"]) + GetJsonDoubleValue(today["consumer_energy_imported_from_generator"]);
                ViewModel.SolarEnergyToday = GetJsonDoubleValue(today["solar_energy_exported"]);
                ViewModel.GridEnergyImportedToday = GetJsonDoubleValue(today["grid_energy_imported"]);
                ViewModel.GridEnergyExportedToday = GetJsonDoubleValue(today["grid_energy_exported_from_solar"]) + GetJsonDoubleValue(today["grid_energy_exported_from_battery"]);
                ViewModel.BatteryEnergyImportedToday = GetJsonDoubleValue(today["battery_energy_imported_from_grid"]) + GetJsonDoubleValue(today["battery_energy_imported_from_solar"]);
                ViewModel.BatteryEnergyExportedToday = GetJsonDoubleValue(today["battery_energy_exported"]);

                ViewModel.EnergyHistoryLastRefreshed = DateTime.Now;
                ViewModel.StatusOK = true;
                ViewModel.NotifyProperties();


            }
            catch (Exception ex)
            {
                ViewModel.StatusOK = false;
                ViewModel.NotifyProperties();
            }
        }

        private void hamburgerMenu_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var frame = (Frame)Window.Current.Content;
            var mainPage = (MainPage)frame.Content;
            mainPage.ToggleMenuPane();
        }

        private void statusLight_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (ViewModel.LastExceptionMessage != null)
            {
                var md = new MessageDialog($"Last error occurred at {ViewModel.LastExceptionDate.ToString("g")}:\r\n{ViewModel.LastExceptionMessage}");
                md.ShowAsync();
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

    }
}
