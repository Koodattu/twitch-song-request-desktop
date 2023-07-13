using Newtonsoft.Json;
using System.IO;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services
{
    internal class AppSettingsService : IAppSettingsService
    {
        private readonly string filePath = "AppSettings.json";

        public AppSettings AppSettings { get; private set; }

        public AppSettingsService()
        {
            if (!File.Exists(filePath))
            {
                AppSettings = new AppSettings();
                return;
            }
            string json = File.ReadAllText(filePath);
            AppSettings? appSettings = JsonConvert.DeserializeObject<AppSettings>(json);
            if (appSettings == null)
            {
                AppSettings = new AppSettings();
                return;
            }
            //Secure secure = new Secure(Environment.MachineName);
            //appSettings.TwitchClientSecret = secure.DecodeAndDecrypt(appSettings.TwitchClientSecret);
            AppSettings = appSettings;
        }

        public void SaveAppSettings()
        {
            //Secure secure = new Secure(Environment.MachineName);
            //appSettings.TwitchClientSecret = secure.EncryptAndEncode(appSettings.TwitchClientSecret);
            string json = JsonConvert.SerializeObject(AppSettings, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public void ResetAppSettings()
        {
            if (!File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            AppSettings = new AppSettings();
        }
    }
}
