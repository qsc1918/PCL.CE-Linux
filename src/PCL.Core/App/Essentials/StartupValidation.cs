using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace PCL.Core.App.Essentials;

public static class StartupValidation
{
    /// <summary>
    ///     确保 WPF 字体渲染环境正常（修复缺失 %windir% 环境变量导致的字体渲染异常 #3555）
    /// [port] 该修复针对 Windows WPF 字体栈，Linux/Avalonia 无此问题，保留为空实现。
    /// </summary>
    public static void EnsureWpfFont()
    {
#if WINDOWS
        try
        {
            _ = new FormattedText("", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                Fonts.SystemTypefaces.First(), 96d, Brushes.Black, 96d);
        }
        catch (UriFormatException)
        {
            Environment.SetEnvironmentVariable("windir", Environment.GetEnvironmentVariable("SystemRoot"),
                EnvironmentVariableTarget.User);
            _ = new FormattedText("", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                Fonts.SystemTypefaces.First(), 96d, Brushes.Black, 96d);
        }
#endif
    }
}
