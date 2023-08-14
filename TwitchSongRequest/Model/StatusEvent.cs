using System;

namespace TwitchSongRequest.Model
{
    internal class StatusEvent
    {
        public DateTime Time { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        public bool History { get; set; }
        public StatusEvent(DateTime time, string type, string message, bool history = false)
        {
            Time = time;
            Type = type;
            Message = message;
            History = history;
        }

        public override string ToString()
        {
            return $"{Time}\t{Type}\t\t{Message}";
        }
    }
}
