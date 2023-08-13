using System;
using System.Threading.Tasks;
using TwitchSongRequest.Model;
using TwitchSongRequest.Services.App;

namespace TwitchSongRequest.Services.Api
{
    internal class SpotifySongService : ISpotifySongService
    {
        private readonly IAppFilesService _appSettingsService;

        public SpotifySongService(IAppFilesService appSettingsService)
        {
            _appSettingsService = appSettingsService;
        }

        public Task<string> GetPlaybackDevice()
        {
            throw new NotImplementedException();
        }

        public Task<int> GetPosition()
        {
            throw new NotImplementedException();
        }

        public Task<SongInfo> GetSongInfo(string id)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetVolume()
        {
            throw new NotImplementedException();
        }

        public Task<bool> Pause()
        {
            throw new NotImplementedException();
        }

        public Task<bool> Play()
        {
            throw new NotImplementedException();
        }

        public Task<bool> PlaySong(string id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetPlaybackDevice(string device)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetPosition(int position)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetVolume(int volume)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Skip()
        {
            throw new NotImplementedException();
        }
    }
}
