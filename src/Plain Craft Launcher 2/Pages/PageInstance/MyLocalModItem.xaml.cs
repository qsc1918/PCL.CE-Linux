using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Microsoft.VisualBasic;
using PCL.Core.App;
using PCL.Core.Utils;
using PCL.Core.Utils.Exts;
using PCL.Core.App.Localization;

namespace PCL;

public partial class MyLocalCompItem
{
    private string GetUpdateCompareDescription()
    {
        var currentName = Entry.compFile.FileName.Replace(".jar", "");
        var newestName = Entry.UpdateFile.FileName.Replace(".jar", "");
        // 缁犫偓閸栨牕鎮曠粔鏉款嚠濮?
        var currentSegs = currentName.Split('-').ToList();
        var newestSegs = newestName.Split('-').ToList();
        var shortened = false;
        foreach (var Seg in currentSegs.ToList())
        {
            if (!newestSegs.Contains(Seg))
                continue;
            currentSegs.Remove(Seg);
            newestSegs.Remove(Seg);
            shortened = true;
        }

        if (shortened && currentSegs.Any() && newestSegs.Any())
        {
            currentName = currentSegs.Join("-");
            newestName = newestSegs.Join("-");
            Entry._Version = currentName; // 娴ｈ法鏁ょ純鎴犵捕娣団剝浼呮担婊€璐熼弰鍓с仛閻ㄥ嫮澧楅張顒€褰?
        }

        return
            Lang.Text("Instance.Resource.Item.UpdateCompare", currentName, Lang.TimeSpan(Entry.compFile.ReleaseDate - DateTime.Now), newestName, Lang.TimeSpan(Entry.UpdateFile.ReleaseDate - DateTime.Now));
    }

