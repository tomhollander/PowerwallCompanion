using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PowerwallCompanion.Lib
{
    internal class DemoApiHelper : IApiHelper
    {
        private IPlatformAdapter platformAdapter;
        public DemoApiHelper(IPlatformAdapter platformAdapter)
        {
            this.platformAdapter = platformAdapter;
        }
        public async Task<JsonObject> CallGetApiWithTokenRefresh(string url)
        {
            var questionMarkSplit = url.Split('?');
            var slashSplit = questionMarkSplit[0].Split('/');

            var fileName = slashSplit.Last();
            if (questionMarkSplit.Length > 1)
            {
                var queryDictionary = System.Web.HttpUtility.ParseQueryString(questionMarkSplit[1]);
                if (queryDictionary.AllKeys.Contains("kind"))
                {
                    fileName += "-" + queryDictionary["kind"];
                }
                if (queryDictionary.AllKeys.Contains("period"))
                {
                    fileName += "-" + queryDictionary["period"];
                }
            }
            fileName += ".json";
            var fileContent = await platformAdapter.ReadFileContents(fileName);
            return (JsonObject)JsonNode.Parse(fileContent);
        }
    }
}
