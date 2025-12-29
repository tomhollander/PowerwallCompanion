using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Services.Store;
using Windows.UI;
using Windows.UI.ViewManagement;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PowerwallCompanion
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private StoreContext context = null;

        public MainPage()
        {
            InitializeComponent();
            Telemetry.TrackUser();

            this.DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    await DownloadAndInstallAllUpdatesAsync();
                }
                catch (Exception ex)
                {
                    Telemetry.TrackException(ex);
                }
            });

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

        public async Task DownloadAndInstallAllUpdatesAsync()
        {
            if (context == null)
            {
                context = StoreContext.GetDefault();
                var hwnd = App.WindowHandle;
                if (hwnd != IntPtr.Zero)
                {
                    WinRT.Interop.InitializeWithWindow.Initialize(context, hwnd);
                }
            }

            // Get the updates that are available.
            IReadOnlyList<StorePackageUpdate> updates =
                await context.GetAppAndOptionalStorePackageUpdatesAsync();

            if (updates.Count > 0)
            {
                bool mandatoryUpdate = updates.Any(u => u.Mandatory);
                if (mandatoryUpdate)
                {
                    
                    var contentDialog = new ContentDialog()
                    {
                        Title = "Update Available",
                        Content = "This version of Powerwall Companion is obsolete. Please upgrade to continue using the app.",
                        CloseButtonText = "OK"
                    };
                    contentDialog.XamlRoot = this.Content.XamlRoot;
                    await contentDialog.ShowAsync();
                }
                // Download and install the updates (user will be prompted)
                IAsyncOperationWithProgress<StorePackageUpdateResult, StorePackageUpdateStatus> downloadOperation =
                    context.RequestDownloadAndInstallStorePackageUpdatesAsync(updates);

                StorePackageUpdateResult result = await downloadOperation.AsTask();

                if (mandatoryUpdate)
                {
                    App.Current.Exit();
                }
            }
            
        }
    }
}
