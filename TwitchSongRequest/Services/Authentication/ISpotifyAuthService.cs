using System.Threading.Tasks;
using System.Threading;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services.Authentication
{
    internal interface ISpotifyAuthService
    {
        Task<ServiceOAuthToken> GenerateOAuthTokens(CancellationToken cancellationToken);
        Task<string> ValidateOAuthTokens();
        Task<ServiceOAuthToken> RefreshOAuthTokens();
    }
}
