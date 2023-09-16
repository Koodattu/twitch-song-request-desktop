using Newtonsoft.Json;
using RestSharp;
using System.Net;
using System.Threading.Tasks;
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

        private async Task<RestResponse> SendRestRequest(string resource, Method method)
        {
            var restClient = new RestClient($"https://api.spotify.com/v1/me/player/play");
            var request = new RestRequest(resource, method);

            string accessToken = _appFilesService.AppSetup.SpotifyAccessTokens.AccessToken!;
            request.AddHeader("Authorization", $"Bearer {accessToken}");

            var response = await restClient.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                response.ErrorException!.Data.Add("Response", response.Content);
                throw response.ErrorException!;
            }

            return response;
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
                return new SongInfo(name, artist, duration, id);
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
            request.AddJsonBody(new { uris = new[] { $"spotify:track:{id}" } });

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

        public async Task<SongInfo> SearchSong(string query)
        {
            var restClient = new RestClient($"https://api.spotify.com/v1/search?q={query}&type=track&limit=1");
            var request = new RestRequest("/", Method.Get);

            string accessToken = _appFilesService.AppSetup.SpotifyAccessTokens.AccessToken!;
            request.AddHeader("Authorization", $"Bearer {accessToken}");

            var response = await restClient.ExecuteAsync<Rootobject>(request);

            if (!response.IsSuccessful)
            {
                response.ErrorException!.Data.Add("Response", response.Content);
                throw response.ErrorException!;
            }

            SongInfo songInfo = new SongInfo
            {
                SongName = response.Data!.tracks.items[0].name,
                Artist = response.Data.tracks.items[0].artists[0].name,
                Duration = response.Data.tracks.items[0].duration_ms / 1000,
                SongId = response.Data.tracks.items[0].id
            };

            return songInfo;
        }
    }
}

public class Rootobject
{
    public Tracks tracks { get; set; }
}

public class Tracks
{
    public string href { get; set; }
    public Item[] items { get; set; }
    public int limit { get; set; }
    public string next { get; set; }
    public int offset { get; set; }
    public object previous { get; set; }
    public int total { get; set; }
}

public class Item
{
    public Album album { get; set; }
    public Artist1[] artists { get; set; }
    public string[] available_markets { get; set; }
    public int disc_number { get; set; }
    public int duration_ms { get; set; }
    public bool _explicit { get; set; }
    public External_Ids external_ids { get; set; }
    public External_Urls2 external_urls { get; set; }
    public string href { get; set; }
    public string id { get; set; }
    public bool is_local { get; set; }
    public string name { get; set; }
    public int popularity { get; set; }
    public string preview_url { get; set; }
    public int track_number { get; set; }
    public string type { get; set; }
    public string uri { get; set; }
}

public class Album
{
    public string album_type { get; set; }
    public Artist[] artists { get; set; }
    public string[] available_markets { get; set; }
    public External_Urls external_urls { get; set; }
    public string href { get; set; }
    public string id { get; set; }
    public Image[] images { get; set; }
    public string name { get; set; }
    public string release_date { get; set; }
    public string release_date_precision { get; set; }
    public int total_tracks { get; set; }
    public string type { get; set; }
    public string uri { get; set; }
}

public class External_Urls
{
    public string spotify { get; set; }
}

public class Artist
{
    public External_Urls1 external_urls { get; set; }
    public string href { get; set; }
    public string id { get; set; }
    public string name { get; set; }
    public string type { get; set; }
    public string uri { get; set; }
}

public class External_Urls1
{
    public string spotify { get; set; }
}

public class Image
{
    public int height { get; set; }
    public string url { get; set; }
    public int width { get; set; }
}

public class External_Ids
{
    public string isrc { get; set; }
}

public class External_Urls2
{
    public string spotify { get; set; }
}

public class Artist1
{
    public External_Urls3 external_urls { get; set; }
    public string href { get; set; }
    public string id { get; set; }
    public string name { get; set; }
    public string type { get; set; }
    public string uri { get; set; }
}

public class External_Urls3
{
    public string spotify { get; set; }
}
