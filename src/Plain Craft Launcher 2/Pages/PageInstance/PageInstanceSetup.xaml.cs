using System.IO;
using System.Text.Json;
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
using PCL.Core.IO;
using PCL.Core.Minecraft;
using PCL.Core.Minecraft.Java.UserPreference;
using PCL.Core.UI;
using PCL.Core.Utils.OS;
using PCL.Core.App.Localization;
using PCL.Core.Utils;

namespace PCL;

public partial class PageInstanceSetup
{
    private new bool isLoaded;

    public PageInstanceSetup()
    {     
        Loaded += PageSetupSystem_Loaded;
        InitializeComponent();

        ComboArgumentIndieV2.SelectionChanged += ComboArgumentIndieV2_SelectionChanged;
        TextArgumentTitle.TextChanged += TextArgumentTitle_TextChanged;
        TextArgumentInfo.TextChanged += TextBoxChange;
        ComboArgumentJava.SelectionChanged += JavaSelectionUpdate;

        RadioRamType2.Check += RadioBoxChange;
        RadioRamType0.Check += RadioBoxChange;
        RadioRamType1.Check += RadioBoxChange;
        SliderRamCustom.Change += SliderChange;

        ComboServerLoginRequire.SelectionChanged += ComboServerLogin_Changed;
        TextServerAuthServer.TextChanged += TextBoxChange;
        TextServerAuthServer.LostFocus += TextServerAuthServer_MouseLeave;
        TextServerAuthRegister.TextChanged += TextBoxChange;
        TextServerAuthName.TextChanged += TextBoxChange;
        TextServerEnter.TextChanged += TextBoxChange;
        BtnServerAuthLittle.Click += BtnServerAuthLittle_Click;
        BtnServerAuthLock.Click += BtnServerAuthLock_Click;
        BtnServerNewProfile.Click += BtnServerNewProfile_Click;

        ComboAdvanceRenderer.SelectionChanged += ComboAdvanceRenderer_SelectionChanged;
        TextAdvanceJvm.TextChanged += TextBoxChange;
        TextAdvanceGame.TextChanged += TextBoxChange;
        TextAdvanceClasspathHead.TextChanged += TextBoxChange;
        TextAdvanceRun.TextChanged += TextAdvanceRun_TextChanged;
        CheckAdvanceRunWait.Change += CheckBoxChange;
        CheckAdvanceJava.Change += CheckBoxChange;
        CheckAdvanceAssetsV2.Change += CheckBoxChange;
        CheckAdvanceUseProxyV2.Change += CheckBoxChange;
        CheckAdvanceDisableJLW.Change += CheckBoxChange;
        CheckAdvanceDisableLF.Change += CheckBoxChange;
        CheckUseDebugLog4j2Config.Change += CheckUseDebugLog4j2Config_CheckChanged;
        CheckAdvanceDisableLwjglUnsafeAgent.Change += CheckBoxChange;

        BtnSwitch.Click += BtnSwitch_Click;
        
        TextServerEnter.TextChanged += TextServerEnter_Change;
        ComboArgumentJava.DropDownOpened += ComboArgumentJava_DropDownOpened;
        CheckArgumentTitleEmpty.Change += CheckArgumentTitleEmpty_Change;
    }

