using System;
using System.Collections.Generic;
using System.Text;

namespace PowerwallCompanion.Lib
{
    public interface ITokenStore
    {
        string AccessToken { get; set; }
        string RefreshToken { get; set; }
    }
}
