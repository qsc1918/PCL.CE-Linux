namespace PCL.Core.Utils.OS;

using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Platform.Storage;

// [port] WPF Clipboard/DataFormats.FileDrop → Avalonia IClipboard + DataTransferItem.SetFile（IStorageItem）。
// Linux 桌面环境对文件剪贴板支持有限，失败时静默降级。

public static class ClipboardUtils {
    /// <summary>
    /// 将剪贴板内容设置为用于复制/粘贴操作的文件或文件夹路径列表。
    /// </summary>
    /// <param name="paths">要设置到剪贴板的文件或文件夹路径数组。</param>
    public static async Task SetClipboardFilesAsync(string[] paths) {
        if (paths is null || paths.Length == 0) {
            throw new ArgumentException("Paths cannot be null or empty.", nameof(paths));
        }

        var topLevel = (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        var clipboard = topLevel?.Clipboard;
        if (topLevel is null || clipboard is null) return;

        var data = new DataTransfer();
        foreach (var path in paths) {
            try {
                IStorageItem? storageItem = null;
                if (File.Exists(path)) {
                    storageItem = await topLevel.StorageProvider.TryGetFileFromPathAsync(path);
                } else if (Directory.Exists(path)) {
                    storageItem = await topLevel.StorageProvider.TryGetFolderFromPathAsync(path);
                }
                if (storageItem is null) continue;

                var item = new DataTransferItem();
                item.SetFile(storageItem);
                data.Add(item);
            } catch {
                // 单个路径失败时跳过
            }
        }
        if (data.Items.Count == 0) return;
        await clipboard.SetDataAsync(data);
    }
}
