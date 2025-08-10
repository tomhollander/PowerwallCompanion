using PowerwallCompanion.Lib.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace PowerwallCompanion.ViewModels
{
    public class StatusViewModel : INotifyPropertyChanged
    {
        private StatusEnum _status;
        public enum StatusEnum
        {
            Online,
            GridOutage,
            Error
        }


        public StatusViewModel()
        {
        }

        public void Reset()
        {
            EnergyHistoryLastRefreshed = DateTime.MinValue;
            PowerHistoryLastRefreshed = DateTime.MinValue;
            LiveStatusLastRefreshed = DateTime.MinValue;

        }

        public InstantaneousPower InstantaneousPower
        {
            get; set;
        }

        public double AnimatedBatteryPercentStart
        {
            get; set;
        }

        public double AnimatedBatteryPercentEnd
        {
            get; set;
        }
        public void NotifyPowerProperties()
        {
            NotifyPropertyChanged(nameof(InstantaneousPower));
            NotifyPropertyChanged(nameof(AnimatedBatteryPercentStart));
            NotifyPropertyChanged(nameof(AnimatedBatteryPercentEnd));
            NotifyPropertyChanged(nameof(MinBatteryPercentToday));
            NotifyPropertyChanged(nameof(MaxBatteryPercentToday));

            NotifyPropertyChanged(nameof(CostPerHour));
            NotifyPropertyChanged(nameof(FeedInPerHour));
            NotifyPropertyChanged(nameof(TariffFeedInVisibility));
            NotifyPropertyChanged(nameof(TariffCostVisibility));
            NotifyPropertyChanged(nameof(Time));
        }

        public void NotifyDailyEnergyProperties()
        {
            NotifyPropertyChanged(nameof(EnergyTotalsYesterday));
            NotifyPropertyChanged(nameof(EnergyTotalsToday));
            NotifyPropertyChanged(nameof(ShowBothGridSettingsToday));
            NotifyPropertyChanged(nameof(ShowBothGridSettingsYesterday));
            NotifyPropertyChanged(nameof(EnergyCostTooltipToday));
            NotifyPropertyChanged(nameof(EnergyCostTooltipYesterday));
            NotifyPropertyChanged(nameof(Time));
        }

        public void NotifyGraphProperties()
        {
            NotifyPropertyChanged(nameof(PowerChartSeries));
            NotifyPropertyChanged(nameof(GraphDayBoundary));
            NotifyPropertyChanged(nameof(ChartMaxDate));
        }

    

        public void NotifyChangedSettings()
        {
            NotifyPropertyChanged(nameof(ShowClock));
        }


        public double MinBatteryPercentToday
        {
            get; set;
        }

        public double MaxBatteryPercentToday
        {
            get; set;
        }
        
        public EnergyTotals EnergyTotalsYesterday
        {
            get; set; 
        }

        public EnergyTotals EnergyTotalsToday
        {
            get; set;
        }

        
        public Visibility ShowBothGridSettingsToday
        {
            get 
            { 
                if (EnergyTotalsToday== null)
                {
                    return Visibility.Collapsed;
                }
                return EnergyTotalsToday.GridEnergyExported > 500 ? Visibility.Visible : Visibility.Collapsed; 
            }
        }

        public Visibility ShowBothGridSettingsYesterday
        {
            get
            {
                if (EnergyTotalsYesterday == null)
                {
                    return Visibility.Collapsed;
                }
                return EnergyTotalsYesterday.GridEnergyExported > 500 ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        
        public PowerChartSeries PowerChartSeries
        {
            get; set;
        }

       
        public DateTime ChartMaxDate
        {
            get; set;
        }

        public EnergySiteInfo EnergySiteInfo
        {
            get; set; 
        }
        public StatusEnum Status
        {
            get { return _status; }
            set
            {
                _status = value;
                NotifyPropertyChanged(nameof(Status));
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
            get
            {
                return Settings.ShowClock;
            }
        }

        public string TariffName
        {
            get; set;
        }

        public decimal TariffBuyRate
        {
            get; set;
        }

        public decimal TariffSellRate
        {
            get; set;
        }

        public decimal CostPerHour
        {
            get 
            {
                if (InstantaneousPower == null)
                {
                    return 0;
                }
                return TariffSellRate * (decimal)(InstantaneousPower.HomeFromGrid / 1000); 
            }
        }

        public decimal FeedInPerHour
        {
            get 
            {
                if (InstantaneousPower == null)
                {
                    return 0;
                }
                return TariffBuyRate * (decimal)(InstantaneousPower.SolarToGrid / 1000); 
            }

        }

        public string EnergyCostsTooltip
        {
            get => $"Buy at: {TariffSellRate.ToString("c")}/kWh\nSell at: {TariffBuyRate.ToString("c")}/kWh";
        }


        public Brush TariffColor
        {
            get; set;
        }
        public Visibility TariffFeedInVisibility
        {
            get
            {
                if (InstantaneousPower == null)
                {
                    return Visibility.Collapsed;
                }
                return (TariffBuyRate > 0) && (InstantaneousPower.SolarToGrid > 50D) ? Visibility.Visible : Visibility.Collapsed;

            }
        }

        public Visibility TariffCostVisibility
        {
            get
            {
                if (InstantaneousPower == null)
                {
                    return Visibility.Collapsed;
                }    
                return InstantaneousPower.HomeFromGrid > 50D ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility TariffBadgeVisibility
        {
            get; set; 
        }

        
        public string EnergyCostTooltipToday
        {
            get
            {
                if (EnergyTotalsToday == null)
                {
                    return null;
                }
                string message = "Estimated net cost of today's energy based on current rate plan.\n";

                if (Settings.TariffDailySupplyCharge > 0)
                {
                    message += $"Daily supply charge: {Settings.TariffDailySupplyCharge.ToString("c")}\n";
                }
                decimal energyCost = EnergyTotalsToday.EnergyCost - Settings.TariffDailySupplyCharge;
                message += $"Energy cost: {energyCost.ToString("c")}\nFeed in: {EnergyTotalsToday.EnergyFeedIn.ToString("c")}";

                return message;
            }
        }

        public string EnergyCostTooltipYesterday
        {
            get
            {
                if (EnergyTotalsYesterday == null)
                {
                    return null;
                }
                string message = "Estimated net cost of yesterday's energy based on current rate plan.\n";

                if (Settings.TariffDailySupplyCharge > 0)
                {
                    message += $"Daily supply charge: {Settings.TariffDailySupplyCharge.ToString("c")}\n";
                }
                decimal energyCost = EnergyTotalsYesterday.EnergyCost - Settings.TariffDailySupplyCharge;
                message += $"Energy cost: {energyCost.ToString("c")}\nFeed in: {EnergyTotalsYesterday.EnergyFeedIn.ToString("c")}";

                return message;
            }
        }

        public void NotifyTariffProperties()
        {
            NotifyPropertyChanged(nameof(TariffName));
            NotifyPropertyChanged(nameof(TariffBuyRate));
            NotifyPropertyChanged(nameof(TariffSellRate));
            NotifyPropertyChanged(nameof(TariffColor));
            NotifyPropertyChanged(nameof(TariffFeedInVisibility));
            NotifyPropertyChanged(nameof(TariffCostVisibility));
            NotifyPropertyChanged(nameof(CostPerHour));
            NotifyPropertyChanged(nameof(FeedInPerHour));
            NotifyPropertyChanged(nameof(TariffBadgeVisibility));
            NotifyPropertyChanged(nameof(EnergyCostsTooltip));
        }

        public string LastExceptionMessage { get; set; }
        public DateTime LastExceptionDate { get; set; }
        public DateTime LiveStatusLastRefreshed { get; set; }
        public DateTime EnergyHistoryLastRefreshed { get; set; }
        public DateTime EnergySiteInfoLastRefreshed { get; set; }

        public DateTime PowerHistoryLastRefreshed { get; set; }
        public DateTime GraphDayBoundary
        {
            get { return DateTime.Today; }
        }

        // Specifies the current day for the min/max battery charge level 
        public DateTime BatteryDay
        { 
            get; set;  
        }
        
        
        private double _gridLowCarbonPercent;
        public double GridLowCarbonPercent
        {
            get { return _gridLowCarbonPercent; }
            set
            {
                _gridLowCarbonPercent = value;
                NotifyPropertyChanged(nameof(GridLowCarbonPercent));
            }
        }
        private GridEnergySources _gridEnergySources;
        public GridEnergySources GridEnergySources
        {
            get { return _gridEnergySources; }
            set
            {
                _gridEnergySources = value;
                NotifyPropertyChanged(nameof(GridEnergySources));
                NotifyPropertyChanged(nameof(GridEnergySourcesList));
            }
        }
        public List<GridEnergySources> GridEnergySourcesList
        {
            get { return _gridEnergySources == null ? null : new List<GridEnergySources>() { _gridEnergySources }; }
        }

        private string _gridEnergySourcesStatusMessage;
        public string GridEnergySourcesStatusMessage
        {
            get { return _gridEnergySourcesStatusMessage; }
            set
            {
                _gridEnergySourcesStatusMessage = value;
                NotifyPropertyChanged(nameof(GridEnergySourcesStatusMessage));
            }
        }

        public Visibility EnergySourcesVisibility
        {
            get 
            {  
                if (Settings.ShowEnergySources)
                {
                    return Visibility.Visible;
                }
                else
                { 
                    return Visibility.Collapsed; 
                }
            }
        }

        public Visibility EnergyRatesVisibility
        {
            get
            {
                if (Settings.ShowEnergyRates)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        public string SelectedEnergySourceName { get; set; }
        public int SelectedEnergySourcePower { get; set; }
        public string SelectedEnergySourcePercentageLabel { get; set; }
        public Brush SelectedEnergySourceBrush { get; set; }
        public void NotifySelectedEnergySourceProperties()
        {
            NotifyPropertyChanged(nameof(SelectedEnergySourceName));
            NotifyPropertyChanged(nameof(SelectedEnergySourcePower));
            NotifyPropertyChanged(nameof(SelectedEnergySourceBrush));
            NotifyPropertyChanged(nameof(SelectedEnergySourcePercentageLabel));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
