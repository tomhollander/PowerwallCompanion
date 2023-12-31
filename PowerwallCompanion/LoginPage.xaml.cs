using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using TeslaAuth;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace PowerwallCompanion
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginPage : Page
    {
        private TeslaAuthHelper teslaAuth = new TeslaAuthHelper(TeslaAccountRegion.Unknown, Licenses.TeslaAppClientId, Licenses.TeslaAppClientSecret, Licenses.TeslaAppRedirectUrl,
                    Scopes.BuildScopeString(new[] { Scopes.EnergyDeviceData, Scopes.VechicleDeviceData }));

        public LoginPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Disabled;
            authFailureMessage.Visibility = Visibility.Collapsed;
            errorBanner.Visibility = Visibility.Collapsed;
            warningBanner.Visibility = Visibility.Visible;
            webView.Source = new Uri(teslaAuth.GetLoginUrlForBrowser());

        }

        private void webView_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            webView.EnsureCoreWebView2Async().AsTask().GetAwaiter().GetResult();
            webView.CoreWebView2.CookieManager.DeleteAllCookies();
            webView.Visibility = Visibility.Visible;
            //webView.Source = new Uri(teslaAuth.GetLoginUrlForBrowser());
        }



        private async void webView_NavigationStarting(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
        {
            var url = args.Uri.ToString();

            if (url.Contains(Licenses.TeslaAppRedirectUrl))
            {
                warningBanner.Visibility = Visibility.Collapsed;
                webView.Visibility = Visibility.Collapsed;

                if (await CompleteLogin(url))
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        UpdateMenuButtons();
                        this.Frame.Navigate(typeof(StatusPage), true);
                    });
                }
                else
                {
                    // Token could not be used
                    errorBanner.Visibility = Visibility.Visible;
                    warningBanner.Visibility = Visibility.Collapsed;
                    webView.Visibility = Visibility.Visible;
                    webView.CoreWebView2.CookieManager.DeleteAllCookies();
                    webView.Source = new Uri(teslaAuth.GetLoginUrlForBrowser());
                }


            }

        }

        private async Task<bool> CompleteLogin(string url)
        {
            try
            {
                var tokens = await teslaAuth.GetTokenAfterLoginAsync(url);

                if (CheckTokenScopes(tokens.AccessToken))
                {
                    Settings.AccessToken = tokens.AccessToken;
                    Settings.RefreshToken = tokens.RefreshToken;
                    Settings.SignInName = "Tesla User";
                    Settings.UseLocalGateway = false;
                    await GetSiteId();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
 
        }

        private bool CheckTokenScopes(string accessToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(accessToken);
            var token = jsonToken as JwtSecurityToken;
            var scopes = token.Claims.Where(x => x.Type == "scp").Select(x => x.Value).ToList();
            return (scopes.Contains("energy_device_data") && scopes.Contains("vehicle_device_data"));
        }

        private async Task GetSiteId()
        {
            var productsResponse = await ApiHelper.CallGetApiWithTokenRefresh("/api/1/products", "Products");
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


        
        private void UpdateMenuButtons()
        {
            var frame = (Frame)Window.Current.Content;
            var mainPage = (MainPage)frame.Content;
        }


        private async void TextBlock_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // Sign in as demo user
            Settings.AccessToken = "DEMO";
            Settings.RefreshToken = "DEMO";
            Settings.SignInName = "Demo User";
            Settings.UseLocalGateway = false;
            await GetSiteId();
            this.Frame.Navigate(typeof(StatusPage), true);
        }



        private void hideAuthInfoButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            authInfo.Visibility = Visibility.Collapsed;
        }

        private void showAuthInfoLink_Clicked(Windows.UI.Xaml.Documents.Hyperlink sender, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            authInfo.Visibility = Visibility.Visible;
        }
    }
}
