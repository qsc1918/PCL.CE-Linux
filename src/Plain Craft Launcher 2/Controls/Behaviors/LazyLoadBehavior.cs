using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Xaml.Interactivity;

namespace PCL;

internal static class LazyLoader
{
    public static void EnableLazyLoad(this Control element, Action action)
    {
        var behavior = new LazyLoadBehavior();
        behavior.Action = action;
        Interaction.GetBehaviors(element).Add(behavior);
    }
}

public class LazyLoadBehavior : Behavior<Control>
{
    public static readonly AvaloniaProperty ActionProperty = AvaloniaProperty.Register(nameof(Action),
        typeof(Action), typeof(LazyLoadBehavior), new PropertyMetadata(null));

    public Action Action
    {
        get => (Action)GetValue(ActionProperty);
        set => SetValue(ActionProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.LayoutUpdated += OnLayoutUpdated;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.LayoutUpdated -= OnLayoutUpdated;
        base.OnDetaching();
    }

    private void OnLayoutUpdated(object sender, EventArgs e)
    {
        var element = AssociatedObject;
        if (element is null || element.RenderSize.Width < double.Epsilon || !element.IsVisible)
            return;

        var scrollViewer = FindParentScrollViewer(element);
        if (scrollViewer is null)
            return;

        var elementBounds = element.TransformToAncestor(scrollViewer)
            .TransformBounds(new Rect(new Point(0d, 0d), element.RenderSize));
        var viewport = new Rect(0d, 0d, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);

        if (!viewport.IntersectsWith(elementBounds))
            return;

        Action?.Invoke();
        // 仅执行一次
        element.LayoutUpdated -= OnLayoutUpdated;
    }

    private static ScrollViewer FindParentScrollViewer(AvaloniaObject d)
    {
        for (var current = d; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is ScrollViewer scrollViewer)
                return scrollViewer;
        }

        return null;
    }
}
