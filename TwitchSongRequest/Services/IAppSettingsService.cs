using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services
{
    internal interface IAppSettingsService
    {
        AppSettings AppSettings { get; }
        void SaveAppSettings();
        void ResetAppSettings();
    }
}
