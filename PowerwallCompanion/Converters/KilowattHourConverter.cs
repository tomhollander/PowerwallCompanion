using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace PowerwallCompanion.Converters
{
    class KilowattHourConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string precision = (string) parameter ?? Settings.EnergyDecimals.ToString();
            double scaledValue = (double)(value) / 1000;
            return scaledValue.ToString("f" + precision);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
