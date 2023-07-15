using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchLib.Api;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace TwitchSongRequest.Services.Api
{
    internal class TwitchApiService : ITwitchApiService
    {
        private readonly IAppSettingsService _appSettingsService;

        private TwitchClient? streamerClient;
        private TwitchClient? botClient;

        public TwitchApiService(IAppSettingsService appSettingsService)
        {
            _appSettingsService = appSettingsService;
        }

        public TwitchClient GetTwitchStreamerClient()
        {
            if (streamerClient == null)
            {
                string streamerName = _appSettingsService.AppSettings.StreamerInfo.AccountName!;
                string accessToken = _appSettingsService.AppSettings.StreamerAccessTokens.AccessToken!;
                streamerClient = SetupTwitchClient(streamerName, accessToken, streamerName);
            }
            
            return streamerClient;
        }

        public TwitchClient GetTwitchBotClient()
        {
            if (botClient == null)
            {
                string botName = _appSettingsService.AppSettings.BotInfo.AccountName!;
                string accessToken = _appSettingsService.AppSettings.BotAccessTokens.AccessToken!;
                string streamerName = _appSettingsService.AppSettings.StreamerInfo.AccountName!;
                botClient = SetupTwitchClient(botName, accessToken, streamerName);
            }

            return botClient;
        }

        private TwitchClient SetupTwitchClient(string clientName, string accessToken, string channelName)
        {
            ConnectionCredentials credentials = new ConnectionCredentials(clientName, accessToken);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            TwitchClient twitchClient = new TwitchClient(customClient);
            twitchClient.Initialize(credentials, channelName);
            twitchClient.Connect();

            return twitchClient;
        }

        public async Task<string?> CreateReward(string name)
        {
            TwitchAPI twitchAPI = new TwitchAPI(); 
            twitchAPI.Settings.AccessToken = _appSettingsService.AppSettings.StreamerAccessTokens.AccessToken!;
            twitchAPI.Settings.ClientId = _appSettingsService.AppSettings.TwitchClient.ClientId!;

            string username = _appSettingsService.AppSettings.StreamerInfo.AccountName!;
            var users = await twitchAPI.Helix.Users.GetUsersAsync(logins: new List<string>() { username });
            string broadcasterId = users.Users[0].Id;

            var rewards = await twitchAPI.Helix.ChannelPoints.GetCustomRewardAsync(broadcasterId);

            bool rewardExists = rewards.Data.Any(x => x.Title == name);

            if (rewardExists)
            {
                return null;
            }

            var request = new TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward.CreateCustomRewardsRequest
            {
                Title = name,
                Cost = 100,
                Prompt = "Song Request",
                IsEnabled = true,
                IsUserInputRequired = true
            };

            var response = await twitchAPI.Helix.ChannelPoints.CreateCustomRewardsAsync(broadcasterId, request);

            if (response.Data.Length == 1)
            {
                return response.Data[0].Id;
            }

            return null;
        }
    }
}
