namespace TwitchSongRequest.Model
{
    internal enum SongRequestPlatform
    {
        YOUTUBE,
        SPOTIFY
    }

    internal enum ConnectionStatus
    {
        NOT_CONNECTED,
        DISCONNECTED,
        CONNECTED
    }

    internal enum PlaybackStatus
    {
        ERROR,
        PLAYING,
        PAUSED
    }
}
