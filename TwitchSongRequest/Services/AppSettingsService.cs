using Newtonsoft.Json;
using System.IO;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services
{
    internal class AppSettingsService : IAppSettingsService
    {
        private readonly string filePath = "AppSettings.json";

        public AppSettings GetAppSettings()
        {
            if (!File.Exists(filePath))
            {
                return new AppSettings();
            }
            string json = File.ReadAllText(filePath);
            AppSettings? appSettings = JsonConvert.DeserializeObject<AppSettings>(json);
            if (appSettings == null)
            {
                return new AppSettings();
            }
            //Secure secure = new Secure(Environment.MachineName);
            //appSettings.TwitchClientSecret = secure.DecodeAndDecrypt(appSettings.TwitchClientSecret);
            return appSettings;
        }

        public void SaveAppSettings(AppSettings appSettings)
        {
            //Secure secure = new Secure(Environment.MachineName);
            //appSettings.TwitchClientSecret = secure.EncryptAndEncode(appSettings.TwitchClientSecret);
            string json = JsonConvert.SerializeObject(appSettings, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public AppSettings ResetAppSettings()
        {
            if (!File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return new AppSettings();
        }
    }
}
