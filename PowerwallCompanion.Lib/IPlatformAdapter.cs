using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PowerwallCompanion.Lib
{
    public interface IPlatformAdapter
    {
        string AccessToken { get; set; }
        string RefreshToken { get; set; }

        Task<string> ReadFileContents(string filename);
        Task SaveGatewayDetailsToCache(JsonObject json);
        Task<JsonObject> ReadGatewayDetailsFromCache();
    }
}
