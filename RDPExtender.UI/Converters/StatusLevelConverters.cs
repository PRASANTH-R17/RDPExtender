using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using RDPExtender.Models;

namespace RDPExtender.UI.Converters;

public sealed class StatusLevelToBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = (StatusLevel)(value ?? StatusLevel.Warning) switch
        {
            StatusLevel.Ok => "SuccessBgBrush",
            StatusLevel.Error => "ErrorBgBrush",
            _ => "WarningBgBrush"
        };
        return Application.Current?.TryFindResource(key) ?? Brushes.LightGray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class StatusLevelToForegroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = (StatusLevel)(value ?? StatusLevel.Warning) switch
        {
            StatusLevel.Ok => "SuccessFgBrush",
            StatusLevel.Error => "ErrorFgBrush",
            _ => "WarningFgBrush"
        };
        return Application.Current?.TryFindResource(key) ?? Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class StatusLevelToBadgeIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (StatusLevel)(value ?? StatusLevel.Warning) switch
        {
            StatusLevel.Ok => "\uE73E",
            StatusLevel.Error => "\uE711",
            _ => "\uE915"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var invert = string.Equals(parameter as string, "Invert", StringComparison.Ordinal);
        var visible = value is true;
        if (invert)
        {
            visible = !visible;
        }

        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
