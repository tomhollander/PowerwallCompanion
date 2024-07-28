using System;
using System.Drawing;
using System.Globalization;

namespace PowerwallCompanion.Lib.Models
{
    public class Tariff
    {
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Season { get; set; }
        public bool IsDynamic { get; set; }
        public Tuple<decimal, decimal> DynamicSellAndFeedInRate { get; set; }
        public string DisplayName
        {
            get
            {
                if (IsDynamic)
                {
                    if (DynamicSellAndFeedInRate.Item1 < 0)
                        return "Negative";
                    else if (DynamicSellAndFeedInRate.Item1 < 0.10M)
                        return "Very Cheap";
                    else if (DynamicSellAndFeedInRate.Item1 < 0.20M)
                        return "Cheap";
                    else if (DynamicSellAndFeedInRate.Item1 < 0.30M)
                        return "Moderate";
                    else
                        return "Expensive";
                }
                else
                {
                    TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                    return textInfo.ToTitleCase(Name.ToLower().Replace("_", " "));
                }

            }
        }
        public System.Drawing.Color Color
        {
            get
            {
                if (IsDynamic)
                {
                    if (DynamicSellAndFeedInRate.Item1 < 0)
                        return Color.Magenta;
                    else if (DynamicSellAndFeedInRate.Item1 < 0.10M)
                        return Color.Blue;
                    else if(DynamicSellAndFeedInRate.Item1 < 0.20M)
                        return Color.Green;
                    else if (DynamicSellAndFeedInRate.Item1 < 0.30M)
                        return Color.DarkOrange;
                    else
                        return Color.Red;
                }
                else
                {
                    switch (Name)
                    {
                        case "SUPER_OFF_PEAK":
                            return Color.Blue;
                        case "OFF_PEAK":
                            return Color.Green;
                        case "PARTIAL_PEAK":
                            return Color.DarkOrange;
                        case "ON_PEAK":
                            return Color.Red;
                        default:
                            return Color.DarkGray;
                    }
                }

            }
        }

        public override bool Equals(object obj)
        {
            var t = obj as Tariff;
            if (t == null)
                return false;
            return t.Name == Name && t.StartDate == StartDate && t.EndDate == EndDate;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ StartDate.GetHashCode() ^ EndDate.GetHashCode();
        }

    }
}
