using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.PointOfService;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace PowerwallCompanion.ViewModels
{
    public class Tariff
    {
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string DisplayName
        {
            get
            {
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                return textInfo.ToTitleCase(Name.ToLower().Replace("_", " "));
            }
        }
        public Brush Color
        {
            get
            {
                Color c;
                switch (Name)
                {
                    case "SUPER_OFF_PEAK":
                        c = Colors.Blue;
                        break;
                    case "OFF_PEAK":
                        c = Colors.Green;
                        break;
                    case "PARTIAL_PEAK":
                        c = Colors.Yellow;
                        break;
                    case "ON_PEAK":
                        c = Colors.Red;
                        break;
                    default:
                        c = Colors.White;
                        break;
                }
                return new SolidColorBrush(c);
            }
        }

    }
}
