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
using TwitchLib.Client.Events;
using TwitchSongRequest.Services.Authentication;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Api.Helix.Models.ChannelPoints.GetCustomRewardRedemption;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateRedemptionStatus;

namespace TwitchSongRequest.Services.Api
{
    internal class TwitchApiService : ITwitchApiService
    {
        private readonly IAppFilesService _appSettingsService;
        private readonly ILoggerService _loggerService;
        private readonly ITwitchAuthService _twitchAuthService;

        private TwitchClient? streamerClient;
        private TwitchClient? botClient;

        public event Action<ChatMessage> MessageEvent;
        public event Action<object?, OnLogArgs> LogEvent;

        public TwitchApiService(IAppFilesService appSettingsService, ILoggerService loggerService, ITwitchAuthService twitchAuthService)
        {
            _appSettingsService = appSettingsService;
            _loggerService = loggerService;
            _twitchAuthService = twitchAuthService;
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
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            TwitchClient twitchClient = new TwitchClient(customClient);
            ConnectionCredentials credentials = new ConnectionCredentials(clientName, accessToken);
            twitchClient.Initialize(credentials);
            twitchClient.OnLog += (s, e) => LogEvent.Invoke(s, e);
            twitchClient.OnConnected += (s, e) => twitchClient.JoinChannel(channelName);
            if (receiveMessageEvents)
            {
                twitchClient.OnMessageReceived += (s, e) => MessageEvent.Invoke(e.ChatMessage);
            }
            twitchClient.Connect();

            return twitchClient;
        }

        public void RefreshStreamerClientCredentials()
        {
            streamerClient?.Disconnect();
            string streamerName = _appSettingsService.AppSetup.StreamerInfo.AccountName!;
            string accessToken = _appSettingsService.AppSetup.StreamerAccessTokens.AccessToken!;
            ConnectionCredentials credentials = new ConnectionCredentials(streamerName, accessToken);
            streamerClient?.SetConnectionCredentials(credentials);
            streamerClient?.Connect();
        }

        public void RefreshBotClientCredentials()
        {
            botClient?.Disconnect();
            string botName = _appSettingsService.AppSetup.BotInfo.AccountName!;
            string accessToken = _appSettingsService.AppSetup.BotAccessTokens.AccessToken!;
            ConnectionCredentials credentials = new ConnectionCredentials(botName, accessToken);
            botClient?.SetConnectionCredentials(credentials);
            botClient?.Connect();
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

        public async Task<bool?> CompleteRedeem(string redeemer, string input, bool refund)
        {
            TwitchAPI twitchAPI = new TwitchAPI();
            twitchAPI.Settings.AccessToken = _appSettingsService.AppSetup.StreamerAccessTokens.AccessToken!;
            twitchAPI.Settings.ClientId = _appSettingsService.AppSetup.TwitchClient.ClientId!;
            string username = _appSettingsService.AppSetup.StreamerInfo.AccountName!;

            GetUsersResponse users = await twitchAPI.Helix.Users.GetUsersAsync(logins: new List<string>() { username });
            string broadcasterId = users.Users[0].Id;

            string rewardId = _appSettingsService.AppSetup.ChannelRedeemRewardId!;

            GetCustomRewardRedemptionResponse redeems = await twitchAPI.Helix.ChannelPoints.GetCustomRewardRedemptionAsync(broadcasterId, rewardId, status: "UNFULFILLED", first: "50", sort: "NEWEST");

            RewardRedemption? redeem = redeems.Data.FirstOrDefault(x => string.Equals(x.UserName, redeemer, StringComparison.OrdinalIgnoreCase) && x.UserInput.Contains(input, StringComparison.OrdinalIgnoreCase));
            if (redeem == null)
            {
                _loggerService.LogError($"Redeem not found for user {redeemer} with input {input}");
                return false;
            }

            CustomRewardRedemptionStatus customRewardRedemptionStatus = refund ? CustomRewardRedemptionStatus.CANCELED : CustomRewardRedemptionStatus.FULFILLED;
            UpdateRedemptionStatusResponse response = await twitchAPI.Helix.ChannelPoints.UpdateRedemptionStatusAsync(broadcasterId, rewardId, new List<string>() { redeem.Id }, new UpdateCustomRewardRedemptionStatusRequest
            {
                Status = customRewardRedemptionStatus
            });

            return response.Data[0].Status == customRewardRedemptionStatus;
        }

        public async Task<bool?> RefundRedeems(List<SongRequest> requests)
        {
            TwitchAPI twitchAPI = new TwitchAPI();
            twitchAPI.Settings.AccessToken = _appSettingsService.AppSetup.StreamerAccessTokens.AccessToken!;
            twitchAPI.Settings.ClientId = _appSettingsService.AppSetup.TwitchClient.ClientId!;
            string username = _appSettingsService.AppSetup.StreamerInfo.AccountName!;

            GetUsersResponse users = await twitchAPI.Helix.Users.GetUsersAsync(logins: new List<string>() { username });
            string broadcasterId = users.Users[0].Id;

            string rewardId = _appSettingsService.AppSetup.ChannelRedeemRewardId!;

            List<string> redeemIds = new List<string>();

            GetCustomRewardRedemptionResponse redeems = await twitchAPI.Helix.ChannelPoints.GetCustomRewardRedemptionAsync(broadcasterId, rewardId, status: "UNFULFILLED", first: "50", sort: "NEWEST");

            foreach (SongRequest request in requests)
            {
                RewardRedemption? redeem = redeems.Data.FirstOrDefault(x => string.Equals(x.UserName, request.Requester, StringComparison.OrdinalIgnoreCase) && x.UserInput == request.RequestInput);
                if (redeem == null)
                {
                    _loggerService.LogError($"Redeem not found for user {request.Requester} with input {request.RequestInput}");
                }
                else
                {
                    redeemIds.Add(redeem.Id);
                }
            }

            UpdateRedemptionStatusResponse response = await twitchAPI.Helix.ChannelPoints.UpdateRedemptionStatusAsync(broadcasterId, rewardId, redeemIds, new UpdateCustomRewardRedemptionStatusRequest
            {
                Status = CustomRewardRedemptionStatus.CANCELED
            });

            return response.Data.All(x => x.Status == CustomRewardRedemptionStatus.CANCELED);
        }

        public async Task SendChatMessage(string channel, string message, string? replyId = null)
        {
            TwitchClient twitchClient = _appSettingsService.AppSettings.ReplyWithBot ? botClient! : streamerClient!;

            // check that client is connected
            if (!twitchClient.IsConnected)
            {
                throw new Exception("Twitch client is not connected");
            }

            // check that we have joined the channel before sending a message
            if (!twitchClient.JoinedChannels.Any(x => x.Channel == channel))
            {
                throw new Exception($"Twitch client has not joined channel {channel}");
            }

            if (replyId != null)
            {
                await Task.Run(() => twitchClient.SendReply(channel, replyId, message));
            }
            else
            {
                await Task.Run(() => twitchClient.SendMessage(channel, message));
            }
        }
    }
}
