using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace HermesDesktop.WinUI.Converters
{
    public class InvertBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
                return !b ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility v)
                return v != Visibility.Visible;
            return true;
        }
    }
}
