using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace PowerwallCompanion.Converters
{
    class StatusMessageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool boolValue = (bool)value;
            string service = Settings.UseLocalGateway ? "Local Gateway" : "Tesla";
            if (boolValue == true)
            {
                return "Connected to " + service;
            }
            else
            {
                return "Not Connected to " + service;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
