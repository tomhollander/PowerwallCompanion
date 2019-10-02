using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using PowerwallCompanion.ViewModels;
using Windows.UI.Popups;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PowerwallCompanion
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {

        public HomeViewModel ViewModel
        {
            get; set;
        }

        public HomePage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            ViewModel = new HomeViewModel();
            App.HomeViewModel = ViewModel;

            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(30);
            timer.Tick += Timer_Tick;
            timer.Start();

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            RefreshData(); 
            base.OnNavigatedTo(e);
        }

        private void Timer_Tick(object sender, object e)
        {
            RefreshData();
        }

        private async Task RefreshData()
        {
            if (Settings.UseLocalGateway)
            {
                RefreshDataFromLocalGateway();
            }
            else
            {
                RefreshDataFromTeslaOwnerApi();
            }
        }

        private async Task RefreshDataFromTeslaOwnerApi()
        { 
            try
            {
                var powerInfo = await ApiHelper.CallGetApiWithTokenRefresh($"{ApiHelper.BaseUrl}/api/1/energy_sites/{Settings.SiteId}/live_status", "LiveStatus");

#if FAKE
                ViewModel.BatteryPercent = 72;
                ViewModel.HomeValue = 900D;
                ViewModel.SolarValue = 1900D;
                ViewModel.BatteryValue = -1000D;
                ViewModel.GridValue = 0D;
                ViewModel.GridActive = true;
#else
                ViewModel.BatteryPercent = (powerInfo["response"]["energy_left"].Value<double>() / powerInfo["response"]["total_pack_energy"].Value<double>()) * 100D;
                ViewModel.HomeValue = powerInfo["response"]["load_power"].Value<double>();
                ViewModel.SolarValue = powerInfo["response"]["solar_power"].Value<double>(); 
                ViewModel.BatteryValue = powerInfo["response"]["battery_power"].Value<double>();
                ViewModel.GridValue = powerInfo["response"]["grid_power"].Value<double>();
                ViewModel.GridActive = powerInfo["response"]["grid_status"].Value<string>() != "Inactive";
#endif
                ViewModel.NotifyProperties();
                ViewModel.StatusOK = true;
            }
            catch (UnauthorizedAccessException ex)
            {
                this.Frame?.Navigate(typeof(LoginPage));
                ViewModel.LastExceptionMessage = ex.Message;
                ViewModel.LastExceptionDate = DateTime.Now;
                ViewModel.NotifyProperties();
                ViewModel.StatusOK = false;
            }
            catch (Exception ex)
            {
                ViewModel.LastExceptionMessage = ex.Message;
                ViewModel.LastExceptionDate = DateTime.Now;
                ViewModel.NotifyProperties();
                ViewModel.StatusOK = false;
            }
        }

        private async Task RefreshDataFromLocalGateway()
        {
            try
            {
                var soe = await ApiHelper.CallApiIgnoreCerts($"https://{Settings.LocalGatewayIP}/api/system_status/soe");
                var aggregateJson = await ApiHelper.CallApiIgnoreCerts($"https://{Settings.LocalGatewayIP}/api/meters/aggregates");
                var gridStatus = await ApiHelper.CallApiIgnoreCerts($"https://{Settings.LocalGatewayIP}/api/system_status/grid_status");

                ViewModel.BatteryPercent = soe["percentage"].Value<double>();
                ViewModel.HomeValue = aggregateJson["load"]["instant_power"].Value<double>();
                ViewModel.SolarValue = aggregateJson["solar"]["instant_power"].Value<double>();
                ViewModel.BatteryValue = aggregateJson["battery"]["instant_power"].Value<double>();
                ViewModel.GridValue = aggregateJson["site"]["instant_power"].Value<double>();
                ViewModel.GridActive = gridStatus["grid_status"].Value<string>() == "SystemGridConnected";

                ViewModel.NotifyProperties();
                ViewModel.StatusOK = true;
            }
            catch (Exception ex)
            {
                ViewModel.LastExceptionMessage = ex.Message;
                ViewModel.LastExceptionDate = DateTime.Now;
                ViewModel.NotifyProperties();
                ViewModel.StatusOK = false;
            }
        }

        private JObject LoadJson(string url)
        {
            var client = new HttpClient();
            var responseTask = client.GetStringAsync(url);
            responseTask.Wait();
            return JObject.Parse(responseTask.Result);
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
    }
}
;