using Microsoft.AppCenter.Crashes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace PowerwallCompanion
{
    internal static class DateUtils
    {
        private static TimeZoneInfo powerwallTimeZone = null;

        public static async Task GetInstallationTimeZone()
        {
            try
            {
                var siteInfoJson = await ApiHelper.CallGetApiWithTokenRefresh($"/api/1/energy_sites/{Settings.SiteId}/site_info", "SiteInfo");
                Settings.InstallationTimeZone = siteInfoJson["respose"]["installation_time_zone"].Value<string>();
            }
            catch
            {
                Settings.InstallationTimeZone = TZConvert.WindowsToIana(TimeZoneInfo.Local.Id);
            }
        }

        public static DateTime ConvertToPowerwallDate(DateTime date)
        {
            try
            {
                if (powerwallTimeZone == null)
                {
                    var windowsTimeZone = TZConvert.IanaToWindows(Settings.InstallationTimeZone);
                    powerwallTimeZone = TimeZoneInfo.FindSystemTimeZoneById(windowsTimeZone);
                }
                var offset = powerwallTimeZone.GetUtcOffset(date);
                var dto = new DateTimeOffset(date);
                return dto.ToOffset(offset).DateTime;
            }
            catch 
            {
                // Unable to convert for some reason; assume local time 
                return date;
            }

        }
    }
}
