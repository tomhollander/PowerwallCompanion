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
        private List<BatteryInfo> _batteryInfos;
        public List<BatteryInfo> BatteryInfos
        {
            get {  return _batteryInfos; }
            set { _batteryInfos = value; 
                NotifyPropertyChanged(nameof(BatteryInfos));
            }

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
        public string Name { get; set; }
        public double WarrantedCapacity {  get { return 13200;  } }

        public double TotalPackEnergy {  get; set; }

        public double CurrentCapacityPercent {  get { return TotalPackEnergy / WarrantedCapacity * 100; } }



    }
}
