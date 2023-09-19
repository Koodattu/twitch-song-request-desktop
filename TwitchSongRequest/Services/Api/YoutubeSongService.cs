using CefSharp;
using CefSharp.OffScreen;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using TwitchSongRequest.Model;
using TwitchSongRequest.Services.App;

namespace TwitchSongRequest.Services.Api
{
    internal class YoutubeSongService : IYoutubeSongService
    {
        private ChromiumWebBrowser ChromeBrowser;
        private string? PlaybackDevice;
        private int Volume;

        private readonly IAppFilesService _appSettingsService;

        public YoutubeSongService(IAppFilesService appSettingsService)
        {
            _appSettingsService = appSettingsService;
            ChromeBrowser = new ChromiumWebBrowser();
            ChromeBrowser.LoadingStateChanged += ChromeBrowser_LoadingStateChanged;
        }

        private async void ChromeBrowser_LoadingStateChanged(object? sender, LoadingStateChangedEventArgs e)
        {
            if (ChromeBrowser.CanExecuteJavascriptInMainFrame)
            {
                //TODO Wait for video to start playing
                bool paused = true;
                while (paused)
                {
                    await Task.Delay(100);
                    var result = await ChromeBrowser.EvaluateScriptAsync($"document.querySelector('video').paused;");
                    if (result.Success)
                    {
                        paused = (bool)result.Result;
                    }
                }

                await SetPlaybackDevice(PlaybackDevice);
                await SetVolume(Volume);
            }
        }

        public async Task<bool> Play()
        {
            if (ChromeBrowser.CanExecuteJavascriptInMainFrame)
            {
                var result = await ChromeBrowser.EvaluateScriptAsPromiseAsync("document.querySelector('video').play();");
                return result.Success;
            }
            return false;
        }

        public async Task<bool> Pause()
        {
            if (ChromeBrowser.CanExecuteJavascriptInMainFrame)
            {
                var result = await ChromeBrowser.EvaluateScriptAsPromiseAsync("document.querySelector('video').pause();");
                return result.Success;
            }
            return false;
        }

        public async Task<bool> SetVolume(int volume)
        {
            Volume = volume;
            if (ChromeBrowser.CanExecuteJavascriptInMainFrame)
            {
                var result = await ChromeBrowser.EvaluateScriptAsync($"document.querySelector('video').volume = {(volume / 100.0).ToString(CultureInfo.InvariantCulture)};");
                return result.Success;
            }
            return false;
        }

        public async Task<bool> SetPosition(int position)
        {
            if (ChromeBrowser.CanExecuteJavascriptInMainFrame)
            {
                var result = await ChromeBrowser.EvaluateScriptAsync($"document.querySelector('video').currentTime = {position};");
                return result.Success;
            }
            return false;
        }

