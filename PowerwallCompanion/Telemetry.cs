using PowerwallCompanion.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerwallCompanion
{
    internal static class Telemetry
    {
        private static AzureFunctionsTelemetry telemetryProvider;

        static Telemetry()
        {
            telemetryProvider = new AzureFunctionsTelemetry(new WindowsTelemetryPlatformAdapter());
        }

        public static void TrackException(Exception ex)
        {
            telemetryProvider.WriteExceptionSafe(ex, true);
        }

        public static async Task TrackUnhandledException(Exception ex)
        {
            await telemetryProvider.WriteException(ex, false);
        }

        public static void TrackEvent(string eventName, IDictionary<string, string> metadata)
        {
            telemetryProvider.WriteEventSafe(eventName, metadata);
        }

        public static void TrackEvent(string eventName)
        {
            telemetryProvider.WriteEventSafe(eventName, null);
        }

    }
}
