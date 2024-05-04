﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace PowerwallCompanion.Converters
{
    internal class PostitiveNegativeCostBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var cost = System.Convert.ToDecimal(value);
            if (cost > 0M)
            {
                return new SolidColorBrush(Windows.UI.Colors.LightGray);
            }
            else
            {
                return new SolidColorBrush(Windows.UI.Colors.LightGreen);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
