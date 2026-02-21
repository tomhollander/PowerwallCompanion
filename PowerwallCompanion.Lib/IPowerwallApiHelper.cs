using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PowerwallCompanion.Lib
{
    public interface IPowerwallApiHelper
    {
        Task<JsonObject> CallGetApiWithTokenRefresh(string url);
    }
}
