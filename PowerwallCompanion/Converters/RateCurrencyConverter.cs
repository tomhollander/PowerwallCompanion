using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;

namespace PowerwallCompanion.Converters
{
    internal class RateCurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var rate = System.Convert.ToDecimal(value);

            var currencySymbol = NumberFormatInfo.CurrentInfo.CurrencySymbol;

            if (rate == 0)
            {
                return String.Empty;
            }
            else if (Math.Abs(rate) > 1)
            {
                return rate.ToString("C");
            }
            else if (currencySymbol == "$" || currencySymbol == "€")
            {
                return (rate * 100).ToString("0") + "c";
            }
            else if (currencySymbol == "£")
            {
                return (rate * 100).ToString("0") + "p";
            }
            else
            {
                return rate.ToString("C");
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
