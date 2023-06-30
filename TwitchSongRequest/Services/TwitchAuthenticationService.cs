using CefSharp.DevTools.Accessibility;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services
{
    internal class TwitchAuthenticationService : ITwitchAuthenticationService
    {
        //private const string Scopes = "user:read:email";
        private const string RedirectUri = "http://localhost:8080";

        Task<TwitchAccessToken> ITwitchAuthenticationService.GetTwitchOAuthToken(string twitchClientId, string twitchClientSecret, string scopes)
        {
            // Open the browser window for authorization
            string authorizationUrl = $"https://id.twitch.tv/oauth2/authorize?client_id={twitchClientId}&redirect_uri={Uri.EscapeUriString(RedirectUri)}&response_type=code&scope={Uri.EscapeUriString(scopes)}";
            Process.Start(new ProcessStartInfo(authorizationUrl) { UseShellExecute = true });

            // Start the local HTTP server to handle the redirect
            string code = StartHttpServer();

            // Exchange the authorization code for an access token
            TwitchAccessToken token = ExchangeAuthorizationCodeForToken(twitchClientId, twitchClientSecret, code);
            return Task.FromResult(token);
        }

        Task<bool> ITwitchAuthenticationService.ValidateTwitchOAuthToken(string twitchClientId, string twitchClientSecret, string scope)
        {
            throw new NotImplementedException();
        }

        Task<bool> ITwitchAuthenticationService.RefreshTwitchOAuthToken(string twitchClientId, string twitchClientSecret, string scope)
        {
            throw new NotImplementedException();
        }

        private string StartHttpServer()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add(RedirectUri + "/");
            listener.Start();

            // Handle the redirect synchronously
            var context = listener.GetContext();
            var request = context.Request;
            var response = context.Response;

            // Parse the authorization code from the query parameters
            string code = request.QueryString["code"];

            // Return a response to the browser
            byte[] responseBytes = Encoding.UTF8.GetBytes("Authorization successful. You may close this window.");
            response.ContentLength64 = responseBytes.Length;
            response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
            response.OutputStream.Close();

            listener.Stop();

            return code;
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
                throw new Exception($"Error exchanging authorization code: {response.ErrorMessage}");
            }
        }
    }
}
