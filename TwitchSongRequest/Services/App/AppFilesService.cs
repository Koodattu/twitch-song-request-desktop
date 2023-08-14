using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using TwitchSongRequest.Helpers;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services.App
{
    internal class AppFilesService : IAppFilesService
    {
        private ILoggerService _loggerService;

        private readonly string appSettingsPath = "AppSettings.json";
        private readonly string appSetupPath = "AppSetup.json";

        private readonly string songQueuePath = "SongQueue.json";
        private readonly string songHistoryPath = "SongHistory.json";

        public AppSettings AppSettings { get; private set; }
        public AppSetup AppSetup { get; private set; }

        public AppFilesService(ILoggerService loggerService)
        {
            _loggerService = loggerService;

            _loggerService.LogInfo("AppFilesService: Initializing");

            AppSettings = new AppSettings();
            AppSetup = new AppSetup();

            try
            {
                if (File.Exists(appSettingsPath))
                {
                    string settingsJson = File.ReadAllText(appSettingsPath);
                    AppSettings = JsonConvert.DeserializeObject<AppSettings>(settingsJson) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, "AppFilesService: Error loading AppSettings.json");
            }

            try
            {
                if (File.Exists(appSetupPath))
                {
                    string tokensJson = File.ReadAllText(appSetupPath);
                    AppSetup = JsonConvert.DeserializeObject<AppSetup>(tokensJson) ?? new AppSetup();
                    AppSetup.TwitchClient.ClientSecret = Secure.DecryptString(AppSetup.TwitchClient.ClientSecret);
                    AppSetup.SpotifyClient.ClientSecret = Secure.DecryptString(AppSetup.SpotifyClient.ClientSecret);

                    AppSetup.StreamerAccessTokens.AccessToken = Secure.DecryptString(AppSetup.StreamerAccessTokens.AccessToken);
                    AppSetup.StreamerAccessTokens.RefreshToken = Secure.DecryptString(AppSetup.StreamerAccessTokens.RefreshToken);

                    AppSetup.BotAccessTokens.AccessToken = Secure.DecryptString(AppSetup.BotAccessTokens.AccessToken);
                    AppSetup.BotAccessTokens.RefreshToken = Secure.DecryptString(AppSetup.BotAccessTokens.RefreshToken);

                    AppSetup.SpotifyAccessTokens.AccessToken = Secure.DecryptString(AppSetup.SpotifyAccessTokens.AccessToken);
                    AppSetup.SpotifyAccessTokens.RefreshToken = Secure.DecryptString(AppSetup.SpotifyAccessTokens.RefreshToken);
                }
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, "AppFilesService: Error loading AppSetup.json");
            }
        }

        public IEnumerable<string> GetAppLogs()
        {
            var fileContent = string.Empty;
            using (var f = new FileStream("./Logs/log.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var s = new StreamReader(f))
            {
                fileContent = s.ReadToEnd();
            }
            return fileContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
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

        public void SaveAppSetup()
        {
            // create a new copy instead of reference
            AppSetup appSetup = JsonConvert.DeserializeObject<AppSetup>(JsonConvert.SerializeObject(AppSetup)) ?? new AppSetup();

            appSetup.TwitchClient.ClientSecret = Secure.EncryptString(appSetup.TwitchClient.ClientSecret);
            appSetup.SpotifyClient.ClientSecret = Secure.EncryptString(appSetup.SpotifyClient.ClientSecret);

            appSetup.StreamerAccessTokens.AccessToken = Secure.EncryptString(appSetup.StreamerAccessTokens.AccessToken);
            appSetup.StreamerAccessTokens.RefreshToken = Secure.EncryptString(appSetup.StreamerAccessTokens.RefreshToken);

            appSetup.BotAccessTokens.AccessToken = Secure.EncryptString(appSetup.BotAccessTokens.AccessToken);
            appSetup.BotAccessTokens.RefreshToken = Secure.EncryptString(appSetup.BotAccessTokens.RefreshToken);

            appSetup.SpotifyAccessTokens.AccessToken = Secure.EncryptString(appSetup.SpotifyAccessTokens.AccessToken);
            appSetup.SpotifyAccessTokens.RefreshToken = Secure.EncryptString(appSetup.SpotifyAccessTokens.RefreshToken);
            
            string json = JsonConvert.SerializeObject(appSetup, Formatting.Indented);
            File.WriteAllText(appSetupPath, json);
        }

        public void ResetAppSetup()
        {
            if (!File.Exists(appSetupPath))
            {
                File.Delete(appSetupPath);
            }
            AppSetup = new AppSetup();
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
