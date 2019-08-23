using Newtonsoft.Json;
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
using WinRTXamlToolkit.Controls.DataVisualization.Charting;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace PowerwallCompanion
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BatteryHistoryPage : Page
    {
        public BatteryHistoryViewModel ViewModel { get; set; }
        public BatteryHistoryPage()
        {
            this.InitializeComponent();

            this.ViewModel = new BatteryHistoryViewModel();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                await Task.Delay(10);

                if ((DateTime.Now - ViewModel.DataLastUpdated).TotalMinutes > 10)
                {
                    await ViewModel.RefreshData();
                }

                axis.Minimum = 0D;
                axis.Maximum = 100D;

                areaChart.Visibility = Visibility.Visible;
                progressRing.IsActive = false;
            }
            catch(UnauthorizedAccessException)
            {
                progressRing.IsActive = false;
                this.Frame.Navigate(typeof(LoginPage));
            }
            catch (Exception ex)
            {
                progressRing.IsActive = false;
            }
   
            base.OnNavigatedTo(e);
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

