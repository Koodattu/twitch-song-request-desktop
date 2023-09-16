using System.Threading.Tasks;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services.Api
{
    internal interface ISongService
    {
        Task<bool> Play();
        Task<bool> Pause();
        Task<bool> SetPosition(int position);
        Task<int> GetPosition();
        Task<SongInfo> GetSongInfo(string id);
        Task<bool> PlaySong(string id);
        Task<SongInfo> SearchSong(string query);
    }
}
