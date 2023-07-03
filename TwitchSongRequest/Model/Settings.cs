using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchSongRequest.Model
{
    internal class Settings
    {
        public string TwitchClientId { get; set; }
        public string TwitchClientSecret { get; set; }
        public string SpotifyClientId { get; set; }
        public string SpotifyClientSecret { get; set; }
    }
}