    public void Refresh()
    {
        Dispatcher.BeginInvoke(new Func<Task>(async () =>
        {
            // 閺囧瓨鏌?
            if (Entry.CanUpdate)
            {
                BtnUpdate.IsVisible = true;
                BtnUpdate.ToolTip = $"{GetUpdateCompareDescription()}\r\n{Lang.Text("Instance.Resource.Item.UpdateToolTip")}";
            }
            else
            {
                BtnUpdate.IsVisible = false;
            }

            // 閺嶅洭顣芥稉搴㈠伎鏉?
            string descFileName;
            if (Entry.IsFolder)
                // 閺傚洣娆㈡径褰掋€嶉惃鍕濞堝﹤顦╅悶?
                descFileName = Entry.Name;
            else
                switch (Entry.State)
                {
                    case ModLocalComp.LocalCompFile.LocalFileStatus.Fine:
                    {
                        descFileName = ModBase.GetFileNameWithoutExtentionFromPath(Entry.path);
                        break;
                    }
                    case ModLocalComp.LocalCompFile.LocalFileStatus.Disabled:
                    {
                        descFileName =
                            ModBase.GetFileNameWithoutExtentionFromPath(Entry.path.Replace(".disabled", "")
                                .Replace(".old", "")); // McMod.McModState.Unavailable
                        break;
                    }

                    default:
                    {
                        descFileName = ModBase.GetFileNameFromPath(Entry.path);
                        break;
                    }
                }

            string newDescription;
            var compTemp = Entry.Comp;
            if (Entry.IsFolder)
            {
                // 閺傚洣娆㈡径褰掋€嶉惃鍕濞堝﹥妯夌粈?
                Title = Entry.Name;
                newDescription = Entry.Description;
            }
            else if (Config.Download.Comp.UiCompNameSolution == 1)
            {
                // 閺嶅洭顣介弰鍓с仛閺傚洣娆㈤崥宥忕礉鐠囷附鍎忛弰鍓с仛鐠囨垵鎮?
                // 閺嶅洭顣?
                Title = descFileName;
                SubTitle = "";
                // 閹诲繗鍫?
                if (Entry.Comp is null)
                {
                    newDescription = Entry.Name;
                }
                else
                {
                    var titles = await Task.Run(() => compTemp.GetControlTitle(false));
                    newDescription = titles.Key + titles.Value;
                }

                newDescription = newDescription.Replace("  |  ", " / ");
                if (Entry.Version is not null)
                    newDescription += $" ({Entry.Version})";
            }
            else
            {
                // 閺嶅洭顣介弰鍓с仛鐠囨垵鎮曢敍宀冾嚊閹懏妯夌粈鐑樻瀮娴犺泛鎮?
                // 閺嶅洭顣?
                if (Entry.Comp is null)
                {
                    Title = Entry.Name;
                    SubTitle = Entry.Version is null ? "" : "  |  " + Entry.Version;
                }
                else
                {
                    var titles = await Task.Run(() => compTemp.GetControlTitle(false));
                    Title = titles.Key;
                    SubTitle = titles.Value + (Entry.Version is null ? "" : "  |  " + Entry.Version);
                }

                // 閹诲繗鍫?
                newDescription = descFileName;
            }

            if (Entry.Comp is not null)
                newDescription += ": " + Entry.Comp.Description.Replace("\r", "").Replace("\n", "");
            else if (Entry.Description is not null)
                newDescription += ": " + Entry.Description.Replace("\r", "").Replace("\n", "");
            else if (!Entry.IsFileAvailable) newDescription += ": " + Lang.Text("Instance.Resource.Item.InfoUnavailable");
            Description = newDescription;
            if (Checked)
                LabTitle.SetResourceReference(TextBlock.ForegroundProperty,
                    Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine ? "ColorBrush2" : "ColorBrush5");
            else
                LabTitle.SetResourceReference(TextBlock.ForegroundProperty,
                    Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine ? "ColorBrush1" : "ColorBrushGray4");
            // 娑?Logo
            Logo = Entry.GetLogo();

            // 閸ョ偓鐖ｉ崣鍏呯瑓鐟欐帞娈?Logo
            if (Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine)
            {
                if (imgState is not null)
                {
                    Children.Remove(imgState);
                    imgState = null;
                }
            }
            else
            {
                if (imgState is null)
                {
                    imgState = new Image
                    {
                        Width = 20d,
                        Height = 20d,
                        Margin = new Thickness(0d, 0d, -5, -3),
                        IsHitTestVisible = false,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Bottom
                    };
                    RenderOptions.SetBitmapScalingMode(imgState, BitmapScalingMode.HighQuality);
                    SetColumn(imgState, 1);
                    SetRow(imgState, 1);
                    SetRowSpan(imgState, 2);
                    Children.Add(imgState);
                    // <Image x:Name="ImgState" RenderOptions.BitmapScalingMode="HighQuality" Width="16" Height="16" Margin="0,0,-3,-1"
                    // Grid.Column="1" Grid.Row="1" Grid.RowSpan="2" IsHitTestVisible="False"
                    // HorizontalAlignment="Right" VerticalAlignment="Bottom"
                    // Source="/Images/Icons/Unavailable.png" />
                }

                imgState.Source = new MyBitmap(ModBase.pathImage + $"Icons/{Entry.State}.png");
            }

            // 閺嶅洨顒?
            if (Entry.IsFolder)
                // 娑撶儤鏋冩禒璺恒仚濞ｈ濮為弽鍥╊劮
                Tags = new List<string> { Lang.Text("Instance.Resource.Item.FolderTag") };
            else if (Entry.Comp is not null) Tags = Entry.Comp.Tags;
        }));
    }

