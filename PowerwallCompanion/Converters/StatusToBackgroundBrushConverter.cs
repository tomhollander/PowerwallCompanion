using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using PowerwallCompanion.ViewModels;
using System;
using Windows.UI;

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
