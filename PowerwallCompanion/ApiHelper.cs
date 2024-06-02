using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.AppCenter.Utils.Synchronization;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeslaAuth;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage;
using Windows.Web.Http.Filters;

namespace PowerwallCompanion
{
    static class ApiHelper
    {
        public static string _baseUrl;

        public static async Task<JObject> CallGetApiWithTokenRefresh(string url, string demoId)
        {
            string fullUrl = url;
            if (!fullUrl.StartsWith("http"))
            {
                fullUrl = await GetBaseUrl() + fullUrl;
            }

            if (Settings.AccessToken == null)
            {
                throw new UnauthorizedAccessException();
            }

            try
            {
                return await CallGetApi(fullUrl, demoId);
            }
            catch (UnauthorizedAccessException)
            {
                await RefreshToken();
                return await CallGetApi(fullUrl, demoId);
            }
        }

        public static async Task<JObject> CallApiIgnoreCerts(string url)
        {
            var filter = new HttpBaseProtocolFilter();
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Expired);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);
            filter.CacheControl.ReadBehavior = HttpCacheReadBehavior.MostRecent;
            filter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache;

            using (var client = new Windows.Web.Http.HttpClient(filter))
            {
                var response = await client.GetAsync(new Uri(url));
                var responseMessage = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    return JObject.Parse(responseMessage);
                }
                else
                {
                    throw new HttpRequestException(responseMessage);
                }
            }
        }

        private static async Task<JObject> CallGetApi(string url, string demoId)
        {
            Analytics.TrackEvent("CallGetApi", new Dictionary<string, string> { { "URL", url } });
            if (Settings.AccessToken == "DEMO")
            {
                return await GetDemoDocument(demoId);
            }
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Settings.AccessToken);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("X-Tesla-User-Agent");
            var response = await client.GetAsync(url);
            var responseMessage = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return JObject.Parse(responseMessage);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException();
            }
            else
            {
                Analytics.TrackEvent("CallGetApi failed", new Dictionary<string, string> { { "URL", url }, { "StatusCode", response.StatusCode.ToString() }, { "Message", responseMessage } });
                throw new HttpRequestException(responseMessage);
            }
        }

        private static async Task<JObject> GetDemoDocument(string demoId)
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///DemoData/{demoId}.json"));
            using (var inputStream = await file.OpenReadAsync())
            using (var classicStream = inputStream.AsStreamForRead())
            using (var streamReader = new StreamReader(classicStream))
            {
                var content = await streamReader.ReadToEndAsync();
                return JObject.Parse(content);
            }
        }

        private static SemaphoreSlim _tokenRefreshSemaphore = new SemaphoreSlim(1, 1);
        private static DateTime _tokenLastRefreshed;
        private static async Task RefreshToken()
        {
            await _tokenRefreshSemaphore.WaitAsync(); // Token refreshes invalidate past tokens, so we don't want to do this multiple times at once
            try
            {
                if ((DateTime.Now - _tokenLastRefreshed).TotalSeconds < 10)
                {
                    return; // Token was likely refreshed while waiting for the semaphore
                }
                var helper = new TeslaAuthHelper(TeslaAccountRegion.Unknown, Licenses.TeslaAppClientId, Licenses.TeslaAppClientSecret, Licenses.TeslaAppRedirectUrl,
                Scopes.BuildScopeString(new[] { Scopes.EnergyDeviceData, Scopes.VehicleDeviceData }));
                var tokens = await helper.RefreshTokenAsync(Settings.RefreshToken);
                Analytics.TrackEvent("RefreshToken");
                Settings.AccessToken = tokens.AccessToken;
                Settings.RefreshToken = tokens.RefreshToken;
                _tokenLastRefreshed = DateTime.Now;

            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
                throw new UnauthorizedAccessException();
            }
            finally
            {
                _tokenRefreshSemaphore.Release();
            }
        }

        private static async Task<string> GetBaseUrl()
        {
            if (_baseUrl == null)
            {
                var response = await CallGetApiWithTokenRefresh("https://fleet-api.prd.na.vn.cloud.tesla.com/api/1/users/region", "region");
                _baseUrl = response["response"]["fleet_api_base_url"].Value<string>()
                    ?? "https://fleet-api.prd.na.vn.cloud.tesla.com";
            }
            return _baseUrl;
        }
    }

 
}
