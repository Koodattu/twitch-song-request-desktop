using System;
using System.Windows.Data;

namespace TwitchSongRequest.Converters
{
    public class MmSsFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int ss = value is double ? System.Convert.ToInt32((double)value) : (int)value;
            int mm = ss / 60;
            ss %= 60;
            return string.Format(@"{0:D2}:{1:D2}", mm, ss);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
