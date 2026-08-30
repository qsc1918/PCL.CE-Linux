using System;
using System.IO;
using System.Security;
using PCL.Core.Logging;

namespace PCL.Core.UI.Theme;

// [port] Windows 注册表深色模式检测保留；Linux 上默认返回浅色（后续可接入 gsettings/桌面环境检测）。

public static class SystemThemeHelper {
    private const string ThemeRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeKey = "AppsUseLightTheme";

    /// <summary>
    /// 检查系统是否处于深色模式。
    /// </summary>
    /// <returns>如果系统使用深色模式，则返回 true；否则返回 false（包括注册表不可访问的情况）。</returns>
    public static bool IsSystemInDarkMode() {
        if (!OperatingSystem.IsWindows()) {
            return false;
        }

        try {
            using var registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(ThemeRegistryPath);
            if (registryKey is null) {
                LogWrapper.Warn($"注册表键 {ThemeRegistryPath} 不存在");
                return false;
            }

            var value = registryKey.GetValue(AppsUseLightThemeKey) as int?;
            return value == 0; // 0 表示深色模式（AppsUseLightTheme = false）
        } catch (Exception ex) when (ex is SecurityException or IOException) {
            LogWrapper.Warn(ex, $"无法访问注册表键 {ThemeRegistryPath}");
            return false;
        }
    }

    [Obsolete("Use ThemeService.IsDarkMode instead")]
    public static bool IsDarkMode() => ThemeService.IsDarkMode;
}
