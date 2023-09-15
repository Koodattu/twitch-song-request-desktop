﻿using CommunityToolkit.Mvvm.ComponentModel;
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

        private readonly DispatcherTimer dispatcherTimer;

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

            PlaybackDevices = new ObservableCollection<string>(GetPlaybackDevices());
            SongRequestQueue = new ObservableCollection<SongRequest>(GetSavedSongRequestQueue());
            SongRequestHistory = new ObservableCollection<SongRequest>(GetSavedSongRequestHistory());

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

        private ObservableCollection<string> _playbackDevices = new ObservableCollection<string>();
        public ObservableCollection<string> PlaybackDevices
        {
            get => _playbackDevices;
            set => SetProperty(ref _playbackDevices, value);
        }

        private StatusEvent _statusText;
        public StatusEvent StatusText
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
            set
            {
                //CurrentSong.Service?.SetPosition(value);
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
        public ICommand PositionChangedCommand => new RelayCommand<int>((e) => PositionChanged(e));
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

            // nothing to play since queue is empty and no current song
            if (SongRequestQueue.Count == 0 && CurrentSong.Service == null)
            {
                _loggerService.LogWarning("No songs in queue to play");
                PlaybackStatus = PlaybackStatus.Error;
                return;
            }

            bool result;

            try
            {
                // songs in queue but no current song so add first song from queue to current song
                if (SongRequestQueue.Count > 0 && CurrentSong.Service == null)
                {
                    _loggerService.LogInfo("Adding first song from queue to playback");
                    CurrentSong = SongRequestQueue.First();
                    SongRequestQueue.Remove(CurrentSong);
                    result = await CurrentSong.Service!.PlaySong(CurrentSong.Id!);
                }
                else
                {
                    result = await _currentSong.Service!.Play();
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

            bool result;

            try
            {
                if (SongRequestQueue.Count > 0)
                {
                    CurrentSong = SongRequestQueue.First();
                    SongRequestQueue.Remove(CurrentSong);
                    result = await CurrentSong.Service!.PlaySong(CurrentSong.Id!);
                }
                else
                {
                    CurrentSong = new SongRequest();
                    PlaybackStatus = PlaybackStatus.Paused;
                    result = true;
                }
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, "Error skipping song");
                result = false;
            }

            PlaybackStatus = result ? PlaybackStatus : PlaybackStatus.Error;
        }

        private void PositionChanged(int position)
        {
            _loggerService.LogInfo($"Changing playback position to: {position}");
            Position = position;
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
                        await _twitchApiService.RefundRedeem(e.Requester!, e.RequestInput!);
                        _loggerService.LogSuccess($"Refunded points for song: {e?.SongName}");
                    }
                    catch (Exception ex)
                    {
                        _loggerService.LogError(ex, $"Error refunding points.");
                    }
                }
                SongRequestQueue.Remove(e);
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
                var streamerClient = await _twitchApiService.GetTwitchStreamerClient();
                _twitchApiService.MessageEvent += OnTwitchClientMessageReceived;
                var botClient = await _twitchApiService.GetTwitchBotClient();
                _loggerService.LogSuccess("Connected to Twitch clients");
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, "Unable to connect to Twitch clients");
            }
        }

        private async void OnTwitchClientMessageReceived(ChatMessage chatMessage)
        {
            if (string.IsNullOrWhiteSpace(chatMessage.CustomRewardId))
            {
                return;
            }
            if (chatMessage.CustomRewardId != AppSetup.ChannelRedeemRewardId)
            {
                return;
            }

            _loggerService.LogInfo($"Received redeem request from {chatMessage.Username} for {chatMessage.Message}");

            try
            {
                string input = chatMessage.Message;
                Tuple<bool, string> songAddResult = await AddSongToQueue(input, chatMessage.Username);

                if (!AppSettings.ReplyInChat)
                {
                    return;
                }

                // check if song was added successfully
                if (songAddResult.Item1)
                {
                    await _twitchApiService.ReplyToChatMessage(chatMessage.Channel, chatMessage.Id, songAddResult.Item2);
                    _loggerService.LogSuccess(songAddResult.Item2!);
                }
                else
                {
                    await _twitchApiService.ReplyToChatMessage(chatMessage.Channel, chatMessage.Id, songAddResult.Item2);
                    await _twitchApiService.RefundRedeem(chatMessage.Username, input);
                    _loggerService.LogWarning(songAddResult.Item2);
                }
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, $"Error processing redeem request from {chatMessage.Username} for {chatMessage.Message}");
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
            _loggerService.LogInfo($"Adding song to queue {input} {requester}");

            bool success = false;
            string message = string.Empty;

            try
            {
                input = input.Trim();

                string? songName = string.Empty;
                int duration = 0;
                string? url = string.Empty;
                string? id = string.Empty;
                SongRequestPlatform? platform = null;
                ISongService? songService = null;

                if (input.Contains("youtube.com/", StringComparison.OrdinalIgnoreCase) || input.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
                {
                    _loggerService.LogInfo($"Adding youtube song to queue {input} {requester}");
                    //https://youtu.be/u54Kf3zxDso
                    //https://www.youtube.com/watch?v=u54Kf3zxDso

                    //TODO: Add youtube song
                    platform = SongRequestPlatform.Youtube;
                    songService = _youtubeSongService;

                    string[] urlSplit = input.Split(new string[] { "?", "&", "be/" }, StringSplitOptions.RemoveEmptyEntries);
                    string videoId = urlSplit[1].Replace("v=", "");

                    SongInfo? info = await _youtubeSongService.GetSongInfo(videoId);
                    songName = info.SongName;
                    duration = info.Duration;
                    url = $"https://www.youtube.com/watch?v={videoId}";
                    id = videoId;
                }
                else if (input.Contains("open.spotify.com", StringComparison.OrdinalIgnoreCase))
                {
                    _loggerService.LogInfo($"Adding spotify song to queue {input} {requester}");

                    //https://open.spotify.com/track/1hEh8Hc9lBAFWUghHBsCel?si=c4cdc1947a184ac0
                    //spotify:track:6RIbDs0p4XusU2PZSiDgeZ

                    //TODO: Add spotify song
                    platform = SongRequestPlatform.Spotify;
                    songService = _spotifySongService;

                    string[] urlSplit = input.Split(new string[] { "track/", "track:", "?" }, StringSplitOptions.RemoveEmptyEntries);
                    string trackId = urlSplit[1];

                    SongInfo? info = await _spotifySongService.GetSongInfo(trackId);
                    songName = $"{info.SongName} - {info.Artist}";
                    duration = info.Duration;
                    url = $"https://open.spotify.com/track/{trackId}";
                    id = trackId;
                }
                else if (input.Contains("soundcloud.com", StringComparison.OrdinalIgnoreCase))
                {
                    _loggerService.LogInfo($"Adding soundcloud song to queue {input} {requester}");

                    //TODO: Add soundcloud song
                    platform = SongRequestPlatform.Soundcloud;
                    //songService = _soundcloudSongService;
                }
                else
                {
                    _loggerService.LogInfo($"Searching for song to add to queue {input} {requester}");
                    songName = null;
                    //TODO: Search for song using selected platform
                    platform = AppSettings.SongSearchPlatform;
                }

                int maxSongDurationSeconds = AppSettings.MaxSongDurationMinutes * 60 + AppSettings.MaxSongDurationSeconds;
                if (songName == null)
                {
                    success = false;
                    message = $"Unable to add song \"{input}\" to queue.";
                }
                else if (duration > maxSongDurationSeconds)
                {
                    success = false;
                    message = $"Unable to add song \"{input}\" to queue. Song duration is too long (max {AppSettings.MaxSongDurationMinutes}:{AppSettings.MaxSongDurationSeconds})";
                }
                else
                {
                    success = true;
                    message = $"Added \"{songName}\" to queue.";

                    SongRequest songRequest = new SongRequest
                    {
                        SongName = songName,
                        Duration = duration,
                        Requester = requester,
                        Platform = platform,
                        RequestInput = input,
                        Url = url,
                        Id = id,
                        Service = songService,
                    };

                    App.Current.Dispatcher.Invoke(delegate
                    {
                        SongRequestQueue.Add(songRequest);
                    });
                }
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, $"Unable to add song to queue {input} {requester}");
            }

            Tuple<bool, string> result = new Tuple<bool, string>(success, message);
            return result;
        }

        private async void SetupYoutubeService(string playbackDevice, int volume)
        {
            _loggerService.LogInfo("Setting up Youtube Service");
            try
            {
                await _youtubeSongService.SetPlaybackDevice(playbackDevice);
                await _youtubeSongService.SetVolume(volume);
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
                _loggerService.LogSuccess("Successfully validated Spotify login");
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

        private async void TimerCallback(object? sender, EventArgs e)
        {
            if (PlaybackStatus != PlaybackStatus.Playing)
            {
                return;
            }

            int curTime = Position++;

            // every 15 seconds, check if the song is still playing
            if (curTime % 15 == 0)
            {
                curTime = await CurrentSong.Service!.GetPosition();
            }

            if (curTime == -1)
            {
                return;
            }

            SetProperty(ref _position, curTime, nameof(Position));
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
