using Newtonsoft.Json;
using TwitchSongRequest.Services.Api;

namespace TwitchSongRequest.Model
{
    internal class SongRequest
    {
        public string? SongName { get; set; }
        public int? Duration { get; set; }
        public string? Requester { get; set; }
        public string? RequestInput { get; set; }
        public string? Url { get; set; }
        public string? Id { get; set; }
        public SongRequestPlatform? Platform { get; set; }
        [JsonIgnore]
        public ISongService? Service { get; set; }
    }
}
