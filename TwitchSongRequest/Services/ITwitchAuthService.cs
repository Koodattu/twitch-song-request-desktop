using System.Threading.Tasks;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services
{
    internal interface ITwitchAuthService
    {
        Task<TwitchAccessToken> GenerateTwitchAccesTokens(string twitchClientId, string twitchClientSecret, string scopes);
        Task<bool> ValidateTwitchAccessTokens(TwitchAccessToken accessTokens, string twitchClientId, string twitchClientSecret);
        Task<TwitchAccessToken> RefreshTwitchAccessTokens(TwitchAccessToken accessTokens, string twitchClientId, string twitchClientSecret);
    }
}
