using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Threading;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.Utils.OS;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageSetupLaunch
{
    private bool isLoad;

    public PageSetupLaunch()
    {
        Loaded += PageSetupLaunch_Loaded;
        InitializeComponent();
    }

    private void PageSetupLaunch_Loaded(object sender, RoutedEventArgs e)
    {
        // 闁插秴顦查崝鐘烘祰闁劌鍨?
        PanBack.ScrollToHome();
        RefreshRam(false);
        if (ModInstanceList.McMcInstanceSelected is null)
            BtnSwitch.IsVisible = false;
        else
            BtnSwitch.IsVisible = true;

        // 闂堢偤鍣告径宥呭鏉炰粙鍎撮崚?
        if (isLoad)
            return;
        isLoad = true;

        ModAnimation.AniControlEnabled += 1;
        Reload();
        ModAnimation.AniControlEnabled -= 1;

        // 閸愬懎鐡ㄩ懛顏勫З閸掗攱鏌?
        var timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 1) };
        timer.Tick += (_, _) => RefreshRam();
        timer.Start();
        RectRamGame.SizeChanged += (s, e) => RefreshRamText();
    }

    public void Reload()
    {
        try
        {
            // 閸氼垰濮╅崣鍌涙殶
            TextArgumentTitle.Text = Config.Launch.Title;
            TextArgumentInfo.Text = Config.Launch.TypeInfo;
            ComboArgumentIndieV2.SelectedIndex = Config.Launch.IndieSolutionV2;
            ComboArgumentVisibie.SelectedIndex = (int)Config.Launch.LauncherVisibility;
            ComboArgumentPriority.SelectedValue = ((int)Config.Launch.ProcessPriority).ToString();
            ComboArgumentWindowType.SelectedIndex = (int)Config.Launch.GameWindowMode;
            TextArgumentWindowWidth.Text = Config.Launch.GameWindowWidth.ToString();
            TextArgumentWindowHeight.Text = Config.Launch.GameWindowHeight.ToString();
            ComboMsAuthType.SelectedIndex = Config.Launch.LoginMsAuthType;
            ComboPreferredIpStack.SelectedIndex = (int)Config.Launch.PreferredIpStack;
            WindowTypeUIRefresh();

            // 濞撳憡鍨欓崘鍛摠
            ((MyRadioBox)FindName("RadioRamType" + Config.Launch.MemoryAllocationMode)).Checked = true;
            SliderRamCustom.Value = Config.Launch.CustomMemorySize;
            RamType(Config.Launch.MemoryAllocationMode);

            // 妤傛楠囩拋鍓х枂
            ComboAdvanceRenderer.SelectedIndex = Config.Launch.Renderer;
            TextAdvanceJvm.Text = Config.Launch.JvmArgs;
            TextAdvanceGame.Text = Config.Launch.GameArgs;
            TextAdvanceRun.Text = Config.Launch.PreLaunchCommand;
            CheckAdvanceRunWait.Checked = Config.Launch.PreLaunchCommandWait;
            CheckAdvanceDisableLF.Checked = Config.Launch.DisableLF;
            CheckAdvanceGraphicCard.Checked = Config.Launch.SetGpuPreference;
            CheckAdvanceNoJavaw.Checked = Config.Launch.NoJavaw;
            CheckAdvanceDisableLwjglUnsafeAgent.Checked = Config.Launch.DisableLwjglUnsafeAgent;
            CheckAdvanceDisableCrashAnalysis.Checked = Config.Launch.DisableCrashAnalysis;
            CheckAdvanceLockMemory.Checked = Config.Launch.LockMemory;
            if (SystemInfo.IsArm64System)
            {
                CheckAdvanceDisableJLW.Checked = true;
                CheckAdvanceDisableJLW.IsEnabled = false;
                CheckAdvanceDisableJLW.ToolTip = Lang.Text("Setup.Launch.Advanced.DisableJlw.Arm64Notice");
            }
            else
            {
                CheckAdvanceDisableJLW.Checked = Config.Launch.DisableJlw;
            }
        }

        catch (NullReferenceException ex)
        {
            ModBase.Log(
                ex,
                Lang.Text("Setup.Launch.Error.ConfigReset"),
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Setup.Launch.Error.ConfigReset"));
            Reset();
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                Lang.Text("Setup.Launch.Error.LoadFailed"),
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Setup.Launch.Error.LoadFailed"));
        }
    }

    // 閸掓繂顫愰崠?
    public void Reset()
    {
        try
        {
            Config.Launch.Reset();
            ModBase.Log("[Setup] 瀹告彃鍨垫慨瀣閸氼垰濮╃拋鍓х枂");
            HintService.Hint(Lang.Text("Setup.Launch.Initialized"), HintType.Success, false);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                Lang.Text("Setup.Launch.Error.InitFailed"),
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Setup.Launch.Error.InitFailed"));
        }

        Reload();
    }

    // 鐏忓棙甯舵禒鑸垫暭閸欐鐭鹃悽鍗炲煂鐠佸墽鐤嗛弨鐟板綁
    private void RadioBoxChange(object senderRaw, ModBase.RouteEventArgs e)
    {
        var sender = (MyRadioBox)senderRaw;
        var gotCfg = sender.Tag?.ToString()?.Split("/") ?? Array.Empty<string>();
        if (ModAnimation.AniControlEnabled == 0 && gotCfg.Length >= 2)
            SetByTag(gotCfg[0], int.Parse(gotCfg[1]));
    }

    private void TextBoxChange(object senderRaw, RoutedEventArgs e)
    {
        var sender = (MyTextBox)senderRaw;
        if (ModAnimation.AniControlEnabled == 0)
            SetByTag(sender.Tag?.ToString(), sender.Text);
    }

    private void TextArgumentTitle_OnTextChanged(object senderRaw, TextChangedEventArgs e)
    {
        var sender = (MyTextBox)senderRaw;
        if (ModAnimation.AniControlEnabled == 0)
            SetByTag(sender.Tag?.ToString(), sender.Text);
    }

    private void SliderChange(object senderRaw, bool user)
    {
        var sender = (MySlider)senderRaw;
        if (ModAnimation.AniControlEnabled == 0)
            SetByTag(sender.Tag?.ToString(), sender.Value);
    }

    private void ComboChange(object senderRaw, SelectionChangedEventArgs e)
    {
        var sender = (MyComboBox)senderRaw;
        if (ModAnimation.AniControlEnabled == 0)
        {
            var senderTag = sender.Tag?.ToString();
            SetByTag(senderTag,
                senderTag == "LaunchArgumentPriority" ? Convert.ToInt32(sender.SelectedValue) : sender.SelectedIndex);
            if (senderTag == "LaunchArgumentWindowType") WindowTypeUIRefresh();
        }
    }

    private void CheckBoxChange(object senderRaw, bool user)
    {
        var sender = (MyCheckBox)senderRaw;
        if (ModAnimation.AniControlEnabled == 0)
            SetByTag(sender.Tag?.ToString(), sender.Checked);
    }

    private static void SetByTag(string tag, object value)
        => ConfigService.TrySetValue(tag, value);

    // 閸掑洦宕查崚鏉跨杽娓氬瀚粩瀣啎缂?
    private void BtnSwitch_Click(object sender, PointerReleasedEventArgs e)
    {
        ModInstanceList.McMcInstanceSelected.Load();
        PageInstanceLeft.McInstance = ModInstanceList.McMcInstanceSelected;
        ModMain.frmMain.PageChange(FormMain.PageType.InstanceSetup, FormMain.PageSubType.VersionSetup);
    }

    private void ComboAdvanceRenderer_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ComboChange(sender, e);
        ComboAdvanceRenderer_SelectionChanged((MyComboBox)sender, e);
    }

    private void ComboArgumentIndie_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ComboChange(sender, e);
        ComboArgumentIndie_SelectionChanged(sender, e);
    }

    #region 濞撳憡鍨欓崘鍛摠

    public void RamType(int type)
    {
        if (SliderRamCustom is null)
            return;
        SliderRamCustom.IsEnabled = type == 1;
    }

    /// <summary>
    ///     閸掗攱鏌?UI 娑撳﹦娈?RAM 閺勫墽銇氶妴?
    /// </summary>
    public void RefreshRam(bool showAnim)
    {
        if (LabRamGame is null || LabRamUsed is null || ModMain.frmMain.pageCurrent != FormMain.PageType.Setup ||
            ModMain.frmSetupLeft.pageID != FormMain.PageSubType.SetupLaunch)
            return;
        // 閼惧嘲褰囬崘鍛摠閹懎鍠?
        var ramGame = Math.Round(GetRam(ModInstanceList.McMcInstanceSelected, false), 5);
        var phyRam = KernelInterop.GetPhysicalMemoryBytes();
        var ramTotal = Math.Round((double)phyRam.Total / 1024 / 1024 / 1024, 1);
        var ramAvailable = Math.Round((double)phyRam.Available / 1024 / 1024 / 1024, 1);
        var ramGameActual = Math.Round(Math.Min(ramGame, ramAvailable), 5);
        var ramUsed = Math.Round(ramTotal - ramAvailable, 5);
        var ramEmpty = Math.Round(ModBase.MathClamp(ramTotal - ramUsed - ramGame, 0d, 1000d), 1);
        // 鐠佸墽鐤嗛張鈧径褍褰查悽銊ュ敶鐎?
        if (ramTotal <= 1.5d)
            SliderRamCustom.MaxValue = (int)Math.Round(Math.Max(Math.Floor((ramTotal - 0.3d) / 0.1d), 1d));
        else if (ramTotal <= 8d)
            SliderRamCustom.MaxValue = (int)Math.Round(Math.Floor((ramTotal - 1.5d) / 0.5d) + 12d);
        else if (ramTotal <= 16d)
            SliderRamCustom.MaxValue = (int)Math.Round(Math.Floor((ramTotal - 8d) / 1d) + 25d);
        else
            SliderRamCustom.MaxValue = (int)Math.Round(Math.Floor((ramTotal - 16d) / 2d) + 33d);
        // 鐠佸墽鐤嗛弬鍥ㄦ拱
        LabRamGame.Text = $"{Lang.Number(ramGame, "N1")} GiB{(ramGame != ramGameActual ? $" ({Lang.Text("Setup.Launch.Memory.AvailableSuffix", Lang.Number(ramGameActual, "N1"))})" : "")}";
        LabRamUsed.Text = $"{Lang.Number(ramUsed, "N1")} GiB";
        LabRamTotal.Text = $" / {Lang.Number(ramTotal, "N1")} GiB";
        LabRamWarn.IsVisible =
            ramGame == 1d && !ModJava.IsGameSet64BitJava() && !SystemInfo.Is32BitSystem && ModJava.Javas.ExistAnyJava()
                ? true
                : false;
        HintRamTooHigh.IsVisible = ramGame / ramTotal > 0.75d ? true : false;
        if (showAnim)
        {
            // 鐎硅棄瀹抽崝銊ф暰
            ModAnimation.AniStart(
                new[]
                {
                    ModAnimation.AaGridLengthWidth(ColumnRamUsed, ramUsed - ColumnRamUsed.Width.Value, 800,
                        ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong)),
                    ModAnimation.AaGridLengthWidth(ColumnRamGame, ramGameActual - ColumnRamGame.Width.Value, 800,
                        ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong)),
                    ModAnimation.AaGridLengthWidth(ColumnRamEmpty, ramEmpty - ColumnRamEmpty.Width.Value, 800,
                        ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong))
                }, "SetupLaunch Ram Grid");
        }
        else
        {
            // 鐎硅棄瀹崇拋鍓х枂
            ColumnRamUsed.Width = new GridLength(ramUsed, GridUnitType.Star);
            ColumnRamGame.Width = new GridLength(ramGameActual, GridUnitType.Star);
            ColumnRamEmpty.Width = new GridLength(ramEmpty, GridUnitType.Star);
        }
    }

    private void RefreshRam()
    {
        RefreshRam(true);
    }

    private int ramTextLeft = 2;
    private int ramTextRight = 1;

    /// <summary>
    ///     閸掗攱鏌?UI 娑撳﹦娈戦弬鍥ㄦ拱娴ｅ秶鐤嗛妴?
    /// </summary>
    private void RefreshRamText()
    {
        // 閼惧嘲褰囩€硅棄瀹虫穱鈩冧紖
        var rectUsedWidth = RectRamUsed.Bounds.Width;
        var totalWidth = PanRamDisplay.Bounds.Width;
        var labGameWidth = LabRamGame.Bounds.Width;
        var labUsedWidth = LabRamUsed.Bounds.Width;
        var labTotalWidth = LabRamTotal.Bounds.Width;
        var labGameTitleWidth = LabRamGameTitle.Bounds.Width;
        var labUsedTitleWidth = LabRamUsedTitle.Bounds.Width;
        // 瀹革缚鏅?
        int left;
        if (rectUsedWidth - 30d < labUsedWidth || rectUsedWidth - 30d < labUsedTitleWidth)
            // 閸忋劌鍟撴稉宥勭瑓娴?
            left = 0;
        else if (rectUsedWidth - 25d < labUsedWidth + labTotalWidth)
            // 閺勫墽銇氭稉宥勭瑓鐎瑰本鏆ｉ弫鐗堝祦
            left = 1;
        else
            // 濮濓絽鐖?
            left = 2;
        if (ramTextLeft != left)
        {
            ramTextLeft = left;
            switch (left)
            {
                case 0:
                {
                    ModAnimation.AniStart(
                        new[]
                        {
                            ModAnimation.AaOpacity(LabRamUsed, -LabRamUsed.Opacity, 100),
                            ModAnimation.AaOpacity(LabRamTotal, -LabRamTotal.Opacity, 100),
                            ModAnimation.AaOpacity(LabRamUsedTitle, -LabRamUsedTitle.Opacity, 100)
                        }, "SetupLaunch Ram TextLeft");
                    break;
                }
                case 1:
                {
                    ModAnimation.AniStart(
                        new[]
                        {
                            ModAnimation.AaOpacity(LabRamUsed, 1d - LabRamUsed.Opacity, 100),
                            ModAnimation.AaOpacity(LabRamTotal, -LabRamTotal.Opacity, 100),
                            ModAnimation.AaOpacity(LabRamUsedTitle, 0.7d - LabRamUsedTitle.Opacity, 100)
                        }, "SetupLaunch Ram TextLeft");
                    break;
                }
                case 2:
                {
                    ModAnimation.AniStart(
                        new[]
                        {
                            ModAnimation.AaOpacity(LabRamUsed, 1d - LabRamUsed.Opacity, 100),
                            ModAnimation.AaOpacity(LabRamTotal, 1d - LabRamTotal.Opacity, 100),
                            ModAnimation.AaOpacity(LabRamUsedTitle, 0.7d - LabRamUsedTitle.Opacity, 100)
                        }, "SetupLaunch Ram TextLeft");
                    break;
                }
            }
        }

        // 閸欏厖鏅?
        int right;
        if (totalWidth < labGameWidth + 2d + rectUsedWidth || totalWidth < labGameTitleWidth + 2d + rectUsedWidth)
            // 閹搞倕鍩岄張鈧崣瀹犵珶
            right = 0;
        else
            // 濮濓絽鐖堕幆鍛枌
            right = 1;
        if (right == 0)
        {
            if (ModAnimation.AniControlEnabled == 0 &&
                (ramTextRight != right || ModAnimation.AniIsRun("SetupLaunch Ram TextRight")))
            {
                // 闂団偓鐟曚礁濮╅悽?
                ModAnimation.AniStart(
                    new[]
                    {
                        ModAnimation.AaX(LabRamGame, totalWidth - labGameWidth - LabRamGame.Margin.Left, 100,
                            ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)),
                        ModAnimation.AaX(LabRamGameTitle, totalWidth - labGameTitleWidth - LabRamGameTitle.Margin.Left,
                            100, ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak))
                    }, "SetupLaunch Ram TextRight");
            }
            else
            {
                // 娑撳秹娓剁憰浣稿З閻?
                ModAnimation.AniStop("SetupLaunch Ram TextRight");
                LabRamGame.Margin = new Thickness(totalWidth - labGameWidth, 3d, 0d, 0d);
                LabRamGameTitle.Margin = new Thickness(totalWidth - labGameTitleWidth, 0d, 0d, 5d);
            }
        }
        else if (ModAnimation.AniControlEnabled == 0 &&
                 (ramTextRight != right || ModAnimation.AniIsRun("SetupLaunch Ram TextRight")))
        {
            // 闂団偓鐟曚礁濮╅悽?
            ModAnimation.AniStart(
                new[]
                {
                    ModAnimation.AaX(LabRamGame, 2d + rectUsedWidth - LabRamGame.Margin.Left, 100,
                        ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)),
                    ModAnimation.AaX(LabRamGameTitle, 2d + rectUsedWidth - LabRamGameTitle.Margin.Left, 100,
                        ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak))
                }, "SetupLaunch Ram TextRight");
        }
        else
        {
            // 娑撳秹娓剁憰浣稿З閻?
            ModAnimation.AniStop("SetupLaunch Ram TextRight");
            LabRamGame.Margin = new Thickness(2d + rectUsedWidth, 3d, 0d, 0d);
            LabRamGameTitle.Margin = new Thickness(2d + rectUsedWidth, 0d, 0d, 5d);
        }

        ramTextRight = right;
    }

    /// <summary>
    ///     閼惧嘲褰囪ぐ鎾冲鐠佸墽鐤嗛惃?RAM 閸婄鈧倸宕熸担宥勮礋 GB閵?
    /// </summary>
    public static double GetRam(McInstance version, bool useVersionJavaSetup, bool? is32BitJava = default)
    {
        // ------------------------------------------
        // 娣囶喗鏁兼稉瀣煙娴狅絿鐖滈弮鍫曟付鐟曚椒绔撮獮鏈垫叏閺€?PageInstanceSetup
        // ------------------------------------------

        var ramGive = default(double);
        if (Config.Launch.MemoryAllocationMode == 0)
        {
            // 閼奉亜濮╅柊宥囩枂
            var ramAvailable =
                Math.Round((double)KernelInterop.GetAvailablePhysicalMemoryBytes() / 1024 / 1024 / 1024 * 10) / 10;
            // 绾喖鐣鹃棁鈧Ч鍌滄畱閸愬懎鐡ㄩ崐?
            double ramMininum; // 閺冪姾顔戞俊鍌欑秿娑旂喖娓剁憰浣风箽鐠囦胶娈戦張鈧担搴ㄦ鎼达箑鍞寸€?
            double ramTarget1; // 娴兼媽顓搁懗钘夊瀵搫鐢崝銊ょ啊閻ㄥ嫬鍞寸€?
            double ramTarget2; // 娴兼媽顓稿▽鈥虫殣闂傤噣顣芥禍鍡欐畱閸愬懎鐡?
            double ramTarget3; // 閺€鍙ョ閻у彞绔炬稉顏呮綏鐠愩劌鎷?Mod 閸滃苯鍘滆ぐ閬嶆付鐟曚胶娈戦崘鍛摠
            if (version is not null && !version.IsLoaded)
                version.Load();
            if (version is not null && version.Modable)
            {
                // 閸欘垰鐣ㄧ憗?Mod 閻ㄥ嫬鐤勬笟?
                var modDir = new DirectoryInfo(version.PathIndie + @"mods\");
                var modCount = modDir.Exists ? modDir.GetFiles().Length : 0;
                ramMininum = 0.5d + modCount / 150d;
                ramTarget1 = 1.5d + modCount / 90d;
                ramTarget2 = 2.7d + modCount / 50d;
                ramTarget3 = 4.5d + modCount / 25d;
            }
            else if (version is not null && version.Info.HasOptiFine)
            {
                // OptiFine 鐎圭偘绶?
                ramMininum = 0.5d;
                ramTarget1 = 1.5d;
                ramTarget2 = 3d;
                ramTarget3 = 5d;
            }
            else
            {
                // 閺咁噣鈧艾鐤勬笟?
                ramMininum = 0.5d;
                ramTarget1 = 1.5d;
                ramTarget2 = 2.5d;
                ramTarget3 = 4d;
            }

            var ramStages = new[]
            {
                (Delta: ramTarget1, Ratio: 1d),
                (Delta: ramTarget2 - ramTarget1, Ratio: 0.7d),
                (Delta: ramTarget3 - ramTarget2, Ratio: 0.4d),
                (Delta: ramTarget3, Ratio: 0.15d)
            };
            foreach (var (RamDelta, RamRatio) in ramStages)
            {
                ramGive += Math.Min(ramAvailable * RamRatio, RamDelta);
                ramAvailable -= RamDelta / RamRatio;
                if (ramAvailable < 0.1d)
                    break;
            }

            // 娑撳秳缍嗘禍搴㈡付娴ｅ骸鈧?
            ramGive = Math.Round(Math.Max(ramGive, ramMininum), 1);
        }
        else
        {
            // 閹靛濮╅柊宥囩枂
            var value = Config.Launch.CustomMemorySize;
            ramGive = value switch
            {
                <= 12 => value * 0.1d + 0.3d,
                <= 25 => (value - 12) * 0.5d + 1.5d,
                <= 33 => (value - 25) * 1 + 8,
                _ => (value - 33) * 2 + 16
            };
        }

        // 閼汇儰濞囬悽?32 娴?Java閿涘苯鍨梽鎰煑娑?1G
        if (is32BitJava ?? !ModJava.IsGameSet64BitJava(useVersionJavaSetup ? version : null))
            ramGive = Math.Min(1d, ramGive);
        return ramGive;
    }

    #endregion

    #region 閸忔湹绮柅澶愩€?

    private void WindowTypeUIRefresh()
    {
        if (ComboArgumentWindowType is null)
            return;
        if (ComboArgumentWindowType.SelectedIndex == 3 && LabArgumentWindowMiddle is not null &&
            LabArgumentWindowMiddle.IsVisible == false)
        {
            LabArgumentWindowMiddle.IsVisible = true;
            TextArgumentWindowHeight.IsVisible = true;
            TextArgumentWindowWidth.IsVisible = true;
        }
        else if (ComboArgumentWindowType.SelectedIndex != 3 && LabArgumentWindowMiddle is not null &&
                 LabArgumentWindowMiddle.IsVisible == true)
        {
            LabArgumentWindowMiddle.IsVisible = false;
            TextArgumentWindowHeight.IsVisible = false;
            TextArgumentWindowWidth.IsVisible = false;
        }
    }

    // 閸欘垵顫嗛幀褔鈧瀚ㄩ惄瀛樺复閸忔娊妫撮惃鍕劅閸?
    private void ComboArgumentVisibie_SelectionChanged(object sender, SelectionChangedEventArgs sizeChangedEventArgs)
    {
        ComboChange(sender, sizeChangedEventArgs);
        if (ModAnimation.AniControlEnabled != 0)
            return;
        if (ComboArgumentVisibie.SelectedIndex == 0)
            if (ModMain.MyMsgBox(
                    Lang.Text("Setup.Launch.Options.Visibility.CloseImmediately.Warning.Message"),
                    Lang.Text("Setup.Launch.Options.Visibility.CloseImmediately.Warning.Title"),
                    Lang.Text("Setup.Launch.Options.Visibility.CloseImmediately.Warning.Continue"),
                    Lang.Text("Common.Action.Cancel")) == 2)
                ComboArgumentVisibie.SelectedItem = sizeChangedEventArgs.RemovedItems[0];
    }

    // 鐎圭偘绶ラ梾鏃傤瀲閹绘劗銇?
    private void ComboArgumentIndie_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModAnimation.AniControlEnabled != 0)
            return;
        ModMain.MyMsgBox(Lang.Text("Setup.Launch.Options.InstanceIsolation.DefaultPolicyHint"));
    }

    #endregion

    #region 妤傛楠囩拋鍓х枂

    private void TextAdvanceRun_TextChanged(object sender, TextChangedEventArgs e)
    {
        CheckAdvanceRunWait.IsVisible =
            string.IsNullOrEmpty(TextAdvanceRun.Text) ? false : true;
    }

    // JVM 閸欏倹鏆熼柌宥堫啎
    private void TextAdvanceJvm_TextChanged(object sender, TextChangedEventArgs e)
    {
        BtnAdvanceJvmReset.IsVisible =
            TextAdvanceJvm.Text == Config.Launch.JvmArgsConfig.DefaultValue
                ? false
                : true;
    }

    private void BtnAdvanceJvmReset_Click(object sender, EventArgs e)
    {
        Config.Launch.JvmArgsConfig.Reset();
        Reload();
    }

    private void ComboAdvanceRenderer_SelectionChanged(MyComboBox sender, object e)
    {
        if (ModAnimation.AniControlEnabled != 0)
            return;
        if (!States.Hint.Renderer && ComboAdvanceRenderer.SelectedIndex != 0)
        {
            if (ModMain.MyMsgBox(Lang.Text("Setup.Launch.Advanced.Renderer.Warning.Message"),
                    Lang.Text("Common.Dialog.Warning"),
                    Lang.Text("Setup.Launch.Advanced.Renderer.Warning.Confirm"),
                    Lang.Text("Common.Action.Cancel"), isWarn: true) == 2)
            {
                ComboAdvanceRenderer.SelectedItem = ((SelectionChangedEventArgs)e).RemovedItems[0];
            }
            else
            {
                Config.Launch.Renderer = sender.SelectedIndex;
                States.Hint.Renderer = true;
            }
        }
        else
        {
            Config.Launch.Renderer = sender.SelectedIndex;
        }
    }

    #endregion
}