    public void RefreshColor(object sender, EventArgs e)
    {
        InitLate(sender, e);
        // 鐟欙箑褰傛０婊嗗閸斻劎鏁?
        var time = IsMouseOver ? 120 : 180;
        var ani = new List<ModAnimation.AniData>();
        // ButtonStack
        if (buttonStack is not null)
        {
            if (IsMouseOver)
            {
                ani.Add(ModAnimation.AaOpacity(buttonStack, 1d - buttonStack.Opacity, (int)Math.Round(time * 0.7d),
                    (int)Math.Round(time * 0.3d)));
                ani.Add(ModAnimation.AaDouble(
                    i => ColumnPaddingRight.Width =
                        new GridLength(Math.Max(0, ColumnPaddingRight.Width.Value + (double)i)),
                    5 + Buttons.Count() * 25 - ColumnPaddingRight.Width.Value, (int)Math.Round(time * 0.3d),
                    (int)Math.Round(time * 0.7d)));
            }
            else
            {
                ani.Add(ModAnimation.AaOpacity(buttonStack, -buttonStack.Opacity, (int)Math.Round(time * 0.4d)));
                ani.Add(ModAnimation.AaDouble(
                    i => ColumnPaddingRight.Width =
                        new GridLength(Math.Max(0, ColumnPaddingRight.Width.Value + (double)i)),
                    4d - ColumnPaddingRight.Width.Value, (int)Math.Round(time * 0.4d)));
            }
        }

        // RectBack
        if (IsMouseOver || Checked)
        {
            ani.AddRange(new[]
            {
                ModAnimation.AaColor(RectBack, Border.BackgroundProperty, isMouseDown ? "ColorBrush6" : "ColorBrushBg1",
                    time),
                ModAnimation.AaOpacity(RectBack, 1d - RectBack.Opacity, time, ease: new ModAnimation.AniEaseOutFluent())
            });
            if (isMouseDown)
                ani.Add(ModAnimation.AaScaleTransform(RectBack,
                    0.996d - ((ScaleTransform)RectBack.RenderTransform).ScaleX, (int)Math.Round(time * 1.2d),
                    ease: new ModAnimation.AniEaseOutFluent()));
            else
                ani.Add(ModAnimation.AaScaleTransform(RectBack, 1d - ((ScaleTransform)RectBack.RenderTransform).ScaleX,
                    (int)Math.Round(time * 1.2d), ease: new ModAnimation.AniEaseOutFluent()));
        }
        else
        {
            ani.AddRange(new[]
            {
                ModAnimation.AaOpacity(RectBack, -RectBack.Opacity, time),
                ModAnimation.AaScaleTransform(RectBack, 0.996d - ((ScaleTransform)RectBack.RenderTransform).ScaleX,
                    time, ease: new ModAnimation.AniEaseOutFluent()),
                ModAnimation.AaScaleTransform(RectBack, -0.196d, 1, after: true)
            });
        }

        ModAnimation.AniStart(ani, "LocalModItem Color " + Uuid);
    }

    // 鐟欙箑褰傞搹姘珯閸栨牕鍞寸€?
    private void InitLate(object sender, EventArgs e)
    {
        if (buttonHandler is not null)
        {
            buttonHandler((MyLocalCompItem)sender, e);
            buttonHandler = null;
        }
    }

    // 閺勫墽銇氶弴瀛樻煀閺冦儱绻?
    private void BtnUpdate_PreviewMouseRightButtonUp(object sender, PointerReleasedEventArgs e)
    {
        e.Handled = true;
        ShowUpdateLog();
    }

    private void ShowUpdateLog()
    {
        if (Entry.Comp is not null)
        {
            if (!Information.IsNumeric(Entry.Comp.Id))
            {
                var modrinthUrl = Entry.changelogUrls.FirstOrDefault(x => x.Contains("modrinth.com"));
                if (modrinthUrl is not null)
                {
                    ModBase.OpenWebsite(modrinthUrl);
                    return;
                }
            }
            else
            {
                var curseForgeUrl = Entry.changelogUrls.FirstOrDefault(x => x.Contains("curseforge.com"));
                if (curseForgeUrl is not null)
                {
                    ModBase.OpenWebsite(curseForgeUrl);
                    return;
                }
            }
        }

        ModBase.Log(
            Lang.Text("Instance.Resource.Item.OpenChangelogFailed"),
            ModBase.LogLevel.Hint,
            userSummary: Lang.Text("Instance.Resource.Item.OpenChangelogFailed"));
    }

