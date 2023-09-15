using System.Threading.Tasks;

namespace TwitchSongRequest.Services.Api
{
    internal interface ISpotifySongService : ISongService
    {
        Task<bool> AddSongToQueue(string id);
        Task<bool> Skip();
    }
}
