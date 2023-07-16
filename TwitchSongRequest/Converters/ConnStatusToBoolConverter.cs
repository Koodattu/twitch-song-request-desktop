using System;
using System.Globalization;
using System.Windows.Data;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Converters
{
    internal class ConnStatusToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value as ConnectionStatus? ?? ConnectionStatus.Error;
            switch (status)
            {
                case ConnectionStatus.NotConnected:
                case ConnectionStatus.Disconnected:
                case ConnectionStatus.Connected:
                case ConnectionStatus.Cancelled:
                case ConnectionStatus.Error:
                    return true;
                case ConnectionStatus.Authenticated:
                case ConnectionStatus.Connecting:
                case ConnectionStatus.Refreshing:
                    return false;
                default:
                    return "ERROR";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
