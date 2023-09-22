using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services.Api
{
    internal interface ITwitchApiService
    {
        event Action<ChatMessage> MessageEvent;
        event Action<object?, OnLogArgs> LogEvent;
        Task<TwitchClient> GetTwitchStreamerClient();
        Task<TwitchClient> GetTwitchBotClient();
        void RefreshStreamerClientCredentials();
        void RefreshBotClientCredentials();
        Task SendChatMessage(string channel, string message, string? replyId = null);
        Task<string?> CreateReward(string name);
        Task<bool?> CompleteRedeem(string redeemer, string input, bool refund);
        Task<bool?> RefundRedeems(List<SongRequest> requests);
    }
}
