using CefSharp;
using CefSharp.OffScreen;
using System;
using System.Globalization;
using System.Security.Policy;
using System.Threading.Tasks;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Services
{
    internal class YoutubeSongService : ISongService
    {
        private ChromiumWebBrowser ChromeBrowser;
        private string? PlaybackDevice;
        private int Volume;

        public YoutubeSongService()
        {
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

        public async Task<bool> Skip()
        {
            throw new NotImplementedException();
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
                    tcs.TrySetResult(new SongInfo(title, null, duration));
                }
            };

            _youtubeBrowser.LoadingStateChanged += handler;

            await _youtubeBrowser.LoadUrlAsync($"https://www.youtube.com/embed/{id}?autoplay=1");

            return await tcs.Task;
        }

        public async Task<bool> PlaySong(string id)
        {
            var resp = await ChromeBrowser.LoadUrlAsync($"https://www.youtube.com/embed/{id}?autoplay=1");
            return resp.Success;
        }
    }
}
