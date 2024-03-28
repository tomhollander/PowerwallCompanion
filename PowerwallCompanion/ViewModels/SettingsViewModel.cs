using Microsoft.AppCenter.Crashes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace PowerwallCompanion.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private List<KeyValuePair<string, string>> _energySourceZones;

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

        public string LocalGatewayIP
        {
            get { return Settings.LocalGatewayIP; }
            set
            {
                Settings.LocalGatewayIP = value;
            }
        }

        public string LocalGatewayPassword
        {
            get { return Settings.LocalGatewayPassword; }
            set
            {
                Settings.LocalGatewayPassword = value;
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

        public bool? ShowEnergySources
        {
            get { return Settings.ShowEnergySources; }
            set
            {
                Settings.ShowEnergySources = value.Value;
            }
        }

        public bool EnergySourcesUseLocation
        {
            get
            {
                return Settings.EnergySourcesZoneOverride == null;
            }
            set
            {
                Settings.EnergySourcesZoneOverride = null;
                LastSavedEnergySourceZone = null;
            }
        }

        public bool EnergySourcesUseCustomZone
        {
            get
            {
                return Settings.EnergySourcesZoneOverride != null;
            }
            set
            {
                //if (EnergySourceZones?.Count > 1) // Could be a single null item if the list failed to load
                //{
                //    //LastSavedEnergySourceZone = EnergySourceZones?[1].Key;
                //    //Settings.EnergySourcesZoneOverride = EnergySourceZones?[1].Key;
                //}
            }
        }


        public string EnergySourcesZoneOverride
        {
            get { return Settings.EnergySourcesZoneOverride; }
            set
            {
                Settings.EnergySourcesZoneOverride = value;
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

        public bool? StoreBatteryHistory
        {
            get { return Settings.StoreBatteryHistory; }
            set
            {
                Settings.StoreBatteryHistory = value.Value;
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

        public int PowerDecimals
        {
            get { return Settings.PowerDecimals; }
            set
            {
                Settings.PowerDecimals = value;
            }
        }

        public int EnergyDecimals
        {
            get { return Settings.EnergyDecimals; }
            set
            {
                Settings.EnergyDecimals = value;
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



        public List<KeyValuePair<string, string>> EnergySourceZones
        {
            get { return _energySourceZones; }
            set
            {
                _energySourceZones = value;
                NotifyPropertyChanged(nameof(EnergySourceZones));
            }
        }

        public string LastSavedEnergySourceZone
        {
            set
            {
                _selectedEnergySourceZone = _energySourceZones.Where(e => e.Key == value).FirstOrDefault();
                NotifyPropertyChanged(nameof(SelectedEnergySourceZone));
            }
        }

        private KeyValuePair<string, string> _selectedEnergySourceZone;
        public KeyValuePair<string, string> SelectedEnergySourceZone
        {
            get { return _selectedEnergySourceZone; }
            set
            {
                _selectedEnergySourceZone = value;
                Settings.EnergySourcesZoneOverride = value.Key;
            }
        }

        public string AppVersion
        {
            get
            {
                Package package = Package.Current;
                PackageId packageId = package.Id;
                PackageVersion version = packageId.Version;

                return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
            }
        }

        public void UpdateProps()
        {
            NotifyPropertyChanged(nameof(SignedIn));
            NotifyPropertyChanged(nameof(SignInName));
            NotifyPropertyChanged(nameof(ShowClock));
            NotifyPropertyChanged(nameof(ShowEnergySources));
            NotifyPropertyChanged(nameof(GraphScale));
            NotifyPropertyChanged(nameof(PlaySounds));
            NotifyPropertyChanged(nameof(StoreBatteryHistory));
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
