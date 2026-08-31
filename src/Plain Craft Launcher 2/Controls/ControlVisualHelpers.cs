using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace PCL;

internal static class ControlVisualHelpers
{
    internal static bool ShouldAnimate(Control control, object? animationOverride = null)
    {
        return control.IsLoaded && ModAnimation.AniControlEnabled == 0 && !false.Equals(animationOverride);
    }

    internal static void AnimateColorOrSetResource(Control target, AvaloniaProperty property,
        string resourceKey, int duration, string animationKey, bool shouldAnimate)
    {
        if (shouldAnimate)
        {
            ModAnimation.AniStart(ModAnimation.AaColor(target, property, resourceKey, duration), animationKey);
        }
        else
        {
            ModAnimation.AniStop(animationKey);
            target.Bind(property, target.GetResourceObservable(resourceKey)); // [port] Avalonia 动态资源绑定
        }
    }
}
