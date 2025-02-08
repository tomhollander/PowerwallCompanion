using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

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
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Color.FromArgb(0, 0x17, 0x17, 0x17);
            titleBar.ButtonForegroundColor = Colors.DarkGray;
            titleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;

            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(800, 600));
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

        public void ToggleMenuPane()
        {
            splitView.IsPaneOpen = !splitView.IsPaneOpen;
        }

        private void homeMenuButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            frame.Navigate(typeof(StatusPage));
        }

        private void chartMenuButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            frame.Navigate(typeof(ChartPage));
        }


        private void settingsMenuButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            frame.Navigate(typeof(SettingsPage));
        }

        private void batteryStatusMenuButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            frame.Navigate(typeof(BatteryInfoPage));
        }

        private void frame_Navigated(object sender, NavigationEventArgs e)
        {
            homeMenuButton.IsChecked = (e.SourcePageType == typeof(StatusPage));
            chartMenuButton.IsChecked = (e.SourcePageType == typeof(ChartPage));
            settingsMenuButton.IsChecked = (e.SourcePageType == typeof(SettingsPage));
            batteryStauusMenuButton.IsChecked = (e.SourcePageType == typeof(BatteryInfoPage));
            splitView.IsPaneOpen = false;
        }
    }
}
