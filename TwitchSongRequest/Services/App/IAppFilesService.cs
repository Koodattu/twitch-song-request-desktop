using System.Collections.Generic;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services.App
{
    internal interface IAppFilesService
    {
        IEnumerable<string> GetAppLogs();
        AppSettings AppSettings { get; }
        void SaveAppSettings();
        void ResetAppSettings();
        AppSetup AppSetup { get; }
        void SaveAppSetup();
        void ResetAppSetup();
        List<SongRequest> GetSongQueue();
        void SaveSongQueue(List<SongRequest> songRequests);
        List<SongRequest> GetSongHistory();
        void SaveSongHistory(List<SongRequest> songRequests);
    }
}
