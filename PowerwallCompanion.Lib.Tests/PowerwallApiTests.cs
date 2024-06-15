using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerwallCompanion.Lib.Tests
{
    [TestClass]
    public class PowerwallApiTests
    {
        [TestMethod]
        public async Task CanGetFirstSiteIdFromProductsResponse()
        {
            var mockApiHelper = new MockApiHelper();
            mockApiHelper.SetResponse("/api/1/products",
                @"{ ""response"": [ { ""id"": ""id1"" }, {""id"": ""id2"", ""resource_type"": ""fish"" }, {""id"": ""id3"", ""resource_type"": ""battery"", ""energy_site_id"": 11111, ""site_name"": ""Site One"" }, {""id"": ""id4"", ""resource_type"": ""battery"", ""energy_site_id"": 22222, ""site_name"": ""Site Two"" } ] }");
            var api = new PowerwallApi(null, new TestPlatformAdapter(), mockApiHelper) ;
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

    }
}
