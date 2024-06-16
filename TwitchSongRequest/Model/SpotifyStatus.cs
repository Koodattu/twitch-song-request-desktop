using System;

namespace TwitchSongRequest.Model
{
    public class SpotifyStatus
    {
        public string? CurrentSongId { get; set; }
        public int Duration { get; set; }
        public int Progress { get; set; }
        public bool IsPlaying { get; set; }
        public DateTime StatusTimestamp { get; set; }
        public int TimeSinceLastUpdate => (int)(DateTime.Now - StatusTimestamp).TotalSeconds;
    }
}
