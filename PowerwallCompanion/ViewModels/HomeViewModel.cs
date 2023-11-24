using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerwallCompanion.ViewModels
{
    public class HomeViewModel : INotifyPropertyChanged
    {
        private double _batteryPercent;
        private double _homeValue;
        private double _solarValue;
        private double _batteryValue;
        private double _gridValue;
        private bool _gridActive;
        private bool _statusOK;
        private double _minPercentToday;
        private double _maxPercentToday;
        public void NotifyProperties()
        {
            NotifyPropertyChanged(nameof(BatteryPercent));
            NotifyPropertyChanged(nameof(BatteryStatus));
            NotifyPropertyChanged(nameof(BatteryValue));
            NotifyPropertyChanged(nameof(HomeValue));
            NotifyPropertyChanged(nameof(HomeFromBattery));
            NotifyPropertyChanged(nameof(HomeFromGrid));
            NotifyPropertyChanged(nameof(HomeFromSolar));
            NotifyPropertyChanged(nameof(SolarValue));
            NotifyPropertyChanged(nameof(SolarToBattery));
            NotifyPropertyChanged(nameof(SolarToGrid));
            NotifyPropertyChanged(nameof(SolarToHome));
            NotifyPropertyChanged(nameof(GridValue));
            NotifyPropertyChanged(nameof(GridActive));
            NotifyPropertyChanged(nameof(Time));
        }

        public void NotifyChangedSettings()
        {
            NotifyPropertyChanged(nameof(ShowClock));
        }

        public double BatteryPercent
        {
            get { return _batteryPercent;  }
            set
            {
                _batteryPercent = value;
            }
        }
        public double HomeValue
        {
            get { return _homeValue; }
            set
            {
                _homeValue = value;
            }
        }

        public double SolarValue
        {
            get { return _solarValue; }
            set
            {
                _solarValue = value;
                
            }
        }

        public double BatteryValue
        {
            get { return _batteryValue; }
            set
            {
                _batteryValue = value;
            }
        }

        public double GridValue
        {
            get { return _gridValue; }
            set
            {
                _gridValue = value;
            }
        }

        public double MinBatteryPercentToday
        {
            get { return 20; } // _minPercentToday; }
        }

        public double MaxBatteryPercentToday
        {
            get { return 80; } // _maxPercentToday; }
        }

        public double HomeFromGrid
        {
            get {  return GridValue > 0D ? GridValue : 0D; }
        }

        public double HomeFromBattery
        {
            get { return BatteryValue > 0D ? BatteryValue : 0D; }
        }

        public double HomeFromSolar
        {
            get { return HomeValue - HomeFromGrid - HomeFromBattery;  }
        }

        public double SolarToGrid
        {
            get { return GridValue < 0D ? -GridValue : 0D; }
        }

        public double SolarToBattery
        {
            get { return BatteryValue < 0D ? -BatteryValue : 0D; }
        }

        public double SolarToHome
        {
            get { return SolarValue - SolarToGrid - SolarToBattery;  }
        }

        public bool StatusOK
        {
            get { return _statusOK; }
            set
            {
                _statusOK = value;
                NotifyPropertyChanged(nameof(StatusOK));
            }
        }


        public string BatteryStatus
        {
            get
            {
                if (BatteryValue < -20)
                {
                    return "Charging";
                }
                else if (BatteryValue > 20)
                {
                    return "Discharging";
                }
                else
                {
                    return "Standby";
                }
            }
        }

        public bool GridActive
        {
            get { return _gridActive; }
            set
            {
                _gridActive = value;
            }
        }
        public string Time
        {
            get
            {
                string pattern = DateTimeFormatInfo.CurrentInfo.ShortTimePattern;
                string patternWithoutAmPm = pattern.Replace("tt", "");
                return DateTime.Now.ToString(patternWithoutAmPm);
            }
        }

        public bool ShowClock
        {
            get { return Settings.ShowClock; }
        }

        public string LastExceptionMessage { get; set; }
        public DateTime LastExceptionDate { get; set; }

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
