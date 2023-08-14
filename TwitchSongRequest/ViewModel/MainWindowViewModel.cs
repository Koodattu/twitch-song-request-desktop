using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;

namespace TwitchSongRequest.ViewModel
{
    internal class MainWindowViewModel : ObservableObject
    {
        public MainWindowViewModel()
        {
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
            MainViewViewModel? viewModel = App.Current.Services.GetService<MainViewViewModel>();
            viewModel?.SaveAppSettings();
            viewModel?.SaveAppSetup();
            viewModel?.SaveSongQueue();
            viewModel?.SaveSongHistory();
        }
    }
}
