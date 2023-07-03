using AdonisUI.Controls;
using CefSharp;
using System.Windows;
using System.Windows.Controls;

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
    
    }
}
