using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using QAHub.Models;

namespace QAHub.Views;

/// <summary>Returns an accent color per StandardCategory for category badges.</summary>
public class StandardCategoryBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var color = value is StandardCategory category
            ? category switch
            {
                StandardCategory.Ansi => Color.FromRgb(0x2F, 0x81, 0xF7),
                StandardCategory.Iec => Color.FromRgb(0x10, 0x9D, 0x6A),
                StandardCategory.Iso => Color.FromRgb(0xE1, 0x93, 0x3A),
                StandardCategory.CompanySpec => Color.FromRgb(0x8B, 0x5C, 0xF6),
                _ => Color.FromRgb(0x6B, 0x72, 0x80)
            }
            : Color.FromRgb(0x6B, 0x72, 0x80);

        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}