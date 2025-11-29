using PowerwallCompanion.Lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace PowerwallCompanion.Lib
{
    public class BatteryCapacityEstimator
    {
        public struct ChargeRun 
        {
            public DateTime StartDate;
            public DateTime EndDate;
            public double StartSoe;
            public double EndSoe;
        }

        private PowerwallApi _powerwallApi;
        private int _numberOfBatteries;

        public BatteryCapacityEstimator(PowerwallApi powerwallApi, int numberOfBatteries)
        {
            this._powerwallApi = powerwallApi;
            this._numberOfBatteries = numberOfBatteries;
        }

        public async Task<double> GetEstimatedBatteryCapacity(DateTime baselineDate)
        {
            List<ChargeRun> positiveChargeRuns = new List<ChargeRun>();

            int positiveRunsFound = 0;
            int idealLengthRunsFound = 0;

            double minimumRunLength = _numberOfBatteries == 1 ? 60 : (_numberOfBatteries == 2 ? 40 : 30);
            const double idealRunLength = 80;
            const int idealRunsToFind = 4;
            const int maxRunsToFind = 10;
            const int maxDaysToSearch = 30;
            const int numberOfRunsForEstimate = 4;

            const double chargeLossFactor = 0.96;

            // Search up to maxDaysToSearch days backward, capturing qualifying runs
            for (int daysSearched = 0; daysSearched < maxDaysToSearch; daysSearched++)
            {
                if (positiveRunsFound >= maxRunsToFind) break; // Safety cap on total runs
                if (idealLengthRunsFound >= idealRunsToFind) break; // Found enough ideal-length runs

                var date = baselineDate.AddDays(-daysSearched);
                try
                {
                    var batterySoeData = await _powerwallApi.GetBatteryHistoricalChargeLevel(date, date.AddDays(1));
                    var positiveChargeRun = GetLargestPositiveChargeRun(batterySoeData);
                    double runLength = positiveChargeRun.EndSoe - positiveChargeRun.StartSoe;

                    if (runLength >= minimumRunLength)
                    {
                        positiveChargeRuns.Add(positiveChargeRun);
                        positiveRunsFound++;
                        if (runLength >= idealRunLength) idealLengthRunsFound++;
                    }
                }
                catch (Exception)
                {
                    // Ignore days with no data
                }
            }

            // Order runs by length (SOE delta) and take the top numberOfRunsForEstimate
            var selectedRuns = positiveChargeRuns
                .OrderByDescending(r => r.EndSoe - r.StartSoe)
                .Take(numberOfRunsForEstimate)
                .ToList();

            var positiveCapacityEstimates = new List<double>();

            foreach (var chargeRun in selectedRuns)
            {
                double energyForRun = Math.Abs(await GetBatteryEnergyForChargeRun(chargeRun)) * chargeLossFactor;
                double capacityEstimate = energyForRun / (Math.Abs(chargeRun.EndSoe - chargeRun.StartSoe) / 100);
                positiveCapacityEstimates.Add(capacityEstimate);
            }

            if (positiveCapacityEstimates.Any())
            {
                return positiveCapacityEstimates.Average();
            }
            else
            {
                throw new InvalidOperationException("No valid charge or discharge runs found to estimate battery capacity");
            }
        }

        private async Task LogRun(string type, ChargeRun run, double runEnergy, double estimate)
        {
            using (var sw = new StreamWriter("c:\\temp\\powerwall_estimates_capped.csv", true))
                await sw.WriteLineAsync($"{type},{run.StartDate},{run.EndDate},{run.StartSoe},{run.EndSoe},{runEnergy},{estimate}");
        }

        private ChargeRun GetLargestPositiveChargeRun(List<ChartDataPoint> soeData)
        {
            const double minSoeThreshold = 5.0;
            const double maxSoeThreshold = 95.0;

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
                    // Store the current run if it's the best so far and meets the end threshold requirement
                    if (currentMaxSoe - currentMinSoe > bestMaxSoe - bestMinSoe && currentMaxSoe >= maxSoeThreshold)
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
            // Store the current run if it's the best so far and meets the end threshold requirement
            if (currentMaxSoe - currentMinSoe > bestMaxSoe - bestMinSoe && currentMaxSoe >= maxSoeThreshold)
            {
                bestMinDate = currentMinDate;
                bestMaxDate = currentMaxDate;
                bestMinSoe = currentMinSoe;
                bestMaxSoe = currentMaxSoe;
            }

            // If no valid run found (doesn't end at 95% or higher), return empty run
            if (bestMaxSoe < maxSoeThreshold)
            {
                return new ChargeRun
                {
                    StartDate = soeData[0].XValue,
                    EndDate = soeData[0].XValue,
                    StartSoe = 0,
                    EndSoe = 0
                };
            }

            // Cap the charge run at the low end (5%) only
            var cappedRun = CapChargeRunToLowSoeRange(soeData, bestMinDate, bestMaxDate, bestMinSoe, bestMaxSoe, minSoeThreshold);
            
            return cappedRun;
        }

        private ChargeRun CapChargeRunToLowSoeRange(List<ChartDataPoint> soeData, DateTime startDate, DateTime endDate, double startSoe, double endSoe, double minSoeThreshold)
        {
            DateTime cappedStartDate = startDate;
            double cappedStartSoe = startSoe;

            // If start SOE is below threshold, find where it crosses the minimum threshold
            if (startSoe < minSoeThreshold)
            {
                var crossingPoint = FindSoeCrossingPoint(soeData, startDate, endDate, minSoeThreshold, true);
                if (crossingPoint != null)
                {
                    cappedStartDate = crossingPoint.XValue;
                    cappedStartSoe = minSoeThreshold;
                }
            }

            return new ChargeRun
            {
                StartDate = cappedStartDate,
                EndDate = endDate,
                StartSoe = cappedStartSoe,
                EndSoe = endSoe
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

        private ChartDataPoint FindSoeCrossingPoint(List<ChartDataPoint> soeData, DateTime runStartDate, DateTime runEndDate, double targetSoe, bool findFirstCrossing)
        {
            var relevantPoints = soeData.Where(p => p.XValue >= runStartDate && p.XValue <= runEndDate).OrderBy(p => p.XValue).ToList();
            
            if (relevantPoints.Count < 2)
                return null;

            if (findFirstCrossing)
            {
                // Find first point where SOE crosses above the target (for minimum threshold)
                for (int i = 1; i < relevantPoints.Count; i++)
                {
                    var prevPoint = relevantPoints[i - 1];
                    var currentPoint = relevantPoints[i];
                    
                    if (prevPoint.YValue < targetSoe && currentPoint.YValue >= targetSoe)
                    {
                        // Interpolate to find exact crossing point
                        return InterpolatePoint(prevPoint, currentPoint, targetSoe);
                    }
                }
            }
            else
            {
                // Find last point where SOE crosses below the target (for maximum threshold)
                for (int i = relevantPoints.Count - 1; i > 0; i--)
                {
                    var prevPoint = relevantPoints[i - 1];
                    var currentPoint = relevantPoints[i];
                    
                    if (prevPoint.YValue <= targetSoe && currentPoint.YValue > targetSoe)
                    {
                        // Interpolate to find exact crossing point
                        return InterpolatePoint(prevPoint, currentPoint, targetSoe);
                    }
                }
            }

            return null;
        }

        private ChartDataPoint InterpolatePoint(ChartDataPoint point1, ChartDataPoint point2, double targetSoe)
        {
            if (Math.Abs(point2.YValue - point1.YValue) < 0.001) // Avoid division by zero
            {
                return new ChartDataPoint(point1.XValue, targetSoe);
            }

            // Linear interpolation
            double ratio = (targetSoe - point1.YValue) / (point2.YValue - point1.YValue);
            long interpolatedTicks = point1.XValue.Ticks + (long)((point2.XValue.Ticks - point1.XValue.Ticks) * ratio);
            DateTime interpolatedTime = new DateTime(interpolatedTicks);

            return new ChartDataPoint(interpolatedTime, targetSoe);
        }
    }
}
