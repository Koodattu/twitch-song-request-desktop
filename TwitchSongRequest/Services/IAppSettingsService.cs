﻿using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services
{
    internal interface IAppSettingsService
    {
        AppSettings AppSettings { get; }
        void SaveAppSettings();
        void ResetAppSettings();
        AppTokens AppTokens { get; }
        void SaveAppTokens();
        void ResetAppTokens();
    }
}
