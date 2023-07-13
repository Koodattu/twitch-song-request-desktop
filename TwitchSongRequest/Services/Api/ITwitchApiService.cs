using System.Threading.Tasks;

namespace TwitchSongRequest.Services.Api
{
    internal interface ITwitchApiService
    {
        Task<bool> CreateReward(string name, string accessToken, int broadcasterId, string clientId);
    }
}
