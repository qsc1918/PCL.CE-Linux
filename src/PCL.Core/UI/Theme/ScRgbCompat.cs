using System;
using Avalonia.Media;

namespace PCL.Core.UI.Theme;

/// <summary>
/// [port] Avalonia Color 无 ScRGB 浮点接口（WPF Color.ScR/FromScRgb）。
/// WPF 的 ScR/ScG/ScB/ScA 是 sRGB 分量的浮点表达（R/255），据此提供等价实现。
/// </summary>
public static class ScRgbCompat
{
    public static Color FromScRgb(float a, float r, float g, float b)
    {
        return Color.FromArgb(
            (byte)Math.Clamp(a * 255f + 0.5f, 0f, 255f),
            (byte)Math.Clamp(r * 255f + 0.5f, 0f, 255f),
            (byte)Math.Clamp(g * 255f + 0.5f, 0f, 255f),
            (byte)Math.Clamp(b * 255f + 0.5f, 0f, 255f));
    }
}
