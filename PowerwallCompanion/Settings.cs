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
        private static Windows.Storage.ApplicationDataContainer _roamingSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        public const decimal DefaultGraphScale = 1.0M;

        private static T GetSetting<T>(string key, T defaultValue)
        {
            // Use local settings if they exist, otherwise roaming settings
            var savedValue = _roamingSettings.Values[key];
            if (savedValue != null)
            {
                savedValue = _localSettings.Values[key];
            }

            if (savedValue == null)
            {
                return defaultValue;
            }
            else
            {
                try
                {
                    if (typeof(T) == typeof(bool))
                    {
                        return (T)(object)bool.Parse((string)savedValue);
                    }
                    else if (typeof(T) == typeof(int))
                    {
                        return (T)(object)Int32.Parse((string)savedValue, CultureInfo.InvariantCulture);
                    }
                    else if (typeof(T) == typeof(decimal))
                    {
                        return (T)(object)Decimal.Parse((string)savedValue, CultureInfo.InvariantCulture);
                    }
                    else if (typeof(T) == typeof(string))
                    {
                        return (T)(object)((string)savedValue);
                    }
                    else
                    {
                        return (T)savedValue;
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
                _roamingSettings.Values["AccessToken"] = value;
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
                _roamingSettings.Values["RefreshToken"] = value;
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
                _roamingSettings.Values["SignInName"] = value;
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
                _roamingSettings.Values["SiteId"] = value;
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
                _roamingSettings.Values["graphScale"] = value.ToString(CultureInfo.InvariantCulture);
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
                _roamingSettings.Values["showClock"] = value.ToString();
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
                _roamingSettings.Values["showAnimations"] = value.ToString();
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
                _roamingSettings.Values["ShowEnergyRates"] = value.ToString();
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
                _roamingSettings.Values["TariffProvider"] = value;
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
                _roamingSettings.Values["TariffDailySupplyCharge"] = value.ToString(CultureInfo.InvariantCulture);
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
                _roamingSettings.Values["TariffNonBypassableCharge"] = value.ToString(CultureInfo.InvariantCulture);
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
                _roamingSettings.Values["AmberElectricApiKey"] = value;
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
                _roamingSettings.Values["ShowEnergySources"] = value.ToString();
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
                _roamingSettings.Values["EnergySourcesZoneOverride"] = value;
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
                _roamingSettings.Values["LocalGatewayIP"] = value;
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
                _roamingSettings.Values["LocalGatewayPassword"] = value;
            }
        }

        public static DateTime CachedGatewayDetailsUpdated
        {
            get
            {
                string date = _roamingSettings.Values["CachedGatewayDetailsUpdated"] as string;
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
                _roamingSettings.Values["CachedGatewayDetailsUpdated"] = value.ToString("O");
            }
        }

        public static Dictionary<string, string> AvailableSites
        {
            get
            {
                var json = _roamingSettings.Values["AvailableSites"] as string;
                return json == null ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            }
            set
            {
                var json = JsonSerializer.Serialize(value);
                _roamingSettings.Values["AvailableSites"] = json;
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
                _roamingSettings.Values["PlaySounds"] = value.ToString();
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
                _roamingSettings.Values["StoreBatteryHistory"] = value.ToString();
            }
        }

        public static bool EstimateBatteryCapacity
        {
            get
            {
                return GetSetting<bool>("EstimateBatteryCapacity", true);
            }
            set
            {
                _roamingSettings.Values["EstimateBatteryCapacity"] = value.ToString();
            }
        }

        public static bool UseLocalGatewayForBatteryCapacity
        {
            get
            {
                if (_roamingSettings.Values["UseLocalGatewayForBatteryCapacity"] == null)
                {
                    return LocalGatewayIP != null; // Leave this off unless a local gateway is configured
                }
                return GetSetting<bool>("UseLocalGatewayForBatteryCapacity", true);
            }
            set
            {
                _roamingSettings.Values["UseLocalGatewayForBatteryCapacity"] = value.ToString();
            }
        }

        // Not set by the user
        public static string InstallationTimeZone
        {
            get
            {
                return _roamingSettings.Values["InstallationTimeZone"] as string;
            }
            set
            {
                _roamingSettings.Values["InstallationTimeZone"] = value;
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
                _roamingSettings.Values["PowerDecimals"] = value.ToString(CultureInfo.InvariantCulture);
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
                _roamingSettings.Values["EnergyDecimals"] = value.ToString(CultureInfo.InvariantCulture);
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
                _roamingSettings.Values["WindowHeight"] = value.ToString(CultureInfo.InvariantCulture);
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
                _roamingSettings.Values["WindowWidth"] = value.ToString(CultureInfo.InvariantCulture);
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
                _roamingSettings.Values["WindowState"] = value; 
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
                _roamingSettings.Values["PowerDisplayMode"] = value;
            }
        }

    }
}
