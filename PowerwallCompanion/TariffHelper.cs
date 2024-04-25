using Microsoft.AppCenter.Crashes;
using Newtonsoft.Json.Linq;
using PowerwallCompanion.ViewModels;
using Syncfusion.UI.Xaml.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace PowerwallCompanion
{
    public class TariffHelper
    {

        private JObject ratePlan;


        public TariffHelper(JObject ratePlan)
        {
            this.ratePlan = ratePlan;
        }


        private JProperty GetSeasonForDate(DateTime date)
        {
            var seasons = ratePlan["response"]["seasons"];
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
                        if (fromHour < toHour)
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
            var buyRates = ratePlan["response"]["energy_charges"];
            var buySeason = buyRates[tariff.Season];
            var buyRate = buySeason[tariff.Name].Value<decimal>();
            
            var sellRates = ratePlan["response"]["sell_tariff"]["energy_charges"];
            var sellSeason = sellRates[tariff.Season];
            var sellRate = sellSeason[tariff.Name].Value<decimal>();

            return new Tuple<decimal, decimal>(buyRate, sellRate);
        }
    }
}
