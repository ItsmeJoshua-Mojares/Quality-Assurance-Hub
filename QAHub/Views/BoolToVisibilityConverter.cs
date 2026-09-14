using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QAHub.Views
{
    /// <summary>
    /// Register in App.xaml resources as:
    /// <local:BoolToVisibilityConverter x:Key="BoolToVisConverter" />
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var flag = value is bool b && b;
            if (parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase))
            {
                flag = !flag;
            }
            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility v && v == Visibility.Visible;
        }
    }
}
