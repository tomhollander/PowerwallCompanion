using PowerwallCompanion.Lib.Models;
using System;
using System.Collections.Generic;
using System.Runtime;
using System.Text;

namespace PowerwallCompanion.Lib
{
    public static class EnergyApiFactory
    {
        public static IEnergyAPI CreateEnergyApi(IPlatformAdapter platformAdapter)
        {
            if (platformAdapter.EnergyProvider == EnergyProvider.Powerwall)
            {
                return new PowerwallApi(platformAdapter.SiteId, platformAdapter);
            }
            else if (platformAdapter.EnergyProvider == EnergyProvider.Sigenergy)
            {
                return new SigenergyApi(platformAdapter.SiteId, platformAdapter);
            }
            else
            {
                throw new Exception("Invalid energy provider configured");
            }
        }

    }
}
