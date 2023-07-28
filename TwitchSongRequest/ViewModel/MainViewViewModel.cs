using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.CoreAudioApi;
using NLog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TwitchLib.Client.Events;
using TwitchSongRequest.Helpers;
using TwitchSongRequest.Model;
using TwitchSongRequest.Services;
using TwitchSongRequest.Services.Api;
using TwitchSongRequest.Services.Authentication;

namespace TwitchSongRequest.ViewModel
{
    internal class MainViewViewModel : ObservableObject
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly IAppSettingsService _appSettingsService;

        private readonly ITwitchAuthService _twitchAuthService;
        private readonly ISpotifyAuthService _spotifyAuthService;

        private readonly ITwitchApiService _twitchApiService;

        private readonly ISpotifySongService _spotifySongService;
        private readonly IYoutubeSongService _youtubeSongService;

        private readonly DispatcherTimer dispatcherTimer;

        public MainViewViewModel(IAppSettingsService appSettingsService, ITwitchAuthService twitchAuthService, ISpotifyAuthService spotifyAuthService, ITwitchApiService twitchApiService, ISpotifySongService spotifySongService, IYoutubeSongService youtubeSongService)
        {
            _appSettingsService = appSettingsService;

            _twitchAuthService = twitchAuthService;
            _spotifyAuthService = spotifyAuthService;

            _twitchApiService = twitchApiService;

            _spotifySongService = spotifySongService;
            _youtubeSongService = youtubeSongService;

            _playbackDevice = AppSettings.PlaybackDevice ?? GetDefaultPlaybackDevice();
            _volume = AppSettings.Volume ?? 100;

            SetupYoutubeService(_playbackDevice, _volume);
            ValidateLogins();

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

        public AppTokens AppTokens
        {
            get => _appSettingsService.AppTokens;
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
        public ICommand CreateRewardCommand => new RelayCommand<string?>((e) => CreateReward(e));
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
            PlaybackStatus = result ? PlaybackStatus.Playing : PlaybackStatus.Error;
        }

        private async void Pause()
        {
            bool result = await _currentSong.Service!.Pause();
            PlaybackStatus = result ? PlaybackStatus.Paused : PlaybackStatus.Error;
        }

        private async void Skip()
        {
            if (SongRequestQueue.Count > 0)
            {
                CurrentSong = SongRequestQueue.First();
                SongRequestQueue.Remove(CurrentSong);
                LoadNextSong();
            }
            else
            {
                CurrentSong = new SongRequest();
            }
        }
        
        private void LoadNextSong()
        {
            CurrentSong?.Service?.PlaySong(CurrentSong.Id!);
        }

        private void ProcessStartUri(string? uri)
        {
            if (!string.IsNullOrWhiteSpace(uri))
            {
                Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
            }
        }

        private void ProcessStartBrowserUrl(Tuple<WebBrowser, string>? browserStartInfo)
        {
            WebBrowserLauncher.Launch(browserStartInfo!.Item1, browserStartInfo!.Item2);
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

        private CancellationTokenSource? connectCts;
        private async Task<bool> ConnectOrCancel(Action<ServiceOAuthToken> setTokens, Func<CancellationToken, Task<ServiceOAuthToken>> generateTokens, Func<Task<string>> validateTokens, Action<ConnectionStatus> setStatus, string account)
        {
            if (connectCts != null)
            {
                connectCts.Cancel();
                connectCts = null;
                setStatus(ConnectionStatus.Cancelled);
                return false;
            }

            setStatus(ConnectionStatus.Connecting);
            connectCts = new CancellationTokenSource();

            try
            {
                setTokens(await generateTokens(connectCts.Token));
                setStatus(ConnectionStatus.Authenticated);
                return true;
            }
            catch (HttpRequestException ex)
            {
                string dataString = string.Join(",", ex.Data.Values.Cast<object>().Select(v => v.ToString()));
                _logger.Log(LogLevel.Error, ex, $"Error connecting to {account}, Data: {dataString}");
                setStatus(ConnectionStatus.Error);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, $"Error connecting to {account}");
                setStatus(ConnectionStatus.Error);
            }
            finally
            {
                if (connectCts != null)
                {
                    connectCts.Cancel();
                    connectCts = null;
                }
            }

            OnPropertyChanged(nameof(AppSettings));

            return false;
        }

        private async void ConnectOrCancelStreamer()
        {
            AppTokens.StreamerInfo.Scope = "chat:edit channel:read:redemptions channel:manage:redemptions";
            var setTokensAction = new Action<ServiceOAuthToken>(tokens => AppTokens.StreamerAccessTokens = tokens);
            bool result = await ConnectOrCancel(setTokensAction, _twitchAuthService.GenerateStreamerOAuthTokens, _twitchAuthService.ValidateStreamerOAuthTokens, status => Connections.StreamerStatus = status, "Twitch Streamer");
            if (result)
            {
                await ValidateStreamerLogin();
                await ConnectTwitchClients();
            }
        }

        private async void ConnectOrCancelBot()
        {
            AppTokens.BotInfo.Scope = "chat:edit";
            var setTokensAction = new Action<ServiceOAuthToken>(tokens => AppTokens.BotAccessTokens = tokens);
            bool result = await ConnectOrCancel(setTokensAction, _twitchAuthService.GenerateBotOAuthTokens, _twitchAuthService.ValidateBotOAuthTokens, status => Connections.BotStatus = status, "Twitch Bot");
            if (result)
            {
                await ValidateBotLogin();
            }
        }

        private async void ConnectOrCancelSpotify()
        {
            AppTokens.SpotifyInfo.Scope = "user-modify-playback-state user-read-playback-state user-read-currently-playing";
            var setTokensAction = new Action<ServiceOAuthToken>(tokens => AppTokens.SpotifyAccessTokens = tokens);
            bool result = await ConnectOrCancel(setTokensAction, _spotifyAuthService.GenerateOAuthTokens, _spotifyAuthService.ValidateOAuthTokens, status => Connections.SpotifyStatus = status, "Spotify");
            if (result)
            {
                await ValidateSpotifyLogin();
            }
        }

        private async Task ConnectTwitchClients()
        {
            var client = await _twitchApiService.GetTwitchStreamerClient();
            client.OnMessageReceived += OnTwitchClientMessageReceived;
            _ = await _twitchApiService.GetTwitchBotClient();
        }

        private async void OnTwitchClientMessageReceived(object? sender, OnMessageReceivedArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.ChatMessage.CustomRewardId))
            {
                return;
            }
            if (e.ChatMessage.CustomRewardId != AppTokens.ChannelRedeemRewardId)
            {
                return;
            }

