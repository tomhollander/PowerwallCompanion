﻿using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace PowerwallCompanion.Converters
{
    class BatteryCapacityToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var doubleValue = (double)value;
            if (doubleValue < 70)
            {
                return new SolidColorBrush(Colors.DarkRed);
            } 
            else if (doubleValue < 90)
            {
                return new SolidColorBrush(Colors.DarkGoldenrod);
            }
            else
            {
                return new SolidColorBrush(Colors.DarkGreen);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
