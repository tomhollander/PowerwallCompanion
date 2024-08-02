using PowerwallCompanion.Lib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.Profile;

namespace PowerwallCompanion
{
    public class UwpTelemetryPlatformAdapter : ITelemetryPlatformAdapter
    {
        string userId;

        public string Platform => "Windows";
        public string UserId
        {
            get
            {
                if (userId == null)
                {
                    SystemIdentificationInfo systemIdentificationInfo = SystemIdentification.GetSystemIdForPublisher();
                    var hardwareId = systemIdentificationInfo.Id;
                    userId = Windows.Security.Cryptography.CryptographicBuffer.EncodeToHexString(hardwareId);
                }
                return userId;
            }
        }

        public string AppName => Windows.ApplicationModel.Package.Current.DisplayName;

        public string AppVersion
        {
            get
            {
                var version = Windows.ApplicationModel.Package.Current.Id.Version;
                return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
            }
        }

        public string OSVersion
        {
            get
            {
                string deviceFamilyVersion = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
                ulong version = ulong.Parse(deviceFamilyVersion);
                ulong major = (version & 0xFFFF000000000000L) >> 48;
                ulong minor = (version & 0x0000FFFF00000000L) >> 32;
                ulong build = (version & 0x00000000FFFF0000L) >> 16;
                ulong revision = (version & 0x000000000000FFFFL);
                return $"{major}.{minor}.{build}.{revision}";
            }
        }

        public string Region => CultureInfo.CurrentCulture.Name;
    }
}
