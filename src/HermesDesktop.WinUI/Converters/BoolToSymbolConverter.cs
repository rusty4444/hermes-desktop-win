using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Controls;

namespace HermesDesktop.WinUI.Converters
{
    public class BoolToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isDirectory)
            {
                return isDirectory ? Symbol.Folder : Symbol.Document;
            }
            return Symbol.Document;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
