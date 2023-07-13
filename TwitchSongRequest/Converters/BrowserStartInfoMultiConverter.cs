using System;
using System.Globalization;
using System.Windows.Data;
using TwitchSongRequest.Model;

namespace TwitchSongRequest.Converters
{
    public class BrowserStartInfoMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is WebBrowser browser && values[1] is string url)
            {
                return new Tuple<WebBrowser, string>(browser, url);
            }
            return Binding.DoNothing;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
