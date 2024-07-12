using PowerwallCompanion.Lib;
using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Windows.Storage;

namespace PowerwallCompanion
{
    public class UwpPlatformAdapter : IPlatformAdapter
    {
        private Windows.Storage.ApplicationDataContainer _localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        public string GetPersistedData(string key)
        {
           return _localSettings.Values[key] as string;
        }

        public void PersistData(string key, string value)
        {
            _localSettings.Values[key] = value;
        }

        public string AccessToken { get => Settings.AccessToken; set => Settings.AccessToken = value; }
        public string RefreshToken { get => Settings.RefreshToken; set => Settings.RefreshToken = value; }
        public string InstallationTimeZone { get => Settings.InstallationTimeZone; set => Settings.InstallationTimeZone = value; }

        public async Task<string> ReadFileContents(string filename)
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///DemoData/{filename}"));
            using (var inputStream = await file.OpenReadAsync())
            using (var classicStream = inputStream.AsStreamForRead())
            using (var streamReader = new StreamReader(classicStream))
            {
                return await streamReader.ReadToEndAsync();
            }
        }

        public async Task SaveGatewayDetailsToCache(JsonObject json)
        {
            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            Windows.Storage.StorageFile cacheFile = await storageFolder.CreateFileAsync("gateway_system_status.json", Windows.Storage.CreationCollisionOption.ReplaceExisting);
            await Windows.Storage.FileIO.WriteTextAsync(cacheFile, json.ToString());
        }

        public async Task<JsonObject> ReadGatewayDetailsFromCache()
        {
            try
            {
                Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                Windows.Storage.StorageFile cacheFile = await storageFolder.GetFileAsync("gateway_system_status.json");
                string text = await Windows.Storage.FileIO.ReadTextAsync(cacheFile);
                return (JsonObject)JsonObject.Parse(text);
            }
            catch
            {
                return null;
            }
        }


    }
}
