﻿using PowerwallCompanion.Lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using TeslaAuth;

namespace PowerwallCompanion
{
    internal class ApiHelper
    {
        private string _baseUrl;
        private ITokenStore _tokenStore;

        public ApiHelper(ITokenStore tokenStore)
        {
            _tokenStore = tokenStore;
        }

        public async Task<JsonObject> CallGetApiWithTokenRefresh(string url)
        {
            string fullUrl = url;
            if (!fullUrl.StartsWith("http"))
            {
                fullUrl = await GetBaseUrl() + fullUrl;
            }

            if (_tokenStore.AccessToken == null)
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
    
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _tokenStore.AccessToken);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("X-Tesla-User-Agent");
            var response = await client.GetAsync(url);
            var responseMessage = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return (JsonObject)JsonNode.Parse(responseMessage);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException();
            }
            else
            {
                throw new HttpRequestException(responseMessage);
            }
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
                var tokens = await helper.RefreshTokenAsync(_tokenStore.RefreshToken);
                _tokenStore.AccessToken = tokens.AccessToken;
                _tokenStore.RefreshToken = tokens.RefreshToken;
                _tokenLastRefreshed = DateTime.Now;

            }
            catch (Exception ex)
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