            string input = e.ChatMessage.Message;
            string? songName = await AddSongToQueue(input, e.ChatMessage.Username, "");

            if (!AppSettings.ReplyInChat)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(songName))
            {
                await _twitchApiService.ReplyToChatMessage(e.ChatMessage.Channel, e.ChatMessage.Id, $"Added {songName} to queue.");
            }
            else
            {
                await _twitchApiService.ReplyToChatMessage(e.ChatMessage.Channel, e.ChatMessage.Id, $"Failed to add {input} to queue.");
                await _twitchApiService.RefundRedeem(e.ChatMessage.Username, input);
            }
        }

        private async void CreateReward(string? rewardName)
        {
            _appSettingsService.AppTokens.RewardCreationStatus = RewardCreationStatus.Creating;
            OnPropertyChanged(nameof(AppTokens));
            try
            {
                string? rewardId = await _twitchApiService.CreateReward(rewardName!);
                if (!string.IsNullOrWhiteSpace(rewardId))
                {
                    _appSettingsService.AppTokens.ChannelRedeemRewardId = rewardId;
                    _appSettingsService.AppTokens.RewardCreationStatus = RewardCreationStatus.Created;
                }
                else
                {
                    _appSettingsService.AppTokens.RewardCreationStatus = RewardCreationStatus.AlreadyExists;
                }
            }
            catch (Exception ex)
            {
                _appSettingsService.AppTokens.RewardCreationStatus = RewardCreationStatus.Error;
                _logger.Log(LogLevel.Error, ex, "Error creating reward");
            }
            OnPropertyChanged(nameof(AppTokens));
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
                SaveAppSetup();
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
                _appSettingsService.ResetAppTokens();
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error resetting setup");
            }
            OnPropertyChanged(nameof(AppTokens));
        }

        private async void ResetSettings()
        {
            bool result = await ConfirmationDialogViewModel.ShowDialog("Reset Settings", "Are you sure you want to reset everything?");
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
                _logger.Log(LogLevel.Error, ex, "Error resetting settings");
            }
            OnPropertyChanged(nameof(AppSettings));
        }

        internal void SaveAppSettings()
        {
            try
            {
                _appSettingsService.SaveAppSettings();
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error saving settings");
            }
        }

        internal void SaveAppSetup()
        {
            try
            {
                _appSettingsService.SaveAppTokens();
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error saving settings");
            }
        }

        internal async Task<string?> AddSongToQueue(string input, string requester, string redeemRequestId)
        {
            input = input.Trim();

            string? songName = string.Empty;
            int? duration = 0;
            string? url = string.Empty;
            string? id = string.Empty;
            SongRequestPlatform? platform = null;
            ISongService? songService = null;

            if (input.Contains("youtube", StringComparison.OrdinalIgnoreCase) || input.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
            {
                //https://youtu.be/u54Kf3zxDso
                //https://www.youtube.com/watch?v=u54Kf3zxDso

                //TODO: Add youtube song
                platform = SongRequestPlatform.Youtube;
                songService = _youtubeSongService;

                string[] urlSplit = input.Split(new string[] { "?", "&" }, StringSplitOptions.RemoveEmptyEntries);
                string videoId = urlSplit[1].Replace("v=", "");

                var info = await _youtubeSongService.GetSongInfo(videoId);
                songName = info.SongName;
                duration = info.Duration;
                url = $"https://www.youtube.com/watch?v={videoId}";
                id = videoId;
            } 
            else if (input.Contains("spotify", StringComparison.OrdinalIgnoreCase))
            {
                //https://open.spotify.com/track/1hEh8Hc9lBAFWUghHBsCel?si=c4cdc1947a184ac0
                //spotify:track:6RIbDs0p4XusU2PZSiDgeZ

                //TODO: Add spotify song
                platform = SongRequestPlatform.Spotify;
                songService = _spotifySongService;

                string[] urlSplit = input.Split(new string[] { "track/", "track:", "?"}, StringSplitOptions.RemoveEmptyEntries);
                string trackId = urlSplit[1];

                var info = await _spotifySongService.GetSongInfo(trackId);
                songName = $"{info.SongName} - {info.Artist}";
                duration = info.Duration;
                url = $"https://open.spotify.com/track/{trackId}";
                id = trackId;
            }
            /*else if (input.Contains("soundcloud.com", StringComparison.OrdinalIgnoreCase))
            {
                //TODO: Add soundcloud song
                platform = SongRequestPlatform.Soundcloud;
                //songService = _soundcloudSongService;
            }
            else
            {
                //TODO: Search for song using selected platform
                platform = AppSettings.SongSearchPlatform;
            }*/

            SongRequest songRequest = new SongRequest
            {
                SongName = songName,
                Duration = duration,
                Requester = requester,
                Platform = platform,
                RedeemRequestId = redeemRequestId,
                Url = url,
                Id = id,
                Service = songService,
            };

            App.Current.Dispatcher.Invoke(delegate
            {
                SongRequestQueue.Add(songRequest);
            });

            return songName;
        }

        private async void SetupYoutubeService(string playbackDevice, int volume)
        {
            await _youtubeSongService.SetPlaybackDevice(playbackDevice);
            await _youtubeSongService.SetVolume(volume);
        }

        private async void ValidateLogins()
        {
            if (AppTokens.StreamerAccessTokens.AccessToken != null)
            {
                await ValidateStreamerLogin();
                await ConnectTwitchClients();
            }
            if (AppTokens.BotAccessTokens.AccessToken != null)
            {
                await ValidateBotLogin();
            }
            if (AppTokens.SpotifyAccessTokens.AccessToken != null)
            {
                await ValidateSpotifyLogin();
            }
        }

        private async Task ValidateStreamerLogin()
        {
            try
            {
                var statusAction = new Action<ConnectionStatus>(status => Connections.StreamerStatus = status);
                var setTokensAction = new Action<ServiceOAuthToken>(tokens => AppTokens.StreamerAccessTokens = tokens);
                var setNameAction = new Action<string>(name => AppTokens.StreamerInfo.AccountName = name);
                await ValidateLogin(statusAction, _twitchAuthService.ValidateStreamerOAuthTokens, _twitchAuthService.RefreshStreamerOAuthTokens, setTokensAction, setNameAction);
            }
            catch (HttpRequestException ex)
            {
                string dataString = string.Join(",", ex.Data.Values.Cast<object>().Select(v => v.ToString()));
                _logger.Log(LogLevel.Error, ex, $"Error validating Twitch Streamer login, Data: {dataString}");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error validating Twitch Streamer login");
            }
        }

        private async Task ValidateBotLogin()
        {
            try
            {
                var statusAction = new Action<ConnectionStatus>(status => Connections.BotStatus = status);
                var setTokensAction = new Action<ServiceOAuthToken>(tokens => AppTokens.BotAccessTokens = tokens);
                var setNameAction = new Action<string>(name => AppTokens.BotInfo.AccountName = name);
                await ValidateLogin(statusAction, _twitchAuthService.ValidateBotOAuthTokens, _twitchAuthService.RefreshBotOAuthTokens, setTokensAction, setNameAction);
            }
            catch (HttpRequestException ex)
            {
                string dataString = string.Join(",", ex.Data.Values.Cast<object>().Select(v => v.ToString()));
                _logger.Log(LogLevel.Error, ex, $"Error validating Twitch Bot login, Data: {dataString}");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error validating Twitch Bot login");
            }
        }

        private async Task ValidateSpotifyLogin()
        {
            try
            {
                var statusAction = new Action<ConnectionStatus>(status => Connections.SpotifyStatus = status);
                var setTokensAction = new Action<ServiceOAuthToken>(tokens => AppTokens.SpotifyAccessTokens = tokens);
                var setNameAction = new Action<string>(name => AppTokens.SpotifyInfo.AccountName = name);
                await ValidateLogin(statusAction, _spotifyAuthService.ValidateOAuthTokens, _spotifyAuthService.RefreshOAuthTokens, setTokensAction, setNameAction);
            }
            catch (HttpRequestException ex)
            {
                string dataString = string.Join(",", ex.Data.Values.Cast<object>().Select(v => v.ToString()));
                _logger.Log(LogLevel.Error, ex, $"Error validating Spotify login, Data: {dataString}");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error validating Spotify login");
            }
        }

        private async Task ValidateLogin(Action<ConnectionStatus> setStatus, Func<Task<string>> validateTokens, Func<Task<ServiceOAuthToken>> refreshTokens, Action<ServiceOAuthToken> setTokens, Action<string> setAccountName)
        {
            setStatus(ConnectionStatus.Connecting);

            // 1. try to validate token
            try
            {
                setAccountName(await validateTokens());
                setStatus(ConnectionStatus.Connected);
            }
            catch
            {
                setStatus(ConnectionStatus.Refreshing);
            }

            // 2. if not logged in, try to refresh token
            try
            {
                setTokens(await refreshTokens());
                setStatus(ConnectionStatus.Connected);
            }
            catch
            {
                setStatus(ConnectionStatus.Error);
                throw;
            }

            // 3. revalidate token
            try
            {
                setAccountName(await validateTokens());
                setStatus(ConnectionStatus.Connected);
            }
            catch
            {
                setStatus(ConnectionStatus.Error);
                throw;
            }
        }

        private async void TimerCallback(object? sender, EventArgs e)
        {
            if (PlaybackStatus != PlaybackStatus.Playing)
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
            var playbackDevices = new List<string>();

            try
            {
                using var devices = new MMDeviceEnumerator();
                foreach (var device in devices.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                {
                    playbackDevices.Add(device.FriendlyName);
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error getting playback devices");
            }

            return playbackDevices;
        }

        private string GetDefaultPlaybackDevice()
        {
            try
            {
                using (var devices = new MMDeviceEnumerator())
                {
                    return devices.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).FriendlyName;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error getting default playback device");
            }

            return string.Empty;
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
                    Platform = i % 2 == 0 ? SongRequestPlatform.Youtube : SongRequestPlatform.Spotify,
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
                    Platform = i % 2 == 0 ? SongRequestPlatform.Youtube : SongRequestPlatform.Spotify,
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
                Platform = SongRequestPlatform.Youtube,
                Url = url,
                Service = _youtubeSongService,
            };
        }
    }
}
