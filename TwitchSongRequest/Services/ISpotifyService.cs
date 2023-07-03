using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchSongRequest.Services
{
    internal interface ISpotifyService
    {
        internal Task<bool> ValidateSpotifyOAuthToken(string spotifyClientId, string spotifyClientSecret);
        internal Task<bool> RefreshSpotifyOAuthToken(string spotifyClientId, string spotifyClientSecret);
        internal Task<bool> PlaySong(string spotifyClientId, string spotifyClientSecret, string songUri);
        internal Task<bool> PauseSong(string spotifyClientId, string spotifyClientSecret);
    }
}
