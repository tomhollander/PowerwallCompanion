using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
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
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            authFailureMessage.Visibility = Visibility.Collapsed;
            if (Settings.UseLocalGateway)
            {
                localGatwayRadioButton.IsChecked = true;
                gatewayIpTextBox.Text = Settings.LocalGatewayIP ?? String.Empty;
            }
            else
            {
                teslaAccountRadioButton.IsChecked = true;
                emailTextBox.Text = Settings.SignInName ?? String.Empty;
            }
            
        }

        private async void signInButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                authFailureMessage.Visibility = Visibility.Collapsed;

                if (emailTextBox.Text == "demo@example.com" && passwordTextBox.Password == "demo")
                {
                    Settings.AccessToken = "DEMO";
                    Settings.SignInName = emailTextBox.Text;
                    Settings.RefreshToken = null;
                    Settings.UseLocalGateway = false;
                    this.Frame.Navigate(typeof(HomePage));
                    return;
                }

                var message = new JObject();
                message["grant_type"] = "password";
                message["client_id"] = "e4a9949fcfa04068f59abb5a658f2bac0a3428e4652315490b659d5ab3f35a9e";
                message["client_secret"] = "c75f14bbadc8bee3a7594412c31416f8300256d7668ea7e6e7f06727bfb9d220";
                message["email"] = emailTextBox.Text;
                message["password"] = passwordTextBox.Password;

                var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("X-Tesla-User-Agent");
                var requestMessage = new StringContent(message.ToString(), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(ApiHelper.BaseUrl + "/oauth/token", requestMessage);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = JObject.Parse(await response.Content.ReadAsStringAsync());
                    Settings.AccessToken = responseJson["access_token"].Value<string>();
                    Settings.RefreshToken = responseJson["refresh_token"].Value<string>();
                    Settings.SignInName = emailTextBox.Text.ToLower();
                    Settings.UseLocalGateway = false;
                    UpdateMenuButtons();
                    await GetSiteId();
                    this.Frame.Navigate(typeof(HomePage));
                }
                else
                {
                    authFailureMessage.Visibility = Visibility.Visible;
                }
            }
            catch
            {
                Settings.AccessToken = null;
                Settings.RefreshToken = null;
                Settings.SignInName = null;
                authFailureMessage.Visibility = Visibility.Visible;
            }
        }

        private async Task GetSiteId()
        {
            var productsResponse = await ApiHelper.CallGetApiWithTokenRefresh(ApiHelper.BaseUrl + "/api/1/products", "Products");
            foreach (var product in productsResponse["response"])
            {
                if (product["resource_type"]?.Value<string>() == "battery" && product["energy_site_id"] != null)
                {
                    var id = product["energy_site_id"].Value<int>();
                    Settings.SiteId = id.ToString();
                    return;
                }
            }
            throw new Exception("Powerwall site not found");
        }


        private async void ConnectButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            authFailureMessage.Visibility = Visibility.Collapsed;
            try
            {
                var siteMaster = await ApiHelper.CallApiIgnoreCerts($"https://{gatewayIpTextBox.Text}/api/sitemaster");
                if (siteMaster["running"].Value<bool>() == true)
                {
                    Settings.SignInName = null;
                    Settings.UseLocalGateway = true;
                    Settings.LocalGatewayIP = gatewayIpTextBox.Text;
                    UpdateMenuButtons();
                    this.Frame.Navigate(typeof(HomePage));
                }
                else
                {
                    throw new Exception();
                }
            }
            catch
            {
                Settings.AccessToken = null;
                Settings.RefreshToken = null;
                Settings.SignInName = null;
                Settings.LocalGatewayIP = null;
                authFailureMessage.Visibility = Visibility.Visible;
            }
        }

        private void UpdateMenuButtons()
        {
            var frame = (Frame)Window.Current.Content;
            var mainPage = (MainPage)frame.Content;
            mainPage.ShowHideButtons();
        }

        private void TeslaAccountRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            teslaAccountSignInControls.Visibility = Visibility.Visible;
            localGatewaySignInControls.Visibility = Visibility.Collapsed;
        }

        private void LocalGatwayRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            teslaAccountSignInControls.Visibility = Visibility.Collapsed;
            localGatewaySignInControls.Visibility = Visibility.Visible;
        }
    }
}
