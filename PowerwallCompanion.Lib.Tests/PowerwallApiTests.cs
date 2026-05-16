using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerwallCompanion.Lib.Tests
{
    [TestClass]
    public class PowerwallApiTests
    {
        private string siteInfoDoc = @"{ ""response"": { ""installation_time_zone"": ""Australia/Sydney"" } }";

        [TestMethod]
        public async Task CanGetFirstSiteIdFromProductsResponse()
        {
            var mockApiHelper = new MockApiHelper();
            mockApiHelper.SetResponse("/api/1/products",
                @"{ ""response"": [ { ""id"": ""id1"" }, {""id"": ""id2"", ""resource_type"": ""fish"" }, {""id"": ""id3"", ""resource_type"": ""battery"", ""energy_site_id"": 11111, ""site_name"": ""Site One"" }, {""id"": ""id4"", ""resource_type"": ""battery"", ""energy_site_id"": 22222, ""site_name"": ""Site Two"" } ] }");
            var api = new PowerwallApi(null, new TestPlatformAdapter(), mockApiHelper);
            var siteId = await api.GetFirstSiteId();
            Assert.AreEqual("11111", siteId);
        }

        [TestMethod]
        public async Task CanGetAllEnergySites()
        {
            var mockApiHelper = new MockApiHelper();
            mockApiHelper.SetResponse("/api/1/products",
                @"{ ""response"": [ { ""id"": ""id1"" }, {""id"": ""id2"", ""resource_type"": ""fish"" }, {""id"": ""id3"", ""resource_type"": ""battery"", ""energy_site_id"": 11111, ""site_name"": ""Site One"" }, {""id"": ""id4"", ""resource_type"": ""battery"", ""energy_site_id"": 22222, ""site_name"": ""Site Two"" } ] }");
            var api = new PowerwallApi(null, new TestPlatformAdapter(), mockApiHelper);
            var sites = await api.GetEnergySites();
            Assert.AreEqual(2, sites.Count);
            CollectionAssert.Contains(sites, new KeyValuePair<string, string>("11111", "Site One"));
            CollectionAssert.Contains(sites, new KeyValuePair<string, string>("22222", "Site Two"));
        }

        [TestMethod]
        public async Task CanGetInstantaneousPower()
        {
            var mockApiHelper = new MockApiHelper();
            mockApiHelper.SetResponse("/api/1/energy_sites/11111/live_status",
                @"{
   ""response"":{
      ""solar_power"":44,
      ""energy_left"":0,
      ""percentage_charged"":53,
      ""backup_capable"":true,
      ""battery_power"":550,
      ""load_power"":1930.2943267822266,
      ""grid_status"":""Active"",
      ""grid_services_active"":false,
      ""grid_power"":1336.2943267822266,
      ""grid_services_power"":0,
      ""generator_power"":0,
      ""storm_mode_active"":false,
      ""timestamp"":""2020-07-26T16:37:27+10:00""}}");
            var api = new PowerwallApi("11111", new TestPlatformAdapter(), mockApiHelper);
            var power = await api.GetInstantaneousPower();
            Assert.AreEqual(1930.2943267822266, power.HomePower);
            Assert.AreEqual(550, power.BatteryPower);
            Assert.AreEqual(1336.2943267822266, power.GridPower);
            Assert.AreEqual(44, power.SolarPower);
            Assert.AreEqual(1336.2943267822266, power.GridToHome);
            Assert.AreEqual(550, power.BatteryToHome);
            Assert.AreEqual(44, power.SolarToHome);
            Assert.AreEqual(0, power.SolarToGrid);
            Assert.AreEqual(0, power.SolarToBattery);
            Assert.AreEqual(44, power.SolarToHome);
            Assert.AreEqual(53, power.BatteryStoragePercent);
            Assert.IsTrue(power.GridActive);
        }

        [TestMethod]
        public async Task CanExportDailyPowerData()
        {
            var testPlatformAdapter = new TestPlatformAdapter()
            {
                InstallationTimeZone = "Australia/Sydney"
            };
            var mockApiHelper = new MockApiHelper();
            mockApiHelper.SetResponse("/api/1/energy_sites/11111/site_info", siteInfoDoc);
            mockApiHelper.SetResponse("/api/1/energy_sites/11111/calendar_history?kind=power&period=day&start_date=2019-08-25T00%3A00%3A00.0000000%2B10%3A00&end_date=2019-08-25T23%3A59%3A58.0000000%2B10%3A00&fill_telemetry=0",
            @"{
  ""response"": {
    ""serial_number"": ""1111111-01-F--T17G0000000"",
    ""installation_time_zone"": ""Australia/Sydney"",
    ""time_series"": [
      {
        ""timestamp"": ""2019-08-25T00:00:00+10:00"",
        ""solar_power"": 10,
        ""battery_power"": 0,
        ""grid_power"": 3609.204488658905,
        ""grid_services_power"": 0
      },
      {
        ""timestamp"": ""2019-08-25T00:05:00+10:00"",
        ""solar_power"": 20,
        ""battery_power"": 0,
        ""grid_power"": 3585.4354988098144,
        ""grid_services_power"": 0
      },
      {
        ""timestamp"": ""2019-08-25T00:10:00+10:00"",
        ""solar_power"": 30,
        ""battery_power"": 0,
        ""grid_power"": 3579.784003067017,
        ""grid_services_power"": 0
      }]}}");

            mockApiHelper.SetResponse("/api/1/energy_sites/11111/calendar_history?kind=energy&period=day&start_date=2019-08-25T00%3A00%3A00.0000000%2B10%3A00&end_date=2019-08-25T23%3A59%3A58.0000000%2B10%3A00&fill_telemetry=0",
@"{
  ""response"": {
    ""serial_number"": ""1111111-01-F--T17G0000000"",
    ""installation_time_zone"": ""Australia/Sydney"",
    ""time_series"": [
      {
        ""timestamp"": ""2019-08-25T00:00:00+10:00"",
        ""solar_energy_exported"": 0,
        ""generator_energy_exported"": 0,
        ""grid_energy_imported"": 100,
        ""grid_energy_exported_from_solar"": 0,
        ""grid_energy_exported_from_battery"": 0,
        ""grid_energy_exported_from_generator"": 0,
        ""battery_energy_exported"": 0,
        ""battery_energy_imported_from_grid"": 0,
        ""battery_energy_imported_from_solar"": 0,
        ""battery_energy_imported_from_generator"": 0,
        ""consumer_energy_imported_from_grid"": 100,
        ""consumer_energy_imported_from_solar"": 0,
        ""consumer_energy_imported_from_battery"": 0,
        ""consumer_energy_imported_from_generator"": 0,
        ""total_home_usage"": 100
      },
      {
        ""timestamp"": ""2019-08-25T00:05:00+10:00"",
        ""solar_energy_exported"": 5,
        ""generator_energy_exported"": 0,
        ""grid_energy_imported"": 150,
        ""grid_energy_exported_from_solar"": 0,
        ""grid_energy_exported_from_battery"": 0,
        ""grid_energy_exported_from_generator"": 0,
        ""battery_energy_exported"": 0,
        ""battery_energy_imported_from_grid"": 0,
        ""battery_energy_imported_from_solar"": 5,
        ""battery_energy_imported_from_generator"": 0,
        ""consumer_energy_imported_from_grid"": 145,
        ""consumer_energy_imported_from_solar"": 0,
        ""consumer_energy_imported_from_battery"": 0,
        ""consumer_energy_imported_from_generator"": 0,
        ""total_home_usage"": 145
      },
      {
        ""timestamp"": ""2019-08-25T00:10:00+10:00"",
        ""solar_energy_exported"": 10,
        ""generator_energy_exported"": 0,
        ""grid_energy_imported"": 200,
        ""grid_energy_exported_from_solar"": 0,
        ""grid_energy_exported_from_battery"": 0,
        ""grid_energy_exported_from_generator"": 0,
        ""battery_energy_exported"": 0,
        ""battery_energy_imported_from_grid"": 0,
        ""battery_energy_imported_from_solar"": 10,
        ""battery_energy_imported_from_generator"": 0,
        ""consumer_energy_imported_from_grid"": 190,
        ""consumer_energy_imported_from_solar"": 0,
        ""consumer_energy_imported_from_battery"": 0,
        ""consumer_energy_imported_from_generator"": 0,
        ""total_home_usage"": 190
      }]}}");

            mockApiHelper.SetResponse("/api/1/energy_sites/11111/calendar_history?kind=soe&period=day&start_date=2019-08-25T00%3A00%3A00.0000000%2B10%3A00&end_date=2019-08-25T23%3A59%3A58.0000000%2B10%3A00&fill_telemetry=0",
@"{
  ""response"": {
    ""serial_number"": ""1111111-01-F--T17G0000000"",
    ""installation_time_zone"": ""Australia/Sydney"",
    ""time_series"": [
      {
        ""timestamp"": ""2019-08-25T00:00:00+10:00"",
        ""soe"": 11
      },
      {
        ""timestamp"": ""2019-08-25T00:10:00+10:00"",
        ""soe"": 17
      }]}}");
            var api = new PowerwallApi("11111", testPlatformAdapter, mockApiHelper);
            var stream = new MemoryStream();
            await api.ExportPowerDataToCsv(stream, new DateTime(2019, 8, 25), new DateTime(2019, 8, 25, 23, 59, 59), null);
            var sr = new StreamReader(stream);
            stream.Position = 0;
            var line = sr.ReadLine();
            Assert.AreEqual("timestamp,battery_power,grid_power,grid_services_power,load_power,solar_power,battery_energy_exported,battery_energy_imported_from_generator,battery_energy_imported_from_grid,battery_energy_imported_from_solar,consumer_energy_imported_from_battery,consumer_energy_imported_from_generator,consumer_energy_imported_from_grid,consumer_energy_imported_from_solar,generator_energy_exported,grid_energy_exported_from_battery,grid_energy_exported_from_generator,grid_energy_exported_from_solar,grid_energy_imported,solar_energy_exported,total_battery_charge,total_battery_discharge,total_grid_energy_exported,total_home_usage,total_solar_generation,battery_soe", line);
            line = sr.ReadLine();
            Assert.AreEqual("2019-08-25 00:00:00,0,3609.204488658905,0,3619.204488658905,10,0,0,0,0,0,0,100,0,0,0,0,0,100,0,0,0,0,100,0,11", line);
            line = sr.ReadLine();
            Assert.AreEqual("2019-08-25 00:05:00,0,3585.4354988098144,0,3605.4354988098144,20,0,0,0,5,0,0,145,0,0,0,0,0,150,5,0,0,0,145,0,11", line);
            line = sr.ReadLine();
            Assert.AreEqual("2019-08-25 00:10:00,0,3579.784003067017,0,3609.784003067017,30,0,0,0,10,0,0,190,0,0,0,0,0,200,10,0,0,0,190,0,17", line);
            Assert.IsTrue(sr.EndOfStream);
        }

        [TestMethod]
        public async Task CanGetDetailsFromFromSiteInfo()
        {
            var mockApiHelper = new MockApiHelper();
            mockApiHelper.SetResponse("/api/1/energy_sites/11111/site_info",
                @"
{ ""response"": {
    ""id"": ""--GatewayId--"",
    ""site_name"": ""--SiteName--"",
    ""battery_count"": 11,
    ""installation_date"": ""2017-09-04T16:49:44\u002B10:00"",
    ""backup_reserve_percent"": 22,
    ""components"" : {
        ""batteries"" : [
            { ""part_number"": ""XYZ123"" },
            { ""part_number"": ""ABC9000"" }
        ]
    }
}}");
            var api = new PowerwallApi("11111", new TestPlatformAdapter(), mockApiHelper);
            var energySiteInfo = await api.GetEnergySiteInfo();
            Assert.AreEqual("--GatewayId--", energySiteInfo.GatewayId);
            Assert.AreEqual("--SiteName--", energySiteInfo.SiteName);
            Assert.AreEqual(11, energySiteInfo.NumberOfBatteries);
            Assert.AreEqual(22, energySiteInfo.ReservePercent);
            Assert.AreEqual(new DateTime(2017, 9, 4, 16, 49, 44), energySiteInfo.InstallDate);
            Assert.AreEqual("XYZ123", energySiteInfo.PowerwallPartNumber);

        }

        [TestMethod]
        public async Task ExportPowerDataToCsv_TruncatesExistingFileContent()
        {
            var testPlatformAdapter = new TestPlatformAdapter()
            {
                InstallationTimeZone = "Australia/Sydney"
            };
            var mockApiHelper = new MockApiHelper();
            mockApiHelper.SetResponse("/api/1/energy_sites/11111/calendar_history?kind=power&period=day&start_date=2019-08-25T00%3A00%3A00.0000000%2B10%3A00&end_date=2019-08-25T23%3A59%3A58.0000000%2B10%3A00&fill_telemetry=0",
            @"{
  ""response"": {
    ""time_series"": [
      {
        ""timestamp"": ""2019-08-25T00:00:00+10:00"",
        ""solar_power"": 10,
        ""battery_power"": 0,
        ""grid_power"": 100,
        ""grid_services_power"": 0
      }]}}");

            mockApiHelper.SetResponse("/api/1/energy_sites/11111/calendar_history?kind=energy&period=day&start_date=2019-08-25T00%3A00%3A00.0000000%2B10%3A00&end_date=2019-08-25T23%3A59%3A58.0000000%2B10%3A00&fill_telemetry=0",
            @"{
  ""response"": {
    ""time_series"": [
      {
        ""timestamp"": ""2019-08-25T00:00:00+10:00"",
        ""solar_energy_exported"": 0,
        ""grid_energy_imported"": 10,
        ""grid_energy_exported_from_solar"": 0,
        ""grid_energy_exported_from_battery"": 0,
        ""grid_energy_exported_from_generator"": 0,
        ""battery_energy_exported"": 0,
        ""battery_energy_imported_from_grid"": 0,
        ""battery_energy_imported_from_solar"": 0,
        ""battery_energy_imported_from_generator"": 0,
        ""consumer_energy_imported_from_grid"": 10,
        ""consumer_energy_imported_from_solar"": 0,
        ""consumer_energy_imported_from_battery"": 0,
        ""consumer_energy_imported_from_generator"": 0,
        ""total_home_usage"": 10
      }]}}");

            mockApiHelper.SetResponse("/api/1/energy_sites/11111/calendar_history?kind=soe&period=day&start_date=2019-08-25T00%3A00%3A00.0000000%2B10%3A00&end_date=2019-08-25T23%3A59%3A58.0000000%2B10%3A00&fill_telemetry=0",
            @"{
  ""response"": {
    ""time_series"": [
      {
        ""timestamp"": ""2019-08-25T00:00:00+10:00"",
        ""soe"": 50
      }]}}");

            var api = new PowerwallApi("11111", testPlatformAdapter, mockApiHelper);

            // Pre-populate stream with longer content
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            await writer.WriteLineAsync("This is old data that should be truncated");
            await writer.WriteLineAsync("Second line of old data");
            await writer.WriteLineAsync("Third line of old data");
            await writer.FlushAsync();
            stream.Position = 0;

            await api.ExportPowerDataToCsv(stream, new DateTime(2019, 8, 25), new DateTime(2019, 8, 25, 23, 59, 59), null);

            stream.Position = 0;
            var sr = new StreamReader(stream);
            var allContent = sr.ReadToEnd();

            // Verify no old content remains
            Assert.IsFalse(allContent.Contains("old data"), "Old data should be truncated");

            // Verify new content is correct
            stream.Position = 0;
            sr = new StreamReader(stream);
            var line = sr.ReadLine();
            Assert.IsTrue(line.StartsWith("timestamp,"));
            line = sr.ReadLine();
            Assert.IsTrue(line.StartsWith("2019-08-25 00:00:00,"));
            Assert.IsTrue(sr.EndOfStream, "Should only have header and one data row");
        }

        [TestMethod]
        public async Task ExportPowerDataToCsv_HandlesNullEnergyEntry()
        {
            var testPlatformAdapter = new TestPlatformAdapter()
            {
                InstallationTimeZone = "Australia/Sydney"
            };
            var mockApiHelper = new MockApiHelper();
            mockApiHelper.SetResponse("/api/1/energy_sites/11111/calendar_history?kind=power&period=day&start_date=2019-08-25T00%3A00%3A00.0000000%2B10%3A00&end_date=2019-08-25T23%3A59%3A58.0000000%2B10%3A00&fill_telemetry=0",
            @"{
  ""response"": {
    ""time_series"": [
      {
        ""timestamp"": ""2019-08-25T00:00:00+10:00"",
        ""solar_power"": 10,
        ""battery_power"": 0,
        ""grid_power"": 100,
        ""grid_services_power"": 0
      },
      {
        ""timestamp"": ""2019-08-25T00:10:00+10:00"",
        ""solar_power"": 20,
        ""battery_power"": 0,
        ""grid_power"": 200,
        ""grid_services_power"": 0
      }]}}");

            mockApiHelper.SetResponse("/api/1/energy_sites/11111/calendar_history?kind=energy&period=day&start_date=2019-08-25T00%3A00%3A00.0000000%2B10%3A00&end_date=2019-08-25T23%3A59%3A58.0000000%2B10%3A00&fill_telemetry=0",
            @"{
  ""response"": {
    ""time_series"": [
      {
        ""timestamp"": ""2019-08-25T00:00:00+10:00"",
        ""solar_energy_exported"": 0,
        ""grid_energy_imported"": 10,
        ""grid_energy_exported_from_solar"": 0,
        ""grid_energy_exported_from_battery"": 0,
        ""grid_energy_exported_from_generator"": 0,
        ""battery_energy_exported"": 0,
        ""battery_energy_imported_from_grid"": 0,
        ""battery_energy_imported_from_solar"": 0,
        ""battery_energy_imported_from_generator"": 0,
        ""consumer_energy_imported_from_grid"": 10,
        ""consumer_energy_imported_from_solar"": 0,
        ""consumer_energy_imported_from_battery"": 0,
        ""consumer_energy_imported_from_generator"": 0,
        ""total_home_usage"": 10
      }]}}");

            mockApiHelper.SetResponse("/api/1/energy_sites/11111/calendar_history?kind=soe&period=day&start_date=2019-08-25T00%3A00%3A00.0000000%2B10%3A00&end_date=2019-08-25T23%3A59%3A58.0000000%2B10%3A00&fill_telemetry=0",
            @"{""response"": {""time_series"": []}}");

            var api = new PowerwallApi("11111", testPlatformAdapter, mockApiHelper);
            var stream = new MemoryStream();
            await api.ExportPowerDataToCsv(stream, new DateTime(2019, 8, 25), new DateTime(2019, 8, 25, 23, 59, 59), null);

            stream.Position = 0;
            var sr = new StreamReader(stream);
            var header = sr.ReadLine();
            var line1 = sr.ReadLine();
            var line2 = sr.ReadLine();

            // Both rows should have same number of columns as header
            var headerColCount = header.Split(',').Length;
            var line1ColCount = line1.Split(',').Length;
            var line2ColCount = line2.Split(',').Length;

            Assert.AreEqual(headerColCount, line1ColCount, "First row should have same column count as header");
            Assert.AreEqual(headerColCount, line2ColCount, "Second row should have same column count as header (uses LastOrDefault)");
        }
    }

}

