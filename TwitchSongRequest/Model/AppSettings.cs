namespace TwitchSongRequest.Model
{
    internal class AppSettings
    {
        public TwitchAccessToken? StreamerAccessTokens { get; set; }
        public TwitchAccessToken? BotAccessTokens { get; set; }
        public SpotifyAccessToken? SpotifyAccessTokens { get; set; }
        public string? TwitchClientId { get; set; }
        public string? TwitchClientSecret { get; set; }
        public string? TwitchStreamerAccessToken { get; set; }
        public string? TwitchStreamerRefreshToken { get; set; }
        public string? TwitchBotAccessToken { get; set; }
        public string? TwitchBotRefreshToken { get; set; }
        public string? SpotifyClientId { get; set; }
        public string? SpotifyClientSecret { get; set; }
        public string? SpotifyAccessToken { get; set; }
        public string? SpotifyRefreshToken { get; set; }
        public string? ChannelRedeemRewardName { get; set; }
        public string? ChannelRedeemRewardId { get; set; }
        public string? TwitchStreamerName { get; set; }
        public string? TwitchBotName { get; set; }
        public string? SpotifyAccountName { get; set; }
        public int? Volume { get; set; }
        public string? PlaybackDevice { get; set; }
        public int? MaxSongLengthMinutes { get; set; }
        public int? MaxSongLengthSeconds { get; set; }
        public SongRequestPlatform SongSearchPlatform { get; set; }
        public RewardCreationStatus RewardCreationStatus { get; set; }
    }
}
