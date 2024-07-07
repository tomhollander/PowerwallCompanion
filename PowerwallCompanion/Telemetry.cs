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
        private static MongoDBTelemetry mongoDbTelemetry;

        static Telemetry()
        {
            mongoDbTelemetry = new MongoDBTelemetry(new UwpTelemetryPlatformAdapter());
        }

        public static void TrackException(Exception ex)
        {
            mongoDbTelemetry.WriteExceptionSafe(ex, true);
        }

        public static async Task TrackUnhandledException(Exception ex)
        {
            await mongoDbTelemetry.WriteException(ex, false);
        }

        public static void TrackEvent(string eventName, IDictionary<string, string> metadata)
        {
            mongoDbTelemetry.WriteEventSafe(eventName, metadata);
        }

        public static void TrackEvent(string eventName)
        {
            mongoDbTelemetry.WriteEventSafe(eventName, null);
        }

    }
}