    // 鐟欙箑褰傞弴瀛樻煀
    private void BtnUpdate_Click(object sender, EventArgs e)
    {
        switch (ModMain.MyMsgBox(
                    $"{Lang.Text("Instance.Resource.Item.UpdateConfirm.Message", Entry.Name)}\r\n\r\n{GetUpdateCompareDescription()}",
                    Lang.Text("Instance.Resource.Item.UpdateConfirm.Title"),
                    Lang.Text("Instance.Resource.Item.Update"), Lang.Text("Instance.Resource.Item.ViewChangelog"), Lang.Text("Common.Action.Cancel")))
        {
            case 1: // 閺囧瓨鏌?
            {
                switch (Entry.Comp.Type)
                {
                    case ModComp.CompType.Mod:
                    {
                        ModMain.frmInstanceMod ??= new PageInstanceCompResource(ModComp.CompType.Mod);
                        ModMain.frmInstanceMod.UpdateResource(new[] { Entry });
                        break;
                    }
                    case ModComp.CompType.ResourcePack:
                    {
                        ModMain.frmInstanceResourcePack ??= new PageInstanceCompResource(ModComp.CompType.ResourcePack);
                        ModMain.frmInstanceResourcePack.UpdateResource(new[] { Entry });
                        break;
                    }
                    case ModComp.CompType.Shader:
                    {
                        ModMain.frmInstanceShader ??= new PageInstanceCompResource(ModComp.CompType.Shader);
                        ModMain.frmInstanceShader.UpdateResource(new[] { Entry });
                        break;
                    }
                    case ModComp.CompType.DataPack:
                    {
                        ModMain.frmInstanceSavesDatapack ??= new PageInstanceSavesDatapack();
                        ModMain.frmInstanceSavesDatapack.UpdateResource(new[] { Entry });
                        break;
                    }
                }

                break;
            }
            case 2: // 閺屻儳婀呴弴瀛樻煀閺冦儱绻?
            {
                ShowUpdateLog();
                break;
            }
            case 3: // 閸欐牗绉?
            {
                break;
            }
        }
    }

    // 閼奉亪鈧倸绨查敍?4465閿?
    private void PanTitle_SizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
    {
        // 0閿涙艾鍙忛柈銊ㄥ灊鐏炴洩绱癆uto - Auto - (Auto) - 1*
        // 1閿涙艾甯囩紓?Subtitle閿涙uto - 1* - (Auto) - 0
        // 2閿涙氨鎴风紒顓炲竾缂?Title閿?* - 0 - (Auto) - 0
        var currentCompressLevel =
            ColumnExtend.Width.IsStar ? 0 : ColumnTitle.Width.IsStar ? 2 : 1; // Subtitle 閸欘垵鍏橀弰?Collapsed
        var newCompressLevel = default(int);
        switch (currentCompressLevel)
        {
            case 0:
            {
                if (ColumnExtend.Bounds.Width < 0.5d)
                    newCompressLevel = LabSubtitle.IsVisible == false ? 2 : 1;
                else
                    return;

                break;
            }
            case 1:
            {
                if (ColumnSubtitle.Bounds.Width < 0.5d)
                    newCompressLevel = 2;
                else if (!LabSubtitle.IsTextTrimmed())
                    newCompressLevel = 0;
                else
                    return;

                break;
            }
            case 2:
            {
                if (!LabTitle.IsTextTrimmed())
                    newCompressLevel = LabSubtitle.IsVisible == false ? 0 : 1;
                else
                    return;

                break;
            }
        }

        switch (newCompressLevel)
        {
            case 0:
            {
                // 閸忋劑鍎撮懜鎺戠潔閿涙uto - Auto - (Auto) - 1*
                ColumnTitle.Width = GridLength.Auto;
                ColumnSubtitle.Width = GridLength.Auto;
                ColumnExtend.Width = new GridLength(1d, GridUnitType.Star);
                break;
            }
            case 1:
            {
                // 閸樺缂?Subtitle閿涙uto - 1* - (Auto) - 0
                ColumnTitle.Width = GridLength.Auto;
                ColumnSubtitle.Width = new GridLength(1d, GridUnitType.Star);
                ColumnExtend.Width = new GridLength(0d, GridUnitType.Pixel);
                break;
            }
            case 2:
            {
                // 缂佈呯敾閸樺缂?Title閿?* - 0 - (Auto) - 0
                ColumnTitle.Width = new GridLength(1d, GridUnitType.Star);
                ColumnSubtitle.Width = new GridLength(0d, GridUnitType.Pixel);
                ColumnExtend.Width = new GridLength(0d, GridUnitType.Pixel);
                break;
            }
        }
    }

