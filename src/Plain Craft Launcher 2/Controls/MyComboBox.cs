using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Input;

using PCL.Core.App.Localization;
namespace PCL;

public class MyComboBox : ComboBox
{
    public delegate void TextChangedEventHandler(object sender, TextChangedEventArgs e);

    public static readonly StyledProperty<string> HintTextProperty = AvaloniaProperty.Register<MyComboBox, string>(
        nameof(HintText), string.Empty);

    static MyComboBox()
    {
        HintTextProperty.Changed.AddClassHandler<MyComboBox>((d, e) =>
        {
            if (d.textBox is not null)
                d.textBox.HintText = (string)e.NewValue;
        });
    }

    private string _Text;

    // 鼠标按下接口
    private bool isMouseDown;

    // 修复 WPF Bug：下拉框文本修改后，依然误认为还选择着此前的选项，导致再次点击该选项时内容不变
    private bool isTextChanging;
    private double realWidth; // 由于下拉框 Popup 宽度与 Width 一致，故不能为 NaN（Auto）
    private MyTextBox textBox;

    // 基础
    public int Uuid = ModBase.GetUuid();

    public MyComboBox()
    {
        _Text = SelectedItem?.ToString() ?? "";
        // [port] WPF Preview(MouseDown/MouseUp) 隧道事件 → Avalonia 用 AddHandler(Pointer* , Tunnel, handledEventsToo:true) 捕获已处理事件
        AddHandler(InputElement.PointerPressedEvent, new EventHandler<PointerPressedEventArgs>(MyComboBox_PreviewPointerPressed), RoutingStrategies.Tunnel, true);
        AddHandler(InputElement.PointerReleasedEvent, new EventHandler<PointerReleasedEventArgs>(MyComboBox_PreviewPointerReleased), RoutingStrategies.Tunnel, true);
        PointerExited += MyComboBox_PreviewPointerReleased;
        this.PropertyChanged += (_, e) => { if (e.Property == IsEnabledProperty) RefreshColor(); };
        PointerEntered += (_, _) => RefreshColor();
        PointerExited += (_, _) => RefreshColor();
        AddHandler(InputElement.PointerPressedEvent, new EventHandler<PointerPressedEventArgs>((_, _) => RefreshColor()), RoutingStrategies.Tunnel, true);
        AddHandler(InputElement.PointerReleasedEvent, new EventHandler<PointerReleasedEventArgs>((_, _) => RefreshColor()), RoutingStrategies.Tunnel, true);
        // [port] WPF GotKeyboardFocus → Avalonia GotFocus（冒泡）
        GotFocus += (_, _) => RefreshColor();
        DropDownOpened += MyComboBox_DropDownOpened;
        DropDownClosed += MyComboBox_DropDownClosed;
        TextChanged += MyComboBox_TextChanged;
    }

    public string HintText
    {
        get => (string)GetValue(HintTextProperty);
        set => SetValue(HintTextProperty, value);
    }

    public new string Text
    {
        get
        {
            if (IsEditable)
            {
                if (textBox is null) return _Text ?? "";
                return textBox.Text ?? "";
            }

            return (SelectedItem ?? "").ToString();
        }
        set
        {
            if (IsEditable)
            {
                if (textBox is null)
                    _Text = value;
                else
                    textBox.Text = value;
            }
            else
            {
                throw new NotSupportedException("该 ComboBox 不支持修改文本。");
            }
        }
    }

    public bool DropDownWidthSync { get; set; } = true;

    // [port] 模板名称作用域缓存。
    // Stubs 里的 Template.FindName 垫片是 control.FindNameScope()?.Find(name)，
    // 模板刚应用时可能落到外层（页面）的名称作用域，取不到模板内元素并静默返回 null。
    // 后果：textBox 为 null → OnApplyTemplate 里 textBox.LostFocus 直接 NullReferenceException
    //       （日志表现为"初始化可编辑文本框失败（TextArgumentTitle）"，随后还会连带触发弹窗崩溃）。
    // 故在 TemplateApplied 时用模板自身的名称作用域取部件。
    private INameScope? _templateNameScope;

    public ContentPresenter ContentPresenter =>
        (_templateNameScope?.Find("PART_Content") as ContentPresenter)
        ?? (ContentPresenter)Template.FindName("PART_Content", this);

