using PowerwallCompanion.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace PowerwallCompanion.Converters
{
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var status = (StatusViewModel.StatusEnum)value;
            if (status == StatusViewModel.StatusEnum.Online)
            {
                return new SolidColorBrush(Colors.LimeGreen);
            }
            else if (status == StatusViewModel.StatusEnum.GridOutage)
            {
                return new SolidColorBrush(Colors.Orange);
            }
            else if (status == StatusViewModel.StatusEnum.Error)
            {
                return new SolidColorBrush(Colors.DarkGray);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
