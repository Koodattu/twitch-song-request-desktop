using Newtonsoft.Json;
using RestSharp;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services
{
    internal class TwitchAuthService : ITwitchAuthService
    {
        private const string RedirectUri = "http://localhost:8080";
        HttpListener httpListener = new HttpListener() { Prefixes = { RedirectUri + "/"} };

        public async Task<TwitchAccessToken> GenerateTwitchAccesTokens(string twitchClientId, string twitchClientSecret, string scopes)
        {
            // Open the browser window for authorization
            string authorizationUrl = $"https://id.twitch.tv/oauth2/authorize?client_id={twitchClientId}&redirect_uri={Uri.EscapeDataString(RedirectUri)}&response_type=code&scope={Uri.EscapeDataString(scopes)}";
            Process.Start(new ProcessStartInfo(authorizationUrl) { UseShellExecute = true });

            // Start the local HTTP server to handle the redirect
            string code = await StartHttpServer();

            // Exchange the authorization code for an access token
            TwitchAccessToken token = ExchangeAuthorizationCodeForToken(twitchClientId, twitchClientSecret, code);
            return token;
        }

        public async Task<string?> ValidateTwitchAccessTokens(TwitchAccessToken accessTokens, string clientId, string clientSecret)
        {
            var client = new RestClient("https://id.twitch.tv/oauth2/validate");
            var request = new RestRequest("/", Method.Get);

            request.AddHeader("Authorization", $"OAuth {accessTokens.AccessToken}");

            var response = await client.ExecuteAsync<TwitchAccessToken>(request);

            if (response.IsSuccessful && response.StatusCode == HttpStatusCode.OK)
            {
                var obj = JsonConvert.DeserializeObject<dynamic>(response.Content);
                return obj.login;
            }
            else
            {
                throw new ValidationException($"Error validating Twitch OAuth access token: {response.ErrorMessage}");
            }
        }

        public Task<TwitchAccessToken> RefreshTwitchAccessTokens(TwitchAccessToken accessTokens, string twitchClientId, string twitchClientSecret)
        {
            throw new NotImplementedException();
        }

        private async Task<string> StartHttpServer()
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
                var completedTask = await Task.WhenAny(contextTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException("Timeout while waiting for authorization code.");
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

        private TwitchAccessToken ExchangeAuthorizationCodeForToken(string clientId, string clientSecret, string code)
        {
            var client = new RestClient("https://id.twitch.tv/oauth2/token");
            var request = new RestRequest("/", Method.Post);

            request.AddParameter("client_id", clientId);
            request.AddParameter("client_secret", clientSecret);
            request.AddParameter("code", code);
            request.AddParameter("grant_type", "authorization_code");
            request.AddParameter("redirect_uri", RedirectUri);

            var response = client.Execute<TwitchAccessToken>(request);

            if (response.IsSuccessful)
            {
                return response.Data;
            }
            else
            {
                throw new AuthenticationException($"Error exchanging Twitch authorization code: {response.ErrorMessage}");
            }
        }
    }
}
