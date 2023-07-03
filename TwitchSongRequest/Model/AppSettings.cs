using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchSongRequest.Model
{
    internal class AppSettings
    {
        public string TwitchClientId { get; set; }
        public string TwitchClientSecret { get; set; }
        public string TwitchStreamerOAuthToken { get; set; }
        public string TwitchBotOAuthToken { get; set; }
        public string SpotifyClientId { get; set; }
        public string SpotifyClientSecret { get; set; }
        public string SpotifyOAuthToken { get; set; }
        public string ChannelRedeemRewardName { get; set; }
        public string ChannelRedeemRewardId { get; set; }
    }
}
