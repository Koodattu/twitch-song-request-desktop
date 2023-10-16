using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchSongRequest.Helpers;
using TwitchSongRequest.Model;
using TwitchSongRequest.Services.Api;
using TwitchSongRequest.Services.App;
using TwitchSongRequest.Services.Authentication;

namespace TwitchSongRequest.ViewModel
{
    internal class MainViewViewModel : ObservableObject
    {
        private readonly ILoggerService _loggerService;
        private readonly IAppFilesService _appFilesService;

        private readonly ITwitchAuthService _twitchAuthService;
        private readonly ISpotifyAuthService _spotifyAuthService;

        private readonly ITwitchApiService _twitchApiService;

        private readonly ISpotifySongService _spotifySongService;
        private readonly IYoutubeSongService _youtubeSongService;

        private readonly DispatcherTimer everySecondTimer;
        private readonly DispatcherTimer fifteenSecondTimer;

        private bool _isSetupDone = false;

        public MainViewViewModel(ILoggerService loggerService, IAppFilesService appSettingsService, ITwitchAuthService twitchAuthService, ISpotifyAuthService spotifyAuthService, ITwitchApiService twitchApiService, ISpotifySongService spotifySongService, IYoutubeSongService youtubeSongService)
        {
            _loggerService = loggerService;
            _loggerService.LogInfo("Setting up MainViewViewModel");
            _loggerService.StatusEvent += (statusEvent) => 
            {
                if (StatusText != null)
                {
                    App.Current.Dispatcher.Invoke(delegate
                    {
                        StatusFeed.Insert(StatusFeed.Count, StatusText);
                    });
                }
                StatusText = statusEvent;
            };

            _appFilesService = appSettingsService;
            ReadLogsToStatusFeed();

            _twitchAuthService = twitchAuthService;
            _spotifyAuthService = spotifyAuthService;

            _twitchApiService = twitchApiService;

            _spotifySongService = spotifySongService;
            _youtubeSongService = youtubeSongService;

            _playbackDevices = new ObservableCollection<string>(GetPlaybackDevices());
            _songRequestQueue = new ObservableCollection<SongRequest>(GetSavedSongRequestQueue());
            _songRequestHistory = new ObservableCollection<SongRequest>(GetSavedSongRequestHistory());

            _playbackDevice = _appFilesService.AppSettings.PlaybackDevice ?? GetDefaultPlaybackDevice();
            _volume = AppSettings.Volume ?? 100;

            SetupYoutubeService(_playbackDevice, _volume);
            ValidateLogins();

            everySecondTimer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, EverySecondCallback, Application.Current.Dispatcher);
            everySecondTimer.Start();
            fifteenSecondTimer = new DispatcherTimer(new TimeSpan(0, 0, 15), DispatcherPriority.Normal, FifteenSecondCallback, Application.Current.Dispatcher);
            fifteenSecondTimer.Start();
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

        private ObservableCollection<string> _playbackDevices = new ObservableCollection<string>();
        public ObservableCollection<string> PlaybackDevices
        {
            get => _playbackDevices;
            set => SetProperty(ref _playbackDevices, value);
        }

        private StatusEvent? _statusText;
        public StatusEvent? StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private ObservableCollection<StatusEvent> _statusFeed = new ObservableCollection<StatusEvent>();
        public ObservableCollection<StatusEvent> StatusFeed
        {
            get => _statusFeed;
            set => SetProperty(ref _statusFeed, value);
        }

