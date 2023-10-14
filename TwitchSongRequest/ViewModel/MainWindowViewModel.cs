using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;
using TwitchSongRequest.Services.App;

namespace TwitchSongRequest.ViewModel
{
    internal class MainWindowViewModel : ObservableObject
    {
        public MainWindowViewModel()
        {
            IAppFilesService? appFilesService = App.Current.Services.GetService<IAppFilesService>();
            if (appFilesService != null)
            {
                MainWindowState = appFilesService.AppSettings.StartMinimized ? WindowState.Minimized : WindowState.Normal;
            }
        }

        private string _title = "Twitch Music Song Request Bot";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

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
