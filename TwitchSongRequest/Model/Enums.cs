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
        SUCCESS,
        ERROR,
        ALREADY_EXISTS
    }
}
