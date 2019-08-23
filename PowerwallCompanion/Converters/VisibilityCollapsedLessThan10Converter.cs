using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace PowerwallCompanion.Converters
{
    class VisibilityCollapsedLessThan10Converter : IValueConverter
    {
        public object Visbility { get; private set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            double doubleValue = (double)value;
            if (doubleValue < 10D)
            {
                return Visibility.Collapsed;
            }
            else
            {
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
