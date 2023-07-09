using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TwitchSongRequest.ViewModel
{
    internal class ConfirmationDialogViewModel : ObservableObject
    {
        private TaskCompletionSource<bool> tcs;

        public ConfirmationDialogViewModel()
        {

        }

        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }
        private bool _isShown = false;
        public bool IsShown
        {
            get => _isShown;
            set => SetProperty(ref _isShown, value);
        }

        public ICommand YesCommand => new RelayCommand(Yes);
        public ICommand NoCommand => new RelayCommand(No);

        private void Yes()
        {
            IsShown = false;
            tcs.SetResult(true);
        }

        private void No()
        {
            IsShown = false;
            tcs.SetResult(false);
        }

        public Task<bool> ShowDialog(string title, string description)
        {
            Title = title;
            Description = description;
            IsShown = true;
            tcs = new TaskCompletionSource<bool>();
            return tcs.Task;
        }
    }
}
