using Newtonsoft.Json;
using RestSharp;
using System;
using System.Net;
using System.Threading.Tasks;
using TwitchSongRequest.Model;
using TwitchSongRequest.Services.App;

namespace TwitchSongRequest.Services.Api
{
    internal class SpotifySongService : ISpotifySongService
    {
        private readonly IAppFilesService _appFilesService;

        public SpotifySongService(IAppFilesService appFilesService)
        {
            _appFilesService = appFilesService;
        }

        public async Task<int> GetPosition()
        {
            var restClient = new RestClient($"https://api.spotify.com/v1/me/player");
            var request = new RestRequest("/", Method.Get);

            string accessToken = _appFilesService.AppSetup.SpotifyAccessTokens.AccessToken!;
            request.AddHeader("Authorization", $"Bearer {accessToken}");

            var response = await restClient.ExecuteAsync(request);

            if (response.IsSuccessful && response.StatusCode == HttpStatusCode.OK && response.Content != null)
            {
                var obj = JsonConvert.DeserializeObject<dynamic>(response.Content);
                var duration = int.Parse(obj!.progress_ms.ToString()) / 1000;
                return duration;
            }
            else
            {
                response.ErrorException!.Data.Add("Response", response.Content);
                throw response.ErrorException!;
            }
        }

        public async Task<SongInfo> GetSongInfo(string id)
        {
            var restClient = new RestClient($"https://api.spotify.com/v1/tracks/{id}");
            var request = new RestRequest("/", Method.Get);

            string accessToken = _appFilesService.AppSetup.SpotifyAccessTokens.AccessToken!;
            request.AddHeader("Authorization", $"Bearer {accessToken}");

            var response = await restClient.ExecuteAsync(request);

            if (response.IsSuccessful && response.StatusCode == HttpStatusCode.OK && response.Content != null)
            {
                var obj = JsonConvert.DeserializeObject<dynamic>(response.Content);
                var name = obj!.name.ToString();
                var artist = obj!.artists[0].name.ToString();
                var duration = int.Parse(obj!.duration_ms.ToString()) / 1000;
                return new SongInfo(name, artist, duration);
            }
            else
            {
                response.ErrorException!.Data.Add("Response", response.Content);
                throw response.ErrorException!;
            }
        }

        public async Task<bool> PlaySong(string id)
        {
            var restClient = new RestClient($"https://api.spotify.com/v1/me/player/play");
            var request = new RestRequest("/", Method.Put);

            string accessToken = _appFilesService.AppSetup.SpotifyAccessTokens.AccessToken!;
            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddJsonBody(new { uris = new[] { $"spotify:track:{id}" }});

            var response = await restClient.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                response.ErrorException!.Data.Add("Response", response.Content);
                throw response.ErrorException!;
            }

            return response.StatusCode == HttpStatusCode.OK;
        }

        public async Task<bool> Play()
        {
            var restClient = new RestClient($"https://api.spotify.com/v1/me/player/play");
            var request = new RestRequest("/", Method.Put);

            string accessToken = _appFilesService.AppSetup.SpotifyAccessTokens.AccessToken!;
            request.AddHeader("Authorization", $"Bearer {accessToken}");

            var response = await restClient.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                response.ErrorException!.Data.Add("Response", response.Content);
                throw response.ErrorException!;
            }

            return response.StatusCode == HttpStatusCode.NoContent;
        }

        public async Task<bool> Pause()
        {
            var restClient = new RestClient($"https://api.spotify.com/v1/me/player/pause");
            var request = new RestRequest("/", Method.Put);

            string accessToken = _appFilesService.AppSetup.SpotifyAccessTokens.AccessToken!;
            request.AddHeader("Authorization", $"Bearer {accessToken}");

            var response = await restClient.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                response.ErrorException!.Data.Add("Response", response.Content);
                throw response.ErrorException!;
            }

            return response.StatusCode == HttpStatusCode.NoContent;
        }

        public async Task<bool> Skip()
        {
            var restClient = new RestClient($"https://api.spotify.com/v1/me/player/next");
            var request = new RestRequest("/", Method.Put);

            string accessToken = _appFilesService.AppSetup.SpotifyAccessTokens.AccessToken!;
            request.AddHeader("Authorization", $"Bearer {accessToken}");

            var response = await restClient.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                response.ErrorException!.Data.Add("Response", response.Content);
                throw response.ErrorException!;
            }

            return response.StatusCode == HttpStatusCode.OK;
        }

        public async Task<bool> AddSongToQueue(string id)
        {
            var restClient = new RestClient($"https://api.spotify.com/v1/me/player/queue?uri=spotify:track:{id}");
            var request = new RestRequest("/", Method.Post);

            string accessToken = _appFilesService.AppSetup.SpotifyAccessTokens.AccessToken!;
            request.AddHeader("Authorization", $"Bearer {accessToken}");

            var response = await restClient.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                response.ErrorException!.Data.Add("Response", response.Content);
                throw response.ErrorException!;
            }

            return response.StatusCode == HttpStatusCode.OK;
        }

        public async Task<bool> SetPosition(int position)
        {
            int positionMs = position * 1000;
            var restClient = new RestClient($"https://api.spotify.com/v1/me/player/seek?position_ms={positionMs}");
            var request = new RestRequest("/", Method.Put);

            string accessToken = _appFilesService.AppSetup.SpotifyAccessTokens.AccessToken!;
            request.AddHeader("Authorization", $"Bearer {accessToken}");

            var response = await restClient.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                response.ErrorException!.Data.Add("Response", response.Content);
                throw response.ErrorException!;
            }

            return response.StatusCode == HttpStatusCode.NoContent;
        }
    }
}
