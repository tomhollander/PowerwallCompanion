using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerwallCompanion.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public bool SignedIn
        {
            get {  return Settings.AccessToken != null || Settings.LocalGatewayIP != null;  }
        }

        public string SignInName
        {
            get
            {
                if (Settings.UseLocalGateway)
                {
                    return "Local Gateway User";
                }
                return Settings.SignInName;
            }
        }

        public bool? ShowClock
        {
            get {  return Settings.ShowClock;}
            set
            {
                Settings.ShowClock = value.Value;
                App.HomeViewModel.NotifyChangedSettings();
            }
        }

        public decimal GraphScale
        {
            get { return Settings.GraphScale; }
            set
            {
                Settings.GraphScale = value;
                App.HomeViewModel.NotifyChangedSettings();
            }
        }

        public void UpdateProps()
        {
            NotifyPropertyChanged(nameof(SignedIn));
            NotifyPropertyChanged(nameof(SignInName));
            NotifyPropertyChanged(nameof(ShowClock));
            NotifyPropertyChanged(nameof(GraphScale));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
