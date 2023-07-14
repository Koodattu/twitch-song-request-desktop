using System.Threading.Tasks;
using TwitchLib.Client;

namespace TwitchSongRequest.Services.Api
{
    internal interface ITwitchApiService
    {
        TwitchClient SetupTwitchStreamerClient();
        TwitchClient SetupTwitchBotClient();
        Task<string?> CreateReward(string name);
    }
}
