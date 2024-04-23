using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using PowerwallCompanion.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
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
    public sealed partial class SettingsPage : Page
    {
        public SettingsViewModel ViewModel
        {
            get; set;
        }

        public SettingsPage()
        {
            this.InitializeComponent();
            Analytics.TrackEvent("SettingsPage opened");
            this.ViewModel = new SettingsViewModel();
            LoadEnergySourceZones();
        }

        private async Task LoadEnergySourceZones()
        {
            try
            {
                var energySourceZones = new List<KeyValuePair<string, string>>();
                var client = new HttpClient();
                var url = $"https://api.electricitymap.org/v3/zones";
                var response = await client.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();
                var json = JsonNode.Parse(responseContent);
                energySourceZones.Add(new KeyValuePair<string, string>(null, String.Empty));
                foreach (var zone in json.AsObject())
                {
                    string zoneName = zone.Value["zoneName"].GetValue<string>();
                    string country = zone.Value["countryName"]?.GetValue<string>();
                    string name;
                    if (country == null)
                    {
                        name = zoneName;
                    }
                    else
                    {
                        name = $"{country} / {zoneName}";
                    }
                    
                    energySourceZones.Add(new KeyValuePair<string, string>(zone.Key, name));
                }
                ViewModel.EnergySourceZones = energySourceZones.OrderBy(kvp => kvp.Value).ToList();
                ViewModel.LastSavedEnergySourceZone = Settings.EnergySourcesZoneOverride;
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
                ViewModel.EnergySourceZones = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>(null, String.Empty) } ;
            }
        }
  

        private void signInButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(LoginPage));
        }

        private void signOutButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Settings.AccessToken = null;
            Settings.RefreshToken = null;
            Settings.SignInName = null;
            Settings.InstallationTimeZone = null;
            this.Frame.Navigate(typeof(LoginPage));
        }

        private void hamburgerMenu_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var frame = (Frame)Window.Current.Content;
            var mainPage = (MainPage)frame.Content;
            mainPage.ToggleMenuPane();
        }

        private void signedInLabel_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // Copy the Access Token to the clipboard
            var dp = new DataPackage();
            dp.SetText(Settings.AccessToken);
            Clipboard.SetContent(dp);
        }
    }
}
