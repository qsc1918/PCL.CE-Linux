using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace PCL;

// [port] Stub types for excluded features (ModVideoBack, BlurBorder, MotdRenderer, etc.)
// These provide minimal API surface so code referencing them compiles.
// All methods are no-ops or return default values.

/// <summary>Stub for ModVideoBack (video background feature, deferred).</summary>
public class ModVideoBack
{
    public static void Stop() { }
    public static void Play(string path, bool isLoop = true) { }
    public static bool IsPlaying => false;
}

/// <summary>Stub for BlurBorder (HLSL blur effect, Avalonia no custom pixel shader).</summary>
public class BlurBorder : Border
{
    public static readonly StyledProperty<double> BlurRadiusProperty =
        AvaloniaProperty.Register<BlurBorder, double>(nameof(BlurRadius), 10d);
    public double BlurRadius { get => GetValue(BlurRadiusProperty); set => SetValue(BlurRadiusProperty, value); }
}

/// <summary>Stub for MotdRenderer (MOTD rendering, deferred).</summary>
public class MotdRenderer : Control
{
    public string MotdText { get; set; } = "";
}
