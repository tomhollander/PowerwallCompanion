using PowerwallCompanion.Lib.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PowerwallCompanion.Lib
{
    public interface ITariffProvider
    {
        string ProviderName { get; }
        Task<List<Tariff>> GetTariffsForDay(DateTime date);
        bool IsSingleRatePlan { get; }
        decimal DailySupplyCharge { get; }
        Task<Tariff> GetInstantaneousTariff();
        Task<Tuple<decimal, decimal>> GetEnergyCostAndFeedInFromEnergyHistory(List<JsonNode> energyHistoryTimeSeries);
        Tuple<decimal, decimal> GetRatesForTariff(Tariff tariff);
    }
}
