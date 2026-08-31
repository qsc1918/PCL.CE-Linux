using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Input;

namespace PCL;

public partial class MyCompItem : Grid
{
    private string stateLast;

    /// <summary>
    ///     是否允许交互。目前仅用于 PageDownloadCompDetail 的顶部栏展示：若关闭碰撞检测，则无法展开 Tooltip。
    /// </summary>
    public bool CanInteraction { get; set; } = true;

    public void RefreshColor(object sender, EventArgs e)
    {
        if (!CanInteraction)
            return;
        // 判断当前颜色
        string stateNew;
        int time;
        if (IsPointerOver)
        {
            if (isMouseDown)
            {
                stateNew = "MouseDown";
                time = 120;
            }
            else
            {
                stateNew = "MouseOver";
                time = 120;
            }
        }
        else
        {
            stateNew = "Idle";
            time = 180;
        }

        if ((stateLast ?? "") == (stateNew ?? ""))
            return;
        stateLast = stateNew;
        // 触发颜色动画
        if (IsLoaded && ModAnimation.AniControlEnabled == 0) // 防止默认属性变更触发动画
        {
            // 有动画
            var ani = new List<ModAnimation.AniData>();
            if (IsPointerOver)
            {
                if (PanButtons is not null && _HasActionButtons)
                    ani.Add(ModAnimation.AaOpacity(PanButtons, 1d - PanButtons.Opacity, (int)Math.Round(time * 0.35d),
                        (int)Math.Round(time * 0.15d)));
                ani.AddRange(new[]
                {
                    ModAnimation.AaColor(RectBack, Border.BackgroundProperty,
                        isMouseDown ? "ColorBrush6" : "ColorBrushBg1", time),
                    ModAnimation.AaOpacity(RectBack, 1d - RectBack.Opacity, time,
                        ease: new ModAnimation.AniEaseOutFluent())
                });
                if (isMouseDown)
                    ani.Add(ModAnimation.AaScaleTransform(RectBack,
                        0.996d - ((ScaleTransform)RectBack.RenderTransform).ScaleX, (int)Math.Round(time * 1.2d),
                        ease: new ModAnimation.AniEaseOutFluent()));
                else
                    ani.Add(ModAnimation.AaScaleTransform(RectBack,
                        1d - ((ScaleTransform)RectBack.RenderTransform).ScaleX, (int)Math.Round(time * 1.2d),
                        ease: new ModAnimation.AniEaseOutFluent()));
            }
            else
            {
                if (PanButtons is not null && _HasActionButtons)
                    ani.Add(ModAnimation.AaOpacity(PanButtons, -PanButtons.Opacity, (int)Math.Round(time * 0.4d)));
                ani.AddRange(new[]
                {
                    ModAnimation.AaOpacity(RectBack, -RectBack.Opacity, time),
                    ModAnimation.AaColor(RectBack, Border.BackgroundProperty,
                        isMouseDown ? "ColorBrush6" : "ColorBrush7", time),
                    ModAnimation.AaScaleTransform(RectBack, 0.996d - ((ScaleTransform)RectBack.RenderTransform).ScaleX,
                        time, ease: new ModAnimation.AniEaseOutFluent()),
                    ModAnimation.AaScaleTransform(RectBack, -0.196d, 1, after: true)
                });
            }

            ModAnimation.AniStart(ani, "CompItem Color " + Uuid);
        }
        else
        {
            // 无动画
            ModAnimation.AniStop("CompItem Color " + Uuid);
            if (_RectBack is not null)
                RectBack.Opacity = 0d;
            if (PanButtons is not null)
                PanButtons.Opacity = 0d;
        }
    }

    #region 基础属性

    public int Uuid = ModBase.GetUuid();

    // Logo
    public string Logo
    {
        get => PathLogo.Source;
        set => PathLogo.Source = value;
    }

    // 标题
    public string Title
    {
        get => LabTitle.Text;
        set
        {
            if ((LabTitle.Text ?? "") == (value ?? ""))
                return;
            LabTitle.Text = value;
        }
    }

