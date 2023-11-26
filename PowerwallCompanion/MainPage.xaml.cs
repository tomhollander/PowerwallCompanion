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
using Windows.UI.ViewManagement;
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
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(800, 600));
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
                frame.Navigate(typeof(StatusPage));
            }
        }



        public void ToggleMenuPane()
        {
            splitView.IsPaneOpen = !splitView.IsPaneOpen;
        }

        private void homeMenuButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            frame.Navigate(typeof(StatusPage));
            homeMenuButton.IsChecked = true;
            chartMenuButton.IsChecked = false;
            settingsMenuButton.IsChecked = false;
            splitView.IsPaneOpen = false;
        }

        private void chartMenuButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            frame.Navigate(typeof(ChartPage));
            homeMenuButton.IsChecked = false;
            chartMenuButton.IsChecked = true;
            settingsMenuButton.IsChecked = false;
            splitView.IsPaneOpen = false;
        }


        private void settingsMenuButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            frame.Navigate(typeof(SettingsPage));
            homeMenuButton.IsChecked = false;
            chartMenuButton.IsChecked = false;
            settingsMenuButton.IsChecked = true;
            splitView.IsPaneOpen = false;
        }


    }
}
