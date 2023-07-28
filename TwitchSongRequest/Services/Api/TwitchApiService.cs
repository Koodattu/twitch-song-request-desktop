using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchLib.Api;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomRewardRedemptionStatus;
using TwitchLib.Api.Core.Enums;

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

        public async Task<TwitchClient> GetTwitchStreamerClient()
        {
            if (streamerClient == null)
            {
                string streamerName = _appSettingsService.AppTokens.StreamerInfo.AccountName!;
                string accessToken = _appSettingsService.AppTokens.StreamerAccessTokens.AccessToken!;
                streamerClient = await Task.Run(() => SetupTwitchClient(streamerName, accessToken, streamerName));
            }
            
            return streamerClient;
        }

        public async Task<TwitchClient> GetTwitchBotClient()
        {
            if (botClient == null)
            {
                string botName = _appSettingsService.AppTokens.BotInfo.AccountName!;
                string accessToken = _appSettingsService.AppTokens.BotAccessTokens.AccessToken!;
                string streamerName = _appSettingsService.AppTokens.StreamerInfo.AccountName!;
                botClient = await Task.Run(() => SetupTwitchClient(botName, accessToken, streamerName));
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
            twitchAPI.Settings.AccessToken = _appSettingsService.AppTokens.StreamerAccessTokens.AccessToken!;
            twitchAPI.Settings.ClientId = _appSettingsService.AppTokens.TwitchClient.ClientId!;

            string username = _appSettingsService.AppTokens.StreamerInfo.AccountName!;
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

        public async Task<bool?> RefundRedeem(string redeemer, string input)
        {
            TwitchAPI twitchAPI = new TwitchAPI();
            twitchAPI.Settings.AccessToken = _appSettingsService.AppTokens.StreamerAccessTokens.AccessToken!;
            twitchAPI.Settings.ClientId = _appSettingsService.AppTokens.TwitchClient.ClientId!;
            string username = _appSettingsService.AppTokens.StreamerInfo.AccountName!;

            var users = await twitchAPI.Helix.Users.GetUsersAsync(logins: new List<string>() { username });
            string broadcasterId = users.Users[0].Id;

            string rewardId = _appSettingsService.AppTokens.ChannelRedeemRewardId!;

            var redeems = await twitchAPI.Helix.ChannelPoints.GetCustomRewardRedemptionAsync(broadcasterId, rewardId);

            var redeem = redeems.Data.FirstOrDefault(x => x.UserName == redeemer && x.UserInput == input);
            if (redeem == null)
            {
                throw new Exception($"Redeem not found for user {redeemer} with input {input}");
            }
            var response = await twitchAPI.Helix.ChannelPoints.UpdateRedemptionStatusAsync(broadcasterId, rewardId, new List<string>() { redeem.Id }, new UpdateCustomRewardRedemptionStatusRequest
            {
                Status = CustomRewardRedemptionStatus.CANCELED
            });

            return response.Data[0].Status == CustomRewardRedemptionStatus.CANCELED;
        }

        public async Task ReplyToChatMessage(string channel, string replyId, string message)
        {
            TwitchClient twitchClient = _appSettingsService.AppSettings.ReplyWithBot ? botClient! : streamerClient!;

            // check that we have joined the channel before sending a message
            if (twitchClient.JoinedChannels.Any(x => x.Channel == channel))
            {
                await Task.Run(() => twitchClient.SendReply(channel, replyId, message));
            }
            else
            {
                throw new Exception($"Twitch client has not joined channel {channel}");
            }
        }
    }
}
