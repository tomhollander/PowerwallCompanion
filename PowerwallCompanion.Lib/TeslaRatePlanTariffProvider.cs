﻿using PowerwallCompanion.Lib.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PowerwallCompanion.Lib
{
    public class TeslaRatePlanTariffProvider : ITariffProvider
    {

        private JsonObject ratePlan;
        private decimal dailySupplyCharge;
        private decimal nonBypassableCharge;

        public TeslaRatePlanTariffProvider(JsonObject ratePlan, decimal dailySupplyCharge, decimal nonBypassableCharge)
        {
            if (ratePlan == null)
            {
                throw new ArgumentNullException(nameof(ratePlan));
            }
            this.ratePlan = ratePlan;
            this.dailySupplyCharge = dailySupplyCharge;
            this.nonBypassableCharge = nonBypassableCharge;
        }

        public string ProviderName => "Tesla";

        private JsonNode GetSeasonForDate(DateTime date)
        {
            var seasons = ratePlan["response"]["seasons"].AsObject();
            if (seasons == null)
            {
                return null;
            }
            foreach (var season in seasons)
            {
                var seasonData = season.Value;
                int fromMonth = seasonData["fromMonth"].GetValue<int>();
                int toMonth = seasonData["toMonth"].GetValue<int>();
                int fromDay = seasonData["fromDay"].GetValue<int>();
                int toDay = seasonData["toDay"].GetValue<int>();

                // Ensure fromDay/toDay is not outside the valid dates for the month (yes it can happen with the tariff data)
                int daysInFromMonth = DateTime.DaysInMonth(date.Year, fromMonth);
                if (fromDay > daysInFromMonth)
                {
                    toDay = daysInFromMonth;
                }
                int daysInToMonth = DateTime.DaysInMonth(date.Year, toMonth);
                if (toDay > daysInToMonth)
                {
                    toDay = daysInToMonth;
                }

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
                    return seasonData;
                }
            }
            return null;
        }

        public async Task<List<Tariff>> GetTariffsForDay(DateTime date)
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

            foreach (var rate in season["tou_periods"].AsObject())
            {
                var rateName = rate.Key;
                foreach (var period in rate.Value.AsArray())
                {
                    var fromWeekDay = period["fromDayOfWeek"].GetValue<int>();
                    var toWeekDay = period["toDayOfWeek"].GetValue<int>();
                    if (currentWeekDay >= fromWeekDay && currentWeekDay <= toWeekDay)
                    {
                        var fromHour = period["fromHour"].GetValue<int>();
                        var fromMinute = period["fromMinute"].GetValue<int>();
                        var toHour = period["toHour"].GetValue<int>();
                        var toMinute = period["toMinute"].GetValue<int>();
                        if (fromHour == 0 && toHour == 0 && fromMinute == 0 && toMinute == 0)
                        {
                            var tariff = new Tariff
                            {
                                Name = rateName,
                                Season = season.GetPropertyName(),
                                StartDate = date,
                                EndDate = date.AddDays(1),
                                DisplayName = GetDisplayName(rateName),
                                Color = GetColor(rateName),
                            };
                            tariffs.Add(tariff);
                        }
                        else if (fromHour < toHour)
                        {
                            var tariff = new Tariff
                            {
                                Name = rateName,
                                Season = season.GetPropertyName(),
                                StartDate = date.AddHours(fromHour).AddMinutes(fromMinute),
                                EndDate = date.AddHours(toHour).AddMinutes(toMinute),
                                DisplayName = GetDisplayName(rateName),
                                Color = GetColor(rateName),
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
                                    Season = season.GetPropertyName(),
                                    StartDate = date,
                                    EndDate = date.AddHours(toHour).AddMinutes(toMinute),
                                    DisplayName = GetDisplayName(rateName),
                                    Color = GetColor(rateName),
                                };
                                tariffs.Add(morningTariff);
                            }

                            if (fromHour > 0 || fromMinute > 0)
                            {
                                var eveningTariff = new Tariff
                                {
                                    Name = rateName,
                                    Season = season.GetPropertyName(),
                                    StartDate = date.AddHours(fromHour).AddMinutes(fromMinute),
                                    EndDate = date.AddDays(1),
                                    DisplayName = GetDisplayName(rateName),
                                    Color = GetColor(rateName),
                                };
                                tariffs.Add(eveningTariff);
                            }
                        }
                    }
                  
                }
            }
            return tariffs;
        }

        public bool IsSingleRatePlan
        {
            get
            {
                var season = GetSeasonForDate(DateTime.Now.Date);
                if (season == null || season.AsObject()["tou_periods"] == null)
                {
                    return true;
                }
                return season.AsObject()["tou_periods"].AsObject().Count() <= 1;
            }
        }

        public async Task<Tariff> GetInstantaneousTariff()
        {
            return await GetTariffForInstant(DateTime.Now);
        }
        // Not part of interface, pubilc for testing
        public async Task<Tariff> GetTariffForInstant(DateTime date)
        {
            var tariffs = await GetTariffsForDay(date.Date);
            return GetTariffForInstant(date, tariffs);
        }

        private Tariff GetTariffForInstant(DateTime date, List<Tariff> tariffs)
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
                buyRate = buySeason[tariff.Name].GetValue<decimal>();
            }
            catch { }

            decimal sellRate = 0;
            var sellRates = ratePlan["response"]["sell_tariff"]["energy_charges"];
            try
            {
                var sellSeason = sellRates[tariff.Season];
                sellRate = sellSeason[tariff.Name].GetValue<decimal>();
            }
            catch { }

            return new Tuple<decimal, decimal>(buyRate, sellRate);
        }

        public async Task<Tuple<decimal, decimal>>GetEnergyCostAndFeedInFromEnergyHistory(List<JsonNode> energyHistoryTimeSeries)
        {
            // This currently assumes the history is for a single day

            var startDate = Utils.GetUnspecifiedDateTime(energyHistoryTimeSeries.First()["timestamp"]);
            var tariffs = await GetTariffsForDay(startDate.Date);
            var rates = new Dictionary<string, Tuple<decimal, decimal>>();
            foreach (var tariff in tariffs)
            {
                rates[tariff.Name] = GetRatesForTariff(tariff);
            }
            decimal totalCost = 0M;
            decimal totalFeedIn = 0M;
            decimal nonBypassableCost = 0M;
            foreach (var energyHistory in energyHistoryTimeSeries)
            {
                var timestamp = Utils.GetUnspecifiedDateTime(energyHistory["timestamp"]);
                var energyImported = Utils.GetValueOrDefault<double>(energyHistory["grid_energy_imported"]) / 1000; // Convert to kWh
                var energyExported = (Utils.GetValueOrDefault<double>(energyHistory["grid_energy_exported_from_solar"]) +
                    Utils.GetValueOrDefault<double>(energyHistory["grid_energy_exported_from_battery"]) +
                    Utils.GetValueOrDefault<double>(energyHistory["grid_energy_exported_from_generator"])) / 1000; // Convert to kWh
                var tariff = GetTariffForInstant(timestamp, tariffs);
                if (tariff == null)
                {
                    continue;
                }
                var rate = rates[tariff.Name];
                totalCost += (decimal)energyImported * rate.Item1;
                totalFeedIn += (decimal)energyExported * rate.Item2;
                nonBypassableCost += (decimal)energyImported * this.nonBypassableCharge;
            }

            if (nonBypassableCharge > 0)
            {
                // TODO: confirm this is the right way of doing it... 
                totalCost += nonBypassableCost;
                totalCost = Math.Max(totalCost, nonBypassableCost); // Ensure we charge at least the non-bypassable charge
            }

            totalCost += dailySupplyCharge;
            return new Tuple<decimal, decimal>(totalCost, totalFeedIn);
            

        }

        private string GetDisplayName(string tariffName)
        {
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            return textInfo.ToTitleCase(tariffName.ToLower().Replace("_", " "));
        }

        private Color GetColor(string tariffName)
        {
            switch (tariffName)
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

        public decimal DailySupplyCharge
        {
            get => this.dailySupplyCharge;
        }
    }
}
