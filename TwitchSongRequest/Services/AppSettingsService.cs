using Newtonsoft.Json;
using System.IO;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services
{
    internal class AppSettingsService : IAppSettingsService
    {
        private readonly string appSettingsPath = "AppSettings.json";
        private readonly string appTokensPath = "AppTokens.json";

        public AppSettings AppSettings { get; private set; }
        public AppTokens AppTokens { get; private set; }

        public AppSettingsService()
        {
            AppSettings = new AppSettings();
            AppTokens = new AppTokens();

            if (File.Exists(appSettingsPath))
            {
                string settingsJson = File.ReadAllText(appSettingsPath);
                AppSettings = JsonConvert.DeserializeObject<AppSettings>(settingsJson) ?? new AppSettings();
            }

            if (File.Exists(appTokensPath))
            {
                string tokensJson = File.ReadAllText(appTokensPath);
                AppTokens = JsonConvert.DeserializeObject<AppTokens>(tokensJson) ?? new AppTokens();
                //Secure secure = new Secure(Environment.MachineName);
                //AppTokens.TwitchClientSecret = secure.DecodeAndDecrypt(AppTokens.TwitchClientSecret);
            }
        }

        public void SaveAppSettings()
        {
            string json = JsonConvert.SerializeObject(AppSettings, Formatting.Indented);
            File.WriteAllText(appSettingsPath, json);
        }

        public void ResetAppSettings()
        {
            if (!File.Exists(appSettingsPath))
            {
                File.Delete(appSettingsPath);
            }
            AppSettings = new AppSettings();
        }

        public void SaveAppTokens()
        {
            //Secure secure = new Secure(Environment.MachineName);
            //AppTokens.TwitchClientSecret = secure.EncryptAndEncode(AppTokens.TwitchClientSecret);
            string json = JsonConvert.SerializeObject(AppTokens, Formatting.Indented);
            File.WriteAllText(appTokensPath, json);
        }

        public void ResetAppTokens()
        {
            if (!File.Exists(appTokensPath))
            {
                File.Delete(appTokensPath);
            }
            AppTokens = new AppTokens();
        }
    }
}
