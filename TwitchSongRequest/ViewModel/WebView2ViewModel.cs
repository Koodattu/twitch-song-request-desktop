using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Web.WebView2.Wpf;

namespace TwitchSongRequest.ViewModel
{
    internal class WebView2ViewModel : ObservableObject
    {
        private MainViewViewModel _mainViewViewModel;

        public WebView2ViewModel(MainViewViewModel mainViewViewModel)
        {
            _mainViewViewModel = mainViewViewModel;
            _webView2Browser = new WebView2();
        }

        private WebView2 _webView2Browser;
        public WebView2 WebView2Browser
        {
            get => _webView2Browser;
            set => SetProperty(ref _webView2Browser, value);
        }
    }
}