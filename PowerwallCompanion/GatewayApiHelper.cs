using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PowerwallCompanion
{
    internal class GatewayApiHelper
    {
        public async static Task<JsonObject> CallGetApi(string uriPath)
        {
            if (Settings.LocalGatewayIP == null || Settings.LocalGatewayPassword == null)
            {
                throw new InvalidOperationException("Gateway details not configured");
            }

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            var client = new HttpClient(handler);
            var payload = $"{{\"username\":\"customer\",\"password\":\"{Settings.LocalGatewayPassword}\", \"email\":\"me@example.com\",\"clientInfo\":{{\"timezone\":\"Australia/Sydney\"}}}}";
            var content = new StringContent(payload, new UTF8Encoding(), "application/json");
            var response = await client.PostAsync($"https://{Settings.LocalGatewayIP}/api/login/Basic", content);
            var cookies = response.Headers.SingleOrDefault(header => header.Key == "Set-Cookie").Value;

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://{Settings.LocalGatewayIP}{uriPath}");
            foreach (var cookie in cookies)
            {
                request.Headers.Add("Cookie", cookie);
            }
            response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            return (JsonObject) JsonObject.Parse(responseBody);
        }
    }
}
