using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.OffScreen;
using System;
using System.Globalization;

namespace TwitchSongRequest.ViewModel
{
    internal class ChromeBrowserViewModel : ObservableObject
    {
        public ChromeBrowserViewModel(string playbackDevice, int volume)
        {
            ChromeBrowser.LoadingStateChanged += ChromeBrowser_LoadingStateChanged;
            PlaybackDevice = playbackDevice;
            Volume = volume;
        }

        private string PlaybackDevice { get; set; }
        private int Volume { get; set; }

        private async void ChromeBrowser_LoadingStateChanged(object? sender, LoadingStateChangedEventArgs e)
        {
            if (ChromeBrowser.CanExecuteJavascriptInMainFrame)
            {
                //TODO Wait for video to start playing
                bool paused = true;
                while (paused)
                {
                    await Task.Delay(100);
                    paused = (bool)(await ChromeBrowser.EvaluateScriptAsync($"document.querySelector('video').paused;")).Result;
                }

                ChangePlaybackDevice(PlaybackDevice);
                SetVideoVolume(Volume);
            }
        }

        private ChromiumWebBrowser _chromeBrowser = new();
        public ChromiumWebBrowser ChromeBrowser
        {
            get => _chromeBrowser;
            set => SetProperty(ref _chromeBrowser, value);
        }

        internal async Task<bool> PlayVideo()
        {
            if (ChromeBrowser.CanExecuteJavascriptInMainFrame)
            {
                var result = await ChromeBrowser.EvaluateScriptAsPromiseAsync("document.querySelector('video').play();");
                return result.Success;
            }
            return false;
        }

        internal void PauseVideo()
        {
            ChromeBrowser.ExecuteScriptAsync("document.querySelector('video').pause();");
        }

        internal async void ChangePlaybackDevice(string device)
        {
            string currentAddress = string.Empty;
            //ChromeBrowser.Dispatcher.Invoke(() => currentAddress = ChromeBrowser.Address);
            currentAddress = ChromeBrowser.Address;
            if (currentAddress != null && currentAddress.ToLower().Contains("youtube") && ChromeBrowser.CanExecuteJavascriptInMainFrame)
            {
                var result = await ChromeBrowser.EvaluateScriptAsPromiseAsync($@"
                    const videoElement = document.getElementsByTagName('video')[0];
                    const audioDevices = await navigator.mediaDevices.enumerateDevices();
                    const desiredDevice = audioDevices.find(device => device.kind === 'audiooutput' && device.label.includes('{device}'));
                    if (desiredDevice) {{
                        videoElement.setSinkId(desiredDevice.deviceId);
                    }}
                    ");
            }
        }

        internal async void SetVideoVolume(int volume)
        {
            Volume = volume;
            if (ChromeBrowser.CanExecuteJavascriptInMainFrame)
            {
                var result = await ChromeBrowser.EvaluateScriptAsync($"document.querySelector('video').volume = {(volume / 100.0).ToString(CultureInfo.InvariantCulture)};");
            }
        }

        internal async void SetVideoPosition(int seconds)
        {
            if (ChromeBrowser.CanExecuteJavascriptInMainFrame)
            {
                var result = await ChromeBrowser.EvaluateScriptAsync($"document.querySelector('video').currentTime = {seconds};");
            }
        }

        internal async Task<int> GetVideoCurrentTime()
        {
            var result = await ChromeBrowser.EvaluateScriptAsync($"document.querySelector('video').currentTime;");
            int currentTime = int.Parse(result.Result.ToString());
            return currentTime;
        }

        internal async Task<string> GetVideoTitle()
        {
            var titleResponse = await ChromeBrowser.EvaluateScriptAsync($"document.title;");
            string title = titleResponse.Result.ToString();
            title = title.Replace(" - YouTube", "");
            return title;
        }

        internal async Task<int> GetVideoDuration()
        {
            var result = await ChromeBrowser.EvaluateScriptAsync($"document.querySelector('video').duration;");
            int duration = int.Parse(result.Result.ToString());
            return duration;
        }

        internal void ChangeAddress(string url)
        {
            ChromeBrowser.Load(url);
        }

        private ChromiumWebBrowser _youtubeBrowser = new();

        internal async Task<Tuple<string, int>> GetYoutubeVideoInfo(string url)
        {
            _youtubeBrowser.GetBrowserHost().SetAudioMuted(true);
            var tcs = new TaskCompletionSource<Tuple<string, int>>(TaskCreationOptions.RunContinuationsAsynchronously);

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
                        paused = (bool)(await _youtubeBrowser.EvaluateScriptAsync($"document.querySelector('video').paused;")).Result;
                    }

                    var titleResponse = await _youtubeBrowser.EvaluateScriptAsync($"document.title;");
                    string title = titleResponse.Result.ToString();
                    title = title.Replace(" - YouTube", "");
                    var durationResponse = await _youtubeBrowser.EvaluateScriptAsync($"document.querySelector('video').duration;");
                    int duration = Convert.ToInt32(durationResponse.Result);
                    tcs.TrySetResult(new Tuple<string, int>(title, duration));
                }
            };

            _youtubeBrowser.LoadingStateChanged += handler;

            await _youtubeBrowser.LoadUrlAsync(url);

            return await tcs.Task;
        }
        
    }
}
