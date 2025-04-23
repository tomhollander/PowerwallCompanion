using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PowerwallCompanion.Lib.Tests
{
    [TestClass]
    public class UtilsTests
    {
        [TestMethod]
        public void CalendarHistoryUrlTest()
        {
            var url = Utils.GetCalendarHistoryUrl("12345", "America/New_York", "energy","day", new DateTime(2024, 2, 1), new DateTime(2024, 2, 2));
            StringAssert.Contains(url, "/api/1/energy_sites/12345/calendar_history?");
            StringAssert.Contains(url, "kind=energy");
            StringAssert.Contains(url, "period=day");
            StringAssert.Contains(url, "start_date=" + WebUtility.UrlEncode("2024-02-01T00:00:00.0000000-05:00"));
            StringAssert.Contains(url, "end_date=" + WebUtility.UrlEncode("2024-02-01T23:59:59.0000000-05:00"));
        }

        [TestMethod]
        public void GetUnspecifiedDateTimeTest()
        {
            var obj = new JsonObject();
            obj.Add("timestamp", new DateTimeOffset(2024, 2, 1, 12, 34, 56, new TimeSpan(-5, 0, 0)));
            var date = Utils.GetUnspecifiedDateTime(obj["timestamp"]);
            Assert.AreEqual(new DateTime(2024, 2, 1, 12, 34, 56), date);
        }

        [TestMethod]
        public void GetCalendarHistoryUrlForLondonInSummer()
        {
            var url = Utils.GetCalendarHistoryUrl("12345", "Europe/London", "energy", "day", new DateTime(2024, 6, 1), new DateTime(2024, 6, 2));
            StringAssert.Contains(url, "/api/1/energy_sites/12345/calendar_history?");
            StringAssert.Contains(url, "kind=energy");
            StringAssert.Contains(url, "period=day");
            StringAssert.Contains(url, "start_date=" + WebUtility.UrlEncode("2024-06-01T00:00:00.0000000+01:00"));
            StringAssert.Contains(url, "end_date=" + WebUtility.UrlEncode("2024-06-01T23:59:59.0000000+01:00"));
        }

        [TestMethod]
        public void GetCalendarHistoryUrlForLondonInWinter()
        {
            var url = Utils.GetCalendarHistoryUrl("12345", "Europe/London", "energy", "day", new DateTime(2024, 2, 1), new DateTime(2024, 2, 2));
            StringAssert.Contains(url, "/api/1/energy_sites/12345/calendar_history?");
            StringAssert.Contains(url, "kind=energy");
            StringAssert.Contains(url, "period=day");
            StringAssert.Contains(url, "start_date=" + WebUtility.UrlEncode("2024-02-01T00:00:00.0000000+00:00"));
            StringAssert.Contains(url, "end_date=" + WebUtility.UrlEncode("2024-02-01T23:59:59.0000000+00:00"));
        }

    }
}
