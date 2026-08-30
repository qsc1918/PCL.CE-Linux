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
        // 閲嶅鍔犺浇閮ㄥ垎
        PanBack.ScrollToHome();
        RefreshRam(false);

        // 鐢变簬鍚勪釜瀹炰緥涓嶅悓锛屾瘡娆￠兘闇€瑕侀噸鏂板姞杞?
        ModAnimation.AniControlEnabled += 1;
        Reload();
        ModAnimation.AniControlEnabled -= 1;

        // 闈為噸澶嶅姞杞介儴鍒?
        if (isLoaded)
            return;
        isLoaded = true;

        // 鍐呭瓨鑷姩鍒锋柊
        var timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 1) };
        timer.Tick += (_, _) => RefreshRam();
        timer.Start();
        RectRamGame.SizeChanged += (s, e) => RefreshRamText();
    }

    public void Reload()
    {
        try
        {
            // 鍚姩鍙傛暟
            TextArgumentTitle.Text = Config.Instance.Title[PageInstanceLeft.McInstance.PathInstance];
            CheckArgumentTitleEmpty.Checked = Config.Instance.UseGlobalTitle[PageInstanceLeft.McInstance.PathInstance];
            TextArgumentInfo.Text = Config.Instance.TypeInfo[PageInstanceLeft.McInstance.PathInstance];
            var _unused = PageInstanceLeft.McInstance.PathIndie; // 瑙﹀彂鑷姩鍒ゅ畾
            ComboArgumentIndieV2.SelectedIndex = Config.Instance.IndieV2[PageInstanceLeft.McInstance.PathInstance] ? 0 : 1;
            CheckArgumentTitleEmpty.Visibility = TextArgumentTitle.Text.Length > 0 ? Visibility.Collapsed : Visibility.Visible;
            TextArgumentTitle.HintText = CheckArgumentTitleEmpty.Checked == true ? Lang.Text("Common.Option.Default") : Lang.Text("Instance.Setup.FollowGlobal");
            RefreshJavaComboBox();

            // 娓告垙鍐呭瓨
            var ramType = Config.Instance.MemorySolution[PageInstanceLeft.McInstance.PathInstance];
            ((MyRadioBox)FindName("RadioRamType" + ramType)).Checked = true;
            SliderRamCustom.Value = Config.Instance.CustomMemorySize[PageInstanceLeft.McInstance.PathInstance];
            RamType(ramType);

            // 鏈嶅姟鍣?
            TextServerEnter.Text = Config.Instance.ServerToEnter[PageInstanceLeft.McInstance.PathInstance];
            ComboServerLoginRequire.SelectedIndex = Config.InstanceAuth.LoginRequirementSolution[PageInstanceLeft.McInstance.PathInstance];
            comboServerLoginLast = ComboServerLoginRequire.SelectedIndex;
            ServerLogin(ComboServerLoginRequire.SelectedIndex);
            TextServerAuthServer.Text = Config.InstanceAuth.AuthServerAddress[PageInstanceLeft.McInstance.PathInstance];
            TextServerAuthName.Text = Config.InstanceAuth.AuthServerDisplayName[PageInstanceLeft.McInstance.PathInstance];
            TextServerAuthRegister.Text = Config.InstanceAuth.AuthRegisterAddress[PageInstanceLeft.McInstance.PathInstance];

            // 楂樼骇璁剧疆
            ComboAdvanceRenderer.SelectedIndex = Config.Instance.Renderer[PageInstanceLeft.McInstance.PathInstance];
            TextAdvanceClasspathHead.Text = Config.Instance.ClasspathHead[PageInstanceLeft.McInstance.PathInstance];
            TextAdvanceJvm.Text = Config.Instance.JvmArgs[PageInstanceLeft.McInstance.PathInstance];
            TextAdvanceGame.Text = Config.Instance.GameArgs[PageInstanceLeft.McInstance.PathInstance];
            TextAdvanceRun.Text = Config.Instance.PreLaunchCommand[PageInstanceLeft.McInstance.PathInstance];
            CheckAdvanceRunWait.Checked = Config.Instance.PreLaunchCommandWait[PageInstanceLeft.McInstance.PathInstance];
            CheckAdvanceDisableLwjglUnsafeAgent.Checked = Config.Instance.DisableLwjglUnsafeAgent[PageInstanceLeft.McInstance.PathInstance];
            if (Config.Instance.AssetVerifySolutionV1[PageInstanceLeft.McInstance.PathInstance] == 2)
            {
                ModBase.Log("[Setup] 宸茶縼绉昏€佺増鏈殑鍏抽棴鏂囦欢鏍￠獙璁剧疆");
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
                "閲嶈浇瀹炰緥鐙珛璁剧疆鏃跺嚭閿?,
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Setup.Error.OperationFailed"));
        }
    }

    // 鍒濆鍖?
    public void Reset()
    {
        try
        {
            if (!Config.InstanceAuth.AuthLocked[PageInstanceLeft.McInstance.PathInstance])
                Config.InstanceAuth.Reset(PageInstanceLeft.McInstance.PathInstance);

            Config.Instance.Reset(PageInstanceLeft.McInstance.PathInstance);

            ModBase.Log("[Setup] 宸插垵濮嬪寲瀹炰緥鐙珛璁剧疆");
            HintService.Hint(Lang.Text("Instance.Setup.Initialize.Success"), HintType.Success, false);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "鍒濆鍖栧疄渚嬬嫭绔嬭缃け璐?,
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Setup.Error.OperationFailed"));
        }

        Reload();
    }

    // 灏嗘帶浠舵敼鍙樿矾鐢卞埌璁剧疆鏀瑰彉
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

    // 鍒囨崲鍒板叏灞€璁剧疆
    private void BtnSwitch_Click(object sender, MouseButtonEventArgs e)
    {
        ModMain.frmMain.PageChange(FormMain.PageType.Setup);
    }

    #region 娓告垙鍐呭瓨
    public void RamType(int type)
    {
        if (SliderRamCustom is null)
            return;
        SliderRamCustom.IsEnabled = type == 1;
    }

    /// <summary>
    ///     鍒锋柊 UI 涓婄殑 RAM 鏄剧ず銆?
    /// </summary>
    public void RefreshRam(bool showAnim)
    {
        if (LabRamGame is null || LabRamUsed is null ||
            ModMain.frmMain.pageCurrent != FormMain.PageType.InstanceSetup ||
            ModMain.frmInstanceLeft.pageID != FormMain.PageSubType.VersionSetup)
            return;
        // 鑾峰彇鍐呭瓨鎯呭喌
        var ramGame = Math.Round(GetRam(PageInstanceLeft.McInstance), 5);
        var phyRam = KernelInterop.GetPhysicalMemoryBytes();
        var ramTotal = Math.Round((double)(phyRam.Total / 1024 / 1024 / 1024), 1);
        var ramAvailable = Math.Round((double)(phyRam.Available / 1024 / 1024 / 1024), 1);
        var ramGameActual = Math.Round(Math.Min(ramGame, ramAvailable), 5);
        var ramUsed = Math.Round(ramTotal - ramAvailable, 5);
        var ramEmpty = Math.Round(ModBase.MathClamp(ramTotal - ramUsed - ramGame, 0d, 1000d), 1);
        // 璁剧疆鏈€澶у彲鐢ㄥ唴瀛?
        if (ramTotal <= 1.5d)
            SliderRamCustom.MaxValue = (int)Math.Round(Math.Max(Math.Floor((ramTotal - 0.3d) / 0.1d), 1d));
        else if (ramTotal <= 8d)
            SliderRamCustom.MaxValue = (int)Math.Round(Math.Floor((ramTotal - 1.5d) / 0.5d) + 12d);
        else if (ramTotal <= 16d)
            SliderRamCustom.MaxValue = (int)Math.Round(Math.Floor((ramTotal - 8d) / 1d) + 25d);
        else
            SliderRamCustom.MaxValue = (int)Math.Round(Math.Floor((ramTotal - 16d) / 2d) + 33d);
        // 璁剧疆鏂囨湰
        LabRamGame.Text = $"{Lang.Number(ramGame, "N1")} GiB{(ramGame != ramGameActual ? $" ({Lang.Text("Setup.Launch.Memory.AvailableSuffix", Lang.Number(ramGameActual, "N1"))})" : "")}";
        LabRamUsed.Text = $"{Lang.Number(ramUsed, "N1")} GiB";
        LabRamTotal.Text = $" / {Lang.Number(ramTotal, "N1")} GiB";
        LabRamWarn.Visibility =
            ramGame == 1d && !ModJava.IsGameSet64BitJava(PageInstanceLeft.McInstance) && !SystemInfo.Is32BitSystem &&
            ModJava.Javas.ExistAnyJava()
                ? Visibility.Visible
                : Visibility.Collapsed;
        HintRamTooHigh.Visibility = ramGame / ramTotal > 0.75d ? Visibility.Visible : Visibility.Collapsed;
        if (showAnim)
        {
            // 瀹藉害鍔ㄧ敾
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
            // 瀹藉害璁剧疆
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
    ///     鍒锋柊 UI 涓婄殑鏂囨湰浣嶇疆銆?
    /// </summary>
    private void RefreshRamText()
    {
        // 鑾峰彇瀹藉害淇℃伅
        var rectUsedWidth = RectRamUsed.Bounds.Width;
        var totalWidth = PanRamDisplay.Bounds.Width;
        var labGameWidth = LabRamGame.Bounds.Width;
        var labUsedWidth = LabRamUsed.Bounds.Width;
        var labTotalWidth = LabRamTotal.Bounds.Width;
        var labGameTitleWidth = LabRamGameTitle.Bounds.Width;
        var labUsedTitleWidth = LabRamUsedTitle.Bounds.Width;
        // 宸︿晶
        int left;
        if (rectUsedWidth - 30d < labUsedWidth || rectUsedWidth - 30d < labUsedTitleWidth)
            // 鍏ㄥ啓涓嶄笅浜?
            left = 0;
        else if (rectUsedWidth - 25d < labUsedWidth + labTotalWidth)
            // 鏄剧ず涓嶄笅瀹屾暣鏁版嵁
            left = 1;
        else
            // 姝ｅ父
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

        // 鍙充晶
        int right;
        if (totalWidth < labGameWidth + 2d + rectUsedWidth || totalWidth < labGameTitleWidth + 2d + rectUsedWidth)
            // 鎸ゅ埌鏈€鍙宠竟
            right = 0;
        else
            // 姝ｅ父鎯呭喌
            right = 1;
        if (right == 0)
        {
            if (ModAnimation.AniControlEnabled == 0 &&
                (ramTextRight != right || ModAnimation.AniIsRun("VersionSetup Ram TextRight")))
            {
                // 闇€瑕佸姩鐢?
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
                // 涓嶉渶瑕佸姩鐢?
                LabRamGame.Margin = new Thickness(totalWidth - labGameWidth, 3d, 0d, 0d);
                LabRamGameTitle.Margin = new Thickness(totalWidth - labGameTitleWidth, 0d, 0d, 5d);
            }
        }
        else if (ModAnimation.AniControlEnabled == 0 &&
                 (ramTextRight != right || ModAnimation.AniIsRun("VersionSetup Ram TextRight")))
        {
            // 闇€瑕佸姩鐢?
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
            // 涓嶉渶瑕佸姩鐢?
            LabRamGame.Margin = new Thickness(2d + rectUsedWidth, 3d, 0d, 0d);
            LabRamGameTitle.Margin = new Thickness(2d + rectUsedWidth, 0d, 0d, 5d);
        }

        ramTextRight = right;
    }

    /// <summary>
    ///     鑾峰彇褰撳墠璁剧疆鐨?RAM 鍊笺€傚崟浣嶄负 GB銆?
    /// </summary>
    public static double GetRam(McInstance version, bool? is32BitJava = default)
    {
        var instancePath = version?.PathInstance;
        // 璺熼殢鍏ㄥ眬璁剧疆
        if (Config.Instance.MemorySolution[instancePath] == 2)
            return PageSetupLaunch.GetRam(version, true, is32BitJava);

        // ------------------------------------------
        // 淇敼涓嬫柟浠ｇ爜鏃堕渶瑕佷竴骞朵慨鏀?PageSetupLaunch
        // ------------------------------------------

        // 浣跨敤褰撳墠瀹炰緥鐨勮缃?
        var ramGive = default(double);
        if (Config.Instance.MemorySolution[instancePath] == 0)
        {
            // 鑷姩閰嶇疆
            var ramAvailable =
                Math.Round((double)(KernelInterop.GetAvailablePhysicalMemoryBytes() / 1024 / 1024 / 1024 * 10)) / 10;
            // 纭畾闇€姹傜殑鍐呭瓨鍊?
            double ramMininum; // 鏃犺濡備綍涔熼渶瑕佷繚璇佺殑鏈€浣庨檺搴﹀唴瀛?
            double ramTarget1; // 浼拌鑳藉媺寮哄甫鍔ㄤ簡鐨勫唴瀛?
            double ramTarget2; // 浼拌娌″暐闂浜嗙殑鍐呭瓨
            double ramTarget3; // 瀹夎杩囧闄勫姞缁勪欢闇€瑕佺殑鍐呭瓨
            if (version is not null && !version.IsLoaded)
                version.Load();
            if (version is not null && version.Modable)
            {
                // 鍙畨瑁?Mod 鐨勫疄渚?
                var modDir = new DirectoryInfo(version.PathIndie + @"mods\");
                var modCount = modDir.Exists ? modDir.GetFiles().Length : 0;
                ramMininum = 0.5d + modCount / 150d;
                ramTarget1 = 1.5d + modCount / 90d;
                ramTarget2 = 2.7d + modCount / 50d;
                ramTarget3 = 4.5d + modCount / 25d;
            }
            else if (version is not null && version.Info.HasOptiFine)
            {
                // OptiFine 瀹炰緥
                ramMininum = 0.5d;
                ramTarget1 = 1.5d;
                ramTarget2 = 3d;
                ramTarget3 = 5d;
            }
            else
            {
                // 鏅€氬疄渚?
                ramMininum = 0.5d;
                ramTarget1 = 1.5d;
                ramTarget2 = 2.5d;
                ramTarget3 = 4d;
            }

            double ramDelta;
            // 棰勫垎閰嶅唴瀛橈紝闃舵涓€锛? ~ T1锛?00%
            ramDelta = ramTarget1;
            ramGive += Math.Min(ramAvailable, ramDelta);
            ramAvailable -= ramDelta;
            if (ramAvailable >= 0.1d)
            {
                // 棰勫垎閰嶅唴瀛橈紝闃舵浜岋紝T1 ~ T2锛?0%
                ramDelta = ramTarget2 - ramTarget1;
                ramGive += Math.Min(ramAvailable * 0.7d, ramDelta);
                ramAvailable -= ramDelta / 0.7d;
                if (ramAvailable >= 0.1d)
                {
                    // 棰勫垎閰嶅唴瀛橈紝闃舵涓夛紝T2 ~ T3锛?0%
                    ramDelta = ramTarget3 - ramTarget2;
                    ramGive += Math.Min(ramAvailable * 0.4d, ramDelta);
                    ramAvailable -= ramDelta / 0.4d;
                    if (ramAvailable >= 0.1d)
                    {
                        // 棰勫垎閰嶅唴瀛橈紝闃舵鍥涳紝T3 ~ T3 * 2锛?5%
                        ramDelta = ramTarget3;
                        ramGive += Math.Min(ramAvailable * 0.15d, ramDelta);
                        ramAvailable -= ramDelta / 0.15d;
                    }
                }
            }

            // 涓嶄綆浜庢渶浣庡€?
            ramGive = Math.Round(Math.Max(ramGive, ramMininum), 1);
        }
        else
        {
            // 鎵嬪姩閰嶇疆
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

        // 鑻ヤ娇鐢?32 浣?Java锛屽垯闄愬埗涓?1G
        if (is32BitJava ?? !ModJava.IsGameSet64BitJava(PageInstanceLeft.McInstance))
            ramGive = Math.Min(1d, ramGive);
        return ramGive;
    }

    #endregion

    #region 鏈嶅姟鍣?

    // 鍏ㄥ眬
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
        LabServerAuthName.Visibility = type == 2 || type == 3 ? Visibility.Visible : Visibility.Collapsed;
        TextServerAuthName.Visibility = type == 2 || type == 3 ? Visibility.Visible : Visibility.Collapsed;
        LabServerAuthRegister.Visibility = type == 2 || type == 3 ? Visibility.Visible : Visibility.Collapsed;
        TextServerAuthRegister.Visibility = type == 2 || type == 3 ? Visibility.Visible : Visibility.Collapsed;
        LabServerAuthServer.Visibility = type == 2 || type == 3 ? Visibility.Visible : Visibility.Collapsed;
        TextServerAuthServer.Visibility = type == 2 || type == 3 ? Visibility.Visible : Visibility.Collapsed;
        BtnServerAuthLittle.Visibility = type == 2 || type == 3 ? Visibility.Visible : Visibility.Collapsed;
        BtnServerNewProfile.Visibility = type == 2 || type == 3 ? Visibility.Visible : Visibility.Collapsed;
        if (type == 0 || type == 1)
            BtnServerAuthLock.Visibility = Visibility.Collapsed;
        else
            BtnServerAuthLock.Visibility = Visibility.Visible;
        if (Config.InstanceAuth.AuthLocked[PageInstanceLeft.McInstance.PathInstance])
        {
            HintServerLoginLock.Visibility = Visibility.Visible;
            ComboServerLoginRequire.IsEnabled = false;
            TextServerAuthServer.IsEnabled = false;
            TextServerAuthName.IsEnabled = false;
            TextServerAuthRegister.IsEnabled = false;
            BtnServerAuthLittle.IsEnabled = false;
        }
        else
        {
            HintServerLoginLock.Visibility = Visibility.Collapsed;
            ComboServerLoginRequire.IsEnabled = true;
            TextServerAuthServer.IsEnabled = true;
            TextServerAuthName.IsEnabled = true;
            TextServerAuthRegister.IsEnabled = true;
            BtnServerAuthLittle.IsEnabled = true;
        }

        CardServer.TriggerForceResize();
        // 閬垮厤姝ｇ増楠岃瘉鍜岀绾块獙璇佸嚭鐜版鎻愮ず
        if (type != 2 && type != 3)
        {
            LabServerAuthServerSecurity.Visibility = Visibility.Collapsed;
            LabServerAuthServerSecurityCL.Visibility = Visibility.Collapsed;
            LabServerAuthServerSecurityVerify.Visibility = Visibility.Collapsed;
        }
        // 濡傛灉寮€澶翠负 http:// 缁欎簣璀﹀憡
        else if (TextServerAuthServer.Text.StartsWithF("https://"))
        {
            LabServerAuthServerSecurity.Visibility = Visibility.Collapsed;
            LabServerAuthServerSecurityVerify.Visibility = Visibility.Visible;
            LabServerAuthServerSecurityCL.Visibility = Visibility.Visible;
        }
        else if (TextServerAuthServer.Text.StartsWithF("http://"))
        {
            LabServerAuthServerSecurity.Visibility = Visibility.Visible;
            LabServerAuthServerSecurityCL.Visibility = Visibility.Visible;
            LabServerAuthServerSecurityVerify.Visibility = Visibility.Collapsed;
        }
        else
        {
            LabServerAuthServerSecurity.Visibility = Visibility.Collapsed;
            LabServerAuthServerSecurityVerify.Visibility = Visibility.Collapsed;
            LabServerAuthServerSecurityCL.Visibility = Visibility.Collapsed;
        }
    }

    // LittleSkin
    private void BtnServerAuthLittle_Click(object sender, MouseButtonEventArgs e)
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

    // 閿佸畾璁剧疆
    private void BtnServerAuthLock_Click(object sender, MouseButtonEventArgs e)
    {
        if (ModMain.MyMsgBox(
                Lang.Text("Instance.Setup.Server.LockLoginMethod.Message"),
                Lang.Text("Instance.Setup.Server.LockLoginMethod.Title"), Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel"), isWarn: true) == 1)
        {
            Config.InstanceAuth.AuthLocked[PageInstanceLeft.McInstance.PathInstance] = true;
            Reload();
        }
    }

    // 璺宠浆鏂板缓妗ｆ
    private void BtnServerNewProfile_Click(object sender, MouseButtonEventArgs e)
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
        if (sender is MyTextBox textBox) textBox.Text = textBox.Text.Replace("锛?, ":");
    }

    #endregion

    #region Java 閫夋嫨

    // 鍒锋柊 Java 涓嬫媺妗嗘樉绀?
    public void RefreshJavaComboBox()
    {
        if (ComboArgumentJava is null)
            return;

        // 鑾峰彇瀹炰緥鐨?Java 鍋忓ソ锛堝凡鍏煎鏂版棫鏍煎紡锛?
        var preference = ModJava.GetInstanceJavaPreference(PageInstanceLeft.McInstance);

        // === 1. 鍒濆鍖栧浐瀹氶€夐」锛堜娇鐢ㄧ被鍨嬪畨鍏ㄧ殑 Tag锛?===
        ComboArgumentJava.Items.Clear();

        // 閫夐」 0: 璺熼殢鍏ㄥ眬璁剧疆
        ComboArgumentJava.Items.Add(new MyComboBoxItem
        {
            Content = Lang.Text("Instance.Setup.FollowGlobal"),
            Tag = new UseGlobalPreference()
        });

        // 閫夐」 1: 鑷姩閫夋嫨
        ComboArgumentJava.Items.Add(new MyComboBoxItem
        {
            Content = Lang.Text("Instance.Setup.Options.Java.AutoSelect"),
            Tag = new AutoSelect() // Nothing 琛ㄧず鑷姩閫夋嫨
        });

        // 閫夐」 2: 鐩稿璺緞閫夐」
        MyComboBoxItem relativePathItem;
        if (preference is UseRelativePath)
        {
            var relPref = (UseRelativePath)preference;
            var absPath = Path.GetFullPath(Path.Combine(Basics.ExecutableDirectory, relPref.RelativePath));
            var javaEntry = ModJava.Javas.Get(absPath);

            if (Files.IsPathWithinDirectory(absPath, Basics.ExecutableDirectory) && javaEntry is not null &&
                javaEntry.IsEnabled)
                // 鏈夋晥璺緞锛氭樉绀哄叿浣?Java 淇℃伅
                relativePathItem = new MyComboBoxItem
                {
                    Content = Lang.Text("Instance.Setup.Options.Java.SelectRelative.WithJava", javaEntry.ToString()),
                    Tag = new UseRelativePath(relPref.RelativePath),
                    ToolTip = Lang.Text("Instance.Setup.Options.Java.RelativePathToolTip", relPref.RelativePath, absPath)
                };
            else
                // 鏃犳晥璺緞锛氭彁绀虹敤鎴烽噸鏂伴€夋嫨
                relativePathItem = new MyComboBoxItem
                {
                    Content = Lang.Text("Instance.Setup.Options.Java.SelectRelative.Invalid"),
                    Tag = new UseRelativePath(relPref.RelativePath),
                    ToolTip = Lang.Text("Instance.Setup.Options.Java.InvalidPathToolTip", absPath)
                };
        }
        else
        {
            // 鏈厤缃浉瀵硅矾寰勶細浣跨敤榛樿妯℃澘
            relativePathItem = new MyComboBoxItem
            {
                Content = Lang.Text("Instance.Setup.Options.Java.SelectRelative"),
                Tag = new UseRelativePath(@"jre\bin\java.exe"),
                ToolTip = Lang.Text("Instance.Setup.Options.Java.SelectRelativeToolTip")
            };
        }

        ComboArgumentJava.Items.Add(relativePathItem);

        // === 2. 娣诲姞鎵€鏈夊彲鐢?Java 杩愯鏃?===
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
            Config.Instance.SelectedJava[PageInstanceLeft.McInstance.PathInstance] = "浣跨敤鍏ㄥ眬璁剧疆";
            ModBase.Log(
                ex,
                "鏇存柊瀹炰緥璁剧疆 Java 涓嬫媺妗嗗け璐?,
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

        // === 3. 鏍规嵁褰撳墠鍋忓ソ璁剧疆閫変腑椤癸紙浼樺厛浣跨敤鏂版牸寮?preference锛?===
        if (preference is null)
        {
            // 鑷姩閫夋嫨
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
            // 鍦?Java 鍒楄〃涓煡鎵惧尮閰嶉」锛堜粠绱㈠紩 3 寮€濮嬶級
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

        // 闄嶇骇澶勭悊锛氭棤鍖归厤椤规椂鍥為€€鍒拌嚜鍔ㄩ€夋嫨
        if (selectedItem is null && ComboArgumentJava.Items.Count > 1)
            selectedItem = ComboArgumentJava.Items[1] as MyComboBoxItem;

        // 璁剧疆閫変腑椤?
        if (selectedItem is not null) ComboArgumentJava.SelectedItem = selectedItem;

        // === 4. 鏃犲彲鐢?Java 鏃剁殑闄嶇骇澶勭悊 ===
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

        // === 5. 鍒锋柊鍏宠仈鎺т欢 ===
        RefreshRam(true);
    }

    // 闃绘鍦ㄦ棤鏁堢姸鎬佷笅灞曞紑涓嬫媺妗?
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

    // 涓嬫媺妗嗛€夋嫨鏇存敼澶勭悊锛堜繚瀛樻柊鏍煎紡閰嶇疆锛?
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

        // 鏍规嵁 Tag 绫诲瀷鐢熸垚鍋忓ソ瀵硅薄
        if (selectedItem.Tag is null or AutoSelect)
        {
            // 鑷姩閫夋嫨锛氬瓨鍌ㄧ┖瀛楃涓?
            preference = new AutoSelect();
            logMessage = "[Java] 淇敼瀹炰緥 Java 閫夋嫨璁剧疆锛氳嚜鍔ㄩ€夋嫨";
        }
        else if (selectedItem.Tag is UseGlobalPreference)
        {
            preference = new UseGlobalPreference();
            logMessage = "[Java] 淇敼瀹炰緥 Java 閫夋嫨璁剧疆锛氳窡闅忓叏灞€璁剧疆";
        }
        else if (selectedItem.Tag is UseRelativePath)
        {
            // 鐩稿璺緞锛氶渶瑕佺敤鎴烽€夋嫨瀹為檯鏂囦欢
            var ret = SystemDialogs.SelectFile(Lang.Text("Setup.Java.SelectFile.Filter"), Lang.Text("Setup.Java.SelectFile.Title"), Basics.ExecutableDirectory);
            if (string.IsNullOrWhiteSpace(ret))
                // 鐢ㄦ埛鍙栨秷锛屼笉淇濆瓨閰嶇疆锛屼繚鎸佸師閫夋嫨
                return;

            ret = Path.GetFullPath(ret);
            var relativePath = Path.GetRelativePath(Basics.ExecutableDirectory, ret);

            // 楠岃瘉璺緞鏄惁鍦ㄥ惎鍔ㄥ櫒鐩綍鍐?
            if (!Files.IsPathWithinDirectory(relativePath, Basics.ExecutableDirectory))
            {
                HintService.Hint(Lang.Text("Instance.Setup.Options.Java.PathOutOfRange"), HintType.Error);
                return;
            }

            preference = new UseRelativePath(relativePath);
            logMessage = $"[Java] 淇敼瀹炰緥 Java 閫夋嫨璁剧疆锛氱浉瀵硅矾寰?| {relativePath}";
        }
        else if (selectedItem.Tag is JavaEntry)
        {
            var javaEntry = (JavaEntry)selectedItem.Tag;
            preference = new ExistingJava(javaEntry.Installation.JavaExePath);
            logMessage = $"[Java] 淇敼瀹炰緥 Java 閫夋嫨璁剧疆锛歿javaEntry}";
        }

        // 淇濆瓨閰嶇疆
        var json = JsonSerializer.Serialize(preference, JsonCompat.SerializerOptions);
        Config.Instance.SelectedJava[PageInstanceLeft.McInstance.PathInstance] = json;


        ModBase.Log(logMessage);
        RefreshRam(true);
    }

    #endregion

    #region 鍏朵粬璁剧疆

    // 鐗堟湰闅旂璀﹀憡
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

    // 娓告垙绐楀彛
    private void CheckArgumentTitleEmpty_Change(object sender, bool e)
    {
        TextArgumentTitle.HintText = CheckArgumentTitleEmpty.Checked == true ? Lang.Text("Common.Option.Default") : Lang.Text("Instance.Setup.FollowGlobal");
        CheckBoxChange(sender,e);
    }

    private void TextArgumentTitle_TextChanged(object sender, TextChangedEventArgs e)
    {
        CheckArgumentTitleEmpty.Visibility = TextArgumentTitle.Text.Length > 0 ? Visibility.Collapsed : Visibility.Visible;
        TextBoxChange(sender,e);
    }

    #endregion

    #region 楂樼骇璁剧疆

    private void TextAdvanceRun_TextChanged(object sender, TextChangedEventArgs e)
    {
        CheckAdvanceRunWait.Visibility = string.IsNullOrEmpty(TextAdvanceRun.Text) ? Visibility.Collapsed : Visibility.Visible;
        TextBoxChange(sender,e);
    }

    private void ComboAdvanceRenderer_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModAnimation.AniControlEnabled != 0)
            return;

        var args = e; // 杞崲浜嬩欢鍙傛暟

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
