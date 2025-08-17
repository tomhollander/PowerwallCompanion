using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;

namespace PowerwallCompanion.Converters
{
    internal class BatteryPowerTooltipMessageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            double doubleValue = (double)value / 1000;
            string message = string.Empty;
            if (doubleValue < 0)
            {
                doubleValue = Math.Abs(doubleValue);
                message = " kW of power charging the battery now";
            }
            else
            {
                message = " kW of power discharging from the battery now";
            }
            return doubleValue.ToString("f2") + message;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
