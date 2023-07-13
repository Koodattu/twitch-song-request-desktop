using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using TwitchSongRequest.Services;

namespace TwitchSongRequest.ViewModel
{
    internal class MainWindowViewModel : ObservableObject
    {
        private readonly IAppSettingsService appSettingsService;

        public MainWindowViewModel(IAppSettingsService appSettingsService)
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
        }
    }
}
