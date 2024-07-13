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
    public class MongoDBTelemetry
    {
        ITelemetryPlatformAdapter platformAdapter;
        Guid sessionId;
        public const string dataSource = "Cluster0";
        public const string database = "powerwallCompanion";
        public const string collection = "telemetry";

        public MongoDBTelemetry(ITelemetryPlatformAdapter platformAdapter)
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
            await WriteDocumentToMongoDB(message);
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
            await WriteDocumentToMongoDB(message);
        }

        private JsonObject BuildMessage()
        {
            JsonObject document = new JsonObject();
            document.Add("platform", platformAdapter.Platform);
            document.Add("userId", platformAdapter.UserId);

            document.Add("timestamp", JsonObject.Parse("{\"$date\": \"" + DateTime.UtcNow.ToString("O") + "\"}"));
            document.Add("appName", platformAdapter.AppName);
            document.Add("appVersion", platformAdapter.AppVersion);
            document.Add("osVersion", platformAdapter.OSVersion);
            document.Add("region", platformAdapter.Region);
            document.Add("sessionId", sessionId.ToString());
            return document;
        }

        private async Task WriteDocumentToMongoDB(JsonObject document)
        {
#if DEBUG
            Debug.WriteLine("Debug telemetry event: " + document.ToJsonString(new JsonSerializerOptions() {  WriteIndented = true }));
#else
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("api-key", Keys.MongoDBDataApiKey);
            var request = new HttpRequestMessage(HttpMethod.Post, Keys.MongoDBDataEndpoint);

            var insertDocument = new JsonObject();
            insertDocument.Add("dataSource", dataSource);
            insertDocument.Add("database", database);
            insertDocument.Add("collection", collection);
            insertDocument.Add("document", document);

            var content = new StringContent(insertDocument.ToString());
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            request.Content = content;
            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var contentString = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to write telemetry data to MongoDB. Status code: {response.StatusCode}, content: {contentString}");
            }
#endif
        }

    }
}
