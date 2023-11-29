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
            }
        }

        public bool? PlaySounds
        {
            get { return Settings.PlaySounds; }
            set
            {
                Settings.PlaySounds = value.Value;
            }
        }

        public decimal GraphScale
        {
            get { return Settings.GraphScale; }
            set
            {
                Settings.GraphScale = value;
            }
        }

        public List<KeyValuePair<string, string>> AvailableSites
        {
            get
            {
                var availableSites = Settings.AvailableSites;
                if (availableSites == null) // Older versions may sign in without this list
                {
                    return new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>(Settings.SiteId, "Default")
                    };
                }
                else
                {
                    return availableSites.ToList();
                }
            }
        }

        public KeyValuePair<string, string> SelectedSite
        {
            get => AvailableSites.Where(s => s.Key == Settings.SiteId).FirstOrDefault();
            set => Settings.SiteId = value.Key;
        }

        public void UpdateProps()
        {
            NotifyPropertyChanged(nameof(SignedIn));
            NotifyPropertyChanged(nameof(SignInName));
            NotifyPropertyChanged(nameof(ShowClock));
            NotifyPropertyChanged(nameof(GraphScale));
            NotifyPropertyChanged(nameof(PlaySounds));
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
