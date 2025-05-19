using Mixpanel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerwallCompanion
{
    internal static class Telemetry
    {
        private static Dictionary<string, DateTime> exceptionLastTrackedTimes = new Dictionary<string, DateTime>();

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
            if (lastTracked != DateTime.MinValue && (DateTime.UtcNow - lastTracked).TotalSeconds < 180)
            {
                // Don't track the same exception more than once every 180 seconds
                return;
            }
            exceptionLastTrackedTimes[exceptionKey] = DateTime.UtcNow;

            TrackEventToMixpanelSafe("Exception", BuildExceptionMetadata(ex, true));
        }

        public static void TrackUnhandledException(Exception ex)
        {
            TrackEventToMixpanelSafe("Exception", BuildExceptionMetadata(ex, false));
        }

        private static Dictionary<string, string> BuildExceptionMetadata(Exception ex, bool handled)
        {
            // Split stack trace into 256 character segments as this is the limit for Mixpanel strings
            var stackTrace1 = ex.StackTrace.Length > 256 ? ex.StackTrace.Substring(0, 256) : ex.StackTrace;
            var stackTrace2 = ex.StackTrace.Length > 256 ? ex.StackTrace.Substring(256) : null;
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
