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
        public string DisplayName { get; set; }
        public System.Drawing.Color Color { get; set; }
       

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
