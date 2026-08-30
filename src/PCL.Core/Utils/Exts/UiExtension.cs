namespace PCL.Core.Utils.Exts;

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;

// [port] WPF TransformToAncestor → Avalonia TransformToVisual（返回 Matrix）；
// Rect.IntersectsWith → Rect.Intersects；FormattedText 构造去掉 PixelsPerDip（Avalonia 12 六参构造）。

/// <summary>
/// 提供 Avalonia UI 控件的扩展方法。
/// </summary>
public static class UiExtension {
    /// <summary>
    /// 检查控件是否在指定窗口的可视区域内，且控件本身可见。
    /// </summary>
    /// <param name="element">要检查的 Control。</param>
    /// <param name="mainWindow">主窗口，用于确定可视区域。</param>
    /// <returns>如果控件部分或完全在窗口可视区域内且可见，则返回 true；否则返回 false。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="element"/> 或 <paramref name="mainWindow"/> 为 null 时抛出。</exception>
    public static bool IsVisibleInWindow(this Control element, Window mainWindow) {
        if (!element.IsVisible) return false;

        try {
            var matrix = element.TransformToVisual(mainWindow);
            if (matrix is null) return false;
            // [port] Avalonia 12 Matrix 无 TransformBounds，手动变换矩形四角
            var rect = new Rect(0, 0, element.Bounds.Width, element.Bounds.Height);
            var p1 = matrix.Value.Transform(rect.TopLeft);
            var p2 = matrix.Value.Transform(rect.TopRight);
            var p3 = matrix.Value.Transform(rect.BottomLeft);
            var p4 = matrix.Value.Transform(rect.BottomRight);
            var minX = Math.Min(Math.Min(p1.X, p2.X), Math.Min(p3.X, p4.X));
            var minY = Math.Min(Math.Min(p1.Y, p2.Y), Math.Min(p3.Y, p4.Y));
            var maxX = Math.Max(Math.Max(p1.X, p2.X), Math.Max(p3.X, p4.X));
            var maxY = Math.Max(Math.Max(p1.Y, p2.Y), Math.Max(p3.Y, p4.Y));
            var bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
            var windowRect = new Rect(0, 0, mainWindow.Bounds.Width, mainWindow.Bounds.Height);
            return windowRect.Intersects(bounds);
        } catch (InvalidOperationException) {
            return false;
        }
    }

    /// <summary>
    /// 检查 TextBlock 是否因 TextTrimming 属性导致文本被截断。
    /// </summary>
    /// <param name="textBlock">要检查的 TextBlock。</param>
    /// <returns>如果文本被截断，则返回 true；否则返回 false。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="textBlock"/> 为 null 时抛出。</exception>
    public static bool IsTextTrimmed(this TextBlock textBlock) {
        if (textBlock.TextTrimming == TextTrimming.None) return false;

        try {
            var formattedText = new FormattedText(
                textBlock.Text ?? string.Empty,
                System.Globalization.CultureInfo.CurrentCulture,
                textBlock.FlowDirection,
                new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
                textBlock.FontSize,
                textBlock.Foreground
                );

            return formattedText.Width > textBlock.Bounds.Width;
        } catch (Exception) {
            return false;
        }
    }
}
