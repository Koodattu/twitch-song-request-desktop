namespace TwitchSongRequest.Model
{
    internal class SongInfo
    {
        public string? SongName { get; set; }
        public string? Artist { get; set; }
        public int Duration { get; set; }

        public SongInfo(string? songName, string? artist, int duration)
        {
            SongName = songName;
            Artist = artist;
            Duration = duration;
        }
    }
}
