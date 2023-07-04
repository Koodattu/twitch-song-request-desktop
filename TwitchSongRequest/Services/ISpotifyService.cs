using System.Threading.Tasks;

namespace TwitchSongRequest.Services
{
    internal interface ISpotifyService
    {
        internal Task<bool> ValidateSpotifyAccessToken();
        internal Task<bool> RefreshSpotifyAccessToken();
        internal Task<bool> GetCurrentSong();
        internal Task<bool> GetPlaybackDevices();
        internal Task<bool> Play();
        internal Task<bool> Pause();
        internal Task<bool> Skip();
        internal Task<bool> AddToQueue(string song);
    }
}
