using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace PowerwallCompanion.Converters
{
    class BatteryCapacityToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var doubleValue = (double)value;
            if (doubleValue < 70)
            {
                return new SolidColorBrush(Colors.DarkRed);
            } 
            else if (doubleValue < 90)
            {
                return new SolidColorBrush(Colors.DarkGoldenrod);
            }
            else
            {
                return new SolidColorBrush(Colors.DarkGreen);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
