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
                    case ConnectionStatus.CONNECTED:
                        return new SolidColorBrush(Colors.Green);
                    case ConnectionStatus.DISCONNECTED:
                    case ConnectionStatus.ERROR:
                        return new SolidColorBrush(Colors.Red);
                    case ConnectionStatus.NOT_CONNECTED:
                    case ConnectionStatus.CONNECTING:
                        return new SolidColorBrush(Colors.Yellow);
                    default:
                        return new SolidColorBrush(Colors.White);
                }
            }

            if (value is PlaybackStatus && value != null)
            {
                switch (value)
                {
                    case PlaybackStatus.PLAYING:
                        return new SolidColorBrush(Colors.Green);
                    case PlaybackStatus.ERROR:
                        return new SolidColorBrush(Colors.Red);
                    case PlaybackStatus.PAUSED:
                        return new SolidColorBrush(Colors.Yellow);
                    case PlaybackStatus.WAITING:
                        return new SolidColorBrush(Colors.Cyan);
                    default:
                        return new SolidColorBrush(Colors.White);
                }
            }

            if (value is RewardCreationStatus && value != null)
            {
                switch (value)
                {
                    case RewardCreationStatus.SUCCESS:
                        return new SolidColorBrush(Colors.Green);
                    case RewardCreationStatus.ERROR:
                    case RewardCreationStatus.ALREADY_EXISTS:
                        return new SolidColorBrush(Colors.Red);
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
