using Microsoft.Win32;
using System;
using System.Diagnostics;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Helpers
{
    internal static class WebBrowserLauncher
    {
        public static void Launch(WebBrowser browser, string url)
        {
            string browserKeyPath;
            switch (browser)
            {
                case WebBrowser.CHROME:
                    browserKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe";
                    break;
                case WebBrowser.FIREFOX:
                    browserKeyPath = @"SOFTWARE\Mozilla\Mozilla Firefox";
                    break;
                case WebBrowser.EDGE:
                    browserKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe";
                    break;
                default:
                    throw new NotSupportedException($"Unsupported browser: {browser}");
            }

            string? browserPath = GetBrowserPathFromRegistry(browserKeyPath);
            if (browserPath == null)
            {
                throw new Exception($"{browser} is not installed.");
            }

            Process.Start(new ProcessStartInfo(browserPath, url));
        }

        private static string? GetBrowserPathFromRegistry(string browserKeyPath)
        {
            using (RegistryKey? browserKey = Registry.LocalMachine.OpenSubKey(browserKeyPath))
            {
                if (browserKey == null)
                {
                    return null;
                }

                // For Firefox, we need to handle a little differently
                if (browserKeyPath.Contains("Mozilla Firefox"))
                {
                    string currentVersion = (string)browserKey.GetValue("CurrentVersion")!;
                    using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(browserKeyPath + "\\" + currentVersion + "\\Main"))
                    {
                        if (key == null)
                        {
                            return null;
                        }

                        return (string)key.GetValue("PathToExe")!;
                    }
                }
                else
                {
                    return (string)browserKey.GetValue(null)!;
                }
            }
        }
    }
}
