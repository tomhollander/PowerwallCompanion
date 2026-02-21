using PowerwallCompanion.Lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace PowerwallCompanion.Lib
{
    public interface IEnergyAPI
    {

        Task<InstantaneousPower> GetInstantaneousPower();

        Task<Tuple<double, double>> GetBatteryMinMaxToday();

        Task<EnergyTotals> GetEnergyTotalsForDay(int dateOffset, ITariffProvider tariffHelper);

        Task<EnergyTotals> GetEnergyTotalsForPeriod(DateTime startDate, DateTime endDate, string period, ITariffProvider tariffHelper);

        Task<JsonObject> GetRatePlan();

        Task<PowerChartSeries> GetPowerChartSeriesForLastTwoDays();

        Task<PowerChartSeries> GetPowerChartSeriesForPeriod(string period, DateTime startDate, DateTime endDate, PowerChartType chartType);

        Task<List<ChartDataPoint>> GetBatteryHistoricalChargeLevel(DateTime startDate, DateTime endDate);

        Task<EnergyChartSeries> GetEnergyChartSeriesForPeriod(string period, DateTime startDate, DateTime endDate, ITariffProvider tariffHelper);
        Task ExportPowerDataToCsv(Stream stream, DateTime startDate, DateTime endDate);

        Task ExportEnergyDataToCsv(Stream stream, DateTime startDate, DateTime endDate, string period, ITariffProvider tariffHelper);

        DateTime ConvertToPowerwallDate(DateTime date);

        Task<EnergySiteInfo> GetEnergySiteInfo();

        Task StoreInstallationTimeZone();
        

    }
}
