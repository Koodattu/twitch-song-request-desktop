using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TwitchSongRequest.ViewModel;

namespace TwitchSongRequest.View
{
    /// <summary>
    /// Interaction logic for ChromeBrowserUserControl.xaml
    /// </summary>
    public partial class ChromeBrowserUserControl : UserControl
    {
        public ChromeBrowserUserControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService<ChromeBrowserViewModel>();
        }
    }
}
