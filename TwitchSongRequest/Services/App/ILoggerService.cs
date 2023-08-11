using System;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services.App
{
    internal interface ILoggerService
    {
        event Action<StatusEvent> StatusEvent;
        void LogInfo(string message);
        void LogError(Exception exception, string message);
    }
}
