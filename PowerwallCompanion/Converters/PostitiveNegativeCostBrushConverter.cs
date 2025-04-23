using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace PowerwallCompanion.Converters
{
    internal class PostitiveNegativeCostBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var cost = System.Convert.ToDecimal(value);
            if (cost > 0M)
            {
                return new SolidColorBrush(Colors.LightGray); // Use Colors from Windows.UI
            }
            else
            {
                return new SolidColorBrush(Colors.LightGreen); // Use Colors from Windows.UI
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
