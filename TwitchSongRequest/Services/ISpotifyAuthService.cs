using System.Threading.Tasks;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services
{
    internal interface ISpotifyAuthService
    {
        Task<SpotifyAccessToken> GenerateSpotifyAccesTokens(string clientId, string clientSecret, string scopes);
        Task<bool> ValidateSpotifyAccessTokens(SpotifyAccessToken accessTokens, string clientId, string clientSecret);
        Task<SpotifyAccessToken> RefreshSpotifyAccessTokens(SpotifyAccessToken accessTokens, string clientId, string clientSecret);
    }
}
