using CefSharp.Wpf;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TwitchSongRequest.ViewModel
{
    internal class MainViewViewModel : ObservableObject
    {
        public MainViewViewModel()
        {

        }

        private ChromiumWebBrowser _chromeBrowser = new();
        public ChromiumWebBrowser ChromeBrowser
        {
            get => _chromeBrowser;
            set => SetProperty(ref _chromeBrowser, value);
        }

    }
}
