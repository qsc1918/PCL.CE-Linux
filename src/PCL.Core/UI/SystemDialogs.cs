using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using PCL.Core.App.Localization;
using PCL.Core.Logging;

namespace PCL.Core.UI;

/// <summary>
///     提供文件和文件夹对话框相关的实用方法。
/// [port] WPF Microsoft.Win32 对话框（同步）→ Avalonia StorageProvider（异步）。
/// 调用点需改为 await；Windows 特有的 Filter 字符串语法被解析为 FilePickerFileType。
/// </summary>
public static class SystemDialogs
{
    private static TopLevel? _GetTopLevel()
        => (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    /// <summary>
    ///     将 WPF 过滤器语法（"描述|*.png;*.jpg|描述2|*.zip"）解析为 Avalonia FilePickerFileType 列表。
    /// </summary>
    private static List<FilePickerFileType> _ParseFilter(string filter)
    {
        var result = new List<FilePickerFileType>();
        if (string.IsNullOrWhiteSpace(filter)) return result;

        var parts = filter.Split('|');
        for (var i = 0; i + 1 < parts.Length; i += 2)
        {
            var description = parts[i].Trim();
            var patterns = parts[i + 1]
                .Split(';')
                .Select(p => p.Trim().TrimStart('*'))
                .Where(p => p.Length > 0)
                .ToArray();
            if (patterns.Length == 0) continue;
            result.Add(new FilePickerFileType(description) { Patterns = patterns });
        }

        // WPF 语法中 "*.*" 表示所有文件，Avalonia 无对应类型，忽略即可（不添加任何限制）
        return result;
    }

    /// <summary>
    ///     显示保存文件对话框，要求用户选择保存位置。
    /// </summary>
    public static async Task<string> SelectSaveFileAsync(
        string? title,
        string fileName,
        string? fileFilter = null,
        string? initialDirectory = null)
    {
        var top = _GetTopLevel();
        if (top is null)
        {
            LogWrapper.Warn("Dialog", "无可用窗口，无法打开保存文件对话框");
            return "";
        }

        var dialogTitle = title ?? Lang.Text("SystemDialog.File.SelectTitle");
        LogWrapper.Info("Dialog", $"打开保存文件对话框：{dialogTitle}");
        var options = new FilePickerSaveOptions
        {
            Title = dialogTitle,
            SuggestedFileName = fileName,
            SuggestedStartLocation = await _TryGetFolderAsync(top, initialDirectory),
            FileTypeChoices = _ParseFilter(fileFilter ?? Lang.Text("SystemDialog.File.AllFilesFilter"))
        };

        var file = await top.StorageProvider.SaveFilePickerAsync(options);
        if (file is null)
        {
            LogWrapper.Info("Dialog", "选择文件被取消");
            return "";
        }

        var selectedPath = file.TryGetLocalPath() ?? "";
        LogWrapper.Info("Dialog", $"选择文件返回：{selectedPath}");
        return string.IsNullOrEmpty(selectedPath) ? "" : Path.GetFullPath(selectedPath);
    }

    /// <summary>
    ///     显示打开文件对话框，要求用户选择单个文件。
    /// </summary>
    public static async Task<string> SelectFileAsync(
        string? fileFilter = null,
        string? title = null,
        string? initialDirectory = null)
    {
        var files = await _SelectFilesAsync(fileFilter, title, initialDirectory, false, single: true);
        return files.Length == 0 ? "" : files[0];
    }

    /// <summary>
    ///     显示打开文件对话框，要求用户选择文件。
    /// </summary>
    public static Task<string[]> SelectFilesAsync(
        string? fileFilter = null,
        string? title = null,
        string? initialDirectory = null,
        bool allowMultiSelect = true)
        => _SelectFilesAsync(fileFilter, title, initialDirectory, allowMultiSelect, single: false);

    private static async Task<string[]> _SelectFilesAsync(
        string? fileFilter,
        string? title,
        string? initialDirectory,
        bool allowMultiSelect,
        bool single)
    {
        var top = _GetTopLevel();
        if (top is null)
        {
            LogWrapper.Warn("Dialog", "无可用窗口，无法打开选择文件对话框");
            return [];
        }

        var dialogTitle = title ?? Lang.Text("SystemDialog.File.SelectTitle");
        var num = allowMultiSelect ? "多" : "单";
        LogWrapper.Info("Dialog", $"打开选择{num}个文件对话框: {dialogTitle}");
        var options = new FilePickerOpenOptions
        {
            Title = dialogTitle,
            AllowMultiple = allowMultiSelect && !single,
            SuggestedStartLocation = await _TryGetFolderAsync(top, initialDirectory),
            FileTypeFilter = _ParseFilter(fileFilter ?? Lang.Text("SystemDialog.File.AllFilesFilter"))
        };

        var files = await top.StorageProvider.OpenFilePickerAsync(options);
        if (files.Count == 0)
        {
            LogWrapper.Info("Dialog", "选择文件被取消");
            return [];
        }

        var selectedFiles = files
            .Select(f => f.TryGetLocalPath())
            .Where(p => !string.IsNullOrEmpty(p))
            .Cast<string>()
            .ToArray();
        LogWrapper.Info("Dialog", $"选择{num}个文件返回: {string.Join(",", selectedFiles)}");
        return selectedFiles;
    }

    /// <summary>
    ///     显示文件夹选择对话框，要求用户选择一个文件夹。
    /// </summary>
    public static async Task<string> SelectFolderAsync(string? title = null, string? initialDirectory = null)
    {
        var top = _GetTopLevel();
        if (top is null)
        {
            LogWrapper.Warn("Dialog", "无可用窗口，无法打开选择文件夹对话框");
            return "";
        }

        var dialogTitle = title ?? Lang.Text("SystemDialog.Folder.SelectTitle");
        LogWrapper.Info("Dialog", $"打开选择文件夹对话框: {dialogTitle}");
        var options = new FolderPickerOpenOptions
        {
            Title = dialogTitle,
            AllowMultiple = false,
            SuggestedStartLocation = await _TryGetFolderAsync(top, initialDirectory)
        };

        var folders = await top.StorageProvider.OpenFolderPickerAsync(options);
        if (folders.Count == 0)
        {
            LogWrapper.Info("Dialog", "选择文件夹被取消");
            return "";
        }

        var selectedPath = folders[0].TryGetLocalPath() ?? "";
        if (string.IsNullOrEmpty(selectedPath))
        {
            LogWrapper.Info("Dialog", "选择文件夹返回: 空");
            return "";
        }

        var normalizedPath = Path.GetFullPath(selectedPath).TrimEnd(Path.DirectorySeparatorChar) +
                             Path.DirectorySeparatorChar;
        LogWrapper.Info("Dialog", $"选择文件夹返回: {normalizedPath}");
        return normalizedPath;
    }

    private static async Task<IStorageFolder?> _TryGetFolderAsync(TopLevel top, string? path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return null;
        try
        {
            return await top.StorageProvider.TryGetFolderFromPathAsync(path);
        }
        catch
        {
            return null;
        }
    }
}
