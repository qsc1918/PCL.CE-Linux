using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using PCL.Core.UI.Animation.ValueProcessor;

namespace PCL.Core.UI.Animation.Animatable;

// [port] 原 WpfAnimatable：WPF DependencyObject/DependencyProperty 底座 → AvaloniaObject/AvaloniaProperty。
// 对外行为与上游一致：Width/Height 动画读取实际布局尺寸（WPF ActualWidth → Avalonia Bounds）。

public sealed class AvaloniaAnimatable(AvaloniaObject owner, AvaloniaProperty? property) : IAnimatable
{
    public AvaloniaObject Owner { get; set; } = owner;
    public AvaloniaProperty? Property { get; set; } = property;

    public object? GetValue()
    {
        AvaloniaProperty? actualProperty;

        if (Property == Layoutable.WidthProperty)
        {
            // WPF 语义：Width 动画读取 ActualWidth；Avalonia 对应为 Bounds.Width
            return Owner is Visual visual ? visual.Bounds.Width : Owner.GetValue(Property!);
        }
        else if (Property == Layoutable.HeightProperty)
        {
            return Owner is Visual visual ? visual.Bounds.Height : Owner.GetValue(Property!);
        }
        else
        {
            actualProperty = Property;
        }

        ArgumentNullException.ThrowIfNull(actualProperty);

        var value  = Owner.GetValue(actualProperty);
        return value switch
        {
            SolidColorBrush brush => (NColor)brush,
            Color color => (NColor)color,
            ScaleTransform scaleTransform => (NScaleTransform)scaleTransform,
            RotateTransform rotateTransform => (NRotateTransform)rotateTransform,
            _ => value
        };
    }

    public void SetValue(object value)
    {
        value = ValueProcessorManager.Filter(value);
        ArgumentNullException.ThrowIfNull(Property);

        value = value switch
        {
            NColor color => Property.Name switch
            {
                "Color" => (Color)color,
                _ => (SolidColorBrush)color
            },
            NScaleTransform st => (Transform)st,
            NRotateTransform rt => (Transform)rt,
            _ => value
        };

        Owner.SetValue(Property, value);
    }

    public void SetValue<T>(T value)
    {
        value = ValueProcessorManager.Filter(value);
        ArgumentNullException.ThrowIfNull(Property);
        _SetValueCore(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _SetValueCore<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(Property);

        if (typeof(T) == typeof(NColor))
        {
            var color = Unsafe.As<T, NColor>(ref value);

            Owner.SetValue(
                Property,
                Property.Name == "Color"
                    ? (Color)color
                    : (SolidColorBrush)color);

            return;
        }

        if (typeof(T) == typeof(NScaleTransform))
        {
            var st = Unsafe.As<T, NScaleTransform>(ref value);
            Owner.SetValue(Property, (Transform)st);
            return;
        }

        if (typeof(T) == typeof(NRotateTransform))
        {
            var rt = Unsafe.As<T, NRotateTransform>(ref value);
            Owner.SetValue(Property, (Transform)rt);
            return;
        }

        Owner.SetValue(Property, value!);
    }
}
