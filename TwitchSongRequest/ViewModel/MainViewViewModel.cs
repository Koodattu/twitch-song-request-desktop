using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.CoreAudioApi;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace TwitchSongRequest.ViewModel
{
    internal class MainViewViewModel : ObservableObject
    {
        public MainViewViewModel()
        {
            _chromeBrowserViewModel = new();
            _setupViewModel = new();
            OpenSetupCommand = new RelayCommand(OpenSetup);
            GetPlaybackDevices();
        }

        private ChromeBrowserViewModel _chromeBrowserViewModel;
        public ChromeBrowserViewModel ChromeBrowserViewModel
        {
            get => _chromeBrowserViewModel;
        }

        private SetupViewModel _setupViewModel;
        public SetupViewModel SetupViewModel
        {
            get => _setupViewModel;
        }

        private bool _isSetupOpen;
        public bool IsSetupOpen
        {
            get => _isSetupOpen;
            set
            {
                SetProperty(ref _isSetupOpen, value);
            }
        }

        private string _playbackDevice;
        public string PlaybackDevice
        {
            get => _playbackDevice;
            set
            {
                SetProperty(ref _playbackDevice, value);
                ChromeBrowserViewModel.ChangePlaybackDevice(value);
            }
        }

        private ObservableCollection<string> _playbackDevices = new();
        public ObservableCollection<string> PlaybackDevices
        {
            get => _playbackDevices;
            set => SetProperty(ref _playbackDevices, value);
        }

        public ICommand OpenSetupCommand { get; }
        private void OpenSetup() => IsSetupOpen = true;

        private void GetPlaybackDevices()
        {
            using (var devices = new MMDeviceEnumerator())
            {
                foreach (var device in devices.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                {
                    PlaybackDevices.Add(device.FriendlyName);
                }

                PlaybackDevice = devices.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).FriendlyName;
            }
        }
    }
}
