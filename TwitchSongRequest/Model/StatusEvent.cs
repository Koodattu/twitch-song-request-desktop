using System;

namespace TwitchSongRequest.Model
{
    internal class StatusEvent
    {
        public DateTime Time { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }

        public StatusEvent(DateTime time, string type, string message)
        {
            Time = time;
            Type = type;
            Message = message;
        }

        public override string ToString()
        {
            return $"{Time}  |  {Type}  |  {Message}";
        }
    }
}
