using System;
using Microsoft.UI.Xaml.Data;

namespace HermesDesktop.WinUI.Converters
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // If the value is null, return Collapsed; otherwise, return Visible.
            // We can invert the logic if needed by using a parameter, but for now we do this.
            if (value == null)
            {
                return Windows.UI.Xaml.Visibility.Collapsed;
            }
            return Windows.UI.Xaml.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
