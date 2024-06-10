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

    }
}
