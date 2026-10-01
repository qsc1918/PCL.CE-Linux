using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Primitives; // [port] TemplateAppliedEventArgs / INameScope 取模板部件
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.VisualTree;
using PCL.Core.App; // [port] WPF 静态 Config 类 → PCL.Core.App.Config（此前缺 using，名称未找到）
using PCL.Core.UI.Controls.SvgIcon;

namespace PCL;

public class MyMenuItem : MenuItem
{
    // 指向动画

    private const int AnimationTimeIn = 100;
    private const int AnimationTimeOut = 200;

    public static readonly StyledProperty<string> SvgIconProperty = AvaloniaProperty.Register<MyMenuItem, string>(
        nameof(SvgIcon), string.Empty);

    static MyMenuItem()
    {
        SvgIconProperty.Changed.AddClassHandler<MyMenuItem>((d, e) =>
        {
            if (d is MyMenuItem { IsLoaded: true } item)
                item.UpdateTemplateIcon();
        });
    }

    private SvgIcon? _svgIconControl;
    private string _colorName;

    // 基础

    public int Uuid = ModBase.GetUuid();

    public MyMenuItem()
    {
        Loaded += MyMenuItem_Loaded;
        PointerEntered += (_, _) => RefreshColor();
        PointerExited += (_, _) => RefreshColor();
        // [port] WPF IsEnabledChanged → Avalonia PropertyChanged 上的 IsEnabledProperty
        this.PropertyChanged += (_, e) => { if (e.Property == IsEnabledProperty) RefreshColor(); };
    }

    public string SvgIcon
    {
        get => (string)GetValue(SvgIconProperty);
        set => SetValue(SvgIconProperty, value);
    }

    private (string BackName, string ForeName, int Time) GetVisualState()
    {
        if (!IsEnabled)
            return ("ColorBrushTransparent", "ColorBrushGray5", AnimationTimeOut);
        if (IsPointerOver)
            return ("ColorBrush6", "ColorBrush2", AnimationTimeIn);
        return ("ColorBrushTransparent", "ColorBrush1", AnimationTimeOut);
    }

    private void MyMenuItem_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateTemplateIcon();
        // [port] Config = PCL.Core.App.Config（补 using PCL.Core.App 后可用；Avalonia 无额外等价物，数值来源一致）
        ((ContextMenu)Parent).Opacity = Config.Preference.Theme.WindowOpacity / 1000.0 + 0.4;
    }

    // [port] 缓存模板名称作用域，供 UpdateTemplateIcon 取模板部件（见其说明）
    private INameScope? _templateNameScope;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _templateNameScope = e.NameScope;
    }

    private void UpdateTemplateIcon()
    {
        // [port] Stubs 的 Template.FindName 垫片是 control.FindNameScope()?.Find(name)，
        //        菜单项此时已在 ContextMenu 树内，可能落到外层名称作用域而取不到模板内的 Icon（静默 null）。
        //        故优先用 TemplateApplied 缓存的模板名称作用域。
        var iconControl = _templateNameScope?.Find("Icon") as Path
                          ?? (Path)Template.FindName("Icon", this);
        if (iconControl is null)
            return;

        if (SvgIconControlHelper.HasSvgIcon(SvgIcon))
        {
            iconControl.IsVisible = false;
            EnsureSvgIconControl(iconControl);
            _svgIconControl!.Icon = SvgIcon;
            _svgIconControl.IsVisible = true;
            return;
        }

        _svgIconControl?.IsVisible = false;
        if (Icon is null) return;

        iconControl.IsVisible = true;
        iconControl.Data = Geometry.Parse(Icon.ToString());
    }

    private void EnsureSvgIconControl(Path iconControl)
    {
        if (_svgIconControl is not null)
            return;

        _svgIconControl = new SvgIcon
        {
            VerticalAlignment = VerticalAlignment.Center,
            Stretch = Stretch.Uniform,
            Margin = iconControl.Margin,
            Height = iconControl.Height,
            Width = iconControl.Width,
            IsHitTestVisible = false,
            // [port] WPF Visibility → Avalonia IsVisible
            IsVisible = false
        };
        // [port] WPF SetBinding(prop, binding) → Avalonia Bind(prop, binding)
        _svgIconControl.Bind(Core.UI.Controls.SvgIcon.SvgIcon.IconBrushProperty,
            new Binding(nameof(Foreground)) { Source = this });

        // [port] WPF VisualTreeHelper.GetParent → Avalonia GetVisualParent
        if (iconControl.GetVisualParent() is not Grid grid) return;

        Grid.SetColumn(_svgIconControl, Grid.GetColumn(iconControl));
        Grid.SetRow(_svgIconControl, Grid.GetRow(iconControl));
        grid.Children.Add(_svgIconControl);
    }

    private void RefreshColor()
    {
        var (backName, foreName, time) = GetVisualState();

        // 重复性验证
        if ((_colorName ?? "") == (backName ?? ""))
            return;
        _colorName = backName;
        // 触发颜色动画
        if (IsLoaded && ModAnimation.AniControlEnabled == 0) // 防止默认属性变更触发动画
        {
            // 有动画
            ModAnimation.AniStart(
                new[]
                {
                    ModAnimation.AaColor(this, BackgroundProperty, backName, time),
                    ModAnimation.AaColor(this, ForegroundProperty, foreName, time)
                }, "MyMenuItem Color " + Uuid);
        }
        else
        {
            // 无动画
            ModAnimation.AniStop("MyMenuItem Color " + Uuid);
            this.SetResourceReference(BackgroundProperty, backName);
            this.SetResourceReference(ForegroundProperty, foreName);
        }
    }

    private void MyMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ModMain.RaiseCustomEvent(this);
    }
}
