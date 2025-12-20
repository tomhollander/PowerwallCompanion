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
    internal class ApiHelper : IApiHelper
    {
        private string _baseUrl;
        private IPlatformAdapter _platformAdatper;
        
        // Reuse a single HttpClient instance to prevent JNI reference table overflow on Android
        private static readonly HttpClient _httpClient = new HttpClient();

        public ApiHelper(IPlatformAdapter platformAdapter)
        {
            _platformAdatper = platformAdapter;
        }

        public async Task<JsonObject> CallGetApiWithTokenRefresh(string url)
        {
            string fullUrl = url;
            if (!fullUrl.StartsWith("http"))
            {
                fullUrl = await GetBaseUrl() + fullUrl;
            }

            if (_platformAdatper.AccessToken == null)
            {
                throw new UnauthorizedAccessException();
            }

            try
            {
                return await CallGetApi(fullUrl);
            }
            catch (UnauthorizedAccessException)
            {
                await RefreshToken();
                return await CallGetApi(fullUrl);
            }
        }


        private async Task<JsonObject> CallGetApi(string url)
        {
            int retries = 0;

            while (retries < 3)
            {
                // Create a new request message for each attempt to ensure headers are correct and the request can be resent
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _platformAdatper.AccessToken);
                request.Headers.UserAgent.ParseAdd("X-Tesla-User-Agent");

                var response = await _httpClient.SendAsync(request);
                var responseMessage = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"GET {url} => {response.StatusCode} {responseMessage}");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = (JsonObject)JsonNode.Parse(responseMessage);
                    if (responseJson["response"].GetValueKind() == System.Text.Json.JsonValueKind.String && responseJson["response"].GetValue<string>() == "")
                    {
                        throw new NoDataException("Tesla API returned no data. Use the Tesla app to check that your Powerwall is connected to your network.");
                    }
                    return responseJson;
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
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
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


        private SemaphoreSlim _tokenRefreshSemaphore = new SemaphoreSlim(1, 1);
        private DateTime _tokenLastRefreshed;
        private async Task RefreshToken()
        {
            await _tokenRefreshSemaphore.WaitAsync(); // Token refreshes invalidate past tokens, so we don't want to do this multiple times at once
            try
            {
                if ((DateTime.Now - _tokenLastRefreshed).TotalSeconds < 10)
                {
                    return; // Token was likely refreshed while waiting for the semaphore
                }
                var helper = new TeslaAuthHelper(TeslaAccountRegion.Unknown, Keys.TeslaAppClientId, Keys.TeslaAppClientSecret, Keys.TeslaAppRedirectUrl,
                Scopes.BuildScopeString(new[] { Scopes.EnergyDeviceData, Scopes.VehicleDeviceData }));
                var tokens = await helper.RefreshTokenAsync(_platformAdatper.RefreshToken);
                _platformAdatper.AccessToken = tokens.AccessToken;
                _platformAdatper.RefreshToken = tokens.RefreshToken;
                _tokenLastRefreshed = DateTime.Now;

            }
            catch (Exception)
            {
                throw new UnauthorizedAccessException();
            }
            finally
            {
                _tokenRefreshSemaphore.Release();
            }
        }

        private async Task<string> GetBaseUrl()
        {
            if (_baseUrl == null)
            {
                var response = await CallGetApiWithTokenRefresh("https://fleet-api.prd.na.vn.cloud.tesla.com/api/1/users/region");
                _baseUrl = response["response"]["fleet_api_base_url"].GetValue<string>()
                    ?? "https://fleet-api.prd.na.vn.cloud.tesla.com";
            }
            return _baseUrl;
        }
    }

 
}
