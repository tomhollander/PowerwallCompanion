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
            Assert.AreEqual(1336.2943267822266, power.HomeFromGrid);
            Assert.AreEqual(550, power.HomeFromBattery);
            Assert.AreEqual(44, power.HomeFromSolar);
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
            await api.ExportPowerDataToCsv(stream, new DateTime(2019, 8, 25), new DateTime(2019, 8, 25, 23, 59, 59));
            var sr = new StreamReader(stream);
            stream.Position = 0;
            var line = sr.ReadLine();
            Assert.AreEqual("timestamp,solar_power,battery_power,grid_power,grid_services_power,load_power,battery_soe", line);
            line = sr.ReadLine();
            Assert.AreEqual("2019-08-25 00:00:00,10,0,3609.204488658905,0,3619.204488658905,11", line);
            line = sr.ReadLine();
            Assert.AreEqual("2019-08-25 00:05:00,20,0,3585.4354988098144,0,3605.4354988098144,11", line);
            line = sr.ReadLine();
            Assert.AreEqual("2019-08-25 00:10:00,30,0,3579.784003067017,0,3609.784003067017,17", line);
            Assert.IsTrue(sr.EndOfStream);
        }
    }

}