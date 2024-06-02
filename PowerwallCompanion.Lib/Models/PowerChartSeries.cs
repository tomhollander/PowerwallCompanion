using System;
using System.Collections.Generic;
using System.Text;

namespace PowerwallCompanion.Lib.Models
{
    public class PowerChartSeries
    {
        public List<ChartDataPoint> Home { get; set; }
        public List<ChartDataPoint> Solar { get; set; }
        public List<ChartDataPoint> Grid { get; set; }
        public List<ChartDataPoint> Battery { get; set; }
    }
}
