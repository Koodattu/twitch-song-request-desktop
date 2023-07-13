using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TwitchSongRequest.Helpers;
using TwitchSongRequest.Model;
using TwitchSongRequest.Services;
using TwitchSongRequest.Services.Api;
using TwitchSongRequest.Services.Authentication;

namespace TwitchSongRequest.ViewModel
{
    internal class MainViewViewModel : ObservableObject
    {
        private readonly IAppSettingsService _appSettingsService;

        private readonly ITwitchAuthService _twitchAuthService;
        private readonly ISpotifyAuthService _spotifyAuthService;

        private readonly ISpotifySongService _spotifySongService;
        private readonly IYoutubeSongService _youtubeSongService;

        private readonly DispatcherTimer dispatcherTimer;

        public MainViewViewModel(IAppSettingsService appSettingsService, ITwitchAuthService twitchAuthService, ISpotifyAuthService spotifyAuthService, ISpotifySongService spotifySongService, IYoutubeSongService youtubeSongService)
        {
            _appSettingsService = appSettingsService;

            _twitchAuthService = twitchAuthService;
            _spotifyAuthService = spotifyAuthService;

            _spotifySongService = spotifySongService;
            _youtubeSongService = youtubeSongService;

            _playbackDevice = AppSettings.PlaybackDevice ?? GetDefaultPlaybackDevice();
            _volume = AppSettings.Volume ?? 100;

            SetupYoutubeService(_playbackDevice, _volume);
            ValidateLogins();

            GenerateMockRequests();

            dispatcherTimer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, TimerCallback, Application.Current.Dispatcher);
            dispatcherTimer.Start();
        }

