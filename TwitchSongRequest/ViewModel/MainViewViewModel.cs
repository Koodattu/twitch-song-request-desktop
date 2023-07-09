using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TwitchSongRequest.Model;
using TwitchSongRequest.Services;

namespace TwitchSongRequest.ViewModel
{
    internal class MainViewViewModel : ObservableObject
    {
        internal static YoutubeSongService youtubeSongService = new();
        internal static SpotifySongService spotifySongService = new();

        private readonly IAppSettingsService appSettingsService;
        private readonly ITwitchAuthService twitchAuthService;
        private readonly ISpotifyAuthService spotifyAuthService;
        
        private readonly DispatcherTimer dispatcherTimer;

        public MainViewViewModel()
        {
            appSettingsService = App.Current.Services.GetService<IAppSettingsService>()!;
            twitchAuthService = App.Current.Services.GetService<ITwitchAuthService>()!;
            spotifyAuthService = App.Current.Services.GetService<ISpotifyAuthService>()!;

            _appSettings = appSettingsService.GetAppSettings();
            _playbackDevice = _appSettings.PlaybackDevice ?? GetDefaultPlaybackDevice();
            _volume = _appSettings.Volume ?? 100;

            youtubeSongService.SetPlaybackDevice(_playbackDevice);
            youtubeSongService.SetVolume(_volume);

            ValidateStreamerLogin();
            ValidateBotLogin();
            ValidateSpotifyLogin();

            GenerateMockRequests();

            dispatcherTimer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, TimerCallback, Application.Current.Dispatcher);
            dispatcherTimer.Start();
        }

        private async void ValidateStreamerLogin()
        {
            // 1. try to validate token
            Connections.StreamerStatus = ConnectionStatus.CONNECTING;

            try
            {
                AppSettings.TwitchStreamerName = await twitchAuthService.ValidateTwitchAccessTokens(AppSettings.StreamerAccessTokens!, AppSettings.TwitchClientId!, AppSettings.TwitchClientSecret!);
                Connections.StreamerStatus = ConnectionStatus.CONNECTED;
                return;
            }
            catch (Exception ex)
            {
                // TODO: Log error
                Connections.StreamerStatus = ConnectionStatus.ERROR;
            }

            // 2. if not logged in, try to refresh token
            try
            {
                AppSettings.StreamerAccessTokens = await twitchAuthService.RefreshTwitchAccessTokens(AppSettings.StreamerAccessTokens!, AppSettings.TwitchClientId!, AppSettings.TwitchClientSecret!);
                Connections.StreamerStatus = ConnectionStatus.CONNECTED;
                return;
            }
            catch (Exception ex)
            {
                // TODO: Log error
                Connections.StreamerStatus = ConnectionStatus.ERROR;
            }

            // 3. revalidate token
            try
            {
                AppSettings.TwitchStreamerName = await twitchAuthService.ValidateTwitchAccessTokens(AppSettings.StreamerAccessTokens!, AppSettings.TwitchClientId!, AppSettings.TwitchClientSecret!);
                Connections.StreamerStatus = ConnectionStatus.CONNECTED;
                return;
            }
            catch (Exception ex)
            {
                // TODO: Log error
                Connections.StreamerStatus = ConnectionStatus.ERROR;
            }
        }

        private void ValidateBotLogin()
        {

        }

        private void ValidateSpotifyLogin()
        {

        }

        private async void TimerCallback(object? sender, EventArgs e)
        {
            if (PlaybackStatus != PlaybackStatus.PLAYING)
            {
                return;
            }

            int curTime = await CurrentSong.Service!.GetPosition();

            if (curTime == -1)
            {
                return;
            }

            SetProperty(ref _position, curTime, nameof(Position));
        }

        private void GenerateMockRequests()
        {
            for (int i = 0; i < 10; i++)
            {
                SongRequestQueue.Add(new SongRequest
                {
                    SongName = $"Song {i}",
                    Duration = 100 * i,
                    Requester = $"Requester {i}",
                    Url = $"https://www.youtube.com/watch?v=41VlNOyPD9U?t=10s&autoplay={i}",
                    Platform = i % 2 == 0 ? SongRequestPlatform.YOUTUBE : SongRequestPlatform.SPOTIFY,
                    Service = i % 2 == 0 ? youtubeSongService : spotifySongService
                });
            }
            for (int i = 0; i < 4; i++)
            {
                SongRequestHistory.Add(new SongRequest
                {
                    SongName = $"Song {i}",
                    Duration = 100 * i,
                    Requester = $"Requester {i}",
                    Url = $"Url {i}",
                    Platform = i % 2 == 0 ? SongRequestPlatform.YOUTUBE : SongRequestPlatform.SPOTIFY,
                    Service = i % 2 == 0 ? youtubeSongService : spotifySongService
                });
            }
        }

        private string _playbackDevice;
        public string PlaybackDevice
        {
            get => _playbackDevice;
            set
            {
                AppSettings.PlaybackDevice = value;
                _ = youtubeSongService.SetPlaybackDevice(value);
                SetProperty(ref _playbackDevice, value);
            }
        }

