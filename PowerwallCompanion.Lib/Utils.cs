using System;
using System.Collections.Generic;
using System.Runtime;
using System.Text;
using System.Text.Json.Nodes;
using TimeZoneConverter;

namespace PowerwallCompanion.Lib
{
    internal static class Utils
    {
        public static string GetCalendarHistoryUrl(string siteId, string timeZone, string kind, string period, DateTime periodStart, DateTime periodEnd)
        {
            var sb = new StringBuilder();

            DateTime periodStartUnspecifiedTZ = new DateTime(periodStart.Ticks, DateTimeKind.Unspecified);
            DateTime periodEndUnspecifiedTZ = new DateTime(periodEnd.Ticks, DateTimeKind.Unspecified);

            var windowsTimeZone = TZConvert.IanaToWindows(timeZone);
            var startOffset = TimeZoneInfo.FindSystemTimeZoneById(windowsTimeZone).GetUtcOffset(periodStart);
            var endOffset = TimeZoneInfo.FindSystemTimeZoneById(windowsTimeZone).GetUtcOffset(periodEnd);
            var startDate = new DateTimeOffset(periodStartUnspecifiedTZ, startOffset);
            var endDate = new DateTimeOffset(periodEndUnspecifiedTZ, endOffset).AddSeconds(-1);

            sb.Append($"/api/1/energy_sites/{siteId}/calendar_history?");
            sb.Append("kind=" + kind);
            sb.Append("&period=" + period.ToLowerInvariant());
            if (period != "Lifetime")
            {
                sb.Append("&start_date=" + Uri.EscapeDataString(startDate.ToString("o")));
                sb.Append("&end_date=" + Uri.EscapeDataString(endDate.ToString("o")));
            }
            sb.Append("&time_zone=" + Uri.EscapeDataString(timeZone));
            sb.Append("&fill_telemetry=0");
            return sb.ToString();
        }

        public static T GetValueOrDefault<T>(JsonNode obj)
        {
            if (obj == null)
            {
                return default(T);
            }
            try
            {
                return obj.GetValue<T>();
            }
            catch
            {
                return default(T);
            }
        }
    }
}
