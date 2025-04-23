using PowerwallCompanion.Lib.Models;
using Syncfusion.UI.Xaml.Charts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using static PowerwallCompanion.ViewModels.StatusViewModel;

namespace PowerwallCompanion.ViewModels
{
    public class ChartViewModel : INotifyPropertyChanged
    {
        private DateTimeOffset? _calendarDate;
        private DateTime _periodStart;
        private DateTime _periodEnd;
        private string _period;

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

        public DateTimeIntervalType ChartPeriodInterval
        {
            get
            {
                switch (Period)
                {
                    case "Day":
                        return DateTimeIntervalType.Hours;
                    case "Week":
                        return DateTimeIntervalType.Days;
                    case "Month":
                        return DateTimeIntervalType.Days;
                    case "Year":
                        return DateTimeIntervalType.Months;
                    case "Lifetime":
                        return DateTimeIntervalType.Years;
                    default:
                        return DateTimeIntervalType.Days;
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

        public EnergyTotals EnergyTotals
        {
            get; set;
        }
       

        public PowerChartSeries PowerChartSeries { get; set;  }
        public PowerChartSeries PowerChartStackingSeries { get; set; }
  

        public List<ChartDataPoint> BatteryDailySoeGraphData
        {
            get; set;
        }


        public EnergyChartSeries EnergyChartSeries { get; set; }


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

        public DateTime ChartsLastUpdated { get; set; }

    }

}
