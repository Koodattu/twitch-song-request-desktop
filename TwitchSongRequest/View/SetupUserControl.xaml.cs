using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using TwitchSongRequest.ViewModel;

namespace TwitchSongRequest.View
{
    /// <summary>
    /// Interaction logic for SetupUserControl.xaml
    /// </summary>
    public partial class SetupUserControl : UserControl
    {
        public SetupUserControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService<SetupViewModel>();
        }
    }
}
