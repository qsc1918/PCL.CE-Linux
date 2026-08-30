using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageInstanceSavesLeft : IRefreshable
{
    public static string currentSave;

    // 鍒濆鍖?
    private bool isLoad;

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (isLoad)
            return;
        isLoad = true;
    }

    private void BtnOpenFolder_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        ModBase.OpenExplorer($@"{currentSave}\");
    }

    #region 榫欑尗鐗?椤甸潰绠＄悊

    /// <summary>
    ///     褰撳墠椤甸潰鐨勭紪鍙枫€備粠 0 寮€濮嬭绠椼€?
    /// </summary>
    public FormMain.PageSubType pageID = FormMain.PageSubType.Default;

    public PageInstanceSavesLeft()
    {
        InitializeComponent();
        Loaded += Page_Loaded;
        ItemInfo.Check += PageCheck;
        ItemDatapack.Check += PageCheck;
        BtnOpenFolder.Click += BtnOpenFolder_Click;
    }

    /// <summary>
    ///     鍕鹃€変簨浠舵敼鍙橀〉闈€?
    /// </summary>
    private void PageCheck(object sender, ModBase.RouteEventArgs e)
    {
        if (sender is MyListItem item && item.Tag is not null)
            PageChange((FormMain.PageSubType)ModBase.Val(item.Tag));
    }

    public object PageGet(FormMain.PageSubType id = FormMain.PageSubType.Default)
    {
        if ((int)id == -1)
            id = pageID;
        switch (id)
        {
            case FormMain.PageSubType.VersionSavesInfo:
            {
                if (ModMain.frmInstanceSavesInfo is null)
                    ModMain.frmInstanceSavesInfo = new PageInstanceSavesInfo();
                return ModMain.frmInstanceSavesInfo;
            }
            case FormMain.PageSubType.VersionSavesDatapack:
            {
                if (ModMain.frmInstanceSavesDatapack is null)
                    ModMain.frmInstanceSavesDatapack = new PageInstanceSavesDatapack();
                return ModMain.frmInstanceSavesDatapack;
            }

            default:
            {
                throw new Exception(Lang.Text("Instance.Saves.Left.UnknownSubPage", (int)id));
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
        try
        {
            PageChangeRun((MyPageRight)PageGet(id));
            pageID = id;
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                Lang.Text("Instance.Saves.Left.SwitchFailed", (int)id),
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Saves.Left.SwitchFailed", (int)id));
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

    public void RefreshButton_Click(object sender, EventArgs e) // 鐢辫竟鏍忔寜閽尶鍚嶈皟鐢?
    {
        Refresh((FormMain.PageSubType)ModBase.Val(((MyIconButton)sender).Tag));
    }

    public void Refresh()
    {
        Refresh(ModMain.frmMain.PageCurrentSub);
    }

    public void Refresh(FormMain.PageSubType subType)
    {
        switch (subType)
        {
            case FormMain.PageSubType.VersionSavesDatapack:
            {
                if (ModMain.frmInstanceSavesDatapack is null)
                    ModMain.frmInstanceSavesDatapack = new PageInstanceSavesDatapack();
                if (ItemDatapack.Checked)
                    ModMain.frmInstanceSavesDatapack.Refresh();
                else
                    ItemDatapack.Checked = true;

                break;
            }
        }

        HintService.Hint(Lang.Text("Instance.Saves.Left.Refreshing"));
    }

    #endregion
}
