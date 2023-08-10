using Newtonsoft.Json;
using System.IO;
using TwitchSongRequest.Helpers;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services.App
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
                AppTokens.TwitchClient.ClientSecret = Secure.DecryptString(AppTokens.TwitchClient.ClientSecret ?? "");
                AppTokens.SpotifyClient.ClientSecret = Secure.DecryptString(AppTokens.SpotifyClient.ClientSecret ?? "");

                AppTokens.StreamerAccessTokens.AccessToken = Secure.DecryptString(AppTokens.StreamerAccessTokens.AccessToken ?? "");
                AppTokens.StreamerAccessTokens.RefreshToken = Secure.DecryptString(AppTokens.StreamerAccessTokens.RefreshToken ?? "");

                AppTokens.BotAccessTokens.AccessToken = Secure.DecryptString(AppTokens.BotAccessTokens.AccessToken ?? "");
                AppTokens.BotAccessTokens.RefreshToken = Secure.DecryptString(AppTokens.BotAccessTokens.RefreshToken ?? "");

                AppTokens.SpotifyAccessTokens.AccessToken = Secure.DecryptString(AppTokens.SpotifyAccessTokens.AccessToken ?? "");
                AppTokens.SpotifyAccessTokens.RefreshToken = Secure.DecryptString(AppTokens.SpotifyAccessTokens.RefreshToken ?? "");
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
            AppTokens.TwitchClient.ClientSecret = Secure.EncryptString(AppTokens.TwitchClient.ClientSecret ?? "");
            AppTokens.SpotifyClient.ClientSecret = Secure.EncryptString(AppTokens.SpotifyClient.ClientSecret ?? "");

            AppTokens.StreamerAccessTokens.AccessToken = Secure.EncryptString(AppTokens.StreamerAccessTokens.AccessToken ?? "");
            AppTokens.StreamerAccessTokens.RefreshToken = Secure.EncryptString(AppTokens.StreamerAccessTokens.RefreshToken ?? "");

            AppTokens.BotAccessTokens.AccessToken = Secure.EncryptString(AppTokens.BotAccessTokens.AccessToken ?? "");
            AppTokens.BotAccessTokens.RefreshToken = Secure.EncryptString(AppTokens.BotAccessTokens.RefreshToken ?? "");

            AppTokens.SpotifyAccessTokens.AccessToken = Secure.EncryptString(AppTokens.SpotifyAccessTokens.AccessToken ?? "");
            AppTokens.SpotifyAccessTokens.RefreshToken = Secure.EncryptString(AppTokens.SpotifyAccessTokens.RefreshToken ?? "");

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