    #region 閸╄櫣顢呯仦鐐粹偓?

    public int Uuid = ModBase.GetUuid();

    // Logo
    public string Logo
    {
        get => PathLogo.Source;
        set => PathLogo.Source = value;
    }

    // 閺嶅洭顣?
    public string Title
    {
        get => field;
        set
        {
            var rawValue = value;
            switch (Entry.State)
            {
                case ModLocalComp.LocalCompFile.LocalFileStatus.Fine:
                {
                    LabTitle.TextDecorations = null;
                    break;
                }
                case ModLocalComp.LocalCompFile.LocalFileStatus.Disabled:
                {
                    LabTitle.TextDecorations = TextDecorations.Strikethrough;
                    break;
                }
                case ModLocalComp.LocalCompFile.LocalFileStatus.Unavailable:
                {
                    LabTitle.TextDecorations = TextDecorations.Strikethrough;
                    value += Lang.Text("Instance.Resource.Item.ErrorSuffix");
                    break;
                }
            }

            if ((LabTitle.Text ?? "") == (value ?? ""))
                return;
            LabTitle.Text = value;
            field = rawValue;
        }
    }

    // 閸擃垱鐖ｆ０?
    public string SubTitle
    {
        get => LabSubtitle?.Text ?? "";
        set
        {
            if ((LabSubtitle.Text ?? "") == (value ?? ""))
                return;
            LabSubtitle.Text = value;
            LabSubtitle.IsVisible = string.IsNullOrEmpty(value) ? false : true;
        }
    }

    // 閹诲繗鍫?
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

