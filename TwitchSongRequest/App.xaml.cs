using CefSharp.OffScreen;
using CefSharp;
using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TwitchSongRequest.Services;
using TwitchSongRequest.Services.Authentication;
using TwitchSongRequest.Services.Api;
using TwitchSongRequest.ViewModel;

namespace TwitchSongRequest
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal IServiceProvider Services { get; }
        internal new static App Current => (App)Application.Current;

        public App()
        {
            var settings = new CefSettings();
            settings.CefCommandLineArgs["autoplay-policy"] = "no-user-gesture-required";
            settings.CefCommandLineArgs.Remove("mute-audio");
            Cef.Initialize(settings, true, browserProcessHandler: null);
            Services = ConfigureServices();
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Services
            services.AddSingleton<IAppSettingsService, AppSettingsService>();

            services.AddSingleton<ITwitchAuthService, TwitchAuthService>();
            services.AddSingleton<ISpotifyAuthService, SpotifyAuthService>();

            services.AddSingleton<ITwitchApiService, TwitchApiService>();

            services.AddSingleton<ISpotifySongService, SpotifySongService>();
            services.AddSingleton<IYoutubeSongService, YoutubeSongService>();

            // Viewmodels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<MainViewViewModel>();

            return services.BuildServiceProvider();
        }
    }
}
