using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TwitchSongRequest.Helpers;
using TwitchSongRequest.Model;
using TwitchSongRequest.Services;

namespace TwitchSongRequest.ViewModel
{
    internal class MainViewViewModel : ObservableObject
    {
        private readonly IAppSettingsService appSettingsService;

        private readonly DispatcherTimer dispatcherTimer;

        public MainViewViewModel()
        {
            appSettingsService = App.Current.Services.GetService<IAppSettingsService>()!;

            _appSettings = appSettingsService.GetAppSettings();
            _playbackDevice = _appSettings.PlaybackDevice ?? GetDefaultPlaybackDevice();
            _volume = _appSettings.Volume ?? 100;

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
                _ = App.YoutubeSongService.SetPlaybackDevice(value);
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
                _ = App.YoutubeSongService.SetVolume(value);
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
                progress.ProgressChanged += (_, s) => Connections.StreamerStatus = s;
                AppSettings.StreamerInfo.Scope = "channel:read:redemptions channel:manage:redemptions";
                (AppSettings.StreamerAccessTokens, AppSettings.StreamerInfo.AccountName) = await ConnectToService(App.TwitchAuthService, AppSettings.TwitchClient!, AppSettings.StreamerInfo, progress, streamerCts.Token);
            }
            catch (Exception ex)
            {
                //TODO: Log error
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
            Progress<ConnectionStatus> progress = new();

            try
            {
                progress.ProgressChanged += (_, s) => Connections.BotStatus = s;
                AppSettings.BotInfo.Scope = "chat:read chat:edit";
                (AppSettings.BotAccessTokens, AppSettings.BotInfo.AccountName) = await ConnectToService(App.TwitchAuthService, AppSettings.TwitchClient!, AppSettings.BotInfo, progress, botCts.Token);
            }
            catch (Exception ex)
            {
                //TODO: Log error
            }
            finally
            {
                botCts.Cancel();
                botCts = null;
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
            Progress<ConnectionStatus> progress = new();

            try
            {
                progress.ProgressChanged += (_, s) => Connections.SpotifyStatus = s;
                AppSettings.SpotifyInfo.Scope = "user-modify-playback-state";
                (AppSettings.SpotifyAccessTokens, AppSettings.SpotifyInfo.AccountName) = await ConnectToService(App.SpotifyAuthService, AppSettings.SpotifyClient!, AppSettings.SpotifyInfo, progress, spotifyCts.Token);
            }
            catch (Exception ex)
            {
                //TODO: Log error
            }
            finally
            {
                spotifyCts.Cancel();
                spotifyCts = null;
            }

            OnPropertyChanged(nameof(AppSettings));
        }

        private async Task<Tuple<ServiceOAuthToken, string>> ConnectToService(IAuthService authService, ClientCredentials credentials, ClientInfo info, IProgress<ConnectionStatus> progress, CancellationToken ctsToken)
        {
            progress.Report(ConnectionStatus.CONNECTING);

            try
            {
                ServiceOAuthToken tokens = await authService.GenerateOAuthTokens(credentials, info, ctsToken);
                string accountName = await authService.ValidateOAuthTokens(tokens);
                progress.Report(ConnectionStatus.CONNECTED);
                return new Tuple<ServiceOAuthToken, string>(tokens, accountName);
            }
            catch
            {
                progress.Report(ConnectionStatus.ERROR);
                throw;
            }
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
                AppSettings = appSettingsService.ResetAppSettings();
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
                appSettingsService.SaveAppSettings(AppSettings);
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
            await App.YoutubeSongService.SetPlaybackDevice(playbackDevice);
            await App.YoutubeSongService.SetVolume(volume);
        }

        private void ValidateLogins()
        {
            if (AppSettings.StreamerAccessTokens.AccessToken != null)
            {
                ValidateStreamerLogin();
            }
            if (AppSettings.BotAccessTokens.AccessToken != null)
            {
                ValidateBotLogin();
            }
            if (AppSettings.SpotifyAccessTokens.AccessToken != null)
            {
                ValidateSpotifyLogin();
            }
        }

        private async void ValidateStreamerLogin()
        {
            Progress<ConnectionStatus> progress = new();
            try
            {
                progress.ProgressChanged += (_, s) => Connections.StreamerStatus = s;
                (AppSettings.StreamerAccessTokens, AppSettings.StreamerInfo.AccountName) = await ValidateLogin(App.TwitchAuthService, AppSettings.StreamerAccessTokens!, progress);
            }
            catch (Exception ex)
            {
                //TODO: log error
            }
            finally
            {
                progress.ProgressChanged -= (_, s) => Connections.StreamerStatus = s;
            }
        }

        private async void ValidateBotLogin()
        {
            Progress<ConnectionStatus> progress = new();
            try
            {
                progress.ProgressChanged += (_, s) => Connections.BotStatus = s;
                (AppSettings.BotAccessTokens, AppSettings.BotInfo.AccountName) = await ValidateLogin(App.TwitchAuthService, AppSettings.BotAccessTokens!, progress);
            }
            catch (Exception ex)
            {
                //TODO: log error
            }
            finally
            {
                progress.ProgressChanged -= (_, s) => Connections.BotStatus = s;
            }
        }

        private async void ValidateSpotifyLogin()
        {
            Progress<ConnectionStatus> progress = new();
            try
            {
                progress.ProgressChanged += (_, s) => Connections.SpotifyStatus = s;
                (AppSettings.SpotifyAccessTokens, AppSettings.SpotifyInfo.AccountName) = await ValidateLogin(App.SpotifyAuthService, AppSettings.SpotifyAccessTokens!, progress);
            }
            catch (Exception ex)
            {
                //TODO: log error
            }
            finally
            {
                progress.ProgressChanged -= (_, s) => Connections.SpotifyStatus = s;
            }
        }

        private async Task<Tuple<ServiceOAuthToken, string>> ValidateLogin(IAuthService authService, ServiceOAuthToken tokens, IProgress<ConnectionStatus> progress)
        {
            progress.Report(ConnectionStatus.CONNECTING);

            // 1. try to validate token
            try
            {
                string accountName = await authService.ValidateOAuthTokens(tokens);
                progress.Report(ConnectionStatus.CONNECTED);
                return new Tuple<ServiceOAuthToken, string>(tokens, accountName);
            }
            catch
            {
                progress.Report(ConnectionStatus.REFRESHING);
            }

            // 2. if not logged in, try to refresh token
            try
            {
                tokens = await authService.RefreshOAuthTokens(tokens, AppSettings.TwitchClient!);
                progress.Report(ConnectionStatus.CONNECTED);
            }
            catch
            {
                progress.Report(ConnectionStatus.ERROR);
                throw;
            }

            // 3. revalidate token
            try
            {
                string accountName = await authService.ValidateOAuthTokens(tokens);
                progress.Report(ConnectionStatus.CONNECTED);
                return new Tuple<ServiceOAuthToken, string>(tokens, accountName);
            }
            catch
            {
                progress.Report(ConnectionStatus.ERROR);
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
                    Service = i % 2 == 0 ? App.YoutubeSongService : App.SpotifySongService
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
                    Service = i % 2 == 0 ? App.YoutubeSongService : App.SpotifySongService
                });
            }
        }

        private async void TestAddYoutubeSong()
        {
            string url = "https://www.youtube.com/watch?v=41VlNOyPD9U?t=10s&autoplay=1";
            string[] urlSplit = url.Split('?', '&');
            string embedUrl = (urlSplit[0] + "?" + urlSplit[1]).Replace("watch?v=", "embed/") + "?autoplay=1";
            string videoId = urlSplit[1].Replace("v=", "");
            var info = await App.YoutubeSongService.GetSongInfo(videoId);
            CurrentSong = new SongRequest()
            {
                SongName = info.SongName,
                Requester = "Test",
                Duration = info.Duration,
                Platform = SongRequestPlatform.YOUTUBE,
                Url = url,
                Service = App.YoutubeSongService,
            };
        }
    }
}