        private ObservableCollection<string> _playbackDevices = new ObservableCollection<string>(GetPlaybackDevices());
        public ObservableCollection<string> PlaybackDevices
        {
            get => _playbackDevices;
            set => SetProperty(ref _playbackDevices, value);
        }

        private bool _isSettingsOpen;
        public bool IsSettingsOpen
        {
            get => _isSettingsOpen;
            set => SetProperty(ref _isSettingsOpen, value);
        }

        private bool _isSetupOpen;
        public bool IsSetupOpen
        {
            get => _isSetupOpen;
            set => SetProperty(ref _isSetupOpen, value);
        }

        private AppSettings _appSettings;
        public AppSettings AppSettings
        {
            get => _appSettings;
            set => SetProperty(ref _appSettings, value);
        }

        private PlaybackStatus _playbackStatus;
        public PlaybackStatus PlaybackStatus
        {
            get => _playbackStatus;
            set => SetProperty(ref _playbackStatus, value);
        }

        private SongRequest _currentSong = new SongRequest();
        public SongRequest CurrentSong
        {
            get => _currentSong;
            set => SetProperty(ref _currentSong, value);
        }

        private ObservableCollection<SongRequest> _songRequestQueue = new ObservableCollection<SongRequest>();
        public ObservableCollection<SongRequest> SongRequestQueue
        {
            get => _songRequestQueue;
            set => SetProperty(ref _songRequestQueue, value);
        }

        private ObservableCollection<SongRequest> _songRequestHistory = new ObservableCollection<SongRequest>();
        public ObservableCollection<SongRequest> SongRequestHistory
        {
            get => _songRequestHistory;
            set => SetProperty(ref _songRequestHistory, value);
        }

        private int _volume;
        public int Volume
        {
            get => _volume;
            set
            {
                AppSettings.Volume = value;
                _ = youtubeSongService.SetVolume(value);
                SetProperty(ref _volume, value);
            }
        }

        private int _position;
        public int Position
        {
            get => _position;
            set
            {
                CurrentSong.Service?.SetPosition(value);
                SetProperty(ref _position, value);
            }
        }

        private ConnectionsStatus _connections = new ConnectionsStatus();
        public ConnectionsStatus Connections
        {
            get => _connections;
            set => SetProperty(ref _connections, value);
        }

        public IEnumerable<SongRequestPlatform> SongRequestPlatforms
        {
            get => Enum.GetValues(typeof(SongRequestPlatform)).Cast<SongRequestPlatform>();
        }

        public IEnumerable<WebBrowser> WebBrowsers
        {
            get => Enum.GetValues(typeof(WebBrowser)).Cast<WebBrowser>();
        }

        public ICommand PlayCommand => new RelayCommand(Play);
        public ICommand PauseCommand => new RelayCommand(Pause);
        public ICommand SkipCommand => new RelayCommand(Skip);
        public ICommand OpenSongUrlCommand => new RelayCommand<string?>((e) => OpenSongUrl(e));
        public ICommand RemoveSongQueueCommand => new RelayCommand<SongRequest>((e) => RemoveSongQueue(e));
        public ICommand RemoveSongHistoryCommand => new RelayCommand<SongRequest>((e) => RemoveSongHistory(e));
        public ICommand ReplaySongHistoryCommand => new RelayCommand<SongRequest>((e) => ReplaySongHistory(e));
        public ICommand ClearQueueCommand => new RelayCommand<bool?>((e) => ClearQueue(e));
        public ICommand ClearHistoryCommand => new RelayCommand(ClearHistory);
        public ICommand ConnectStreamerCommand => new RelayCommand(ConnectStreamer);
        public ICommand ConnectBotCommand => new RelayCommand(ConnectBot);
        public ICommand CreateRewardCommand => new RelayCommand(CreateReward);
        public ICommand ConnectSpotifyCommand => new RelayCommand(ConnectSpotify);
        public ICommand OpenSetupCommand => new RelayCommand(OpenSetup);
        public ICommand SaveSetupCommand => new RelayCommand(() => CloseSetup(true));
        public ICommand CloseSetupCommand => new RelayCommand(() => CloseSetup(false));
        public ICommand OpenSettingsCommand => new RelayCommand(OpenSettings);
        public ICommand SaveSettingsCommand => new RelayCommand(() => CloseSettings(true));
        public ICommand CloseSettingsCommand => new RelayCommand(() => CloseSettings(false));

        private async void Play()
        {
            bool result = await _currentSong.Service!.Play();
            PlaybackStatus = result ? PlaybackStatus.PLAYING : PlaybackStatus.ERROR;
        }

        private async void Pause()
        {
            bool result = await _currentSong.Service!.Pause();
            PlaybackStatus = PlaybackStatus.PAUSED;
        }

