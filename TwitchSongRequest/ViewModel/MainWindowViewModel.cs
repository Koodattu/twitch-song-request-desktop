using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;
using TwitchSongRequest.Model;
using TwitchSongRequest.Services.App;

namespace TwitchSongRequest.ViewModel
{
    internal class MainWindowViewModel : ObservableObject
    {
        private readonly IAppFilesService? _appFilesService;

        public MainWindowViewModel()
        {
            _appFilesService = App.Current.Services.GetService<IAppFilesService>();
            if (_appFilesService != null)
            {
                MainWindowState = _appFilesService.AppSettings.StartMinimized ? WindowState.Minimized : WindowState.Normal;
            }
        }

        private string _title = "Twitch Music Song Request Bot";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public SongRequest? CurrentSongRequest
        {
            get
            {
                MainViewViewModel? viewModel = App.Current.Services.GetService<MainViewViewModel>();
                return viewModel?.CurrentSong;
            }
        }

        public PlaybackStatus? PlaybackStatus
        {
            get
            {
                MainViewViewModel? viewModel = App.Current.Services.GetService<MainViewViewModel>();
                return viewModel?.PlaybackStatus;
            }
        }

        public string? AutoPlayStatus
        {
            get
            {
                return _appFilesService!.AppSettings.AutoPlay ? "AutoPlay: On" : "AutoPlay: Off";
            }
        }

        public bool? MinimizeToSystemTray => _appFilesService?.AppSettings.MinimizeToSystemTray;

        private WindowState _mainWindowState;
        public WindowState MainWindowState
        {
            get => _mainWindowState;
            set => SetProperty(ref _mainWindowState, value);
        }

        public ICommand WindowClosingCommand => new RelayCommand(WindowClosing);

        private void WindowClosing()
        {
            MainViewViewModel? viewModel = App.Current.Services.GetService<MainViewViewModel>();
            viewModel?.SaveAppSettings();
            viewModel?.SaveAppSetup();
            viewModel?.SaveSongQueue();
            viewModel?.SaveSongHistory();
        }
    }
}
