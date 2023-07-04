using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using NAudio.CoreAudioApi;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TwitchSongRequest.Model;
using TwitchSongRequest.Services;

namespace TwitchSongRequest.ViewModel
{
    internal class MainViewViewModel : ObservableObject
    {
        private readonly IAppSettingsService appSettingsService;
        private readonly ISpotifyService spotifyService;
        private readonly ITwitchAuthenticationService twitchAuthService;

        public MainViewViewModel()
        {
            appSettingsService = App.Current.Services.GetService<IAppSettingsService>()!;
            spotifyService = App.Current.Services.GetService<ISpotifyService>()!;
            twitchAuthService = App.Current.Services.GetService<ITwitchAuthenticationService>()!;

            _appSettings = appSettingsService.GetAppSettings();
            _playbackDevice = _appSettings.PlaybackDevice ?? GetDefaultPlaybackDevice();
            _volume = _appSettings.Volume ?? 100;

            _chromeBrowserViewModel = new ChromeBrowserViewModel(_playbackDevice, _volume);
        }

        private ChromeBrowserViewModel _chromeBrowserViewModel;
        public ChromeBrowserViewModel ChromeBrowserViewModel
        {
            get => _chromeBrowserViewModel;
            set => SetProperty(ref _chromeBrowserViewModel, value);
        }

        private string _playbackDevice;
        public string PlaybackDevice
        {
            get => _playbackDevice;
            set
            {
                SetProperty(ref _playbackDevice, value);
                _chromeBrowserViewModel.ChangePlaybackDevice(value);
                _appSettings.PlaybackDevice = value;
            }
        }

        private ObservableCollection<string> _playbackDevices = new ObservableCollection<string>(GetPlaybackDevices());
        public ObservableCollection<string> PlaybackDevices
        {
            get => _playbackDevices;
            set => SetProperty(ref _playbackDevices, value);
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

        private int _volume;
        public int Volume
        {
            get => _volume;
            set
            {
                SetProperty(ref _volume, value);
                _chromeBrowserViewModel.SetVideoVolume(value);
                _appSettings.Volume = value;
            }
        }

        private int _position;
        public int Position
        {
            get => _position;
            set
            {
                SetProperty(ref _position, value);
                _chromeBrowserViewModel.SetVideoPosition(value);
            }
        }

        private ConnectionsStatus _connections = new ConnectionsStatus();
        public ConnectionsStatus Connections
        {
            get => _connections;
            set => SetProperty(ref _connections, value);
        }

        public ICommand PlayCommand => new RelayCommand(Play);
        public ICommand PauseCommand => new RelayCommand(Pause);
        public ICommand SkipCommand => new RelayCommand(Skip);
        public ICommand ClearQueueCommand => new RelayCommand<bool?>((e) => ClearQueue(e));
        public ICommand ClearHistoryCommand => new RelayCommand(ClearHistory);
        public ICommand OpenSetupCommand => new RelayCommand(OpenSetup);
        public ICommand ConnectStreamerCommand => new RelayCommand(ConnectStreamer);
        public ICommand ConnectBotCommand => new RelayCommand(ConnectBot);
        public ICommand CreateRewardCommand => new RelayCommand(CreateReward);
        public ICommand ConnectSpotifyCommand => new RelayCommand(ConnectSpotify);
        public ICommand SaveSetupCommand => new RelayCommand(() => CloseSetup(true));
        public ICommand CloseSetupCommand => new RelayCommand(() => CloseSetup(false));

        private async void Play()
        {
            bool result = false;
            if (_currentSong.Platform == SongRequestPlatform.YOUTUBE)
            {
                result = await _chromeBrowserViewModel.PlayVideo();
            }
            else
            {
                result = await spotifyService.Play();
            }
            PlaybackStatus = result ? PlaybackStatus.PLAYING : PlaybackStatus.ERROR;
        }

        private void Pause()
        {
            if (_currentSong.Platform == SongRequestPlatform.YOUTUBE)
            {
                _chromeBrowserViewModel.PauseVideo();
            }
            else
            {
                spotifyService.Pause();
            }
            PlaybackStatus = PlaybackStatus.PAUSED;
        }

        private async void Skip()
        {
            //TODO: Skip song
            string url = "https://www.youtube.com/watch?v=41VlNOyPD9U?t=10s&autoplay=1";
            string[] urlSplit = url.Split('?', '&');
            string embedUrl = (urlSplit[0] + "?" + urlSplit[1]).Replace("watch?v=", "embed/") + "?autoplay=1";
            var info = await _chromeBrowserViewModel.GetYoutubeVideoInfo(embedUrl);
            _chromeBrowserViewModel.ChangeAddress(embedUrl);
        }

        private void ClearQueue(bool? refundAll)
        {
            //TODO: Clear queue
        }

        private void ClearHistory()
        {
            //TODO: Clear history
        }

        private void OpenSetup()
        {
            IsSetupOpen = true;
        }

        private void ConnectStreamer()
        {
            //TODO: Connect to Twitch streamer account
        }

        private void ConnectBot()
        {
            //TODO: Connect to Twitch bot account
        }

        private void CreateReward()
        {
            //TODO: Create Twitch reward
        }

        private void ConnectSpotify()
        {
            //TODO: Connect to Spotify
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
