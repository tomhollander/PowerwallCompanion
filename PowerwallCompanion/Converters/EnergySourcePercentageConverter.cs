using PowerwallCompanion.ViewModels;
using Syncfusion.UI.Xaml.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace PowerwallCompanion.Converters
{
    internal class EnergySourcePercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var segment = (BarSegment)value;
            var energySources = (GridEnergySources)segment.Item;
            int percentage = (int)(segment.YData / energySources.Total * 100);
            return $" ({percentage}%)";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
