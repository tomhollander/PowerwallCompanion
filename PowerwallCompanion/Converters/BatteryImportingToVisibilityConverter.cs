using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace PowerwallCompanion.Converters
{
    internal class BatteryImportingToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            double batteryValue = (double)value;
            if (batteryValue < -20)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}