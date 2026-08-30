using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;

namespace PCL.Core.UI;

/// <summary>
/// 图像加载帮助类，提供通用的图像加载功能
/// </summary>
public static class ImageLoaderHelper {
    /// <summary>
    /// 异步设置服务器图标
    /// </summary>
    /// <param name="base64String">Base64 图像字符串</param>
    /// <param name="imageElement">目标 Image 控件</param>
    /// <param name="defaultImageUri">默认图像 URI，如果为 null 则使用内置默认图标</param>
    /// <returns></returns>
    public static async Task SetServerLogoAsync(string base64String, Image imageElement, string? defaultImageUri = null) {
        await SetImageFromBase64Async(base64String, imageElement,
            defaultImageUri ?? "avares://PCL/Images/Icons/DefaultServer.png");
    }

    /// <summary>
    /// 通用的 Base64 图像加载方法
    /// </summary>
    /// <param name="base64String">Base64 图像字符串</param>
    /// <param name="imageElement">目标 Image 控件</param>
    /// <param name="fallbackImageUri">加载失败时的后备图像 URI</param>
    /// <returns></returns>
    public static async Task SetImageFromBase64Async(
        string base64String, 
        Image imageElement, 
        string? fallbackImageUri = null) {
        ArgumentNullException.ThrowIfNull(nameof(imageElement));
        
        if (string.IsNullOrWhiteSpace(base64String)) {
            SetFallbackImage(imageElement, fallbackImageUri);
            return;
        }

        try {
            // 提取 Base64 数据部分
            var base64Data = base64String.Contains(',') 
                ? base64String.Split(',')[1] 
                : base64String;

            // 验证 Base64 字符串
            if (string.IsNullOrWhiteSpace(base64Data)) {
                SetFallbackImage(imageElement, fallbackImageUri);
                return;
            }

            // 异步转换图像
            var bitmapImage = await Task.Run(() => _CreateBitmapFromBase64(base64Data));
            
            // 在 UI 线程上设置图像
            if (imageElement.Dispatcher.CheckAccess()) {
                imageElement.Source = bitmapImage;
            } else {
                await imageElement.Dispatcher.InvokeAsync(() => imageElement.Source = bitmapImage);
            }
        } catch {
            SetFallbackImage(imageElement, fallbackImageUri);
        }
    }

    /// <summary>
    /// 从 Base64 字符串创建 Bitmap（[port] WPF BitmapImage → Avalonia Bitmap）
    /// </summary>
    /// <param name="base64Data">Base64 数据</param>
    /// <returns>Bitmap 对象</returns>
    private static Bitmap _CreateBitmapFromBase64(string base64Data) {
        var imageBytes = Convert.FromBase64String(base64Data);

        using var ms = new MemoryStream(imageBytes);
        return new Bitmap(ms); // Avalonia Bitmap 直接解码流，跨线程安全
    }

    /// <summary>
    /// 从 URI 加载位图：支持 avares:// 资源与本地文件路径（[port] 替代 WPF BitmapImage(UriSource)）
    /// </summary>
    private static Bitmap? _LoadBitmapFromUri(string uri) {
        try {
            if (uri.StartsWith("avares://", StringComparison.OrdinalIgnoreCase)) {
                using var stream = Avalonia.Platform.AssetLoader.Open(new Uri(uri));
                return new Bitmap(stream);
            }
            return new Bitmap(uri); // 本地文件路径
        } catch {
            return null;
        }
    }

    /// <summary>
    /// 设置后备图像
    /// </summary>
    /// <param name="imageElement">目标 Image 控件</param>
    /// <param name="fallbackImageUri">后备图像 URI</param>
    public static void SetFallbackImage(Image imageElement, string? fallbackImageUri) {
        try {
            if (!string.IsNullOrWhiteSpace(fallbackImageUri)) {
                var defaultBitmap = _LoadBitmapFromUri(fallbackImageUri);
                if (defaultBitmap is null) throw new InvalidOperationException("后备图像加载失败");
                if (imageElement.Dispatcher.CheckAccess()) {
                    imageElement.Source = defaultBitmap;
                } else {
                    imageElement.Dispatcher.Invoke(() => imageElement.Source = defaultBitmap);
                }
            } else {
                // 如果没有提供后备图像，则清空 Source
                if (imageElement.Dispatcher.CheckAccess()) {
                    imageElement.Source = null;
                }
                else {
                    imageElement.Dispatcher.Invoke(() => imageElement.Source = null);
                }
            }
        } catch {
            // 处理后备图像加载失败的情况
            if (imageElement.Dispatcher.CheckAccess()) {
                imageElement.Source = null;
            } else {
                imageElement.Dispatcher.Invoke(() => imageElement.Source = null);
            }
        }
    }

    /// <summary>
    /// 从文件路径异步加载图像
    /// </summary>
    /// <param name="imagePath">图像文件路径</param>
    /// <param name="imageElement">目标 Image 控件</param>
    /// <param name="fallbackImageUri">后备图像 URI</param>
    /// <returns></returns>
    public static async Task SetImageFromFileAsync(
        string imagePath,
        Image imageElement,
        string fallbackImageUri) {
        ArgumentNullException.ThrowIfNull(nameof(imageElement));

        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath)) {
            SetFallbackImage(imageElement, fallbackImageUri);
            return;
        }

        try {
            var bitmapImage = await Task.Run(() => new Bitmap(imagePath));

            if (imageElement.Dispatcher.CheckAccess()) {
                imageElement.Source = bitmapImage;
            } else {
                await imageElement.Dispatcher.InvokeAsync(() => imageElement.Source = bitmapImage);
            }
        } catch {
            SetFallbackImage(imageElement, fallbackImageUri);
        }
    }
}
