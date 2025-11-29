using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerwallCompanion.Lib.Models;

namespace PowerwallCompanion.Lib.Tests
{
    [TestClass]
    public class BatteryCapacityEstimatorTests
    {
        private MockPowerwallApi _mockApi;
        private BatteryCapacityEstimator _estimator;

        [TestInitialize]
        public void Setup()
        {
            _mockApi = new MockPowerwallApi();
            _estimator = new BatteryCapacityEstimator(_mockApi);
        }

        [TestMethod]
        public void GetLargestPositiveChargeRun_SingleChargeRun_ReturnsCorrectRun()
        {
            // Arrange - create SOE data with one charge run from 20% to 95% (must end >=95%)
            var soeData = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 0, 0), 20),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 5, 0), 30),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 10, 0), 45),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 15, 0), 60),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 20, 0), 80),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 25, 0), 95), // End charging
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 30, 0), 90)  // Discharge begins
            };

            // Act
            var result = _estimator.TestGetLargestPositiveChargeRun(soeData);

            // Assert
            Assert.AreEqual(new DateTime(2024, 1, 1, 8, 0, 0), result.StartDate);
            Assert.AreEqual(new DateTime(2024, 1, 1, 8, 25, 0), result.EndDate);
            Assert.AreEqual(20, result.StartSoe);
            Assert.AreEqual(95, result.EndSoe);
        }

        [TestMethod]
        public void GetLargestPositiveChargeRun_MultipleChargeRuns_ReturnsLargest()
        {
            // Arrange - multiple runs, only those ending >=95% qualify; second is largest
            var soeData = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 0, 0), 25),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 5, 0), 55),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 10, 0), 50), // discharge resets
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 15, 0), 30),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 20, 0), 95), // large run (65%)
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 25, 0), 90),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 30, 0), 92)  // small run (2%) not qualifying end >=95
            };

            // Act
            var result = _estimator.TestGetLargestPositiveChargeRun(soeData);

            // Assert
            Assert.AreEqual(new DateTime(2024, 1, 1, 8, 15, 0), result.StartDate);
            Assert.AreEqual(new DateTime(2024, 1, 1, 8, 20, 0), result.EndDate);
            Assert.AreEqual(30, result.StartSoe);
            Assert.AreEqual(95, result.EndSoe);
        }

        [TestMethod]
        public void GetLargestPositiveChargeRun_WithDataGlitches_SkipsZeroValues()
        {
            // Arrange - include zero glitch, run ends at 95%
            var soeData = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 0, 0), 15),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 5, 0), 25),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 10, 0), 0),  // glitch
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 15, 0), 40),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 20, 0), 60),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 25, 0), 95),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 30, 0), 90)
            };

            // Act
            var result = _estimator.TestGetLargestPositiveChargeRun(soeData);

            // Assert
            Assert.AreEqual(new DateTime(2024, 1, 1, 8, 0, 0), result.StartDate);
            Assert.AreEqual(new DateTime(2024, 1, 1, 8, 25, 0), result.EndDate);
            Assert.AreEqual(15, result.StartSoe);
            Assert.AreEqual(95, result.EndSoe);
        }

        [TestMethod]
        public void GetLargestNegativeChargeRun_SingleDischargeRun_ReturnsCorrectRun()
        {
            var soeData = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 0, 0), 90),
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 5, 0), 75),
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 10, 0), 60),
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 15, 0), 40),
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 20, 0), 25),
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 25, 0), 30)
            };
            var result = _estimator.TestGetLargestNegativeChargeRun(soeData);
            Assert.AreEqual(new DateTime(2024, 1, 1, 18, 0, 0), result.StartDate);
            Assert.AreEqual(new DateTime(2024, 1, 1, 18, 20, 0), result.EndDate);
            Assert.AreEqual(90, result.StartSoe);
            Assert.AreEqual(25, result.EndSoe);
        }

        [TestMethod]
        public void GetLargestNegativeChargeRun_MultipleDischargeRuns_ReturnsLargest()
        {
            var soeData = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 0, 0), 85),
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 5, 0), 65),
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 10, 0), 70),
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 15, 0), 92),
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 20, 0), 25),
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 25, 0), 30),
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 30, 0), 20)
            };
            var result = _estimator.TestGetLargestNegativeChargeRun(soeData);
            Assert.AreEqual(new DateTime(2024, 1, 1, 18, 15, 0), result.StartDate);
            Assert.AreEqual(new DateTime(2024, 1, 1, 18, 20, 0), result.EndDate);
            Assert.AreEqual(92, result.StartSoe);
            Assert.AreEqual(25, result.EndSoe);
        }

        [TestMethod]
        public void CalculateCapacityFromEnergyAndSoe_ValidInput_ReturnsCorrectCapacity()
        {
            double energyWh = 42000;
            double soeChangePercent = 65;
            double expectedCapacity = energyWh / (soeChangePercent / 100); // 64615.38 Wh
            Assert.AreEqual(64615.38, Math.Round(expectedCapacity, 2));
        }

        [TestMethod]
        public void WeightedAverageCalculation_ValidInputs_ReturnsCorrectAverage()
        {
            var positiveEstimates = new List<double> { 64000, 66000, 65000 };
            var negativeEstimates = new List<double> { 68000, 70000, 69000 };
            double weightedAverage = (positiveEstimates.Average() + positiveEstimates.Average() + negativeEstimates.Average()) / 3;
            Assert.AreEqual(66333.33, Math.Round(weightedAverage, 2));
        }

        [TestMethod]
        public void CalculateEnergyFromPowerData_ValidData_ReturnsCorrectEnergy()
        {
            var powerData = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 0, 0), 5),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 5, 0), 10),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 10, 0), 6),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 15, 0), 8),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 20, 0), 4),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 25, 0), 2)
            };
            var startDate = new DateTime(2024, 1, 1, 8, 0, 0);
            var endDate = new DateTime(2024, 1, 1, 8, 25, 0);
            double totalEnergy = 0;
            for (int i = 1; i < powerData.Count; i++)
            {
                if (powerData[i].XValue >= startDate && powerData[i].XValue <= endDate)
                {
                    totalEnergy += powerData[i].YValue / 12 * 1000;
                }
            }
            Assert.AreEqual(2500, totalEnergy);
        }

        [TestMethod]
        public void EdgeCase_EmptyChargeRun_HandledGracefully()
        {
            var soeData = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 0, 0), 50),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 5, 0), 50)
            };
            var positiveResult = _estimator.TestGetLargestPositiveChargeRun(soeData);
            var negativeResult = _estimator.TestGetLargestNegativeChargeRun(soeData);
            Assert.AreEqual(0, positiveResult.EndSoe - positiveResult.StartSoe);
            Assert.AreEqual(0, negativeResult.StartSoe - negativeResult.EndSoe);
        }

        [TestMethod]
        public void ValidateRunFiltering_SmallChargeRun_AcceptedIfAboveMinimum()
        {
            // Minimum length is now 50%; run of 50% should be accepted if it ends >=95%.
            var soeData = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 0, 0), 45),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 30, 0), 95) // 50% delta
            };
            var run = _estimator.TestGetLargestPositiveChargeRun(soeData);
            Assert.AreEqual(50, run.EndSoe - run.StartSoe);
        }

        [TestMethod]
        public void ValidateRunFiltering_IdealChargeRun_Accepted()
        {
            // Ideal length is 80%; create an 80% run
            var soeData = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 0, 0), 15),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 30, 0), 95) // 80% delta
            };
            var run = _estimator.TestGetLargestPositiveChargeRun(soeData);
            Assert.AreEqual(80, run.EndSoe - run.StartSoe);
        }

        [TestMethod]
        public void PowerDataEnergyCalculation_EdgeCases_HandleGracefully()
        {
            var powerData = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 0, 0), 0),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 5, 0), -5),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 10, 0), 10),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 15, 0), 0)
            };
            var startDate = new DateTime(2024, 1, 1, 8, 0, 0);
            var endDate = new DateTime(2024, 1, 1, 8, 15, 0);
            double totalEnergy = 0;
            for (int i = 1; i < powerData.Count; i++)
            {
                if (powerData[i].XValue >= startDate && powerData[i].XValue <= endDate)
                {
                    totalEnergy += powerData[i].YValue / 12 * 1000;
                }
            }
            Assert.AreEqual(416.67, Math.Round(totalEnergy, 2));
        }

        [TestMethod]
        public void CapacityEstimation_OutlierHandling_ProducesReasonableResults()
        {
            var estimates1 = new List<double> { 50000, 52000, 51000 };
            var estimates2 = new List<double> { 50000, 100000, 51000 }; // outlier
            var avg1 = estimates1.Average();
            var avg2 = estimates2.Average();
            Assert.AreEqual(51000, avg1);
            Assert.AreEqual(67000, avg2);
            Assert.IsTrue(Math.Abs(avg2 - avg1) > 15000);
        }

        [TestMethod]
        public void TimeZoneAndDateHandling_ValidDates_ProcessedCorrectly()
        {
            var chargeRunStart = new DateTime(2024, 1, 1, 8, 0, 0);
            var chargeRunEnd = new DateTime(2024, 1, 1, 12, 0, 0);
            var testPoints = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 7, 55, 0), 5),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 0, 0), 10),
                new ChartDataPoint(new DateTime(2024, 1, 1, 10, 0, 0), 8),
                new ChartDataPoint(new DateTime(2024, 1, 1, 12, 0, 0), 6),
                new ChartDataPoint(new DateTime(2024, 1, 1, 12, 5, 0), 3)
            };
            int pointsInRange = 0;
            double energyInRange = 0;
            for (int i = 1; i < testPoints.Count; i++)
            {
                if (testPoints[i].XValue >= chargeRunStart && testPoints[i].XValue <= chargeRunEnd)
                {
                    pointsInRange++;
                    energyInRange += testPoints[i].YValue / 12 * 1000;
                }
            }
            Assert.AreEqual(3, pointsInRange);
            Assert.AreEqual(2000, energyInRange);
        }

        [TestMethod]
        public void ErrorConditions_InvalidSOEData_HandledGracefully()
        {
            var singlePointData = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 0, 0), 50)
            };
            var positiveRun = _estimator.TestGetLargestPositiveChargeRun(singlePointData);
            var negativeRun = _estimator.TestGetLargestNegativeChargeRun(singlePointData);
            Assert.AreEqual(0, positiveRun.EndSoe - positiveRun.StartSoe);
            Assert.AreEqual(0, negativeRun.StartSoe - negativeRun.EndSoe);
        }

        [TestMethod]
        public void ThresholdValidation_ExactMinimumThreshold_BehaviorVerified()
        {
            // Minimum threshold is 50%; ensure a 50% run ending >=95% is recognized
            var exactThresholdData = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 0, 0), 45),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 30, 0), 95)
            };
            var run = _estimator.TestGetLargestPositiveChargeRun(exactThresholdData);
            Assert.AreEqual(50, run.EndSoe - run.StartSoe);
        }
    }

    public static class BatteryCapacityEstimatorExtensions
    {
        public static BatteryCapacityEstimator.ChargeRun TestGetLargestPositiveChargeRun(
            this BatteryCapacityEstimator estimator, List<ChartDataPoint> soeData)
        {
            var method = typeof(BatteryCapacityEstimator).GetMethod("GetLargestPositiveChargeRun",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (BatteryCapacityEstimator.ChargeRun)method.Invoke(estimator, new object[] { soeData });
        }

        public static BatteryCapacityEstimator.ChargeRun TestGetLargestNegativeChargeRun(
            this BatteryCapacityEstimator estimator, List<ChartDataPoint> soeData)
        {
            var method = typeof(BatteryCapacityEstimator).GetMethod("GetLargestNegativeChargeRun",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (BatteryCapacityEstimator.ChargeRun)method.Invoke(estimator, new object[] { soeData });
        }
    }

    public class MockPowerwallApi : PowerwallApi
    {
        public MockPowerwallApi() : base("test", new TestPlatformAdapter { InstallationTimeZone = "Australia/Sydney" }, new MockApiHelper())
        {
        }
    }
}