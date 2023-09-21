using System.Threading.Tasks;

namespace TwitchSongRequest.Services.Api
{
    internal interface IYoutubeSongService : ISongService
    {
        Task<bool> SetVolume(int volume);
        Task<int> GetVolume();
        Task<string> GetPlaybackDevice();
        Task<bool> SetPlaybackDevice(string device);
        Task SetupService(string playbackDevice, int volume);
    }
}
