using Mixpanel;
using PowerwallCompanion.Lib;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;

namespace PowerwallCompanion
{
    internal static class Telemetry
    {

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

            TrackEventToMixpanelSafe("Exception", BuildExceptionMetadata(ex, true));
        }

        public static void TrackUnhandledException(Exception ex)
        {
            TrackEventToMixpanelSafe("Exception", BuildExceptionMetadata(ex, false));
        }

        private static Dictionary<string, string> BuildExceptionMetadata(Exception ex, bool handled)
        {
            return new Dictionary<string, string>()
            {
                { "Type", ex.GetType().ToString() },
                { "Message", ex.Message },
                { "FullException", ex.ToString() },
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
            await mc.TrackAsync(eventName, dict);
        }

    }
}
