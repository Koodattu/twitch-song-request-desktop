using Newtonsoft.Json;
using RestSharp;
using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchSongRequest.Helpers;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services
{
    internal class TwitchAuthService : IAuthService
    {
        private const string RedirectUri = "http://localhost:8080";
        private readonly HttpListener httpListener = new HttpListener() { Prefixes = { RedirectUri + "/"} };

        public async Task<ServiceOAuthToken> GenerateOAuthTokens(ClientCredentials credentials, ClientInfo info, CancellationToken cancellationToken)
        {
            // Open the browser window for authorization
            string authorizationUrl = $"https://id.twitch.tv/oauth2/authorize?client_id={credentials.ClientId}&redirect_uri={Uri.EscapeDataString(RedirectUri)}&response_type=code&scope={Uri.EscapeDataString(info.Scope!)}";
            WebBrowserLauncher.Launch(info.Browser, authorizationUrl);

            // Start the local HTTP server to handle the redirect
            string code = await StartHttpServer(cancellationToken);

            // Exchange the authorization code for an access token
            ServiceOAuthToken token = ExchangeAuthorizationCodeForToken(credentials.ClientId!, credentials.ClientSecret!, code);
            return token;
        }

        public async Task<string> ValidateOAuthTokens(ServiceOAuthToken tokens)
        {
            var restClient = new RestClient("https://id.twitch.tv/oauth2/validate");
            var request = new RestRequest("/", Method.Get);

            request.AddHeader("Authorization", $"OAuth {tokens.AccessToken}");

            var response = await restClient.ExecuteAsync(request);

            if (response.IsSuccessful && response.StatusCode == HttpStatusCode.OK && response.Content != null)
            {
                var obj = JsonConvert.DeserializeObject<dynamic>(response.Content);
                return obj.login;
            }
            else
            {
                throw new HttpResponseException(response.StatusCode, $"Error validating Twitch OAuth access token: {response.ErrorMessage}");
            }
        }

        public async Task<ServiceOAuthToken> RefreshOAuthTokens(ServiceOAuthToken tokens, ClientCredentials credentials)
        {
            var restClient = new RestClient("https://id.twitch.tv/oauth2/token");
            var request = new RestRequest("/", Method.Post);
            request.AddParameter("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("client_id", credentials.ClientId);
            request.AddParameter("client_secret", credentials.ClientSecret);
            request.AddParameter("refresh_token", tokens.RefreshToken);
            request.AddParameter("grant_type", "refresh_token");

            var response = await restClient.ExecuteAsync<ServiceOAuthToken>(request);

            if (response.IsSuccessful && response.Data != null)
            {
                return response.Data;
            }
            else
            {
                throw new HttpResponseException(response.StatusCode, $"Error exchanging Twitch authorization code: {response.ErrorMessage}");
            }
        }

        private async Task<string> StartHttpServer(CancellationToken cancellationToken)
        {
            if (!httpListener!.IsListening)
            {
                httpListener.Start();
            }

            // Use a cancellation token source for the timeout functionality
            var cts = new CancellationTokenSource();

            try
            {
                // Handle the redirect asynchronously with timeout
                var contextTask = httpListener.GetContextAsync();
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
                httpListener.Stop();
                cts.Cancel();
            }
        }

        private ServiceOAuthToken ExchangeAuthorizationCodeForToken(string clientId, string clientSecret, string code)
        {
            var restClient = new RestClient("https://id.twitch.tv/oauth2/token");
            var request = new RestRequest("/", Method.Post);

            request.AddParameter("client_id", clientId);
            request.AddParameter("client_secret", clientSecret);
            request.AddParameter("code", code);
            request.AddParameter("grant_type", "authorization_code");
            request.AddParameter("redirect_uri", RedirectUri);

            var response = restClient.Execute<ServiceOAuthToken>(request);

            if (response.IsSuccessful && response.Data != null)
            {
                return response.Data;
            }
            else
            {
                throw new HttpResponseException(response.StatusCode, $"Error exchanging Twitch authorization code: {response.ErrorMessage}");
            }
        }
    }
}
