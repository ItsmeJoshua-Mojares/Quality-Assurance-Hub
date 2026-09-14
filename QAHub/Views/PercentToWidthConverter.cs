using System;
using System.Globalization;
using System.Windows.Data;

namespace QAHub.Views;

/// <summary>
/// Converts a 0-100 percentage into a pixel width for simple homemade bar
/// charts (no external charting library referenced in this project).
/// ConverterParameter is the max width in pixels, e.g. "300".
/// Register in App.xaml resources as:
/// <local:PercentToWidthConverter x:Key="PercentToWidthConverter" />
/// </summary>
public class PercentToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var percent = value switch
        {
            double d => d,
            int i => i,
            _ => 0.0
        };

        var maxWidth = parameter != null && double.TryParse(parameter.ToString(), out var mw) ? mw : 200.0;

        percent = Math.Clamp(percent, 0, 100);
        return maxWidth * (percent / 100.0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
