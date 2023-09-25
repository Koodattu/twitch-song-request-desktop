using System.Threading.Tasks;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services.Api
{
    internal interface ISpotifySongService : ISongService
    {
        Task<SpotifyState?> GetSpotifyState();
        Task<string?> GetComputerDevice();
        Task<bool> AddSongToQueue(string id);
        Task<bool> Skip();
    }
}
