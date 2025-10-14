using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace JeffPires.VisualChatGPTStudio.Utils
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool flag ? (Visibility)(flag ? 0 : 2) : (object)Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility visibility ? visibility == Visibility.Visible : (object)false;
        }
    }
}
