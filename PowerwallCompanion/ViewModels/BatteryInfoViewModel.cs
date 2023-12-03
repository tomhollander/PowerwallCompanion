using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerwallCompanion.ViewModels
{
    public class BatteryInfoViewModel : INotifyPropertyChanged
    {

        public string SiteName { get; set; }

        public int NumberOfBatteries { get; set; }
        public double WarrantedCapacity { get { return 13200 * NumberOfBatteries; } }

        public double TotalPackEnergy { get; set; }

        public double CurrentCapacityPercent { get { return TotalPackEnergy / WarrantedCapacity * 100; } }
        public double Degradation { get { return 100 - CurrentCapacityPercent; } }

        public void NotifyAllProperties()
        {
            NotifyPropertyChanged(nameof(SiteName));
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
