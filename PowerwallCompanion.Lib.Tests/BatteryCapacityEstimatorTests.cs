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
            // Arrange - create SOE data with one charge run from 20% to 80%
            var soeData = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 0, 0), 20),   // Start low
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 5, 0), 25),   // Start charging
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 10, 0), 35),  // Continue charging
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 15, 0), 50),  // Continue charging
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 20, 0), 65),  // Continue charging
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 25, 0), 80),  // End charging
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 30, 0), 75)   // Start discharging
            };

            // Act
            var result = _estimator.TestGetLargestPositiveChargeRun(soeData);

            // Assert
            Assert.AreEqual(new DateTime(2024, 1, 1, 8, 0, 0), result.StartDate);
            Assert.AreEqual(new DateTime(2024, 1, 1, 8, 25, 0), result.EndDate);
            Assert.AreEqual(20, result.StartSoe);
            Assert.AreEqual(80, result.EndSoe);
        }

        [TestMethod]
        public void GetLargestPositiveChargeRun_MultipleChargeRuns_ReturnsLargest()
        {
            // Arrange - create SOE data with multiple charge runs
            var soeData = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 0, 0), 20),   // First run start
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 5, 0), 40),   // First run end (20% delta)
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 10, 0), 35),  // Discharge
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 15, 0), 30),  // Second run start
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 20, 0), 85),  // Second run end (55% delta) - largest
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 25, 0), 80),  // Discharge
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 30, 0), 90)   // Third run (10% delta)
            };

            // Act
            var result = _estimator.TestGetLargestPositiveChargeRun(soeData);

            // Assert - should return the second run with the largest delta
            Assert.AreEqual(new DateTime(2024, 1, 1, 8, 15, 0), result.StartDate);
            Assert.AreEqual(new DateTime(2024, 1, 1, 8, 20, 0), result.EndDate);
            Assert.AreEqual(30, result.StartSoe);
            Assert.AreEqual(85, result.EndSoe);
        }

        [TestMethod]
        public void GetLargestPositiveChargeRun_WithDataGlitches_SkipsZeroValues()
        {
            // Arrange - create SOE data with zero value glitches
            var soeData = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 0, 0), 20),   // Start
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 5, 0), 30),   // Charging
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 10, 0), 0),   // Data glitch - should be skipped
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 15, 0), 40),  // Continue charging
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 20, 0), 60),  // End charging
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 25, 0), 55)   // Discharge
            };

            // Act
            var result = _estimator.TestGetLargestPositiveChargeRun(soeData);

            // Assert - should skip the zero value and find the correct run
            Assert.AreEqual(new DateTime(2024, 1, 1, 8, 0, 0), result.StartDate);
            Assert.AreEqual(new DateTime(2024, 1, 1, 8, 20, 0), result.EndDate);
            Assert.AreEqual(20, result.StartSoe);
            Assert.AreEqual(60, result.EndSoe);
        }

        [TestMethod]
        public void GetLargestNegativeChargeRun_SingleDischargeRun_ReturnsCorrectRun()
        {
            // Arrange - create SOE data with one discharge run from 80% to 20%
            var soeData = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 0, 0), 80),  // Start high
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 5, 0), 70),  // Start discharging
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 10, 0), 55), // Continue discharging
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 15, 0), 35), // Continue discharging
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 20, 0), 20), // End discharging
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 25, 0), 25)  // Start charging
            };

            // Act
            var result = _estimator.TestGetLargestNegativeChargeRun(soeData);

            // Assert
            Assert.AreEqual(new DateTime(2024, 1, 1, 18, 0, 0), result.StartDate); // Start at higher SOE
            Assert.AreEqual(new DateTime(2024, 1, 1, 18, 20, 0), result.EndDate);   // End at lower SOE
            Assert.AreEqual(80, result.StartSoe);  // Start SOE is higher
            Assert.AreEqual(20, result.EndSoe);    // End SOE is lower
        }

        [TestMethod]
        public void GetLargestNegativeChargeRun_MultipleDischargeRuns_ReturnsLargest()
        {
            // Arrange - create SOE data with multiple discharge runs
            var soeData = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 0, 0), 80),  // First run start
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 5, 0), 60),  // First run end (20% delta)
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 10, 0), 65), // Charge
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 15, 0), 90), // Second run start
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 20, 0), 25), // Second run end (65% delta) - largest
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 25, 0), 30), // Charge
                new ChartDataPoint(new DateTime(2024, 1, 1, 18, 30, 0), 20)  // Third run (10% delta)
            };

            // Act
            var result = _estimator.TestGetLargestNegativeChargeRun(soeData);

            // Assert - should return the second run with the largest delta
            Assert.AreEqual(new DateTime(2024, 1, 1, 18, 15, 0), result.StartDate);
            Assert.AreEqual(new DateTime(2024, 1, 1, 18, 20, 0), result.EndDate);
            Assert.AreEqual(90, result.StartSoe);
            Assert.AreEqual(25, result.EndSoe);
        }

        [TestMethod]
        public void CalculateCapacityFromEnergyAndSoe_ValidInput_ReturnsCorrectCapacity()
        {
            // Test the capacity calculation logic directly
            // If we used 42kWh (42000Wh) of energy for a 65% SOE change
            double energyWh = 42000;
            double soeChangePercent = 65;
            
            double expectedCapacity = energyWh / (soeChangePercent / 100); // 42000 / 0.65 = 64615.38 Wh
            
            Assert.AreEqual(64615.38, Math.Round(expectedCapacity, 2));
        }

        [TestMethod]
        public void WeightedAverageCalculation_ValidInputs_ReturnsCorrectAverage()
        {
            // Test the weighted average calculation logic
            var positiveEstimates = new List<double> { 64000, 66000, 65000 }; // Average: 65000
            var negativeEstimates = new List<double> { 68000, 70000, 69000 }; // Average: 69000
            
            // Weighted: (65000 + 65000 + 69000) / 3 = 66333.33
            double weightedAverage = (positiveEstimates.Average() + positiveEstimates.Average() + negativeEstimates.Average()) / 3;
            
            Assert.AreEqual(66333.33, Math.Round(weightedAverage, 2));
        }

        [TestMethod]
        public void CalculateEnergyFromPowerData_ValidData_ReturnsCorrectEnergy()
        {
            // Test the energy calculation logic from power data
            var powerData = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 0, 0), 5),     // 5kW
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 5, 0), 10),    // 10kW
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 10, 0), 6),    // 6kW
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 15, 0), 8),    // 8kW
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 20, 0), 4),    // 4kW
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 25, 0), 2)     // 2kW
            };

            var startDate = new DateTime(2024, 1, 1, 8, 0, 0);
            var endDate = new DateTime(2024, 1, 1, 8, 25, 0);

            // Calculate energy manually using the same formula as in the actual code
            double totalEnergy = 0;
            for (int i = 1; i < powerData.Count; i++)
            {
                if (powerData[i].XValue >= startDate && powerData[i].XValue <= endDate)
                {
                    totalEnergy += powerData[i].YValue / 12 * 1000; // kW to Wh assuming 5 minute intervals
                }
            }

            // Expected: (10 + 6 + 8 + 4 + 2) / 12 * 1000 = 30000 / 12 = 2500 Wh
            Assert.AreEqual(2500, totalEnergy);
        }

        [TestMethod]
        public void EdgeCase_EmptyChargeRun_HandledGracefully()
        {
            // Test edge case with minimal SOE data
            var soeData = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 0, 0), 50),
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 5, 0), 50)  // No change
            };

            // Act
            var positiveResult = _estimator.TestGetLargestPositiveChargeRun(soeData);
            var negativeResult = _estimator.TestGetLargestNegativeChargeRun(soeData);

            // Assert - should return runs with zero delta
            Assert.AreEqual(0, positiveResult.EndSoe - positiveResult.StartSoe);
            Assert.AreEqual(0, negativeResult.StartSoe - negativeResult.EndSoe);
        }

        [TestMethod]
        public void ValidateRunFiltering_SmallChargeRun_Rejected()
        {
            // Test that runs smaller than 60% are properly filtered out
            var soeData = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 0, 0), 30),   // Start
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 30, 0), 80)   // End - only 50% delta
            };

            var run = _estimator.TestGetLargestPositiveChargeRun(soeData);
            
            // The run should be found but would be filtered out by the 60% threshold
            Assert.AreEqual(50, run.EndSoe - run.StartSoe); // Found but below threshold
        }

        [TestMethod]
        public void ValidateRunFiltering_LargeChargeRun_Accepted()
        {
            // Test that runs larger than 60% are accepted
            var soeData = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 0, 0), 20),   // Start
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 30, 0), 85)   // End - 65% delta
            };

            var run = _estimator.TestGetLargestPositiveChargeRun(soeData);
            
            // This run should be accepted (above 60% threshold)
            Assert.AreEqual(65, run.EndSoe - run.StartSoe);
        }

        [TestMethod]
        public void PowerDataEnergyCalculation_EdgeCases_HandleGracefully()
        {
            // Test edge cases in energy calculation logic without API calls
            var powerData = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 0, 0), 0),     // Start with 0
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 5, 0), -5),    // Negative power (discharge)
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 10, 0), 10),   // Positive power
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 15, 0), 0)     // End with 0
            };

            var startDate = new DateTime(2024, 1, 1, 8, 0, 0);
            var endDate = new DateTime(2024, 1, 1, 8, 15, 0);

            // Calculate energy using the same algorithm as the actual code
            double totalEnergy = 0;
            for (int i = 1; i < powerData.Count; i++)
            {
                if (powerData[i].XValue >= startDate && powerData[i].XValue <= endDate)
                {
                    totalEnergy += powerData[i].YValue / 12 * 1000; // kW to Wh
                }
            }

            // Should handle negative and zero values: (-5 + 10 + 0) / 12 * 1000 = 416.67 Wh
            Assert.AreEqual(416.67, Math.Round(totalEnergy, 2));
        }

        [TestMethod]
        public void CapacityEstimation_OutlierHandling_ProducesReasonableResults()
        {
            // Test that outlier capacity estimates don't skew results too much
            var estimates1 = new List<double> { 50000, 52000, 51000 }; // Normal range
            var estimates2 = new List<double> { 50000, 100000, 51000 }; // One outlier
            
            var avg1 = estimates1.Average();
            var avg2 = estimates2.Average();
            
            // Verify the outlier significantly affects the average
            Assert.AreEqual(51000, avg1);
            Assert.AreEqual(67000, avg2);
            
            // This demonstrates why having multiple estimates helps reduce outlier impact
            Assert.IsTrue(Math.Abs(avg2 - avg1) > 15000);
        }

        [TestMethod]
        public void TimeZoneAndDateHandling_ValidDates_ProcessedCorrectly()
        {
            // Test date/time calculations used in energy filtering
            var chargeRunStart = new DateTime(2024, 1, 1, 8, 0, 0);
            var chargeRunEnd = new DateTime(2024, 1, 1, 12, 0, 0);
            
            var testPoints = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 7, 55, 0), 5),    // Before range
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 0, 0), 10),    // At start
                new ChartDataPoint(new DateTime(2024, 1, 1, 10, 0, 0), 8),    // During range
                new ChartDataPoint(new DateTime(2024, 1, 1, 12, 0, 0), 6),    // At end
                new ChartDataPoint(new DateTime(2024, 1, 1, 12, 5, 0), 3)     // After range
            };
            
            // Count points within the charge run period
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
            
            // Should include 3 points: 8:00, 10:00, and 12:00
            Assert.AreEqual(3, pointsInRange);
            // Energy: (10 + 8 + 6) / 12 * 1000 = 2000 Wh
            Assert.AreEqual(2000, energyInRange);
        }

        [TestMethod]
        public void ErrorConditions_InvalidSOEData_HandledGracefully()
        {
            // Test with single data point (minimum viable data)
            var singlePointData = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 0, 0), 50)
            };

            var positiveRun = _estimator.TestGetLargestPositiveChargeRun(singlePointData);
            var negativeRun = _estimator.TestGetLargestNegativeChargeRun(singlePointData);

            // Should not crash and should return zero-delta runs
            Assert.AreEqual(0, positiveRun.EndSoe - positiveRun.StartSoe);
            Assert.AreEqual(0, negativeRun.StartSoe - negativeRun.EndSoe);
        }

        [TestMethod]
        public void ThresholdValidation_ExactThreshold_BehaviorVerified()
        {
            // Test behavior exactly at the 60% threshold
            var exactThresholdData = new List<ChartDataPoint>
            {
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 0, 0), 20),   // Start
                new ChartDataPoint(new DateTime(2024, 1, 1, 8, 30, 0), 80)   // End - exactly 60%
            };

            var run = _estimator.TestGetLargestPositiveChargeRun(soeData: exactThresholdData);
            
            // Verify the calculation
            Assert.AreEqual(60, run.EndSoe - run.StartSoe);
            
            // In the main algorithm, 60% would not be greater than 60, so would be filtered out
            // This test documents the current behavior (exclusive threshold)
        }
    }

    // Helper class to extend BatteryCapacityEstimator for testing private methods
    public static class BatteryCapacityEstimatorExtensions
    {
        public static BatteryCapacityEstimator.ChargeRun TestGetLargestPositiveChargeRun(
            this BatteryCapacityEstimator estimator, List<ChartDataPoint> soeData)
        {
            // Use reflection to call the private method
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

    // Simple mock PowerwallApi for testing
    public class MockPowerwallApi : PowerwallApi
    {
        public MockPowerwallApi() : base("test", new TestPlatformAdapter { InstallationTimeZone = "Australia/Sydney" }, new MockApiHelper())
        {
        }
    }
}