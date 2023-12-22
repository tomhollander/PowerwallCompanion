using PowerwallCompanion.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
            this.ViewModel = new SettingsViewModel();
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
            Settings.LocalGatewayIP = null;
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
