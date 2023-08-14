using Newtonsoft.Json;
using RestSharp;
using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchSongRequest.Helpers;
using TwitchSongRequest.Model;
using TwitchSongRequest.Services.App;

namespace TwitchSongRequest.Services.Authentication
{
    internal class TwitchAuthService : ITwitchAuthService
    {
        private const string RedirectUri = "http://localhost:8080";

        private readonly IAppFilesService _appSettingsService;
        private readonly HttpListener _httpListener;

        public TwitchAuthService(IAppFilesService appSettingsService)
        {
            _appSettingsService = appSettingsService;
            _httpListener = new HttpListener() { Prefixes = { RedirectUri + "/" } };
        }

        public Task<ServiceOAuthToken> GenerateStreamerOAuthTokens(CancellationToken cancellationToken)
        {
            string clientId = _appSettingsService.AppSetup.TwitchClient.ClientId!;
            string scope = _appSettingsService.AppSetup.StreamerInfo.Scope!;
            WebBrowser webBrowser = _appSettingsService.AppSetup.StreamerInfo.Browser;
            return GenerateOAuthTokens(clientId, scope, webBrowser, cancellationToken);
        }

        public Task<ServiceOAuthToken> GenerateBotOAuthTokens(CancellationToken cancellationToken)
        {
            string clientId = _appSettingsService.AppSetup.TwitchClient.ClientId!;
            string scope = _appSettingsService.AppSetup.BotInfo.Scope!;
            WebBrowser webBrowser = _appSettingsService.AppSetup.BotInfo.Browser;
            return GenerateOAuthTokens(clientId, scope, webBrowser, cancellationToken);
        }

        public Task<string> ValidateStreamerOAuthTokens()
        {
            string accessToken = _appSettingsService.AppSetup.StreamerAccessTokens.AccessToken!;
            return ValidateOAuthTokens(accessToken);
        }

        public Task<string> ValidateBotOAuthTokens()
        {
            string accessToken = _appSettingsService.AppSetup.BotAccessTokens.AccessToken!;
            return ValidateOAuthTokens(accessToken);
        }

        public Task<ServiceOAuthToken> RefreshStreamerOAuthTokens()
        {
            string clientId = _appSettingsService.AppSetup.TwitchClient.ClientId!;
            string clientSecret = _appSettingsService.AppSetup.TwitchClient.ClientSecret!;
            string refreshToken = _appSettingsService.AppSetup.StreamerAccessTokens.RefreshToken!;
            return RefreshOAuthTokens(clientId, clientSecret, refreshToken);
        }

        public Task<ServiceOAuthToken> RefreshBotOAuthTokens()
        {
            string clientId = _appSettingsService.AppSetup.TwitchClient.ClientId!;
            string clientSecret = _appSettingsService.AppSetup.TwitchClient.ClientSecret!;
            string refreshToken = _appSettingsService.AppSetup.BotAccessTokens.RefreshToken!;
            return RefreshOAuthTokens(clientId, clientSecret, refreshToken);
        }

        private async Task<ServiceOAuthToken> GenerateOAuthTokens(string clientId, string scope, WebBrowser webBrowser, CancellationToken cancellationToken)
        {
            // Open the browser window for authorization
            string authorizationUrl = $"https://id.twitch.tv/oauth2/authorize?client_id={clientId}&redirect_uri={Uri.EscapeDataString(RedirectUri)}&response_type=code&scope={Uri.EscapeDataString(scope)}";
            WebBrowserLauncher.Launch(webBrowser, authorizationUrl);

            // Start the local HTTP server to handle the redirect
            string code = await StartHttpServer(cancellationToken);

            // Exchange the authorization code for an access token
            ServiceOAuthToken token = await ExchangeAuthorizationCodeForToken(code);
            return token;
        }

        private async Task<string> ValidateOAuthTokens(string accessToken)
        {
            var restClient = new RestClient("https://id.twitch.tv/oauth2/validate");
            var request = new RestRequest("/", Method.Get);

            request.AddHeader("Authorization", $"OAuth {accessToken}");

            var response = await restClient.ExecuteAsync(request);

            if (response.IsSuccessful && response.StatusCode == HttpStatusCode.OK && response.Content != null)
            {
                var obj = JsonConvert.DeserializeObject<dynamic>(response.Content);
                return obj.login;
            }
            else
            {
                response.ErrorException!.Data.Add("Response", response.Content);
                throw response.ErrorException!;
            }
        }

        private async Task<ServiceOAuthToken> RefreshOAuthTokens(string clientId, string clientSecret, string refreshToken)
        {
            var restClient = new RestClient("https://id.twitch.tv/oauth2/token");
            var request = new RestRequest("/", Method.Post);
            request.AddParameter("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("client_id", clientId);
            request.AddParameter("client_secret", clientSecret);
            request.AddParameter("refresh_token", refreshToken);
            request.AddParameter("grant_type", "refresh_token");

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
            ClientCredentials credentials = _appSettingsService.AppSetup.TwitchClient;

            var restClient = new RestClient("https://id.twitch.tv/oauth2/token");
            var request = new RestRequest("/", Method.Post);

            request.AddParameter("client_id", credentials.ClientId);
            request.AddParameter("client_secret", credentials.ClientSecret);
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
