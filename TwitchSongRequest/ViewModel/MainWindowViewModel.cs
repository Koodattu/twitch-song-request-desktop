using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;
using TwitchSongRequest.Services.App;

namespace TwitchSongRequest.ViewModel
{
    internal class MainWindowViewModel : ObservableObject
    {
        private readonly IAppFilesService appSettingsService;

        public MainWindowViewModel(IAppFilesService appSettingsService)
        {
            this.appSettingsService = appSettingsService;
            _title = "Twitch Music Song Request Bot";
        }

        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public ICommand WindowClosingCommand => new RelayCommand(WindowClosing);

        private void WindowClosing()
        {
            appSettingsService.SaveAppSettings();
            appSettingsService.SaveAppSetup();
            MainViewViewModel? viewModel = App.Current.Services.GetService<MainViewViewModel>();
            viewModel?.SaveSongQueue();
            viewModel?.SaveSongHistory();
        }
    }
}
