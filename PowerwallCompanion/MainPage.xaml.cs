using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;

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
            SetupUI();
            ShowHideButtons();
            if (Settings.AccessToken == null && Settings.LocalGatewayIP == null)
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
                frame.Navigate(typeof(HomePage));
            }
        }


        private async Task SetupUI()
        {
            var displayRequest = new Windows.System.Display.DisplayRequest();
            displayRequest.RequestActive();

            // If we have a phone contract, hide the status bar
            if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
            {
                var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                await statusBar.HideAsync();
            }

        }

        public void ShowHideButtons()
        {
            if (Settings.UseLocalGateway)
            {
                chartMenuButton.Visibility = Visibility.Collapsed;
                batteryHistoryMenuButton.Visibility = Visibility.Collapsed;
                energyMenuButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                chartMenuButton.Visibility = Visibility.Visible;
                batteryHistoryMenuButton.Visibility = Visibility.Visible;
                energyMenuButton.Visibility = Visibility.Visible;
            }
        }

        public void ToggleMenuPane()
        {
            splitView.IsPaneOpen = !splitView.IsPaneOpen;
        }

        private void homeMenuButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            frame.Navigate(typeof(HomePage));
            homeMenuButton.IsChecked = true;
            energyMenuButton.IsChecked = false;
            chartMenuButton.IsChecked = false;
            batteryHistoryMenuButton.IsChecked = false;
            settingsMenuButton.IsChecked = false;
            splitView.IsPaneOpen = false;
        }

        private void chartMenuButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            frame.Navigate(typeof(ChartPage));
            homeMenuButton.IsChecked = false;
            energyMenuButton.IsChecked = false;
            chartMenuButton.IsChecked = true;
            batteryHistoryMenuButton.IsChecked = false;
            settingsMenuButton.IsChecked = false;
            splitView.IsPaneOpen = false;
        }

        private void batteryHistoryMenuButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            frame.Navigate(typeof(BatteryHistoryPage));
            homeMenuButton.IsChecked = false;
            energyMenuButton.IsChecked = false;
            chartMenuButton.IsChecked = false;
            batteryHistoryMenuButton.IsChecked = true;
            settingsMenuButton.IsChecked = false;
            splitView.IsPaneOpen = false;
        }

        private void settingsMenuButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            frame.Navigate(typeof(SettingsPage));
            homeMenuButton.IsChecked = false;
            energyMenuButton.IsChecked = false;
            chartMenuButton.IsChecked = false;
            batteryHistoryMenuButton.IsChecked = false;
            settingsMenuButton.IsChecked = true;
            splitView.IsPaneOpen = false;
        }

        private void energyMenuButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            frame.Navigate(typeof(EnergyPage));
            homeMenuButton.IsChecked = false;
            energyMenuButton.IsChecked = true;
            chartMenuButton.IsChecked = false;
            batteryHistoryMenuButton.IsChecked = false;
            settingsMenuButton.IsChecked = false;
            splitView.IsPaneOpen = false;
        }
    }
}
