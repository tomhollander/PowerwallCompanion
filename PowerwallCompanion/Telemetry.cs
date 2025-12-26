using Mixpanel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PowerwallCompanion
{
    internal static class Telemetry
    {
        private static Dictionary<string, DateTime> exceptionLastTrackedTimes = new Dictionary<string, DateTime>();
        private static bool? _shouldLogExceptionCache;
        private static DateTime _shouldLogExceptionCacheTime = DateTime.MinValue;

        public static void TrackUser()
        {
            try
            {
                var telemetryAdapter = new WindowsTelemetryPlatformAdapter();
                var mc = new MixpanelClient(Keys.MixpanelToken);
                mc.PeopleSetAsync(new
                {
                    DistinctId = telemetryAdapter.UserId,
                    Region = telemetryAdapter.Region,
                });
            }
            catch
            {
                // Ignore errors
            }
        }

        public static void TrackException(Exception ex)
        {
            string exceptionKey = ex.Message + ":" + ex.GetType().ToString();
            DateTime lastTracked = exceptionLastTrackedTimes.ContainsKey(exceptionKey) ? exceptionLastTrackedTimes[exceptionKey] : DateTime.MinValue;
            if (lastTracked != DateTime.MinValue && (DateTime.UtcNow - lastTracked).TotalSeconds < 300)
            {
                // Don't track the same exception more than once every 300 seconds
                return;
            }
            exceptionLastTrackedTimes[exceptionKey] = DateTime.UtcNow;

            Task.Run(async () =>
            {
                try
                {
                    // Check if telemetry is disabled for this version
                    if (await ShouldLogException())
                    {
                        await TrackEventToMixpanel("Exception", BuildExceptionMetadata(ex, true));
                    }
                }
                catch
                {
                    // Ignore errors
                }
            });
        }

        private static async Task<bool> ShouldLogException()
        {
            if (_shouldLogExceptionCache.HasValue && (DateTime.UtcNow - _shouldLogExceptionCacheTime).TotalHours < 8)
            {
                return _shouldLogExceptionCache.Value;
            }

            try
            {
                using (var client = new HttpClient())
                {
                    var json = await client.GetStringAsync("https://tomsapps2.blob.core.windows.net/powerwall-companion/disable-exception-telemetry-version.json");
                    var versions = JsonSerializer.Deserialize<string[]>(json);

                    var telemetryAdapter = new WindowsTelemetryPlatformAdapter();
                    if (versions.Contains(telemetryAdapter.AppVersion) || versions.Contains("*"))
                    {
                        _shouldLogExceptionCache = false;
                    }
                    else
                    {
                        _shouldLogExceptionCache = true;
                    }
                    _shouldLogExceptionCacheTime = DateTime.UtcNow;
                    return _shouldLogExceptionCache.Value;
                }
            }
            catch
            {
                return true;
            }
        }

        public static void TrackUnhandledException(Exception ex)
        {
            TrackEventToMixpanelSafe("Exception", BuildExceptionMetadata(ex, false));
        }

        private static Dictionary<string, string> BuildExceptionMetadata(Exception ex, bool handled)
        {
            // Split stack trace into 256 character segments as this is the limit for Mixpanel strings
            string stackTrace1 = null;
            string stackTrace2 = null;

            if (ex.StackTrace != null)
            {
                stackTrace1 = ex.StackTrace.Length > 256 ? ex.StackTrace.Substring(0, 256) : ex.StackTrace;
                stackTrace2 = ex.StackTrace.Length > 256 ? ex.StackTrace.Substring(256) : null;
            }

            return new Dictionary<string, string>()
            {
                { "Type", ex.GetType().ToString() },
                { "Message", ex.Message },
                { "StackTrace1", stackTrace1 },
                { "StackTrace2", stackTrace2 },
                { "IsHandled", handled.ToString() }
            };
        }

        public static void TrackEvent(string eventName, IDictionary<string, string> metadata)
        {
            TrackEventToMixpanelSafe(eventName, metadata);
        }

        public static void TrackEvent(string eventName)
        {
            TrackEventToMixpanelSafe(eventName, null);
        }

        private static void TrackEventToMixpanelSafe(string eventName, IDictionary<string, string> metadata)
        {
            try
            {
                Task.Run(async () => await TrackEventToMixpanel(eventName, metadata));
            }
            catch
            {

            }
        }

        private static async Task TrackEventToMixpanel(string eventName, IDictionary<string, string> metadata)
        {
            var telemetryAdapter = new WindowsTelemetryPlatformAdapter();
            var mc = new MixpanelClient(Keys.MixpanelToken);
            dynamic expando = new ExpandoObject();
            IDictionary<string, object> dict = expando;

            dict.Add("DistinctId", telemetryAdapter.UserId);
            dict.Add("AppVersion", telemetryAdapter.AppVersion);
            dict.Add("OSVersion", telemetryAdapter.OSVersion);
            dict.Add("AppName", telemetryAdapter.AppName);
            dict.Add("Platform", "Windows");

            if (metadata != null)
            {
                foreach (var kvp in metadata)
                {
                    dict.Add(kvp.Key, kvp.Value);
                }
            }
#if DEBUG
            Debug.WriteLine($"Telemetry: {eventName} {string.Join(", ", dict.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");
#else
            await mc.TrackAsync(eventName, dict);
#endif
        }

    }
}
