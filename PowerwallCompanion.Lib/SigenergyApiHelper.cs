using PowerwallCompanion.Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using TeslaAuth;

namespace PowerwallCompanion.Lib
{
    public class SigenergyApiHelper 
    {
        private string _baseUrl;
        private IPlatformAdapter _platformAdatper;
        private Dictionary<string, JsonObject> _responseCache = new Dictionary<string, JsonObject>();
        
        // Reuse a single HttpClient instance to prevent JNI reference table overflow on Android
        private static readonly HttpClient _httpClient = new HttpClient();

        public SigenergyApiHelper(string countryCode, IPlatformAdapter platformAdapter)
        {
            _platformAdatper = platformAdapter;
            _baseUrl = $"https://api-{countryCode}.sigencloud.com";
        }

        private async Task GetNewAccessToken()
        {
            var authUrl = $"{_baseUrl}/openapi/auth/login/key";
            string encodedPassword = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{Keys.SigenergyAppKey}:{Keys.SigenergyAppSecret}"));
            var content = new StringContent($"{{\"key\":\"{encodedPassword}\"}}", System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(authUrl, content);
            var responseMessage = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"POST {authUrl} => {response.StatusCode} {responseMessage}");
            var jsonResponse = (JsonObject)JsonNode.Parse(responseMessage);
            if (response.IsSuccessStatusCode)
            {
                var data = jsonResponse["data"].GetValue<string>();
                var dataJson = (JsonObject)JsonNode.Parse(data);
                if (dataJson["accessToken"].GetValueKind() == System.Text.Json.JsonValueKind.String)
                {
                    _platformAdatper.AccessToken = dataJson["accessToken"].GetValue<string>();
                }
                else
                {
                    throw new Exception("Authentication response did not contain an access token.");
                }
            }
            else
            {
                throw new HttpRequestException($"Authentication failed with HTTP {response.StatusCode}");
            }
        }

        public async Task<JsonObject> CallGetApiWithTokenRefresh(string url, bool cache)
        {
            if (cache && _responseCache.ContainsKey(url))
            {
                return _responseCache[url];
            }
            if (_platformAdatper.AccessToken == null)
            {
                throw new UnauthorizedAccessException();
            }

            try
            {
                var response = await CallGetApi(url);
                _responseCache[url] = response;
                return response;
            }
            catch (UnauthorizedAccessException)
            {
                await GetNewAccessToken();
                return await CallGetApi(url);
            }
        }


        private async Task<JsonObject> CallGetApi(string url)
        {
            int retries = 0;

            while (retries < 3)
            {
                // Create a new request message for each attempt to ensure headers are correct and the request can be resent
                using var request = new HttpRequestMessage(HttpMethod.Get, _baseUrl + url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _platformAdatper.AccessToken);
                request.Headers.UserAgent.ParseAdd("X-Tesla-User-Agent");

                var response = await _httpClient.SendAsync(request);
                var responseMessage = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"GET {url} => {response.StatusCode} {responseMessage}");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = (JsonObject)JsonNode.Parse(responseMessage);
                    string dataValue = responseJson["data"]?.GetValue<string>() ?? "";
                    var dataJson = (JsonObject)JsonNode.Parse(dataValue);
                    return dataJson;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    retries++;
                    // Check for Retry-After header
                    if (response.Headers.RetryAfter?.Delta.HasValue == true)
                    {
                        await Task.Delay(response.Headers.RetryAfter.Delta.Value);
                    }
                    else
                    {
                        // Fallback to linear backoff if header is missing
                        await Task.Delay(1000 * retries);
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.FailedDependency)
                {
                    throw new UnauthorizedAccessException();
                }
                else
                {
                    throw new HttpRequestException($"Request failed with HTTP {response.StatusCode}");
                }
            }
            throw new HttpRequestException("Failed after maximum retries");
        }


       
    }

 
}
