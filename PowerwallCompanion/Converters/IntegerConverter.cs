using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;

namespace PowerwallCompanion.Converters
{
    class IntegerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (targetType == typeof(Int32))
            {
                Int32 result;
                bool ok = Int32.TryParse((string)value, out result);
                if (ok)
                {
                    return result > 4 ? 4 : result;
                }
            }
            return 1;
        }
    }
}