        private bool _isStatusFeedOpen;
        public bool IsStatusFeedOpen
        {
            get => _isStatusFeedOpen;
            set => SetProperty(ref _isStatusFeedOpen, value);
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

        private bool _startWithWindows;
        public bool StartWithWindows
        {
            get 
            {
                RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                _startWithWindows = rk?.GetValue("TwitchSongRequest") != null;
                return _startWithWindows;
            }
            set 
            {
                bool result = ToggleStartWithWindows(value);
                SetProperty(ref _startWithWindows, result);
            }
        }

        public AppSettings AppSettings
        {
            get => _appFilesService.AppSettings;
        }

        public AppSetup AppSetup
        {
            get => _appFilesService.AppSetup;
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

        private ObservableCollection<SongRequest> _songRequestQueue;
        public ObservableCollection<SongRequest> SongRequestQueue
        {
            get => _songRequestQueue;
            set => SetProperty(ref _songRequestQueue, value);
        }

        private ObservableCollection<SongRequest> _songRequestHistory;
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
            set => SetProperty(ref _position, value);
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
        public ICommand SeekPositionCommand => new RelayCommand(SeekPosition);
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
        public ICommand OpenStatusFeedCommand => new RelayCommand(OpenStatusFeed);
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
            _loggerService.LogInfo("Setting playback state to playing");

            bool result = false;

            try
            {
                // songs in queue but no current song so add first song from queue to current song
                if (CurrentSong.Service != null)
                {
                    result = await _currentSong.Service!.Play();
                }
                else
                {
                    await PlayNextSong();
                }
            }
            catch (Exception ex)
            {
                result = false;
                _loggerService.LogError(ex, "Error playing song");
            }

            PlaybackStatus = result ? PlaybackStatus.Playing : PlaybackStatus.Error;
        }

        private async void Pause()
        {
            _loggerService.LogInfo("Setting playback state to paused");

            // nothing to pause since queue is empty and no current song
            if (CurrentSong.Service == null)
            {
                _loggerService.LogWarning("No song to queue to pause");
                PlaybackStatus = PlaybackStatus.Error;
                return;
            }

            bool result;
            try
            {
                result = await _currentSong.Service!.Pause();
            }
            catch (Exception ex)
            {
                result = false;
                _loggerService.LogError(ex, "Error pausing song");
            }
            PlaybackStatus = result ? PlaybackStatus.Paused : PlaybackStatus.Error;
        }

        private async void Skip()
        {
            _loggerService.LogInfo("Skipping current song");
            await PlayNextSong();
        }

        private async void SeekPosition()
        {
            _loggerService.LogInfo($"Changing playback position to: {Position}");

            if (CurrentSong?.Service == null)
            {
                _loggerService.LogWarning("No song to seek position");
                return;
            }

            bool result = false;

            try
            {
                result = await CurrentSong.Service.SetPosition(Position);
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, $"Error changing playback position to: {Position}");
            }
            PlaybackStatus = result ? PlaybackStatus : PlaybackStatus.Error;
        }

        private void ProcessStartUri(string? uri)
        {
            _loggerService.LogInfo($"Opening uri: {uri}");
            try
            {
                if (!string.IsNullOrWhiteSpace(uri))
                {
                    Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, $"Error opening uri: {uri}");
            }
        }

        private void ProcessStartBrowserUrl(Tuple<WebBrowser, string>? browserStartInfo)
        {
            _loggerService.LogInfo($"Opening browser: {browserStartInfo?.Item1} {browserStartInfo?.Item2}");
            WebBrowserLauncher.Launch(browserStartInfo!.Item1, browserStartInfo!.Item2);
        }

        private async void RemoveSongQueue(SongRequest? e)
        {
            if (e != null)
            {
                if (AppSettings.RefundAllPoints)
                {
                    try
                    {
                        await _twitchApiService.CompleteRedeem(e.Requester!, e.RequestInput!, true);
                        _loggerService.LogSuccess($"Refunded points for song: {e?.SongName}");
                    }
                    catch (Exception ex)
                    {
                        _loggerService.LogError(ex, $"Error refunding points.");
                    }
                }
                SongRequestQueue.Remove(e!);
                _loggerService.LogSuccess($"Removed song from queue: {e?.SongName}");
            }
        }

        private void RemoveSongHistory(SongRequest? e)
        {
            if (e != null)
            {
                SongRequestHistory.Remove(e);
                _loggerService.LogSuccess($"Removed song from history: {e?.SongName}");
            }
        }

        private void ReplaySongHistory(SongRequest? e)
        {
            if (e != null)
            {
                SongRequestQueue.Add(e);
                SongRequestHistory.Remove(e);
                _loggerService.LogSuccess($"Added song back to queue from history: {e?.SongName}");
            }
        }

        private async void ClearQueue(bool? refundAll)
        {
            bool result = await ConfirmationDialogViewModel.ShowDialog("Clear Queue", $"Are you sure you want to clear the song queue {(refundAll == true ? "and refund all" : string.Empty)}?");
            if (!result)
            {
                return;
            }

            if (refundAll == true)
            {
                try
                {
                    await _twitchApiService.RefundRedeems(SongRequestQueue.ToList());
                    _loggerService.LogSuccess($"Refunded channel points.");
                }
                catch (Exception ex)
                {
                    _loggerService.LogError(ex, $"Failed to refund channel points.");
                }
            }
            SongRequestQueue.Clear();
            _loggerService.LogSuccess($"Cleared all songs from queue.");
        }

