﻿using CefSharp;
using CefSharp.OffScreen;
using RestSharp;
using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TwitchSongRequest.Model;
using TwitchSongRequest.Services.App;

namespace TwitchSongRequest.Services.Api
{
    internal class YoutubeSongService : IYoutubeSongService
    {
        private IAppFilesService _appFilesService;
        private ChromiumWebBrowser? _chromeBrowser;

        public YoutubeSongService(IAppFilesService appFilesService)
        {
            _appFilesService = appFilesService;
        }

        public async Task SetupService(string playbackDevice, int volume)
        {
            const string html = @"
                <!DOCTYPE html>
                <html>
                    <body>
                        <div id='player'></div>
                        <script>
                        var tag = document.createElement('script');
                        tag.src = 'https://www.youtube.com/iframe_api';
                        var firstScriptTag = document.getElementsByTagName('script')[0];
                        firstScriptTag.parentNode.insertBefore(tag, firstScriptTag);

                        var player;
                        function onYouTubeIframeAPIReady() {
                            player = new YT.Player('player', {
                            height: '360',
                            width: '640',
                            videoId: '',
                            events: {
                                'onReady': onPlayerReady,
                                'onStateChange': onPlayerStateChange
                            }
                            });
                        }

                        function onPlayerReady(event) {
                            CefSharp.PostMessage(event);
                        }
                        
                        function onPlayerStateChange(event) {
                            CefSharp.PostMessage(event);
                            CefSharp.PostMessage(player);
                        }
                        </script>
                    </body>
                </html>
            ";
            var base64EncodedHtml = Convert.ToBase64String(Encoding.UTF8.GetBytes(html));

            _chromeBrowser = new ChromiumWebBrowser(address: "about:blank", automaticallyCreateBrowser: false);
            _chromeBrowser.JavascriptMessageReceived += OnBrowserJavascriptMessageReceived;
            _chromeBrowser.CreateBrowser();
            await _chromeBrowser.WaitForInitialLoadAsync();

            _chromeBrowser.LoadHtml(html, "https://www.youtube.com");
            await _chromeBrowser.WaitForNavigationAsync();

            bool setDevice = await SetPlaybackDevice(playbackDevice);
            bool setVolume = await SetVolume(volume);
        }

        private void OnBrowserJavascriptMessageReceived(object? sender, JavascriptMessageReceivedEventArgs e)
        {

        }

        public async Task<SongInfo> GetSongInfo(string id)
        {
            RestClient client = new RestClient($"https://www.youtube.com/watch?v={id}");
            RestRequest request = new RestRequest("/", Method.Get);
            RestResponse response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful || response.Content == null)
            {
                throw new Exception($"Failed to get song info for video id {id}");
            }

            // Find the title using regex
            // This is just an example and may not work if YouTube changes its code
            string? title = string.Empty;
            Match titleMatch = Regex.Match(response.Content, "<title>(.*?) - YouTube</title>");
            if (titleMatch.Success)
            {
                title = titleMatch.Groups[1].Value;
                title = Regex.Unescape(title);
                title = System.Net.WebUtility.HtmlDecode(title);
            }

            // Finding the duration is a bit trickier as it's usually embedded in the JavaScript
            // This is just an example and may not work if YouTube changes its code
            int durationInSeconds = 0;
            Match durationMatch = Regex.Match(response.Content, "\"lengthSeconds\":\"(\\d+)\"");
            if (durationMatch.Success)
            {
                durationInSeconds = int.Parse(durationMatch.Groups[1].Value);
            }

            return new SongInfo(title, null, durationInSeconds, id);
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
                title = Regex.Unescape(title);
                title = System.Net.WebUtility.HtmlDecode(title);
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

        public async Task<bool> PlaySong(string id)
        {
            JavascriptResponse? result = await _chromeBrowser.EvaluateScriptAsync($"player.loadVideoById('{id}');");
            bool setDevice = await SetPlaybackDevice(_appFilesService.AppSettings.PlaybackDevice!);
            return result.Success;
        }

        public async Task<bool> Play()
        {
            JavascriptResponse? result = await _chromeBrowser.EvaluateScriptAsync("player.playVideo();");
            return result.Success;
        }

        public async Task<bool> Pause()
        {
            JavascriptResponse? result = await _chromeBrowser.EvaluateScriptAsync("player.pauseVideo();");
            return result.Success;
        }

        public async Task<string> GetPlaybackDevice()
        {
            string script = @"
            async function getCurrentAudioOutput() {
                // This will try to target the video inside the iframe
                const iframe = document.querySelector('iframe');
                if(!iframe) return 'iframe not found';

                const videoElement = iframe.contentWindow.document.querySelector('video');
                if(!videoElement) return 'video not found inside iframe';

                const currentDeviceId = videoElement.sinkId;
                const audioDevices = await navigator.mediaDevices.enumerateDevices();
                const currentDevice = audioDevices.find(device => device.kind === 'audiooutput' && device.deviceId === currentDeviceId);
                return currentDevice ? currentDevice.label : 'Default';
            }
            getCurrentAudioOutput();
            ";

            JavascriptResponse result = await _chromeBrowser.EvaluateScriptAsync(script);
            if (result.Success && result.Result != null)
            {
                return result.Result.ToString()!;
            }

            return string.Empty;
        }

        public async Task<bool> SetPlaybackDevice(string device)
        {
            if (_chromeBrowser!.IsBrowserInitialized && _chromeBrowser.CanExecuteJavascriptInMainFrame)
            {
                string script = $@"
                async function setAudioOutput() {{
                    // This will try to target the video inside the iframe
                    const iframe = document.querySelector('iframe');
                    if(!iframe) return 'iframe not found';

                    const videoElement = iframe.contentWindow.document.querySelector('video');
                    if(!videoElement) return 'video not found inside iframe';

                    const audioDevices = await navigator.mediaDevices.enumerateDevices();
                    const desiredDevice = audioDevices.find(d => d.kind === 'audiooutput' && d.label.includes('{device}'));
            
                    if (desiredDevice) {{
                        await videoElement.setSinkId(desiredDevice.deviceId);
                        return true;
                    }}
                    return false;
                }}
                setAudioOutput();
                ";

                var result = await _chromeBrowser.EvaluateScriptAsync(script);
                return result.Success && result.Result != null && Convert.ToBoolean(result.Result);
            }

            return false;
        }

        public async Task<int> GetPosition()
        {
            JavascriptResponse? response = await _chromeBrowser.EvaluateScriptAsync("player.getCurrentTime();");
            if (response.Success && response.Result != null)
            {
                return Convert.ToInt32(response.Result);
            }
            return -1;
        }

        public async Task<int> GetPlayerState()
        {
            JavascriptResponse? response = await _chromeBrowser.EvaluateScriptAsync("player.getPlayerState();");
            if (response.Success && response.Result != null)
            {
                return Convert.ToInt32(response.Result);
            }
            return -1;
        }

        public async Task<bool> SetPosition(int position)
        {
            JavascriptResponse? result = await _chromeBrowser.EvaluateScriptAsync($"player.seekTo({position}, true);");
            return result.Success;
        }

        public async Task<int> GetVolume()
        {
            JavascriptResponse? response = await _chromeBrowser.EvaluateScriptAsync("player.getVolume();");
            if (response.Success && response.Result != null)
            {
                return Convert.ToInt32(response.Result);
            }
            return -1;
        }

        public async Task<bool> SetVolume(int volume)
        {
            JavascriptResponse? result = await _chromeBrowser.EvaluateScriptAsync($"player.setVolume({volume.ToString(CultureInfo.InvariantCulture)});");
            return result.Success;
        }
    }
}
