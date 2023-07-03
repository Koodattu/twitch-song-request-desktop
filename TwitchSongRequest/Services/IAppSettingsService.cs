using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services
{
    internal interface IAppSettingsService
    {
        internal AppSettings GetAppSettings();
        internal void SaveAppSettings(AppSettings appSettings);
    }
}
