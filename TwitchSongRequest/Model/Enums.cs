namespace TwitchSongRequest.Model
{
    internal enum SongRequestPlatform
    {
        Spotify,
        Youtube,
        Soundcloud
    }

    internal enum ConnectionStatus
    {
        NotConnected,
        Disconnected,
        Connected,
        Connecting,
        Refreshing,
        Cancelled,
        Error
    }

    internal enum PlaybackStatus
    {
        Waiting,
        Error,
        Playing,
        Paused
    }

    internal enum RewardCreationStatus
    {
        Waiting,
        Creating,
        Error,
        Created,
        AlreadyExists
    }

    internal enum WebBrowser
    {
        Chrome,
        Firefox,
        Edge
    }
}
