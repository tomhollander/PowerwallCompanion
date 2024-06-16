using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PowerwallCompanion.Lib.Tests
{
    internal class TestPlatformAdapter : IPlatformAdapter
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }

        public Task<string> ReadFileContents(string filename)
        {
            throw new NotImplementedException();
        }
        public Task<JsonObject> ReadGatewayDetailsFromCache()
        {
            throw new NotImplementedException();
        }

        public Task SaveGatewayDetailsToCache(JsonObject json)
        {
            throw new NotImplementedException();
        }
    }
}
