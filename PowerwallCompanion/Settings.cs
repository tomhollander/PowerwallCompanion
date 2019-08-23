using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerwallCompanion
{
    static class Settings
    {
        private static Windows.Storage.ApplicationDataContainer _localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        public const decimal DefaultGraphScale = 1.0M;


        public static string AccessToken
        {
            get
            {
                return _localSettings.Values["AccessToken"] as string;
            }
            set
            {
                _localSettings.Values["AccessToken"] = value;
            }
        }

        public static string RefreshToken
        {
            get
            {
                return _localSettings.Values["RefreshToken"] as string;
            }
            set
            {
                _localSettings.Values["RefreshToken"] = value;
            }
        }

        public static string SignInName
        {
            get
            {
                return _localSettings.Values["SignInName"] as string;
            }
            set
            {
                _localSettings.Values["SignInName"] = value;
            }
        }

        public static string SiteId
        {
            get
            {
                return _localSettings.Values["SiteId"] as string;
            }
            set
            {
                _localSettings.Values["SiteId"] = value;
            }
        }

        public static decimal GraphScale
        {
            get
            {
                if (_localSettings.Values["graphScale"] == null)
                {
                    return DefaultGraphScale;
                }
                else
                {
                    return Decimal.Parse((string)_localSettings.Values["graphScale"]);
                }
            }
            set
            {
                _localSettings.Values["graphScale"] = value.ToString();
            }
        }

        public static bool ShowClock
        {
            get
            {
                if (_localSettings.Values["showClock"] == null)
                {
                    return false;
                }
                else
                {
                    return bool.Parse((string)_localSettings.Values["showClock"]);
                }
            }
            set
            {
                _localSettings.Values["showClock"] = value.ToString();
            }
        }

        public static bool UseLocalGateway
        {
            get
            {
                if (_localSettings.Values["UseLocalGateway"] == null)
                {
                    return false;
                }
                else
                {
                    return Boolean.Parse((string)_localSettings.Values["UseLocalGateway"]);
                }
            }
            set
            {
                _localSettings.Values["UseLocalGateway"] = value.ToString();
            }
        }

        public static string LocalGatewayIP
        {
            get
            {
                return _localSettings.Values["LocalGatewayIP"] as string;
            }
            set
            {
                _localSettings.Values["LocalGatewayIP"] = value;
            }
        }
    }
}
