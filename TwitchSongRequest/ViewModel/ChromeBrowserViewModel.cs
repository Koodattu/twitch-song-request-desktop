using CefSharp.Wpf;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;
using CefSharp;
using System;

namespace TwitchSongRequest.ViewModel
{
    internal class ChromeBrowserViewModel : ObservableObject
    {
        public ChromeBrowserViewModel()
        {
            ChromeBrowser.LoadingStateChanged += ChromeBrowser_LoadingStateChanged;
        }

        private void ChromeBrowser_LoadingStateChanged(object? sender, LoadingStateChangedEventArgs e)
        {
            ChangePlaybackDevice(PlaybackDevice);
        }

        private string _playbackDevice;
        public string PlaybackDevice
        {
            get => _playbackDevice;
            set
            {
                SetProperty(ref _playbackDevice, value);
                ChangePlaybackDevice(value);
            }
        }

        private ChromiumWebBrowser _chromeBrowser = new();
        public ChromiumWebBrowser ChromeBrowser
        {
            get => _chromeBrowser;
            set => SetProperty(ref _chromeBrowser, value);
        }

        private void PlayVideo()
        {
            ChromeBrowser.ExecuteScriptAsync("document.querySelector('video').play();");
        }

        private void PauseVideo()
        {
            ChromeBrowser.ExecuteScriptAsync("document.querySelector('video').pause();");
        }

        internal async void ChangePlaybackDevice(string device)
        {
            string currentAddress = string.Empty;
            ChromeBrowser.Dispatcher.Invoke(() => currentAddress = ChromeBrowser.Address);
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

        private async void SetVideoVolume(double volume)
        {
            var result = await ChromeBrowser.EvaluateScriptAsync($"document.querySelector('video').volume = {volume};");
        }

        private async void SetVideoPosition(int seconds)
        {
            var result = await ChromeBrowser.EvaluateScriptAsync($"document.querySelector('video').currentTime = {seconds};");
        }

        private async Task<int> GetVideoCurrentTime()
        {
            var result = await ChromeBrowser.EvaluateScriptAsync($"document.querySelector('video').currentTime;");
            int currentTime = int.Parse(result.Result.ToString());
            return currentTime;
        }

        private async Task<string> GetVideoTitle()
        {
            var titleResponse = await ChromeBrowser.EvaluateScriptAsync($"document.title;");
            string title = titleResponse.Result.ToString();
            title = title.Replace(" - YouTube", "");
            return title;
        }

        private async Task<int> GetVideoDuration()
        {
            var result = await ChromeBrowser.EvaluateScriptAsync($"document.querySelector('video').duration;");
            int duration = int.Parse(result.Result.ToString());
            return duration;
        }

        private void ChangeAddress(string url)
        {
            ChromeBrowser.Address = url;
        }

        private async Task<Tuple<string, int>> GetYoutubeVideoInfo(string url)
        {
            ChromiumWebBrowser chromiumWebBrowser = new();
            chromiumWebBrowser.Address = url;
            var titleResponse = await ChromeBrowser.EvaluateScriptAsync($"document.title;");
            string title = titleResponse.Result.ToString();
            title = title.Replace(" - YouTube", "");
            var result = await ChromeBrowser.EvaluateScriptAsync($"document.querySelector('video').duration;");
            int duration = int.Parse(result.Result.ToString());
            return new Tuple<string, int>(title, duration);
        }
    }
}
