using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using TwitchSongRequest.ViewModel;

namespace TwitchSongRequest.View
{
    /// <summary>
    /// Interaction logic for MainViewUserControl.xaml
    /// </summary>
    public partial class MainViewUserControl : UserControl
    {
        public MainViewUserControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService<MainViewViewModel>();
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
    }
}
