namespace TwitchSongRequest.Model
{
    internal class AppSetup
    {
        public ServiceOAuthToken StreamerAccessTokens { get; set; }
        public ServiceOAuthToken BotAccessTokens { get; set; }
        public ServiceOAuthToken SpotifyAccessTokens { get; set; }
        public ClientCredentials TwitchClient { get; set; }
        public ClientCredentials SpotifyClient { get; set; }
        public ClientInfo StreamerInfo { get; set; }
        public ClientInfo BotInfo { get; set; }
        public ClientInfo SpotifyInfo { get; set; }
        public string? SpotifyDevice { get; set; }
        public string? ChannelRedeemRewardName { get; set; }
        public string? ChannelRedeemRewardId { get; set; }
        public RewardCreationStatus RewardCreationStatus { get; set; }

        public AppSetup()
        {
            StreamerAccessTokens = new ServiceOAuthToken();
            BotAccessTokens = new ServiceOAuthToken();
            SpotifyAccessTokens = new ServiceOAuthToken();
            TwitchClient = new ClientCredentials();
            SpotifyClient = new ClientCredentials();
            StreamerInfo = new ClientInfo();
            BotInfo = new ClientInfo();
            SpotifyInfo = new ClientInfo();
        }
    }
}
