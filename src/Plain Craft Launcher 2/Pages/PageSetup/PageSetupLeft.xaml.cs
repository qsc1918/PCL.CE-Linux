using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using PCL.Core.App;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageSetupLeft
{
    private bool isLoad;
    private bool isPageSwitched; // 濡傛灉鍦?Loaded 鍓嶅垏鎹㈠埌鍏朵粬椤甸潰锛屼細瀵艰嚧瑙﹀彂 Loaded 鏃跺啀娆″垏鎹竴娆?

    private void PageSetupLeft_Loaded(object sender, RoutedEventArgs e)
    {
        // 鏄惁澶勪簬闅愯棌鐨勫瓙椤甸潰
        var isHiddenPage = false;
        var hide = Config.Preference.Hide;

        if (ItemLaunch.Checked && hide.SetupLaunch) isHiddenPage = true;
        if (ItemJava.Checked && hide.SetupJava) isHiddenPage = true;
        if (ItemGameManage.Checked && hide.SetupGameManage)  isHiddenPage = true;
        if (ItemGameLink.Checked && hide.SetupGameLink) isHiddenPage = true;
        if (ItemUI.Checked && hide.SetupUi) isHiddenPage = true;
        if (ItemLauncherLanguage.Checked && hide.SetupLauncherLanguage) isHiddenPage = true;
        if (ItemLauncherMisc.Checked && hide.SetupLauncherMisc) isHiddenPage = true;
        if (ItemAbout.Checked && hide.SetupAbout) isHiddenPage = true;
        if (ItemUpdate.Checked && hide.SetupUpdate) isHiddenPage = true;
        if (ItemFeedback.Checked && hide.SetupFeedback) isHiddenPage = true;
        if (ItemLog.Checked && hide.SetupLog) isHiddenPage = true;
        if (PageSetupUI.HiddenForceShow)
            isHiddenPage = false;
        // 鑻ラ〉闈㈤敊璇紝鎴栧皻鏈姞杞斤紝鍒欑户缁?
        if (isLoad && !isHiddenPage)
            return;
        isLoad = true;
        // 鍒锋柊瀛愰〉闈㈤殣钘忔儏鍐?
        PageSetupUI.HiddenRefresh();
        // 閫夋嫨绗竴涓湭琚鐢ㄧ殑瀛愰〉闈?
        if (isPageSwitched)
            return;
        var hideCfg = Config.Preference.Hide;
        if (!hideCfg.SetupLaunch) 
            ItemLaunch.SetChecked(true, false, false);
        else if (!hideCfg.SetupJava) 
            ItemJava.SetChecked(true, false, false);    
        else if (!hideCfg.SetupGameManage) 
            ItemGameManage.SetChecked(true, false, false);
        else if (!hideCfg.SetupGameLink) 
            ItemGameLink.SetChecked(true, false, false);    
        else if (!hideCfg.SetupUi) 
            ItemUI.SetChecked(true, false, false);
        else if (!hideCfg.SetupLauncherLanguage)
            ItemLauncherLanguage.SetChecked(true, false, false);
        else if (!hideCfg.SetupLauncherMisc) 
            ItemLauncherMisc.SetChecked(true, false, false);
        else if (!hideCfg.SetupAbout) 
            ItemAbout.SetChecked(true, false, false);   
        else if (!hideCfg.SetupUpdate) 
            ItemUpdate.SetChecked(true, false, false);
        else if (!hideCfg.SetupFeedback) 
            ItemFeedback.SetChecked(true, false, false);
        else if (!hideCfg.SetupLog) 
            ItemLog.SetChecked(true, false, false);
        else 
            ItemLaunch.SetChecked(true, false, false);
    }

    private void PageOtherLeft_Unloaded(object sender, RoutedEventArgs e)
    {
        isPageSwitched = false;
    }

    public void Reset(object sender, EventArgs e)
    {
        switch (ModBase.Val(((MyIconButton)sender).Tag))
        {
            case (double)FormMain.PageSubType.SetupLaunch:
            {
                if (ModMain.MyMsgBox(Lang.Text("Setup.Left.Reset.Launch.Message"), Lang.Text("Setup.Left.Reset.Title"), button2: Lang.Text("Common.Action.Cancel"), isWarn: true) == 1)
                {
                    if (ModMain.frmSetupLaunch is null)
                        ModMain.frmSetupLaunch = new PageSetupLaunch();
                    ModMain.frmSetupLaunch.Reset();
                    ItemLaunch.Checked = true;
                }

                break;
            }
            case (double)FormMain.PageSubType.SetupUI:
            {
                if (ModMain.MyMsgBox(Lang.Text("Setup.Left.Reset.Ui.Message"),
                        Lang.Text("Setup.Left.Reset.Title"), button2: Lang.Text("Common.Action.Cancel"), isWarn: true) == 1)
                {
                    if (ModMain.frmSetupUI is null)
                        ModMain.frmSetupUI = new PageSetupUI();
                    ModMain.frmSetupUI.Reset();
                    ItemUI.Checked = true;
                }

                break;
            }
            case (double)FormMain.PageSubType.SetupGameManage:
            {
                if (ModMain.MyMsgBox(Lang.Text("Setup.Left.Reset.GameManage.Message"), Lang.Text("Setup.Left.Reset.Title"), button2: Lang.Text("Common.Action.Cancel"), isWarn: true) == 1)
                {
                    if (ModMain.frmSetupGameManage is null)
                        ModMain.frmSetupGameManage = new PageSetupGameManage();
                    ModMain.frmSetupGameManage.Reset();
                    ItemGameManage.Checked = true;
                }

                break;
            }
            case (double)FormMain.PageSubType.SetupGameLink:
            {
                if (ModMain.MyMsgBox(Lang.Text("Setup.Left.Reset.GameLink.Message"), Lang.Text("Setup.Left.Reset.Title"), button2: Lang.Text("Common.Action.Cancel"), isWarn: true) == 1)
                {
                    if (ModMain.frmSetupGameLink is null)
                        ModMain.frmSetupGameLink = new PageSetupGameLink();
                    ModMain.frmSetupGameLink.Reset();
                    ItemGameLink.Checked = true;
                }

                break;
            }
            case (double)FormMain.PageSubType.SetupLauncherLanguage:
            {
                if (ModMain.MyMsgBox(Lang.Text("Setup.Left.Reset.Language.Message"), Lang.Text("Setup.Left.Reset.Title"), button2: Lang.Text("Common.Action.Cancel"), isWarn: true) == 1)
                {
                    if (ModMain.frmSetupLauncherLanguage is null)
                        ModMain.frmSetupLauncherLanguage = new PageSetupLauncherLanguage();
                    ModMain.frmSetupLauncherLanguage.Reset();
                    ItemLauncherLanguage.Checked = true;
                }

                break;
            }
            case (double)FormMain.PageSubType.SetupLauncherMisc:
            {
                if (ModMain.MyMsgBox(Lang.Text("Setup.Left.Reset.Misc.Message"), Lang.Text("Setup.Left.Reset.Title"), button2: Lang.Text("Common.Action.Cancel"), isWarn: true) == 1)
                {
                    if (ModMain.frmSetupLauncherMisc is null)
                        ModMain.frmSetupLauncherMisc = new PageSetupLauncherMisc();
                    ModMain.frmSetupLauncherMisc.Reset();
                    ItemLauncherMisc.Checked = true;
                }

                break;
            }
        }
    }

    public static void TryFeedback() // Handles ItemFeedback.Click
    {
        ModBase.RunInNewThread(() =>
        {
            if (!ModBase.CanFeedback(true))
                return;
            switch (ModMain.MyMsgBox(Lang.Text("Setup.Left.Feedback.Message"), Lang.Text("Setup.Left.Feedback.Title"),
                        Lang.Text("Setup.Left.Feedback.SubmitNew"), Lang.Text("Setup.Left.Feedback.ViewList"), Lang.Text("Common.Action.Cancel")))
            {
                case 1:
                {
                    ModBase.Feedback();
                    break;
                }
                case 2:
                {
                    ModBase.OpenWebsite("https://github.com/PCL-Community/PCL2-CE/issues/");
                    break;
                }
            }
        });
    }

    public void Refresh(object sender, EventArgs e) // 鐢辫竟鏍忔寜閽尶鍚嶈皟鐢?
    {
        switch (ModBase.Val(((MyIconButton)sender).Tag))
        {
            case (double)FormMain.PageSubType.SetupFeedback:
            {
                if (ModMain.frmSetupFeedback is not null) ModMain.frmSetupFeedback.Loader.Start(isForceRestart: true);
                ItemFeedback.Checked = true;
                break;
            }
            case (double)FormMain.PageSubType.SetupJava:
            {
                if (ModMain.frmSetupJava is not null) ModMain.frmSetupJava.loader.Start(isForceRestart: true);
                ItemJava.Checked = true;
                break;
            }
        }

        HintService.Hint(Lang.Text("Setup.Left.Refreshing"), log: false);
    }

    #region 椤甸潰鍒囨崲

    /// <summary>
    ///     褰撳墠椤甸潰鐨勭紪鍙枫€備粠宸﹀線鍙充粠 0 寮€濮嬭绠椼€?
    /// </summary>
    public FormMain.PageSubType pageID;

    public PageSetupLeft()
    {
        InitializeComponent();
        // 閫夋嫨绗竴涓湭琚鐢ㄧ殑瀛愰〉闈?
        var hideCfg = Config.Preference.Hide;
        if (!hideCfg.SetupLaunch)
            pageID = FormMain.PageSubType.SetupLaunch;
        else if (!hideCfg.SetupJava)
            pageID = FormMain.PageSubType.SetupJava;
        else if (!hideCfg.SetupGameManage)
            pageID = FormMain.PageSubType.SetupGameManage;
        else if (!hideCfg.SetupGameLink)
            pageID = FormMain.PageSubType.SetupGameLink;    
        else if (!hideCfg.SetupUi)
            pageID = FormMain.PageSubType.SetupUI;
        else if (!hideCfg.SetupLauncherLanguage)
            pageID = FormMain.PageSubType.SetupLauncherLanguage;
        else if (!hideCfg.SetupLauncherMisc)
            pageID = FormMain.PageSubType.SetupLauncherMisc;
        else if (!hideCfg.SetupAbout)
            pageID = FormMain.PageSubType.SetupAbout;        
        else if (!hideCfg.SetupUpdate)
            pageID = FormMain.PageSubType.SetupUpdate;
        else if (!hideCfg.SetupFeedback)
            pageID = FormMain.PageSubType.SetupFeedback;
        else if (!hideCfg.SetupLog)
            pageID = FormMain.PageSubType.SetupLog;
        else
            pageID = FormMain.PageSubType.SetupLaunch;
        AnimatedControl = PanItem;
        Loaded += PageSetupLeft_Loaded;
        Unloaded += PageOtherLeft_Unloaded;
    }

    /// <summary>
    ///     鍕鹃€変簨浠舵敼鍙橀〉闈€?
    /// </summary>
    private void PageCheck(object senderRaw, ModBase.RouteEventArgs e)
    {
        var sender = (MyListItem)senderRaw;
        // 灏氭湭鍒濆鍖栨帶浠跺睘鎬ф椂锛宻ender.Tag 涓?Nothing锛屼細璺宠繃鍒囨崲锛屼笖鐢变簬 PageID 榛樿涓?0 鑰屽垏鎹㈠埌绗竴涓〉闈?
        // 鑻ヤ娇鐢?IsLoaded锛屽垯浼氬鑷存ā鎷熺偣鍑讳笉琚墽琛岋紙妯℃嫙鐐瑰嚮鍒囨崲椤甸潰鏃讹紝鎺т欢鐨?IsLoaded 涓?False锛?
        if (sender.Tag is not null)
            PageChange((FormMain.PageSubType)ModBase.Val(sender.Tag));
    }

    /// <summary>
    ///     鑾峰彇褰撳墠瀵艰埅鎸囧畾鐨勫彸椤甸潰銆?
    /// </summary>
    public object PageGet(FormMain.PageSubType? id = null)
    {
        var targetID = id ?? pageID;
        switch (id)
        {
            case FormMain.PageSubType.SetupLaunch:
            {
                if (ModMain.frmSetupLaunch is null)
                    ModMain.frmSetupLaunch = new PageSetupLaunch();
                return ModMain.frmSetupLaunch;
            }
            case FormMain.PageSubType.SetupUI:
            {
                if (ModMain.frmSetupUI is null)
                    ModMain.frmSetupUI = new PageSetupUI();
                return ModMain.frmSetupUI;
            }
            case FormMain.PageSubType.SetupGameManage:
            {
                if (ModMain.frmSetupGameManage is null)
                    ModMain.frmSetupGameManage = new PageSetupGameManage();
                return ModMain.frmSetupGameManage;
            }
            case FormMain.PageSubType.SetupUpdate:
            {
                if (ModMain.frmSetupUpdate is null)
                    ModMain.frmSetupUpdate = new PageSetupUpdate();
                return ModMain.frmSetupUpdate;
            }
            case FormMain.PageSubType.SetupAbout:
            {
                if (ModMain.frmSetupAbout is null)
                    ModMain.frmSetupAbout = new PageSetupAbout();
                return ModMain.frmSetupAbout;
            }
            case FormMain.PageSubType.SetupLog:
            {
                if (ModMain.frmSetupLog is null)
                    ModMain.frmSetupLog = new PageSetupLog();
                return ModMain.frmSetupLog;
            }
            case FormMain.PageSubType.SetupFeedback:
            {
                if (ModMain.frmSetupFeedback is null)
                    ModMain.frmSetupFeedback = new PageSetupFeedback();
                return ModMain.frmSetupFeedback;
            }
            case FormMain.PageSubType.SetupGameLink:
            {
                if (ModMain.frmSetupGameLink is null)
                    ModMain.frmSetupGameLink = new PageSetupGameLink();
                return ModMain.frmSetupGameLink;
            }
            case FormMain.PageSubType.SetupLauncherLanguage:
            {
                if (ModMain.frmSetupLauncherLanguage is null)
                    ModMain.frmSetupLauncherLanguage = new PageSetupLauncherLanguage();
                return ModMain.frmSetupLauncherLanguage;
            }
            case FormMain.PageSubType.SetupLauncherMisc:
            {
                if (ModMain.frmSetupLauncherMisc is null)
                    ModMain.frmSetupLauncherMisc = new PageSetupLauncherMisc();
                return ModMain.frmSetupLauncherMisc;
            }
            case FormMain.PageSubType.SetupJava:
            {
                if (ModMain.frmSetupJava is null)
                    ModMain.frmSetupJava = new PageSetupJava();
                return ModMain.frmSetupJava;
            }

            default:
            {
                throw new Exception("鏈煡鐨勮缃瓙椤甸潰绉嶇被锛? + (int)id);
            }
        }
    }

    /// <summary>
    ///     鍒囨崲鐜版湁椤甸潰銆?
    /// </summary>
    public void PageChange(FormMain.PageSubType id)
    {
        if (pageID == id)
            return;
        ModAnimation.AniControlEnabled += 1;
        isPageSwitched = true;
        try
        {
            PageChangeRun((MyPageRight)PageGet(id));
            pageID = id;
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                $"鍒囨崲鍒嗛〉闈㈠け璐ワ紙ID {(int)id}锛?,
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Setup.Error.OperationFailed"));
        }
        finally
        {
            ModAnimation.AniControlEnabled -= 1;
        }
    }

    private static void PageChangeRun(MyPageRight target)
    {
        ModAnimation.AniStop("FrmMain PageChangeRight"); // 鍋滄涓婚〉闈㈢殑鍙抽〉闈㈠垏鎹㈠姩鐢伙紝闃叉瀹冧笌鏈姩鐢讳竴璧疯Е鍙戝娆?PageOnEnter
        if (target.Parent is not null)
            target.SetValue(ContentPresenter.ContentProperty, null);
        ModMain.frmMain.pageRight = target;
        ((MyPageRight)ModMain.frmMain.PanMainRight.Child).PageOnExit();
        ModAnimation.AniStart(new[]
        {
            ModAnimation.AaCode(() =>
            {
                ((MyPageRight)ModMain.frmMain.PanMainRight.Child).PageOnForceExit();
                ModMain.frmMain.PanMainRight.Child = ModMain.frmMain.pageRight;
                ModMain.frmMain.pageRight.Opacity = 0d;
            }, 130),
            ModAnimation.AaCode(() =>
            {
                // 寤惰繜瑙﹀彂椤甸潰閫氱敤鍔ㄧ敾锛屼互浣垮緱鍦?Loaded 浜嬩欢涓姞杞界殑鎺т欢寰椾互澶勭悊
                ModMain.frmMain.pageRight.Opacity = 1d;
                ModMain.frmMain.pageRight.PageOnEnter();
            }, 30, true)
        }, "PageLeft PageChange");
    }

    #endregion
}
