using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace PowerwallCompanion.Converters
{
    class HideGridRowConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            double doubleValue = (double)value;
            if (doubleValue < 50)
            {
                return new GridLength(0D);
            }
            return GridLength.Auto;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
