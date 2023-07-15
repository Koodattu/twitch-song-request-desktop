using System.Threading.Tasks;
using TwitchLib.Client;

namespace TwitchSongRequest.Services.Api
{
    internal interface ITwitchApiService
    {
        Task<TwitchClient> GetTwitchStreamerClient();
        Task<TwitchClient> GetTwitchBotClient();
        Task ReplyToChatMessage(string channel, string replyId, string message);
        Task<string?> CreateReward(string name);
    }
}
