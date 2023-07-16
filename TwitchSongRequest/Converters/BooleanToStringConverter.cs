using System;
using System.Globalization;
using System.Windows.Data;

namespace TwitchSongRequest.Converters
{
    internal class BooleanToStringConverter : IValueConverter
    {
        public string? True { get; set; }
        public string? False { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var boolValue = value as bool? ?? true;
            if (parameter != null && parameter.ToString() == "!")
            {
                boolValue = !boolValue;
            }

            if (boolValue)
            {
                return True!;
            }
            return False!;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
