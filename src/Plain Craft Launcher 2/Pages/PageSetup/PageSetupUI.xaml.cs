using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.UI;
using PCL.Core.Utils;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageSetupUI
{
    public string[] ThemeColors => Basics.IsAprilFool 
        ? [Lang.Text("Setup.Ui.Theme.Color.SkyBlue"), Lang.Text("Setup.Ui.Theme.Color.CatBlue"), Lang.Text("Setup.Ui.Theme.Color.CrashBlue"), Lang.Text("Setup.Ui.Theme.Color.Hmcl")]
        : [Lang.Text("Setup.Ui.Theme.Color.SkyBlue"), Lang.Text("Setup.Ui.Theme.Color.CatBlue"), Lang.Text("Setup.Ui.Theme.Color.CrashBlue")];
    
    public new bool isLoaded;

    public PageSetupUI()
    {
        InitializeComponent();
        Loaded += PageSetupUI_Loaded;
        Loaded += (_, _) => HiddenRefresh();
    }

    private void PageSetupUI_Loaded(object sender, RoutedEventArgs e)
    {
        // 闁插秴顦查崝鐘烘祰闁劌鍨?
        PanBack.ScrollToHome();

        ModAnimation.AniControlEnabled += 1;
        Reload(); // #4826閿涘苯婀В蹇旑偧鏉╂稑鍙嗘い鐢告桨閺冨爼鍏橀崚閿嬫煀娑撯偓娑?
        ModAnimation.AniControlEnabled -= 1;

        // 闂堢偤鍣告径宥呭鏉炰粙鍎撮崚?
        if (isLoaded)
            return;
        isLoaded = true;

        SliderLoad();

        PanLauncherHide.IsVisible = true;
    }

    public void Reload()
    {
        try
        {
            // 閸氼垰濮╅崳?
            SliderLauncherOpacity.Value = Config.Preference.Theme.WindowOpacity;
            CheckLauncherLogo.Checked = Config.Preference.ShowStartupLogo;
            ComboDarkMode.SelectedIndex = (int)Config.Preference.Theme.ColorMode;
            ComboDarkColor.SelectedIndex = (int)Config.Preference.Theme.DarkColor;
            ComboLightColor.SelectedIndex = (int)Config.Preference.Theme.LightColor;
            CheckShowLaunchingHint.Checked = Config.Preference.ShowLaunchingHint;

            // 鐎涙ぞ缍嬬拋鍓х枂
            ComboUiFont.SelectedFontTag = Config.Preference.Font;
            ComboUiMotdFont.SelectedFontTag = Config.Preference.MotdFont;

            CheckBlur.Checked = Config.Preference.Blur.IsEnabled;
            SliderBlurValue.Value = Config.Preference.Blur.Radius;
            SliderBlurSamplingRate.Value = Config.Preference.Blur.SamplingRate;
            ComboBlurType.SelectedIndex = Config.Preference.Blur.KernelType;
            PanBlurValue.IsVisible = CheckBlur.Checked == true ? true : false;
            CheckLockWindowSize.Checked = Config.Preference.LockWindowSize;

            // 閼冲本娅欓崶鍓у
            SliderBackgroundOpacity.Value = Config.Preference.Background.WallpaperOpacity;
            SliderBackgroundBlur.Value = Config.Preference.Background.WallpaperBlurRadius;
            ComboBackgroundSuit.SelectedIndex = Config.Preference.Background.WallpaperSuitMode;
            CheckBackgroundColorful.Checked = Config.Preference.Background.BackgroundColorful;
            var autoPauseVideo = Config.Preference.Background.AutoPauseVideo;
            CheckAutoPauseVideo.Checked = autoPauseVideo;
            if (ModVideoBack.IsGaming)
                if (autoPauseVideo)
                    BtnBackgroundRefresh.IsEnabled = false;

            BackgroundRefresh(false, false);

            // 閺嶅洭顣介弽?
            ((MyRadioBox)FindName("RadioLogoType" + (int)Config.Preference.WindowTitleType))
                .Checked = true;
            CheckLogoLeft.IsVisible = RadioLogoType0.Checked ? true : false;
            PanLogoText.IsVisible = RadioLogoType2.Checked ? true : false;
            PanLogoChange.IsVisible = RadioLogoType3.Checked ? true : false;
            TextLogoText.Text = Config.Preference.WindowTitleCustomText;
            CheckLogoLeft.Checked = Config.Preference.TopBarLeftAlign;

            // 閼冲本娅欓棅鍏呯
            CheckMusicRandom.Checked = Config.Preference.Music.ShufflePlayback;
            CheckMusicAuto.Checked = Config.Preference.Music.StartOnStartup;
            CheckMusicStop.Checked = Config.Preference.Music.StopInGame;
            CheckMusicStart.Checked = Config.Preference.Music.StartInGame;
            CheckMusicSMTC.Checked = Config.Preference.Music.EnableSMTC;
            SliderMusicVolume.Value = Config.Preference.Music.Volume;
            MusicRefreshUI();

            // 娑撳銆?
            try
            {
                ComboCustomPreset.SelectedIndex = Config.Preference.Homepage.SelectedPreset;
            }
            catch
            {
                Config.Preference.Homepage.SelectedPresetConfig.Reset();
            }

            ((MyRadioBox)FindName("RadioCustomType" + Config.Preference.Homepage.Type)).Checked = true;
            TextCustomNet.Text = Config.Preference.Homepage.CustomUrl;
            ModSetup.UiCustomType(Config.Preference.Homepage.Type);

            // 閸旂喕鍏橀梾鎰
            // 閼惧嘲褰囬柊宥囩枂缂佸嫬绱╅悽?
            var uiHidden = Config.Preference.Hide;

            // 娑撳銆夐棃?
            CheckHiddenPageDownload.Checked = uiHidden.PageDownload;
            CheckHiddenPageSetup.Checked = uiHidden.PageSetup;
            CheckHiddenPageTools.Checked = uiHidden.PageTools;

            // 鐎涙劙銆夐棃?鐠佸墽鐤?
            CheckHiddenSetupLaunch.Checked = uiHidden.SetupLaunch;
            CheckHiddenSetupUI.Checked = uiHidden.SetupUi;
            CheckHiddenSetupLauncherLanguage.Checked = uiHidden.SetupLauncherLanguage;
            CheckHiddenSetupGameManage.Checked = uiHidden.SetupGameManage;
            CheckHiddenSetupJava.Checked = uiHidden.SetupJava;
            CheckHiddenLauncherMisc.Checked = uiHidden.SetupLauncherMisc;
            CheckHiddenSetupUpdate.Checked = uiHidden.SetupUpdate;
            CheckHiddenSetupGameLink.Checked = uiHidden.SetupGameLink;
            CheckHiddenSetupAbout.Checked = uiHidden.SetupAbout;
            CheckHiddenSetupFeedback.Checked = uiHidden.SetupFeedback;
            CheckHiddenSetupLog.Checked = uiHidden.SetupLog;

            // 鐎涙劙銆夐棃?瀹搞儱鍙?
            CheckHiddenToolsGameLink.Checked = uiHidden.ToolsGameLink;
            CheckHiddenToolsTest.Checked = uiHidden.ToolsTest;

            // 鐎涙劙銆夐棃?鐎圭偘绶ョ拋鍓х枂
            CheckHiddenVersionEdit.Checked = uiHidden.InstanceEdit;
            CheckHiddenVersionExport.Checked = uiHidden.InstanceExport;
            CheckHiddenVersionSave.Checked = uiHidden.InstanceSave;
            CheckHiddenVersionScreenshot.Checked = uiHidden.InstanceScreenshot;
            CheckHiddenVersionMod.Checked = uiHidden.InstanceMod;
            CheckHiddenVersionResourcePack.Checked = uiHidden.InstanceResourcePack;
            CheckHiddenVersionShader.Checked = uiHidden.InstanceShader;
            CheckHiddenVersionSchematic.Checked = uiHidden.InstanceSchematic;
            CheckHiddenVersionServer.Checked = uiHidden.InstanceServer;

            // 閻楃懓鐣鹃崝鐔诲厴
            CheckHiddenFunctionSelect.Checked = uiHidden.FunctionSelect;
            CheckHiddenFunctionModUpdate.Checked = uiHidden.FunctionModUpdate;
            CheckHiddenFunctionHidden.Checked = uiHidden.FunctionHidden;
        }
        catch (NullReferenceException ex)
        {
            ModBase.Log(
                ex,
                Lang.Text("Setup.Ui.Error.ConfigReset"),
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Setup.Ui.Error.ConfigReset"));
            Reset();
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                Lang.Text("Setup.Ui.Error.LoadFailed"),
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Setup.Ui.Error.LoadFailed"));
        }
    }

    // 閸掓繂顫愰崠?
    public void Reset()
    {
        try
        {
            Config.Preference.Reset();
            ModBase.Log("[Setup] 瀹告彃鍨垫慨瀣娑擃亝鈧冨鐠佸墽鐤嗛敍?);
            HintService.Hint(Lang.Text("Setup.Ui.Initialized"), HintType.Success, false);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                Lang.Text("Setup.Ui.Error.InitFailed"),
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Setup.Ui.Error.InitFailed"));
        }

        Reload();
    }

    // 鐏忓棙甯舵禒鑸垫暭閸欐鐭鹃悽鍗炲煂鐠佸墽鐤嗛弨鐟板綁
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
            SetByTag(sender.Tag?.ToString(), sender.SelectedIndex);
    }

    private void CheckBoxChange(object senderRaw, bool user)
    {
        var sender = (MyCheckBox)senderRaw;
        if (ModAnimation.AniControlEnabled == 0)
            SetByTag(sender.Tag?.ToString(), sender.Checked);
    }

    private void TextBoxChange(object senderRaw, RoutedEventArgs e)
    {
        var sender = (MyTextBox)senderRaw;
        if (ModAnimation.AniControlEnabled == 0)
            SetByTag(sender.Tag?.ToString(), sender.Text);
    }

    private void RadioBoxChange(object senderRaw, ModBase.RouteEventArgs e)
    {
        var sender = (MyRadioBox)senderRaw;
        var gotCfg = sender.Tag?.ToString()?.Split("/") ?? Array.Empty<string>();
        if (ModAnimation.AniControlEnabled == 0 && gotCfg.Length >= 2)
            SetByTag(gotCfg[0], int.Parse(gotCfg[1]));
    }

    private static void SetByTag(string tag, object value)
    {
        ConfigService.TrySetValue(tag, value);
    }

    private void ComboFontChange(object sender, SelectionChangedEventArgs e)
    {
        if (ModAnimation.AniControlEnabled == 0) Config.Preference.Font = ComboUiFont.SelectedFontTag;
    }

    private void ComboMotdFontChange(object sender, SelectionChangedEventArgs e)
    {
        if (ModAnimation.AniControlEnabled == 0) Config.Preference.MotdFont = ComboUiMotdFont.SelectedFontTag;
    }

    // 閼冲本娅欓崶鍓у
    private void BtnUIBgOpen_Click(object sender, PointerReleasedEventArgs e)
    {
        ModBase.OpenExplorer(ModBase.exePath + @"PCL\Pictures\");
    }

    private void BtnBackgroundRefresh_Click(object sender, PointerReleasedEventArgs e)
    {
        BackgroundRefresh(true, true);
    }

    public void BackgroundRefreshUI(bool show, int count)
    {
        if (PanBackgroundOpacity is null)
            return;
        if (show)
        {
            PanBackgroundOpacity.Visibility = true;
            PanBackgroundBlur.Visibility = true;
            PanBackgroundSuit.Visibility = true;
            BtnBackgroundClear.Visibility = true;
            CheckAutoPauseVideo.Visibility = true;
            CardBackground.Title = Lang.Text("Setup.Ui.Background.TitleWithCount", count);
        }
        else
        {
            PanBackgroundOpacity.Visibility = false;
            PanBackgroundBlur.Visibility = false;
            PanBackgroundSuit.Visibility = false;
            BtnBackgroundClear.Visibility = false;
            CheckAutoPauseVideo.Visibility = false;
            CardBackground.Title = Lang.Text("Setup.Ui.Background.TitleDefault");
        }

        CardBackground.TriggerForceResize();
    }

    private void BtnBackgroundClear_Click(object sender, PointerReleasedEventArgs e)
    {
        if (ModMain.MyMsgBox(Lang.Text("Setup.Ui.Background.Clear.Confirm.Message"),
                Lang.Text("Common.Dialog.Warning"), button2: Lang.Text("Common.Action.Cancel"),
                isWarn: true) == 1)
        {
            ModBase.DeleteDirectory(ModBase.exePath + @"PCL\Pictures");
            BackgroundRefresh(false, true);
            HintService.Hint(Lang.Text("Setup.Ui.Background.Clear.Success"), HintType.Success);
        }
    }

    /// <summary>
    ///     閸掗攱鏌婇懗灞炬珯閸ュ墽澧栭崣濠咁啎缂冾噣銆?UI閵?
    /// </summary>
    /// <param name="isHint">閺勵垰鎯侀弰鍓с仛閸掗攱鏌婇幓鎰仛閵?/param>
    /// <param name="refresh">閺勵垰鎯侀崚閿嬫煀閸ュ墽澧栭弰鍓с仛閵?/param>
    public static void BackgroundRefresh(bool isHint, bool refresh)
    {
        try
        {
            // 閼惧嘲褰囬崣顖滄暏閻ㄥ嫬娴橀悧鍥ㄦ瀮娴?
            Directory.CreateDirectory(ModBase.exePath + @"PCL\Pictures\");
            var pic = ModBase.EnumerateFiles(ModBase.exePath + @"PCL\Pictures\").Where(file =>
                    !(file.Extension.Equals(".ini", StringComparison.OrdinalIgnoreCase) ||
                      file.Extension.Equals(".db", StringComparison.OrdinalIgnoreCase))).Select(file => file.FullName)
                .ToList();

            // 鐟欏棝顣堕崝鐘烘祰瀵倸鐖舵径鍕倞

            EventHandler<ExceptionRoutedEventArgs> videoHandler = (sender, e) =>
            {
                var videoEx = e.ErrorException;
                var videoAddress = ModMain.frmMain.VideoBack.Source.ToString();
                if (ModMain.frmMain.VideoBack.Source is not null)
                {
                    ModVideoBack.VideoStop();

                    if (videoEx.Message.Contains("0xC00D109B"))
                        ModBase.Log(
                            $"""
                             閸掗攱鏌婇懗灞炬珯閸愬懎顔愭径杈Е閿涘矁顕氱憴鍡涱暥閺傚洣娆㈤崣顖濆厴楠炲爼娼?H.264閿涘湏VC閿涘鐗稿蹇嬧偓?
                             娴ｇ姴褰叉禒銉ョ毦鐠囨洑濞囬悽銊潒妫版垼娴嗛惍浣镐紣閸忛攱澧﹀鈧憴鍡涱暥閺傚洣娆㈤獮鎯邦啎鐎规氨娲伴弽鍥ㄧ壐瀵繋璐?H.264閿涘湏VC閿涘绱濋悞璺烘倵鏉烆剛鐖滅拠銉潒妫版垯鈧?
                             閺傚洣娆㈤敍姝縱ideoAddress}
                             """,
                            ModBase.LogLevel.Msgbox,
                            userSummary: Lang.Text("Setup.Ui.Error.BackgroundVideoUnsupported"));
                    else
                        ModBase.Log(
                            videoEx,
                            $"閸掗攱鏌婇懗灞炬珯閸愬懎顔愭径杈Е閿涘澖videoAddress}閿?,
                            ModBase.LogLevel.Msgbox,
                            userSummary: Lang.Text("Setup.Ui.Error.OperationFailed"));
                }
            };
            ModMain.frmMain.VideoBack.MediaFailed -= videoHandler;
            ModVideoBack.GamingStateChanged -= ModVideoBack.OnGamingStateChanged;
            ModVideoBack.ForcePlayChanged -= ModVideoBack.OnForcePlayChanged;
            ModVideoBack.GamingStateChanged += ModVideoBack.OnGamingStateChanged;
            ModVideoBack.ForcePlayChanged += ModVideoBack.OnForcePlayChanged;
            if (!Config.Preference.Background.AutoPauseVideo)
                ModVideoBack.ForcePlay = true;
            // 閸旂姾娴?
            if (pic.Count == 0)
            {
                if (refresh)
                {
                    if (ModMain.frmMain.ImgBack.IsVisible == false)
                    {
                        if (isHint)
                            HintService.Hint(Lang.Text("Setup.Ui.Background.NoAvailableContent"), HintType.Error);
                    }
                    else
                    {
                        ModMain.frmMain.ImgBack.IsVisible = false;
                        if (isHint)
                            HintService.Hint(Lang.Text("Setup.Ui.Background.Cleared"), HintType.Success);
                    }
                }

                if (ModMain.frmSetupUI is not null)
                    ModMain.frmSetupUI.BackgroundRefreshUI(false, 0);
            }
            else
            {
                if (refresh)
                {
                    var address = RandomUtils.PickRandom(pic);
                    try
                    {
                        ModMain.frmMain.ImgBack.Background = null;
                        ModVideoBack.VideoStop();
                        ModBase.Log("[UI] 閸旂姾娴囬懗灞炬珯閸愬懎顔愰敍? + address);
                        ModMain.frmMain.ImgBack.Background = new MyBitmap(address);
                        _ = Config.Preference.Background.WallpaperSuitMode;
                        ModMain.frmMain.ImgBack.Visibility = true;
                        if (isHint)
                                HintService.Hint(Lang.Text("Setup.Ui.Background.Refresh.Success", ModBase.GetFileNameFromPath(address)), HintType.Success,
                                false);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            ModMain.frmMain.VideoBack.MediaFailed += videoHandler;
                            ModBase.Log(ex, "[UI] 閸旂姾娴囬懗灞炬珯閸ュ墽澧栨径杈Е" + address);
                            if (ModBase.modeDebug)
                                HintService.Hint(Lang.Text("Setup.Ui.Background.ImageLoadFailed", address));
                            ModMain.frmMain.ImgBack.Visibility = true;
                            ModMain.frmMain.VideoBack.Source = new Uri(address, UriKind.Absolute);
                            ModVideoBack.VideoPlay();
                            if (isHint)
                            HintService.Hint(Lang.Text("Setup.Ui.Background.Refresh.Success", ModBase.GetFileNameFromPath(address)), HintType.Success,
                                    false);
                        }
                        catch (Exception playEx)
                        {
                            ModBase.Log(playEx, "閹绢厽鏂侀懗灞炬珯閸愬懎顔愰弮璺哄毉閻滅増婀惌銉╂晩鐠囶垽绱?);
                        }
                    }
                }

                if (ModMain.frmSetupUI is not null)
                    ModMain.frmSetupUI.BackgroundRefreshUI(true, pic.Count);
            }
        }

        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閸掗攱鏌婇懗灞炬珯閸愬懎顔愰弮璺哄毉閻滅増婀惌銉╂晩鐠?,
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Setup.Ui.Error.OperationFailed"));
        }
    }

    // 妞ゅ爼鍎撮弽?
    private void BtnLogoChange_Click(object sender, PointerReleasedEventArgs e)
    {
        var fileName = SystemDialogs.SelectFile(
            Lang.Text("Setup.Ui.ImageFile.Filter"),
            Lang.Text("Setup.Ui.ImageFile.SelectTitle"));
        if (string.IsNullOrEmpty(fileName))
            return;
        try
        {
            // 閹风柉绀夐弬鍥︽
            File.Delete(ModBase.exePath + @"PCL\Logo.png");
            ModBase.CopyFile(fileName, ModBase.exePath + @"PCL\Logo.png");
            // 鐠佸墽鐤嗚ぐ鎾冲閺勫墽銇?
            ModMain.frmMain.ImageTitleLogo.Source = null; // 闂冨弶顒涢崶鐘辫礋 Source 鐏炵偞鈧冨閸氬海娈戦崐鑲╂祲閸氬矁鈧奔绗夐弴瀛樻煀 (#5628)
            ModMain.frmMain.ImageTitleLogo.Source = ModBase.exePath + @"PCL\Logo.png";
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("閸欏倹鏆熼弮鐘虫櫏"))
                ModBase.Log(
                    """
                    閺€鐟板綁閺嶅洭顣介弽蹇撴禈閻楀洤銇戠拹銉礉鐠囥儱娴橀悧鍥ㄦ瀮娴犺泛褰查懗钘夎嫙闂堢偞鐖ｉ崙鍡樼壐瀵繈鈧?
                    娴ｇ姴褰叉禒銉ョ毦鐠囨洑濞囬悽銊ф暰閸ョ偓澧﹀鈧拠銉︽瀮娴犺泛鑻熼柌宥嗘煀娣囨繂鐡ㄩ敍宀冪箹娴兼俺顔€閸ュ墽澧栭崣妯硅礋閺嶅洤鍣弽鐓庣础閵?
                    """,
                    ModBase.LogLevel.Msgbox,
                    userSummary: Lang.Text("Setup.Ui.Error.TitleImageInvalidFormat"));
            else
                ModBase.Log(
                    ex,
                    "鐠佸墽鐤嗛弽鍥暯閺嶅繐娴橀悧鍥с亼鐠?,
                    ModBase.LogLevel.Msgbox,
                    userSummary: Lang.Text("Setup.Ui.Error.OperationFailed"));
            ModMain.frmMain.ImageTitleLogo.Source = null;
        }
    }

    private void RadioLogoType3_Check(object sender, ModBase.RouteEventArgs e)
    {
        if (!(ModAnimation.AniControlEnabled == 0 && e.raiseByMouse))
            return;
        Refresh: ;

        // 瀹稿弶婀侀崶鍓у閸掓瑤绗夐崘宥夆偓澶嬪
        if (File.Exists(ModBase.exePath + @"PCL\Logo.png"))
        {
            try
            {
                ModMain.frmMain.ImageTitleLogo.Source = null; // 闂冨弶顒涢崶鐘辫礋 Source 鐏炵偞鈧冨閸氬海娈戦崐鑲╂祲閸氬矁鈧奔绗夐弴瀛樻煀 (#5628)
                ModMain.frmMain.ImageTitleLogo.Source = ModBase.exePath + @"PCL\Logo.png";
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("閸欏倹鏆熼弮鐘虫櫏"))
                    ModBase.Log(
                        """
                        鐠嬪啯鏆ｉ弽鍥暯閺嶅繐娴橀悧鍥с亼鐠愩儻绱濈拠銉ユ禈閻楀洦鏋冩禒璺哄讲閼宠棄鑻熼棃鐐寸垼閸戝棙鐗稿蹇嬧偓?
                        娴ｇ姴褰叉禒銉ョ毦鐠囨洑濞囬悽銊ф暰閸ョ偓澧﹀鈧拠銉︽瀮娴犺泛鑻熼柌宥嗘煀娣囨繂鐡ㄩ敍宀冪箹娴兼俺顔€閸ュ墽澧栭崣妯硅礋閺嶅洤鍣弽鐓庣础閵?
                        """,
                        ModBase.LogLevel.Msgbox,
                        userSummary: Lang.Text("Setup.Ui.Error.TitleImageResizeInvalidFormat"));
                else
                    ModBase.Log(
                        ex,
                        "鐠嬪啯鏆ｉ弽鍥暯閺嶅繐娴橀悧鍥с亼鐠?,
                        ModBase.LogLevel.Msgbox,
                        userSummary: Lang.Text("Setup.Ui.Error.OperationFailed"));
                ModMain.frmMain.ImageTitleLogo.Source = null;
                e.handled = true;
                try
                {
                    File.Delete(ModBase.exePath + @"PCL\Logo.png");
                }
                catch (Exception exx)
                {
                    ModBase.Log(
                        exx,
                        "濞撳懐鎮婇柨娆掝嚖閻ㄥ嫭鐖ｆ０妯荤埉閸ュ墽澧栨径杈Е",
                        ModBase.LogLevel.Msgbox,
                        userSummary: Lang.Text("Setup.Ui.Error.OperationFailed"));
                }
            }

            return;
        }

        // 濞屸剝婀侀崶鍓у閸掓瑨顩﹀Ч鍌炩偓澶嬪
        var fileName = SystemDialogs.SelectFile(Lang.Text("Setup.Ui.ImageFile.Filter"), Lang.Text("Setup.Ui.ImageFile.SelectTitle"));
        if (string.IsNullOrEmpty(fileName))
        {
            ModMain.frmMain.ImageTitleLogo.Source = null;
            e.handled = true;
        }
        else
        {
            try
            {
                // 閹风柉绀夐弬鍥︽
                File.Delete(ModBase.exePath + @"PCL\Logo.png");
                ModBase.CopyFile(fileName, ModBase.exePath + @"PCL\Logo.png");
                goto Refresh;
            }
            catch (Exception ex)
            {
                ModBase.Log(
                    ex,
                    "婢跺秴鍩楅弽鍥暯閺嶅繐娴橀悧鍥с亼鐠?,
                    ModBase.LogLevel.Msgbox,
                    userSummary: Lang.Text("Setup.Ui.Error.OperationFailed"));
            }
        }
    }

    private void BtnLogoDelete_Click(object sender, PointerReleasedEventArgs e)
    {
        try
        {
            File.Delete(ModBase.exePath + @"PCL\Logo.png");
            RadioLogoType1.SetChecked(true, true);
            HintService.Hint(Lang.Text("Setup.Ui.Logo.Clear.Success"), HintType.Success);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "濞撳懐鈹栭弽鍥暯閺嶅繐娴橀悧鍥с亼鐠?,
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Setup.Ui.Error.OperationFailed"));
        }
    }

    // 閼冲本娅欓棅鍏呯
    private void BtnMusicOpen_Click(object sender, PointerReleasedEventArgs e)
    {
        ModBase.OpenExplorer(ModBase.exePath + @"PCL\Musics\");
    }

    private void BtnMusicRefresh_Click(object sender, PointerReleasedEventArgs e)
    {
        ModMusic.MusicRefreshPlay(true);
    }

    public void MusicRefreshUI()
    {
        if (PanBackgroundOpacity is null)
            return;
        if (ModMusic.musicAllList.Any())
        {
            PanMusicVolume.Visibility = true;
            PanMusicDetail.Visibility = true;
            BtnMusicClear.Visibility = true;
            CardMusic.Title = Lang.Text("Setup.Ui.Music.TitleWithCount", ModBase.EnumerateFiles(ModBase.exePath + @"PCL\Musics\").Count());
        }
        else
        {
            PanMusicVolume.Visibility = false;
            PanMusicDetail.Visibility = false;
            BtnMusicClear.Visibility = false;
            CardMusic.Title = Lang.Text("Setup.Ui.Music.Title");
        }

        CardMusic.TriggerForceResize();
    }

    private void BtnMusicClear_Click(object sender, PointerReleasedEventArgs e)
    {
        if (ModMain.MyMsgBox(Lang.Text("Setup.Ui.Music.Clear.Confirm.Message"),
                Lang.Text("Common.Dialog.Warning"), button2: Lang.Text("Common.Action.Cancel"),
                isWarn: true) == 1)
            ModBase.RunInThread(() =>
            {
                HintService.Hint(Lang.Text("Setup.Ui.Music.Deleting"));
                // 閸嬫粍顒涢幘顓熸杹闂婂厖绠?
                ModMusic.musicNAudio = null;
                ModMusic.musicWaitingList = new List<string>();
                ModMusic.musicAllList = new List<string>();
                Thread.Sleep(200);
                // 閸掔娀娅庨弬鍥︽
                try
                {
                    ModBase.DeleteDirectory(ModBase.exePath + @"PCL\Musics");
                    // DisableSMTCSupport()
                    HintService.Hint(Lang.Text("Setup.Ui.Music.Delete.Success"), HintType.Success);
                }
                catch (Exception ex)
                {
                    ModBase.Log(
                        ex,
                        "閸掔娀娅庨懗灞炬珯闂婂厖绠版径杈Е",
                        ModBase.LogLevel.Msgbox,
                        userSummary: Lang.Text("Setup.Ui.Error.OperationFailed"));
                }

                try
                {
                    Directory.CreateDirectory(ModBase.exePath + @"PCL\Musics");
                    ModBase.RunInUi(() => ModMusic.MusicRefreshPlay(false));
                }
                catch (Exception ex)
                {
                    ModBase.Log(
                        ex,
                        "闁插秴缂撻懗灞炬珯闂婂厖绠伴弬鍥︽婢剁懓銇戠拹?,
                        ModBase.LogLevel.Msgbox,
                        userSummary: Lang.Text("Setup.Ui.Error.OperationFailed"));
                }
            });
    }

    private void CheckMusicStart_Change(object sender, bool user)
    {
        CheckBoxChange(sender, user);
        if (ModAnimation.AniControlEnabled != 0)
            return;
        if (CheckMusicStart.Checked == true)
            CheckMusicStop.Checked = false;
    }

    private void CheckMusicStop_Change()
    {
        if (ModAnimation.AniControlEnabled != 0)
            return;
        if (CheckMusicStop.Checked == true)
            CheckMusicStart.Checked = false;
    }

    // 娑撳銆?

    private void BtnCustomRefresh_Click(object sender, PointerReleasedEventArgs e)
    {
        ModMain.frmLaunchRight.ForceRefresh();
        HintService.Hint(Lang.Text("Setup.Ui.Homepage.Refresh.Success"), HintType.Success);
    }

    private void BtnCustomTutorial_Click(object sender, PointerReleasedEventArgs e)
    {
        ModBase.OpenWebsite("https://docs.pclc.cc/ce/customization/xaml-format");
    }

    // 娑撳顣?
    private void ThemeColor_Change(object senderRaw, SelectionChangedEventArgs e)
    {
        var sender = (MyComboBox)senderRaw;
        SetByTag(sender.Tag?.ToString(), sender.SelectedIndex);
        ThemeManager.ThemeRefresh();
    }

    // 鐠х偛濮?
    private void BtnLauncherDonate_Click(object sender, PointerReleasedEventArgs e)
    {
        ModBase.OpenWebsite("https://afdian.com/a/LTCat");
    }

    // 濠婃垵濮╅弶?
    private void SliderLoad()
    {
        SliderMusicVolume.getHintText = new Func<object, object>(v =>
            Lang.Number(Math.Ceiling(Convert.ToDouble(v) * 0.1d) / 100d, "P0"));
        SliderLauncherOpacity.getHintText = new Func<object, object>(v =>
            Lang.Number(Math.Round(40 + Convert.ToDouble(v) * 0.1d) / 100d, "P0"));
        SliderBackgroundOpacity.getHintText = new Func<object, object>(v =>
            Lang.Number(Math.Round(Convert.ToDouble(v) * 0.1d) / 100d, "P0"));
        SliderBackgroundBlur.getHintText = new Func<object, object>(v => Lang.Text("Setup.Ui.Slider.Pixel", Lang.Number(Convert.ToDouble(v), "N0")));
        SliderBlurValue.getHintText = new Func<object, object>(v => Lang.Text("Setup.Ui.Slider.Pixel", Lang.Number(Convert.ToDouble(v), "N0")));
        SliderBlurSamplingRate.getHintText = new Func<object, object>(v => Lang.Number(Convert.ToDouble(v) / 100d, "P0"));
    }

    private void CheckMusicStart_OnChange(object sender, bool user)
    {
        CheckBoxChange(sender, user);
        CheckMusicStart_Change(sender, user);
    }

    private void CheckMusicStop_OnChange(object sender, bool user)
    {
        CheckBoxChange(sender, user);
        CheckMusicStop_Change();
    }

    #region 閸旂喕鍏橀梾鎰

    /// <summary>
    ///     閺勵垰鎯佸鍝勫煑閺勫墽銇氱悮顐ゎ洣閻劎娈戦崝鐔诲厴閵?
    /// </summary>
    public static bool HiddenForceShow
    {
        get => field;
        set
        {
            field = value;
            HiddenRefresh();
        }
    }

    /// <summary>
    ///     閺囧瓨鏌婇崝鐔诲厴闂呮劘妫岀敮锔芥降閻ㄥ嫭妯夌粈鍝勫綁閸栨牓鈧?
    /// </summary>
    public static void HiddenRefresh()
    {
        if (ModMain.frmMain.PanTitleSelect is null || !ModMain.frmMain.PanTitleSelect.IsLoaded)
            return;
        try
        {
            // 閼惧嘲褰囬柊宥囩枂缂佸嫬绱╅悽銊や簰缂傗晝鐓禒锝囩垳
            var conf = Config.Preference.Hide;

            // 妞ゅ爼鍎撮弽蹇ョ窗娑撳娴囬妴浣筋啎缂冾喓鈧礁浼愰崗?
            var isAllTitleHidden = !HiddenForceShow && conf.PageDownload && conf.PageSetup && conf.PageTools;

            if (isAllTitleHidden)
            {
                ModMain.frmMain.PanTitleSelect.IsVisible = false;
            }
            else
            {
                ModMain.frmMain.PanTitleSelect.IsVisible = true;
                ModMain.frmMain.BtnTitleSelect1.IsVisible = !HiddenForceShow && conf.PageDownload
                    ? false
                    : true;
                ModMain.frmMain.BtnTitleSelect2.IsVisible =
                    !HiddenForceShow && conf.PageSetup ? false : true;
                ModMain.frmMain.BtnTitleSelect3.IsVisible =
                    !HiddenForceShow && conf.PageTools ? false : true;
            }

            // 閸旂喕鍏橀梾鎰鐠佸墽鐤嗛崡锛勫
            if (ModMain.frmSetupUI is not null)
            {
                ModMain.frmSetupUI.CardSwitch.IsVisible = !HiddenForceShow && conf.FunctionHidden
                    ? false
                    : true;
                ModMain.frmSetupUI.CardSwitch.Title = HiddenForceShow ? Lang.Text("Setup.Ui.FeatureHide.TitleTemporarilyDisabled") : Lang.Text("Setup.Ui.FeatureHide.Title");
            }

            // 鐠佸墽鐤嗙€涙劙銆夐棃?(FrmSetupLeft)
            if (ModMain.frmSetupLeft is not null)
            {
                ModMain.frmSetupLeft.ItemLaunch.IsVisible =
                    !HiddenForceShow && conf.SetupLaunch ? false : true;
                ModMain.frmSetupLeft.ItemUI.IsVisible =
                    !HiddenForceShow && conf.SetupUi ? false : true;
                ModMain.frmSetupLeft.ItemLauncherLanguage.IsVisible = !HiddenForceShow && conf.SetupLauncherLanguage
                    ? false
                    : true;
                ModMain.frmSetupLeft.ItemGameManage.IsVisible = !HiddenForceShow && conf.SetupGameManage
                    ? false
                    : true;
                ModMain.frmSetupLeft.ItemLauncherMisc.IsVisible = !HiddenForceShow && conf.SetupLauncherMisc
                    ? false
                    : true;
                ModMain.frmSetupLeft.ItemJava.IsVisible =
                    !HiddenForceShow && conf.SetupJava ? false : true;
                ModMain.frmSetupLeft.ItemUpdate.IsVisible =
                    !HiddenForceShow && conf.SetupUpdate ? false : true;
                ModMain.frmSetupLeft.ItemGameLink.IsVisible = !HiddenForceShow && conf.SetupGameLink
                    ? false
                    : true;
                ModMain.frmSetupLeft.ItemAbout.IsVisible =
                    !HiddenForceShow && conf.SetupAbout ? false : true;
                ModMain.frmSetupLeft.ItemFeedback.IsVisible = !HiddenForceShow && conf.SetupFeedback
                    ? false
                    : true;
                ModMain.frmSetupLeft.ItemLog.IsVisible =
                    !HiddenForceShow && conf.SetupLog ? false : true;

                var categories = new[]
                {
                    (ModMain.frmSetupLeft.TextGameCategory,
                        !(conf.SetupLaunch && conf.SetupJava && conf.SetupGameManage)),
                    (ModMain.frmSetupLeft.TextToolsCategory, !conf.SetupGameLink),
                    (ModMain.frmSetupLeft.TextLauncherCategory, !(conf.SetupUi && conf.SetupLauncherLanguage && conf.SetupLauncherMisc)),
                    (ModMain.frmSetupLeft.TextAboutCategory,
                        !(conf.SetupAbout && conf.SetupUpdate && conf.SetupFeedback && conf.SetupLog))
                };

                foreach (var category in categories)
                {
                    var isVisible = category.Item2 || HiddenForceShow;
                    category.Item1.IsVisible = isVisible ? true : false;
                    if (isVisible)
                        category.Item1.Opacity = 0.6d;
                }

                // 缂佺喕顓哥拋鍓х枂妞ら潧褰查悽銊┿€嶉弫浼村櫤
                var setupCount = 0;
                if (!conf.SetupLaunch)
                    setupCount += 1;
                if (!conf.SetupUi)
                    setupCount += 1;
                if (!conf.SetupLauncherLanguage)
                    setupCount += 1;
                if (!conf.SetupGameManage)
                    setupCount += 1;
                if (!conf.SetupLauncherMisc)
                    setupCount += 1;
                if (!conf.SetupJava)
                    setupCount += 1;
                if (!conf.SetupUpdate)
                    setupCount += 1;
                if (!conf.SetupGameLink)
                    setupCount += 1;
                if (!conf.SetupAbout)
                    setupCount += 1;
                if (!conf.SetupFeedback)
                    setupCount += 1;
                if (!conf.SetupLog)
                    setupCount += 1;
                ModMain.frmSetupLeft.PanItem.IsVisible =
                    setupCount < 2 && !HiddenForceShow ? false : true;
            }

            // 瀹搞儱鍙跨€涙劙銆夐棃?(FrmToolsLeft)
            if (ModMain.frmToolsLeft is not null)
            {
                ModMain.frmToolsLeft.ItemGameLink.IsVisible = !HiddenForceShow && conf.ToolsGameLink
                    ? false
                    : true;
                ModMain.frmToolsLeft.ItemTest.IsVisible =
                    !HiddenForceShow && conf.ToolsTest ? false : true;
                
                // 婢跺嫮鎮婇崚鍡欒閺嶅洭顣?
                var isGameLinkVisible = (!HiddenForceShow && !conf.ToolsGameLink) || HiddenForceShow;
                ModMain.frmToolsLeft.TextGameLinkCategory.IsVisible = isGameLinkVisible ? true : false;
                if (isGameLinkVisible) ModMain.frmToolsLeft.TextGameLinkCategory.Opacity = 0.6;

                var isToolsVisible = (!HiddenForceShow && !conf.ToolsTest) || HiddenForceShow;
                ModMain.frmToolsLeft.TextToolsCategory.IsVisible = isToolsVisible ? true : false;
                if (isToolsVisible) ModMain.frmToolsLeft.TextToolsCategory.Opacity = 0.6;
                
                // 缂佺喕顓稿銉ュ徔妞ら潧褰查悽銊┿€嶉弫浼村櫤
                var toolsCount = 0;
                if (!conf.ToolsGameLink)
                    toolsCount += 1;
                if (!conf.ToolsTest)
                    toolsCount += 1;
                ModMain.frmToolsLeft.PanItem.IsVisible =
                    toolsCount < 2 && !HiddenForceShow ? false : true;
            }

            // 閸忔湹绮崗銉ュ經閸掗攱鏌?
            if (ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSelect)
                ModMain.frmSelectRight.BtnEmptyDownload_Loaded();
            if (ModMain.frmMain.pageCurrent == FormMain.PageType.Launch)
                ModMain.frmLaunchLeft.RefreshButtonsUI();
            if (ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSetup &&
                ModMain.frmInstanceModDisabled is not null)
                ModMain.frmInstanceModDisabled.BtnDownload_Loaded();
        }

        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閸掗攱鏌婇崝鐔诲厴闂呮劘妫屾い鍦窗婢惰精瑙?,
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Setup.Ui.Error.OperationFailed"));
        }
    }

    // ================= 鐠佸墽鐤嗘い鐢告桨閸楀繐鎮?=================
    private void HiddenSetupMain()
    {
        var isChecked = (bool)CheckHiddenPageSetup.Checked;
        CheckHiddenSetupLaunch.Checked = isChecked;
        CheckHiddenSetupUI.Checked = isChecked;
        CheckHiddenSetupLauncherLanguage.Checked = isChecked;
        CheckHiddenSetupGameManage.Checked = isChecked;
        CheckHiddenLauncherMisc.Checked = isChecked;
        CheckHiddenSetupJava.Checked = isChecked;
        CheckHiddenSetupUpdate.Checked = isChecked;
        CheckHiddenSetupGameLink.Checked = isChecked;
        CheckHiddenSetupAbout.Checked = isChecked;
        CheckHiddenSetupFeedback.Checked = isChecked;
        CheckHiddenSetupLog.Checked = isChecked;
    }

    // ================= 鐠佸墽鐤嗘い鐢告桨閸楀繐鎮?=================
    private void HiddenSetupMain(object sender, bool user)
    {
        if (!user)
            return; // 娴犲懎顦╅悶鍡欐暏閹撮鍋ｉ崙浼欑礉闂冨弶顒涘璇叉儕閻?
        var isChecked = (bool)CheckHiddenPageSetup.Checked;
        CheckHiddenSetupLaunch.Checked = isChecked;
        CheckHiddenSetupUI.Checked = isChecked;
        CheckHiddenSetupLauncherLanguage.Checked = isChecked;
        CheckHiddenSetupGameManage.Checked = isChecked;
        CheckHiddenLauncherMisc.Checked = isChecked;
        CheckHiddenSetupJava.Checked = isChecked;
        CheckHiddenSetupUpdate.Checked = isChecked;
        CheckHiddenSetupGameLink.Checked = isChecked;
        CheckHiddenSetupAbout.Checked = isChecked;
        CheckHiddenSetupFeedback.Checked = isChecked;
        CheckHiddenSetupLog.Checked = isChecked;
    }

    private void HiddenSetupSub(object sender, bool user)
    {
        if (!user)
            return;
        var conf = Config.Preference.Hide;
        // 閸掋倖鏌囬弰顖氭儊閸忋劑鍎撮崟楣冣偓?
        var allChecked = conf.SetupLaunch && conf.SetupUi && conf.SetupLauncherLanguage && conf.SetupJava &&
                         conf.SetupUpdate && conf.SetupGameLink && conf.SetupAbout && conf.SetupFeedback &&
                         conf.SetupLog && conf.SetupLauncherMisc && conf.SetupGameManage;
        CheckHiddenPageSetup.Checked = allChecked;
    }

    // ================= 瀹搞儱鍙挎い鐢告桨閸楀繐鎮?=================
    private void HiddenToolsMain(object sender, bool user)
    {
        if (!user)
            return;
        var isChecked = (bool)CheckHiddenPageTools.Checked;
        CheckHiddenToolsGameLink.Checked = isChecked;
        CheckHiddenToolsTest.Checked = isChecked;
    }

    private void HiddenToolsSub(object sender, bool user)
    {
        if (!user)
            return;
        var conf = Config.Preference.Hide;
        var allChecked = conf.ToolsGameLink && conf.ToolsTest;
        CheckHiddenPageTools.Checked = allChecked;
    }

    // 鐠€锕€鎲￠幓鎰仛
    private void HiddenHint(object sender, bool user)
    {
        if (ModAnimation.AniControlEnabled == 0 && sender is MyCheckBox checkBox && checkBox.Checked == true)
            HintService.Hint(Lang.Text("Setup.Ui.FeatureHide.TemporaryHint"));
    }

    #endregion
}
