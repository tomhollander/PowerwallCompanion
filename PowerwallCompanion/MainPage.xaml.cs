using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PowerwallCompanion
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            Telemetry.TrackUser();

            //// TODO Windows.UI.ViewManagement.ApplicationView is no longer supported. Use Microsoft.UI.Windowing.AppWindow instead. For more details see https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing
            //ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(800, 600));
            if (Settings.AccessToken == null || Settings.SiteId == null || GetTokenAzp(Settings.AccessToken) != Keys.TeslaAppClientId)
            {
                var installInfo = WebView2Install.GetInfo();
                if (installInfo.InstallType == InstallType.NotInstalled)
                {
                    frame.Navigate(typeof(DownloadWebViewPage));
                }
                else
                {
                    frame.Navigate(typeof(LoginPage));
                }
            }
            else
            {
                frame.Navigate(typeof(StatusPage));
            }
        }

        private string GetTokenAzp(string accessToken)
        {
            try
            {
                // Parse JWT and get azp claim
                var handler = new JwtSecurityTokenHandler();
                var jwtSecurityToken = handler.ReadJwtToken(accessToken);
                var azp = jwtSecurityToken.Claims.FirstOrDefault(claim => claim.Type == "azp").Value;
                return azp;
            }
            catch (Exception ex)
            {
                Telemetry.TrackException(ex);
                return null;
            }
        }

       

  
        private void navView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            navView.IsPaneOpen = false;
            if (args.IsSettingsInvoked)
            {
                frame.Navigate(typeof(SettingsPage));
            }
            else if (args.InvokedItemContainer.Tag.ToString() == "Status")
            {
                frame.Navigate(typeof(StatusPage));
            }
            else if (args.InvokedItemContainer.Tag.ToString() == "Charts")
            {
                frame.Navigate(typeof(ChartPage));
            }
            else if (args.InvokedItemContainer.Tag.ToString() == "BatteryInfo")
            {
                frame.Navigate(typeof(BatteryInfoPage));
            }
        }
    }
}
