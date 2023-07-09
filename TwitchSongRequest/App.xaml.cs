﻿using CefSharp.OffScreen;
using CefSharp;
using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TwitchSongRequest.Services;

namespace TwitchSongRequest
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal static YoutubeSongService YoutubeSongService = new();
        internal static SpotifySongService SpotifySongService = new();

        internal static TwitchAuthService TwitchAuthService = new();
        internal static SpotifyAuthService SpotifyAuthService = new();

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

            return services.BuildServiceProvider();
        }
    }
}
