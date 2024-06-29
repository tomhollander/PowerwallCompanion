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
    public class StatusToBackgroundBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var status = (StatusViewModel.StatusEnum)value;
            if (status == StatusViewModel.StatusEnum.Online)
            {
                return new SolidColorBrush(Colors.DimGray);
            }
            else if (status == StatusViewModel.StatusEnum.GridOutage)
            {
                return new SolidColorBrush(Colors.SaddleBrown);
            }
            else if (status == StatusViewModel.StatusEnum.Error)
            {
                return new SolidColorBrush(Color.FromArgb(255, 50, 50, 50));
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
