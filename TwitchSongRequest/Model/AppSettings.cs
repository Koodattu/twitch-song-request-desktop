namespace TwitchSongRequest.Model
{
    internal class AppSettings
    {
        public int? Volume { get; set; }
        public string? PlaybackDevice { get; set; }
        public int? MaxSongDurationMinutes { get; set; }
        public int? MaxSongDurationSeconds { get; set; }
        public bool ReplyInChat { get; set; }
        public bool ReplyWithBot { get; set; }
        public SongRequestPlatform SongSearchPlatform { get; set; }
    }
}
