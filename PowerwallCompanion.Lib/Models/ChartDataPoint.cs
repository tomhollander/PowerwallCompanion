using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerwallCompanion.Lib.Models
{
    public class ChartDataPoint 
    {

        public ChartDataPoint(DateTime xValue, double yValue)
        {
            XValue = xValue;
            YValue = yValue;
        }

        public DateTime XValue
        {
            get; set;
        }

        
        public double YValue
        {
            get; set;
        }
    }
}
