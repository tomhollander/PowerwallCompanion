using PowerwallCompanion.Lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace PowerwallCompanion.Lib
{
    public class BatteryCapacityEstimator
    {
        internal struct ChargeRun
        {
            public DateTime StartDate;
            public DateTime EndDate;
            public double StartSoe;
            public double EndSoe;
        }

        private PowerwallApi _powerwallApi;
        public BatteryCapacityEstimator(PowerwallApi powerwallApi)
        {
            this._powerwallApi = powerwallApi;
        }

        public async Task<double> GetEstimatedBatteryCapacity(DateTime baselineDate)
        {
            List<ChargeRun> positiveChargeRuns = new List<ChargeRun>();
            List<ChargeRun> negativeChargeRuns = new List<ChargeRun>();

            int positiveRunsFound = 0;
            int negativeRunsFound = 0;
            int daysSearched = 0;

            // Find at least 3 significant charge and discharge runs
            while (positiveRunsFound < 3 || negativeRunsFound < 3)
            {
                var date = baselineDate.AddDays(-daysSearched);
                var batterySoeData = await _powerwallApi.GetBatteryHistoricalChargeLevel(date, date.AddDays(1));
                var positiveChargeRun = GetLargestPositiveChargeRun(batterySoeData);
                if (positiveChargeRun.EndSoe - positiveChargeRun.StartSoe > 60)
                {
                    positiveChargeRuns.Add(positiveChargeRun);
                    positiveRunsFound++;
                }
                var negativeChargeRun = GetLargestNegativeChargeRun(batterySoeData);
                if (negativeChargeRun.StartSoe - negativeChargeRun.EndSoe > 60)
                {
                    negativeChargeRuns.Add(negativeChargeRun);
                    negativeRunsFound++;
                }
                daysSearched++;
            }

            // Calculate capacity estimates from each run
            var positiveCapacityEstimates = new List<double>();
            var negativeCapacityEstimates = new List<double>();

            foreach (var chargeRun in positiveChargeRuns)
            {
                double energyForRun = Math.Abs(await GetBatteryEnergyForChargeRun(chargeRun));
                double capacityEstimate = energyForRun / (Math.Abs(chargeRun.EndSoe - chargeRun.StartSoe) / 100);
                positiveCapacityEstimates.Add(capacityEstimate);
            }

            foreach (var chargeRun in negativeChargeRuns)
            {
                double energyForRun = Math.Abs(await GetBatteryEnergyForChargeRun(chargeRun));
                double capacityEstimate = energyForRun / (Math.Abs(chargeRun.EndSoe - chargeRun.StartSoe) / 100);
                negativeCapacityEstimates.Add(capacityEstimate);
            }

            // Return the weighted average of both estimates - double weight to positive estimates
            return (positiveCapacityEstimates.Average() + positiveCapacityEstimates.Average() + negativeCapacityEstimates.Average()) / 3;
            

        }

        private ChargeRun GetLargestPositiveChargeRun(List<ChartDataPoint> soeData)
        {
            double lastSoe = soeData[0].YValue;

            DateTime currentMinDate = soeData[0].XValue;
            DateTime currentMaxDate = soeData[0].XValue;
            double currentMinSoe = soeData[0].YValue;
            double currentMaxSoe = soeData[0].YValue;

            DateTime bestMinDate = soeData[0].XValue;
            DateTime bestMaxDate = soeData[0].XValue;
            double bestMinSoe = soeData[0].YValue;
            double bestMaxSoe = soeData[0].YValue;

            foreach (var point in soeData)
            {
                // Skip surprise zero values due to data glitches
                if (point.YValue == 0 && lastSoe > 10)
                {
                    continue;
                }

                if (point.YValue <= lastSoe)
                {
                    // Store the current run if it's the best so far
                    if (currentMaxSoe - currentMinSoe > bestMaxSoe - bestMinSoe)
                    {
                        bestMinDate = currentMinDate;
                        bestMaxDate = currentMaxDate;
                        bestMinSoe = currentMinSoe;
                        bestMaxSoe = currentMaxSoe;
                    }

                    // Decreasing SOE, reset current run
                    currentMinDate = point.XValue;
                    currentMinSoe = point.YValue;
                    currentMaxDate = point.XValue;
                    currentMaxSoe = point.YValue;
                }
                else if (point.YValue > lastSoe)
                {
                    // Increasing SOE, update current max
                    currentMaxDate = point.XValue;
                    currentMaxSoe = point.YValue;
                }
                lastSoe = point.YValue;
            }
            // Store the current run if it's the best so far
            if (currentMaxSoe - currentMinSoe > bestMaxSoe - bestMinSoe)
            {
                bestMinDate = currentMinDate;
                bestMaxDate = currentMaxDate;
                bestMinSoe = currentMinSoe;
                bestMaxSoe = currentMaxSoe;
            }
            return new ChargeRun
            {
                StartDate = bestMinDate,
                EndDate = bestMaxDate,
                StartSoe = bestMinSoe,
                EndSoe = bestMaxSoe
            };
        }

        private ChargeRun GetLargestNegativeChargeRun(List<ChartDataPoint> soeData)
        {
            double lastSoe = soeData[0].YValue;

            DateTime currentMinDate = soeData[0].XValue;
            DateTime currentMaxDate = soeData[0].XValue;
            double currentMinSoe = soeData[0].YValue;
            double currentMaxSoe = soeData[0].YValue;

            DateTime bestMinDate = soeData[0].XValue;
            DateTime bestMaxDate = soeData[0].XValue;
            double bestMinSoe = soeData[0].YValue;
            double bestMaxSoe = soeData[0].YValue;

            foreach (var point in soeData)
            {
                // Skip surprise zero values due to data glitches
                if (point.YValue == 0 && lastSoe > 10)
                {
                    continue;
                }

                if (point.YValue >= lastSoe)
                {
                    // Store the current run if it's the best so far
                    if (currentMaxSoe - currentMinSoe > bestMaxSoe - bestMinSoe)
                    {
                        bestMinDate = currentMinDate;
                        bestMaxDate = currentMaxDate;
                        bestMinSoe = currentMinSoe;
                        bestMaxSoe = currentMaxSoe;
                    }

                    // Increasing SOE, reset current run
                    currentMinDate = point.XValue;
                    currentMinSoe = point.YValue;
                    currentMaxDate = point.XValue;
                    currentMaxSoe = point.YValue;
                }
                else if (point.YValue < lastSoe)
                {
                    // Decreasing SOE, update current min
                    currentMinDate = point.XValue;
                    currentMinSoe = point.YValue;
                }
                lastSoe = point.YValue;
            }

            // Store the current run if it's the best so far
            if (currentMaxSoe - currentMinSoe > bestMaxSoe - bestMinSoe)
            {
                bestMinDate = currentMinDate;
                bestMaxDate = currentMaxDate;
                bestMinSoe = currentMinSoe;
                bestMaxSoe = currentMaxSoe;
            }

            return new ChargeRun
            {
                StartDate = bestMaxDate,  // Discharge starts at higher SOE
                EndDate = bestMinDate,    // Discharge ends at lower SOE
                StartSoe = bestMaxSoe,    // Start SOE is higher
                EndSoe = bestMinSoe       // End SOE is lower
            };
        }

        private async Task<double> GetBatteryEnergyForChargeRun(ChargeRun chargeRun)
        {
            var powerData = await _powerwallApi.GetPowerChartSeriesForPeriod("day", chargeRun.StartDate, chargeRun.EndDate, PowerwallApi.PowerChartType.AllData);
            double totalEnergy = 0;
            for (int i = 1; i < powerData.Battery.Count; i++)
            {
                if (powerData.Battery[i].XValue >= chargeRun.StartDate && powerData.Battery[i].XValue <= chargeRun.EndDate)
                {
                    totalEnergy += powerData.Battery[i].YValue / 12 * 1000; // kW to Wh assuming 5 minute intervals
                }
            }
            return totalEnergy;
        }
    }
}
