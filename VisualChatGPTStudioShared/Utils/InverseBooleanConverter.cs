using System;
using System.Globalization;
using System.Windows.Data;

namespace JeffPires.VisualChatGPTStudio.Utils
{
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool flag ? (object)!flag : (object)true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool flag ? (object)!flag : (object)false;
        }
    }
}
