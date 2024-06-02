using PowerwallCompanion.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerwallCompanion
{
    public class TokenStore : ITokenStore
    {
        public string AccessToken { get => Settings.AccessToken; set => Settings.AccessToken = value; }
        public string RefreshToken { get => Settings.RefreshToken; set => Settings.RefreshToken = value; }
    }
}
