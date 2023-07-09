using System.Threading;
using System.Threading.Tasks;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services
{
    internal interface IAuthService
    {
        Task<ServiceOAuthToken> GenerateOAuthTokens(ClientCredentials credentials, ClientInfo info, CancellationToken cancellationToken);
        Task<string> ValidateOAuthTokens(ServiceOAuthToken tokens);
        Task<ServiceOAuthToken> RefreshOAuthTokens(ServiceOAuthToken tokens, ClientCredentials credentials);
    }
}
