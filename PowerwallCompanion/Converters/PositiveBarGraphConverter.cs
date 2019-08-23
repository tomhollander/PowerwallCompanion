using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace PowerwallCompanion.Converters
{
    class PositiveBarGraphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            double doubleValue = (double)value;
            if (doubleValue < 0)
            {
                return 0D;
            }
            return doubleValue / 30 / (double)Settings.GraphScale;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
