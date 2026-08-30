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

    // 閸掓繂顫愰崠?
    private bool isLoad;

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (isLoad)
            return;
        isLoad = true;
    }

    private void BtnOpenFolder_Click(object sender, PointerReleasedEventArgs e)
    {
        e.Handled = true;
        ModBase.OpenExplorer($@"{currentSave}\");
    }

    #region 姒瑧灏楅悧?妞ょ敻娼扮粻锛勬倞

    /// <summary>
    ///     瑜版挸澧犳い鐢告桨閻ㄥ嫮绱崣鏋偓鍌欑矤 0 瀵偓婵顓哥粻妞尖偓?
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
    ///     閸曢箖鈧绨ㄦ禒鑸垫暭閸欐﹢銆夐棃顫偓?
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
    ///     閸掑洦宕查悳鐗堟箒妞ょ敻娼伴妴?
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

    public void RefreshButton_Click(object sender, EventArgs e) // 閻㈣精绔熼弽蹇斿瘻闁筋喖灏堕崥宥堢殶閻?
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
