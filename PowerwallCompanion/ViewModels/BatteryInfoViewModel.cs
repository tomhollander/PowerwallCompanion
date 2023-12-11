using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.UserProfile;

namespace PowerwallCompanion.ViewModels
{
    public class BatteryInfoViewModel : INotifyPropertyChanged
    {

        public string SiteName { get; set; }
        public int NumberOfBatteries { get; set; }
        public DateTime InstallDate { get; set; }
        public string InstallDateString { get { return InstallDate.ToString("d"); } }
        public double WarrantedCapacity { 
            get 
            {
                int capacity = 13500;
                var region = GlobalizationPreferences.HomeGeographicRegion;
                if (region == "AU" || region == "NZ")
                {
                    capacity = 13200;
                }

                return capacity * NumberOfBatteries; 
            } 
        }

        public double TotalPackEnergy { get; set; }

        public double CurrentCapacityPercent { get { return TotalPackEnergy / WarrantedCapacity * 100; } }
        public double Degradation { get { return 100 - CurrentCapacityPercent; } }

        public void NotifyAllProperties()
        {
            NotifyPropertyChanged(nameof(SiteName));
            NotifyPropertyChanged(nameof(InstallDateString));
            NotifyPropertyChanged(nameof(NumberOfBatteries));
            NotifyPropertyChanged(nameof(WarrantedCapacity));
            NotifyPropertyChanged(nameof(TotalPackEnergy));
            NotifyPropertyChanged(nameof(CurrentCapacityPercent));
            NotifyPropertyChanged(nameof(Degradation));
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class BatteryInfo
    {
        


    }
}