        public async Task<bool> SetPlaybackDevice(string device)
        {
            PlaybackDevice = device;
            if (ChromeBrowser.Address != null && ChromeBrowser.Address.Contains("youtube", StringComparison.InvariantCultureIgnoreCase) && ChromeBrowser.CanExecuteJavascriptInMainFrame)
            {
                var result = await ChromeBrowser.EvaluateScriptAsPromiseAsync($@"
                    const videoElement = document.getElementsByTagName('video')[0];
                    const audioDevices = await navigator.mediaDevices.enumerateDevices();
                    const desiredDevice = audioDevices.find(device => device.kind === 'audiooutput' && device.label.includes('{device}'));
                    if (desiredDevice) {{
                        videoElement.setSinkId(desiredDevice.deviceId);
                    }}
                    ");
                return result.Success;
            }
            return false;
        }

        public Task<int> GetVolume()
        {
            throw new NotImplementedException();
        }

        public async Task<int> GetPosition()
        {
            if (!ChromeBrowser.CanExecuteJavascriptInMainFrame)
            {
                return -1;
            }

            var curTimeResponse = await ChromeBrowser.EvaluateScriptAsync($"document.querySelector('video').currentTime;");

            if (!curTimeResponse.Success)
            {
                return -1;
            }

            int currentTime = Convert.ToInt32(curTimeResponse.Result);
            return currentTime;
        }

        public Task<string> GetPlaybackDevice()
        {
            throw new NotImplementedException();
        }

        public async Task<SongInfo> GetSongInfo(string videoId)
        {
            RestClient client = new RestClient($"https://www.youtube.com/watch?v={videoId}");
            RestRequest request = new RestRequest("/", Method.Get);
            RestResponse response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful || response.Content == null)
            {
                throw new Exception($"Failed to get song info for video id {videoId}");
            }


            // Find the title using regex
            // This is just an example and may not work if YouTube changes its code
            string? title = string.Empty;
            Match titleMatch = Regex.Match(response.Content, "<title>(.*?) - YouTube</title>");
            if (titleMatch.Success)
            {
                title = titleMatch.Groups[1].Value;
            }


            // Finding the duration is a bit trickier as it's usually embedded in the JavaScript
            // This is just an example and may not work if YouTube changes its code
            int durationInSeconds = 0;
            Match durationMatch = Regex.Match(response.Content, "\"lengthSeconds\":\"(\\d+)\"");
            if (durationMatch.Success)
            {
                durationInSeconds = int.Parse(durationMatch.Groups[1].Value);
            }

            return new SongInfo(title, null, durationInSeconds, videoId);
        }

        public async Task<bool> PlaySong(string id)
        {
            var resp = await ChromeBrowser.LoadUrlAsync($"https://www.youtube.com/embed/{id}?autoplay=1");
            return resp.Success;
        }

        public async Task<SongInfo> SearchSong(string query)
        {
            RestClient client = new RestClient($"https://www.youtube.com/results?search_query={query}&sp=EgIQAQ%3D%3D");
            RestRequest request = new RestRequest("/", Method.Get);
            request.AddHeader("Accept-Language", "en-US,en;q=0.9");
            RestResponse response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful || response.Content == null)
            {
                throw new Exception($"Failed to search for song {query}");
            }

            Match idMatch = Regex.Match(response.Content, "/watch\\?v=([a-zA-Z0-9_-]+)");

            string? videoId = string.Empty;
            if (idMatch.Success)
            {
                videoId = idMatch.Groups[1].Value;
            }

            if (string.IsNullOrWhiteSpace(videoId))
            {
                throw new Exception($"Failed to find video id for song {query}");
            }

            // Regex to find video title
            // This is highly dependent on YouTube's current HTML structure.
            string? title = string.Empty;
            Match titleMatch = Regex.Match(response.Content, "\"title\":{\"runs\":\\[{\"text\":\"(.*?)\"}\\]");
            if (titleMatch.Success)
            {
                title = titleMatch.Groups[1].Value;
            }

            // Regex to find video duration
            // This is also highly dependent on YouTube's current HTML structure.
            int durationInSeconds = 0;
            Match durationMatch = Regex.Match(response.Content, "\"lengthText\":{\"accessibility\":{\"accessibilityData\":{\"label\":\"(.*?)\"}}");
            if (durationMatch.Success)
            {
                Regex regex = new Regex("(\\d+)\\s*(second|minute|hour)s?", RegexOptions.IgnoreCase);
                foreach (Match match in regex.Matches(durationMatch.Groups[1].Value))
                {
                    int value = int.Parse(match.Groups[1].Value);
                    string unit = match.Groups[2].Value.ToLower();

                    switch (unit)
                    {
                        case "second":
                            durationInSeconds += value;
                            break;
                        case "minute":
                            durationInSeconds += value * 60;
                            break;
                        case "hour": // Optional, in case the video is really long
                            durationInSeconds += value * 3600;
                            break;
                    }
                }
            }

            return new SongInfo(title, null, durationInSeconds, videoId);
        }
    }
}
