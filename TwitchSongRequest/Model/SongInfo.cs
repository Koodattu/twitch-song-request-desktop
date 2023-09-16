namespace TwitchSongRequest.Model
{
    internal class SongInfo
    {
        public string? SongName { get; set; }
        public string? Artist { get; set; }
        public int Duration { get; set; }
        public string? SongId { get; set; }

        public SongInfo(string? songName, string? artist, int duration, string? songId)
        {
            SongName = songName;
            Artist = artist;
            Duration = duration;
            SongId = songId;
        }

        public SongInfo()
        {
                
        }
    }
}
