using System;
using System.Globalization;
using System.Windows.Data;
using QAHub.Models;

namespace QAHub.Views;

/// <summary>Renders a StandardCategory enum as a human-friendly label.</summary>
public class StandardCategoryLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is StandardCategory category ? category.GetDisplayName() : "Other";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}