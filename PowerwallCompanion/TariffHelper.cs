using Microsoft.AppCenter.Crashes;
using Newtonsoft.Json.Linq;
using PowerwallCompanion.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerwallCompanion
{
    public class TariffHelper
    {

        private JObject ratePlan;


        public TariffHelper(JObject ratePlan)
        {
            if (ratePlan == null)
            {
                throw new ArgumentNullException(nameof(ratePlan));
            }
            this.ratePlan = ratePlan;
        }


        private JProperty GetSeasonForDate(DateTime date)
        {
            var seasons = ratePlan["response"]["seasons"];
            if (seasons == null)
            {
                return null;
            }
            foreach (var season in seasons)
            {
                var seasonData = season.First;
                int fromMonth = seasonData["fromMonth"].Value<int>();
                int toMonth = seasonData["toMonth"].Value<int>();
                int fromDay = seasonData["fromDay"].Value<int>();
                int toDay = seasonData["toDay"].Value<int>();
                DateTime fromDate = new DateTime(date.Year, fromMonth, fromDay);
                DateTime toDate = new DateTime(date.Year, toMonth, toDay);

                bool dateIsInSeason = false;
                if (toDate > fromDate)
                {
                    dateIsInSeason = (date >= fromDate && date <= toDate);
                }
                else
                {
                    dateIsInSeason = (date >= fromDate || date <= toDate);
                }
                if (dateIsInSeason)
                {
                    return (JProperty)season;
                }
            }
            return null;
        }

        public List<Tariff> GetTariffsForDay(DateTime date)
        {
            var tariffs = new List<Tariff>();
            var season = GetSeasonForDate(date);
            if (season == null)
            {
                return tariffs;
            }

            int currentWeekDay = ((int)date.DayOfWeek - 1); // Tesla uses 0 for Monday
            if (currentWeekDay == -1)
            {
                currentWeekDay = 6;
            }

            foreach (var rate in season.First()["tou_periods"])
            {
                var rateName = ((JProperty)rate).Name;
                foreach (var period in rate.First.Value<JArray>())
                {
                    var fromWeekDay = period["fromDayOfWeek"].Value<int>();
                    var toWeekDay = period["toDayOfWeek"].Value<int>();
                    if (currentWeekDay >= fromWeekDay && currentWeekDay <= toWeekDay)
                    {
                        var fromHour = period["fromHour"].Value<int>();
                        var fromMinute = period["fromMinute"].Value<int>();
                        var toHour = period["toHour"].Value<int>();
                        var toMinute = period["toMinute"].Value<int>();
                        if (fromHour == 0 && toHour == 0 && fromMinute == 0 && toMinute == 0)
                        {
                            var tariff = new Tariff
                            {
                                Name = rateName,
                                Season = season.Name,
                                StartDate = date,
                                EndDate = date.AddDays(1),
                            };
                            tariffs.Add(tariff);
                        }
                        else if (fromHour < toHour)
                        {
                            var tariff = new Tariff
                            {
                                Name = rateName,
                                Season = season.Name,
                                StartDate = date.AddHours(fromHour).AddMinutes(fromMinute),
                                EndDate = date.AddHours(toHour).AddMinutes(toMinute),
                            };
                            tariffs.Add(tariff);
                        }
                        else
                        {
                            if (toHour > 0 || toHour > 0)
                            {
                                var morningTariff = new Tariff
                                {
                                    Name = rateName,
                                    Season = season.Name,
                                    StartDate = date,
                                    EndDate = date.AddHours(toHour).AddMinutes(toMinute),
                                };
                                tariffs.Add(morningTariff);
                            }

                            if (fromHour > 0 || fromMinute > 0)
                            {
                                var eveningTariff = new Tariff
                                {
                                    Name = rateName,
                                    Season = season.Name,
                                    StartDate = date.AddHours(fromHour).AddMinutes(fromMinute),
                                    EndDate = date.AddDays(1)
                                };
                                tariffs.Add(eveningTariff);
                            }
                        }
                    }
                  
                }
            }
            return tariffs;

        }

        public Tariff GetTariffForInstant(DateTime date)
        {
            var tariffs = GetTariffsForDay(date.Date);
            return GetTariffForInstant(date, tariffs);
        }

        public Tariff GetTariffForInstant(DateTime date, List<Tariff> tariffs)
        {
            // This version is more efficient if you already have the tariffs for the day
            foreach (var tariff in tariffs)
            {
                if (date >= tariff.StartDate && date < tariff.EndDate)
                {
                    return tariff;
                }
            }
            return null;
        }

        public Tuple<decimal, decimal> GetRatesForTariff(Tariff tariff)
        {
            decimal buyRate = 0;
            var buyRates = ratePlan["response"]["energy_charges"];
            try
            {
                var buySeason = buyRates[tariff.Season];
                buyRate = buySeason[tariff.Name].Value<decimal>();
            }
            catch { }

            decimal sellRate = 0;
            var sellRates = ratePlan["response"]["sell_tariff"]["energy_charges"];
            try
            {
                var sellSeason = sellRates[tariff.Season];
                sellRate = sellSeason[tariff.Name].Value<decimal>();
            }
            catch { }

            return new Tuple<decimal, decimal>(buyRate, sellRate);
        }

        public Tuple<decimal, decimal>GetEnergyCostAndFeedInFromEnergyHistory(JArray energyHistoryTimeSeries)
        {
            // This currently assumes the history is for a single day
            try
            {
                var startDate = energyHistoryTimeSeries.First()["timestamp"].Value<DateTime>();
                var tariffs = GetTariffsForDay(startDate.Date);
                var rates = new Dictionary<string, Tuple<decimal, decimal>>();
                foreach (var tariff in tariffs)
                {
                    rates[tariff.Name] = GetRatesForTariff(tariff);
                }
                decimal totalCost = 0M;
                decimal totalFeedIn = 0M;
                foreach (var energyHistory in energyHistoryTimeSeries)
                {
                    var timestamp = energyHistory["timestamp"].Value<DateTime>();
                    var energyImported = energyHistory["grid_energy_imported"].Value<double>() / 1000; // Convert to kWh
                    var energyExported = (energyHistory["grid_energy_exported_from_solar"].Value<double>() +
                        energyHistory["grid_energy_exported_from_battery"].Value<double>() +
                        energyHistory["grid_energy_exported_from_generator"].Value<double>()) / 1000; // Convert to kWh
                    var tariff = GetTariffForInstant(timestamp, tariffs);
                    if (tariff == null)
                    {
                        continue;
                    }
                    var rate = rates[tariff.Name];
                    totalCost += (decimal)energyImported * rate.Item1;
                    totalFeedIn += (decimal)energyExported * rate.Item2;
                }
                return new Tuple<decimal, decimal>(totalCost, totalFeedIn);
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
                return null;
            }
        }
    }
}
