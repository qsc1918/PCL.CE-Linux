using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;
using PCL.Core.UI;
using PCL.Core.UI.Controls.SvgIcon;

namespace PCL;

internal static class SvgIconControlHelper
{
    internal static bool HasSvgIcon(string? icon)
    {
        return !string.IsNullOrWhiteSpace(icon);
    }

    internal static void ApplyVisibility(
        Path legacyIcon,
        SvgIcon svgIcon,
        bool useSvgIcon)
    {
        // [port] WPF Visibility → Avalonia IsVisible 布尔
        legacyIcon.IsVisible = !useSvgIcon;
        svgIcon.IsVisible = useSvgIcon;
    }

    internal static void ApplyIcon(Path legacyIcon, SvgIcon svgIcon, string? svgIconName)
    {
        var useSvgIcon = HasSvgIcon(svgIconName);
        svgIcon.Icon = svgIconName ?? string.Empty;
        ApplyVisibility(legacyIcon, svgIcon, useSvgIcon);
    }

    internal static void SetIconBrush(Path legacyIcon, SvgIcon svgIcon, bool useSvgIcon, Brush brush)
    {
        if (useSvgIcon)
            svgIcon.IconBrush = brush;
        else
            legacyIcon.Fill = brush;
    }

    internal static void SetIconResource(Path legacyIcon, SvgIcon svgIcon, bool useSvgIcon, string resourceKey)
    {
        if (useSvgIcon)
            svgIcon.SetResourceReference(SvgIcon.IconBrushProperty, resourceKey);
        else
            legacyIcon.SetResourceReference(Shape.FillProperty, resourceKey);
    }

    internal static void AnimateSvgIconBrushTo(
        SvgIcon svgIcon,
        string resourceKey,
        int duration,
        string? animationKey = null)
    {
        if (svgIcon.IsVisible)
            svgIcon.AnimateIconBrushTo(
                ResolveResourceColor(resourceKey),
                TimeSpan.FromMilliseconds(duration),
                animationKey: animationKey);
    }

    internal static void AnimateSvgIconBrushTo(
        SvgIcon svgIcon,
        ModBase.MyColor color,
        int duration,
        string? animationKey = null)
    {
        if (svgIcon.IsVisible)
            svgIcon.AnimateIconBrushTo(
                new NColor((Color)color),
                TimeSpan.FromMilliseconds(duration),
                animationKey: animationKey);
    }

    private static NColor ResolveResourceColor(string resourceKey)
    {
        if (!ThemeManager.AppResources.Contains(resourceKey))
            return new NColor(resourceKey);

        var resource = ThemeManager.AppResources[resourceKey];
        return resource switch
        {
            SolidColorBrush brush => new NColor(brush),
            Color color => new NColor(color),
            Brush brush => new NColor(brush),
            _ => new NColor(resourceKey)
        };
    }
}
