using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using TeslaAuth;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
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
        private TeslaAuthHelper teslaAuth = new TeslaAuthHelper("PowerwallCompanion/1.0");

        public LoginPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Disabled;
            authFailureMessage.Visibility = Visibility.Collapsed;
            if (Settings.UseLocalGateway)
            {
                localGatwayRadioButton.IsChecked = true;
                gatewayIpTextBox.Text = Settings.LocalGatewayIP ?? String.Empty;
            }
            else
            {
                teslaAccountRadioButton.IsChecked = true;

            }
        }

        private void webView_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            webView.EnsureCoreWebView2Async().AsTask().GetAwaiter().GetResult();
            webView.CoreWebView2.CookieManager.DeleteAllCookies();
            webView.Visibility = Visibility.Visible;
            webView.Source = new Uri(teslaAuth.GetLoginUrlForBrowser());
        }



        private async void webView_NavigationStarting(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
        {
            var url = args.Uri.ToString();
            if (url.Contains("void/callback"))
            {
                webView.Visibility = Visibility.Collapsed;
                await CompleteLogin(url);
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    UpdateMenuButtons();
                    this.Frame.Navigate(typeof(HomePage));
                });


            }

        }


        private async Task CompleteLogin(string url)
        {
            var tokens = await teslaAuth.GetTokenAfterLoginAsync(url);
            Settings.AccessToken = tokens.AccessToken;
            Settings.RefreshToken = tokens.RefreshToken;
            Settings.SignInName = "Tesla User";
            Settings.UseLocalGateway = false;
            await GetSiteId();
        }



        private async Task GetSiteId()
        {
            var productsResponse = await ApiHelper.CallGetApiWithTokenRefresh(ApiHelper.BaseUrl + "/api/1/products", "Products");
            var availableSites = new Dictionary<string, string>();
            bool foundSite = false;
            foreach (var product in productsResponse["response"])
            {
                if (product["resource_type"]?.Value<string>() == "battery" && product["energy_site_id"] != null)
                {
                    var siteName = product["site_name"].Value<string>();
                    var id = product["energy_site_id"].Value<long>();
                    if (!foundSite)
                    {
                        Settings.SiteId = id.ToString();
                        foundSite = true;
                    }
                    availableSites.Add(id.ToString(), siteName);

                }
            }
            if (foundSite)
            {
                Settings.AvailableSites = availableSites;
            }
            else
            {
                throw new Exception("Powerwall site not found");
            }

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
            webView.Source = new Uri(teslaAuth.GetLoginUrlForBrowser());
        }

        private void LocalGatwayRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            teslaAccountSignInControls.Visibility = Visibility.Collapsed;
            localGatewaySignInControls.Visibility = Visibility.Visible;
        }

        private async void TextBlock_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // Sign in as demo user
            Settings.AccessToken = "DEMO";
            Settings.RefreshToken = "DEMO";
            Settings.SignInName = "Demo User";
            Settings.UseLocalGateway = false;
            await GetSiteId();
            this.Frame.Navigate(typeof(HomePage));
        }
    }
}
