using AdonisUI.Controls;
using CefSharp;
using NAudio.CoreAudioApi;
using System.Windows;
using System.Windows.Controls;
using System.Collections;
using TwitchSongRequest.Model;
using System.Threading.Tasks;
using TwitchSongRequest.Services;

namespace TwitchSongRequest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : AdonisWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            CreateMockQueue();
            GetPlaybackDevices();
            ChromeBrowser.LoadingStateChanged += ChromeBrowser_LoadingStateChanged;
            ChromeBrowser.ConsoleMessage += ChromeBrowser_ConsoleMessage;
        }

        private void ChromeBrowser_ConsoleMessage(object? sender, ConsoleMessageEventArgs e)
        {
            if (e.Level == LogSeverity.Error)
            {
                string message = e.Message;
                // Handle the console.log message here
            }
        }

        string playbackDevice = "";

        private void ChromeBrowser_LoadingStateChanged(object? sender, LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading)
            {
                ChangePlaybackDevice(playbackDevice);
            }
        }

        private void GetPlaybackDevices()
        {
            using (var devices = new MMDeviceEnumerator())
            {
                foreach (var device in devices.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                {
                    PlaybackDevicesComboBox.Items.Add(device.FriendlyName);
                }

                PlaybackDevicesComboBox.SelectedItem = devices.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).FriendlyName;
                playbackDevice = PlaybackDevicesComboBox.SelectedItem.ToString();
            }
        }

        private void CreateMockQueue()
        {
            Queue queue = new Queue();
            for (int i = 0; i < 20; i++) 
            {
                SongRequest songRequest = new SongRequest
                {
                    SongName = "SONG NAME SONG NAME" + i,
                    Requester = "REQUESTER NAME" + i,
                    Url = "https://www.youtube.com/embed/41VlNOyPD9U" + i,
                    Duration = i * 100,
                    Platform = i % 2 == 0 ? SongRequestPlatform.SPOTIFY : SongRequestPlatform.YOUTUBE
                };
                queue.Enqueue(songRequest);
            }
            SongRequestsListView.ItemsSource = queue;
            SongRequestsHistoryListView.ItemsSource = queue;
        }

        private void AdonisWindow_StateChanged(object sender, System.EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Visibility = Visibility.Hidden;
                this.WindowState = WindowState.Normal;
                this.Hide();
            }
        }

        private void TrayShow_Click(object sender, RoutedEventArgs e)
        {
            this.Show();
        }

        private void TrayClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TaskbarIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {

            this.Visibility = Visibility.Visible;
            this.Show();
            this.Topmost = true;
        }

        private void SongRequestsListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ListView listView = (ListView)sender;
            GridView gView = (GridView)listView.View;

            int size = 15;

            gView.Columns[0].Width = listView.ActualWidth * 0.25 - size;
            gView.Columns[1].Width = listView.ActualWidth * 0.10 - size;
            gView.Columns[2].Width = listView.ActualWidth * 0.25 - size;
            gView.Columns[3].Width = listView.ActualWidth * 0.20 - size;
            gView.Columns[4].Width = listView.ActualWidth * 0.20 - size;
            gView.Columns[5].Width = size * 5;
        }

        private void SongRequestsHistoryListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ListView listView = (ListView)sender;
            GridView gView = (GridView)listView.View;

            int size = 22;

            gView.Columns[0].Width = listView.ActualWidth * 0.25 - size;
            gView.Columns[1].Width = listView.ActualWidth * 0.10 - size;
            gView.Columns[2].Width = listView.ActualWidth * 0.25 - size;
            gView.Columns[3].Width = listView.ActualWidth * 0.20 - size;
            gView.Columns[4].Width = listView.ActualWidth * 0.20 - size;
            gView.Columns[5].Width = size * 5;
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            ChromeBrowser.ExecuteScriptAsync("document.querySelector('video').play();");
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            ChromeBrowser.ExecuteScriptAsync("document.querySelector('video').pause();");
        }

        private void PlaybackDevicesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChromeBrowser.CanExecuteJavascriptInMainFrame)
            {
                ChangePlaybackDevice(PlaybackDevicesComboBox.SelectedItem.ToString());
            }
        }

        private async void ChangePlaybackDevice(string device)
        {
            string currentAddress = string.Empty;
            ChromeBrowser.Dispatcher.Invoke(() => currentAddress = ChromeBrowser.Address);
            if (currentAddress.ToLower().Contains("youtube"))
            {
                var result = await ChromeBrowser.EvaluateScriptAsPromiseAsync($@"
                    const videoElement = document.getElementsByTagName('video')[0];
                    console.log(videoElement);
                    const audioDevices = await navigator.mediaDevices.enumerateDevices();
                    console.log(audioDevices);
                    const desiredDevice = audioDevices.find(device => device.kind === 'audiooutput' && device.label.includes('{device}'));
                    console.log(desiredDevice);
                    if (desiredDevice) {{
                        videoElement.setSinkId(desiredDevice.deviceId);
                    }}
                    ");
            }
        }

        private async void SetChromeVideoVolume(double volume)
        {
            var result = await ChromeBrowser.EvaluateScriptAsync($"document.querySelector('video').volume = {volume};");
        }

        private async void SetChromeVideoPosition(int seconds)
        {
            var result = await ChromeBrowser.EvaluateScriptAsync($"document.querySelector('video').currentTime = {seconds};");
        }

        private async Task<int> GetChromeVideoDuration()
        {
            var result = await ChromeBrowser.EvaluateScriptAsync($"document.querySelector('video').duration;");
            var duration = int.Parse(result.Result.ToString());
            return duration;
        }

        private async void Skip_Click(object sender, RoutedEventArgs e)
        {
            ChromeBrowser.Address = "https://www.youtube.com/embed/41VlNOyPD9U?autoplay=1";
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            SetupGrid.Visibility = Visibility.Visible;
            //SetupGrid.Visibility = Visibility.Collapsed;
            /*
            ITwitchAuthenticationService service = new TwitchAuthenticationService();
            TwitchAccessToken twitchAccessToken = await service.GetTwitchOAuthToken("3okampa6vvn9vq2l38rz7un83z3w56", "466f8l8llfop12tbsc4phuzzs0c0fe", "user:read:email");
            */
        }
    }
}
