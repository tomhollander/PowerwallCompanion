using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;

namespace PowerwallCompanion.Converters
{
    class DecimalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (targetType == typeof(Decimal))
            {
                Decimal result;
                bool ok = Decimal.TryParse((string)value, out result);
                if (ok)
                {
                    return result;
                }
            }
            return Settings.DefaultGraphScale;
        }
    }
}
