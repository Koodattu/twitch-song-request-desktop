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

        public Task<string> GetPlaybackDevice()
        {
            throw new NotImplementedException();
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

        public Task<int> GetVolume()
        {
            throw new NotImplementedException();
        }

        public Task<bool> Pause()
        {
            throw new NotImplementedException();
        }

        public Task<bool> Play()
        {
            throw new NotImplementedException();
        }

        public Task<bool> PlaySong(string id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetPlaybackDevice(string device)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetPosition(int position)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetVolume(int volume)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Skip()
        {
            throw new NotImplementedException();
        }
    }
}
