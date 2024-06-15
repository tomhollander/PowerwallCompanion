using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PowerwallCompanion.Lib
{
    internal class GatewayApiHelper
    {
        public async Task<JsonObject> CallGetApi(string localGatwayIP, string localGatewayPassword, string uriPath)
        {
            if (String.IsNullOrEmpty(localGatwayIP) || String.IsNullOrEmpty(localGatewayPassword))
            {
                throw new InvalidOperationException("Gateway details not configured");
            }

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            var client = new HttpClient(handler);
            var payload = $"{{\"username\":\"customer\",\"password\":\"{localGatewayPassword}\", \"email\":\"me@example.com\",\"clientInfo\":{{\"timezone\":\"Australia/Sydney\"}}}}";
            var content = new StringContent(payload, new UTF8Encoding(), "application/json");
            var response = await client.PostAsync($"https://{localGatwayIP.Trim()}/api/login/Basic", content);
            if (response.IsSuccessStatusCode == false)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Error '{response.StatusCode}' while authenticating; response: {errorContent}");
            }
            var cookies = response.Headers.SingleOrDefault(header => header.Key == "Set-Cookie").Value;

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://{localGatwayIP.Trim()}{uriPath}");
            foreach (var cookie in cookies)
            {
                request.Headers.Add("Cookie", cookie);
            }
            response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode == false)
            {
                throw new InvalidOperationException($"Error '{response.StatusCode}' while calling API; response: {responseBody}");
            }
            return (JsonObject) JsonObject.Parse(responseBody);
        }
    }
}
