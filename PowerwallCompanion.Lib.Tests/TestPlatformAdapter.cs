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
        private Dictionary<string, string> _data = new Dictionary<string, string>();

        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string InstallationTimeZone { get; set; }

        public string GetPersistedData(string key)
        {
            return _data.ContainsKey(key) ? _data[key] : null;
        }

        public void PersistData(string key, string value)
        {
            _data[key] = value;
        }

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