    // 副标题
    public string SubTitle
    {
        get => LabTitleRaw?.Text ?? "";
        set
        {
            if ((LabTitleRaw.Text ?? "") == (value ?? ""))
                return;
            LabTitleRaw.Text = value;
            LabTitleRaw.IsVisible = string.IsNullOrEmpty(value) ? false : true;
        }
    }

    // 描述
    public string Description
    {
        get => LabInfo.Text;
        set
        {
            if ((LabInfo.Text ?? "") == (value ?? ""))
                return;
            LabInfo.Text = value;
        }
    }

    public MyCompItem()
    {
        InitializeComponent();
        Click += (sender, e) => MyCompItem_Click((MyCompItem)sender, e);
        PointerReleased += Button_PointerReleased;
        PointerPressed += Button_MouseDown;
        PointerExited += Button_PointerExited;
        PointerReleased += Button_PointerExited;
        PointerEntered += RefreshColor;
        PointerExited += RefreshColor;
        PointerPressed += RefreshColor;
        PointerReleased += RefreshColor;
        // Handles
        LabInfo.PointerEntered += LabInfo_PointerEntered;
        BtnDelete.Click += BtnDelete_Click;
        BtnDownload.Click += _BtnDownload_Click;
    }

    // 指向时扩展描述
    private void LabInfo_PointerEntered(object sender, PointerEventArgs e)
    {
        if (IsTextTrimmed(LabInfo))
        {
            ToolTipInfo.Content = LabInfo.Text;
            ToolTipInfo.Width = LabInfo.Bounds.Width + 25d;
            Avalonia.Controls.ToolTip.SetTip(LabInfo, ToolTipInfo); // [port] ToolTip -> SetTip
        }
        else
        {
            Avalonia.Controls.ToolTip.SetTip(LabInfo, null); // [port] ToolTip -> SetTip
        }
    }

