using Microsoft.Web.WebView2.Wpf;
using System.Threading.Tasks;

namespace TwitchSongRequest.Services.Api
{
    internal interface IYoutubeSongService : ISongService
    {
        Task<bool> SetVolume(int volume);
        Task<int> GetVolume();
        Task<string> GetPlaybackDevice();
        Task<bool> SetPlaybackDevice(string device);
        Task SetupService(WebView2 browser, string playbackDevice, int volume);
    }
}
