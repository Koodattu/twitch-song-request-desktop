using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services
{
    internal interface IAppSettingsService
    {
        AppSettings GetAppSettings();
        void SaveAppSettings(AppSettings appSettings);
        AppSettings ResetAppSettings();
    }
}