        private string _playbackDevice;
        public string PlaybackDevice
        {
            get => _playbackDevice;
            set
            {
                AppSettings.PlaybackDevice = value;
                _ = _youtubeSongService.SetPlaybackDevice(value);
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

        public AppSettings AppSettings
        {
            get => _appSettingsService.AppSettings;
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
                _ = _youtubeSongService.SetVolume(value);
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

        private ConfirmationDialogViewModel _confirmationDialogViewModel = new ConfirmationDialogViewModel();
        public ConfirmationDialogViewModel ConfirmationDialogViewModel
        {
            get => _confirmationDialogViewModel;
            set => SetProperty(ref _confirmationDialogViewModel, value);
        }


        public ICommand PlayCommand => new RelayCommand(Play);
        public ICommand PauseCommand => new RelayCommand(Pause);
        public ICommand SkipCommand => new RelayCommand(Skip);
        public ICommand ProcessStartUriCommand => new RelayCommand<string?>((e) => ProcessStartUri(e));
        public ICommand ProcessStartBrowserUrlCommand => new RelayCommand<Tuple<WebBrowser, string>>((e) => ProcessStartBrowserUrl(e));
        public ICommand RemoveSongQueueCommand => new RelayCommand<SongRequest>((e) => RemoveSongQueue(e));
        public ICommand RemoveSongHistoryCommand => new RelayCommand<SongRequest>((e) => RemoveSongHistory(e));
        public ICommand ReplaySongHistoryCommand => new RelayCommand<SongRequest>((e) => ReplaySongHistory(e));
        public ICommand ClearQueueCommand => new RelayCommand<bool?>((e) => ClearQueue(e));
        public ICommand ClearHistoryCommand => new RelayCommand(ClearHistory);
        public ICommand ConnectStreamerCommand => new RelayCommand(ConnectOrCancelStreamer);
        public ICommand ConnectBotCommand => new RelayCommand(ConnectOrCancelBot);
        public ICommand ConnectSpotifyCommand => new RelayCommand(ConnectOrCancelSpotify);
        public ICommand CreateRewardCommand => new RelayCommand(CreateReward);
        public ICommand OpenSetupCommand => new RelayCommand(OpenSetup);
        public ICommand SaveSetupCommand => new RelayCommand(() => CloseSetup(true));
        public ICommand CloseSetupCommand => new RelayCommand(() => CloseSetup(false));
        public ICommand OpenSettingsCommand => new RelayCommand(OpenSettings);
        public ICommand SaveSettingsCommand => new RelayCommand(() => CloseSettings(true));
        public ICommand CloseSettingsCommand => new RelayCommand(() => CloseSettings(false));
        public ICommand ResetSetupCommand => new RelayCommand(ResetSetup);
        public ICommand ResetSettingsCommand => new RelayCommand(ResetSettings);

        private async void Play()
        {
            bool result = await _currentSong.Service!.Play();
            PlaybackStatus = result ? PlaybackStatus.PLAYING : PlaybackStatus.ERROR;
        }

        private async void Pause()
        {
            bool result = await _currentSong.Service!.Pause();
            PlaybackStatus = result ? PlaybackStatus.PAUSED : PlaybackStatus.ERROR;
        }

        private async void Skip()
        {
            if (SongRequestQueue.Count > 0)
            {
                CurrentSong = SongRequestQueue.First();
                SongRequestQueue.Remove(CurrentSong);
            }
            else
            {
                CurrentSong = new SongRequest();
            }
        }
        
        private void ProcessStartUri(string? uri)
        {
            if (!string.IsNullOrWhiteSpace(uri))
            {
                Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
            }
        }

        private void ProcessStartBrowserUrl(Tuple<WebBrowser, string> browserStartInfo)
        {
            WebBrowserLauncher.Launch(browserStartInfo.Item1, browserStartInfo.Item2);
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

        private async void ClearQueue(bool? refundAll)
        {
            bool result = await ConfirmationDialogViewModel.ShowDialog("Clear Queue", $"Are you sure you want to clear the song queue {(refundAll == true ? "and refund all" : string.Empty)}?");
            if (result)
            {
                if (refundAll == true)
                {
                    //TODO refund all channel points
                }
                SongRequestQueue.Clear();
            }

        }

        private async void ClearHistory()
        {
            bool result = await ConfirmationDialogViewModel.ShowDialog("Clear History", "Are you sure you want to clear the song history?");
            if (result)
            {
                SongRequestHistory.Clear();
            }
        }

        private CancellationTokenSource? streamerCts;
        private async void ConnectOrCancelStreamer()
        {
            if (streamerCts != null)
            {
                streamerCts.Cancel();
                streamerCts = null;
                Connections.StreamerStatus = ConnectionStatus.CANCELLED;
                return;
            }

            streamerCts = new CancellationTokenSource();
            Progress<ConnectionStatus> progress = new();

            try
            {
                AppSettings.StreamerInfo.Scope = "chat:read channel:read:redemptions channel:manage:redemptions";
                AppSettings.StreamerAccessTokens = await _twitchAuthService.GenerateStreamerOAuthTokens(streamerCts.Token);
                AppSettings.StreamerInfo.AccountName = await _twitchAuthService.ValidateStreamerOAuthTokens();
                Connections.StreamerStatus = ConnectionStatus.CONNECTED;
            }
            catch (Exception ex)
            {
                //TODO: Log error
                Connections.StreamerStatus = ConnectionStatus.ERROR;
            }
            finally
            {
                if (streamerCts != null)
                {
                    streamerCts.Cancel();
                    streamerCts = null;
                }
            }

            OnPropertyChanged(nameof(AppSettings));
        }

        private CancellationTokenSource? botCts;
        private async void ConnectOrCancelBot()
        {
            if (botCts != null)
            {
                botCts.Cancel();
                botCts = null;
                Connections.BotStatus = ConnectionStatus.CANCELLED;
                return;
            }

            botCts = new CancellationTokenSource();

            try
            {
                AppSettings.BotInfo.Scope = "chat:edit";
                AppSettings.BotAccessTokens= await _twitchAuthService.GenerateBotOAuthTokens(botCts.Token);
                AppSettings.BotInfo.AccountName = await _twitchAuthService.ValidateBotOAuthTokens();
                Connections.BotStatus = ConnectionStatus.CONNECTED;
            }
            catch (Exception ex)
            {
                //TODO: Log error
                Connections.BotStatus = ConnectionStatus.ERROR;
            }
            finally
            {
                if (botCts != null)
                {
                    botCts.Cancel();
                    botCts = null;
                }
            }

            OnPropertyChanged(nameof(AppSettings));
        }

        private CancellationTokenSource? spotifyCts;
        private async void ConnectOrCancelSpotify()
        {
            if (spotifyCts != null)
            {
                spotifyCts.Cancel();
                spotifyCts = null;
                Connections.SpotifyStatus = ConnectionStatus.CANCELLED;
                return;
            }

            spotifyCts = new CancellationTokenSource();

            try
            {
                AppSettings.SpotifyInfo.Scope = "user-modify-playback-state user-read-playback-state user-read-currently-playing";
                AppSettings.SpotifyAccessTokens = await _spotifyAuthService.GenerateOAuthTokens(spotifyCts.Token);
                AppSettings.SpotifyInfo.AccountName = await _spotifyAuthService.ValidateOAuthTokens();
                Connections.SpotifyStatus = ConnectionStatus.CONNECTED;
            }
            catch (Exception ex)
            {
                //TODO: Log error
                Connections.SpotifyStatus = ConnectionStatus.ERROR;
            }
            finally
            {
                if (spotifyCts != null)
                {
                    spotifyCts.Cancel();
                    spotifyCts = null;
                }
            }

            OnPropertyChanged(nameof(AppSettings));
        }

        private void CreateReward()
        {
            //TODO: Create Twitch reward
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

        private async void ResetSetup()
        {
            bool result = await ConfirmationDialogViewModel.ShowDialog("Reset Setup", "Are you sure you want to reset everything?");
            if (!result)
            {
                return;
            }

            try
            {
                _appSettingsService.ResetAppSettings();
            }
            catch (Exception ex)
            {
                //TODO: Log error
            }
            OnPropertyChanged(nameof(AppSettings));
        }

        private void ResetSettings()
        {
            //TODO: Reset settings
        }

        internal void SaveAppSettings()
        {
            try
            {
                _appSettingsService.SaveAppSettings();
            }
            catch (Exception ex)
            {
                //TODO: Log error
            }
        }

        internal void AddSongToQueue(string input)
        {
            input = input.Trim();
            if (input.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) || input.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
            {
                //TODO: Add youtube song
            } 
            else if (input.Contains("spotify.com", StringComparison.OrdinalIgnoreCase))
            {
                //TODO: Add spotify song
            }
            else if (input.Contains("soundcloud.com", StringComparison.OrdinalIgnoreCase))
            {
                //TODO: Add soundcloud song
            }
            else
            {
                //TODO: Search for song using selected platform
            }
        }

        private async void SetupYoutubeService(string playbackDevice, int volume)
        {
            await _youtubeSongService.SetPlaybackDevice(playbackDevice);
            await _youtubeSongService.SetVolume(volume);
        }

        private async void ValidateLogins()
        {
            if (AppSettings.StreamerAccessTokens.AccessToken != null)
            {
                try
                {
                    await ValidateTwitchStreamerLogin();
                }
                catch (Exception ex)
                {
                    //TODO: Log error
                }
            }
            if (AppSettings.BotAccessTokens.AccessToken != null)
            {
                try
                {
                    await ValidateTwitchBotLogin();
                }
                catch (Exception ex)
                {
                    //TODO: Log error
                }
            }
            if (AppSettings.SpotifyAccessTokens.AccessToken != null)
            {
                try
                {
                    await ValidateSpotifyLogin();
                }
                catch (Exception ex)
                {
                    //TODO: Log error
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        private async Task ValidateTwitchStreamerLogin()
        {
            Connections.StreamerStatus = ConnectionStatus.CONNECTING;

            // 1. try to validate token
            try
            {
                AppSettings.StreamerInfo.AccountName = await _twitchAuthService.ValidateStreamerOAuthTokens();
                Connections.StreamerStatus = ConnectionStatus.CONNECTED;
            }
            catch
            {
                Connections.StreamerStatus = ConnectionStatus.REFRESHING;
            }

            // 2. if not logged in, try to refresh token
            try
            {
                AppSettings.StreamerAccessTokens = await _twitchAuthService.RefreshStreamerOAuthTokens();
                Connections.StreamerStatus = ConnectionStatus.CONNECTED;
            }
            catch
            {
                Connections.StreamerStatus = ConnectionStatus.ERROR;
                throw;
            }

            // 3. revalidate token
            try
            {
                AppSettings.StreamerInfo.AccountName = await _twitchAuthService.ValidateStreamerOAuthTokens();
                Connections.StreamerStatus = ConnectionStatus.CONNECTED;
            }
            catch
            {
                Connections.StreamerStatus = ConnectionStatus.ERROR;
                throw;
            }
        }

        private async Task ValidateTwitchBotLogin()
        {
            Connections.BotStatus = ConnectionStatus.CONNECTING;

            // 1. try to validate token
            try
            {
                AppSettings.BotInfo.AccountName = await _twitchAuthService.ValidateBotOAuthTokens();
                Connections.BotStatus = ConnectionStatus.CONNECTED;
            }
            catch
            {
                Connections.BotStatus = ConnectionStatus.REFRESHING;
            }

            // 2. if not logged in, try to refresh token
            try
            {
                AppSettings.BotAccessTokens = await _twitchAuthService.RefreshBotOAuthTokens();
                Connections.BotStatus = ConnectionStatus.CONNECTED;
            }
            catch
            {
                Connections.BotStatus = ConnectionStatus.ERROR;
                throw;
            }

            // 3. revalidate token
            try
            {
                AppSettings.BotInfo.AccountName = await _twitchAuthService.ValidateBotOAuthTokens();
                Connections.BotStatus = ConnectionStatus.CONNECTED;
            }
            catch
            {
                Connections.BotStatus = ConnectionStatus.ERROR;
                throw;
            }
        }

        private async Task ValidateSpotifyLogin()
        {
            Connections.SpotifyStatus = ConnectionStatus.CONNECTING;

            // 1. try to validate token
            try
            {
                AppSettings.SpotifyInfo.AccountName = await _spotifyAuthService.ValidateOAuthTokens();
                Connections.SpotifyStatus = ConnectionStatus.CONNECTED;
            }
            catch
            {
                Connections.SpotifyStatus = ConnectionStatus.REFRESHING;
            }

            // 2. if not logged in, try to refresh token
            try
            {
                AppSettings.SpotifyAccessTokens = await _spotifyAuthService.RefreshOAuthTokens();
                Connections.SpotifyStatus = ConnectionStatus.CONNECTED;
            }
            catch
            {
                Connections.SpotifyStatus = ConnectionStatus.ERROR;
                throw;
            }

            // 3. revalidate token
            try
            {
                AppSettings.SpotifyInfo.AccountName = await _spotifyAuthService.ValidateOAuthTokens();
                Connections.SpotifyStatus = ConnectionStatus.CONNECTED;
            }
            catch
            {
                Connections.SpotifyStatus = ConnectionStatus.ERROR;
                throw;
            }
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
                    Service = i % 2 == 0 ? _youtubeSongService : _spotifySongService
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
                    Service = i % 2 == 0 ? _youtubeSongService : _spotifySongService
                });
            }
        }

        private async void TestAddYoutubeSong()
        {
            string url = "https://www.youtube.com/watch?v=41VlNOyPD9U?t=10s&autoplay=1";
            string[] urlSplit = url.Split('?', '&');
            string embedUrl = (urlSplit[0] + "?" + urlSplit[1]).Replace("watch?v=", "embed/") + "?autoplay=1";
            string videoId = urlSplit[1].Replace("v=", "");
            var info = await _youtubeSongService.GetSongInfo(videoId);
            CurrentSong = new SongRequest()
            {
                SongName = info.SongName,
                Requester = "Test",
                Duration = info.Duration,
                Platform = SongRequestPlatform.YOUTUBE,
                Url = url,
                Service = _youtubeSongService,
            };
        }
    }
}
