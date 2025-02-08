using PowerwallCompanion.Lib.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PowerwallCompanion.Lib
{
    public class AzureFunctionsTelemetry
    {
        ITelemetryPlatformAdapter platformAdapter;
        Guid sessionId;

        public AzureFunctionsTelemetry(ITelemetryPlatformAdapter platformAdapter)
        {
            this.platformAdapter = platformAdapter;
            sessionId = Guid.NewGuid();
        }

        public void WriteExceptionSafe(Exception ex, bool handled)
        {
            try
            {
                Task.Run(async () => await WriteException(ex, handled));
            }
            catch
            {

            }
        }

        public void WriteEventSafe(string eventName, IDictionary<string, string> metadata)
        {
            try
            {
                Task.Run(async () => await WriteEvent(eventName, metadata));
            }
            catch
            {

            }
        }

        public async Task WriteException(Exception ex, bool handled)
        {
            var message = BuildMessage();
            message.Add("type", "exception");
            message.Add("handled", handled);
            message.Add("exceptionType", ex.GetType().Name);
            message.Add("message", ex.Message);
            message.Add("stackTrace", ex.StackTrace);
            await WriteDocumentToApi(message);
        }


        public async Task WriteEvent(string eventName, IDictionary<string, string> metadata)
        {

            var message = BuildMessage();
            message.Add("type", "event");
            message.Add("eventName", eventName);
            if (metadata != null && metadata.Count > 0)
            {
                var jsonMetadata = new JsonObject();
                foreach (var key in metadata.Keys)
                {
                    jsonMetadata.Add(key, metadata[key]);
                }
                message.Add("metadata", jsonMetadata);
            }
            await WriteDocumentToApi(message);
        }

        private JsonObject BuildMessage()
        {
            JsonObject document = new JsonObject();
            // NB: timestamp is added in the Azure function
            document.Add("platform", platformAdapter.Platform);
            document.Add("userId", platformAdapter.UserId);
            document.Add("appName", platformAdapter.AppName);
            document.Add("appVersion", platformAdapter.AppVersion);
            document.Add("osVersion", platformAdapter.OSVersion);
            document.Add("region", platformAdapter.Region);
            document.Add("sessionId", sessionId.ToString());
            return document;
        }

        private async Task WriteDocumentToApi(JsonObject document)
        {
#if DEBUG
            document.Add("timestamp", DateTime.Now.ToString("O"));
            Debug.WriteLine("Debug telemetry event: " + document.ToJsonString(new JsonSerializerOptions() {  WriteIndented = true }));
#else
            var client = new HttpClient();
            var url = $"https://pwcfunctions.azurewebsites.net/api/pwcTelemetry?code={Keys.AzureFunctionsApiKey}";
            var response = await client.PostAsync(url, new StringContent(document.ToJsonString(), Encoding.UTF8, "application/json"));
#endif
        }

    }
}
