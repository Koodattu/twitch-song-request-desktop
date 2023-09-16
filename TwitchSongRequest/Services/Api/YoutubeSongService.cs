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

        private ChromiumWebBrowser _youtubeBrowser = new();
        public async Task<SongInfo> GetSongInfo(string id)
        {
            _youtubeBrowser.GetBrowserHost().SetAudioMuted(true);
            var tcs = new TaskCompletionSource<SongInfo>(TaskCreationOptions.RunContinuationsAsynchronously);

            EventHandler<LoadingStateChangedEventArgs> handler = null;
            handler = async (sender, args) =>
            {
                //Wait for while page to finish loading not just the first frame
                if ((sender as ChromiumWebBrowser).CanExecuteJavascriptInMainFrame && !args.IsLoading)
                {
                    _youtubeBrowser.LoadingStateChanged -= handler;

                    //TODO Wait for video to start playing
                    bool paused = true;
                    while (paused)
                    {
                        await Task.Delay(100);
                        var result = await _youtubeBrowser.EvaluateScriptAsync($"document.querySelector('video').paused;");
                        if (result.Success)
                        {
                            paused = (bool)result.Result;
                        }
                    }

                    var titleResponse = await _youtubeBrowser.EvaluateScriptAsync($"document.title;");
                    string title = titleResponse.Result.ToString();
                    title = title.Replace(" - YouTube", "");
                    var durationResponse = await _youtubeBrowser.EvaluateScriptAsync($"document.querySelector('video').duration;");
                    int duration = Convert.ToInt32(durationResponse.Result);
                    tcs.TrySetResult(new SongInfo(title, null, duration, id));
                }
            };

            _youtubeBrowser.LoadingStateChanged += handler;

            await _youtubeBrowser.LoadUrlAsync($"https://www.youtube.com/embed/{id}?autoplay=1");

            return await tcs.Task;
        }

        public async Task<SongInfo> GetSongInfoV2(string videoId)
        {
            RestClient client = new RestClient($"https://www.youtube.com/watch?v={videoId}");
            RestRequest request = new RestRequest("/", Method.Get);
            RestResponse response = client.Execute(request);

            if (!response.IsSuccessful || response.Content == null)
            {
                throw new Exception($"Failed to get song info for video id {videoId}");
            }

            string? title = string.Empty;

            // Find the title using regex
            Match titleMatch = Regex.Match(response.Content, "<title>(.*?) - YouTube</title>");
            if (titleMatch.Success)
            {
                title = titleMatch.Groups[1].Value;
            }

            int durationInSeconds = 0;

            // Finding the duration is a bit trickier as it's usually embedded in the JavaScript
            // This is just an example and may not work if YouTube changes its code
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
            RestResponse response = client.Execute(request);

            if (!response.IsSuccessful || response.Content == null)
            {
                throw new Exception($"Failed to search for song {query}");
            }

            Match match = Regex.Match(response.Content, "/watch\\?v=([a-zA-Z0-9_-]+)");

            string? videoId = string.Empty;
            if (match.Success)
            {
                videoId = match.Groups[1].Value;
            }

            if (string.IsNullOrWhiteSpace(videoId))
            {
                throw new Exception($"Failed to find video id for song {query}");
            }

            SongInfo songInfo = await GetSongInfoV2(videoId);
            return songInfo;
        }
    }
}
