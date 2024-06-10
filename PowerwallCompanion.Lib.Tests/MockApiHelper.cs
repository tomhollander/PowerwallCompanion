using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PowerwallCompanion.Lib.Tests
{
    internal class MockApiHelper : IApiHelper
    {
        private Dictionary<string, string> responses = new Dictionary<string, string>();


        public void SetResponse(string url, string response)
        {
            responses[url] = response;
        }
        public async Task<JsonObject> CallGetApiWithTokenRefresh(string url)
        {
            if (responses.ContainsKey(url)) {
                return (JsonObject)JsonNode.Parse(responses[url]);
            }
            throw new HttpRequestException("Bad things happened");

        }
    }
}
