using CommunityToolkit.Mvvm.ComponentModel;

namespace TwitchSongRequest.Model
{
    internal class ConnectionsStatus : ObservableObject
    {
        private ConnectionStatus _streamerStatus;
        public ConnectionStatus StreamerStatus
        {
            get => _streamerStatus;
            set => SetProperty(ref _streamerStatus, value);
        }

        private ConnectionStatus _botStatus;
        public ConnectionStatus BotStatus
        {
            get => _botStatus;
            set => SetProperty(ref _botStatus, value);
        }

        private ConnectionStatus _spotifyStatus;
        public ConnectionStatus SpotifyStatus
        {
            get => _spotifyStatus;
            set => SetProperty(ref _spotifyStatus, value);
        }
    }
}
