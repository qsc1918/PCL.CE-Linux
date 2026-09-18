using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;

namespace PCL;

public class MyVirtualizingElement<T> : Control where T : Control
{
    private readonly Func<T> _initializer;

    public MyVirtualizingElement(Func<T> initializer)
    {
        _initializer = initializer;
        // [port] LazyLoadBehavior.cs 被延期排除（DeferredExcludes），EnableLazyLoad 不可用，
        // 此处用 EffectiveViewportChanged 内联实现懒加载：控件滚动进入可视区域时一次性实例化。
        // 原 WPF 的 RenderSize / TransformToAncestor / VisualTreeHelper 在 Avalonia 12 均不存在。
        EffectiveViewportChanged += OnEffectiveViewportChanged;
    }

    private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
    {
        if (e.EffectiveViewport.Width <= 0 || e.EffectiveViewport.Height <= 0)
            return;
        EffectiveViewportChanged -= OnEffectiveViewportChanged;
        Init();
    }

    /// <summary>
    ///     实例化此控件。
    /// </summary>
    public T Init()
    {
        var element = _initializer();
        if (Parent is not null)
        {
            if (Parent is not Panel)
                throw new Exception("MyVirtualizingElement 的父级必须是一个 Panel");
            var parentPanel = (Panel)Parent;
            var currentIndex = parentPanel.Children.IndexOf(this);
            parentPanel.Children.RemoveAt(currentIndex);
            parentPanel.Children.Insert(currentIndex, element);
        }

        return element;
    }

    public static implicit operator T(MyVirtualizingElement<T> virtualized)
    {
        return virtualized.Init();
    }
}

// 非泛型形式
public class MyVirtualizingElement : Control
{
    private readonly Func<Control> _initializer;

    public MyVirtualizingElement(Func<Control> initializer)
    {
        _initializer = initializer;
        // [port] LazyLoadBehavior.cs 被延期排除（DeferredExcludes），EnableLazyLoad 不可用，
        // 此处用 EffectiveViewportChanged 内联实现懒加载，见上方泛型类注释。
        EffectiveViewportChanged += OnEffectiveViewportChanged;
    }

    private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
    {
        if (e.EffectiveViewport.Width <= 0 || e.EffectiveViewport.Height <= 0)
            return;
        EffectiveViewportChanged -= OnEffectiveViewportChanged;
        Init();
    }

    /// <summary>
    ///     实例化此控件。
    /// </summary>
    public Control Init()
    {
        var element = _initializer();
        if (Parent is not null)
        {
            if (Parent is not Panel)
                throw new Exception("MyVirtualizingElement 的父级必须是一个 Panel");
            var parentPanel = (Panel)Parent;
            var currentIndex = parentPanel.Children.IndexOf(this);
            parentPanel.Children.RemoveAt(currentIndex);
            parentPanel.Children.Insert(currentIndex, element);
        }

        return element;
    }

    /// <summary>
    ///     获取实例化后的控件。
    ///     如果该控件没有实例化，则会立即实例化。
    ///     如果类型错误，则返回原值。
    /// </summary>
    public static Control TryInit(Control element)
    {
        if (typeof(MyVirtualizingElement<>).IsInstanceOfGenericType(element))
        {
            var method = element.GetType().GetMethod("Init", Type.EmptyTypes);
            return (Control)method.Invoke(element, null);
        }
        return element is MyVirtualizingElement ? ((MyVirtualizingElement)element).Init() : element;
    }
}
