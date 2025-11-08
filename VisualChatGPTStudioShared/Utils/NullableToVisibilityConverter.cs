using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace JeffPires.VisualChatGPTStudio.Utils
{
    public class NullableToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null || (value is string strValue && string.IsNullOrEmpty(strValue))
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null || (value is string strValue && string.IsNullOrEmpty(strValue))
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
}
