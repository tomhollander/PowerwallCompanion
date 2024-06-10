using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerwallCompanion.Lib.Tests
{
    internal class TestPlatformAdapter : IPlatformAdapter
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }

        public Task<string> ReadFileContents(string filename)
        {
            throw new NotImplementedException();
        }
    }
}
