﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

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

        public static bool ShowEnergyRates
        {
            get
            {
                if (_localSettings.Values["ShowEnergyRates"] == null)
                {
                    return false;
                }
                else
                {
                    return bool.Parse((string)_localSettings.Values["ShowEnergyRates"]);
                }
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
                if (_localSettings.Values["TariffProvider"] == null)
                {
                    return "Tesla";
                }
                else
                {
                    return (string)_localSettings.Values["TariffProvider"];
                }
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
                if (_localSettings.Values["TariffDailySupplyCharge"] == null)
                {
                    return 0.0M;
                }
                else
                {
                    return decimal.Parse((string)_localSettings.Values["TariffDailySupplyCharge"]);
                }
            }
            set
            {
                _localSettings.Values["TariffDailySupplyCharge"] = value.ToString();
            }
        }

        public static decimal TariffNonBypassableCharge
        {
            get
            {
                if (_localSettings.Values["TariffNonBypassableCharge"] == null)
                {
                    return 0.0M;
                }
                else
                {
                    return decimal.Parse((string)_localSettings.Values["TariffNonBypassableCharge"]);
                }
            }
            set
            {
                _localSettings.Values["TariffNonBypassableCharge"] = value.ToString();
            }
        }


        public static string AmberElectricApiKey
        {
            get
            {
                if (_localSettings.Values["AmberElectricApiKey"] == null)
                {
                    return null;
                }
                else
                {
                    return (string)_localSettings.Values["AmberElectricApiKey"];
                }
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
                if (_localSettings.Values["ShowEnergySources"] == null)
                {
                    return false;
                }
                else
                {
                    return bool.Parse((string)_localSettings.Values["ShowEnergySources"]);
                }
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
                return _localSettings.Values["EnergySourcesZoneOverride"] as string;
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

        public static string LocalGatewayPassword
        {
            get
            {
                return _localSettings.Values["LocalGatewayPassword"] as string;
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
                    return DateTime.Parse(date);
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
                if (_localSettings.Values["PlaySounds"] == null)
                {
                    return false;
                }
                else
                {
                    return bool.Parse((string)_localSettings.Values["PlaySounds"]);
                }
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
                if (_localSettings.Values["StoreBatteryHistory"] == null)
                {
                    return false;
                }
                else
                {
                    return bool.Parse((string)_localSettings.Values["StoreBatteryHistory"]);
                }
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
                if (_localSettings.Values["PowerDecimals"] == null)
                {
                    return 1;
                }
                else
                {
                    return Int32.Parse((string)_localSettings.Values["PowerDecimals"]);
                }
            }
            set
            {
                _localSettings.Values["PowerDecimals"] = value.ToString();
            }
        }

        public static int EnergyDecimals
        {
            get
            {
                if (_localSettings.Values["EnergyDecimals"] == null)
                {
                    return 0;
                }
                else
                {
                    return Int32.Parse((string)_localSettings.Values["EnergyDecimals"]);
                }
            }
            set
            {
                _localSettings.Values["EnergyDecimals"] = value.ToString();
            }
        }

        public static int WindowHeight
        {
            get
            {
                if (_localSettings.Values["WindowHeight"] == null)
                {
                    return 1000;
                }
                else
                {
                    return int.Parse((string)_localSettings.Values["WindowHeight"]);
                }
            }
            set
            {
                _localSettings.Values["WindowHeight"] = value.ToString();
            }
        }

        public static int WindowWidth
        {
            get
            {
                if (_localSettings.Values["WindowWidth"] == null)
                {
                    return 1600;
                }
                else
                {
                    return int.Parse((string)_localSettings.Values["WindowWidth"]);
                }
            }
            set
            {
                _localSettings.Values["WindowWidth"] = value.ToString();
            }
        }

        public static string WindowState
        {
            get
            {
                if (_localSettings.Values["WindowState"] == null)
                {
                    return "Restored";
                }
                else
                {
                    return ((string)_localSettings.Values["WindowState"]);
                }
            }
            set
            {
                _localSettings.Values["WindowState"] = value; 
            }
        }
    }
}
