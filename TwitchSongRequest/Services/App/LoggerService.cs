using NLog;
using System;
using System.Linq;
using System.Net.Http;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services.App
{
    internal class LoggerService : ILoggerService
    {
        private static ILogger? _logger;

        public event Action<StatusEvent> StatusEvent;

        public LoggerService()
        {
            _logger = LogManager.GetCurrentClassLogger();
        }

        public void LogError(Exception exception, string message)
        {
            if (exception is HttpRequestException ex)
            {
                string dataString = string.Join(",", ex.Data.Values.Cast<object>().Select(v => v.ToString()));
                dataString = dataString.Replace("\n", " ");
                message = $"{message}, Data: {dataString}";
            }
            _logger?.Error(exception, message);
            StatusEvent.Invoke(new StatusEvent(DateTime.Now, "Error", message + " " + exception.Message));
        }

        public void LogInfo(string message)
        {
            _logger?.Info(message);
            StatusEvent?.Invoke(new StatusEvent(DateTime.Now, "Info", message));
        }

        public void LogWarning(string message)
        {
            _logger?.Info(message);
            StatusEvent?.Invoke(new StatusEvent(DateTime.Now, "Warning", message));
        }

        public void LogError(string message)
        {
            _logger?.Error(message);
            StatusEvent?.Invoke(new StatusEvent(DateTime.Now, "Error", message));
        }

        public void LogSuccess(string message)
        {
            _logger?.Info(message);
            StatusEvent?.Invoke(new StatusEvent(DateTime.Now, "Success", message));
        }
    }
}
