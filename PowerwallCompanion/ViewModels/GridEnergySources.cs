using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerwallCompanion.ViewModels
{
    public class GridEnergySources
    {
        public string Category { get; set; }
        public int Nuclear { get; set; }
        public int Geothermal { get; set; }
        public int Biomass { get; set; }
        public int Coal { get; set; }
        public int Wind { get; set; }
        public int Solar { get; set; }
        public int Hydro { get; set; }
        public int Gas { get; set; }
        public int Oil { get; set; }
        public int BatteryStorage { get; set; }
        public int HydroStorage { get; set; }
        public int Unknown { get; set; }
        public int Total
        {
            get { return Nuclear + Geothermal + Biomass + Coal + Wind + Solar + Hydro + Gas + Oil + BatteryStorage + HydroStorage + Unknown; }
        }
    }
}