    private void PageSetupSystem_Loaded(object sender, RoutedEventArgs e)
    {
        // 闁插秴顦查崝鐘烘祰闁劌鍨?
        PanBack.ScrollToHome();
        RefreshRam(false);

        // 閻㈠彉绨崥鍕嚋鐎圭偘绶ユ稉宥呮倱閿涘本鐦″▎锟犲厴闂団偓鐟曚線鍣搁弬鏉垮鏉?
        ModAnimation.AniControlEnabled += 1;
        Reload();
        ModAnimation.AniControlEnabled -= 1;

        // 闂堢偤鍣告径宥呭鏉炰粙鍎撮崚?
        if (isLoaded)
            return;
        isLoaded = true;

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
            TextArgumentTitle.Text = Config.Instance.Title[PageInstanceLeft.McInstance.PathInstance];
            CheckArgumentTitleEmpty.Checked = Config.Instance.UseGlobalTitle[PageInstanceLeft.McInstance.PathInstance];
            TextArgumentInfo.Text = Config.Instance.TypeInfo[PageInstanceLeft.McInstance.PathInstance];
            var _unused = PageInstanceLeft.McInstance.PathIndie; // 鐟欙箑褰傞懛顏勫З閸掋倕鐣?
            ComboArgumentIndieV2.SelectedIndex = Config.Instance.IndieV2[PageInstanceLeft.McInstance.PathInstance] ? 0 : 1;
            CheckArgumentTitleEmpty.IsVisible = TextArgumentTitle.Text.Length > 0 ? false : true;
            TextArgumentTitle.HintText = CheckArgumentTitleEmpty.Checked == true ? Lang.Text("Common.Option.Default") : Lang.Text("Instance.Setup.FollowGlobal");
            RefreshJavaComboBox();

            // 濞撳憡鍨欓崘鍛摠
            var ramType = Config.Instance.MemorySolution[PageInstanceLeft.McInstance.PathInstance];
            ((MyRadioBox)FindName("RadioRamType" + ramType)).Checked = true;
            SliderRamCustom.Value = Config.Instance.CustomMemorySize[PageInstanceLeft.McInstance.PathInstance];
            RamType(ramType);

            // 閺堝秴濮熼崳?
            TextServerEnter.Text = Config.Instance.ServerToEnter[PageInstanceLeft.McInstance.PathInstance];
            ComboServerLoginRequire.SelectedIndex = Config.InstanceAuth.LoginRequirementSolution[PageInstanceLeft.McInstance.PathInstance];
            comboServerLoginLast = ComboServerLoginRequire.SelectedIndex;
            ServerLogin(ComboServerLoginRequire.SelectedIndex);
            TextServerAuthServer.Text = Config.InstanceAuth.AuthServerAddress[PageInstanceLeft.McInstance.PathInstance];
            TextServerAuthName.Text = Config.InstanceAuth.AuthServerDisplayName[PageInstanceLeft.McInstance.PathInstance];
            TextServerAuthRegister.Text = Config.InstanceAuth.AuthRegisterAddress[PageInstanceLeft.McInstance.PathInstance];

            // 妤傛楠囩拋鍓х枂
            ComboAdvanceRenderer.SelectedIndex = Config.Instance.Renderer[PageInstanceLeft.McInstance.PathInstance];
            TextAdvanceClasspathHead.Text = Config.Instance.ClasspathHead[PageInstanceLeft.McInstance.PathInstance];
            TextAdvanceJvm.Text = Config.Instance.JvmArgs[PageInstanceLeft.McInstance.PathInstance];
            TextAdvanceGame.Text = Config.Instance.GameArgs[PageInstanceLeft.McInstance.PathInstance];
            TextAdvanceRun.Text = Config.Instance.PreLaunchCommand[PageInstanceLeft.McInstance.PathInstance];
            CheckAdvanceRunWait.Checked = Config.Instance.PreLaunchCommandWait[PageInstanceLeft.McInstance.PathInstance];
            CheckAdvanceDisableLwjglUnsafeAgent.Checked = Config.Instance.DisableLwjglUnsafeAgent[PageInstanceLeft.McInstance.PathInstance];
            if (Config.Instance.AssetVerifySolutionV1[PageInstanceLeft.McInstance.PathInstance] == 2)
            {
                ModBase.Log("[Setup] 瀹歌尪绺肩粔鏄忊偓浣哄閺堫剛娈戦崗鎶芥４閺傚洣娆㈤弽锟犵崣鐠佸墽鐤?);
                Config.Instance.AssetVerifySolutionV1Config.Reset(PageInstanceLeft.McInstance.PathInstance);
                Config.Instance.DisableAssetVerifyV2[PageInstanceLeft.McInstance.PathInstance] = true;
            }

            CheckAdvanceAssetsV2.Checked = Config.Instance.DisableAssetVerifyV2[PageInstanceLeft.McInstance.PathInstance];
            CheckAdvanceUseProxyV2.Checked = Config.Instance.UseProxy[PageInstanceLeft.McInstance.PathInstance];
            CheckAdvanceJava.Checked = Config.Instance.IgnoreJavaCompatibility[PageInstanceLeft.McInstance.PathInstance];
            if (SystemInfo.IsArm64System)
            {
                CheckAdvanceDisableJLW.Checked = true;
                CheckAdvanceDisableJLW.IsEnabled = false;
                CheckAdvanceDisableJLW.ToolTip = Lang.Text("Setup.Launch.Advanced.DisableJlw.Arm64ToolTip");
            }
            else
            {
                CheckAdvanceDisableJLW.Checked = Config.Instance.DisableJlw[PageInstanceLeft.McInstance.PathInstance];
            }
            CheckUseDebugLog4j2Config.Checked = Config.Instance.UseDebugLof4j2Config[PageInstanceLeft.McInstance.PathInstance];
            CheckAdvanceDisableLF.Checked = Config.Instance.DisableLF[PageInstanceLeft.McInstance.PathInstance];
        }

        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "闁插秷娴囩€圭偘绶ラ悪顒傜彌鐠佸墽鐤嗛弮璺哄毉闁?,
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Setup.Error.OperationFailed"));
        }
    }

    // 閸掓繂顫愰崠?
    public void Reset()
    {
        try
        {
            if (!Config.InstanceAuth.AuthLocked[PageInstanceLeft.McInstance.PathInstance])
                Config.InstanceAuth.Reset(PageInstanceLeft.McInstance.PathInstance);

            Config.Instance.Reset(PageInstanceLeft.McInstance.PathInstance);

            ModBase.Log("[Setup] 瀹告彃鍨垫慨瀣鐎圭偘绶ラ悪顒傜彌鐠佸墽鐤?);
            HintService.Hint(Lang.Text("Instance.Setup.Initialize.Success"), HintType.Success, false);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閸掓繂顫愰崠鏍х杽娓氬瀚粩瀣啎缂冾喖銇戠拹?,
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Setup.Error.OperationFailed"));
        }

        Reload();
    }

    // 鐏忓棙甯舵禒鑸垫暭閸欐鐭鹃悽鍗炲煂鐠佸墽鐤嗛弨鐟板綁
    private static void SetByTag(string tag, object value)
        => ConfigService.TrySetValue(tag, value, PageInstanceLeft.McInstance.PathInstance);

    private void RadioBoxChange(object o, ModBase.RouteEventArgs routeEventArgs)
    {
        if (ModAnimation.AniControlEnabled != 0)
            return;
        if (o is not MyRadioBox { Tag: string tag }) return;

        var slash = tag.IndexOf('/');
        if (slash < 0) return;

        SetByTag(tag[..slash], int.Parse(tag[(slash + 1)..]));
    }

    private void TextBoxChange(object o, TextChangedEventArgs textChangedEventArgs)
    {
        if (ModAnimation.AniControlEnabled != 0)
            return;
        if (o is not MyTextBox textBox) return;

        SetByTag(textBox.Tag?.ToString(), textBox.Text);
    }

    private void SliderChange(object o, bool user)
    {
        if (ModAnimation.AniControlEnabled != 0)
            return;
        if (o is not MySlider slider) return;

        SetByTag(slider.Tag?.ToString(), slider.Value);
    }

    private void ComboChange(MyComboBox sender, object e)
    {
        if (ModAnimation.AniControlEnabled != 0)
            return;

        SetByTag(sender.Tag?.ToString(), sender.SelectedIndex);
    }

    private void CheckBoxChange(object sender, bool user)
    {
        if (ModAnimation.AniControlEnabled != 0)
            return;
        if (sender is not MyCheckBox checkBox) return;

        SetByTag(checkBox.Tag?.ToString(), checkBox.Checked.GetValueOrDefault());
    }

    // 閸掑洦宕查崚鏉垮弿鐏炩偓鐠佸墽鐤?
    private void BtnSwitch_Click(object sender, PointerReleasedEventArgs e)
    {
        ModMain.frmMain.PageChange(FormMain.PageType.Setup);
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
        if (LabRamGame is null || LabRamUsed is null ||
            ModMain.frmMain.pageCurrent != FormMain.PageType.InstanceSetup ||
            ModMain.frmInstanceLeft.pageID != FormMain.PageSubType.VersionSetup)
            return;
        // 閼惧嘲褰囬崘鍛摠閹懎鍠?
        var ramGame = Math.Round(GetRam(PageInstanceLeft.McInstance), 5);
        var phyRam = KernelInterop.GetPhysicalMemoryBytes();
        var ramTotal = Math.Round((double)(phyRam.Total / 1024 / 1024 / 1024), 1);
        var ramAvailable = Math.Round((double)(phyRam.Available / 1024 / 1024 / 1024), 1);
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
            ramGame == 1d && !ModJava.IsGameSet64BitJava(PageInstanceLeft.McInstance) && !SystemInfo.Is32BitSystem &&
            ModJava.Javas.ExistAnyJava()
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
                }, "VersionSetup Ram Grid");
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
                        }, "VersionSetup Ram TextLeft");
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
                        }, "VersionSetup Ram TextLeft");
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
                        }, "VersionSetup Ram TextLeft");
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
                (ramTextRight != right || ModAnimation.AniIsRun("VersionSetup Ram TextRight")))
            {
                // 闂団偓鐟曚礁濮╅悽?
                ModAnimation.AniStart(
                    new[]
                    {
                        ModAnimation.AaX(LabRamGame, totalWidth - labGameWidth - LabRamGame.Margin.Left, 100,
                            ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)),
                        ModAnimation.AaX(LabRamGameTitle, totalWidth - labGameTitleWidth - LabRamGameTitle.Margin.Left,
                            100, ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak))
                    }, "VersionSetup Ram TextRight");
            }
            else
            {
                // 娑撳秹娓剁憰浣稿З閻?
                LabRamGame.Margin = new Thickness(totalWidth - labGameWidth, 3d, 0d, 0d);
                LabRamGameTitle.Margin = new Thickness(totalWidth - labGameTitleWidth, 0d, 0d, 5d);
            }
        }
        else if (ModAnimation.AniControlEnabled == 0 &&
                 (ramTextRight != right || ModAnimation.AniIsRun("VersionSetup Ram TextRight")))
        {
            // 闂団偓鐟曚礁濮╅悽?
            ModAnimation.AniStart(
                new[]
                {
                    ModAnimation.AaX(LabRamGame, 2d + rectUsedWidth - LabRamGame.Margin.Left, 100,
                        ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)),
                    ModAnimation.AaX(LabRamGameTitle, 2d + rectUsedWidth - LabRamGameTitle.Margin.Left, 100,
                        ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak))
                }, "VersionSetup Ram TextRight");
        }
        else
        {
            // 娑撳秹娓剁憰浣稿З閻?
            LabRamGame.Margin = new Thickness(2d + rectUsedWidth, 3d, 0d, 0d);
            LabRamGameTitle.Margin = new Thickness(2d + rectUsedWidth, 0d, 0d, 5d);
        }

        ramTextRight = right;
    }

    /// <summary>
    ///     閼惧嘲褰囪ぐ鎾冲鐠佸墽鐤嗛惃?RAM 閸婄鈧倸宕熸担宥勮礋 GB閵?
    /// </summary>
    public static double GetRam(McInstance version, bool? is32BitJava = default)
    {
        var instancePath = version?.PathInstance;
        // 鐠虹喖娈㈤崗銊ョ湰鐠佸墽鐤?
        if (Config.Instance.MemorySolution[instancePath] == 2)
            return PageSetupLaunch.GetRam(version, true, is32BitJava);

        // ------------------------------------------
        // 娣囶喗鏁兼稉瀣煙娴狅絿鐖滈弮鍫曟付鐟曚椒绔撮獮鏈垫叏閺€?PageSetupLaunch
        // ------------------------------------------

        // 娴ｈ法鏁よぐ鎾冲鐎圭偘绶ラ惃鍕啎缂?
        var ramGive = default(double);
        if (Config.Instance.MemorySolution[instancePath] == 0)
        {
            // 閼奉亜濮╅柊宥囩枂
            var ramAvailable =
                Math.Round((double)(KernelInterop.GetAvailablePhysicalMemoryBytes() / 1024 / 1024 / 1024 * 10)) / 10;
            // 绾喖鐣鹃棁鈧Ч鍌滄畱閸愬懎鐡ㄩ崐?
            double ramMininum; // 閺冪姾顔戞俊鍌欑秿娑旂喖娓剁憰浣风箽鐠囦胶娈戦張鈧担搴ㄦ鎼达箑鍞寸€?
            double ramTarget1; // 娴兼媽顓搁懗钘夊瀵搫鐢崝銊ょ啊閻ㄥ嫬鍞寸€?
            double ramTarget2; // 娴兼媽顓稿▽鈥虫殣闂傤噣顣芥禍鍡欐畱閸愬懎鐡?
            double ramTarget3; // 鐎瑰顥婃潻鍥ь樋闂勫嫬濮炵紒鍕闂団偓鐟曚胶娈戦崘鍛摠
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

            double ramDelta;
            // 妫板嫬鍨庨柊宥呭敶鐎涙﹫绱濋梼鑸殿唽娑撯偓閿? ~ T1閿?00%
            ramDelta = ramTarget1;
            ramGive += Math.Min(ramAvailable, ramDelta);
            ramAvailable -= ramDelta;
            if (ramAvailable >= 0.1d)
            {
                // 妫板嫬鍨庨柊宥呭敶鐎涙﹫绱濋梼鑸殿唽娴滃矉绱漈1 ~ T2閿?0%
                ramDelta = ramTarget2 - ramTarget1;
                ramGive += Math.Min(ramAvailable * 0.7d, ramDelta);
                ramAvailable -= ramDelta / 0.7d;
                if (ramAvailable >= 0.1d)
                {
                    // 妫板嫬鍨庨柊宥呭敶鐎涙﹫绱濋梼鑸殿唽娑撳绱漈2 ~ T3閿?0%
                    ramDelta = ramTarget3 - ramTarget2;
                    ramGive += Math.Min(ramAvailable * 0.4d, ramDelta);
                    ramAvailable -= ramDelta / 0.4d;
                    if (ramAvailable >= 0.1d)
                    {
                        // 妫板嫬鍨庨柊宥呭敶鐎涙﹫绱濋梼鑸殿唽閸ユ冻绱漈3 ~ T3 * 2閿?5%
                        ramDelta = ramTarget3;
                        ramGive += Math.Min(ramAvailable * 0.15d, ramDelta);
                        ramAvailable -= ramDelta / 0.15d;
                    }
                }
            }

            // 娑撳秳缍嗘禍搴㈡付娴ｅ骸鈧?
            ramGive = Math.Round(Math.Max(ramGive, ramMininum), 1);
        }
        else
        {
            // 閹靛濮╅柊宥囩枂
            var value = Config.Instance.CustomMemorySize[instancePath];
            if (value <= 12)
                ramGive = value * 0.1d + 0.3d;
            else if (value <= 25)
                ramGive = (value - 12) * 0.5d + 1.5d;
            else if (value <= 33)
                ramGive = (value - 25) * 1 + 8;
            else
                ramGive = (value - 33) * 2 + 16;
        }

        // 閼汇儰濞囬悽?32 娴?Java閿涘苯鍨梽鎰煑娑?1G
        if (is32BitJava ?? !ModJava.IsGameSet64BitJava(PageInstanceLeft.McInstance))
            ramGive = Math.Min(1d, ramGive);
        return ramGive;
    }

    #endregion

    #region 閺堝秴濮熼崳?

    // 閸忋劌鐪?
    private int comboServerLoginLast;

    private void ComboServerLogin_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (ModAnimation.AniControlEnabled != 0)
            return;
        ServerLogin(ComboServerLoginRequire.SelectedIndex);
        if (TextServerAuthServer.IsValidated)
            BtnServerAuthLock.IsEnabled = true;
        else
            BtnServerAuthLock.IsEnabled = false;
        if ((ComboServerLoginRequire.SelectedIndex == 2 || ComboServerLoginRequire.SelectedIndex == 3) &&
            !TextServerAuthServer.IsValidated)
            return;
        if (comboServerLoginLast == ComboServerLoginRequire.SelectedIndex)
            return;
        comboServerLoginLast = ComboServerLoginRequire.SelectedIndex;
        Config.InstanceAuth.LoginRequirementSolution[PageInstanceLeft.McInstance.PathInstance] = ComboServerLoginRequire.SelectedIndex;
    }

    private void TextServerAuthServer_MouseLeave(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TextServerAuthServer.Text))
            return;
        if (!(TextServerAuthServer.Text.EndsWithF("/api/yggdrasil/") ||
              TextServerAuthServer.Text.EndsWithF("/api/yggdrasil")))
        {
            if (TextServerAuthServer.Text.EndsWithF("/"))
            {
                TextServerAuthServer.Text = $"{TextServerAuthServer.Text}api/yggdrasil";
                HintService.Hint(Lang.Text("Instance.Setup.Server.AuthServer.AutoFormatted"));
            }
            else
            {
                TextServerAuthServer.Text = $"{TextServerAuthServer.Text}/api/yggdrasil";
                HintService.Hint(Lang.Text("Instance.Setup.Server.AuthServer.AutoFormatted"));
            }
        }

        if (TextServerAuthServer.Text.EndsWithF("/api/yggdrasil/"))
        {
            TextServerAuthServer.Text = TextServerAuthServer.Text.BeforeLast("/");
            HintService.Hint(Lang.Text("Instance.Setup.Server.AuthServer.AutoFormatted"));
        }

        comboServerLoginLast = ComboServerLoginRequire.SelectedIndex;
        ComboChange(ComboServerLoginRequire, null);
    }

    public void ServerLogin(int type)
    {
        LabServerAuthName.IsVisible = type == 2 || type == 3 ? true : false;
        TextServerAuthName.IsVisible = type == 2 || type == 3 ? true : false;
        LabServerAuthRegister.IsVisible = type == 2 || type == 3 ? true : false;
        TextServerAuthRegister.IsVisible = type == 2 || type == 3 ? true : false;
        LabServerAuthServer.IsVisible = type == 2 || type == 3 ? true : false;
        TextServerAuthServer.IsVisible = type == 2 || type == 3 ? true : false;
        BtnServerAuthLittle.IsVisible = type == 2 || type == 3 ? true : false;
        BtnServerNewProfile.IsVisible = type == 2 || type == 3 ? true : false;
        if (type == 0 || type == 1)
            BtnServerAuthLock.IsVisible = false;
        else
            BtnServerAuthLock.IsVisible = true;
        if (Config.InstanceAuth.AuthLocked[PageInstanceLeft.McInstance.PathInstance])
        {
            HintServerLoginLock.IsVisible = true;
            ComboServerLoginRequire.IsEnabled = false;
            TextServerAuthServer.IsEnabled = false;
            TextServerAuthName.IsEnabled = false;
            TextServerAuthRegister.IsEnabled = false;
            BtnServerAuthLittle.IsEnabled = false;
        }
        else
        {
            HintServerLoginLock.IsVisible = false;
            ComboServerLoginRequire.IsEnabled = true;
            TextServerAuthServer.IsEnabled = true;
            TextServerAuthName.IsEnabled = true;
            TextServerAuthRegister.IsEnabled = true;
            BtnServerAuthLittle.IsEnabled = true;
        }

        CardServer.TriggerForceResize();
        // 闁灝鍘ゅ锝囧妤犲矁鐦夐崪宀€顬囩痪鍧楃崣鐠囦礁鍤悳鐗堫劃閹绘劗銇?
        if (type != 2 && type != 3)
        {
            LabServerAuthServerSecurity.IsVisible = false;
            LabServerAuthServerSecurityCL.IsVisible = false;
            LabServerAuthServerSecurityVerify.IsVisible = false;
        }
        // 婵″倹鐏夊鈧径缈犺礋 http:// 缂佹瑤绨ｇ拃锕€鎲?
        else if (TextServerAuthServer.Text.StartsWithF("https://"))
        {
            LabServerAuthServerSecurity.IsVisible = false;
            LabServerAuthServerSecurityVerify.IsVisible = true;
            LabServerAuthServerSecurityCL.IsVisible = true;
        }
        else if (TextServerAuthServer.Text.StartsWithF("http://"))
        {
            LabServerAuthServerSecurity.IsVisible = true;
            LabServerAuthServerSecurityCL.IsVisible = true;
            LabServerAuthServerSecurityVerify.IsVisible = false;
        }
        else
        {
            LabServerAuthServerSecurity.IsVisible = false;
            LabServerAuthServerSecurityVerify.IsVisible = false;
            LabServerAuthServerSecurityCL.IsVisible = false;
        }
    }

    // LittleSkin
    private void BtnServerAuthLittle_Click(object sender, PointerReleasedEventArgs e)
    {
        if (!string.IsNullOrEmpty(TextServerAuthServer.Text) &&
        TextServerAuthServer.Text != "https://littleskin.cn/api/yggdrasil" && ModMain.MyMsgBox(
        Lang.Text("Instance.Setup.Server.LittleSkin.Override.Message"),
        Lang.Text("Instance.Setup.Server.LittleSkin.Override.Title"), Lang.Text("Instance.Setup.Server.LittleSkin.Override.Continue"), Lang.Text("Common.Action.Cancel")) == 2)
            return;
        TextServerAuthServer.Text = "https://littleskin.cn/api/yggdrasil";
        TextServerAuthRegister.Text = "https://littleskin.cn/auth/register";
        TextServerAuthName.Text = Lang.Text("Instance.Setup.Server.LittleSkin.Name");
    }

    // 闁夸礁鐣剧拋鍓х枂
    private void BtnServerAuthLock_Click(object sender, PointerReleasedEventArgs e)
    {
        if (ModMain.MyMsgBox(
                Lang.Text("Instance.Setup.Server.LockLoginMethod.Message"),
                Lang.Text("Instance.Setup.Server.LockLoginMethod.Title"), Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel"), isWarn: true) == 1)
        {
            Config.InstanceAuth.AuthLocked[PageInstanceLeft.McInstance.PathInstance] = true;
            Reload();
        }
    }

    // 鐠哄疇娴嗛弬鏉跨紦濡楋絾顢?
    private void BtnServerNewProfile_Click(object sender, PointerReleasedEventArgs e)
    {
        ModMain.frmMain.PageChange(new FormMain.PageStackData { page = FormMain.PageType.Launch });
        PageLoginAuth.draggedAuthServer = TextServerAuthServer.Text;
        ModBase.RunInNewThread(() =>
        {
            Thread.Sleep(150);
            ModBase.RunInUi(() => ModMain.frmLaunchLeft.RefreshPage(true, ModLaunch.McLoginType.Auth));
        });
    }

    private static void TextServerEnter_Change(object sender, TextChangedEventArgs e)
    {
        if (sender is MyTextBox textBox) textBox.Text = textBox.Text.Replace("閿?, ":");
    }

    #endregion

    #region Java 闁瀚?

    // 閸掗攱鏌?Java 娑撳濯哄鍡樻▔缁€?
    public void RefreshJavaComboBox()
    {
        if (ComboArgumentJava is null)
            return;

        // 閼惧嘲褰囩€圭偘绶ラ惃?Java 閸嬪繐銈介敍鍫濆嚒閸忕厧顔愰弬鐗堟＋閺嶇厧绱￠敍?
        var preference = ModJava.GetInstanceJavaPreference(PageInstanceLeft.McInstance);

        // === 1. 閸掓繂顫愰崠鏍ф祼鐎规岸鈧銆嶉敍鍫滃▏閻劎琚崹瀣暔閸忋劎娈?Tag閿?===
        ComboArgumentJava.Items.Clear();

        // 闁銆?0: 鐠虹喖娈㈤崗銊ョ湰鐠佸墽鐤?
        ComboArgumentJava.Items.Add(new MyComboBoxItem
        {
            Content = Lang.Text("Instance.Setup.FollowGlobal"),
            Tag = new UseGlobalPreference()
        });

        // 闁銆?1: 閼奉亜濮╅柅澶嬪
        ComboArgumentJava.Items.Add(new MyComboBoxItem
        {
            Content = Lang.Text("Instance.Setup.Options.Java.AutoSelect"),
            Tag = new AutoSelect() // Nothing 鐞涖劎銇氶懛顏勫З闁瀚?
        });

        // 闁銆?2: 閻╃顕捄顖氱窞闁銆?
        MyComboBoxItem relativePathItem;
        if (preference is UseRelativePath)
        {
            var relPref = (UseRelativePath)preference;
            var absPath = Path.GetFullPath(Path.Combine(Basics.ExecutableDirectory, relPref.RelativePath));
            var javaEntry = ModJava.Javas.Get(absPath);

            if (Files.IsPathWithinDirectory(absPath, Basics.ExecutableDirectory) && javaEntry is not null &&
                javaEntry.IsEnabled)
                // 閺堝鏅ョ捄顖氱窞閿涙碍妯夌粈鍝勫徔娴?Java 娣団剝浼?
                relativePathItem = new MyComboBoxItem
                {
                    Content = Lang.Text("Instance.Setup.Options.Java.SelectRelative.WithJava", javaEntry.ToString()),
                    Tag = new UseRelativePath(relPref.RelativePath),
                    ToolTip = Lang.Text("Instance.Setup.Options.Java.RelativePathToolTip", relPref.RelativePath, absPath)
                };
            else
                // 閺冪姵鏅ョ捄顖氱窞閿涙碍褰佺粈铏规暏閹寸兘鍣搁弬浼粹偓澶嬪
                relativePathItem = new MyComboBoxItem
                {
                    Content = Lang.Text("Instance.Setup.Options.Java.SelectRelative.Invalid"),
                    Tag = new UseRelativePath(relPref.RelativePath),
                    ToolTip = Lang.Text("Instance.Setup.Options.Java.InvalidPathToolTip", absPath)
                };
        }
        else
        {
            // 閺堫亪鍘ょ純顔炬祲鐎电鐭惧鍕剁窗娴ｈ法鏁ゆ妯款吇濡剝婢?
            relativePathItem = new MyComboBoxItem
            {
                Content = Lang.Text("Instance.Setup.Options.Java.SelectRelative"),
                Tag = new UseRelativePath(@"jre\bin\java.exe"),
                ToolTip = Lang.Text("Instance.Setup.Options.Java.SelectRelativeToolTip")
            };
        }

        ComboArgumentJava.Items.Add(relativePathItem);

        // === 2. 濞ｈ濮為幍鈧張澶婂讲閻?Java 鏉╂劘顢戦弮?===
        MyComboBoxItem selectedItem = null;
        try
        {
            foreach (var curJava in ModJava.Javas.GetSortedJavaList())
            {
                var item = new MyComboBoxItem
                {
                    Content = curJava.ToString(),
                    ToolTip =
                        Lang.Text("Instance.Setup.Options.Java.Details.ToolTip", curJava.Installation.JavaExePath, curJava.Installation.Version, curJava.Source),
                    Tag = curJava
                };
                ToolTipService.SetInitialShowDelay(item, 300);
                ToolTipService.SetBetweenShowDelay(item, 100);
                ComboArgumentJava.Items.Add(item);
            }
        }
        catch (Exception ex)
        {
            Config.Instance.SelectedJava[PageInstanceLeft.McInstance.PathInstance] = "娴ｈ法鏁ら崗銊ョ湰鐠佸墽鐤?;
            ModBase.Log(
                ex,
                "閺囧瓨鏌婄€圭偘绶ョ拋鍓х枂 Java 娑撳濯哄鍡椼亼鐠?,
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Setup.Error.OperationFailed"));
            ComboArgumentJava.Items.Clear();
            ComboArgumentJava.Items.Add(new MyComboBoxItem
            {
                Content = Lang.Text("Instance.Setup.Options.Java.LoadFailed"),
                IsEnabled = false
            });
            ComboArgumentJava.SelectedIndex = 0;
            RefreshRam(true);
            return;
        }

        // === 3. 閺嶈宓佽ぐ鎾冲閸嬪繐銈界拋鍓х枂闁鑵戞い鐧哥礄娴兼ê鍘涙担璺ㄦ暏閺傜増鐗稿?preference閿?===
        if (preference is null)
        {
            // 閼奉亜濮╅柅澶嬪
            selectedItem = ComboArgumentJava.Items[1] as MyComboBoxItem;
        }
        else if (preference is UseGlobalPreference)
        {
            selectedItem = ComboArgumentJava.Items[0] as MyComboBoxItem;
        }
        else if (preference is UseRelativePath)
        {
            selectedItem = ComboArgumentJava.Items[2] as MyComboBoxItem;
        }
        else if (preference is ExistingJava)
        {
            var existPref = (ExistingJava)preference;
            // 閸?Java 閸掓銆冩稉顓熺叀閹垫儳灏柊宥夈€嶉敍鍫滅矤缁便垹绱?3 瀵偓婵绱?
            for (int i = 3, loopTo = ComboArgumentJava.Items.Count - 1; i <= loopTo; i++)
            {
                var item = ComboArgumentJava.Items[i] as MyComboBoxItem;
                if (item is not null && item.Tag is JavaEntry)
                {
                    var javaEntry = (JavaEntry)item.Tag;
                    if (string.Equals(javaEntry.Installation.JavaExePath, existPref.JavaExePath,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        selectedItem = item;
                        break;
                    }
                }
            }
        }

        // 闂勫秶楠囨径鍕倞閿涙碍妫ら崠褰掑帳妞よ妞傞崶鐐衡偓鈧崚鎷屽殰閸斻劑鈧瀚?
        if (selectedItem is null && ComboArgumentJava.Items.Count > 1)
            selectedItem = ComboArgumentJava.Items[1] as MyComboBoxItem;

        // 鐠佸墽鐤嗛柅澶夎厬妞?
        if (selectedItem is not null) ComboArgumentJava.SelectedItem = selectedItem;

        // === 4. 閺冪姴褰查悽?Java 閺冨墎娈戦梽宥囬獓婢跺嫮鎮?===
        if (!ModJava.Javas.ExistAnyJava() && ComboArgumentJava.Items.Count <= 3)
        {
            ComboArgumentJava.Items.Clear();
            var noJavaItem = new MyComboBoxItem
            {
                Content = Lang.Text("Instance.Setup.Options.Java.NoRuntime"),
                ToolTip = Lang.Text("Instance.Setup.Options.Java.NoRuntime.ToolTip"),
                IsEnabled = false
            };
            ComboArgumentJava.Items.Add(noJavaItem);
            ComboArgumentJava.SelectedItem = noJavaItem;
        }

        // === 5. 閸掗攱鏌婇崗瀹犱粓閹貉傛 ===
        RefreshRam(true);
    }

    // 闂冪粯顒涢崷銊︽￥閺佸牏濮搁幀浣风瑓鐏炴洖绱戞稉瀣濡?
    private void ComboArgumentJava_DropDownOpened(object? sender, EventArgs e)
    {
        if (ComboArgumentJava.SelectedItem is null)
        {
            ComboArgumentJava.IsDropDownOpen = false;
            return;
        }

        var firstItem = ComboArgumentJava.Items[0] as MyComboBoxItem;
        if (firstItem is not null &&
        ((string)firstItem.Content == Lang.Text("Instance.Setup.Options.Java.NoRuntime") ||
        (string)firstItem.Content == Lang.Text("Instance.Setup.Options.Java.LoadFailed")))
            ComboArgumentJava.IsDropDownOpen = false;
    }

    // 娑撳濯哄鍡涒偓澶嬪閺囧瓨鏁兼径鍕倞閿涘牅绻氱€涙ɑ鏌婇弽鐓庣础闁板秶鐤嗛敍?
    private void JavaSelectionUpdate(object sender, SelectionChangedEventArgs e)
    {
        if (ModAnimation.AniControlEnabled != 0)
            return;
        if (ComboArgumentJava.SelectedItem is null)
            return;

        var selectedItem = ComboArgumentJava.SelectedItem as MyComboBoxItem;
        if (selectedItem is null || (selectedItem.Tag is null &&
        (string)selectedItem.Content != Lang.Text("Instance.Setup.Options.Java.AutoSelect")))
            return;

        JavaPreference preference = default;
        var logMessage = "";

        // 閺嶈宓?Tag 缁鐎烽悽鐔稿灇閸嬪繐銈界€电钖?
        if (selectedItem.Tag is null or AutoSelect)
        {
            // 閼奉亜濮╅柅澶嬪閿涙艾鐡ㄩ崒銊р敄鐎涙顑佹稉?
            preference = new AutoSelect();
            logMessage = "[Java] 娣囶喗鏁肩€圭偘绶?Java 闁瀚ㄧ拋鍓х枂閿涙俺鍤滈崝銊┾偓澶嬪";
        }
        else if (selectedItem.Tag is UseGlobalPreference)
        {
            preference = new UseGlobalPreference();
            logMessage = "[Java] 娣囶喗鏁肩€圭偘绶?Java 闁瀚ㄧ拋鍓х枂閿涙俺绐￠梾蹇撳弿鐏炩偓鐠佸墽鐤?;
        }
        else if (selectedItem.Tag is UseRelativePath)
        {
            // 閻╃顕捄顖氱窞閿涙岸娓剁憰浣烘暏閹寸兘鈧瀚ㄧ€圭偤妾弬鍥︽
            var ret = SystemDialogs.SelectFile(Lang.Text("Setup.Java.SelectFile.Filter"), Lang.Text("Setup.Java.SelectFile.Title"), Basics.ExecutableDirectory);
            if (string.IsNullOrWhiteSpace(ret))
                // 閻劍鍩涢崣鏍ㄧХ閿涘奔绗夋穱婵嗙摠闁板秶鐤嗛敍灞肩箽閹镐礁甯柅澶嬪
                return;

            ret = Path.GetFullPath(ret);
            var relativePath = Path.GetRelativePath(Basics.ExecutableDirectory, ret);

            // 妤犲矁鐦夌捄顖氱窞閺勵垰鎯侀崷銊ユ儙閸斻劌娅掗惄顔肩秿閸?
            if (!Files.IsPathWithinDirectory(relativePath, Basics.ExecutableDirectory))
            {
                HintService.Hint(Lang.Text("Instance.Setup.Options.Java.PathOutOfRange"), HintType.Error);
                return;
            }

            preference = new UseRelativePath(relativePath);
            logMessage = $"[Java] 娣囶喗鏁肩€圭偘绶?Java 闁瀚ㄧ拋鍓х枂閿涙氨娴夌€电鐭惧?| {relativePath}";
        }
        else if (selectedItem.Tag is JavaEntry)
        {
            var javaEntry = (JavaEntry)selectedItem.Tag;
            preference = new ExistingJava(javaEntry.Installation.JavaExePath);
            logMessage = $"[Java] 娣囶喗鏁肩€圭偘绶?Java 闁瀚ㄧ拋鍓х枂閿涙javaEntry}";
        }

        // 娣囨繂鐡ㄩ柊宥囩枂
        var json = JsonSerializer.Serialize(preference, JsonCompat.SerializerOptions);
        Config.Instance.SelectedJava[PageInstanceLeft.McInstance.PathInstance] = json;


        ModBase.Log(logMessage);
        RefreshRam(true);
    }

    #endregion

    #region 閸忔湹绮拋鍓х枂

    // 閻楀牊婀伴梾鏃傤瀲鐠€锕€鎲?
    private bool isReverting;

    private void ComboArgumentIndieV2_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModAnimation.AniControlEnabled != 0)
            return;
        if (isReverting)
            return;
        if (ModMain.MyMsgBox(
                Lang.Text("Instance.Setup.Options.InstanceIsolation.Message"),
                Lang.Text("Common.Dialog.Warning"), Lang.Text("Setup.Launch.Advanced.Renderer.Warning.Confirm"), Lang.Text("Common.Action.Cancel"), isWarn: true) == 2)
        {
            isReverting = true;
            ComboArgumentIndieV2.SelectedItem = e.RemovedItems[0];
            isReverting = false;
        }
        else
        {
            bool newValue = ComboArgumentIndieV2.SelectedIndex == 0;
            Config.Instance.IndieV2[PageInstanceLeft.McInstance.PathInstance] = newValue;
        }
    }

    // 濞撳憡鍨欑粣妤€褰?
    private void CheckArgumentTitleEmpty_Change(object sender, bool e)
    {
        TextArgumentTitle.HintText = CheckArgumentTitleEmpty.Checked == true ? Lang.Text("Common.Option.Default") : Lang.Text("Instance.Setup.FollowGlobal");
        CheckBoxChange(sender,e);
    }

    private void TextArgumentTitle_TextChanged(object sender, TextChangedEventArgs e)
    {
        CheckArgumentTitleEmpty.IsVisible = TextArgumentTitle.Text.Length > 0 ? false : true;
        TextBoxChange(sender,e);
    }

    #endregion

    #region 妤傛楠囩拋鍓х枂

    private void TextAdvanceRun_TextChanged(object sender, TextChangedEventArgs e)
    {
        CheckAdvanceRunWait.IsVisible = string.IsNullOrEmpty(TextAdvanceRun.Text) ? false : true;
        TextBoxChange(sender,e);
    }

    private void ComboAdvanceRenderer_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModAnimation.AniControlEnabled != 0)
            return;

        var args = e; // 鏉烆剚宕叉禍瀣╂閸欏倹鏆?

        if (!States.Hint.Renderer && ComboAdvanceRenderer.SelectedIndex != 0)
        {
            if (ModMain.MyMsgBox(Lang.Text("Setup.Launch.Advanced.Renderer.Warning.Message"),
                    Lang.Text("Common.Dialog.Warning"),
                    Lang.Text("Setup.Launch.Advanced.Renderer.Warning.Confirm"), Lang.Text("Common.Action.Cancel"), isWarn: true) == 2)
            {
                ComboAdvanceRenderer.SelectedItem = args.RemovedItems[0];
            }
            else
            {
                ComboChange(ComboAdvanceRenderer, e);
                States.Hint.Renderer = true;
            }
        }
        else
        {
            ComboChange(ComboAdvanceRenderer, e);
        }
    }

    private void CheckUseDebugLog4j2Config_CheckChanged(object sender, bool e)
    {
        if (ModAnimation.AniControlEnabled != 0)
            return;
        var checkBox = sender as MyCheckBox;
        if (checkBox is null) return;
    
        if (checkBox.Checked.GetValueOrDefault() && !States.Hint.DebugLog4j2Config)
        {
            if (ModMain.MyMsgBox(
                    Lang.Text("Instance.Setup.Advanced.UseDebugLog4j.Message"),
                    Lang.Text("Common.Dialog.Warning"), Lang.Text("Setup.Launch.Advanced.Renderer.Warning.Confirm"), Lang.Text("Common.Action.Cancel"), isWarn: true) == 2)
            {
                checkBox.Checked = false;
            }
            else
            {
                CheckBoxChange(sender, e);
                States.Hint.DebugLog4j2Config = true;
            }
        }
        else
        {
            CheckBoxChange(sender, e);
        }
    }

    #endregion
}
