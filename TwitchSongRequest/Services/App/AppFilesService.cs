using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using TwitchSongRequest.Helpers;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services.App
{
    internal class AppFilesService : IAppFilesService
    {
        private readonly string appSettingsPath = "AppSettings.json";
        private readonly string appTokensPath = "AppTokens.json";

        private readonly string songQueuePath = "SongQueue.json";
        private readonly string songHistoryPath = "SongHistory.json";

        public AppSettings AppSettings { get; private set; }
        public AppTokens AppTokens { get; private set; }

        public AppFilesService()
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

        public List<SongRequest> GetSongQueue()
        {
            List<SongRequest> queue = new List<SongRequest>();
            if (File.Exists(songQueuePath))
            {
                string queueJson = File.ReadAllText(songQueuePath);
                queue = JsonConvert.DeserializeObject<List<SongRequest>>(queueJson) ?? new List<SongRequest>();
            }
            return queue;
        }

        public void SaveSongQueue(List<SongRequest> songRequests)
        {
            string json = JsonConvert.SerializeObject(songRequests, Formatting.Indented);
            File.WriteAllText(songQueuePath, json);
        }

        public List<SongRequest> GetSongHistory()
        {
            List<SongRequest> queue = new List<SongRequest>();
            if (File.Exists(songHistoryPath))
            {
                string queueJson = File.ReadAllText(songHistoryPath);
                queue = JsonConvert.DeserializeObject<List<SongRequest>>(queueJson) ?? new List<SongRequest>();
            }
            return queue;
        }

        public void SaveSongHistory(List<SongRequest> songRequests)
        {
            string json = JsonConvert.SerializeObject(songRequests, Formatting.Indented);
            File.WriteAllText(songHistoryPath, json);
        }
    }
}
