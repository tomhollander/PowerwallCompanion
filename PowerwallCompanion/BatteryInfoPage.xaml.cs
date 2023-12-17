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
using Windows.Storage.AccessCache;
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
    public sealed partial class BatteryInfoPage : Page
    {
        public BatteryInfoPage()
        {
            this.ViewModel = new BatteryInfoViewModel();
            ViewModel.NumberOfBatteries = 1;

            GetData();

            this.InitializeComponent();
        }

        public BatteryInfoViewModel ViewModel { get; set; }

        private async Task GetData()
        {
            try
            {
                var tasks = new List<Task> { GetBatteryCapacity(), GetBatteryInfo() };
                await Task.WhenAll(tasks);
                ViewModel.NotifyAllProperties();
            }
            catch
            {

            }
        }

        private async Task GetBatteryCapacity()
        {
            var siteStatusJson = await ApiHelper.CallGetApiWithTokenRefresh($"/api/1/energy_sites/{Settings.SiteId}/site_status", "SiteStatus");
            ViewModel.SiteName = siteStatusJson["response"]["site_name"].Value<string>();
            ViewModel.TotalPackEnergy = siteStatusJson["response"]["total_pack_energy"].Value<double>();
        }

        private async Task GetBatteryInfo()
        {
            var siteInfoJson = await ApiHelper.CallGetApiWithTokenRefresh($"/api/1/energy_sites/{Settings.SiteId}/site_info", "SiteInfo");
            ViewModel.NumberOfBatteries = siteInfoJson["response"]["battery_count"].Value<int>();
            ViewModel.InstallDate = siteInfoJson["response"]["installation_date"].Value<DateTime>();
            if (ViewModel.NumberOfBatteries > 1)
            {
                multiplePowerwallMessage.Visibility = Visibility.Visible;
            }    
        }


    }
}