        private async void ClearHistory()
        {
            bool result = await ConfirmationDialogViewModel.ShowDialog("Clear History", "Are you sure you want to clear the song history?");
            if (result)
            {
                SongRequestHistory.Clear();
                _loggerService.LogSuccess($"Cleared all songs from history");
            }
        }

        private CancellationTokenSource? connectCts;
        private async Task<bool> ConnectOrCancel(Action<ServiceOAuthToken> setTokens, Func<CancellationToken, Task<ServiceOAuthToken>> generateTokens, Action<ConnectionStatus> setStatus, string account)
        {
            if (connectCts != null)
            {
                connectCts.Cancel();
                connectCts = null;
                setStatus(ConnectionStatus.Cancelled);
                _loggerService.LogWarning($"Cancelling connection attempt to account: {account}");
                return false;
            }
            _loggerService.LogInfo($"Connecting to account: {account}");
            setStatus(ConnectionStatus.Connecting);
            connectCts = new CancellationTokenSource();

            try
            {
                setTokens(await generateTokens(connectCts.Token));
                setStatus(ConnectionStatus.Authenticated);
                _loggerService.LogSuccess($"Connected to account: {account}");
                return true;
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, $"Unable to connect to {account}");
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
            try
            {
                AppSetup.StreamerInfo.Scope = "chat:read chat:edit channel:read:redemptions channel:manage:redemptions";
                var setTokensAction = new Action<ServiceOAuthToken>(tokens => AppSetup.StreamerAccessTokens = tokens);
                bool result = await ConnectOrCancel(setTokensAction, _twitchAuthService.GenerateStreamerOAuthTokens, status => Connections.StreamerStatus = status, "Twitch Streamer");
                if (result)
                {
                    await ValidateStreamerLogin();
                    await ConnectTwitchClients();
                }
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, "Unable to connect to Twitch Streamer");
            }
        }

