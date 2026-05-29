using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace HermesDesktop.WinUI.Converters
{
    public class ChatRoleToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var role = value as string;
            return role switch
            {
                "user" => new SolidColorBrush(Windows.UI.Color.FromArgb(40, 0, 120, 212)),       // blue tint
                "assistant" => new SolidColorBrush(Windows.UI.Color.FromArgb(40, 0, 153, 76)),    // green tint
                "error" => new SolidColorBrush(Windows.UI.Color.FromArgb(40, 212, 0, 0)),         // red tint
                "system" => new SolidColorBrush(Windows.UI.Color.FromArgb(40, 128, 128, 128)),    // grey tint
                _ => new SolidColorBrush(Windows.UI.Color.FromArgb(40, 128, 128, 128)),
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b) return !b;
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b) return !b;
            return false;
        }
    }
}
