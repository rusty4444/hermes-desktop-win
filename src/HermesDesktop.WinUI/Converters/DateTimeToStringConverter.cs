using System;
using System.Globalization;
using Microsoft.UI.Xaml.Data;

namespace HermesDesktop.WinUI.Converters
{
    public class DateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double unixTime)
            {
                // Convert Unix timestamp to DateTime
                var dateTime = DateTimeOffset.FromUnixTimeSeconds((long)unixTime).DateTime;
                return dateTime.ToString("g"); // General short date/time format
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
