using System.Threading.Tasks;
using System.Threading;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services.Authentication
{
    internal interface ITwitchAuthService
    {
        Task<ServiceOAuthToken> GenerateStreamerOAuthTokens(CancellationToken cancellationToken);
        Task<ServiceOAuthToken> GenerateBotOAuthTokens(CancellationToken cancellationToken);
        Task<string> ValidateStreamerOAuthTokens();
        Task<string> ValidateBotOAuthTokens();
        Task<ServiceOAuthToken> RefreshStreamerOAuthTokens();
        Task<ServiceOAuthToken> RefreshBotOAuthTokens();
    }
}
