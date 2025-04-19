using Microsoft.UI.Xaml.Controls;
using PowerwallCompanion.Lib;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using TeslaAuth;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace PowerwallCompanion
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginPage : Page
    {
        private TeslaAuthHelper teslaAuth = new TeslaAuthHelper(TeslaAccountRegion.Unknown, Keys.TeslaAppClientId, Keys.TeslaAppClientSecret, Keys.TeslaAppRedirectUrl,
                    Scopes.BuildScopeString(new[] { Scopes.EnergyDeviceData, Scopes.VehicleDeviceData }));

        public LoginPage()
        {
            this.InitializeComponent();
            Telemetry.TrackEvent("LoginPage opened");
            this.NavigationCacheMode = NavigationCacheMode.Disabled;
            authFailureMessage.Visibility = Visibility.Collapsed;
            errorBanner.Visibility = Visibility.Collapsed;
            warningBanner.Visibility = Visibility.Visible;
            webView.Source = new Uri(teslaAuth.GetLoginUrlForBrowser());

        }

        private async void webView_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.CookieManager.DeleteAllCookies();
            webView.Visibility = Visibility.Visible;
            //webView.Source = new Uri(teslaAuth.GetLoginUrlForBrowser());
        }



        private async void webView_NavigationStarting(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
        {
            var url = args.Uri.ToString();

            if (url.StartsWith(Keys.TeslaAppRedirectUrl))
            {
                warningBanner.Visibility = Visibility.Collapsed;
                webView.Visibility = Visibility.Collapsed;

                if (await CompleteLogin(url))
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
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
                var powerwallApi = new PowerwallApi(null, new WindowsPlatformAdapter());
                var tokens = await teslaAuth.GetTokenAfterLoginAsync(url);

                if (CheckTokenScopes(tokens.AccessToken))
                {
                    Settings.AccessToken = tokens.AccessToken;
                    Settings.RefreshToken = tokens.RefreshToken;
                    Settings.SignInName = "Tesla User";
                    Settings.UseLocalGateway = false;
                    Settings.SiteId = await powerwallApi.GetFirstSiteId();
                    Settings.AvailableSites = await powerwallApi.GetEnergySites();
                    await powerwallApi.StoreInstallationTimeZone();
                    Telemetry.TrackEvent("Login succeeded");
                    return true;
                }
                else
                {
                    Telemetry.TrackEvent("Login failed", new Dictionary<string, string> { { "Cause", "Incorrect scopes" } });
                    return false;
                }
            }
            catch (Exception ex)
            {
                Telemetry.TrackException(ex);
                return false;
            }
 
        }

        private bool CheckTokenScopes(string accessToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(accessToken);
            var token = jsonToken as JwtSecurityToken;
            var scopes = token.Claims.Where(x => x.Type == "scp").Select(x => x.Value).ToList();
            return (scopes.Contains("energy_device_data"));
        }

        


        private void TextBlock_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // Sign in as demo user
            Settings.AccessToken = "DEMO";
            Settings.RefreshToken = "DEMO";
            Settings.SignInName = "Demo User";
            Settings.UseLocalGateway = false;
            Settings.SiteId = "DEMO";
            this.Frame.Navigate(typeof(StatusPage), true);
        }



        private void hideAuthInfoButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            authInfo.Visibility = Visibility.Collapsed;
        }


        private void Hyperlink_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            authInfo.Visibility = Visibility.Visible;
        }
    }
}
