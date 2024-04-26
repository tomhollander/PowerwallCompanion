﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Syncfusion.UI.Xaml.Charts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using static PowerwallCompanion.ViewModels.StatusViewModel;

namespace PowerwallCompanion.ViewModels
{
    public class ChartViewModel : INotifyPropertyChanged
    {
        private DateTimeOffset? _calendarDate;
        private DateTime _periodStart;
        private DateTime _periodEnd;
        private string _period;
        private double _homeEnergy;
        private double _solarEnergy;
        private double _gridImportedEnergy;
        private double _gridExportedEnergy;
        private double _batteryImportedEnergy;
        private double _batteryExportedEnergy;
        private double _selfConsumption;
        private double _solarUsePercent;
        private double _gridUsePercent;
        private double _batteryUsePercent;

        public ChartViewModel()
        {
        }

        public IEnumerable<string> PeriodNames
        {
            get => new string[] { "Day", "Week", "Month", "Year", "Lifetime" };
        }

        public DateTimeOffset? CalendarDate
        {
            get { return _calendarDate; }
            set
            {
                if (_calendarDate != value)
                {
                    _calendarDate = value;
                    CalculateStartAndEndDates();
                    NotifyPropertyChanged(nameof(CalendarDate));
                }
            }
        }
        public DateTime PeriodStart
        {
            get { return _periodStart; }
            set { _periodStart = value;
                NotifyPropertyChanged(nameof(PeriodStart));
            }
        }

        public DateTime PeriodEnd
        {
            get { return _periodEnd; }
            set
            {
                _periodEnd = value;
                NotifyPropertyChanged(nameof(PeriodEnd));
            }
        }

        public string Period
        {
            get {  return _period;}
            set { 
                if (_period != value)
                {
                    _period = value;
                    CalculateStartAndEndDates();
                    NotifyPropertyChanged(nameof(Period));
                }
            }
        }

        private void CalculateStartAndEndDates()
        {
            if (!CalendarDate.HasValue)
            {
                return;
            }
            switch (Period)
            {
                case "Day":
                    PeriodStart = CalendarDate.Value.Date;
                    PeriodEnd = PeriodStart.AddDays(1);
                    break;
                case "Week":
                    int offset = ((int)CalendarDate.Value.Date.DayOfWeek - 1);
                    if (offset < 0) {
                        offset += 7;
                    }
                    PeriodStart = CalendarDate.Value.Date.AddDays(-offset);
                    PeriodEnd = PeriodStart.AddDays(7);
                    break;
                case "Month":
                    PeriodStart = new DateTime(CalendarDate.Value.Year, CalendarDate.Value.Month, 1);
                    PeriodEnd = PeriodStart.AddMonths(1);
                    break;
                case "Year":
                    PeriodStart = new DateTime(CalendarDate.Value.Year, 1, 1);
                    PeriodEnd = PeriodStart.AddYears(1);
                    break;
                case "Lifetime":
                    PeriodStart = DateTime.Now.Date;
                    PeriodEnd = PeriodStart.AddDays(1);
                    break;
            }
            CalendarDate = PeriodStart;    
        }

        public double HomeEnergy
        {
            get { return _homeEnergy; }
            set
            {
                _homeEnergy = value;
                NotifyPropertyChanged(nameof(HomeEnergy));  
            }
        }

        public double SolarEnergy
        {
            get { return _solarEnergy; }
            set
            {
                _solarEnergy = value;
                NotifyPropertyChanged(nameof(SolarEnergy));
            }
        }


        public double GridExportedEnergy
        {
            get { return _gridExportedEnergy; }
            set
            {
                _gridExportedEnergy = value;
                NotifyPropertyChanged(nameof(GridExportedEnergy));
            }
        }

        public double GridImportedEnergy
        {
            get { return _gridImportedEnergy; }
            set
            {
                _gridImportedEnergy = value;
                NotifyPropertyChanged(nameof(GridImportedEnergy));
            }
        }

        public double BatteryImportedEnergy
        {
            get { return _batteryImportedEnergy; }
            set
            {
                _batteryImportedEnergy = value;
                NotifyPropertyChanged(nameof(BatteryImportedEnergy));
            }
        }

        public double BatteryExportedEnergy
        {
            get { return _batteryExportedEnergy; }
            set
            {
                _batteryExportedEnergy = value;
                NotifyPropertyChanged(nameof(BatteryExportedEnergy));
            }
        }


        public List<ChartDataPoint> HomeDailyGraphData
        {
            get; set;
        }
        public List<ChartDataPoint> SolarDailyGraphData
        {
            get; set;
        }
        public List<ChartDataPoint> GridDailyGraphData
        {
            get; set;
        }
        public List<ChartDataPoint> BatteryDailyGraphData
        {
            get; set;
        }
        public List<ChartDataPoint> HomeStackedDailyGraphData
        {
            get; set;
        }
        public List<ChartDataPoint> SolarStackedDailyGraphData
        {
            get; set;
        }
        public List<ChartDataPoint> GridStackedDailyGraphData
        {
            get; set;
        }
        public List<ChartDataPoint> BatteryStackedDailyGraphData
        {
            get; set;
        }

        public List<ChartDataPoint> BatteryDailySoeGraphData
        {
            get; set;
        }

        public List<ChartDataPoint> HomeEnergyGraphData
        {
            get; set;
        }
        public List<ChartDataPoint> SolarEnergyGraphData
        {
            get; set;
        }
        public List<ChartDataPoint> GridImportedEnergyGraphData
        {
            get; set;
        }
        public List<ChartDataPoint> GridExportedEnergyGraphData
        {
            get; set;
        }
        public List<ChartDataPoint> BatteryImportedEnergyGraphData
        {
            get; set;
        }
        public List<ChartDataPoint> BatteryExportedEnergyGraphData
        {
            get; set;
        }


        public double SelfConsumption
        {
            get { return _selfConsumption; }
            set
            {
                _selfConsumption = value;
                NotifyPropertyChanged(nameof(SelfConsumption));
            }
        }
        public double SolarUsePercent
        {
            get { return _solarUsePercent; }
            set
            {
                _solarUsePercent = value;
                NotifyPropertyChanged(nameof(SolarUsePercent));
            }
        }

        public double GridUsePercent
        {
            get { return _gridUsePercent; }
            set
            {
                _gridUsePercent = value;
                NotifyPropertyChanged(nameof(GridUsePercent));
            }
        }

        public double BatteryUsePercent
        {
            get { return _batteryUsePercent; }
            set
            {
                _batteryUsePercent = value;
                NotifyPropertyChanged(nameof(BatteryUsePercent));
            }
        }

        public Dictionary<DateTime, Dictionary<string, object>> PowerDataForExport
        { 
            get; set;  
        }

        public Dictionary<DateTime, Dictionary<string, object>> EnergyDataForExport
        {
            get; set;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string LastExceptionMessage { get; set; }
        public DateTime LastExceptionDate { get; set; }

        private StatusEnum _status;
        public StatusEnum Status
        {
            get { return _status; }
            set
            {
                _status = value;
                NotifyPropertyChanged(nameof(Status));
            }
        }

    }

}
