using PowerwallCompanion.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerwallCompanion
{
    public static class TariffProviderFactory
    {
        public static async Task<ITariffProvider> Create(PowerwallApi powerwallApi)
        {
            Telemetry.TrackEvent("Initialising tariff provider", new Dictionary<string, string> { { "provider", Settings.TariffProvider } });
            if (Settings.TariffProvider == "Tesla")
            {
                var ratePlan = await powerwallApi.GetRatePlan();
                return new TeslaRatePlanTariffProvider(ratePlan);
            }
            else if (Settings.TariffProvider == "Amber")
            {
                return new AmberElectricTariffProvider(Settings.AmberElectricApiKey);
            }
            else
            {
                throw new InvalidOperationException("Unknown tariff provider"); 
            }
        }
    }
}
