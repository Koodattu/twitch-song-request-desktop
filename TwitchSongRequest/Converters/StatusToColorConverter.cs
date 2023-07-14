using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ConnectionStatus && value != null)
            {
                switch (value)
                {
                    case ConnectionStatus.Connected:
                        return new SolidColorBrush(Colors.Green);
                    case ConnectionStatus.Disconnected:
                    case ConnectionStatus.Error:
                    case ConnectionStatus.Cancelled:
                        return new SolidColorBrush(Colors.Red);
                    case ConnectionStatus.NotConnected:
                    case ConnectionStatus.Connecting:
                    case ConnectionStatus.Refreshing:
                        return new SolidColorBrush(Colors.Yellow);
                    default:
                        return new SolidColorBrush(Colors.White);
                }
            }

            if (value is PlaybackStatus && value != null)
            {
                switch (value)
                {
                    case PlaybackStatus.Playing:
                        return new SolidColorBrush(Colors.Green);
                    case PlaybackStatus.Error:
                        return new SolidColorBrush(Colors.Red);
                    case PlaybackStatus.Paused:
                        return new SolidColorBrush(Colors.Yellow);
                    case PlaybackStatus.Waiting:
                        return new SolidColorBrush(Colors.Cyan);
                    default:
                        return new SolidColorBrush(Colors.White);
                }
            }

            if (value is RewardCreationStatus && value != null)
            {
                switch (value)
                {
                    case RewardCreationStatus.Created:
                        return new SolidColorBrush(Colors.Green);
                    case RewardCreationStatus.Error:
                    case RewardCreationStatus.AlreadyExists:
                        return new SolidColorBrush(Colors.Red);
                    case RewardCreationStatus.Waiting:
                        return new SolidColorBrush(Colors.Cyan);
                    case RewardCreationStatus.Creating:
                        return new SolidColorBrush(Colors.Yellow);
                    default:
                        return new SolidColorBrush(Colors.White);
                }
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
