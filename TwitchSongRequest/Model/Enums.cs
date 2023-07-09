namespace TwitchSongRequest.Model
{
    internal enum SongRequestPlatform
    {
        SPOTIFY,
        YOUTUBE,
        SOUNDCLOUD
    }

    internal enum ConnectionStatus
    {
        NOT_CONNECTED,
        DISCONNECTED,
        CONNECTED,
        CONNECTING,
        ERROR
    }

    internal enum PlaybackStatus
    {
        WAITING,
        ERROR,
        PLAYING,
        PAUSED
    }

    internal enum RewardCreationStatus
    {
        WAITING,
        PENDING,
        ERROR,
        SUCCESS,
        ALREADY_EXISTS
    }

    internal enum WebBrowser
    {
        CHROME,
        FIREFOX,
        EDGE
    }
}
