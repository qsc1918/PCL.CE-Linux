using Avalonia;

namespace PCL;

/// <summary>
/// [port] WPF SetResourceReference(dp, resourceKey) -> Avalonia resource lookup + set.
/// NOTE: This sets the value once. A live binding would require per-property ResourceObservable subscriptions.
/// For the porting phase, direct set is sufficient; dynamic theme switching can be added later.
/// </summary>
public static class ControlResourceHelper
{
    public static void SetResourceReference(this AvaloniaObject target, AvaloniaProperty property, string resourceKey)
    {
        var app = Application.Current;
        if (app is null) return;
        if (app.TryGetResource(resourceKey, app.ActualThemeVariant, out var value))
        {
            target.SetValue(property, value);
        }
    }
}