    // Tag
    public List<string> Tags
    {
        set
        {
            PanTags.Children.Clear();
            PanTags.IsVisible = value.Any() ? true : false;
            foreach (var TagText in value)
            {
                var newTag = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(12, 0, 0, 0)),
                    Padding = new Thickness(3d, 1d, 3d, 1d),
                    CornerRadius = new CornerRadius(3d),
                    Margin = new Thickness(0d, 0d, 3d, 0d),
                    UseLayoutRounding = false
                };
                var tagTextBlock = new TextBlock
                {
                    Text = TagText,
                    Foreground = new SolidColorBrush(ThemeManager.IsDarkMode
                        ? Color.FromArgb(88, 255, 255, 255)
                        : Color.FromArgb(88, 136, 136, 136)),
                    FontSize = 11d
                };
                newTag.Child = tagTextBlock;
                PanTags.Children.Add(newTag);
            }
        }
    }

    // 閻╃鍙ч懕鏃傛畱 Mod
    public ModLocalComp.LocalCompFile Entry
    {
        get => (ModLocalComp.LocalCompFile)Tag;
        set => Tag = value;
    }

    #endregion

    #region 閻愮懓鍤稉搴″瑎闁?

    // 鐟欙箑褰傞悙鐟板毊娴滃娆?
    public event ClickEventHandler? Click;

    public delegate void ClickEventHandler(object sender, PointerReleasedEventArgs e);

    public MyLocalCompItem()
    {
        InitializeComponent();
        PreviewMouseLeftButtonUp += Button_MouseUp;
        PreviewMouseLeftButtonDown += Button_MouseDown;
        MouseLeave += Button_MouseLeave;
        PreviewMouseLeftButtonUp += Button_MouseLeave;
        MouseLeftButtonDown += Button_MouseSwipeStart;
        MouseEnter += Button_MouseSwipe;
        MouseLeave += Button_MouseSwipe;
        MouseLeftButtonUp += Button_MouseSwipe;
        Loaded += (_, _) => Refresh();
        MouseEnter += RefreshColor;
        MouseLeave += RefreshColor;
        MouseLeftButtonDown += RefreshColor;
        MouseLeftButtonUp += RefreshColor;
        Changed += RefreshColor;
        // Handles
        BtnUpdate.PreviewMouseRightButtonUp += BtnUpdate_PreviewMouseRightButtonUp;
        BtnUpdate.Click += BtnUpdate_Click;
        PanTitle.SizeChanged += PanTitle_SizeChanged;
    }

    private void Button_MouseUp(object sender, PointerReleasedEventArgs e)
    {
        if (isMouseDown)
        {
            Click?.Invoke(sender, e);
            if (e.Handled)
                return;
            ModBase.Log("[Control] 閹稿绗呴張顒€婀?Mod 閸掓銆冩い鐧哥窗" + LabTitle.Text);
        }
    }

    // 姒х姵鐖ｉ悙鐟板毊閸掋倕鐣?
    private bool isMouseDown;

    private void Button_MouseDown(object sender, PointerReleasedEventArgs e)
    {
        if (!IsMouseDirectlyOver)
            return;
        isMouseDown = true;
        if (buttonStack is not null)
            buttonStack.IsHitTestVisible = false;
    }

    private void Button_MouseLeave(object sender, object e)
    {
        isMouseDown = false;
        if (buttonStack is not null)
            buttonStack.IsHitTestVisible = true;
    }

    // 濠婃垵濮╅柅澶夎厬
    public class SwipeSelect
    {
        public int Start { get; set; }
        public int End { get; set; }

        public bool Swiping
        {
            get => field;
            set
            {
                field = value;
                if (TargetFrm is not null)
                    try
                    {
                        var cardSelect = Interaction.CallByName(TargetFrm, "CardSelect", CallType.Get);
                        Interaction.CallByName(cardSelect, "IsHitTestVisible", CallType.Set, !value);
                    }
                    catch
                    {
                    }
            }
        }

        public bool SwipeToState { get; set; }
        public object TargetFrm { get; set; }
    }

    public SwipeSelect CurrentSwipe { get; set; }

    private void Button_MouseSwipeStart(object sender, object e)
    {
        if (Parent is null)
            return; // Mod 閸欘垵鍏樺鑼额潶閸掔娀娅庨敍?3824閿?
        // 瀵偓婵绮﹂崝?
        var index = ((StackPanel)Parent).Children.IndexOf(this);
        CurrentSwipe.Start = index;
        CurrentSwipe.End = index;
        CurrentSwipe.Swiping = true;
        CurrentSwipe.SwipeToState = !Checked;
    }

    private void Button_MouseSwipe(object sender, object e)
    {
        if (Parent is null)
            return; // Mod 閸欘垵鍏樺鑼额潶閸掔娀娅庨敍?3824閿?
        // 缂佹挻娼鎴濆З
        if (Mouse.LeftButton != MouseButtonState.Pressed || !(Mouse.DirectlyOver is MyLocalCompItem)) // #5771
        {
            CurrentSwipe.Swiping = false;
            return;
        }

        // 鐠侊紕鐣诲鎴濆З閼煎啫娲?
        var elements = ((StackPanel)Parent).Children;
        var index = elements.IndexOf(this);
        CurrentSwipe.Start =
            (int)Math.Round(ModBase.MathClamp(Math.Min(CurrentSwipe.Start, index), 0d, elements.Count - 1));
        CurrentSwipe.End =
            (int)Math.Round(ModBase.MathClamp(Math.Max(CurrentSwipe.End, index), 0d, elements.Count - 1));
        // 閸曢箖鈧澧嶉張澶庡瘱閸ョ繝鑵戦惃鍕€?
        if (CurrentSwipe.Start == CurrentSwipe.End)
            return;
        for (int i = CurrentSwipe.Start, loopTo = CurrentSwipe.End; i <= loopTo; i++)
        {
            var item = (MyLocalCompItem)elements[i];
            item.InitLate(item, (EventArgs)e);
            item.Checked = CurrentSwipe.SwipeToState;
        }
    }

    // 閸曢箖鈧濮搁幀?
    public event CheckEventHandler? Check;

    public delegate void CheckEventHandler(object sender, ModBase.RouteEventArgs e);

    public event ChangedEventHandler? Changed;

    public delegate void ChangedEventHandler(object sender, ModBase.RouteEventArgs e);

    public bool Checked
    {
        get => field;
        set
        {
            try
            {
                // 鐟欙箑褰傜仦鐐粹偓褍鈧棿鎱ㄩ弨?
                var rawValue = field;
                if (value == field)
                    return;
                field = value;
                var ChangedEventArgs = new ModBase.RouteEventArgs();
                if (IsInitialized)
                {
                    Changed?.Invoke(this, ChangedEventArgs);
                    if (ChangedEventArgs.handled)
                    {
                        field = rawValue;
                        return;
                    }
                }

                if (value)
                {
                    var checkEventArgs = new ModBase.RouteEventArgs();
                    Check?.Invoke(this, checkEventArgs);
                    if (checkEventArgs.handled)
                        return;
                }

                // 閺囧瓨鏁奸崝銊ф暰
                if (this.IsVisibleInWindow(ModMain.frmMain))
                {
                    var anim = new List<ModAnimation.AniData>();
                    if (Checked)
                    {
                        // 閻㈣鲸妫ら崣妯绘箒
                        var delta = 32d - RectCheck.Bounds.Height;
                        anim.Add(ModAnimation.AaHeight(RectCheck, delta * 0.4d, 200,
                            ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)));
                        anim.Add(ModAnimation.AaHeight(RectCheck, delta * 0.6d, 300,
                            ease: new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)));
                        anim.Add(ModAnimation.AaOpacity(RectCheck, 1d - RectCheck.Opacity, 30));
                        RectCheck.VerticalAlignment = VerticalAlignment.Center;
                        RectCheck.Margin = new Thickness(-3, 0d, 0d, 0d);
                        anim.Add(ModAnimation.AaColor(LabTitle, TextBlock.ForegroundProperty,
                            Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine
                                ? "ColorBrush2"
                                : "ColorBrush5", 200));
                    }
                    else
                    {
                        // 閻㈣鲸婀侀崣妯绘￥
                        anim.Add(ModAnimation.AaHeight(RectCheck, -RectCheck.Bounds.Height, 120,
                            ease: new ModAnimation.AniEaseInFluent(ModAnimation.AniEasePower.Weak)));
                        anim.Add(ModAnimation.AaOpacity(RectCheck, -RectCheck.Opacity, 70, 40));
                        RectCheck.VerticalAlignment = VerticalAlignment.Center;
                        anim.Add(ModAnimation.AaColor(LabTitle, TextBlock.ForegroundProperty,
                            LabTitle.TextDecorations is null ? "ColorBrush1" : "ColorBrushGray4", 120));
                    }

                    ModAnimation.AniStart(anim, "MyLocalCompItem Checked " + Uuid);
                }
                else
                {
                    // 娑撳秴婀粣妤€褰涙稉濠冩閻╁瓨甯寸拋鍓х枂
                    RectCheck.VerticalAlignment = VerticalAlignment.Center;
                    RectCheck.Margin = new Thickness(-3, 0d, 0d, 0d);
                    if (Checked)
                    {
                        RectCheck.Height = 32d;
                        RectCheck.Opacity = 1d;
                        LabTitle.SetResourceReference(TextBlock.ForegroundProperty,
                            Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine
                                ? "ColorBrush2"
                                : "ColorBrush5");
                    }
                    else
                    {
                        RectCheck.Height = 0d;
                        RectCheck.Opacity = 0d;
                        LabTitle.SetResourceReference(TextBlock.ForegroundProperty,
                            Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine
                                ? "ColorBrush1"
                                : "ColorBrushGray4");
                    }

                    ModAnimation.AniStop("MyLocalCompItem Checked " + Uuid);
                }
            }
            catch (Exception ex)
            {
                ModBase.Log(ex, "鐠佸墽鐤?Checked 婢惰精瑙?);
            }
        }
    }

    #endregion

    #region 閸氬骸濮炴潪钘夊敶鐎?

    // 閸欏厖绗呯憴鎺斿Ц閹焦瀵氱粈鍝勬禈閺?
    private Image imgState;

    // 閹稿洤鎮滈懗灞炬珯
    public Border RectBack
    {
        get
        {
            if (field is null)
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
                rect.SetResourceReference(Border.BackgroundProperty, "ColorBrush7");
                rect.SetResourceReference(Border.BorderBrushProperty, "ColorBrush6");
                SetColumnSpan(rect, 999);
                SetRowSpan(rect, 999);
                Children.Insert(0, rect);
                field = rect;
                // <!--<corelocal:BlurBorder x:Name = "RectBack" CornerRadius="3" RenderTransformOrigin="0.5,0.5" SnapsToDevicePixels="True" 
                // IsHitTestVisible = "False" Opacity="0" BorderThickness="1" 
                // Grid.ColumnSpan = "4" Background="{DynamicResource ColorBrush7}" BorderBrush="{DynamicResource ColorBrush6}"/>-->
            }

            return field;
        }
    }

    // 閹稿鎸?
    public Action<MyLocalCompItem, EventArgs> buttonHandler;
    public Control buttonStack;
    public IEnumerable<MyIconButton> Buttons
    {
        get => field;
        set
        {
            field = value;
            // 缁夊娅庨崢?Stack
            if (buttonStack is not null)
            {
                Children.Remove(buttonStack);
                buttonStack = null;
            }

            if (!value.Any())
                return;
            // 濞ｈ濮為弬?Stack
            buttonStack = new StackPanel
            {
                Opacity = 0d,
                Margin = new Thickness(0d, 0d, 5d, 0d),
                Orientation = (Orientation)Avalonia.Forms.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                UseLayoutRounding = false
            };
            SetColumnSpan(buttonStack, 10);
            SetRowSpan(buttonStack, 10);
            // 閺嬪嫰鈧姵瀵滈柦?
            foreach (var Btn in value)
            {
                if (Btn.Height.Equals(double.NaN))
                    Btn.Height = 25d;
                if (Btn.Width.Equals(double.NaN))
                    Btn.Width = 25d;
                ((StackPanel)buttonStack).Children.Add(Btn);
            }

            Children.Add(buttonStack);
        }
    }

    // 閸曢箖鈧娼?
    public Border RectCheck
    {
        get
        {
            if (field is null)
            {
                field = new Border
                {
                    Width = 5d,
                    Height = Checked ? double.NaN : 0d,
                    CornerRadius = new CornerRadius(2d, 2d, 2d, 2d),
                    VerticalAlignment = Checked ? VerticalAlignment.Stretch : VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    UseLayoutRounding = false,
                    Margin = Checked ? new Thickness(-3, 6d, 0d, 6d) : new Thickness(-3, 0d, 0d, 0d)
                };
                field.SetResourceReference(Border.BackgroundProperty, "ColorBrush3");
                SetRowSpan(field, 10);
                Children.Add(field);
            }

            return field;
        }
    }

    #endregion
}
