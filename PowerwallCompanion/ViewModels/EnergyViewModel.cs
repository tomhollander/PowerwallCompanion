using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace PowerwallCompanion.ViewModels
{
    public class EnergyViewModel : INotifyPropertyChanged
    {
                private const double FontScale = 1D;
        public double HomeEnergyToday
        {
            get; set;
        }

        public double HomeEnergyYesterday
        {
            get; set;
        }

        public double SolarEnergyToday
        {
            get; set;
        }

        public double SolarEnergyYesterday
        {
            get; set;
        }

        public double GridEnergyImportedToday
        {
            get; set;
        }

        public double GridEnergyImportedYesterday
        {
            get; set;
        }

        public double GridEnergyExportedToday
        {
            get; set;
        }

        public double GridEnergyExportedYesterday
        {
            get; set;
        }

        public Visibility ShowBothGridSettingsToday
        {
            get { return GridEnergyExportedToday > 500 ? Visibility.Visible : Visibility.Collapsed; }
        }

        public Visibility ShowBothGridSettingsYesterday
        {
            get { return GridEnergyExportedYesterday > 500 ? Visibility.Visible : Visibility.Collapsed; }
        }

        public double BatteryEnergyImportedToday
        {
            get; set;
        }

        public double BatteryEnergyImportedYesterday
        {
            get; set;
        }

        public double BatteryEnergyExportedToday
        {
            get; set;
        }

        public double BatteryEnergyExportedYesterday
        {
            get; set;
        }

        public double LargeFontSize
        {
            get { return FontScale * 60; }
        }
        public double MediumFontSize
        {
            get { return FontScale * 45; }
        }

        public double SmallFontSize
        {
            get { return FontScale * 30; }
        }

        public double LargeCaptionFontSize
        {
            get { return FontScale * 20; }
        }

        public double SmallCaptionFontSize
        {
            get { return FontScale * 16; }
        }

        public DateTime EnergyHistoryLastRefreshed { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public bool StatusOK
        {
            get; set; 
        }

        public string LastExceptionMessage { get; set; }
        public DateTime LastExceptionDate { get; set; }

        public void NotifyProperties()
        {
            NotifyPropertyChanged(nameof(HomeEnergyYesterday));
            NotifyPropertyChanged(nameof(HomeEnergyToday));
            NotifyPropertyChanged(nameof(SolarEnergyYesterday));
            NotifyPropertyChanged(nameof(SolarEnergyToday));
            NotifyPropertyChanged(nameof(GridEnergyImportedYesterday));
            NotifyPropertyChanged(nameof(GridEnergyImportedToday));
            NotifyPropertyChanged(nameof(GridEnergyExportedYesterday));
            NotifyPropertyChanged(nameof(GridEnergyExportedToday));
            NotifyPropertyChanged(nameof(BatteryEnergyImportedYesterday));
            NotifyPropertyChanged(nameof(BatteryEnergyImportedToday));
            NotifyPropertyChanged(nameof(BatteryEnergyExportedYesterday));
            NotifyPropertyChanged(nameof(BatteryEnergyExportedToday));
            NotifyPropertyChanged(nameof(ShowBothGridSettingsToday));
            NotifyPropertyChanged(nameof(ShowBothGridSettingsYesterday));
            NotifyPropertyChanged(nameof(StatusOK));
        }
    }
}
