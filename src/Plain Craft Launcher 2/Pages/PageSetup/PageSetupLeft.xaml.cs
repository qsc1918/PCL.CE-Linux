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
    private bool isPageSwitched; // 婵″倹鐏夐崷?Loaded 閸撳秴鍨忛幑銏犲煂閸忔湹绮い鐢告桨閿涘奔绱扮€佃壈鍤х憴锕€褰?Loaded 閺冭泛鍟€濞嗏€冲瀼閹诡澀绔村▎?

    private void PageSetupLeft_Loaded(object sender, RoutedEventArgs e)
    {
        // 閺勵垰鎯佹径鍕艾闂呮劘妫岄惃鍕摍妞ょ敻娼?
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
        // 閼汇儵銆夐棃銏ゆ晩鐠囶垽绱濋幋鏍х毣閺堫亜濮炴潪鏂ょ礉閸掓瑧鎴风紒?
        if (isLoad && !isHiddenPage)
            return;
        isLoad = true;
        // 閸掗攱鏌婄€涙劙銆夐棃銏ゆ閽樺繑鍎忛崘?
        PageSetupUI.HiddenRefresh();
        // 闁瀚ㄧ粭顑跨娑擃亝婀悮顐ゎ洣閻劎娈戠€涙劙銆夐棃?
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

    public void Refresh(object sender, EventArgs e) // 閻㈣精绔熼弽蹇斿瘻闁筋喖灏堕崥宥堢殶閻?
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

    #region 妞ょ敻娼伴崚鍥ㄥ床

    /// <summary>
    ///     瑜版挸澧犳い鐢告桨閻ㄥ嫮绱崣鏋偓鍌欑矤瀹革箑绶氶崣鍏呯矤 0 瀵偓婵顓哥粻妞尖偓?
    /// </summary>
    public FormMain.PageSubType pageID;

    public PageSetupLeft()
    {
        InitializeComponent();
        // 闁瀚ㄧ粭顑跨娑擃亝婀悮顐ゎ洣閻劎娈戠€涙劙銆夐棃?
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
    ///     閸曢箖鈧绨ㄦ禒鑸垫暭閸欐﹢銆夐棃顫偓?
    /// </summary>
    private void PageCheck(object senderRaw, ModBase.RouteEventArgs e)
    {
        var sender = (MyListItem)senderRaw;
        // 鐏忔碍婀崚婵嗩潗閸栨牗甯舵禒璺虹潣閹勬閿涘ender.Tag 娑?Nothing閿涘奔绱扮捄瀹犵箖閸掑洦宕查敍灞肩瑬閻㈠彉绨?PageID 姒涙顓绘稉?0 閼板苯鍨忛幑銏犲煂缁楊兛绔存稉顏堛€夐棃?
        // 閼汇儰濞囬悽?IsLoaded閿涘苯鍨导姘嚤閼峰瓨膩閹风喓鍋ｉ崙璁崇瑝鐞氼偅澧界悰宀嬬礄濡剝瀚欓悙鐟板毊閸掑洦宕叉い鐢告桨閺冭绱濋幒褌娆㈤惃?IsLoaded 娑?False閿?
        if (sender.Tag is not null)
            PageChange((FormMain.PageSubType)ModBase.Val(sender.Tag));
    }

    /// <summary>
    ///     閼惧嘲褰囪ぐ鎾冲鐎佃壈鍩呴幐鍥х暰閻ㄥ嫬褰告い鐢告桨閵?
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
                throw new Exception("閺堫亞鐓￠惃鍕啎缂冾喖鐡欐い鐢告桨缁夊秶琚敍? + (int)id);
            }
        }
    }

    /// <summary>
    ///     閸掑洦宕查悳鐗堟箒妞ょ敻娼伴妴?
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
                $"閸掑洦宕查崚鍡涖€夐棃銏犮亼鐠愩儻绱橧D {(int)id}閿?,
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
        ModAnimation.AniStop("FrmMain PageChangeRight"); // 閸嬫粍顒涙稉濠氥€夐棃銏㈡畱閸欐娊銆夐棃銏犲瀼閹广垹濮╅悽浼欑礉闂冨弶顒涚€瑰啩绗岄張顒€濮╅悽璁崇鐠х柉袝閸欐垵顦垮▎?PageOnEnter
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
                // 瀵ゆ儼绻滅憴锕€褰傛い鐢告桨闁氨鏁ら崝銊ф暰閿涘奔浜掓担鍨繁閸?Loaded 娴滃娆㈡稉顓炲鏉炵晫娈戦幒褌娆㈠妞句簰婢跺嫮鎮?
                ModMain.frmMain.pageRight.Opacity = 1d;
                ModMain.frmMain.pageRight.PageOnEnter();
            }, 30, true)
        }, "PageLeft PageChange");
    }

    #endregion
}
