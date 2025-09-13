using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PowerwallCompanion
{
    static class Settings
    {
        private static Windows.Storage.ApplicationDataContainer _localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        public const decimal DefaultGraphScale = 1.0M;

        private static T GetSetting<T>(string key, T defaultValue)
        {
            if (_localSettings.Values[key] == null)
            {
                return defaultValue;
            }
            else
            {
                try
                {
                    if (typeof(T) == typeof(bool))
                    {
                        return (T)(object)bool.Parse((string)_localSettings.Values[key]);
                    }
                    else if (typeof(T) == typeof(int))
                    {
                        return (T)(object)Int32.Parse((string)_localSettings.Values[key], CultureInfo.InvariantCulture);
                    }
                    else if (typeof(T) == typeof(decimal))
                    {
                        return (T)(object)Decimal.Parse((string)_localSettings.Values[key], CultureInfo.InvariantCulture);
                    }
                    else if (typeof(T) == typeof(string))
                    {
                        return (T)(object)((string)_localSettings.Values[key]);
                    }
                    else
                    {
                        return (T)_localSettings.Values[key];
                    }
                }
                catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException)
                {
                    return defaultValue;
                }
                
            }
        }

        public static string AccessToken
        {
            get
            {
                return GetSetting<string>("AccessToken", null);
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
                return GetSetting<string>("RefreshToken", null);
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
                return GetSetting<string>("SignInName", null);
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
                return GetSetting<string>("SiteId", null);
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
                return GetSetting<decimal>("graphScale", DefaultGraphScale);
            }
            set
            {
                _localSettings.Values["graphScale"] = value.ToString(CultureInfo.InvariantCulture);
            }
        }

        public static bool ShowClock
        {
            get
            {
                return GetSetting<bool>("showClock", false);
            }
            set
            {
                _localSettings.Values["showClock"] = value.ToString();
            }
        }


        public static bool ShowAnimations
        {
            get
            {
                return GetSetting<bool>("showAnimations", true);
            }
            set
            {
                _localSettings.Values["showAnimations"] = value.ToString();
            }
        }

        public static bool ShowEnergyRates
        {
            get
            {
                return GetSetting<bool>("ShowEnergyRates", false);
            }
            set
            {
                _localSettings.Values["ShowEnergyRates"] = value.ToString();
            }
        }

        public static string TariffProvider
        {
            get
            {
                return GetSetting<string>("TariffProvider", "Tesla");
            }
            set
            {
                _localSettings.Values["TariffProvider"] = value;
            }
        }

        public static decimal TariffDailySupplyCharge
        {
            get
            {
                return GetSetting<decimal>("TariffDailySupplyCharge", 0.0M);
            }
            set
            {
                _localSettings.Values["TariffDailySupplyCharge"] = value.ToString(CultureInfo.InvariantCulture);
            }
        }

        public static decimal TariffNonBypassableCharge
        {
            get
            {
                return GetSetting<decimal>("TariffNonBypassableCharge", 0.0M);
            }
            set
            {
                _localSettings.Values["TariffNonBypassableCharge"] = value.ToString(CultureInfo.InvariantCulture);
            }
        }


        public static string AmberElectricApiKey
        {
            get
            {
                return GetSetting<string>("AmberElectricApiKey", null);
            }
            set
            {
                _localSettings.Values["AmberElectricApiKey"] = value;
            }
        }


        public static bool ShowEnergySources
        {
            get
            {
                return GetSetting<bool>("ShowEnergySources", false);
            }
            set
            {
                _localSettings.Values["ShowEnergySources"] = value.ToString();
            }
        }

        public static string EnergySourcesZoneOverride
        {
            get
            {
                return GetSetting<string>("EnergySourcesZoneOverride", null);
            }
            set
            {
                _localSettings.Values["EnergySourcesZoneOverride"] = value;
            }
        }


        public static bool UseLocalGateway
        {
            get
            {
                return GetSetting<bool>("UseLocalGateway", false);
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
                return GetSetting<string>("LocalGatewayIP", null);
            }
            set
            {
                _localSettings.Values["LocalGatewayIP"] = value;
            }
        }

        public static string LocalGatewayPassword
        {
            get
            {
                return GetSetting<string>("LocalGatewayPassword", null);
            }
            set
            {
                _localSettings.Values["LocalGatewayPassword"] = value;
            }
        }

        public static DateTime CachedGatewayDetailsUpdated
        {
            get
            {
                string date = _localSettings.Values["CachedGatewayDetailsUpdated"] as string;
                if (date == null)
                {
                    return DateTime.MinValue;
                }
                else
                {
                    return DateTime.Parse(date, CultureInfo.InvariantCulture);
                }
            }
            set
            {
                _localSettings.Values["CachedGatewayDetailsUpdated"] = value.ToString("O");
            }
        }

        public static Dictionary<string, string> AvailableSites
        {
            get
            {
                var json = _localSettings.Values["AvailableSites"] as string;
                return json == null ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            }
            set
            {
                var json = JsonSerializer.Serialize(value);
                _localSettings.Values["AvailableSites"] = json;
            }
        }

        public static bool PlaySounds
        {
            get
            {
                return GetSetting<bool>("PlaySounds", false);
            }
            set
            {
                _localSettings.Values["PlaySounds"] = value.ToString();
            }
        }

        public static bool StoreBatteryHistory
        {
            get
            {
                return GetSetting<bool>("StoreBatteryHistory", false);
            }
            set
            {
                _localSettings.Values["StoreBatteryHistory"] = value.ToString();
            }
        }

        // Not set by the user
        public static string InstallationTimeZone
        {
            get
            {
                return _localSettings.Values["InstallationTimeZone"] as string;
            }
            set
            {
                _localSettings.Values["InstallationTimeZone"] = value;
            }
        }

        public static int PowerDecimals
        {
            get
            {
                return GetSetting<int>("PowerDecimals", 1);
            }
            set
            {
                _localSettings.Values["PowerDecimals"] = value.ToString(CultureInfo.InvariantCulture);
            }
        }

        public static int EnergyDecimals
        {
            get
            {
                return GetSetting<int>("EnergyDecimals", 0);
            }
            set
            {
                _localSettings.Values["EnergyDecimals"] = value.ToString(CultureInfo.InvariantCulture);
            }
        }

        public static int WindowHeight
        {
            get
            {
                return GetSetting<int>("WindowHeight", 1000);
            }
            set
            {
                _localSettings.Values["WindowHeight"] = value.ToString(CultureInfo.InvariantCulture);
            }
        }

        public static int WindowWidth
        {
            get
            {
                return GetSetting<int>("WindowWidth", 1600);
            }
            set
            {
                _localSettings.Values["WindowWidth"] = value.ToString(CultureInfo.InvariantCulture);
            }
        }

        public static string WindowState
        {
            get
            {
                return GetSetting<string>("WindowState", "Restored");
            }
            set
            {
                _localSettings.Values["WindowState"] = value; 
            }
        }

        public static string PowerDisplayMode
        {
            get
            {
                return GetSetting<string>("PowerDisplayMode", "Flow");
            }
            set
            {
                _localSettings.Values["PowerDisplayMode"] = value;
            }
        }

    }
}
