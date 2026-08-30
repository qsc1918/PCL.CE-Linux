using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace PCL.Core.Utils;



public static class WpfUtils
{
    public static bool IsDependencyPropertySet(AvaloniaObject obj, AvaloniaProperty dp)
    {
        return obj.IsSet(dp);
    }
}