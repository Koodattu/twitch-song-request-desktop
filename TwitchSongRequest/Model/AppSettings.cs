namespace TwitchSongRequest.Model
{
    internal class AppSettings
    {
        public int? Volume { get; set; }
        public string? PlaybackDevice { get; set; }
        public int MaxSongDurationMinutes { get; set; }
        public int MaxSongDurationSeconds { get; set; }
        public bool ReplyInChat { get; set; }
        public bool ReplyToRedeem { get; set; }
        public bool MessageOnNextSong { get; set; }
        public bool AutoPlay { get; set; }
        public bool ReplyWithBot { get; set; }
        public bool RefundAllPoints { get; set; }
        public bool StartMinimized { get; set; }
        public bool SpotifyAddToQueue { get; set; }
        public SongRequestPlatform SongSearchPlatform { get; set; }
    }
}