        private async void ConnectOrCancelBot()
        {
            try
            {
                AppSetup.BotInfo.Scope = "chat:read chat:edit";
                var setTokensAction = new Action<ServiceOAuthToken>(tokens => AppSetup.BotAccessTokens = tokens);
                bool result = await ConnectOrCancel(setTokensAction, _twitchAuthService.GenerateBotOAuthTokens, status => Connections.BotStatus = status, "Twitch Bot");
                if (result)
                {
                    await ValidateBotLogin();
                }
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, "Unable to connect to Twitch Bot");
            }
        }

        private async void ConnectOrCancelSpotify()
        {
            try
            {
                AppSetup.SpotifyInfo.Scope = "user-modify-playback-state user-read-playback-state user-read-currently-playing";
                var setTokensAction = new Action<ServiceOAuthToken>(tokens => AppSetup.SpotifyAccessTokens = tokens);
                bool result = await ConnectOrCancel(setTokensAction, _spotifyAuthService.GenerateOAuthTokens, status => Connections.SpotifyStatus = status, "Spotify");
                if (result)
                {
                    await ValidateSpotifyLogin();
                }
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, "Unable to connect to Spotify");
            }
        }

        private async Task ConnectTwitchClients()
        {
            try
            {
                _loggerService.LogInfo("Connecting to Twitch clients");

                _twitchApiService.LogEvent -= OnTwitchClientLogEvent;
                _twitchApiService.LogEvent += OnTwitchClientLogEvent;
                _twitchApiService.MessageEvent -= OnTwitchClientMessageReceived;
                _twitchApiService.MessageEvent += OnTwitchClientMessageReceived;

                var streamerClient = await _twitchApiService.GetTwitchStreamerClient();
                var botClient = await _twitchApiService.GetTwitchBotClient();
                _loggerService.LogSuccess("Connected to Twitch clients");
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, "Unable to connect to Twitch clients");
            }
        }

        private async void OnTwitchClientLogEvent(object? sender, OnLogArgs e)
        {
            if (e.Data.Contains("authentication failed"))
            {
                if (sender is TwitchClient client)
                {
                    if (client.TwitchUsername == AppSetup.BotInfo.AccountName!)
                    {
                        await ValidateBotLogin();
                        _twitchApiService.RefreshBotClientCredentials();
                    }
                    if (client.TwitchUsername == AppSetup.StreamerInfo.AccountName!)
                    {
                        await ValidateStreamerLogin();
                        _twitchApiService.RefreshStreamerClientCredentials();
                    }
                }
            }
        }

        private async void OnTwitchClientMessageReceived(ChatMessage chatMessage)
        {
            // check if message is a redeem
            if (string.IsNullOrWhiteSpace(chatMessage.CustomRewardId))
            {
                return;
            }

            // check if redeem is the correct one
            if (chatMessage.CustomRewardId != AppSetup.ChannelRedeemRewardId)
            {
                return;
            }

            _loggerService.LogInfo($"Received redeem request from {chatMessage.DisplayName} for {chatMessage.Message}");

            try
            {
                // try to add song to queue
                string input = chatMessage.Message;
                Tuple<bool, string> songAddResult = await AddSongToQueue(input, chatMessage.DisplayName);

                // dont reply in chat if not enabled in settings
                if (!AppSettings.ReplyInChat || !AppSettings.ReplyToRedeem)
                {
                    return;
                }

                // failed to add song to queue
                if (!songAddResult.Item1)
                {
                    await _twitchApiService.SendChatMessage(chatMessage.Channel, songAddResult.Item2, chatMessage.Id);
                    await _twitchApiService.CompleteRedeem(chatMessage.DisplayName, input, true);
                    _loggerService.LogWarning(songAddResult.Item2);
                    return;
                }

                // song was added succesfully to queue
                await _twitchApiService.SendChatMessage(chatMessage.Channel, songAddResult.Item2, chatMessage.Id);
                _loggerService.LogSuccess(songAddResult.Item2!);
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, $"Error processing redeem request from {chatMessage.DisplayName} for {chatMessage.Message}");
            }
        }

        private async void CreateReward(string? rewardName)
        {
            _loggerService.LogInfo($"Creating reward: {rewardName}");
            _appFilesService.AppSetup.RewardCreationStatus = RewardCreationStatus.Creating;
            OnPropertyChanged(nameof(AppSetup));
            try
            {
                string? rewardId = await _twitchApiService.CreateReward(rewardName!);
                if (!string.IsNullOrWhiteSpace(rewardId))
                {
                    _appFilesService.AppSetup.ChannelRedeemRewardId = rewardId;
                    _appFilesService.AppSetup.RewardCreationStatus = RewardCreationStatus.Created;
                    _loggerService.LogSuccess($"Created reward {rewardName} with id {rewardId}");
                }
                else
                {
                    _appFilesService.AppSetup.RewardCreationStatus = RewardCreationStatus.AlreadyExists;
                    _loggerService.LogWarning($"Reward {rewardName} already exists");
                }
            }
            catch (Exception ex)
            {
                _appFilesService.AppSetup.RewardCreationStatus = RewardCreationStatus.Error;
                _loggerService.LogError(ex, "Unable to create reward");
            }
            OnPropertyChanged(nameof(AppSetup));
        }

        private bool ToggleStartWithWindows(bool e)
        {
            _loggerService.LogInfo($"Setting start with windows to: {e}");

            try
            {
                RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                // add app to startup
                if (e == true)
                {
                    rk?.SetValue(Assembly.GetExecutingAssembly().GetName().Name, Environment.ProcessPath!);
                    _loggerService.LogSuccess("Added app to startup");
                    return true;
                }
                // remove app from startup
                else
                {
                    rk?.DeleteValue(Assembly.GetExecutingAssembly().GetName().Name!, false);
                    _loggerService.LogSuccess("Removed app from startup");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, $"Unable to set start with windows to: {e}");
            }

            return e;
        }

        private void OpenStatusFeed()
        {
            IsStatusFeedOpen = !IsStatusFeedOpen;
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
            ValidateLogins();
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
                _appFilesService.ResetAppSetup();
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, "Unable to reset setup");
            }
            OnPropertyChanged(nameof(AppSetup));
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
                _appFilesService.ResetAppSettings();
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, "Unable to reset settings");
            }
            OnPropertyChanged(nameof(AppSettings));
        }

        private async Task PlayNextSong()
        {
            try
            {
                Position = 0;

                // nothing to play since queue is empty and no current song
                if (SongRequestQueue.Count == 0 && CurrentSong.Service == null)
                {
                    _loggerService.LogWarning("No songs in queue to play");
                    PlaybackStatus = PlaybackStatus.Error;
                    return;
                }

                // add current song to history if it exists
                if (CurrentSong.Service != null)
                {
                    SongRequestHistory.Insert(0, CurrentSong);
                    await _twitchApiService.CompleteRedeem(CurrentSong.Requester!, CurrentSong.RequestInput!, false);
                }

                // change current song to next and remove from queue if queue is not empty
                if (SongRequestQueue.Count > 0)
                {
                    CurrentSong = SongRequestQueue.First();
                    SongRequestQueue.Remove(CurrentSong);
                }
                else
                {
                    CurrentSong = new SongRequest();
                    PlaybackStatus = AppSettings.AutoPlay ? PlaybackStatus.Waiting : PlaybackStatus.Paused;
                }

                // make sure other song services are not playing
                bool ytPaused = await _youtubeSongService.Pause();
                bool spoPaused = await _spotifySongService.Pause();

                // play current song
                if (CurrentSong.Service != null)
                {
                    bool result = await CurrentSong.Service.PlaySong(CurrentSong.Id!);
                    PlaybackStatus = result ? PlaybackStatus.Playing : PlaybackStatus.Error;
                    // reply in chat
                    if (AppSettings.ReplyInChat && AppSettings.MessageOnNextSong)
                    {
                        await _twitchApiService.SendChatMessage(AppSetup.StreamerInfo.AccountName!, $"Now playing: \"{CurrentSong.SongName}\" requested by @{CurrentSong.Requester}");
                    }
                }
                else
                {
                    PlaybackStatus = AppSettings.AutoPlay ? PlaybackStatus.Waiting : PlaybackStatus.Paused;
                }
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, "Error playing next song");
                if (ex.Message.Contains("Unauthorized"))
                {
                    await ValidateSpotifyLogin();
                }
            }
        }

        internal void SaveAppSettings()
        {
            try
            {
                _appFilesService.SaveAppSettings();
                _loggerService.LogInfo("Saved app settings");
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, "Unable to save settings");
            }
        }

        internal void SaveAppSetup()
        {
            try
            {
                _appFilesService.SaveAppSetup();
                _loggerService.LogInfo("Saved app setup");
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, "Unable to save setup");
            }
        }

        internal async Task<Tuple<bool, string>> AddSongToQueue(string input, string requester)
        {
            _loggerService.LogInfo($"Adding song to queue {input} from {requester}");

            bool success = false;
            string message = string.Empty;

            try
            {
                input = input.Trim();

                SongInfo songInfo = new SongInfo();
                string url = string.Empty;
                SongRequestPlatform? platform = null;
                ISongService? songService = null;

                if (input.Contains("youtube.com/", StringComparison.OrdinalIgnoreCase) || input.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
                {
                    _loggerService.LogInfo($"Adding youtube song to queue {input} from {requester}");
                    //https://youtu.be/u54Kf3zxDso
                    //https://www.youtube.com/watch?v=u54Kf3zxDso

                    //TODO: Add youtube song
                    platform = SongRequestPlatform.Youtube;
                    songService = _youtubeSongService;

                    string[] urlSplit = input.Split(new string[] { "?", "&", "be/" }, StringSplitOptions.RemoveEmptyEntries);
                    string videoId = urlSplit[1].Replace("v=", "");

                    songInfo = await _youtubeSongService.GetSongInfo(videoId);
                    url = $"https://www.youtube.com/watch?v={videoId}";
                }
                else if (input.Contains("open.spotify.com", StringComparison.OrdinalIgnoreCase) || input.Contains("spotify.link", StringComparison.OrdinalIgnoreCase))
                {
                    _loggerService.LogInfo($"Adding spotify song to queue {input} from {requester}");

                    //https://open.spotify.com/track/1hEh8Hc9lBAFWUghHBsCel?si=c4cdc1947a184ac0
                    //spotify:track:6RIbDs0p4XusU2PZSiDgeZ

                    //TODO: Add spotify song
                    platform = SongRequestPlatform.Spotify;
                    songService = _spotifySongService;

                    if (input.Contains("spotify.link", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var handler = new HttpClientHandler())
                        {
                            handler.AllowAutoRedirect = false;
                            using (var client = new HttpClient(handler))
                            {
                                HttpResponseMessage response = await client.GetAsync(input);
                                input = response.Headers!.Location!.ToString();
                            }
                        }
                    }

                    string[] urlSplit = input.Split(new string[] { "track/", "track:", "?" }, StringSplitOptions.RemoveEmptyEntries);
                    string trackId = urlSplit[1];

                    songInfo = await _spotifySongService.GetSongInfo(trackId);
                    url = $"https://open.spotify.com/track/{trackId}";
                }
                else if (input.Contains("soundcloud.com", StringComparison.OrdinalIgnoreCase))
                {
                    _loggerService.LogInfo($"Adding soundcloud song to queue {input} from {requester}");

                    //TODO: Add soundcloud song
                    platform = SongRequestPlatform.Soundcloud;
                    //songService = _soundcloudSongService;
                }
                else
                {
                    _loggerService.LogInfo($"Searching for song to add to queue {input} from {requester}");

                    if (input.StartsWith("spotify:", StringComparison.OrdinalIgnoreCase) || input.StartsWith("spo:", StringComparison.OrdinalIgnoreCase))
                    {
                        platform = SongRequestPlatform.Spotify;
                    }
                    else if (input.StartsWith("youtube:", StringComparison.OrdinalIgnoreCase) || input.StartsWith("yt:", StringComparison.OrdinalIgnoreCase))
                    {
                        platform = SongRequestPlatform.Youtube;
                    }
                    else if (input.StartsWith("soundcloud:", StringComparison.OrdinalIgnoreCase) || input.StartsWith("sc:", StringComparison.OrdinalIgnoreCase))
                    {
                        platform = SongRequestPlatform.Soundcloud;
                    }
                    else
                    {
                        platform = AppSettings.SongSearchPlatform;
                    }

                    // remove platform prefix
                    if (input.Contains(":"))
                    {
                        input = input.Split(":")[1];
                    }

                    // search for song on selected platform
                    switch (platform)
                    {
                        case SongRequestPlatform.Spotify:
                            songService = _spotifySongService;
                            songInfo = await songService.SearchSong(input);
                            url = $"https://open.spotify.com/track/{songInfo.SongId}";
                            break;
                        case SongRequestPlatform.Youtube:
                            songService = _youtubeSongService;
                            songInfo = await songService.SearchSong(input);
                            url = $"https://www.youtube.com/watch?v={songInfo.SongId}";
                            break;
                        case SongRequestPlatform.Soundcloud:
                            break;
                        default:
                            break;
                    }
                }

                string songName = songInfo.SongName + (songInfo.Artist != null ? " - " + songInfo.Artist : "");

                // calculate max song duration
                int maxSongDurationSeconds = AppSettings.MaxSongDurationMinutes * 60 + AppSettings.MaxSongDurationSeconds;
                if (songName == null)
                {
                    success = false;
                    message = $"Unable to add \"{input}\" to queue.";
                }
                else if (songInfo.Duration > maxSongDurationSeconds)
                {
                    success = false;
                    message = $"Unable to add \"{input}\" to queue. Duration is too long (max {AppSettings.MaxSongDurationMinutes} minutes)";
                }
                else
                {
                    success = true;
                    message = $"Added \"{songName}\" to queue at position #{SongRequestQueue.Count + 1}.";

                    SongRequest songRequest = new SongRequest
                    {
                        SongName = songName,
                        Duration = songInfo.Duration,
                        Requester = requester,
                        Platform = platform,
                        RequestInput = input,
                        Url = url,
                        Id = songInfo.SongId,
                        Service = songService,
                    };

                    App.Current.Dispatcher.Invoke(delegate
                    {
                        SongRequestQueue.Add(songRequest);
                    });

                    if (platform == SongRequestPlatform.Spotify && AppSettings.SpotifyAddToQueue)
                    {
                        await _spotifySongService.AddSongToQueue(songInfo.SongId!);
                    }
                }
            }
            catch (Exception ex)
            {
                message = $"Unable to add \"{input}\" to queue.";
                _loggerService.LogError(ex, $"Unable to add song to queue {input} from {requester}");

                // refresh tokens if access token expired
                if (ex is HttpRequestException && ex.Data.Values.Cast<object>().Select(v => v.ToString()).Any(x => x!.Contains("access token expired")))
                {
                    _loggerService.LogInfo("Trying to refresh tokens");
                    try
                    {
                        await ValidateSpotifyLogin();
                        return await AddSongToQueue(input, requester);
                    }
                    catch (Exception e)
                    {
                        _loggerService.LogError(e, "Unable to refresh tokens");
                    }
                }
            }

            Tuple<bool, string> result = new Tuple<bool, string>(success, message);
            return result;
        }

        private async void SetupYoutubeService(string playbackDevice, int volume)
        {
            _loggerService.LogInfo("Setting up Youtube Service");
            try
            {
                await _youtubeSongService.SetupService(playbackDevice, volume);
                _loggerService.LogSuccess("Set up Youtube Service");
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, "Unable to setup Youtube Service");
            }
        }

        private async void ValidateLogins()
        {
            _loggerService.LogInfo("Validating logins");
            if (AppSetup.StreamerAccessTokens.AccessToken != null)
            {
                await ValidateStreamerLogin();
                await ConnectTwitchClients();
            }
            if (AppSetup.BotAccessTokens.AccessToken != null)
            {
                await ValidateBotLogin();
            }
            if (AppSetup.SpotifyAccessTokens.AccessToken != null)
            {
                await ValidateSpotifyLogin();
            }
            _isSetupDone = true;
            _loggerService.LogSuccess("Successfully validated logins");
        }

        private async Task ValidateStreamerLogin()
        {
            _loggerService.LogInfo("Validating Twitch Streamer login");
            try
            {
                var statusAction = new Action<ConnectionStatus>(status => Connections.StreamerStatus = status);
                var setTokensAction = new Action<ServiceOAuthToken>(tokens => AppSetup.StreamerAccessTokens = tokens);
                var setNameAction = new Action<string>(name => AppSetup.StreamerInfo.AccountName = name);
                await ValidateLogin(statusAction, _twitchAuthService.ValidateStreamerOAuthTokens, _twitchAuthService.RefreshStreamerOAuthTokens, setTokensAction, setNameAction);
                SaveAppSetup();
                _loggerService.LogSuccess("Successfully validated Twitch Streamer login");
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, $"Unable to validate Twitch Streamer login");
            }
        }

        private async Task ValidateBotLogin()
        {
            _loggerService.LogInfo("Validating Twitch Bot login");
            try
            {
                var statusAction = new Action<ConnectionStatus>(status => Connections.BotStatus = status);
                var setTokensAction = new Action<ServiceOAuthToken>(tokens => AppSetup.BotAccessTokens = tokens);
                var setNameAction = new Action<string>(name => AppSetup.BotInfo.AccountName = name);
                await ValidateLogin(statusAction, _twitchAuthService.ValidateBotOAuthTokens, _twitchAuthService.RefreshBotOAuthTokens, setTokensAction, setNameAction);
                SaveAppSetup();
                _loggerService.LogSuccess("Successfully validated Twitch Bot login");
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, $"Unable to validate Twitch Bot login");
            }
        }

        private async Task ValidateSpotifyLogin()
        {
            _loggerService.LogInfo("Validating Spotify login");
            try
            {
                var statusAction = new Action<ConnectionStatus>(status => Connections.SpotifyStatus = status);
                var setTokensAction = new Action<ServiceOAuthToken>(tokens => AppSetup.SpotifyAccessTokens = tokens);
                var setNameAction = new Action<string>(name => AppSetup.SpotifyInfo.AccountName = name);
                await ValidateLogin(statusAction, _spotifyAuthService.ValidateOAuthTokens, _spotifyAuthService.RefreshOAuthTokens, setTokensAction, setNameAction);
                SaveAppSetup();
                _loggerService.LogSuccess("Successfully validated Spotify login");

                _loggerService.LogInfo("Getting Spotify devices");
                AppSetup.SpotifyDevice = await _spotifySongService.GetComputerDevice();
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, $"Unable to validate Spotify login");
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
                return;
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, $"Unable to validate login");
                setStatus(ConnectionStatus.Refreshing);
            }

            // 2. if not logged in, try to refresh token
            try
            {
                _loggerService.LogInfo($"Refreshing tokens");
                setTokens(await refreshTokens());
                setStatus(ConnectionStatus.Connected);
            }
            catch
            {
                _loggerService.LogError($"Unable to refresh tokens");
                setStatus(ConnectionStatus.Error);
                throw;
            }

            // 3. revalidate token
            try
            {
                _loggerService.LogInfo($"Revalidating login");
                setAccountName(await validateTokens());
                setStatus(ConnectionStatus.Connected);
            }
            catch
            {
                setStatus(ConnectionStatus.Error);
                throw;
            }
        }

        private async void EverySecondCallback(object? sender, EventArgs e)
        {
            // dont do anything if setup is not done
            if (!_isSetupDone)
            {
                return;
            }

            int curTime = Position;
            curTime++;

            // check if current song is finished or if there is no current song
            if (curTime > CurrentSong.Duration || CurrentSong.Service == null)
            {
                // play next song if autoplay is enabled and song queue is not empty
                if (AppSettings.AutoPlay && SongRequestQueue.Count > 0)
                {
                    await PlayNextSong();
                }
                else
                {
                    PlaybackStatus = AppSettings.AutoPlay ? PlaybackStatus.Waiting : PlaybackStatus.Paused;
                    // finished playing current song, move to history
                    if (CurrentSong.Service != null)
                    {
                        if (AppSettings.AutoPlay && Connections.SpotifyStatus == ConnectionStatus.Connected)
                        {
                            await _spotifySongService.Play();
                        }
                        await _twitchApiService.CompleteRedeem(CurrentSong.Requester!, CurrentSong.RequestInput!, false);
                        SongRequestHistory.Insert(0, CurrentSong);
                        CurrentSong = new SongRequest();
                        Position = 0;
                    }
                }
                return;
            }

            // if not playing, dont update position
            if (PlaybackStatus != PlaybackStatus.Playing)
            {
                return;
            }

            SetProperty(ref _position, curTime, nameof(Position));
        }

        private async void FifteenSecondCallback(object? sender, EventArgs e)
        {
            // dont do anything if setup is not done
            if (!_isSetupDone)
            {
                return;
            }

            // every 15 seconds, check if the song is still playing
            if (CurrentSong.Service != null && PlaybackStatus == PlaybackStatus.Playing)
            {
                int curTime = Position;
                if (CurrentSong.Service is SpotifySongService)
                {
                    SpotifyState? state = await _spotifySongService.GetSpotifyState();
                    if (state?.item?.id == CurrentSong.Id)
                    {
                        curTime = state?.progress_ms / 1000 ?? Position;
                        PlaybackStatus = state?.is_playing == true ? PlaybackStatus.Playing : AppSettings.AutoPlay ? PlaybackStatus.Waiting : PlaybackStatus.Paused;
                    }
                }
                else
                {
                    curTime = await CurrentSong.Service.GetPosition();
                }
                SetProperty(ref _position, curTime, nameof(Position));
            }
        }

        private void ReadLogsToStatusFeed()
        {
            try
            {
                List<string> logs = _appFilesService.GetAppLogs().ToList();
                foreach (var log in logs)
                {
                    if (!string.IsNullOrWhiteSpace(log) && !log.Trim().StartsWith("at") && !log.Trim().StartsWith("--- End"))
                    {
                        var split = log.Split(' ', 4);
                        var date = DateTime.Parse(split[0] + " " + split[1]);
                        var level = split[2];
                        var message = split[3];
                        StatusFeed.Insert(StatusFeed.Count, new StatusEvent(date, level, message, true));
                    }
                }
                StatusFeed.Insert(StatusFeed.Count, new StatusEvent(DateTime.Now, "Info", $"Start of current session {new string('-', 100)}", true));
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, "Error reading old logs to status feed");
            }
        }

        private ISongService? GetSongService(SongRequestPlatform? platform)
        {
            switch (platform)
            {
                case SongRequestPlatform.Youtube:
                    return _youtubeSongService;
                case SongRequestPlatform.Spotify:
                    return _spotifySongService;
                case SongRequestPlatform.Soundcloud:
                    //return _soundcloudSongService;
                default:
                    return null;
            }
        }

        private IEnumerable<SongRequest> GetSavedSongRequestQueue()
        {
            List<SongRequest> songRequests = new List<SongRequest>();
            try
            {
                _loggerService.LogInfo("Getting saved song request queue");
                songRequests = _appFilesService.GetSongQueue();
                songRequests.ForEach(songRequests => songRequests.Service = GetSongService(songRequests.Platform));
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, "Unable to get saved song request queue");

            }
            return songRequests;
        }

        private IEnumerable<SongRequest> GetSavedSongRequestHistory()
        {
            List<SongRequest> songRequests = new List<SongRequest>();
            try
            {
                _loggerService.LogInfo("Getting saved song request history");
                songRequests = _appFilesService.GetSongHistory();
                songRequests.ForEach(songRequests => songRequests.Service = GetSongService(songRequests.Platform));
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, "Unable to get saved song request history");
            }
            return songRequests;
        }

        private List<string> GetPlaybackDevices()
        {
            var playbackDevices = new List<string>();

            try
            {
                _loggerService.LogInfo("Getting playback devices");

                using var devices = new MMDeviceEnumerator();
                foreach (var device in devices.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                {
                    playbackDevices.Add(device.FriendlyName);
                }
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, "Unable to get playback devices");
            }

            return playbackDevices;
        }

        private string GetDefaultPlaybackDevice()
        {
            try
            {
                _loggerService.LogInfo("Getting default playback device");

                using (var devices = new MMDeviceEnumerator())
                {
                    return devices.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).FriendlyName;
                }
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, "Unable to get default playback device");
            }

            return string.Empty;
        }

        public void SaveSongQueue()
        {
            try
            {
                _appFilesService.SaveSongQueue(SongRequestQueue.ToList());
                _loggerService.LogSuccess("Successfully saved song queue");
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, "Unable to save song queue");
            }
        }

        public void SaveSongHistory()
        {
            try
            {
                _appFilesService.SaveSongHistory(SongRequestHistory.ToList());
                _loggerService.LogSuccess("Successfully saved song history");
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, "Unable to save song history");
            }
        }
    }
}
