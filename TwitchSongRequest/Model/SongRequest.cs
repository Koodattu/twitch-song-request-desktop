using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchSongRequest.Model
{
    internal class SongRequest
    {
        public string? SongName { get; set; }
        public string? Requester { get; set; }
        public string? Url { get; set; }
        public SongRequestPlatform Platform { get; set; }
    }
}
