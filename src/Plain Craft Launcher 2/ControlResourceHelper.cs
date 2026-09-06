using Avalonia;
using Avalonia.Controls;

namespace PCL;

/// <summary>
/// [port] WPF SetResourceReference(dp, resourceKey) -> Avalonia dynamic resource binding.
/// Uses Bind(GetResourceObservable) so theme changes propagate (matches WPF DynamicResource semantics).
/// </summary>
public static class ControlResourceHelper
{
    public static void SetResourceReference(this AvaloniaObject target, AvaloniaProperty property, string resourceKey)
    {
        // GetResourceObservable is on Control (theme-aware resource lookup). Bind observes it for updates.
        if (target is Control ctrl)
        {
            target.Bind(property, ctrl.GetResourceObservable(resourceKey));
            return;
        }
        var app = Application.Current;
        if (app is null) return;
        if (app.TryGetResource(resourceKey, app.ActualThemeVariant, out var value))
        {
            target.SetValue(property, value);
        }
    }
}
