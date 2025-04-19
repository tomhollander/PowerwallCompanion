using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;

namespace PowerwallCompanion.Converters
{
    public  class EnergyHistoryDateFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if ((string)value == "Month")
            {
                return "d/M";
            }
            else if ((string)value == "Week")
            {
                return "ddd d MMM";
            }
            else if ((string)value == "Lifetime")
            {
                return "yyyy";
            }
            else
            {
                return "MMM";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
