﻿using Newtonsoft.Json;
using RestSharp;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using TwitchSongRequest.Model;
using TwitchSongRequest.Services.App;
using TwitchSongRequest.Services.Authentication;

namespace TwitchSongRequest.Services.Api
{
    internal class SpotifySongService : ISpotifySongService
    {
        private readonly IAppFilesService _appFilesService;
        private readonly ISpotifyAuthService _spotifyAuthService;

        public SpotifySongService(IAppFilesService appFilesService, ISpotifyAuthService spotifyAuthService)
        {
            _appFilesService = appFilesService;
            _spotifyAuthService = spotifyAuthService;
        }

        private async Task<RestResponse> SendRestRequest(string resource, Method method, string? jsonBody = null)
        {
            var restClient = new RestClient($"https://api.spotify.com/v1");
            var request = new RestRequest(resource, method);

            string accessToken = _appFilesService.AppSetup.SpotifyAccessTokens.AccessToken!;
            request.AddHeader("Authorization", $"Bearer {accessToken}");

            if (jsonBody != null)
            {
                request.AddJsonBody(jsonBody);
            }

            RestResponse? response = await restClient.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                // If the access token has expired, refresh it and try again
                if (response.StatusCode == HttpStatusCode.Unauthorized && response.Content != null && response.Content.Contains("expired"))
                {
                    ServiceOAuthToken spotifyTokens = await _spotifyAuthService.RefreshOAuthTokens();
                    _appFilesService.AppSetup.SpotifyAccessTokens = spotifyTokens;
                    return await SendRestRequest(resource, method, jsonBody);
                }
                else
                {
                    response.ErrorException!.Data.Add("Response", response.Content);
                    throw response.ErrorException!;
                }
            }

            return response;
        }

        public async Task<int> GetPosition()
        {
            RestResponse? response = await SendRestRequest($"/me/player?device_id={_appFilesService.AppSetup.SpotifyDevice}", Method.Get);

            if (string.IsNullOrWhiteSpace(response.Content))
            {
                response.ErrorException!.Data.Add("Response", response.Content);
                throw response.ErrorException!;
            }

            var obj = JsonConvert.DeserializeObject<dynamic>(response.Content);
            var duration = int.Parse(obj!.progress_ms.ToString()) / 1000;
            return duration;
        }

        public async Task<SongInfo> GetSongInfo(string id)
        {
            RestResponse? response = await SendRestRequest($"/tracks/{id}", Method.Get);

            if (string.IsNullOrWhiteSpace(response.Content))
            {
                response.ErrorException!.Data.Add("Response", response.Content);
                throw response.ErrorException!;
            }

            var obj = JsonConvert.DeserializeObject<dynamic>(response.Content);
            var name = obj!.name.ToString();
            var artist = obj!.artists[0].name.ToString();
            var duration = int.Parse(obj!.duration_ms.ToString()) / 1000;
            return new SongInfo(name, artist, duration, id);
        }

        public async Task<bool> PlaySong(string id)
        {
            string json = JsonConvert.SerializeObject(new { uris = new[] { $"spotify:track:{id}" } });
            RestResponse? response = await SendRestRequest($"/me/player/play?device_id={_appFilesService.AppSetup.SpotifyDevice}", Method.Put, json);
            return response.StatusCode == HttpStatusCode.NoContent;
        }

        public async Task<bool> Play()
        {
            SpotifyState? spotifyState = await GetSpotifyState();
            if (spotifyState?.is_playing == true)
            {
                return true;
            }

            RestResponse? response = await SendRestRequest($"/me/player/play?device_id={_appFilesService.AppSetup.SpotifyDevice}", Method.Put);
            return response.StatusCode == HttpStatusCode.NoContent;
        }

        public async Task<bool> Pause()
        {
            SpotifyState? spotifyState = await GetSpotifyState();
            if (spotifyState?.is_playing == false)
            {
                return true;
            }

            RestResponse? response = await SendRestRequest($"/me/player/pause?device_id={_appFilesService.AppSetup.SpotifyDevice}", Method.Put);
            return response.StatusCode == HttpStatusCode.NoContent;
        }

        public async Task<bool> Skip()
        {
            RestResponse? response = await SendRestRequest("/me/player/next", Method.Post);
            return response.StatusCode == HttpStatusCode.NoContent;
        }

        public async Task<bool> AddSongToQueue(string id)
        {
            RestResponse? response = await SendRestRequest($"/me/player/queue?uri=spotify:track:{id}&device_id={_appFilesService.AppSetup.SpotifyDevice}", Method.Post);
            return response.StatusCode == HttpStatusCode.OK;
        }

        public async Task<bool> SetPosition(int position)
        {
            int positionMs = position * 1000;
            RestResponse? response = await SendRestRequest($"/me/player/seek?position_ms={positionMs}", Method.Put);
            return response.StatusCode == HttpStatusCode.NoContent;
        }

        public async Task<SongInfo> SearchSong(string query)
        {
            RestResponse? response = await SendRestRequest($"/search?q={HttpUtility.UrlEncode(query)}&type=track&market=FI&limit=1", Method.Get);

            if (string.IsNullOrWhiteSpace(response.Content))
            {
                response.ErrorException!.Data.Add("Response", response.Content);
                throw response.ErrorException!;
            }

            SpotifySearch? searchResult = JsonConvert.DeserializeObject<SpotifySearch>(response.Content!);

            SongInfo songInfo = new SongInfo
            {
                SongName = searchResult?.tracks?.items?[0].name,
                Artist = searchResult?.tracks?.items?[0].artists?[0].name,
                Duration = searchResult!.tracks!.items![0].duration_ms / 1000,
                SongId = searchResult!.tracks.items[0].id
            };

            return songInfo;
        }

        public async Task<string?> GetComputerDevice()
        {
            RestResponse? response = await SendRestRequest("/me/player/devices", Method.Get);

            if (string.IsNullOrWhiteSpace(response.Content))
            {
                response.ErrorException!.Data.Add("Response", response.Content);
                throw response.ErrorException!;
            }

            SpotifyDevices? spotifyDevices = JsonConvert.DeserializeObject<SpotifyDevices>(response.Content!);
            return spotifyDevices?.devices?.FirstOrDefault(x => x.type == "Computer")?.id;
        }

        public async Task<SpotifyState?> GetSpotifyState()
        {
            RestResponse? response = await SendRestRequest($"/me/player?device_id={_appFilesService.AppSetup.SpotifyDevice}", Method.Get);

            SpotifyState? spotifyState;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                spotifyState = JsonConvert.DeserializeObject<SpotifyState>(response.Content!);
            }
            else if (response.StatusCode == HttpStatusCode.NoContent)
            {
                spotifyState = new SpotifyState { is_playing = false };
            } 
            else
            {
                response.ErrorException!.Data.Add("Response", response.Content);
                throw response.ErrorException!;
            }

            return spotifyState;
        }
    }
}