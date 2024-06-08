using System;
using System.Collections.Generic;
using System.Text;

namespace PowerwallCompanion.Lib.Models
{
    public class EnergyChartSeries
    {
        public List<ChartDataPoint> Home { get; set; }
        public List<ChartDataPoint> Solar { get; set; }
        public List<ChartDataPoint> GridImport { get; set; }
        public List<ChartDataPoint> GridExport { get; set; }
        public List<ChartDataPoint> BatteryCharge { get; set; }
        public List<ChartDataPoint> BatteryDischarge { get; set; }
        public EnergyTotals EnergyTotals { get; set; }
    }
}
