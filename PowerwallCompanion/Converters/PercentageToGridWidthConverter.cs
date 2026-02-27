using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace PowerwallCompanion.Converters
{
    class PercentageToGridWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            try
            {
                var doubleValue = (double)value;
                if (Double.IsNaN(doubleValue) || doubleValue < 1)
                {
                    return new GridLength(0);
                }
                return new GridLength(doubleValue, GridUnitType.Star);
            } 
            catch
            {
                return new GridLength(0);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
