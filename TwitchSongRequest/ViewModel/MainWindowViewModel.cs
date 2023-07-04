using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace TwitchSongRequest.ViewModel
{
    internal class MainWindowViewModel : ObservableObject
    {
        public MainWindowViewModel()
        {
            WindowClosingCommand = new RelayCommand(WindowClosing);
        }

        private string _title = "Twitch Music Song Request Bot";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private MainViewViewModel _mainViewViewModel = new MainViewViewModel();
        public MainViewViewModel MainViewViewModel
        {
            get => _mainViewViewModel;
            set => SetProperty(ref _mainViewViewModel, value);
        }

        public ICommand WindowClosingCommand { get; }

        private void WindowClosing()
        {
            MainViewViewModel.SaveAppSettings();
        }
    }
}
