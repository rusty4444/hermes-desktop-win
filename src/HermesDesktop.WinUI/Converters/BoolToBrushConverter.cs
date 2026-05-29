using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace HermesDesktop.WinUI.Converters
{
    public class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b && b)
                return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 153, 76)); // green
            return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 212, 0, 0));       // red
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
