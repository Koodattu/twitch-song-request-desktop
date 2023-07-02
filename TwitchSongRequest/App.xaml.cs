using CefSharp.Wpf;
using CefSharp;
using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TwitchSongRequest.Services;
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
            Services = ConfigureServices();
            //this.InitializeComponent();
            var settings = new CefSettings();
            settings.CefCommandLineArgs["autoplay-policy"] = "no-user-gesture-required";
            Cef.Initialize(settings, true, browserProcessHandler: null);
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Services
            services.AddSingleton<ITwitchAuthenticationService, TwitchAuthenticationService>();

            // ViewModels
            services.AddTransient<MainWindowViewModel>();

            return services.BuildServiceProvider();
        }
    }
}
