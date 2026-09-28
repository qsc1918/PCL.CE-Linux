using System;
using Avalonia.Media;

namespace PCL.Core.UI.Theme;

/// <summary>
/// [port] Avalonia 的 Color 没有 WPF 的 scRGB 浮点接口（<c>Color.FromScRgb</c> / <c>Color.ScR</c>），
/// 故在此提供等价实现。
/// <para>
/// 关键语义：WPF 的 scRGB 通道是<b>线性</b>值（linear-light，0~1，HDR 可 &gt;1），
/// <c>Color.FromScRgb</c> 会先做「线性 → sRGB」传递函数，再量化成 0~255 字节。
/// 它<b>不是</b>「sRGB 分量 / 255」——那只是 WPF 的 <c>Color.ScR</c> 与 <c>Color.R</c> 的换算关系，
/// 与 <c>FromScRgb</c> 的入参量纲无关。
/// </para>
/// <para>
/// 上游 PCL 的颜色全部由 <see cref="LabColor.ToScRgb" />（返回 <c>RgbLinear</c>）产出，
/// 因此这里必须补上线性→sRGB 转换；否则整站配色会整体偏暗
/// （例如标题栏本应 #3184E4，直接量化会得到 #0637C3）。
/// </para>
/// </summary>
public static class ScRgbCompat
{
    public static Color FromScRgb(float a, float r, float g, float b)
    {
        return Color.FromArgb(_ToByte(a), _ToSrgbByte(r), _ToSrgbByte(g), _ToSrgbByte(b));
    }

    /// <summary>
    /// 线性 scRGB 分量 → sRGB 字节（含 sRGB 传递函数与量化）。
    /// </summary>
    private static byte _ToSrgbByte(float linear)
    {
        if (float.IsNaN(linear))
            return 0;

        var v = Math.Clamp((double)linear, 0d, 1d);
        // sRGB 传递函数（IEC 61966-2-1）
        var srgb = v <= 0.0031308d
            ? v * 12.92d
            : 1.055d * Math.Pow(v, 1d / 2.4d) - 0.055d;
        return (byte)Math.Clamp(Math.Round(srgb * 255d), 0d, 255d);
    }

    /// <summary>
    /// Alpha 在 scRGB 中同样是 0~1 的线性值（不参与 gamma 编码），直接量化。
    /// 兼容上游部分调用点传入 0~255 字节值的写法（量化后仍落在 0~255）。
    /// </summary>
    private static byte _ToByte(float value)
    {
        if (float.IsNaN(value))
            return 0;

        var scaled = value > 1f ? value : value * 255f;
        return (byte)Math.Clamp(Math.Round((double)scaled), 0d, 255d);
    }
}
