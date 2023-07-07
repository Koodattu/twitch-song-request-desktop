using System.Threading.Tasks;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services
{
    internal interface ISongService
    {
        Task<bool> Play();
        Task<bool> Pause();
        Task<bool> Skip();
        Task<bool> SetVolume(int volume);
        Task<bool> SetPosition(int position);
        Task<bool> SetPlaybackDevice(string device);
        Task<int> GetVolume();
        Task<int> GetPosition();
        Task<string> GetPlaybackDevice();
        Task<SongInfo> GetSongInfo(string id);
        Task<bool> PlaySong(string id);
    }
}
