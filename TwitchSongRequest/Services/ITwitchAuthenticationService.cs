using System.Threading.Tasks;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services
{
    internal interface ITwitchAuthenticationService
    {
        internal Task<TwitchAccessToken> GetTwitchOAuthToken(string twitchClientId, string twitchClientSecret, string scope);
        internal Task<bool> ValidateTwitchOAuthToken(string twitchClientId, string twitchClientSecret, string scope);
        internal Task<bool> RefreshTwitchOAuthToken(string twitchClientId, string twitchClientSecret, string scope);
    }
}