    private bool IsTextTrimmed(TextBlock textBlock)
    {
        var typeface = new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight,
            textBlock.FontStretch);
        var formattedText = new FormattedText(textBlock.Text, Thread.CurrentThread.CurrentCulture,
            FlowDirection.LeftToRight, typeface, textBlock.FontSize, textBlock.Foreground);
        return formattedText.Width > textBlock.Bounds.Width;
    }

    // Tag
    public List<string> Tags
    {
        set
        {
            PanTags.Children.Clear();
            PanTags.IsVisible = value.Any() ? true : false;
            foreach (var tagText in value)
            {
                var newTag = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(17, 0, 0, 0)),
                    Padding = new Thickness(3d, 1d, 3d, 1d),
                    CornerRadius = new CornerRadius(3d),
                    Margin = new Thickness(0d, 0d, 3d, 0d),
                    UseLayoutRounding = false
                };
                var tagTextBlock = new TextBlock
                {
                    Text = tagText,
                    Foreground = new SolidColorBrush(Color.FromRgb(134, 134, 134)),
                    FontSize = 11d
                };
                newTag.Child = tagTextBlock;
                PanTags.Children.Add(newTag);
            }
        }
    }

    // ‘收藏按钮
    public bool ShowFavoriteBtn
    {
        get => BtnDelete.IsVisible == true;
        set
        {
            BtnDelete.IsVisible = value ? true : false;
            _UpdatePanButtons();
        }
    }

    // 快速下载按钮
    public bool ShowDownloadBtn
    {
        get => BtnDownload.IsVisible == true;
        set
        {
            BtnDownload.IsVisible = value ? true : false;
            _UpdatePanButtons();
        }
    }

    /// <summary>右侧是否存在任意可见的操作按钮（收藏 / 下载），用于决定悬停时是否淡入按钮区。</summary>
    private bool _HasActionButtons => ShowFavoriteBtn || ShowDownloadBtn;

    private void _UpdatePanButtons()
    {
        if (PanButtons is null) return;
        PanButtons.IsVisible = _HasActionButtons ? true : false;
    }

    /// <summary>
    ///     刷新收藏状态（仅更新图标，不影响 hover 展示逻辑）
    /// </summary>
    public void RefreshFavoriteStatus()
    {
        if (Tag is not ModComp.CompProject project) return;

        var isFavourite = ModComp.CompFavorites.IsFavourite(project.Id);
        BtnDelete.SvgIcon = isFavourite ? "lucide/heart-filled" : "lucide/heart";
    }

    #endregion

    #region 点击

    // 触发点击事件
    public event ClickEventHandler? Click;

    public delegate void ClickEventHandler(object sender, PointerEventArgs e);

    private void BtnDelete_Click(object sender, EventArgs e)
    {
        if (PanButtons.Opacity > 0d && Tag is ModComp.CompProject)
        {
            var project = (ModComp.CompProject)Tag;
            ModComp.CompFavorites.ShowMenu(project, (Control)sender, () => RefreshFavoriteStatus());
        }
    }

    private void _BtnDownload_Click(object sender, EventArgs e)
    {
        if (PanButtons.Opacity > 0d && Tag is ModComp.CompProject project)
            ModComp.QuickDownload(project);
    }

    private void MyCompItem_Click(MyCompItem sender, EventArgs e)
    {
        // 记录当前展开的卡片标题（#2712）
        var titles = new List<string>();
        if (ModMain.frmMain.pageCurrent.page == FormMain.PageType.CompDetail)
        {
            foreach (MyCard Card in ModMain.frmDownloadCompDetail.PanResults.Children)
                if (!string.IsNullOrEmpty(Card.Title) && !Card.IsSwapped)
                    titles.Add(Card.Title);
            ModBase.Log("[Comp] 记录当前已展开的卡片：" + string.Join("、", titles));
            var additional = ModMain.frmMain.pageCurrent.additional.Value;
            ModMain.frmMain.pageCurrent.additional = additional with { ExpandedTitles = titles };
        }

        // 打开详情页
        var targetType = default(ModComp.CompType);
        string targetVersion = null;
        var targetLoader = ModComp.CompLoaderType.Any;
        if (ModMain.frmMain.pageCurrent.page == FormMain.PageType.Download)
        {
            if (ModMain.frmMain.PageCurrentSub == FormMain.PageSubType.DownloadCompFavorites)
            {
                targetVersion = "";
                targetLoader = ModComp.CompLoaderType.Any;
            }
            else
            {
                // 从下载页进入
                switch (ModMain.frmMain.PageCurrentSub)
                {
                    case FormMain.PageSubType.DownloadMod:
                    {
                        targetType = ModComp.CompType.Mod;
                        targetVersion = ModMain.frmDownloadMod.Content.loader.input.gameVersion;
                        targetLoader = ModMain.frmDownloadMod.Content.loader.input.modLoader;
                        break;
                    }
                    case FormMain.PageSubType.DownloadPack:
                    {
                        targetType = ModComp.CompType.ModPack;
                        targetVersion = ModMain.frmDownloadPack.Content.loader.input.gameVersion;
                        break;
                    }
                    case FormMain.PageSubType.DownloadDataPack:
                    {
                        targetType = ModComp.CompType.DataPack;
                        targetVersion = ModMain.frmDownloadDataPack.Content.loader.input.gameVersion;
                        break;
                    }
                    case FormMain.PageSubType.DownloadResourcePack:
                    {
                        targetType = ModComp.CompType.ResourcePack;
                        targetVersion = ModMain.frmDownloadResourcePack.Content.loader.input.gameVersion;
                        break;
                    }
                    case FormMain.PageSubType.DownloadShader:
                    {
                        targetType = ModComp.CompType.Shader;
                        targetVersion = ModMain.frmDownloadShader.Content.loader.input.gameVersion;
                        break;
                    }
                    case FormMain.PageSubType.DownloadWorld:
                    {
                        targetType = ModComp.CompType.World;
                        targetVersion = ModMain.frmDownloadWorld.Content.loader.input.gameVersion;
                        break;
                    }
                }
            }
        }
        else if (ModMain.frmMain.pageCurrent.page == FormMain.PageType.InstanceSetup)
        {
            // 从实例设置页进入（查看整合包信息）
            targetType = ModComp.CompType.ModPack;
        }
        else
        {
            // 从详情页进入（查看前置）
            targetType = ModComp.CompType.Any; // 允许任意类别
            var additional = ModMain.frmMain.pageCurrent.additional.Value;
            targetVersion = additional.TargetVersion;
            targetLoader = additional.TargetLoader;
        }

        ModMain.frmMain.PageChange(new FormMain.PageStackData
        {
            page = FormMain.PageType.CompDetail,
            additional = ((ModComp.CompProject)sender.Tag, new List<string>(), targetVersion, targetLoader, targetType, null)
        });
    }

    // 鼠标点击判定
    private bool isMouseDown;

    // 触发点击事件
    private void Button_PointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (!isMouseDown)
            return;
        Click?.Invoke(sender, e);
    }

    private void Button_MouseDown(object sender, PointerPressedEventArgs e)
    {
        if (!CanInteraction)
            return;
        // 检查点击位置是否在按钮区域内
        var clickPosition = e.GetPosition(this);
        var isClickOnButton = false;

        if (PanButtons.IsVisible == true)
            isClickOnButton = _IsClickOnActionButton(BtnDelete, clickPosition) ||
                              _IsClickOnActionButton(BtnDownload, clickPosition);

        // 如果点击在按钮上，不处理主项目点击事件
        if (isClickOnButton) return;

        // 如果点击在其他区域，按原逻辑处理
        // 也要检查是否点击在LabInfo区域（支持ToolTip点击）
        var isClickOnLabInfo = false;
        if (LabInfo.IsVisible == true)
        {
            var labInfoBounds = new Rect(LabInfo.TranslatePoint(new Point(0d, 0d), this), LabInfo.Bounds.Size);
            isClickOnLabInfo = labInfoBounds.Contains(clickPosition);
        }

        if (IsPointerOver || isClickOnLabInfo) isMouseDown = true;
    }

    private void Button_PointerExited(object sender, object e)
    {
        isMouseDown = false;
    }

    // 判断点击是否落在某个操作按钮（收藏 / 下载）上
    private bool _IsClickOnActionButton(Control button, Point clickPosition)
    {
        if (button is null || button.IsVisible != true) return false;
        var bounds = new Rect(button.TranslatePoint(new Point(0d, 0d), this), button.Bounds.Size);
        return bounds.Contains(clickPosition);
    }

    #endregion

    #region 后加载指向背景

    private Border _RectBack;

    public Border RectBack
    {
        get
        {
            if (_RectBack is null)
            {
                var rect = new Border
                {
                    Name = "RectBack",
                    CornerRadius = new CornerRadius(3d),
                    RenderTransform = new ScaleTransform(0.8d, 0.8d),
                    RenderTransformOrigin = new Point(0.5d, 0.5d),
                    BorderThickness = new Thickness(ModBase.GetWPFSize(1d)),
                    IsHitTestVisible = false,
                    Opacity = 0d
                };
                // [port] WPF SetResourceReference → Avalonia 手动解析动态资源（无 SetResourceReference 方法）
                rect.SetValue(Border.BackgroundProperty,
                    this.TryGetResource("ColorBrush7", null, out var bgRes) ? (IBrush)bgRes : null);
                rect.SetValue(Border.BorderBrushProperty,
                    this.TryGetResource("ColorBrush6", null, out var bbRes) ? (IBrush)bbRes : null);
                SetColumnSpan(rect, 999);
                SetRowSpan(rect, 999);
                Children.Insert(0, rect);
                _RectBack = rect;
                // <!--<corelocal:BlurBorder x:Name = "RectBack" CornerRadius="3" RenderTransformOrigin="0.5,0.5" SnapsToDevicePixels="True" 
                // IsHitTestVisible = "False" Opacity="0" BorderThickness="1" 
                // Grid.ColumnSpan = "4" Background="{DynamicResource ColorBrush7}" BorderBrush="{DynamicResource ColorBrush6}"/>-->
            }

            return _RectBack;
        }
    }

    #endregion
}
