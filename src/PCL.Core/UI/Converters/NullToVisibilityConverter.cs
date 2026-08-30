using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace PCL.Core.UI.Converters;

/// <summary>
/// 将可 null 的值转换为可见性状态：若为 null 则隐藏，否则可见。
/// [port] WPF Visibility 枚举 → Avalonia IsVisible bool（XAML 端绑定 IsVisible）。
/// </summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
