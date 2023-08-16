using Microsoft.Extensions.DependencyInjection;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
            this.DataContext = App.Current.Services.GetService<MainViewViewModel>();
            Loaded += (s, e) =>
            {
                this.EventsListBox.IsVisibleChanged += (s, e) =>
                {
                    if (this.EventsListBox.IsVisible)
                    {
                        this.EventsListBox.ScrollIntoView(EventsListBox.Items[EventsListBox.Items.Count - 1]);
                    }
                };

                ((INotifyCollectionChanged)this.EventsListBox.ItemsSource).CollectionChanged += (s, e) =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Add)
                    {
                        this.EventsListBox.ScrollIntoView(EventsListBox.Items[EventsListBox.Items.Count - 1]);
                    }
                };
            };
        }

        public static T GetChildOfType<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);

                var result = (child as T) ?? GetChildOfType<T>(child);
                if (result != null) return result;
            }
            return null;
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
