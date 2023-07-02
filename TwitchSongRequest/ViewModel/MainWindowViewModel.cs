using CommunityToolkit.Mvvm.ComponentModel;

namespace TwitchSongRequest.ViewModel
{
    internal class MainWindowViewModel : ObservableObject
    {
        public MainWindowViewModel()
        {
            
        }

        private string _title = "Twitch Music Song Request Bot";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
    }
}
