namespace TwitchSongRequest.Model
{
    internal class ConnectionsStatus
    {
        public ConnectionStatus TwitchStreamerStatus { get; set; }
        public ConnectionStatus TwitchBotStatus { get; set; }
        public ConnectionStatus SpotifyStatus { get; set; }
    }
}
