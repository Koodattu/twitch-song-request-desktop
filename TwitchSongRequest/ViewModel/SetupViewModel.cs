using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwitchSongRequest.Model;
using TwitchSongRequest.Services;

namespace TwitchSongRequest.ViewModel
{
    internal class SetupViewModel : ObservableObject
    {
        private readonly IAppSettingsService _appSettingsService;

        public SetupViewModel(IAppSettingsService appSettingsService)
        {
            _appSettingsService = appSettingsService;
            _appSettings = _appSettingsService.GetAppSettings();
            SaveSetupCommand = new RelayCommand(() => CloseSetup(true));
            CloseSetupCommand = new RelayCommand(() => CloseSetup(false));
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

        private AppSettings _appSettings;
        public AppSettings AppSettings
        {
            get => _appSettings;
            set
            {
                SetProperty(ref _appSettings, value);
            }
        }

        public RelayCommand SaveSetupCommand { get; private set; }
        public RelayCommand CloseSetupCommand { get; }
        private void CloseSetup(bool save)
        {
            if (save)
            {
                _appSettingsService.SaveAppSettings(AppSettings);
            }
            IsSetupOpen = false;
        }

        internal void OpenSetupView()
        {
            IsSetupOpen = true;
        }
    }
}