        private async void Skip()
        {
            //bool result = await _currentSong.Service.Skip();

            //TODO: Skip song
            string url = "https://www.youtube.com/watch?v=41VlNOyPD9U?t=10s&autoplay=1";
            string[] urlSplit = url.Split('?', '&');
            string embedUrl = (urlSplit[0] + "?" + urlSplit[1]).Replace("watch?v=", "embed/") + "?autoplay=1";
            string videoId = urlSplit[1].Replace("v=", "");
            var info = await youtubeSongService.GetSongInfo(videoId);
            CurrentSong = new SongRequest()
            {
                SongName = info.SongName,
                Requester = "Test",
                Duration = info.Duration,
                Platform = SongRequestPlatform.YOUTUBE,
                Url = url,
                Service = youtubeSongService,
            };
            PlaybackStatus = PlaybackStatus.PLAYING;
            await youtubeSongService.PlaySong(videoId);
        }
        
        private void OpenSongUrl(string? url)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
        }

        private void RemoveSongQueue(SongRequest? e)
        {
            if (e != null)
            {
                SongRequestQueue.Remove(e);
            }
        }

        private void RemoveSongHistory(SongRequest? e)
        {
            if (e != null)
            {
                SongRequestHistory.Remove(e);
            }
        }

        private void ReplaySongHistory(SongRequest? e)
        {
            if (e != null)
            {
                SongRequestQueue.Add(e);
                SongRequestHistory.Remove(e);
            }
        }

        private void ClearQueue(bool? refundAll)
        {
            if (refundAll == true)
            {
                //TODO refund all channel points
            }
            SongRequestQueue.Clear();
        }

        private void ClearHistory()
        {
            SongRequestHistory.Clear();
        }

        private async void ConnectStreamer()
        {
            Connections.StreamerStatus = ConnectionStatus.CONNECTING;

            try
            {
                TwitchAccessToken twitchAccessToken = await twitchAuthService.GenerateTwitchAccesTokens(AppSettings.TwitchClientId!, AppSettings.TwitchClientSecret!, "channel:read:redemptions channel:manage:redemptions");
                string accountName = await twitchAuthService.ValidateTwitchAccessTokens(twitchAccessToken, AppSettings.TwitchClientId!, AppSettings.TwitchClientSecret!);
                AppSettings.StreamerAccessTokens = twitchAccessToken;
                AppSettings.TwitchStreamerName = accountName;
                Connections.StreamerStatus = ConnectionStatus.CONNECTED;
            }
            catch (Exception ex)
            {
                // TODO: Log error
                Connections.StreamerStatus = ConnectionStatus.ERROR;
            }

            OnPropertyChanged(nameof(AppSettings));
        }

        private async void ConnectBot()
        {
            Connections.BotStatus = ConnectionStatus.CONNECTING;

            try
            {
                TwitchAccessToken twitchAccessToken = await twitchAuthService.GenerateTwitchAccesTokens(AppSettings.TwitchClientId!, AppSettings.TwitchClientSecret!, "chat:read chat:edit");
                string accountName = await twitchAuthService.ValidateTwitchAccessTokens(twitchAccessToken, AppSettings.TwitchClientId!, AppSettings.TwitchClientSecret!);
                AppSettings.BotAccessTokens = twitchAccessToken;
                AppSettings.TwitchBotName = accountName;
                Connections.BotStatus = ConnectionStatus.CONNECTED;
            }
            catch (Exception ex)
            {
                // TODO: Log error
                Connections.BotStatus = ConnectionStatus.ERROR;
            }

            OnPropertyChanged(nameof(AppSettings));
        }

        private void CreateReward()
        {
            //TODO: Create Twitch reward
        }

        private void ConnectSpotify()
        {
            //TODO: Connect to Spotify
        }

        private void OpenSettings()
        {
            IsSettingsOpen = true;
        }

        private void CloseSettings(bool save)
        {
            if (save)
            {
                SaveAppSettings();
            }
            IsSettingsOpen = false;
        }

        private void OpenSetup()
        {
            IsSetupOpen = true;
        }

        private void CloseSetup(bool save)
        {
            if (save)
            {
                SaveAppSettings();
            }
            IsSetupOpen = false;
        }

        internal void SaveAppSettings()
        {
            appSettingsService.SaveAppSettings(AppSettings);
        }

        internal void AddSongToQueue(string input)
        {
            input = input.Trim();
            if (input.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) || input.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
            {
                
            } 
            else if (input.Contains("spotify.com", StringComparison.OrdinalIgnoreCase))
            {

            } 
            else
            {

            }
        }

        private static List<string> GetPlaybackDevices()
        {
            using var devices = new MMDeviceEnumerator();
            var playbackDevices = new List<string>();
            foreach (var device in devices.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                playbackDevices.Add(device.FriendlyName);
            }
            return playbackDevices;
        }

        private string GetDefaultPlaybackDevice()
        {
            using (var devices = new MMDeviceEnumerator())
            {
                return devices.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).FriendlyName;
            }
        }
    }
}
