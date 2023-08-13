using System.Collections.Generic;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services.App
{
    internal interface IAppFilesService
    {
        AppSettings AppSettings { get; }
        void SaveAppSettings();
        void ResetAppSettings();
        AppTokens AppTokens { get; }
        void SaveAppTokens();
        void ResetAppTokens();
        List<SongRequest> GetSongQueue();
        void SaveSongQueue(List<SongRequest> songRequests);
        List<SongRequest> GetSongHistory();
        void SaveSongHistory(List<SongRequest> songRequests);
    }
}
