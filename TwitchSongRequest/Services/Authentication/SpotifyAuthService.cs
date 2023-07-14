using Newtonsoft.Json;
using RestSharp;
using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchSongRequest.Helpers;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services.Authentication
{
    internal class SpotifyAuthService : ISpotifyAuthService
    {
        private const string RedirectUri = "http://localhost:8080";

        private readonly IAppSettingsService _appSettingsService;
        private readonly HttpListener _httpListener;

        public SpotifyAuthService(IAppSettingsService appSettingsService)
        {
            _appSettingsService = appSettingsService;
            _httpListener = new HttpListener() { Prefixes = { RedirectUri + "/" } };
        }

        public async Task<ServiceOAuthToken> GenerateOAuthTokens(CancellationToken cancellationToken)
        {
            ClientCredentials credentials = _appSettingsService.AppSettings.SpotifyClient;
            ClientInfo info = _appSettingsService.AppSettings.SpotifyInfo;

            // Open the browser window for authorization
            string authorizationUrl = $"https://accounts.spotify.com/authorize?client_id={credentials.ClientId}&redirect_uri={Uri.EscapeDataString(RedirectUri)}&response_type=code&scope={Uri.EscapeDataString(info.Scope!)}";
            WebBrowserLauncher.Launch(info.Browser, authorizationUrl);

            // Start the local HTTP server to handle the redirect
            string code = await StartHttpServer(cancellationToken);

            // Exchange the authorization code for an access token
            ServiceOAuthToken token = await ExchangeAuthorizationCodeForToken(code);
            return token;
        }

        public async Task<ServiceOAuthToken> RefreshOAuthTokens()
        {
            ServiceOAuthToken tokens = _appSettingsService.AppSettings.SpotifyAccessTokens;
            ClientCredentials credentials = _appSettingsService.AppSettings.SpotifyClient;

            var restClient = new RestClient("https://accounts.spotify.com/api/token");
            var request = new RestRequest("/", Method.Post);

            var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{credentials.ClientId}:{credentials.ClientSecret}"));
            request.AddHeader("Authorization", $"Basic {authHeader}");

            request.AddParameter("refresh_token", tokens.RefreshToken);
            request.AddParameter("grant_type", "refresh_token");

            var response = await restClient.ExecuteAsync<ServiceOAuthToken>(request);

            if (response.IsSuccessful && response.Data != null)
            {
                ServiceOAuthToken newTokens = response.Data;
                newTokens.RefreshToken = tokens.RefreshToken;
                return newTokens;
            }
            else
            {
                response.ErrorException!.Data.Add("Response", response.Content);
                throw response.ErrorException!;
            }
        }

        public async Task<string> ValidateOAuthTokens()
        {
            ServiceOAuthToken tokens = _appSettingsService.AppSettings.SpotifyAccessTokens;

            var restClient = new RestClient("https://api.spotify.com/v1/me");
            var request = new RestRequest("/", Method.Get);

            request.AddHeader("Authorization", $"Bearer {tokens.AccessToken}");

            var response = await restClient.ExecuteAsync(request);

            if (response.IsSuccessful && response.StatusCode == HttpStatusCode.OK && response.Content != null)
            {
                var obj = JsonConvert.DeserializeObject<dynamic>(response.Content);
                return obj.id;
            }
            else
            {
                response.ErrorException!.Data.Add("Response", response.Content);
                throw response.ErrorException!;
            }
        }
        private async Task<string> StartHttpServer(CancellationToken cancellationToken)
        {
            if (!_httpListener!.IsListening)
            {
                _httpListener.Start();
            }

            // Use a cancellation token source for the timeout functionality
            var cts = new CancellationTokenSource();

            try
            {
                // Handle the redirect asynchronously with timeout
                var contextTask = _httpListener.GetContextAsync();
                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(1), cts.Token);
                var cancelTask = Task.Delay(TimeSpan.FromMinutes(2), cancellationToken);
                var completedTask = await Task.WhenAny(contextTask, timeoutTask, cancelTask);

                if (completedTask == timeoutTask)
                {
                    throw new TaskCanceledException("Timeout while waiting for authorization code.");
                }

                if (completedTask == cancelTask)
                {
                    throw new TaskCanceledException("User cancelled authorization.");
                }

                // Proceed with handling the received callback
                var context = await contextTask;
                var request = context.Request;
                var response = context.Response;

                // Parse the authorization code from the query parameters
                string code = request.QueryString["code"];

                // Return a response to the browser
                byte[] responseBytes = Encoding.UTF8.GetBytes("Authorization successful. You may close this window.");
                response.ContentLength64 = responseBytes.Length;
                response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
                response.OutputStream.Close();

                return code;
            }
            finally
            {
                // Stop the listener and cancel the timeout task if it's still active
                _httpListener.Stop();
                cts.Cancel();
            }
        }

        private async Task<ServiceOAuthToken> ExchangeAuthorizationCodeForToken(string code)
        {
            ClientCredentials credentials = _appSettingsService.AppSettings.SpotifyClient;

            var restClient = new RestClient("https://accounts.spotify.com/api/token");
            var request = new RestRequest("/", Method.Post);

            var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{credentials.ClientId}:{credentials.ClientSecret}"));
            request.AddHeader("Authorization", $"Basic {authHeader}");

            request.AddParameter("code", code);
            request.AddParameter("grant_type", "authorization_code");
            request.AddParameter("redirect_uri", RedirectUri);

            var response = await restClient.ExecuteAsync<ServiceOAuthToken>(request);

            if (response.IsSuccessful && response.Data != null)
            {
                return response.Data;
            }
            else
            {
                response.ErrorException!.Data.Add("Response", response.Content);
                throw response.ErrorException!;
            }
        }
    }
}
