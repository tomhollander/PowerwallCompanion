﻿using System;
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
        public string DisplayName
        {
            get
            {
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                return textInfo.ToTitleCase(Name.ToLower().Replace("_", " "));
            }
        }
        public System.Drawing.Color Color
        {
            get
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
