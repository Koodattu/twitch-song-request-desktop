using System;
using System.Threading;
using System.Threading.Tasks;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services
{
    internal class SpotifyAuthService : IAuthService
    {
        public async Task<ServiceOAuthToken> GenerateOAuthTokens(ClientCredentials credentials, ClientInfo info, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<ServiceOAuthToken> RefreshOAuthTokens(ServiceOAuthToken tokens, ClientCredentials credentials)
        {
            throw new NotImplementedException();
        }

        public async Task<string> ValidateOAuthTokens(ServiceOAuthToken tokens)
        {
            throw new NotImplementedException();
        }
    }
}