    public event TextChangedEventHandler? TextChanged;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _templateNameScope = e.NameScope;
        if (!IsEditable)
            return;
        try
        {
            textBox = e.NameScope.Find<MyTextBox>("PART_EditableTextBox");
            if (textBox is null)
            {
                ModBase.Log("[Control] MyComboBox 模板缺少 PART_EditableTextBox，无法初始化可编辑文本框",
                    ModBase.LogLevel.Developer);
                return;
            }

            // [port] WPF AddHandler(LostFocusEvent, RoutedEventHandler) → 直接挂冒泡 LostFocus 事件
            textBox.LostFocus += (_, _) => RefreshColor();
            textBox.changedEventList.Add((sender, e) => TextChanged?.Invoke(sender, (TextChangedEventArgs)e));
            textBox.Tag = Tag; // 有时需要用文本框的 Tag 来写入设置
            if (string.IsNullOrEmpty(Text))
                textBox.Text = _Text;
            else
                TextChanged?.Invoke(this, null);
            if (HintText.Length > 0)
                textBox.HintText = HintText;
            textBox.SetResourceReference(TextBox.CaretBrushProperty, "ColorBrushGray1");
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "初始化可编辑文本框失败（" + (Name ?? "") + "）",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Application.Control.Error.OperationFailed"));
        }
    }

    private void MyComboBox_PreviewPointerPressed(object sender, PointerPressedEventArgs e)
    {
        isMouseDown = true;
    }

    private void MyComboBox_PreviewPointerReleased(object sender, EventArgs e)
    {
        isMouseDown = false;
    }

    // 指向动画
    public void RefreshColor()
    {
        // 判断当前颜色
        string foreColorName;
        string backColorName;
        int time;
        if (IsEnabled)
        {
            if (isMouseDown || IsDropDownOpen ||
                (IsEditable && textBox is not null && textBox.IsFocused))
            {
                foreColorName = "ColorBrush3";
                backColorName = "ColorBrush7";
                time = 10;
            }
            else if (IsPointerOver)
            {
                foreColorName = "ColorBrush4";
                backColorName = "ColorBrush7";
                time = 100;
            }
            else
            {
                foreColorName = "ColorBrushBg0";
                backColorName = "ColorBrushHalfWhite";
                time = 100;
            }
        }
        else
        {
            foreColorName = "ColorBrushGray5";
            backColorName = "ColorBrushGray6";
            time = 200;
        }

        // 触发颜色动画
        if (IsLoaded && ModAnimation.AniControlEnabled == 0) // 防止默认属性变更触发动画
        {
            // 有动画
            ModAnimation.AniStart(
                new[]
                {
                    ModAnimation.AaColor(this, ForegroundProperty, foreColorName, time),
                    ModAnimation.AaColor(this, BackgroundProperty, backColorName, time)
                }, "MyComboBox Color " + Uuid);
        }
        else
        {
            // 无动画
            ModAnimation.AniStop("MyComboBox Color " + Uuid);
            this.SetResourceReference(ForegroundProperty, foreColorName);
            this.SetResourceReference(BackgroundProperty, backColorName);
        }
    }

    private void MyComboBox_DropDownOpened(object sender, EventArgs e)
    {
        realWidth = Width;
        if (DropDownWidthSync)
            Width = Bounds.Width;
        try
        {
            // [port] 同上：改用模板自身的名称作用域取部件
            var popup = _templateNameScope?.Find("PanPopup") as Grid
                        ?? (Grid)Template.FindName("PanPopup", this);
            if (popup is null)
                return;

            popup.Opacity = ModMain.frmMain.Opacity;
            if (!DropDownWidthSync)
                popup.MinWidth = Bounds.Width;
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "设置下拉框属性失败",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Application.Control.Error.OperationFailed"));
        }
    }

    private void MyComboBox_DropDownClosed(object sender, EventArgs e)
    {
        Width = realWidth;
    }

    private void MyComboBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (isTextChanging || !IsEditable)
            return;
        if (SelectedItem is null || Text == SelectedItem.ToString()) return;
        {
            var rawText = Text;
            var rawSelectionStart = textBox.SelectionStart;
            isTextChanging = true;
            SelectedItem = null;
            Text = rawText;
            textBox.SelectionStart = rawSelectionStart;
            isTextChanging = false;
        }
    }

    // 用于 ItemsSource 的自定义容器
    // [port] WPF GetContainerForItemOverride → Avalonia 12 CreateContainerForItemOverride(item, index, recycleKey)
    protected override Control CreateContainerForItemOverride(object item, int index, object recycleKey)
    {
        return new MyComboBoxItem();
    }

    private void MyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded && ModAnimation.AniControlEnabled == 0) ModMain.RaiseCustomEvent(this);
    }

    // [port] WPF IsItemItsOwnContainerOverride → Avalonia 12 NeedsContainerOverride（false = item 自身即容器）
    protected override bool NeedsContainerOverride(object item, int index, out object recycleKey)
    {
        if (item is MyComboBoxItem)
        {
            recycleKey = null;
            return false;
        }
        return base.NeedsContainerOverride(item, index, out recycleKey);
    }

    // [port] WPF SelectedValuePath（属性路径字符串）→ Avalonia 12 用 SelectedValueBinding（IBinding）。
    // 设置该路径后，SelectedValue 即取选中项（MyComboBoxItem）的该属性，保持原逻辑。
    private string? selectedValuePath;
    public string? SelectedValuePath
    {
        get => selectedValuePath;
        set { selectedValuePath = value; SelectedValueBinding = value is null ? null : new Avalonia.Data.Binding(value); }
    }
}
