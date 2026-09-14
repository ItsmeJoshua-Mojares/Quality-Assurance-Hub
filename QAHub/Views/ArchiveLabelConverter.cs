using System;
using System.Globalization;
using System.Windows.Data;
using QAHub.Models;

namespace QAHub.Views;

/// <summary>
/// Register in App.xaml resources as:
/// <local:ArchiveLabelConverter x:Key="ArchiveLabelConverter" />
/// </summary>
public class ArchiveLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is ProjectStatus status && status == ProjectStatus.Active ? "Archive" : "Unarchive";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
