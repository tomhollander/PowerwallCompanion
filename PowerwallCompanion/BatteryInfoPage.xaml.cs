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

            GetData();

            this.InitializeComponent();
        }

        public BatteryInfoViewModel ViewModel { get; set; }

        private async Task GetData()
        {
            try
            {
                var productsResponse = await ApiHelper.CallGetApiWithTokenRefresh(ApiHelper.BaseUrl + "/api/1/products", "Products");
                var powerwalls = new List<BatteryInfo>();

                foreach (var product in productsResponse["response"])
                {
                    if (product["resource_type"]?.Value<string>() == "battery" && product["energy_site_id"].Value<string>() == Settings.SiteId)
                    {
                        var batteryInfo = new BatteryInfo();
                        batteryInfo.Name = product["id"].Value<string>();
                        batteryInfo.TotalPackEnergy = product["total_pack_energy"].Value<double>();
                        powerwalls.Add(batteryInfo);

                    }
                }
                ViewModel.BatteryInfos = powerwalls;
            }
            catch
            {

            }
           

        }

  
    }
}
