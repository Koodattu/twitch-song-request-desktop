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
using TwitchSongRequest.Services.App;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services.Api
{
    internal class TwitchApiService : ITwitchApiService
    {
        private readonly IAppFilesService _appSettingsService;
        private readonly ILoggerService _loggerService;

        private TwitchClient? streamerClient;
        private TwitchClient? botClient;

        public event Action<ChatMessage> MessageEvent;

        public TwitchApiService(IAppFilesService appSettingsService, ILoggerService loggerService)
        {
            _appSettingsService = appSettingsService;
            _loggerService = loggerService;
        }

        public async Task<TwitchClient> GetTwitchStreamerClient()
        {
            if (streamerClient == null)
            {
                string streamerName = _appSettingsService.AppSetup.StreamerInfo.AccountName!;
                string accessToken = _appSettingsService.AppSetup.StreamerAccessTokens.AccessToken!;
                streamerClient = await Task.Run(() => SetupTwitchClient(streamerName, accessToken, streamerName, true));
            }
            
            return streamerClient;
        }

        public async Task<TwitchClient> GetTwitchBotClient()
        {
            if (botClient == null)
            {
                string botName = _appSettingsService.AppSetup.BotInfo.AccountName!;
                string accessToken = _appSettingsService.AppSetup.BotAccessTokens.AccessToken!;
                string streamerName = _appSettingsService.AppSetup.StreamerInfo.AccountName!;
                botClient = await Task.Run(() => SetupTwitchClient(botName, accessToken, streamerName));
            }

            return botClient;
        }

        private TwitchClient SetupTwitchClient(string clientName, string accessToken, string channelName, bool receiveMessageEvents = false)
        {
            ConnectionCredentials credentials = new ConnectionCredentials(clientName, accessToken);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            TwitchClient twitchClient = new TwitchClient(customClient);
            twitchClient.Initialize(credentials);
            twitchClient.OnConnected += (s, e) => twitchClient.JoinChannel(channelName);
            if (receiveMessageEvents)
            {
                twitchClient.OnMessageReceived += (s, e) => MessageEvent.Invoke(e.ChatMessage);
            }
            twitchClient.Connect();

            return twitchClient;
        }

        public async Task<string?> CreateReward(string name)
        {
            TwitchAPI twitchAPI = new TwitchAPI(); 
            twitchAPI.Settings.AccessToken = _appSettingsService.AppSetup.StreamerAccessTokens.AccessToken!;
            twitchAPI.Settings.ClientId = _appSettingsService.AppSetup.TwitchClient.ClientId!;

            string username = _appSettingsService.AppSetup.StreamerInfo.AccountName!;
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
            twitchAPI.Settings.AccessToken = _appSettingsService.AppSetup.StreamerAccessTokens.AccessToken!;
            twitchAPI.Settings.ClientId = _appSettingsService.AppSetup.TwitchClient.ClientId!;
            string username = _appSettingsService.AppSetup.StreamerInfo.AccountName!;

            var users = await twitchAPI.Helix.Users.GetUsersAsync(logins: new List<string>() { username });
            string broadcasterId = users.Users[0].Id;

            string rewardId = _appSettingsService.AppSetup.ChannelRedeemRewardId!;

            var redeems = await twitchAPI.Helix.ChannelPoints.GetCustomRewardRedemptionAsync(broadcasterId, rewardId, status: "UNFULFILLED");

            var redeem = redeems.Data.FirstOrDefault(x => string.Equals(x.UserName, redeemer, StringComparison.OrdinalIgnoreCase) && x.UserInput.Contains(input, StringComparison.OrdinalIgnoreCase));
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

        public async Task<bool?> RefundRedeems(List<SongRequest> requests)
        {
            TwitchAPI twitchAPI = new TwitchAPI();
            twitchAPI.Settings.AccessToken = _appSettingsService.AppSetup.StreamerAccessTokens.AccessToken!;
            twitchAPI.Settings.ClientId = _appSettingsService.AppSetup.TwitchClient.ClientId!;
            string username = _appSettingsService.AppSetup.StreamerInfo.AccountName!;

            var users = await twitchAPI.Helix.Users.GetUsersAsync(logins: new List<string>() { username });
            string broadcasterId = users.Users[0].Id;

            string rewardId = _appSettingsService.AppSetup.ChannelRedeemRewardId!;

            var redeemIds = new List<string>();

            var redeems = await twitchAPI.Helix.ChannelPoints.GetCustomRewardRedemptionAsync(broadcasterId, rewardId, status: "UNFULFILLED");

            foreach (var request in requests)
            {
                var redeem = redeems.Data.FirstOrDefault(x => string.Equals(x.UserName, request.Requester, StringComparison.OrdinalIgnoreCase) && x.UserInput == request.RequestInput);
                if (redeem == null)
                {
                    throw new Exception($"Redeem not found for user {request.Requester} with input {request.RequestInput}");
                }
                redeemIds.Add(redeem.Id);
            }

            var response = await twitchAPI.Helix.ChannelPoints.UpdateRedemptionStatusAsync(broadcasterId, rewardId, redeemIds, new UpdateCustomRewardRedemptionStatusRequest
            {
                Status = CustomRewardRedemptionStatus.CANCELED
            });

            return response.Data.All(x => x.Status == CustomRewardRedemptionStatus.CANCELED);
        }

        public async Task ReplyToChatMessage(string channel, string replyId, string message)
        {
            TwitchClient twitchClient = _appSettingsService.AppSettings.ReplyWithBot ? botClient! : streamerClient!;

            try
            {
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
            catch (Exception ex)
            {
                _loggerService.LogError(ex, $"Error sending message to channel {channel}");
            }
        }
    }
}
