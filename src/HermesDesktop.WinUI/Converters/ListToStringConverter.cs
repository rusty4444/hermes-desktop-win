using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.UI.Xaml.Data;

namespace HermesDesktop.WinUI.Converters
{
    public class ListToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is IList<string> list)
            {
                return string.Join(", ", list);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is string str)
            {
                // Split by comma and trim each part
                return str.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToList();
            }
            return new List<string>();
        }
    }
}
