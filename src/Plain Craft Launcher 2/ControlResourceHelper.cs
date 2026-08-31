using Avalonia;
using Avalonia.Controls;

namespace PCL;

/// <summary>
/// [port] WPF SetResourceReference(dp, resourceKey) -> Avalonia resource binding.
/// Extension method for AvaloniaObject subclasses.
/// </summary>
public static class ControlResourceHelper
{
    public static void SetResourceReference(this AvaloniaObject target, AvaloniaProperty property, string resourceKey)
    {
        if (target is not Control ctrl) return;
        var app = Application.Current;
        if (app is null) return;
        target[!property] = ctrl.GetResourceObservable(resourceKey);
    }
}
