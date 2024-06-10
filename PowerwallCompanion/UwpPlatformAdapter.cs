using Newtonsoft.Json.Linq;
using PowerwallCompanion.Lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace PowerwallCompanion
{
    public class UwpPlatformAdapter : IPlatformAdapter
    {
        public string AccessToken { get => Settings.AccessToken; set => Settings.AccessToken = value; }
        public string RefreshToken { get => Settings.RefreshToken; set => Settings.RefreshToken = value; }

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
    }
}
