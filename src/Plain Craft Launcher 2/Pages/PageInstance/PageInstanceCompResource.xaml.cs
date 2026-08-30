using System.IO;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using Microsoft.VisualBasic.FileIO;
using PCL.Core.App;
using PCL.Core.Logging;
using PCL.Core.UI;
using PCL.Core.UI.Theme;
using PCL.Network;
using PCL.Network.Loaders;
using FileSystem = Microsoft.VisualBasic.FileSystem;
using SearchOption = System.IO.SearchOption;
using PCL.Core.App.Localization;
using PCL.Core.Utils;

namespace PCL;

public partial class PageInstanceCompResource : IRefreshable
{
    #region 濡紕绮嶆穱鈩冧紖缂傛挸鐡?

    // 濡紕绮嶆穱鈩冧紖缂傛挸鐡?- 鐟欙絽鍠呴幒鎺戠碍閺冨爼鍣告径宥呭灡瀵ょ瘞ileInfo鐎佃壈鍤ч惃鍕偓褑鍏橀梻顕€顣?
    private readonly Dictionary<string, (DateTime CreationTime, long Length)> modFileInfoCache = new();

    public PageInstanceCompResource()
    {
        InitializeComponent();
        Unloaded += Page_Unloaded;
        Loaded += (_, _) => PageOther_Loaded();
        Initialized += (_, _) => LoaderInit();
        PageExit += UnselectedAllWithAnimation;
        Load.Click += Load_Click;
        BtnManageBack.Click += BtnManageBack_Click;
        BtnHintBack.Click += BtnHintBack_Click;
        BtnManageOpen.Click += BtnManageOpen_Click;
        BtnHintOpen.Click += BtnManageOpen_Click;
        BtnManageSelectAll.Click += BtnManageSelectAll_Click;
        BtnManageInstall.Click += BtnManageInstall_Click;
        BtnHintInstall.Click += BtnManageInstall_Click;
        BtnManageInfoExport.Click += BtnManageInfoExport_Click;
        BtnManageDownload.Click += BtnManageDownload_Click;
        BtnHintDownload.Click += BtnManageDownload_Click;
        BtnSchematicDownloadMod.Click += BtnSchematicDownloadMod_Click;
        BtnSchematicVersionSelect.Click += BtnSchematicVersionSelect_Click;
        Load.StateChanged += (_, _, _) => UnselectedAllWithAnimation();
        SearchBox.PreviewKeyDown += SearchBox_PreviewKeyDown;
        BtnFilterAll.Check += ChangeFilter;
        BtnFilterCanUpdate.Check += ChangeFilter;
        BtnFilterDisabled.Check += ChangeFilter;
        BtnFilterEnabled.Check += ChangeFilter;
        BtnFilterError.Check += ChangeFilter;
        BtnFilterDuplicate.Check += ChangeFilter;
        BtnSort.Click += BtnSortClick;
        BtnSelectEnable.Click += BtnSelectED_Click;
        BtnSelectDisable.Click += BtnSelectED_Click;
        BtnSelectUpdate.Click += BtnSelectUpdate_Click;
        BtnSelectDelete.Click += BtnSelectDelete_Click;
        BtnSelectCancel.Click += BtnSelectCancel_Click;
        BtnSelectFavorites.Click += BtnSelectFavorites_Click;
        BtnSelectShare.Click += BtnSelectShare_Click;
        SearchBox.TextChanged += SearchRun;
    }

    // 閼惧嘲褰囧Ο锛勭矋娣団剝浼呴敍鍫濈敨缂傛挸鐡ㄩ敍?
    private (DateTime CreationTime, long Length) GetModFileInfo(string path)
    {
        (DateTime CreationTime, long Length) cacheItem;
        if (modFileInfoCache.TryGetValue(path, out cacheItem)) return cacheItem;

        try
        {
            var fileInfo = new FileInfo(path);
            var newItem = (fileInfo.CreationTime, fileInfo.Length);
            if (!modFileInfoCache.ContainsKey(path)) modFileInfoCache.Add(path, newItem);
            return newItem;
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "閼惧嘲褰囧Ο锛勭矋娣団剝浼呮径杈Е: " + path);
            return (DateTime.MinValue, 0L);
        }
    }

    // 妞ょ敻娼伴崗鎶芥４閺冭埖绔婚悶鍡欑处鐎?
    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        modFileInfoCache.Clear();
    }

    #endregion

    #region 閸掓繂顫愰崠?

    private readonly ModComp.CompType currentCompType = ModComp.CompType.Mod;

    private readonly MyLocalCompItem.SwipeSelect currentSwipSelect;

    public PageInstanceCompResource(ModComp.CompType loadCompType)
    {
        currentCompType = loadCompType;
        CurrentFolderPath = ""; // 绾喕绻氶弬鍥︽婢剁鐭惧鍕潶闁插秶鐤嗘稉鐑樼壌閻╊喖缍?
        currentSwipSelect = new MyLocalCompItem.SwipeSelect { TargetFrm = this };

        // 濮濄倛鐨熼悽銊︽Ц鐠佹崘顓搁崳銊﹀韫囧懘娓堕惃鍕┾偓?
        InitializeComponent();

        // 閸?InitializeComponent() 鐠嬪啰鏁ゆ稊瀣倵濞ｈ濮炴禒璁崇秿閸掓繂顫愰崠鏍モ偓?

        if (new[] { ModComp.CompType.Shader, ModComp.CompType.ResourcePack, ModComp.CompType.Schematic }.Contains(
                currentCompType))
        {
            BtnSelectEnable.IsVisible = false;
            BtnSelectDisable.IsVisible = false;
        }

        // 閹舵洖濂栭弬鍥︽缁狅紕鎮婃い鐢告閽樺繋绗呮潪鑺ュ瘻闁?
        if (currentCompType == ModComp.CompType.Schematic)
        {
            BtnManageDownload.IsVisible = false;
            BtnHintDownload.IsVisible = false;
        }

        Unloaded += Page_Unloaded;
        Loaded += (_, _) => PageOther_Loaded();
        LoaderInit();
        PageExit += UnselectedAllWithAnimation;
        // Handles
        Load.Click += Load_Click;
        BtnManageBack.Click += BtnManageBack_Click;
        BtnHintBack.Click += BtnHintBack_Click;
        BtnManageOpen.Click += BtnManageOpen_Click;
        BtnHintOpen.Click += BtnManageOpen_Click;
        BtnManageSelectAll.Click += BtnManageSelectAll_Click;
        BtnManageInstall.Click += BtnManageInstall_Click;
        BtnHintInstall.Click += BtnManageInstall_Click;
        BtnManageDownload.Click += BtnManageDownload_Click;
        BtnHintDownload.Click += BtnManageDownload_Click;
        BtnManageInfoExport.Click += BtnManageInfoExport_Click;
        BtnSchematicDownloadMod.Click += BtnSchematicDownloadMod_Click;
        BtnSchematicVersionSelect.Click += BtnSchematicVersionSelect_Click;
        Load.StateChanged += (_, _, _) => UnselectedAllWithAnimation();
        SearchBox.PreviewKeyDown += SearchBox_PreviewKeyDown;
        BtnFilterAll.Check += ChangeFilter;
        BtnFilterCanUpdate.Check += ChangeFilter;
        BtnFilterDisabled.Check += ChangeFilter;
        BtnFilterEnabled.Check += ChangeFilter;
        BtnFilterError.Check += ChangeFilter;
        BtnFilterDuplicate.Check += ChangeFilter;
        BtnSort.Click += BtnSortClick;
        BtnSelectEnable.Click += BtnSelectED_Click;
        BtnSelectDisable.Click += BtnSelectED_Click;
        BtnSelectUpdate.Click += BtnSelectUpdate_Click;
        BtnSelectDelete.Click += BtnSelectDelete_Click;
        BtnSelectCancel.Click += BtnSelectCancel_Click;
        BtnSelectFavorites.Click += BtnSelectFavorites_Click;
        BtnSelectShare.Click += BtnSelectShare_Click;
        SearchBox.TextChanged += SearchRun;
    }

    private ModLocalComp.CompLocalLoaderData GetRequireLoaderData()
    {
        var res = new ModLocalComp.CompLocalLoaderData();
        res.gameVersion = PageInstanceLeft.McInstance;
        res.frm = this;
        var requireLoaders = new List<ModComp.CompLoaderType>();
        switch (currentCompType)
        {
            case ModComp.CompType.Mod:
            {
                requireLoaders = ModLocalComp.GetCurrentVersionModLoader();
                break;
            }
            case ModComp.CompType.ResourcePack:
            {
                requireLoaders = new[] { ModComp.CompLoaderType.Minecraft }.ToList();
                break;
            }
            case ModComp.CompType.Shader:
            {
                requireLoaders = new[]
                {
                    ModComp.CompLoaderType.OptiFine, ModComp.CompLoaderType.Iris, ModComp.CompLoaderType.Vanilla,
                    ModComp.CompLoaderType.Canvas
                }.ToList();
                break;
            }
            case ModComp.CompType.Schematic:
            {
                requireLoaders = new[] { ModComp.CompLoaderType.Minecraft }.ToList();
                break;
            }
        }

        res.loaders = requireLoaders;
        res.compPath = PageInstanceLeft.McInstance.PathIndie +
                       (PageInstanceLeft.McInstance.Info.HasLabyMod
                           ? Path.Combine("labymod-neo", "fabric", PageInstanceLeft.McInstance.Info.VanillaName)
                           : "") + ModLocalComp.GetPathNameByCompType(currentCompType) + @"\";
        res.compType = currentCompType;
        return res;
    }

    private bool isLoad;

    public void PageOther_Loaded()
    {
        CurrentFolderPath = string.Empty;

        if (ModMain.frmMain.pageLast.page != FormMain.PageType.CompDetail)
            PanBack.ScrollToHome();
        ModAnimation.AniControlEnabled += 1;
        selectedMods.Clear();
        ReloadCompFileList();
        ChangeAllSelected(false);
        ModAnimation.AniControlEnabled -= 1;

        // 闂堢偤鍣告径宥呭鏉炰粙鍎撮崚?
        if (isLoad)
            return;
        isLoad = true;

        // 濡偓閺屻儲妲搁崥锔胯礋閸樼喓鎮婇崶鍓ь吀閻炲棛鏅棃顫瑬妫ｆ牗顐奸幍鎾崇磻
        if (currentCompType == ModComp.CompType.Schematic && !States.Hint.SchematicFirstTime)
            // 閺勫墽銇氭＃鏍偧閹垫挸绱戦幓鎰仛
            ModBase.RunInUi(() =>
            {
                ModMain.MyMsgBox(Lang.Text("Instance.Saves.Folder.DoubleClickHint.Message"), Lang.Text("Instance.Saves.Folder.DoubleClickHint.Title"), Lang.Text("Common.Action.GotIt"));
                States.Hint.SchematicFirstTime = true;
            }, true);

        ModMain.frmMain.KeyDown += FrmMain_KeyDown;
        // 鐠嬪啯鏆ｉ幐澶愭尦鏉堢绐涢敍鍫ｇ箹閻溾晜鍓伴崕鎸庣梾濞夋洑绮?XAML 閺€鐧哥礆
        foreach (MyRadioButton Btn in PanFilter.Children)
            Btn.LabText.Margin = new Thickness(-2, 0d, 8d, 0d);
    }

    /// <summary>
    ///     閸掗攱鏌?Mod 閸掓銆冮妴?
    /// </summary>
    public void ReloadCompFileList(bool forceReload = false)
    {
        if (LoaderRun(forceReload
                ? ModLoader.LoaderFolderRunType.ForceRun
                : ModLoader.LoaderFolderRunType.RunOnUpdated))
        {
            ModBase.Log($"[System] 瀹告彃鍩涢弬?{currentCompType} 閸掓銆?);
            modFileInfoCache.Clear();

            ModBase.RunInUi(() =>
            {
                Filter = FilterType.All;
                PanBack.ScrollToHome();
                SearchBox.Text = "";
            });
        }
    }

    // 瀵搫鍩楅崚閿嬫煀
    private void RefreshSelf()
    {
        Refresh(currentCompType);
    }

    void IRefreshable.Refresh()
    {
        RefreshSelf();
    }

    public static void Refresh(ModComp.CompType whichPage)
    {
        // 瀵搫鍩楅崚閿嬫煀
        try
        {
            ModComp.compProjectCache.Clear();
            ModComp.compFilesCache.Clear();
            File.Delete(ModBase.pathTemp + @"Cache\LocalComp.json");
            ModBase.Log("[CompResource] 閻㈠彉绨悙鐟板毊閸掗攱鏌婇幐澶愭尦閿涘本绔婚悶鍡樻拱閸︽澘浼愮粙瀣╀繆閹垳绱︾€?);
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "瀵搫鍩楅崚閿嬫煀閺冭埖绔婚悶鍡樻拱閸︽澘浼愮粙瀣╀繆閹垳绱︾€涙ê銇戠拹?);
        }

        switch (whichPage)
        {
            case ModComp.CompType.Mod:
            {
                if (ModMain.frmInstanceMod is not null)
                    ModMain.frmInstanceMod.ReloadCompFileList(true); // 閺冪娀娓?Else閿涘矁绻曞▽鈥冲鏉炶棄鍩涙稉顏堫儣閻ㄥ嫭鏌?
                ModMain.frmInstanceLeft.ItemMod.Checked = true;
                break;
            }
            case ModComp.CompType.ResourcePack:
            {
                if (ModMain.frmInstanceResourcePack is not null)
                    ModMain.frmInstanceResourcePack.ReloadCompFileList(true);
                ModMain.frmInstanceLeft.ItemResourcePack.Checked = true;
                break;
            }
            case ModComp.CompType.Shader:
            {
                if (ModMain.frmInstanceShader is not null)
                    ModMain.frmInstanceShader.ReloadCompFileList(true);
                ModMain.frmInstanceLeft.ItemShader.Checked = true;
                break;
            }
            case ModComp.CompType.Schematic:
            {
                if (ModMain.frmInstanceSchematic is not null)
                    ModMain.frmInstanceSchematic.ReloadCompFileList(true);
                ModMain.frmInstanceLeft.ItemSchematic.Checked = true;
                break;
            }
        }

        HintService.Hint(Lang.Text("Instance.Left.Refreshing"), log: false);
    }

    private void LoaderInit()
    {
        PageLoaderInit(Load, PanLoad, PanAllBack, null, ModLocalComp.compResourceListLoader,
            _ => LoadUIFromLoaderOutput(), () => currentCompType, false);
    }

    private void Load_Click(object sender, PointerReleasedEventArgs e)
    {
        if (ModLocalComp.compResourceListLoader.State == ModBase.LoadState.Failed)
            LoaderRun(ModLoader.LoaderFolderRunType.ForceRun);
    }

    public bool LoaderRun(ModLoader.LoaderFolderRunType type)
    {
        string loadPath;
        if (string.IsNullOrEmpty(CurrentFolderPath))
            // 閸旂姾娴囬弽鍦窗瑜?
            loadPath = PageInstanceLeft.McInstance.PathIndie +
                       (PageInstanceLeft.McInstance.Info.HasLabyMod
                           ? Path.Combine("labymod-neo", "fabric", PageInstanceLeft.McInstance.Info.VanillaName)
                           : "") + ModLocalComp.GetPathNameByCompType(currentCompType) + @"\";
        else
            // 閸旂姾娴囪ぐ鎾冲閺傚洣娆㈡径?
            loadPath = CurrentFolderPath;
        return ModLoader.LoaderFolderRun(ModLocalComp.compResourceListLoader, loadPath, type,
            loaderInput: GetRequireLoaderData());
    }

    #endregion

    #region 閺傚洣娆㈡径鐟邦嚤閼?

    /// <summary>
    ///     瑜版挸澧犻弰鍓с仛閻ㄥ嫭鏋冩禒璺恒仚鐠侯垰绶為妴鍌溾敄鐎涙顑佹稉鑼躲€冪粈鐑樼壌閻╊喖缍嶉妴?
    /// </summary>
    public string CurrentFolderPath { get; set; } = "";

    /// <summary>
    ///     鏉╂稑鍙嗛幐鍥х暰閻ㄥ嫭鏋冩禒璺恒仚閵?
    /// </summary>
    private void EnterFolder(string folderPath)
    {
        try
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                HintService.Hint(Lang.Text("Instance.Saves.Folder.NotFound"), HintType.Error);
                return;
            }

            CurrentFolderPath = folderPath;
            ModBase.Log($"[閸樼喓鎮婇崶缍?鏉╂稑鍙嗛弬鍥︽婢剁櫢绱皗folderPath}");

            ModLoader.LoaderFolderRun(ModLocalComp.compResourceListLoader, folderPath,
                ModLoader.LoaderFolderRunType.ForceRun, loaderInput: GetRequireLoaderData());
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "鏉╂稑鍙嗛弬鍥︽婢剁懓銇戠拹?,
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
        }
    }

    /// <summary>
    ///     鏉╂稑鍙嗛幐鍥х暰閺傚洣娆㈡径骞库偓?
    /// </summary>
    private void EnterFolderWithCheck(string folderPath)
    {
        try
        {
            EnterFolder(folderPath);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "鏉╂稑鍙嗛弬鍥︽婢剁懓銇戠拹?,
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
        }
    }

    /// <summary>
    ///     鏉╂柨娲栨稉濠勯獓閺傚洣娆㈡径骞库偓?
    /// </summary>
    private void GoBackToParentFolder()
    {
        if (string.IsNullOrEmpty(CurrentFolderPath))
            return;

        try
        {
            // 閼惧嘲褰囬弽纭呯熅瀵?
            var rootPath = PageInstanceLeft.McInstance.PathIndie +
                           (PageInstanceLeft.McInstance.Info.HasLabyMod
                               ? Path.Combine("labymod-neo", "fabric", PageInstanceLeft.McInstance.Info.VanillaName)
                               : "") + ModLocalComp.GetPathNameByCompType(currentCompType) + @"\";
            rootPath = Path.GetFullPath(rootPath.TrimEnd('\\'));

            // 閼惧嘲褰囬悥鍓侀獓鐠侯垰绶?
            var parentPath = Directory.GetParent(CurrentFolderPath)?.FullName;

            // 婵″倹鐏夐悥鍓侀獓鐠侯垰绶炵亸杈ㄦЦ閺嶇鐭惧鍕灗閼板懐鍩楃痪褑鐭惧鍕瑝閸︺劍鐗寸捄顖氱窞閼煎啫娲块崘鍜冪礉閸掓瑨绻戦崶鐐寸壌閻╊喖缍?
            if (parentPath is null || parentPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase) ||
                !parentPath.StartsWith(rootPath + @"\", StringComparison.OrdinalIgnoreCase))
                CurrentFolderPath = "";
            else
                CurrentFolderPath = parentPath;
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "鐠侯垰绶炴径鍕倞婢惰精瑙?);
            // 閸欐垹鏁撻柨娆掝嚖閺冨墎娲块幒銉ㄧ箲閸ョ偞鐗撮惄顔肩秿
            CurrentFolderPath = "";
        }

        ModBase.Log($"[閸樼喓鎮婇崶缍?鏉╂柨娲栨稉濠勯獓閺傚洣娆㈡径鐧哥窗{(string.IsNullOrEmpty(CurrentFolderPath) ? "閺嶅湱娲拌ぐ? : CurrentFolderPath)}");

        // 闁插秵鏌婇崝鐘烘祰瑜版挸澧犻弬鍥︽婢跺湱娈戦崘鍛啇
        string loadPath;
        if (string.IsNullOrEmpty(CurrentFolderPath))
            // 鏉╂柨娲栭崚鐗堢壌閻╊喖缍?
            loadPath = PageInstanceLeft.McInstance.PathIndie +
                       (PageInstanceLeft.McInstance.Info.HasLabyMod
                           ? Path.Combine("labymod-neo", "fabric", PageInstanceLeft.McInstance.Info.VanillaName)
                           : "") + ModLocalComp.GetPathNameByCompType(currentCompType) + @"\";
        else
            // 閸旂姾娴囪ぐ鎾冲閺傚洣娆㈡径?
            loadPath = CurrentFolderPath;

        // 瀵搫鍩楅崚閿嬫煀UI閻樿埖鈧?
        // 绾喕绻氶幐澶愭尦閻樿埖鈧焦顒滅涵?
        ModBase.RunInUi(() =>
            BtnManageBack.Visibility =
                !string.IsNullOrEmpty(CurrentFolderPath) ? true : false);

        // 瀵ゆ儼绻滄稉鈧敮褍鎮楅崘宥呭鏉炴枻绱濈涵顔荤箽UI閻樿埖鈧礁鍑￠弴瀛樻煀
        ModBase.RunInUi(
            () => ModLoader.LoaderFolderRun(ModLocalComp.compResourceListLoader, loadPath,
                ModLoader.LoaderFolderRunType.ForceRun, loaderInput: GetRequireLoaderData()), true);
    }

    #endregion

    #region UI 閸?

    /// <summary>
    ///     瀹告彃濮炴潪鐣屾畱 Mod UI 缂傛挸鐡ㄩ敍灞肩瑝绾喕绻氶幐澶嬫▔缁€娲€庢惔蹇斿笓閸掓ぜ鈧竟ey 娑?Mod 閻?RawPath閵?
    /// </summary>
    public Dictionary<string, MyLocalCompItem> modItems = new();

    /// <summary>
    ///     鐏忓棗濮炴潪钘夋珤缂佹挻鐏夐惃?Mod 閸掓銆冮崝鐘烘祰娑?UI閵?
    /// </summary>
    private void LoadUIFromLoaderOutput()
    {
        try
        {
            // 閸掋倖鏌囨惔鏃囶嚉閺勫墽銇氶崫顏冪娑擃亪銆夐棃?
            if (ModLocalComp.compResourceListLoader.output.Any())
            {
                PanBack.Visibility = true;
                PanEmpty.Visibility = false;
                PanSchematicEmpty.Visibility = false;
            }
            else
            {
                // 濡偓閺屻儲妲搁崥锔胯礋閹舵洖濂栭弬鍥︽缁鐎锋稉鏀昪hematics閺傚洣娆㈡径閫涚瑝鐎涙ê婀?
                if (currentCompType == ModComp.CompType.Schematic)
                {
                    var schematicsPath = PageInstanceLeft.McInstance.PathIndie + @"schematics\";
                    if (!Directory.Exists(schematicsPath))
                    {
                        PanSchematicEmpty.Visibility = true;
                        PanEmpty.Visibility = false;
                        PanBack.Visibility = false;
                        return;
                    }
                }

                // 閺嶈宓佺紒鍕缁鐎风拋鍓х枂PanEmpty閻ㄥ嫭鏋冮張顒€鍞寸€?
                if (currentCompType == ModComp.CompType.Schematic)
                {
                    // 濡偓閺屻儲妲搁崥锕€婀€涙劖鏋冩禒璺恒仚娑?
                    if (!string.IsNullOrEmpty(CurrentFolderPath))
                    {
                        // 鐎涙劖鏋冩禒璺恒仚娑撹櫣鈹栭惃鍕絹缁€?
                        TxtEmptyTitle.Text = Lang.Text("Instance.Resource.EmptyFolder.Title");
                        TxtEmptyDescription.Text = Lang.Text("Instance.Resource.EmptyFolder.Description");
                    }
                    else
                    {
                        // 閺嶅湱娲拌ぐ鏇氳礋缁岃櫣娈戦幓鎰仛
                        TxtEmptyTitle.Text = Lang.Text("Instance.Resource.Empty.Title");
                        TxtEmptyDescription.Text = Lang.Text("Instance.Resource.Empty.Description");
                    }
                }
                else
                {
                    TxtEmptyTitle.Text = Lang.Text("Instance.Resource.Empty.Title");
                    TxtEmptyDescription.Text = Lang.Text("Instance.Resource.Empty.DescriptionWithDownload");
                }

                // 婵″倹鐏夎ぐ鎾冲閸︺劌鐡欓弬鍥︽婢堕€涜厬閿涘本妯夌粈楦跨箲閸ョ偘绗傛稉鈧痪褎瀵滈柦?
                if (!string.IsNullOrEmpty(CurrentFolderPath))
                    BtnHintBack.Visibility = true;
                else
                    BtnHintBack.Visibility = false;

                PanEmpty.Visibility = true;
                PanBack.Visibility = false;
                PanSchematicEmpty.Visibility = false;
                return;
            }

            // 娣囶喗鏁肩紓鎾崇摠
            modItems.Clear();
            var rootPath = PageInstanceLeft.McInstance.PathIndie +
                           (PageInstanceLeft.McInstance.Info.HasLabyMod
                               ? Path.Combine("labymod-neo", "fabric", PageInstanceLeft.McInstance.Info.VanillaName)
                               : "") + ModLocalComp.GetPathNameByCompType(currentCompType) + @"\";
            rootPath = Path.GetFullPath(rootPath.TrimEnd('\\'));

            var itemsToShow = ModLocalComp.compResourceListLoader.output.Where(item =>
            {
                var itemPath = item.IsFolder ? item.ActualPath : item.path;
                var parentDir = Directory.GetParent(itemPath)?.FullName;
                if (string.IsNullOrEmpty(CurrentFolderPath))
                    return parentDir.Equals(rootPath, StringComparison.OrdinalIgnoreCase);

                return parentDir.Equals(CurrentFolderPath, StringComparison.OrdinalIgnoreCase);
            }).ToList();

            foreach (var ModEntity in itemsToShow)
                modItems[ModEntity.RawPath] = BuildLocalCompItem(ModEntity);
            // 閺勫墽銇氱紒鎾寸亯
            ModBase.RunInUi(() =>
            {
                Filter = FilterType.All;
                SearchBox.Text = ""; // 鏉╂瑤绱扮憴锕€褰傜紒鎾寸亯閸掗攱鏌婇敍灞惧娴犮儵娓剁憰浣告躬 ModItems 閺囧瓨鏌婃稊瀣倵閿涘矁顕涚憴?#3124 閻ㄥ嫯顫嬫０?
                RefreshUI();
                SetSortMethod(SortMethod.CompName);
            });
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                $"閸旂姾娴?{currentCompType} 閸掓銆?UI 婢惰精瑙?,
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
        }
    }

    private MyLocalCompItem BuildLocalCompItem(ModLocalComp.LocalCompFile entry)
    {
        try
        {
            ModAnimation.AniControlEnabled += 1;
            var newItem = new MyLocalCompItem
            {
                Entry = entry,
                buttonHandler = BuildLocalCompItemBtnHandler,
                Checked = selectedMods.Contains(entry.RawPath)
            };
            newItem.CurrentSwipe = currentSwipSelect;
            newItem.Tags = entry.Tags;
            entry.OnCompUpdate += _ => newItem.Refresh();
            // AddHandler Entry.OnCompUpdate, Sub() RunInUi(Sub() DoSort())
            newItem.Refresh();
            ModAnimation.AniControlEnabled -= 1;
            return newItem;
        }
        catch (Exception ex)
        {
            ModAnimation.AniControlEnabled -= 1;
            ModBase.Log(ex, $"閸掓稑缂?UI 妞ょ懓銇戠拹銉窗{entry.RawPath}");
            throw;
        }
    }

    private void BuildLocalCompItemBtnHandler(MyLocalCompItem sender, EventArgs e)
    {
        // 閻愮懓鍤禍瀣╂
        sender.Changed += (ss, ee) => CheckChanged((MyLocalCompItem)ss, ee);
        if (sender.Entry.IsFolder)
        {
            // 閺傚洣娆㈡径褰掋€嶉惃鍕仯閸戣绨ㄦ禒璁圭窗閸欏苯鍤潻娑樺弳閺傚洣娆㈡径鐧哥礉閸楁洖鍤崚鍥ㄥ床闁鑵戦悩鑸碘偓?
            var lastClickTime = DateTime.MinValue;
            sender.Click += (sss, _) =>
            {
                var ss = (MyLocalCompItem)sss;
                var currentTime = DateTime.Now;
                var timeDiff = (currentTime - lastClickTime).TotalMilliseconds;

                if (timeDiff <= 300d)
                    // 300ms閸愬懎寮婚崙浼欑礉鏉╂稑鍙嗛弬鍥︽婢?
                    EnterFolderWithCheck(ss.Entry.ActualPath);
                else
                    // 閸楁洖鍤崚鍥ㄥ床闁鑵戦悩鑸碘偓?
                    ss.Checked = !ss.Checked;

                lastClickTime = currentTime;
            };
        }
        else
        {
            // 閺傚洣娆㈡い鍦畱閻愮懓鍤禍瀣╂閿涙艾鍨忛幑銏も偓澶夎厬閻樿埖鈧?
            sender.Click += (sss, _) =>
            {
                var ss = (MyLocalCompItem)sss;
                ss.Checked = !ss.Checked;
            };
        }

        // 閸ョ偓鐖ｉ幐澶愭尦
        var btnOpen = new MyIconButton { LogoScale = 1.05d, SvgIcon = "lucide/folder-open", Tag = sender };
        btnOpen.ToolTip = Lang.Text("Instance.Saves.OpenFileLocation");
        ToolTipService.SetPlacement(btnOpen, PlacementMode.Center);
        ToolTipService.SetVerticalOffset(btnOpen, 30d);
        ToolTipService.SetHorizontalOffset(btnOpen, 2d);
        btnOpen.Click += (ss, ee) => Open_Click((MyIconButton)ss, ee);
        var btnCont = new MyIconButton { LogoScale = 1d, SvgIcon = "lucide/info", Tag = sender };
        btnCont.ToolTip = Lang.Text("Instance.Saves.Detail");
        ToolTipService.SetPlacement(btnCont, PlacementMode.Center);
        ToolTipService.SetVerticalOffset(btnCont, 30d);
        ToolTipService.SetHorizontalOffset(btnCont, 2d);
        btnCont.Click += Info_Click;
        sender.MouseRightButtonUp += Info_Click;
        var btnDelete = new MyIconButton { LogoScale = 1d, SvgIcon = "lucide/trash-2", Tag = sender };
        btnDelete.ToolTip = Lang.Text("Common.Action.Delete");
        ToolTipService.SetPlacement(btnDelete, PlacementMode.Center);
        ToolTipService.SetVerticalOffset(btnDelete, 30d);
        ToolTipService.SetHorizontalOffset(btnDelete, 2d);
        btnDelete.Click += (ss, ee) => Delete_Click((MyIconButton)ss, ee);
        if (currentCompType != ModComp.CompType.Mod ||
            sender.Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Unavailable)
        {
            sender.Buttons = new[] { btnCont, btnOpen, btnDelete };
        }
        else
        {
            var btnED = new MyIconButton
            {
                LogoScale = 1d,
                SvgIcon = sender.Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine
                    ? "lucide/circle-minus"
                    : "lucide/circle-check",
                Tag = sender
            };
            btnED.ToolTip = sender.Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine ? Lang.Text("Instance.Resource.Disable") : Lang.Text("Instance.Resource.Enable");
            ToolTipService.SetPlacement(btnED, PlacementMode.Center);
            ToolTipService.SetVerticalOffset(btnED, 30d);
            ToolTipService.SetHorizontalOffset(btnED, 2d);
            btnED.Click += (ss, ee) => ED_Click((MyIconButton)ss, ee);
            sender.Buttons = new[] { btnCont, btnOpen, btnED, btnDelete };
        }
    }

    /// <summary>
    ///     閸掗攱鏌婇弫缈犻嚋 UI閵?
    /// </summary>
    public void RefreshUI()
    {
        if (PanList is null)
            return;
        var showingMods = (IsSearching ? searchResult : modItems.Values.Select(i => i.Entry))
            .Where(m => CanPassFilter(m)).ToList();

        // 鐎佃妯夌粈铏规畱鐠у嫭绨潻娑滎攽閹烘帒绨敍宀€鈥樻穱婵囨瀮娴犺泛銇欑純顕€銆?
        if (showingMods.Any())
        {
            var sortMethod = GetSortMethod(currentSortMethod);
            showingMods.Sort((a, b) => sortMethod(a, b));
        }

        // 闁插秵鏌婇崚妤€鍤崚妤勩€?
        ModAnimation.AniControlEnabled += 1;
        if (showingMods.Any())
        {
            PanList.IsVisible = true;
            PanList.Children.Clear();
            foreach (var TargetMod in showingMods)
            {
                if (!modItems.ContainsKey(TargetMod.RawPath))
                    continue;
                var item = modItems[TargetMod.RawPath];

                // 绾喕绻氶崗鍐濞屸剝婀侀悥璺侯啇閸ｎ煉绱濋柆鍨帳闁插秴顦插ǎ璇插瀵倸鐖?
                if (item.Parent is not null) ((Panel)item.Parent).Children.Remove(item);

                ModStyle.MinecraftFormatter.SetColorfulTextLab(item.LabTitle.Text, item.LabTitle,
                    ThemeService.IsDarkMode);
                ModStyle.MinecraftFormatter.SetColorfulTextLab(item.LabInfo.Text, item.LabInfo,
                    ThemeService.IsDarkMode);
                item.Checked = selectedMods.Contains(TargetMod.RawPath); // 閺囧瓨鏌婇柅澶夎厬閻樿埖鈧?
                PanList.Children.Add(item);
            }
        }
        else
        {
            PanList.IsVisible = false;
        }

        ModAnimation.AniControlEnabled -= 1;
        selectedMods =
            new HashSet<string>(selectedMods.Where(m => showingMods.Any(s => (s.RawPath ?? "") == (m ?? ""))));
        RefreshBars();
    }

    /// <summary>
    ///     閸掗攱鏌婃い鑸电埉閸滃苯绨抽弽蹇旀▔缁€鎭掆偓?
    /// </summary>
    public void RefreshBars()
    {
        Dispatcher.BeginInvoke(new Func<Task>(async () =>
        {
            // -----------------
            // 妞ゅ爼鍎撮弽?
            // -----------------

            // 鐠佲剝鏆?
            var anyCount = 0;
            var enabledCount = 0;
            var disabledCount = 0;
            var updateCount = 0;
            var unavalialeCount = 0;
            var itemSource = (IsSearching ? searchResult : modItems.Values.Select(i => i.Entry)).ToArray();
            await Task.Run(() =>
            {
                foreach (var item in itemSource)
                {
                    anyCount += 1;
                    if (item.CanUpdate) updateCount += 1;
                    if (item.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine) enabledCount += 1;
                    if (item.State == ModLocalComp.LocalCompFile.LocalFileStatus.Disabled) disabledCount += 1;
                    if (item.State == ModLocalComp.LocalCompFile.LocalFileStatus.Unavailable) unavalialeCount += 1;
                }
            });
            // 閺勫墽銇?
            BtnFilterAll.Text = IsSearching ? Lang.Text("Instance.Resource.Filter.SearchResult") : Lang.Text("Instance.Resource.Filter.AllWithCount", anyCount);
            BtnFilterCanUpdate.Text = Lang.Text("Instance.Resource.Filter.UpdatableWithCount", updateCount);
            BtnFilterCanUpdate.IsVisible = Filter == FilterType.CanUpdate || updateCount > 0
                ? true
                : false;
            BtnFilterEnabled.Text = Lang.Text("Instance.Resource.Filter.EnabledWithCount", enabledCount);
            BtnFilterEnabled.IsVisible = Filter == FilterType.Enabled || (enabledCount > 0 && enabledCount < anyCount)
                ? true
                : false;
            BtnFilterDisabled.Text = Lang.Text("Instance.Resource.Filter.DisabledWithCount", disabledCount);
            BtnFilterDisabled.IsVisible = Filter == FilterType.Disabled || disabledCount > 0
                ? true
                : false;
            BtnFilterError.Text = Lang.Text("Instance.Resource.Filter.ErrorWithCount", unavalialeCount);
            BtnFilterError.IsVisible = Filter == FilterType.Unavailable || unavalialeCount > 0
                ? true
                : false;
            // 閺屻儲澹橀柌宥咁槻妞ゅ湱娲?
            var duplicateItems = await Task.Run(() => itemSource.GroupBy(m =>
            {
                if (m.Comp is null) return ":Nothing:";

                return m.Comp.Id;
            }).Where(g => g.Count() > 1 && g.First().Comp is not null).SelectMany(g => g).ToList());
            BtnFilterDuplicate.Text = Lang.Text("Instance.Resource.Filter.DuplicateWithCount", duplicateItems.Count);
            BtnFilterDuplicate.IsVisible = Filter == FilterType.Duplicate || duplicateItems.Any()
                ? true
                : false;

            // 鏉╂柨娲栭幐澶愭尦閺勫墽銇氶幒褍鍩楅敍鍫濇躬鐎涙劖鏋冩禒璺恒仚娑擃厽妞傞弰鍓с仛閿?
            if (!string.IsNullOrEmpty(CurrentFolderPath))
                BtnManageBack.IsVisible = true;
            else
                BtnManageBack.IsVisible = false;

            // -----------------
            // 鎼存洟鍎撮弽?
            // -----------------

            // 鐠佲剝鏆?
            var newCount = selectedMods.Count;
            var selected = newCount > 0;
            if (selected)
                LabSelect.Text = Lang.Text("Instance.Resource.SelectedCount", newCount); // 閸欐牗绉烽幍鈧張澶愨偓澶嬪閺冩湹绗夐弴瀛樻煀閺佹澘鐡?
            // 閹稿鎸抽崣顖滄暏閹?
            if (selected)
            {
                var hasUpdate = false;
                var hasEnabled = false;
                var hasDisabled = false;
                var canFavoriteAndShare = true; // 閺勵垰鎯侀崣顖欎簰閺€鎯版閸滃苯鍨庢禍?


                // 濡偓閺屻儲妲搁崥锔藉閺堝鈧鑵戦惃鍕カ濠ф劙鍏橀張澶嬫箒閺佸牏娈戞い鍦窗娣団剝浼呴敍鍫濆祮瀹告彃鐣幋鎰粓缂冩垶娲块弬甯礆
                await Task.Run(() =>
                {
                    foreach (var ModEntity in ModLocalComp.compResourceListLoader.output)
                        if (selectedMods.Contains(ModEntity.RawPath))
                        {
                            if (ModEntity.CanUpdate) hasUpdate = true;
                            if (ModEntity.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine)
                                hasEnabled = true;
                            else if (ModEntity.State == ModLocalComp.LocalCompFile.LocalFileStatus.Disabled)
                                hasDisabled = true;
                            if (ModEntity.Comp is null || string.IsNullOrEmpty(ModEntity.Comp.Id))
                                canFavoriteAndShare = false;
                        }
                });

                BtnSelectDisable.IsEnabled = hasEnabled;
                BtnSelectEnable.IsEnabled = hasDisabled;
                BtnSelectUpdate.IsEnabled = hasUpdate;

                // 闁藉牆顕幎鏇炲閸樼喓鎮婇崶楣冩閽樺繐鍨庢禍?閺囧瓨鏌?閺€鎯版閹稿鎸?
                if (currentCompType == ModComp.CompType.Schematic)
                {
                    BtnSelectUpdate.IsVisible = false;
                    BtnSelectFavorites.IsVisible = false;
                    BtnSelectShare.IsVisible = false;
                }
                else
                {
                    BtnSelectUpdate.IsVisible = true;
                    BtnSelectFavorites.IsVisible = true;
                    BtnSelectShare.IsVisible = true;

                    // 閺嶈宓侀弰顖氭儊瀹告彃濮炴潪浠嬨€嶉惄顔讳繆閹垱娼甸崥顖滄暏/缁備胶鏁ら弨鎯版閸滃苯鍨庢禍顐ｅ瘻闁?
                    BtnSelectFavorites.IsEnabled = canFavoriteAndShare;
                    BtnSelectShare.IsEnabled = canFavoriteAndShare;
                }
            }

            // 閺囧瓨鏌婇弰鍓с仛閻樿埖鈧?
            if (ModAnimation.AniControlEnabled == 0)
            {
                PanListBack.Margin = new Thickness(0d, 0d, 0d, selected ? 95 : 15);
                if (selected)
                {
                    // 娴犲懎婀弫浼村櫤婢х偛濮為弮鑸垫尡閺€鎯у毉閻?鐠哄疇绌崝銊ф暰
                    if (bottomBarShownCount >= newCount)
                    {
                        bottomBarShownCount = newCount;
                        return;
                    }

                    bottomBarShownCount = newCount;
                    // 閸戣櫣骞?鐠哄疇绌崝銊ф暰
                    CardSelect.IsVisible = true;
                    ModAnimation.AniStart(
                        new[]
                        {
                            ModAnimation.AaOpacity(CardSelect, 1d - CardSelect.Opacity, 60),
                            ModAnimation.AaTranslateY(CardSelect, -27 - TransSelect.Y, 120,
                                ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)),
                            ModAnimation.AaTranslateY(CardSelect, 3d, 150, 120,
                                new ModAnimation.AniEaseInoutFluent(ModAnimation.AniEasePower.Weak)),
                            ModAnimation.AaTranslateY(CardSelect, -1, 90, 270,
                                new ModAnimation.AniEaseInoutFluent(ModAnimation.AniEasePower.Weak))
                        }, "Mod Sidebar");
                }
                else
                {
                    // 娑撳秹鍣告径宥嗘尡閺€楣冩閽樺繐濮╅悽?
                    if (bottomBarShownCount == 0)
                        return;
                    bottomBarShownCount = 0;
                    // 闂呮劘妫岄崝銊ф暰
                    ModAnimation.AniStart(
                        new[]
                        {
                            ModAnimation.AaOpacity(CardSelect, -CardSelect.Opacity, 90),
                            ModAnimation.AaTranslateY(CardSelect, -10 - TransSelect.Y, 90,
                                ease: new ModAnimation.AniEaseInFluent(ModAnimation.AniEasePower.Weak)),
                            ModAnimation.AaCode(() => CardSelect.IsVisible = false, after: true)
                        }, "Mod Sidebar");
                }
            }
            else
            {
                ModAnimation.AniStop("Mod Sidebar");
                bottomBarShownCount = newCount;
                if (selected)
                {
                    CardSelect.IsVisible = true;
                    CardSelect.Opacity = 1d;
                    TransSelect.Y = -25;
                }
                else
                {
                    CardSelect.IsVisible = false;
                    CardSelect.Opacity = 0d;
                    TransSelect.Y = -10;
                }
            }
        }));
    }

    private int bottomBarShownCount;

    #endregion

    #region 缁狅紕鎮?

    /// <summary>
    ///     閹垫挸绱?Mods 閺傚洣娆㈡径骞库偓?
    /// </summary>
    private void BtnManageBack_Click(object sender, EventArgs e)
    {
        GoBackToParentFolder();
    }

    private void BtnHintBack_Click(object sender, EventArgs e)
    {
        GoBackToParentFolder();
    }

    private void BtnManageOpen_Click(object sender, EventArgs e)
    {
        try
        {
            string compFilePath;

            // 婵″倹鐏夎ぐ鎾冲閸︺劌鐡欓弬鍥︽婢堕€涜厬閿涘苯鍨幍鎾崇磻瑜版挸澧犵€涙劖鏋冩禒璺恒仚閿涙稑鎯侀崚娆愬ⅵ瀵偓閺嶅湱娲拌ぐ?
            if (string.IsNullOrEmpty(CurrentFolderPath))
                // 閹垫挸绱戦弽鍦窗瑜?
                compFilePath = PageInstanceLeft.McInstance.PathIndie +
                               (PageInstanceLeft.McInstance.Info.HasLabyMod
                                   ? Path.Combine("labymod-neo", "fabric", PageInstanceLeft.McInstance.Info.VanillaName)
                                   : "") + ModLocalComp.GetPathNameByCompType(currentCompType) + @"\";
            else
                // 閹垫挸绱戣ぐ鎾冲鐎涙劖鏋冩禒璺恒仚
                compFilePath = CurrentFolderPath.EndsWith(@"\") ? CurrentFolderPath : CurrentFolderPath + @"\";
            Directory.CreateDirectory(compFilePath);
            ModBase.OpenExplorer(compFilePath);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閹垫挸绱?Mods 閺傚洣娆㈡径鐟般亼鐠?,
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
        }
    }


    /// <summary>
    ///     閸忋劑鈧鈧?
    /// </summary>
    private void BtnManageSelectAll_Click(object sender, PointerReleasedEventArgs e)
    {
        ChangeAllSelected(selectedMods.Count < PanList.Children.Count);
    }

    /// <summary>
    ///     鐎瑰顥?Mod閵?
    /// </summary>
    private void BtnManageInstall_Click(object sender, PointerReleasedEventArgs e)
    {
        string[] fileList = null;
        switch (currentCompType)
        {
            case ModComp.CompType.Mod:
            {
                fileList = SystemDialogs.SelectFiles(
                    Lang.Text("Instance.Resource.Install.FileDialog.Mod.Filter"),
                    Lang.Text("Instance.Resource.Install.FileDialog.Mod.Title"));
                break;
            }
            case ModComp.CompType.ResourcePack:
            {
                fileList = SystemDialogs.SelectFiles(
                    Lang.Text("Instance.Resource.Install.FileDialog.ResourcePack.Filter"),
                    Lang.Text("Instance.Resource.Install.FileDialog.ResourcePack.Title"));
                break;
            }
            case ModComp.CompType.Shader:
            {
                fileList = SystemDialogs.SelectFiles(
                    Lang.Text("Instance.Resource.Install.FileDialog.Shader.Filter"),
                    Lang.Text("Instance.Resource.Install.FileDialog.Shader.Title"));
                break;
            }
            case ModComp.CompType.Schematic:
            {
                fileList = SystemDialogs.SelectFiles(
                    Lang.Text("Instance.Resource.Install.FileDialog.Schematic.Filter"),
                    Lang.Text("Instance.Resource.Install.FileDialog.Schematic.Title"));
                break;
            }
        }

        if (fileList is null || !fileList.Any())
            return;
        InstallCompFiles(fileList, currentCompType, CurrentFolderPath);
    }

    /// <summary>
    ///     鐏忔繆鐦€瑰顥?Mod閵?
    ///     鏉╂柨娲栨潏鎾冲弳閻ㄥ嫭鏋冩禒鑸垫Ц閸氾缚璐熸稉鈧稉?Mod 閺傚洣娆㈤敍灞肩矌閻劋绨崚銈嗘焽閹锋牗瀚跨悰灞艰礋閵?
    /// </summary>
    public static bool InstallMods(IEnumerable<string> filePathList)
    {
        if (!filePathList.Any()) return false;

        // 1. Check file extension
        var firstFile = filePathList.First();
        var extension = firstFile.Split('.').LastOrDefault()?.ToLower();
        string[] allowedExtensions = { "jar", "litemod", "disabled", "old" };

        if (!allowedExtensions.Contains(extension)) return false;

        LogWrapper.Info("[System] 閺傚洣娆㈤弽鐓庣础娑?jar/litemod閿涘苯鐨剧拠鏇炵暔鐟佸懍璐?Mod");

        // 2. Check recycle bin
        if (firstFile.Contains(@":\$RECYCLE.BIN\"))
        {
            HintWrapper.Show(Lang.Text("Instance.Resource.Install.RestoreFromRecycleBin"), HintTheme.Error);
            return true;
        }

        // 3. Determine target instance
        var targetInstance = ModInstanceList.McMcInstanceSelected;
        if (ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSetup) targetInstance = PageInstanceLeft.McInstance;

        // 4. Validate instance status
        if (ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSelect || targetInstance is null ||
            !targetInstance.Modable)
        {
            HintWrapper.Show(Lang.Text("Instance.Resource.Install.SelectModableInstance"));
            return true;
        }

        // 5. Check if user confirmation is required
        var isModPage = ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSetup &&
                        ModMain.frmMain.PageCurrentSub == FormMain.PageSubType.VersionMod;

        if (!isModPage)
        {
            if (ModMain.MyMsgBox(Lang.Text("Instance.Resource.Install.ModConfirm.Message", targetInstance.Name), Lang.Text("Instance.Resource.Install.ModConfirm.Title"), Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel")) !=
                1) return true;
        }

        // 6. Execution: Install Mods
        ExecuteModInstallation(targetInstance, filePathList, isModPage);

        return true;
    }

    private static void ExecuteModInstallation(McInstance targetMcInstance, IEnumerable<string> filePathList,
        bool refreshList)
    {
        // Path resolution logic
        var modPathSuffix = targetMcInstance.Info.HasLabyMod
            ? $@"labymod-neo\fabric\{targetMcInstance.Info.VanillaName}\"
            : "";
        var modFolder = $@"{targetMcInstance.PathIndie}{modPathSuffix}mods\";

        try
        {
            foreach (var modFile in filePathList)
            {
                var fileName = ModBase.GetFileNameFromPath(modFile)
                    .Replace(".disabled", "")
                    .Replace(".old", "");

                if (!fileName.Contains(".")) fileName += ".jar"; // Ensure extension (#4227)

                ModBase.CopyFile(modFile, Path.Combine(modFolder, fileName));
            }

            // Success hint
            if (filePathList.Count() == 1)
            {
                var installedName = ModBase.GetFileNameFromPath(filePathList.First()).Replace(".disabled", "")
                    .Replace(".old", "");
                HintWrapper.Show(Lang.Text("Instance.Resource.Install.SuccessSingle", installedName), HintTheme.Success);
            }
            else
            {
                HintWrapper.Show(Lang.Text("Instance.Resource.Install.SuccessMultiple", filePathList.Count(), Lang.Text("Download.Comp.Type.Mod")), HintTheme.Success);
            }

            // 7. Refresh list if necessary
            if (refreshList)
                ModLoader.LoaderFolderRun(ModLocalComp.compResourceListLoader,
                    modFolder,
                    ModLoader.LoaderFolderRunType.ForceRun,
                    loaderInput: ModMain.frmInstanceMod.GetRequireLoaderData()
                );
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "閹风柉绀夐弬鍥︽婢惰精瑙?);
        }
    }

    /// <summary>
    ///     鐎瑰顥婄紒鍕閺傚洣娆㈤敍鍦d閵嗕浇绁┃鎰瘶閵嗕礁鍘滆ぐ鍗炲瘶閵嗕焦濮囪ぐ杈ㄦ瀮娴犲墎鐡戦敍澶堚偓?
    /// </summary>
    public static void InstallCompFiles(IEnumerable<string> filePathList, ModComp.CompType compType,
        string targetFolderPath = "")
    {
        if (!filePathList.Any())
            return;

        var extension = filePathList.First().AfterLast(".").ToLower();
        string[] validExtensions = null;
        var compTypeName = "";
        var compFolder = "";

        // 濡偓閺屻儱娲栭弨鍓佺彲閿涙艾娲栭弨鍓佺彲娑擃厾娈戦弬鍥︽閺堝鏁婄拠顖滄畱閺傚洣娆㈤崥?
        if (filePathList.First().Contains(@":\$RECYCLE.BIN\"))
        {
            HintService.Hint(Lang.Text("Instance.Resource.Install.RestoreFromRecycleBin"), HintType.Error);
            return;
        }

        // 閼惧嘲褰囬獮鑸殿梾閺屻儳娲伴弽鍥х杽娓?
        var targetInstance = ModInstanceList.McMcInstanceSelected;
        if (ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSetup)
            targetInstance = PageInstanceLeft.McInstance;

        // 閺嶈宓佺紒鍕缁鐎风拋鍓х枂閻╃鍙ч崣鍌涙殶
        switch (compType)
        {
            case ModComp.CompType.Mod:
            {
                validExtensions = new[] { "jar", "litemod", "disabled", "old" };
                compTypeName = "Mod";
                if (string.IsNullOrEmpty(targetFolderPath))
                    compFolder = targetInstance.PathIndie +
                                 (targetInstance.Info.HasLabyMod
                                     ? Path.Combine("labymod-neo", "fabric", targetInstance.Info.VanillaName)
                                     : "") + @"mods\";
                else
                    compFolder = targetFolderPath;

                break;
            }
            case ModComp.CompType.ResourcePack:
            {
                validExtensions = new[] { "zip" };
                compTypeName = Lang.Text("Download.Comp.Type.ResourcePack");
                if (string.IsNullOrEmpty(targetFolderPath))
                    compFolder = targetInstance.PathIndie + @"resourcepacks\";
                else
                    compFolder = targetFolderPath;

                break;
            }
            case ModComp.CompType.Shader:
            {
                validExtensions = new[] { "zip" };
                compTypeName = Lang.Text("Download.Comp.Type.Shader");
                if (string.IsNullOrEmpty(targetFolderPath))
                    compFolder = targetInstance.PathIndie + @"shaderpacks\";
                else
                    compFolder = targetFolderPath;

                break;
            }
            case ModComp.CompType.Schematic:
            {
                validExtensions = new[] { "litematic", "nbt", "schematic", "schem" };
                compTypeName = Lang.Text("Download.Comp.Type.Schematic");
                if (string.IsNullOrEmpty(targetFolderPath))
                    compFolder = targetInstance.PathIndie + @"schematics\";
                else
                    compFolder = targetFolderPath;

                break;
            }
        }

        // 濡偓閺屻儲鏋冩禒鑸靛⒖鐏炴洖鎮?
        if (!validExtensions.Contains(extension))
        {
            HintService.Hint(Lang.Text("Instance.Resource.Install.UnsupportedFormat", extension, compTypeName, string.Join(", ", validExtensions)),
                HintType.Error);
            return;
        }

        ModBase.Log($"[System] 閺傚洣娆㈡稉?{extension} 閺嶇厧绱￠敍灞界毦鐠囨洑缍旀稉绨昪ompTypeName}鐎瑰顥?);

        // 濡偓閺屻儱鐤勬笟瀣悑鐎硅鈧?
        if (compType == ModComp.CompType.Mod && (ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSelect ||
                                                 targetInstance is null || !targetInstance.Modable))
        {
            HintService.Hint(Lang.Text("Instance.Resource.Install.SelectModableInstance"));
            return;
        }

        // 绾喛顓荤€瑰顥?
        var currentPage = FormMain.PageSubType.VersionMod;
        switch (compType)
        {
            case ModComp.CompType.Mod:
            {
                currentPage = FormMain.PageSubType.VersionMod;
                break;
            }
            case ModComp.CompType.ResourcePack:
            {
                currentPage = FormMain.PageSubType.VersionResourcePack;
                break;
            }
            case ModComp.CompType.Shader:
            {
                currentPage = FormMain.PageSubType.VersionShader;
                break;
            }
            case ModComp.CompType.Schematic:
            {
                currentPage = FormMain.PageSubType.VersionSchematic;
                break;
            }
        }

        if (!(ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSetup &&
              ModMain.frmMain.PageCurrentSub == currentPage))
            if (ModMain.MyMsgBox(
                    Lang.Text("Instance.Resource.Install.GenericConfirm.Message", compTypeName, targetInstance.Name),
                    Lang.Text("Instance.Resource.Install.GenericConfirm.Title", compTypeName), Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel")) != 1)
                return;

        // 閹笛嗩攽鐎瑰顥?
        try
        {
            Directory.CreateDirectory(compFolder);
            foreach (var FilePath in filePathList)
            {
                var newFileName = ModBase.GetFileNameFromPath(FilePath);
                if (compType == ModComp.CompType.Mod)
                {
                    newFileName = newFileName.Replace(".disabled", "").Replace(".old", "");
                    if (!newFileName.Contains("."))
                        newFileName += ".jar";
                }

                var destFile = compFolder + newFileName;
                if (File.Exists(destFile))
                    if (ModMain.MyMsgBox(Lang.Text("Instance.Resource.Install.OverwriteConfirm.Message", newFileName), Lang.Text("Instance.Resource.Install.OverwriteConfirm.Title"), Lang.Text("Common.Action.Overwrite"), Lang.Text("Common.Action.Cancel")) != 1)
                        continue;

                ModBase.CopyFile(FilePath, destFile);
            }

            if (filePathList.Count() == 1)
                HintService.Hint(Lang.Text("Instance.Resource.Install.SuccessSingle", ModBase.GetFileNameFromPath(filePathList.First())), HintType.Success);
            else
                HintService.Hint(Lang.Text("Instance.Resource.Install.SuccessMultiple", filePathList.Count(), compTypeName), HintType.Success);

            // 閸掗攱鏌婇崚妤勩€?
            if (ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSetup &&
                ModMain.frmMain.PageCurrentSub == currentPage)
                switch (compType)
                {
                    case ModComp.CompType.Mod:
                    {
                        if (ModMain.frmInstanceMod is not null)
                            ModLoader.LoaderFolderRun(ModLocalComp.compResourceListLoader, compFolder,
                                ModLoader.LoaderFolderRunType.ForceRun,
                                loaderInput: ModMain.frmInstanceMod?.GetRequireLoaderData());

                        break;
                    }
                    case ModComp.CompType.ResourcePack:
                    case ModComp.CompType.Shader:
                    case ModComp.CompType.Schematic:
                    {
                        var currentForm = GetCurrentCompResourceForm();
                        if (currentForm is not null) ModBase.RunInUi(() => currentForm.ReloadCompFileList(true));

                        break;
                    }
                }
        }

        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                $"婢跺秴鍩梴compTypeName}閺傚洣娆㈡径杈Е",
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
        }
    }

    /// <summary>
    ///     閼惧嘲褰囪ぐ鎾冲閻ㄥ嫮绮嶆禒鎯扮カ濠ф劗顓搁悶鍡欑崶娴ｆ挶鈧?
    /// </summary>
    private static PageInstanceCompResource GetCurrentCompResourceForm()
    {
        switch (ModMain.frmMain.PageCurrentSub)
        {
            case FormMain.PageSubType.VersionMod:
            {
                return ModMain.frmInstanceMod;
            }
            case FormMain.PageSubType.VersionResourcePack:
            {
                return ModMain.frmInstanceResourcePack;
            }
            case FormMain.PageSubType.VersionShader:
            {
                return ModMain.frmInstanceShader;
            }
            case FormMain.PageSubType.VersionSchematic:
            {
                return ModMain.frmInstanceSchematic;
            }

            default:
            {
                return null;
            }
        }
    }

    private void BtnManageInfoExport_Click(object sender, PointerReleasedEventArgs e)
    {
        var choice =
            ModMain.MyMsgBox(
                Lang.Text("Instance.Resource.Export.Mode.Message"), Lang.Text("Instance.Resource.Export.Mode.Title"), Lang.Text("Instance.Resource.Export.Mode.Txt"), Lang.Text("Instance.Resource.Export.Mode.Csv"), Lang.Text("Common.Action.Cancel"));

        void ExportText(string content, string fileName)
        {
            try
            {
                var savePath =
                    SystemDialogs.SelectSaveFile(Lang.Text("Instance.Resource.Export.SelectSaveLocation"), fileName, Lang.Text("Instance.Resource.Export.FilesFilter"));
                if (string.IsNullOrWhiteSpace(savePath)) return;
                File.WriteAllText(savePath, content, Encoding.UTF8);
                ModBase.OpenExplorer(savePath);
            }
            catch (Exception ex)
            {
                ModBase.Log(
                    ex,
                    "鐎电厧鍤挧鍕爱娣団剝浼呮径杈Е",
                    ModBase.LogLevel.Msgbox,
                    userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
            }
        }

        ;
        switch (choice)
        {
            case 1: // TXT
            {
                var exportContent = new List<string>();
                foreach (var ModEntity in ModLocalComp.compResourceListLoader.output)
                {
                    exportContent.Add(ModEntity.FileName);
                    _AppendEmbeddedForExport(exportContent, ModEntity.EmbeddedMods, 1);
                }
                ExportText(exportContent.Join("\r\n"), PageInstanceLeft.McInstance.Name + "瀹告彃鐣ㄧ憗鍛畱鐠у嫭绨穱鈩冧紖.txt");
                break;
            }

            case 2: // CSV
            {
                var exportContent = new List<string>();
                exportContent.Add("閺傚洣娆㈤崥?鐠у嫭绨崥宥囆?鐠у嫭绨悧鍫熸拱,濮濄倗澧楅張顒佹纯閺傜増妞傞梻?Mod ID,鐎电懓绨查獮鍐插酱瀹搞儳鈻?ID,閺傚洣娆㈡径褍鐨敍鍫濈摟閼哄偊绱?閺傚洣娆㈢捄顖氱窞,閸愬懎绁靛Ο锛勭矋");
                foreach (var ModEntity in ModLocalComp.compResourceListLoader.output)
                    exportContent.Add(
                        $"{ModEntity.FileName},{ModEntity.Comp?.TranslatedName},{ModEntity.Version},{ModEntity.compFile?.ReleaseDate},{ModEntity.ModId},{ModEntity.Comp?.Id},{GetModFileInfo(ModEntity.path).Length},{ModEntity.path},{string.Join(";", _FlattenEmbeddedNames(ModEntity.EmbeddedMods))}");
                ExportText(exportContent.Join("\r\n"), PageInstanceLeft.McInstance.Name + "瀹告彃鐣ㄧ憗鍛畱鐠у嫭绨穱鈩冧紖.csv");
                break;
            }
        }
    }

    private static void _AppendEmbeddedForExport(List<string> lines, List<ModLocalComp.LocalCompFile> mods, int depth)
    {
        var indent = new string('\t', depth);
        foreach (var mod in mods)
        {
            var line = indent + "閳?" + (mod.Name ?? mod.ModId ?? mod.FileName);
            if (!string.IsNullOrWhiteSpace(mod.Version))
                line += $" ({mod.Version})";
            lines.Add(line);
            if (mod.EmbeddedMods is { Count: > 0 })
                _AppendEmbeddedForExport(lines, mod.EmbeddedMods, depth + 1);
        }
    }

    private static IEnumerable<string> _FlattenEmbeddedNames(List<ModLocalComp.LocalCompFile> mods)
    {
        foreach (var mod in mods)
        {
            yield return mod.Name ?? mod.ModId ?? mod.FileName;
            if (mod.EmbeddedMods is { Count: > 0 })
                foreach (var child in _FlattenEmbeddedNames(mod.EmbeddedMods))
                    yield return child;
        }
    }

    /// <summary>
    ///     娑撳娴?Mod閵?
    /// </summary>
    private void BtnManageDownload_Click(object sender, PointerReleasedEventArgs e)
    {
        switch (currentCompType)
        {
            case ModComp.CompType.Mod:
            {
                ModMain.frmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadMod);
                break;
            }
            case ModComp.CompType.ResourcePack:
            {
                ModMain.frmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadResourcePack);
                break;
            }
            case ModComp.CompType.Shader:
            {
                ModMain.frmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadShader);
                break;
            }
        }

        PageComp.targetVersion = PageInstanceLeft.McInstance; // 鐏忓棗缍嬮崜宥呯杽娓氬顔曠純顔昏礋缁涙盯鈧娅?
    }

    /// <summary>
    ///     娑撳娴囬幎鏇炲Mod閹稿鎸抽悙鐟板毊娴滃娆㈤妴?
    /// </summary>
    private void BtnSchematicDownloadMod_Click(object sender, PointerReleasedEventArgs e)
    {
        ModMain.frmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadMod);
        PageComp.targetVersion = PageInstanceLeft.McInstance; // 鐏忓棗缍嬮崜宥呯杽娓氬顔曠純顔昏礋缁涙盯鈧娅?
    }

    /// <summary>
    ///     鐎圭偘绶ラ柅澶嬪閹稿鎸抽悙鐟板毊娴滃娆㈤妴?
    /// </summary>
    private void BtnSchematicVersionSelect_Click(object sender, PointerReleasedEventArgs e)
    {
        ModMain.frmMain.PageChange(FormMain.PageType.Launch);
        ModMain.frmMain.PageChange(FormMain.PageType.InstanceSelect);
    }

    #endregion

    #region 闁瀚?

    /// <summary>
    ///     闁瀚ㄩ惃?Mod 閻ㄥ嫯鐭惧鍕剁礄娑撳秴鎯?.disabled 閸?.old閿涘鈧?
    /// </summary>
    public HashSet<string> selectedMods = new();

    // 閸楁洟銆嶉崚鍥ㄥ床闁瀚ㄩ悩鑸碘偓?
    public void CheckChanged(MyLocalCompItem sender, ModBase.RouteEventArgs e)
    {
        if (ModAnimation.AniControlEnabled != 0)
            return;
        // 閺囧瓨鏌婇柅澶嬪娴滃棛娈戦崘鍛啇
        var selectedKey = sender.Entry.RawPath;
        if (sender.Checked)
            selectedMods.Add(selectedKey);
        else
            selectedMods.Remove(selectedKey);
        RefreshBars();
    }

    // 閸掑洦宕查幍鈧張澶愩€嶉惃鍕偓澶嬪閻樿埖鈧?
    private void ChangeAllSelected(bool value)
    {
        ModAnimation.AniControlEnabled += 1;
        selectedMods.Clear();
        foreach (var Item in modItems.Values)
        {
            // #4992閿涘od 娴犲氦绻冨銈呮珤閻褰查懗鎴掔瑝鎼存柨婀崚妤勩€冩稉顓ㄧ礉娴ｅ棗娲滄稉鍝勫灠閸掑洦宕查悩鑸碘偓浣瑰娴犮儰绶烽悞鏈电箽閻ｆ瑥婀崚妤勩€冩稉顓ㄧ礉閹碘偓娴犮儱绨茬拠銉ょ矤閸掓銆?UI 閸掋倖鏌囬敍宀冣偓宀勬姜娴犲氦绻冨銈呮珤閸掋倖鏌?
            var shouldSelected = value && PanList.Children.Contains(Item);
            Item.Checked = shouldSelected;
            if (shouldSelected)
                selectedMods.Add(Item.Entry.RawPath);
        }

        ModAnimation.AniControlEnabled -= 1;
        RefreshBars();
    }

    private void UnselectedAllWithAnimation()
    {
        var cacheAniControlEnabled = ModAnimation.AniControlEnabled;
        ModAnimation.AniControlEnabled = 0;
        ChangeAllSelected(false);
        ModAnimation.AniControlEnabled += cacheAniControlEnabled;
    }

    private void FrmMain_KeyDown(object sender, KeyEventArgs e) // 閼汇儳娲冮崥顒冨殰瀹歌京娈戞禍瀣╂閸掓瑥婀潻娑樺弳妞ょ敻娼伴崥搴ㄦ付閻愮懓鍤崣鍏呮櫠閹貉傛閹靛秴褰查惄鎴濇儔閸?(#4311)
    {
        if (!ReferenceEquals(ModMain.frmMain.pageRight, this))
            return;
        if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && e.Key == Key.A)
            ChangeAllSelected(true);
    }

    private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Ctrl + A 娴兼俺顫﹂幖婊呭偍濡楀棙宕熼懢鍑ょ礉鐎佃壈鍤ч弮鐘崇《閸忋劑鈧绱濋幍鈧禒銉ユ躬閹稿绗?Ctrl + A 閺冩儼娴嗙粔鑽ゅ妽閻愰€涗簰娓氭寧宕熼懢?
        if (SearchBox.Text.Any())
            return;
        if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && e.Key == Key.A)
            PanBack.Focus();
    }

    #endregion

    #region 缁涙盯鈧?

    public FilterType Filter
    {
        get => field;
        set
        {
            if (field == value)
                return;
            field = value;
            switch (value)
            {
                case FilterType.All:
                {
                    BtnFilterAll.Checked = true;
                    break;
                }
                case FilterType.Enabled:
                {
                    BtnFilterEnabled.Checked = true;
                    break;
                }
                case FilterType.Disabled:
                {
                    BtnFilterDisabled.Checked = true;
                    break;
                }
                case FilterType.CanUpdate:
                {
                    BtnFilterCanUpdate.Checked = true;
                    break;
                }
                case FilterType.Duplicate:
                {
                    BtnFilterDuplicate.Checked = true;
                    break;
                }

                default:
                {
                    BtnFilterError.Checked = true;
                    break;
                }
            }

            RefreshUI();
        }
    } = FilterType.All;

    public enum FilterType
    {
        All = 0,
        Enabled = 1,
        Disabled = 2,
        CanUpdate = 3,
        Unavailable = 4,
        Duplicate = 5
    }

    /// <summary>
    ///     濡偓閺屻儴顕?Mod 妞よ妲搁崥锔绢儊閸氬牆缍嬮崜宥囩摣闁娈戠猾璇插焼閵?
    /// </summary>
    private bool CanPassFilter(ModLocalComp.LocalCompFile checkingMod)
    {
        switch (Filter)
        {
            case FilterType.All:
            {
                return true;
            }
            case FilterType.Enabled:
            {
                return checkingMod.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine;
            }
            case FilterType.Disabled:
            {
                return checkingMod.State == ModLocalComp.LocalCompFile.LocalFileStatus.Disabled;
            }
            case FilterType.CanUpdate:
            {
                return checkingMod.CanUpdate;
            }
            case FilterType.Unavailable:
            {
                return checkingMod.State == ModLocalComp.LocalCompFile.LocalFileStatus.Unavailable;
            }
            case FilterType.Duplicate:
            {
                var itemSource = IsSearching
                    ? searchResult
                    : ModLocalComp.compResourceListLoader.output ?? new List<ModLocalComp.LocalCompFile>();
                return itemSource is not null && itemSource.Where(m =>
                    checkingMod.Comp is not null && m.Comp is not null &&
                    (checkingMod.Comp.Id ?? "") == (m.Comp.Id ?? "")).Skip(1).Any();
            }

            default:
            {
                return false;
            }
        }
    }

    // 閻愮懓鍤粵娑⑩偓澶愩€嶇憴锕€褰傞惃鍕暭閸?
    private void ChangeFilter(MyRadioButton sender, bool raiseByMouse)
    {
        Filter = (FilterType)Convert.ToInt32(sender.Tag);
        RefreshUI();
        DoSort();
    }

    #endregion

    #region 閹烘帒绨?

    private SortMethod currentSortMethod = SortMethod.CompName;

    private void SetSortMethod(SortMethod target)
    {
        currentSortMethod = target;
        BtnSort.Text = Lang.Text("Instance.Resource.Sort.Text", GetSortName(target));
        // RefreshUI()
        DoSort();
    }

    private enum SortMethod
    {
        FileName,
        CompName,
        TagNums,
        CreateTime,
        ModFileSize
    }

    private string GetSortName(SortMethod method)
    {
        switch (method)
        {
            case SortMethod.FileName:
            {
                return Lang.Text("Instance.Resource.Sort.FileName");
            }
            case SortMethod.CompName:
            {
                return Lang.Text("Instance.Resource.Sort.ResourceName");
            }
            case SortMethod.TagNums:
            {
                return Lang.Text("Instance.Resource.Sort.TagCount");
            }
            case SortMethod.CreateTime:
            {
                return Lang.Text("Instance.Resource.Sort.AddTime");
            }
            case SortMethod.ModFileSize:
            {
                return Lang.Text("Instance.Resource.Sort.FileSize");
            }

            default:
            {
                return Lang.Text("Instance.Resource.Sort.ResourceName");
            }
        }

        return "";
    }

    private void BtnSortClick(object sender, ModBase.RouteEventArgs e)
    {
        var body = new ContextMenu();
        foreach (SortMethod i in Enum.GetValues(typeof(SortMethod)))
        {
            var item = new MyMenuItem();
            item.Header = GetSortName(i);
            item.Click += (_, _) => SetSortMethod(i);
            body.Items.Add(item);
        }

        body.PlacementTarget = (Control)sender;
        body.Placement = PlacementMode.Bottom;
        body.IsOpen = true;
    }

    private readonly object sortLock = new();

    private void DoSort()
    {
        lock (sortLock)
        {
            try
            {
                if (PanList is null || PanList.Children.Count < 2)
                    return;

                // 鐏忓棗鐡欓崗鍐鏉烆剚宕叉稉鍝勫讲閹烘帒绨惃鍕灙鐞?
                var items = PanList.Children.OfType<MyLocalCompItem>().ToList();
                var method = GetSortMethod(currentSortMethod);

                // 閸掑棛顬囬張澶嬫櫏閸滃本妫ら弫鍫ャ€嶉敍鍫滅箽閹镐礁甯慨瀣祲鐎靛綊銆庢惔蹇ョ礆
                var invalid = items.Where(i =>
                    i.Entry is null || (currentSortMethod == SortMethod.TagNums && i.Entry.Comp is null &&
                                        !i.Entry.IsFolder)).ToList();
                var valid = items.Except(invalid).ToList();
                // 娴犲懎顕張澶嬫櫏妞ょ绻樼悰灞惧笓鎼?
                valid.Sort((x, y) => method(x.Entry, y.Entry));
                // 閸氬牆鑻熸穱婵囧瘮閺冪姵鏅ユい鍦畱閸樼喎顫愭い鍝勭碍
                items = valid.Concat(invalid).ToList();

                // 閹靛綊鍣洪弴瀛樻煀UI閸忓啰绀?
                PanList.Children.Clear();
                items.ForEach(i => PanList.Children.Add(i));
            }

            catch (Exception ex)
            {
                ModBase.Log(
                    ex,
                    "閹笛嗩攽閹烘帒绨弮璺哄毉闁?,
                    ModBase.LogLevel.Hint,
                    userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
            }
        }
    }

    private Func<ModLocalComp.LocalCompFile, ModLocalComp.LocalCompFile, int> GetSortMethod(SortMethod method)
    {
        // 闁氨鏁ら惃鍕瀮娴犺泛銇欑純顕€銆婂В鏃囩窛閸戣姤鏆?
        int folderFirstCompare(ModLocalComp.LocalCompFile a, ModLocalComp.LocalCompFile b)
        {
            if (a.IsFolder && !b.IsFolder)
                return -1;
            if (!a.IsFolder && b.IsFolder)
                return 1;
            return 0; // 閻╃鎮撶猾璇茬€烽敍宀勬付鐟曚浇绻樻稉鈧銉︾槷鏉?
        }

        ;

        switch (method)
        {
            case SortMethod.FileName:
            {
                return (a, b) =>
                {
                    // 閺傚洣娆㈡径鐟邦潗缂佸牊甯撻崷銊︽付閸撳秹娼?
                    var folderResult = folderFirstCompare(a, b);
                    if (folderResult != 0)
                        return folderResult;
                    // 婵″倹鐏夐柈鑺ユЦ閺傚洣娆㈡径瑙勫灗闁姤妲搁弬鍥︽閿涘苯鍨幐澶嬫瀮娴犺泛鎮曢幒鎺戠碍
                    return string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase);
                };
            }
            case SortMethod.CompName:
            {
                return (a, b) =>
                {
                    // 閺傚洣娆㈡径鐟邦潗缂佸牊甯撻崷銊︽付閸撳秹娼?
                    var folderResult = folderFirstCompare(a, b);
                    if (folderResult != 0)
                        return folderResult;
                    // 婵″倹鐏夐柈鑺ユЦ閺傚洣娆㈡径瑙勫灗闁姤妲搁弬鍥︽閿涘苯鍨幐澶庣カ濠ф劕鎮曠粔鐗堝笓鎼?
                    return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                };
            }
            case SortMethod.TagNums:
            {
                return (a, b) =>
                {
                    // 閺傚洣娆㈡径鐟邦潗缂佸牊甯撻崷銊︽付閸撳秹娼?
                    var folderResult = folderFirstCompare(a, b);
                    if (folderResult != 0)
                        return folderResult;
                    // 婵″倹鐏夐柈鑺ユЦ閺傚洣娆㈡径鐧哥礉閸掓瑦瀵滈崥宥囆為幒鎺戠碍
                    if (a.IsFolder && b.IsFolder)
                        return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                    // 婵″倹鐏夐柈鑺ユЦ閺傚洣娆㈤敍灞藉灟閹稿鐖ｇ粵鐐殶闁插繑甯撴惔蹇ョ礄閺嶅洨顒锋径姘辨畱閸︺劌澧犻敍?
                    if (!a.IsFolder && !b.IsFolder)
                    {
                        // 鐎瑰鍙忓Λ鈧弻銉礉绾喕绻欳omp娑撳秳璐熺粚?
                        var aTagCount = a.Comp?.Tags?.Count ?? 0;
                        var bTagCount = b.Comp?.Tags?.Count ?? 0;
                        return bTagCount.CompareTo(aTagCount);
                    }

                    // 閻炲棜顔戞稉濠佺瑝娴兼艾鍩屾潏鎹愮箹闁插矉绱濇担鍡曡礋娴滃棗鐣ㄩ崗銊ㄦ崳鐟?
                    return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                };
            }
            case SortMethod.CreateTime:
            {
                return (a, b) =>
                {
                    // 閺傚洣娆㈡径鐟邦潗缂佸牊甯撻崷銊︽付閸撳秹娼?
                    var folderResult = folderFirstCompare(a, b);
                    if (folderResult != 0)
                        return folderResult;
                    // 婵″倹鐏夐柈鑺ユЦ閺傚洣娆㈡径瑙勫灗闁姤妲搁弬鍥︽閿涘苯鍨幐澶婂灡瀵ょ儤妞傞梻瀛樺笓鎼村骏绱欓弬鎵畱閸︺劌澧犻敍?
                    var aPath = a.IsFolder ? a.ActualPath : a.path;
                    var bPath = b.IsFolder ? b.ActualPath : b.path;
                    var aDate = GetModFileInfo(aPath).CreationTime;
                    var bDate = GetModFileInfo(bPath).CreationTime;
                    if (aDate == DateTime.MinValue && bDate == DateTime.MinValue)
                        return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);

                    if (aDate == DateTime.MinValue) return 1; // 閸戞椽鏁婇惃鍕瀮娴犺埖甯撻崷銊ユ倵闂?

                    if (bDate == DateTime.MinValue) return -1;
                    return bDate.CompareTo(aDate);
                };
            }
            case SortMethod.ModFileSize:
            {
                return (a, b) =>
                {
                    // 閺傚洣娆㈡径鐟邦潗缂佸牊甯撻崷銊︽付閸撳秹娼?
                    var folderResult = folderFirstCompare(a, b);
                    if (folderResult != 0)
                        return folderResult;
                    // 婵″倹鐏夐柈鑺ユЦ閺傚洣娆㈡径鐧哥礉閸掓瑦瀵滈崥宥囆為幒鎺戠碍
                    if (a.IsFolder && b.IsFolder)
                        return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                    // 婵″倹鐏夐柈鑺ユЦ閺傚洣娆㈤敍灞藉灟閹稿鏋冩禒璺恒亣鐏忓繑甯撴惔蹇ョ礄婢堆呮畱閸︺劌澧犻敍?
                    if (!a.IsFolder && !b.IsFolder)
                    {
                        var aSize = GetModFileInfo(a.ActualPath).Length;
                        var bSize = GetModFileInfo(b.ActualPath).Length;
                        if (aSize == 0L && bSize == 0L)
                            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);

                        if (aSize == 0L) return 1;

                        if (bSize == 0L) return -1;
                        return bSize.CompareTo(aSize);
                    }

                    // 閻炲棜顔戞稉濠佺瑝娴兼艾鍩屾潏鎹愮箹闁插矉绱濇担鍡曡礋娴滃棗鐣ㄩ崗銊ㄦ崳鐟?
                    return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                };
            }

            default:
            {
                return (a, b) =>
                {
                    // 閺傚洣娆㈡径鐟邦潗缂佸牊甯撻崷銊︽付閸撳秹娼?
                    var folderResult = folderFirstCompare(a, b);
                    if (folderResult != 0)
                        return folderResult;
                    // 婵″倹鐏夐柈鑺ユЦ閺傚洣娆㈡径瑙勫灗闁姤妲搁弬鍥︽閿涘苯鍨幐澶婃倳缁夌増甯撴惔?
                    return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                };
            }
        }
    }

    #endregion

    #region 娑撳绔熼弽?

    // 閸氼垳鏁?/ 缁備胶鏁?
    private void BtnSelectED_Click(object sender, ModBase.RouteEventArgs e)
    {
        EDMods(ModLocalComp.compResourceListLoader.output.Where(m => selectedMods.Contains(m.RawPath)).ToList(),
            !sender.Equals(BtnSelectDisable));
        ChangeAllSelected(false);
    }

    private void EDMods(IEnumerable<ModLocalComp.LocalCompFile> modList, bool isEnable)
    {
        var isSuccessful = true;
        foreach (var ModE in modList)
        {
            var modEntity = ModE; // 娴犲懐鏁ゆ禍搴″箵闂勩倛鍑禒锝呭綁闁插繑妫ゅ▔鏇氭叏閺€鍦畱闂勬劕鍩?
            string newPath = null;
            if (modEntity.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine && !isEnable)
                // 缁備胶鏁?
                newPath = modEntity.path + (File.Exists(modEntity.path + ".old") ? ".old" : ".disabled");
            else if (modEntity.State == ModLocalComp.LocalCompFile.LocalFileStatus.Disabled && isEnable)
                // 閸氼垳鏁?
                newPath = modEntity.RawPath;
            else
                continue;
            // 闁插秴鎳￠崥?
            try
            {
                if (File.Exists(newPath))
                {
                    if (File.Exists(modEntity.path))
                    {
                        // 閸氬本妞傜€涙ê婀稉銈勯嚋閸氬秶袨閻?Mod
                        if ((ModBase.GetFileMD5(modEntity.path) ?? "") != (ModBase.GetFileMD5(newPath) ?? ""))
                        {
                            ModMain.MyMsgBox(
                                Lang.Text("Instance.Resource.Ed.FileConflict.Message", newPath, modEntity.path),
                                Lang.Text("Instance.Resource.Ed.FileConflict"));
                            continue;
                        }
                    }
                    else
                    {
                        // 瀹歌尙绮￠柌宥呮嚒閸氬秷绻冩禍?
                        ModBase.Log("[Mod] Mod 閻ㄥ嫮濮搁幀浣稿嚒鐞氼偄鍨忛幑?, ModBase.LogLevel.Debug);
                        continue;
                    }
                }

                File.Delete(newPath);
                FileSystem.Rename(modEntity.path, newPath);
            }
            catch (FileNotFoundException ex)
            {
                ModBase.Log(
                    ex,
                    $"閺堫亝澹橀崚浼存付鐟曚線鍣搁崨钘夋倳閻?Mod閿涘澖modEntity.path ?? "null"}閿?,
                    ModBase.LogLevel.Feedback,
                    userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
                ReloadCompFileList(true);
                return;
            }
            catch (Exception ex)
            {
                ModBase.Log(ex, $"闁插秴鎳￠崥?Mod 婢惰精瑙﹂敍鍧絤odEntity.path ?? "null"}閿?);
                isSuccessful = false;
            }

            // 閺囧瓨鏁?Loader 娑擃厾娈戦崚妤勩€?
            var newModEntity = new ModLocalComp.LocalCompFile(newPath);
            newModEntity.FromJson(modEntity.ToJson());
            if (ModLocalComp.compResourceListLoader.output.Contains(modEntity))
            {
                var indexOfLoader = ModLocalComp.compResourceListLoader.output.IndexOf(modEntity);
                ModLocalComp.compResourceListLoader.output.RemoveAt(indexOfLoader);
                ModLocalComp.compResourceListLoader.output.Insert(indexOfLoader, newModEntity);
            }

            if (searchResult is not null && searchResult.Contains(modEntity)) // #4862
            {
                var indexOfResult = searchResult.IndexOf(modEntity);
                searchResult.Remove(modEntity);
                searchResult.Insert(indexOfResult, newModEntity);
            }

            // 閺囧瓨鏁?UI 娑擃厾娈戦崚妤勩€?
            try
            {
                var newItem = BuildLocalCompItem(newModEntity);
                modItems[modEntity.RawPath] = newItem;
                var indexOfUi = PanList.Children.IndexOf(PanList.Children.OfType<MyLocalCompItem>()
                    .FirstOrDefault(i => ReferenceEquals(i.Entry, modEntity)));
                if (indexOfUi == -1)
                    continue; // 閸ョ姳璐熼張顏嗙叀閸樼喎娲?Mod 閻ㄥ嫮濮搁幀浣稿嚒缂佸繐鍨忛幑銏犵暚娴?
                PanList.Children.RemoveAt(indexOfUi);
                PanList.Children.Insert(indexOfUi, newItem);
            }
            catch (Exception ex)
            {
                ModBase.Log(
                    ex,
                    $"閺囧瓨鏌?UI 閸掓銆冩い鐟般亼鐠愩儻绱皗modEntity.FileName}",
                    ModBase.LogLevel.Hint,
                    userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
            }
        }

        Dispatcher.Invoke(() => PanList.UpdateLayout(), DispatcherPriority.Background);
        if (isSuccessful)
        {
            RefreshBars();
        }
        else
        {
            HintService.Hint(Lang.Text("Instance.Resource.Ed.ToggleFailed"), HintType.Error);
            ReloadCompFileList(true);
        }

        LoaderRun(ModLoader.LoaderFolderRunType.UpdateOnly);
    }

    // 閺囧瓨鏌?
    private void BtnSelectUpdate_Click(object sender, ModBase.RouteEventArgs e)
    {
        var updateList = ModLocalComp.compResourceListLoader.output
            .Where(m => selectedMods.Contains(m.RawPath) && m.CanUpdate).ToList();
        if (!updateList.Any())
            return;
        UpdateResource(updateList);
        ChangeAllSelected(false);
    }

    /// <summary>
    ///     鐠佹澘缍嶅锝呮躬鏉╂稖顢?Mod 閺囧瓨鏌婇惃?mods 閺傚洣娆㈡径纭呯熅瀵板嫨鈧?
    /// </summary>
    public static List<string> updatingVersions = new();

    public void UpdateResource(IEnumerable<ModLocalComp.LocalCompFile> modList)
    {
        // 閺囧瓨鏌婇崜宥堫劅閸?
        if (currentCompType == ModComp.CompType.Mod && (!States.Hint.UpdateMod || modList.Count() >= 15))
        {
            if (ModMain.MyMsgBox(
                    Lang.Text("Instance.Resource.Update.Warning.Message"),
                    Lang.Text("Instance.Resource.Update.Warning.Title"), Lang.Text("Instance.Resource.Update.Warning.Confirm"), Lang.Text("Common.Action.Cancel"), isWarn: true) == 1)
                States.Hint.UpdateMod = true;
            else
                return;
        }

        try
        {
            // 閺嬪嫰鈧姳绗呮潪鎴掍繆閹?
            modList = modList.ToList(); // 闂冨弶顒涢崚閿嬫煀瑜板崬鎼锋潻顓濆敩閸?
            var fileList = new List<DownloadFile>();
            var fileCopyList = new Dictionary<string, string>();
            foreach (var Entry in modList)
            {
                var file = Entry.UpdateFile;
                if (!file.Available)
                    continue;
                // 绾喛顓婚弴瀛樻煀閸氬海娈戦弬鍥︽閸?
                var currentReplaceName = Entry.compFile.FileName.Replace(".jar", "").Replace(".old", "")
                    .Replace(".disabled", "");
                var newestReplaceName = Entry.UpdateFile.FileName.Replace(".jar", "").Replace(".old", "")
                    .Replace(".disabled", "");
                var currentSegs = currentReplaceName.Split('-').ToList();
                var newestSegs = newestReplaceName.Split('-').ToList();
                var shortened = false;
                while (true) // 缁夊娅庨崜宥咁嚤閻╃鎮撻柈銊ュ瀻閿涘牅绗夐懗鐣屝╅梽銈嗗閺堝娴夐崥宀勩€嶉敍宀冪箹娴兼艾顕遍懛缈犵伐婵?1.2-forge-2 閸?1.3-forge-3 娑擃參妫块惃?forge 鐞氼偄骞撻幒澶涚礉鐎佃壈鍤х亸婵婄槸閺囨寧宕?1.2-2閿?
                {
                    if (!currentSegs.Any() || !newestSegs.Any())
                        break;
                    if ((currentSegs.First() ?? "") != (newestSegs.First() ?? ""))
                        break;
                    currentSegs.RemoveAt(0);
                    newestSegs.RemoveAt(0);
                    shortened = true;
                }

                while (true) // 缁夊娅庨崥搴☆嚤閻╃鎮撻柈銊ュ瀻
                {
                    if (!currentSegs.Any() || !newestSegs.Any())
                        break;
                    if ((currentSegs.Last() ?? "") != (newestSegs.Last() ?? ""))
                        break;
                    currentSegs.RemoveAt(currentSegs.Count - 1);
                    newestSegs.RemoveAt(newestSegs.Count - 1);
                    shortened = true;
                }

                if (shortened && currentSegs.Any() && newestSegs.Any())
                {
                    currentReplaceName = currentSegs.Join("-");
                    newestReplaceName = newestSegs.Join("-");
                }

                // 濞ｈ濮為崚棰佺瑓鏉炶棄鍨悰?
                var tempAddress = ModBase.pathTemp + @"DownloadedComp\" +
                                  Entry.FileName.Replace(currentReplaceName, newestReplaceName);
                var realAddress = ModBase.GetPathFromFullPath(Entry.path) +
                                  Entry.FileName.Replace(currentReplaceName, newestReplaceName);
                fileList.Add(file.ToNetFile(tempAddress, ModComp.DownloadReason.Update));
                fileCopyList[tempAddress] = realAddress;
            }

            // 閺嬪嫰鈧姴濮炴潪钘夋珤
            var installLoaders = new List<ModLoader.LoaderBase>();
            var finishedFileNames = new List<string>();
            installLoaders.Add(new LoaderDownload(Lang.Text("Instance.Resource.Update.Task.DownloadFiles"), fileList)
                { ProgressWeight = modList.Count() * 1.5d }); // 濮ｅ繋閲?Mod 闂団偓鐟?1.5s
            installLoaders.Add(new ModLoader.LoaderTask<int, int>(
                Lang.Text("Instance.Resource.Update.Task.ReplaceFiles"), _ =>
            {
                try
                {
                    foreach (var Entry in modList)
                        if (File.Exists(Entry.path))
                            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(Entry.path, UIOption.AllDialogs,
                                RecycleOption.SendToRecycleBin);
                        else
                            ModBase.Log($"[CompUpdate] 閺堫亝澹橀崚鐗堟纯閺傛澘澧犻惃鍕カ濠ф劖鏋冩禒璁圭礉鐠哄疇绻冪€电懓鐣犻惃鍕灩闂勩倧绱皗Entry.path}", ModBase.LogLevel.Debug);

                    foreach (var Entry in fileCopyList)
                    {
                        if (File.Exists(Entry.Value))
                        {
                            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(Entry.Value, UIOption.AllDialogs,
                                RecycleOption.SendToRecycleBin);
                            ModBase.Log($"[Mod] 閺囧瓨鏌婇崥搴ｆ畱鐠у嫭绨弬鍥︽瀹告彃鐡ㄩ崷顭掔礉鐏忓棔绱伴幎濠傜暊閺€鎯у弳閸ョ偞鏁圭粩娆欑窗{Entry.Value}", ModBase.LogLevel.Debug);
                        }

                        if (Directory.Exists(ModBase.GetPathFromFullPath(Entry.Value)))
                        {
                            File.Move(Entry.Key, Entry.Value);
                            finishedFileNames.Add(ModBase.GetFileNameFromPath(Entry.Value));
                        }
                        else
                        {
                            ModBase.Log($"[Mod] 閺囧瓨鏌婇崥搴ｆ畱閻╊喗鐖ｉ弬鍥︽婢剁懓鍑＄悮顐㈠灩闂勩倧绱皗Entry.Value}", ModBase.LogLevel.Debug);
                        }
                    }
                }
                catch (OperationCanceledException ex)
                {
                    ModBase.Log(ex, "閺囨寧宕查弮褏澧楃挧鍕爱閺傚洣娆㈤弮鎯邦潶娑撹濮╅崣鏍ㄧХ");
                }
            }));
            // 缂佹挻娼径鍕倞
            var loader =
                new ModLoader.LoaderCombo<IEnumerable<ModLocalComp.LocalCompFile>>(
                    Lang.Text("Instance.Resource.Update.Task.Title", PageInstanceLeft.McInstance.Name), installLoaders);
            var pathMods = PageInstanceLeft.McInstance.PathIndie +
                           (PageInstanceLeft.McInstance.Info.HasLabyMod
                               ? Path.Combine("labymod-neo", "fabric", PageInstanceLeft.McInstance.Info.VanillaName)
                               : "") + ModLocalComp.GetPathNameByCompType(currentCompType) + @"\";
            loader.OnStateChanged = _ =>
            {
                // 缂佹挻鐏夐幓鎰仛
                switch (loader.State)
                {
                    case ModBase.LoadState.Finished:
                    {
                        switch (finishedFileNames.Count)
                        {
                            case 0: // 娑撯偓閼割剚妲搁悽鍙樼艾 Mod 閺傚洣娆㈢悮顐㈠窗閻㈩煉绱濋悞璺烘倵閻溾晛顔嶆稉璇插З閸欐牗绉?
                            {
                                ModBase.Log("[CompUpdate] 濞屸剝婀佺挧鍕爱鐞氼偅鍨氶崝鐔告纯閺?);
                                break;
                            }
                            case 1:
                            {
                                HintService.Hint(Lang.Text("Instance.Resource.Update.SuccessSingle", finishedFileNames.Single()), HintType.Success);
                                break;
                            }

                            default:
                            {
                                HintService.Hint(Lang.Text("Instance.Resource.Update.SuccessMultiple", finishedFileNames.Count), HintType.Success);
                                break;
                            }
                        }

                        break;
                    }
                    case ModBase.LoadState.Failed:
                    {
                        HintService.Hint(Lang.Text("Instance.Resource.Update.Failed", loader.Error.Message), HintType.Error);
                        break;
                    }
                    case ModBase.LoadState.Aborted:
                    {
                        HintService.Hint(Lang.Text("Instance.Resource.Update.Aborted"));
                        break;
                    }

                    default:
                    {
                        return;
                    }
                }

                ModBase.Log($"[CompUpdate] 瀹歌弓绮犲锝呮躬鏉╂稖顢戠挧鍕爱閺囧瓨鏌婇惃鍕瀮娴犺泛銇欓崚妤勩€冪粔濠氭珟閿涙pathMods}");
                updatingVersions.Remove(pathMods);
                // 濞撳懐鎮婄紓鎾崇摠
                ModBase.RunInNewThread(() =>
                {
                    try
                    {
                        foreach (var TempFile in fileCopyList.Keys)
                            if (File.Exists(TempFile))
                                File.Delete(TempFile);
                    }
                    catch (Exception ex)
                    {
                        ModBase.Log(ex, "濞撳懐鎮婄挧鍕爱閺囧瓨鏌婄紓鎾崇摠婢惰精瑙?);
                    }
                }, "Clean Comp Update Cache", ThreadPriority.BelowNormal);
            };
            // 閸氼垰濮╅崝鐘烘祰閸?
            ModBase.Log($"[CompUpdate] 瀵偓婵娲块弬?{modList.Count()} 娑擃亣绁┃鎰剁窗{pathMods}");
            updatingVersions.Add(pathMods);
            loader.Start();
            ModLoader.LoaderTaskbarAdd(loader);
            ModMain.frmMain.BtnExtraDownload.ShowRefresh();
            ModMain.frmMain.BtnExtraDownload.Ribble();
            ReloadCompFileList(true);
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "閸掓繂顫愰崠鏍カ濠ф劖娲块弬鏉裤亼鐠?);
        }
    }

    // 閸掔娀娅?
    private void BtnSelectDelete_Click(object sender, ModBase.RouteEventArgs e)
    {
        DeleteMods(ModLocalComp.compResourceListLoader.output.Where(m => selectedMods.Contains(m.RawPath)));
        ChangeAllSelected(false);
    }

    private void DeleteMods(IEnumerable<ModLocalComp.LocalCompFile> modList)
    {
        try
        {
            var isSuccessful = true;
            var isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            // 绾喛顓婚棁鈧憰浣稿灩闂勩倗娈戦弬鍥︽
            // 閺傚洣娆㈡径鐟板涧闂団偓鐟曚礁鍨归梽銈堝殰闊?
            modList = modList.SelectMany(target =>
                {
                    if (target.IsFolder) return new[] { target.path };

                    if (target.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine)
                        return new[]
                            { target.path, target.path + (File.Exists(target.path + ".old") ? ".old" : ".disabled") };

                    return new[] { target.path, target.RawPath };
                }).Distinct()
                .Where(m => m.EndsWithF(@"\__FOLDER__", true)
                    ? Directory.Exists(m.Replace(@"\__FOLDER__", ""))
                    : File.Exists(m)).Select(m => new ModLocalComp.LocalCompFile(m)).ToList();
            // 鐎圭偤妾崚鐘绘珟閺傚洣娆?
            foreach (var ModEntity in modList)
            {
                // 閸掔娀娅?
                try
                {
                    if (ModEntity.IsFolder)
                    {
                        // 閸掔娀娅庨弬鍥︽婢?
                        if (isShiftPressed)
                            Directory.Delete(ModEntity.ActualPath, true);
                        else
                            Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(ModEntity.ActualPath,
                                UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
                    }
                    // 閸掔娀娅庨弬鍥︽
                    else if (isShiftPressed)
                    {
                        File.Delete(ModEntity.path);
                    }
                    else
                    {
                        Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(ModEntity.path, UIOption.OnlyErrorDialogs,
                            RecycleOption.SendToRecycleBin);
                    }
                }
                catch (OperationCanceledException ex)
                {
                    ModBase.Log(ex, "閸掔娀娅庣挧鍕爱鐞氼偂瀵岄崝銊ュ絿濞?);
                    ReloadCompFileList(true);
                    return;
                }
                catch (Exception ex)
                {
                    ModBase.Log(
                        ex,
                        $"閸掔娀娅庣挧鍕爱婢惰精瑙﹂敍鍧組odEntity.path}閿?,
                        ModBase.LogLevel.Msgbox,
                        userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
                    isSuccessful = false;
                }

                // 閸欐牗绉烽柅澶夎厬
                selectedMods.Remove(ModEntity.RawPath);
                // 閺囧瓨鏁?Loader 閸?UI 娑擃厾娈戦崚妤勩€?
                ModLocalComp.compResourceListLoader.output.Remove(ModEntity);
                searchResult?.Remove(ModEntity);
                modItems.Remove(ModEntity.RawPath);
                var indexOfUi = PanList.Children.IndexOf(PanList.Children.OfType<MyLocalCompItem>()
                    .FirstOrDefault(i => i.Entry.Equals(ModEntity)));
                if (indexOfUi >= 0)
                    PanList.Children.RemoveAt(indexOfUi);
            }

            RefreshBars();
            if (!isSuccessful)
            {
                HintService.Hint(Lang.Text("Instance.Resource.Delete.Failed"), HintType.Error);
                ReloadCompFileList(true);
            }
            else if (PanList.Children.Count == 0)
            {
                ReloadCompFileList(true); // 閸掔娀娅庢禍鍡楀弿闁劑銆嶉惄?
            }
            else
            {
                RefreshBars();
            }

            // 閺勫墽銇氱紒鎾寸亯閹绘劗銇?
            if (!isSuccessful)
                return;
            if (isShiftPressed)
            {
                if (modList.Count() == 1)
                    HintService.Hint(Lang.Text("Instance.Resource.Delete.PermanentSingle", modList.Single().FileName), HintType.Success);
                else
                    HintService.Hint(Lang.Text("Instance.Resource.Delete.PermanentMultiple", modList.Count()), HintType.Success);
            }
            else if (modList.Count() == 1)
            {
                HintService.Hint(Lang.Text("Instance.Resource.Delete.RecycleSingle", modList.Single().FileName), HintType.Success);
            }
            else
            {
                HintService.Hint(Lang.Text("Instance.Resource.Delete.RecycleMultiple", modList.Count()), HintType.Success);
            }
        }
        catch (OperationCanceledException ex)
        {
            ModBase.Log(ex, "閸掔娀娅庣挧鍕爱鐞氼偂瀵岄崝銊ュ絿濞?);
            ReloadCompFileList(true);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閸掔娀娅庣挧鍕爱閸戣櫣骞囬張顏嗙叀闁挎瑨顕?,
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
            ReloadCompFileList(true);
        }

        LoaderRun(ModLoader.LoaderFolderRunType.UpdateOnly);
    }

    // 閸欐牗绉烽柅澶嬪
    private void BtnSelectCancel_Click(object sender, ModBase.RouteEventArgs e)
    {
        ChangeAllSelected(false);
    }

    // 閺€鎯版
    private void BtnSelectFavorites_Click(object sender, ModBase.RouteEventArgs e)
    {
        var selected = ModLocalComp.compResourceListLoader.output
            .Where(m => selectedMods.Contains(m.RawPath) && m.Comp is not null).Select(i => i.Comp).ToList();
        ModComp.CompFavorites.ShowMenu(selected, (Control)sender);
    }

    // 閸掑棔闊?
    private void BtnSelectShare_Click(object sender, ModBase.RouteEventArgs e)
    {
        var shareList = ModLocalComp.compResourceListLoader.output
            .Where(m => selectedMods.Contains(m.RawPath) && m.Comp is not null).Select(i => i.Comp.Id).ToHashSet();
        ModBase.ClipboardSet(ModComp.CompFavorites.GetShareCode(shareList));
        ChangeAllSelected(false);
    }

    #endregion

    #region 閸楁洑閲滅挧鍕爱妞?

    // 鐠囷附鍎?
    public void Info_Click(object sender, EventArgs e)
    {
        try
        {
            var modEntry = ((MyLocalCompItem)(sender is MyIconButton iconButton ? iconButton.Tag : sender)).Entry;
            // 閸掋倖鏌囩拠?LabyMod 閺勵垰鎯侀弨顖涘瘮鐎瑰顥?Fabric Mod
            var moddedLabyMod = PageInstanceLeft.McInstance.Info.HasLabyMod && PageInstanceLeft.McInstance.Modable;
            // 閸旂姾娴囨径杈Е娣団剝浼?
            if (modEntry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Unavailable)
            {
                ModMain.MyMsgBox(
                    Lang.Text("Instance.Resource.Item.Info.FailedMessage.WithDetail",
                        modEntry.FileUnavailableReason.ToString()),
                    Lang.Text("Instance.Resource.Item.Info.FailedTitle"));
                return;
            }

            if (modEntry.Comp is not null)
            {
                // 鐠哄疇娴嗛崚?Mod 娑撳娴囨い鐢告桨
                ModMain.frmMain.PageChange(new FormMain.PageStackData
                {
                    page = FormMain.PageType.CompDetail,
                    additional = (modEntry.Comp, new List<string>(), PageInstanceLeft.McInstance.Info.VanillaName,
                        PageInstanceLeft.McInstance.Info.HasForge ? ModComp.CompLoaderType.Forge :
                        PageInstanceLeft.McInstance.Info.HasNeoForge ? ModComp.CompLoaderType.NeoForge :
                        PageInstanceLeft.McInstance.Info.HasFabric || moddedLabyMod ? ModComp.CompLoaderType.Fabric :
                        ModComp.CompLoaderType.Any,
                        currentCompType, null)
                });
            }
            else
            {
                // 鐎甸€涚艾閸樼喓鎮婇崶鐐瀮娴犺绱濇担璺ㄦ暏瀵倹顒為崝鐘烘祰闁灝鍘I閸楋繝銆?
                if (modEntry.path.EndsWithF(".litematic", true) || modEntry.path.EndsWithF(".schem", true) ||
                    modEntry.path.EndsWithF(".schematic", true) || modEntry.path.EndsWithF(".nbt", true))
                {
                    ShowSchematicInfoAsync(modEntry);
                    return;
                }

                // 閼惧嘲褰囨穱鈩冧紖
                var contentLines = new List<string>();

                // 濡偓閺屻儲妲搁崥锔胯礋閺傚洣娆㈡径?
                if (modEntry.IsFolder)
                {
                    // 婢跺嫮鎮婇弬鍥︽婢剁顕涢幆?
                    var folderPath = modEntry.ActualPath;
                    if (Directory.Exists(folderPath))
                    {
                        var fileCount = 0;
                        try
                        {
                            // 閺嶈宓佽ぐ鎾冲鐠у嫭绨猾璇茬€风拋锛勭暬閺傚洣娆㈤弫浼村櫤
                            switch (currentCompType)
                            {
                                case ModComp.CompType.Schematic:
                                {
                                    fileCount = new DirectoryInfo(folderPath)
                                        .EnumerateFiles("*", SearchOption.AllDirectories).Where(f =>
                                            ModLocalComp.LocalCompFile.IsCompFile(f.FullName,
                                                ModComp.CompType.Schematic)).Count();
                                    break;
                                }
                                case ModComp.CompType.Mod:
                                {
                                    fileCount = new DirectoryInfo(folderPath)
                                        .EnumerateFiles("*.jar", SearchOption.AllDirectories).Count();
                                    break;
                                }
                                case ModComp.CompType.ResourcePack:
                                {
                                    fileCount = new DirectoryInfo(folderPath)
                                        .EnumerateFiles("*.zip", SearchOption.AllDirectories).Count();
                                    break;
                                }
                                case ModComp.CompType.Shader:
                                {
                                    fileCount = new DirectoryInfo(folderPath)
                                        .EnumerateFiles("*.zip", SearchOption.AllDirectories).Count();
                                    break;
                                }

                                default:
                                {
                                    fileCount = new DirectoryInfo(folderPath)
                                        .EnumerateFiles("*", SearchOption.AllDirectories).Count();
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            fileCount = 0;
                        }

                        if (fileCount == 0)
                            contentLines.Add(Lang.Text("Instance.Resource.Item.Info.EmptyFolder") + "\r\n");
                        else if (fileCount == 1)
                            contentLines.Add(Lang.Text("Instance.Resource.Item.Info.ContainsOne") + "\r\n");
                        else
                            contentLines.Add(Lang.Text("Instance.Resource.Item.Info.ContainsMany", fileCount) + "\r\n");
                    }
                    else
                    {
                        contentLines.Add(Lang.Text("Instance.Resource.Item.Info.FolderNotFound") + "\r\n");
                    }

                    contentLines.Add(Lang.Text("Instance.Resource.Item.Info.Path", folderPath));
                }
                else
                {
                    // 婢跺嫮鎮婇弲顕€鈧碍鏋冩禒鎯邦嚊閹?
                    if (modEntry.Description is not null)
                        contentLines.Add(modEntry.Description + "\r\n");
                    if (modEntry.Authors is not null)
                        contentLines.Add(Lang.Text("Instance.Resource.Item.Info.Author", modEntry.Authors));
                    contentLines.Add(Lang.Text("Instance.Resource.Item.Info.File", modEntry.FileName, ModBase.GetString(GetModFileInfo(modEntry.path).Length)));
                    if (modEntry.Version is not null)
                        contentLines.Add(Lang.Text("Instance.Resource.Item.Info.Version", modEntry.Version));

                    // 閸樼喓鎮婇崶鐐瀮娴犲墎娈戠拠锔藉剰娣団剝浼呭鏌モ偓姘崇箖瀵倹顒為弬瑙勭《婢跺嫮鎮?
                }

                // 閸欘亝婀侀弲顕€鈧碍鏋冩禒鑸靛閺勫墽銇氱拫鍐槸娣団剝浼?
                if (!modEntry.IsFolder)
                {
                    var debugInfo = new List<string>();
                    if (modEntry.ModId is not null) debugInfo.Add(Lang.Text("Instance.Resource.Item.Info.ModId", modEntry.ModId));
                    if (modEntry.Dependencies.Any())
                    {
                        debugInfo.Add(Lang.Text("Instance.Resource.Item.Info.Dependency"));
                        foreach (var Dep in modEntry.Dependencies)
                            debugInfo.Add(" - " + (Dep.Value is null
                                ? Dep.Key
                                : Lang.Text("Instance.Resource.Item.Info.DependencyVersion", Dep.Key, Dep.Value)));
                    }

                    if (debugInfo.Any())
                    {
                        contentLines.Add("");
                        contentLines.AddRange(debugInfo);
                    }
                }

                // 閺勫墽銇氱拠锔藉剰娣団剝浼?
                if (modEntry.IsFolder)
                {
                    // 閺傚洣娆㈡径鐟板涧閺勫墽銇氶崺鐑樻拱娣団剝浼呴敍灞肩瑝閹绘劒绶甸幖婊呭偍閸旂喕鍏?
                    ModMain.MyMsgBox(contentLines.Join("\r\n"), modEntry.Name, Lang.Text("Instance.Resource.Item.Info.Return"));
                }
                else
                {
                    // 閼惧嘲褰囬悽銊ょ艾閹兼粎鍌ㄩ惃?Mod 閸氬秶袨
                    var modOriginalName = modEntry.Name.Replace(" ", "+");
                    var modSearchName = modOriginalName.Substring(0, 1);
                    for (int i = 1, loopTo = modOriginalName.Count() - 1; i <= loopTo; i++)
                    {
                        var isLastLower = modOriginalName[i - 1].ToString().ToLower()
                            .Equals(modOriginalName[i - 1].ToString());
                        var isCurrentLower = modOriginalName[i].ToString().ToLower()
                            .Equals(modOriginalName[i].ToString());
                        if (isLastLower && !isCurrentLower)
                            // 娑撳﹣绔存稉顏勭摟濮ｅ秳璐熺亸蹇撳晸閿涘矁绻栨稉鈧稉顏勭摟濮ｅ秳璐熸径褍鍟?
                            modSearchName += "+";
                        modSearchName += modOriginalName[i].ToString();
                    }

                    modSearchName = modSearchName.Replace("++", "+").Replace("pti+Fine", "ptiFine");
                    // 閺勫墽銇?
                    if (currentCompType == ModComp.CompType.Schematic || !Lang.IsChineseMainland)
                    {
                        // 閹舵洖濂栭崢鐔烘倞閸ョ偓鏋冩禒鑸靛灗闂堢偘鑵戦弬鍥у隘閸╃喍绗夐弰鍓с仛閻у墽顫栭幖婊呭偍闁銆?
                        if (modEntry.Url is null)
                            ModMain.MyMsgBox(contentLines.Join("\r\n"), modEntry.Name, Lang.Text("Instance.Resource.Item.Info.Return"));
                        else if (ModMain.MyMsgBox(contentLines.Join("\r\n"), modEntry.Name, Lang.Text("Instance.Resource.Item.Info.OpenWebsite"), Lang.Text("Instance.Resource.Item.Info.Return")) ==
                                 1) ModBase.OpenWebsite(modEntry.Url);
                    }
                    // 閸忔湹绮挧鍕爱缁鐎锋穱婵堟殌閻у墽顫栭幖婊呭偍閸旂喕鍏?
                    else if (modEntry.Url is null)
                    {
                        if (ModMain.MyMsgBox(contentLines.Join("\r\n"), modEntry.Name, Lang.Text("Instance.Resource.Item.Info.McMod"), Lang.Text("Instance.Resource.Item.Info.Return")) == 1)
                            ModBase.OpenWebsite("https://www.mcmod.cn/s?key=" + modSearchName + "&site=all&filter=0");
                    }
                    else
                    {
                        switch (ModMain.MyMsgBox(contentLines.Join("\r\n"), modEntry.Name, Lang.Text("Instance.Resource.Item.Info.OpenWebsite"), Lang.Text("Instance.Resource.Item.Info.McMod"),
                                    Lang.Text("Instance.Resource.Item.Info.Return")))
                        {
                            case 1:
                            {
                                ModBase.OpenWebsite(modEntry.Url);
                                break;
                            }
                            case 2:
                            {
                                ModBase.OpenWebsite(
                                    "https://www.mcmod.cn/s?key=" + modSearchName + "&site=all&filter=0");
                                break;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閼惧嘲褰囩挧鍕爱鐠囷附鍎忔径杈Е",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
        }
    }

    // 閹垫挸绱戦弬鍥︽閹碘偓閸︺劎娈戞担宥囩枂
    public void Open_Click(MyIconButton sender, EventArgs e)
    {
        try
        {
            var listItem = (MyLocalCompItem)sender.Tag;
            // 鐎甸€涚艾閺傚洣娆㈡径閫涘▏閻劌鐤勯梽鍛扮熅瀵板嫸绱濈€甸€涚艾閺傚洣娆㈡担璺ㄦ暏閸樼喕鐭惧?
            var targetPath = listItem.Entry.IsFolder ? listItem.Entry.ActualPath : listItem.Entry.path;
            ModBase.OpenExplorer(targetPath);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閹垫挸绱戠挧鍕爱閺傚洣娆㈡担宥囩枂婢惰精瑙?,
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
        }
    }

    // 閸掔娀娅?
    public void Delete_Click(MyIconButton sender, EventArgs e)
    {
        var listItem = (MyLocalCompItem)sender.Tag;
        DeleteMods(new[] { listItem.Entry });
    }

    // 閸氼垳鏁?/ 缁備胶鏁?
    public void ED_Click(MyIconButton sender, EventArgs e)
    {
        var listItem = (MyLocalCompItem)sender.Tag;
        EDMods(new[] { listItem.Entry }, listItem.Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Disabled);
    }

    /// <summary>
    ///     瀵倹顒為弰鍓с仛閸樼喓鎮婇崶鎹愵嚊閹懍淇婇幁顖ょ礉闁灝鍘I閸楋繝銆?
    /// </summary>
    private void ShowSchematicInfoAsync(ModLocalComp.LocalCompFile modEntry)
    {
        // 閺勫墽銇氶崝鐘烘祰閹绘劗銇?
        HintService.Hint(Lang.Text("Instance.Resource.Item.Info.LoadingDetail"));

        // 閸︺劌鎮楅崣鎵殠缁嬪鑵戦崝鐘烘祰NBT閺佺増宓?
        // 绾喕绻?NBT 閺佺増宓佸鎻掑鏉?

        // 閸?UI 缁捐法鈻兼稉顓熸▔缁€楦款嚊閹?
        // 閺嬪嫬缂撶拠锔藉剰娣団剝浼?


        // 閺嶈宓侀弬鍥︽缁鐎烽弰鍓с仛鐠囷妇绮忔穱鈩冧紖

        // 閺勫墽銇氱拫鍐槸娣団剝浼?

        // 閺勫墽銇氱拠锔藉剰鐎电鐦藉?


        // 鐠佹澘缍嶉柨娆掝嚖閺冦儱绻旀担鍡曠瑝閺勫墽銇氶柨娆掝嚖閹绘劗銇氶敍灞芥礈娑撴椽鈧氨鏁ら惃鍕瀮娴犲墎濮搁幀浣诡梾閺屻儱鍑＄紒蹇擃槱閻炲棔绨?
        ModBase.RunInNewThread(() =>
        {
            try
            {
                modEntry.LoadNbtDataIfNeeded();
                ModBase.RunInUi(() =>
                {
                    try
                    {
                        var contentLines = new List<string>();
                        if (modEntry.Description is not null) contentLines.Add(modEntry.Description + "\r\n");
                        if (modEntry.Authors is not null) contentLines.Add(Lang.Text("Instance.Resource.Item.Info.Author", modEntry.Authors));
                        contentLines.Add(Lang.Text("Instance.Resource.Item.Info.File", modEntry.FileName, ModBase.GetString(GetModFileInfo(modEntry.path).Length)));
                        if (modEntry.Version is not null) contentLines.Add(Lang.Text("Instance.Resource.Item.Info.Version", modEntry.Version));
                        if (modEntry.path.EndsWithF(".litematic", true))
                            ShowLitematicDetails(contentLines, modEntry);
                        else if (modEntry.path.EndsWithF(".schem", true))
                            ShowSchemDetails(contentLines, modEntry);
                        else if (modEntry.path.EndsWithF(".schematic", true))
                            ShowSchematicDetails(contentLines, modEntry);
                        else if (modEntry.path.EndsWithF(".nbt", true)) ShowNbtDetails(contentLines, modEntry);
                        ShowDebugInfo(contentLines, modEntry);
                        ShowSchematicDialog(contentLines, modEntry);
                    }
                    catch (Exception ex)
                    {
                        ModBase.Log(
                            ex,
                            "閺勫墽銇氶崢鐔烘倞閸ユ崘顕涢幆鍛亼鐠?,
                            ModBase.LogLevel.Feedback,
                            userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
                    }
                });
            }
            catch (Exception ex)
            {
                ModBase.Log(
                    ex,
                    "閸旂姾娴囬崢鐔烘倞閸?NBT 閺佺増宓佹径杈Е",
                    ModBase.LogLevel.Feedback,
                    userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
            }
        });
    }

    #region 閸樼喓鎮婇崶鐐瀮娴犳儼顕涚紒鍡曚繆閹垱妯夌粈?

    /// <summary>
    ///     閺勫墽銇?Litematic 閺傚洣娆㈤惃鍕嚊缂佸棔淇婇幁?
    /// </summary>
    private void ShowLitematicDetails(List<string> contentLines, ModLocalComp.LocalCompFile modEntry)
    {
        contentLines.Add("");
        contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.DetailInfo"));

        // 閺勫墽銇氶崢鐔奉潗閸氬秶袨閿涘牅绮?NBT Metadata/Name 鐠囪褰囬敍?
        if (modEntry.LitematicOriginalName is not null) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.OriginalName") + modEntry.LitematicOriginalName);

        // 閺勫墽銇氶悧鍫熸拱娣団剝浼?
        if (modEntry.LitematicVersion.HasValue) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.Version") + modEntry.LitematicVersion.Value);

        // 閺勫墽銇氱亸鍝勵嚟娣団剝浼?
        if (modEntry.LitematicEnclosingSize is not null) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.EnclosingSize") + modEntry.LitematicEnclosingSize);

        // 閺勫墽銇氶弬鐟版健閸滃奔缍嬬粔顖滅埠鐠?
        if (modEntry.LitematicTotalBlocks.HasValue)
            contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.TotalBlocks") + Lang.Number(modEntry.LitematicTotalBlocks.Value, "N0"));

        if (modEntry.LitematicTotalVolume.HasValue)
            contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.TotalVolume") + Lang.Number(modEntry.LitematicTotalVolume.Value, "N0"));

        // 閺勫墽銇氶崠鍝勭厵閺佷即鍣?
        if (modEntry.LitematicRegionCount.HasValue) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.RegionCount") + modEntry.LitematicRegionCount.Value);

        // 閺勫墽銇氶弮鍫曟？娣団剝浼?
        if (modEntry.LitematicTimeCreated.HasValue)
            try
            {
                var createdTime = DateTimeOffset.FromUnixTimeMilliseconds(modEntry.LitematicTimeCreated.Value)
                    .ToLocalTime().DateTime;
                contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.CreatedTime") + Lang.Date(createdTime, "G"));
            }
            catch
            {
                contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.CreatedTime") + modEntry.LitematicTimeCreated.Value);
            }

        if (modEntry.LitematicTimeModified.HasValue)
            try
            {
                var modifiedTime = DateTimeOffset.FromUnixTimeMilliseconds(modEntry.LitematicTimeModified.Value)
                    .ToLocalTime().DateTime;
                contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.ModifiedTime") + Lang.Date(modifiedTime, "G"));
            }
            catch
            {
                contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.ModifiedTime") + modEntry.LitematicTimeModified.Value);
            }
    }

    /// <summary>
    ///     閺勫墽銇?Schem 閺傚洣娆㈤惃鍕嚊缂佸棔淇婇幁?
    /// </summary>
    private void ShowSchemDetails(List<string> contentLines, ModLocalComp.LocalCompFile modEntry)
    {
        contentLines.Add("");
        contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.DetailInfo"));

        // 閺勫墽銇氶崢鐔奉潗閸氬秶袨閿涘牅绮?NBT Metadata/Name 鐠囪褰囬敍?
        if (modEntry.SchemOriginalName is not null) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.OriginalName") + modEntry.SchemOriginalName);

        // 閺勫墽銇氶悧鍫熸拱娣団剝浼?
        if (modEntry.StructureGameVersion is not null) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.GameVersion") + modEntry.StructureGameVersion);

        if (modEntry.SpongeVersion.HasValue) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.SpongeVersion") + modEntry.SpongeVersion.Value);

        if (modEntry.StructureDataVersion.HasValue) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.DataVersion") + modEntry.StructureDataVersion.Value);

        // 閺勫墽銇氱亸鍝勵嚟娣団剝浼?
        if (modEntry.LitematicEnclosingSize is not null) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.EnclosingDimensions") + modEntry.LitematicEnclosingSize);

        // 閺勫墽銇氶弬鐟版健閸滃奔缍嬬粔顖滅埠鐠?
        if (modEntry.LitematicTotalBlocks.HasValue)
            contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.TotalBlocks") + Lang.Number(modEntry.LitematicTotalBlocks.Value, "N0"));

        if (modEntry.LitematicTotalVolume.HasValue)
            contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.TotalVolume") + Lang.Number(modEntry.LitematicTotalVolume.Value, "N0"));

        // 閺勫墽銇氶崠鍝勭厵閺佷即鍣?
        if (modEntry.LitematicRegionCount.HasValue) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.RegionCount") + modEntry.LitematicRegionCount.Value);

        contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.FileType",
            Lang.Text("Instance.Resource.Item.Schematic.FileType.Sponge")));
    }

    /// <summary>
    ///     閺勫墽銇?Schematic 閺傚洣娆㈤惃鍕嚊缂佸棔淇婇幁?
    /// </summary>
    private void ShowSchematicDetails(List<string> contentLines, ModLocalComp.LocalCompFile modEntry)
    {
        contentLines.Add("");
        contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.DetailInfo"));

        // 閺勫墽銇氱亸鍝勵嚟娣団剝浼?
        if (modEntry.LitematicEnclosingSize is not null) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.Size") + modEntry.LitematicEnclosingSize);

        // 閺勫墽銇氶弬鐟版健閸滃奔缍嬬粔顖滅埠鐠?
        if (modEntry.LitematicTotalBlocks.HasValue)
            contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.TotalBlocks") + Lang.Number(modEntry.LitematicTotalBlocks.Value, "N0"));

        if (modEntry.LitematicTotalVolume.HasValue)
            contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.TotalVolume") + Lang.Number(modEntry.LitematicTotalVolume.Value, "N0"));

        contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.FileType",
            Lang.Text("Instance.Resource.Item.Schematic.FileType.Mcedit")));
    }

    /// <summary>
    ///     閺勫墽銇?NBT 缂佹挻鐎弬鍥︽閻ㄥ嫯顕涚紒鍡曚繆閹?
    /// </summary>
    private void ShowNbtDetails(List<string> contentLines, ModLocalComp.LocalCompFile modEntry)
    {
        contentLines.Add("");
        contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.DetailInfo"));

        // 閺勫墽銇氭担婊嗏偓鍛繆閹?
        if (modEntry.StructureAuthor is not null) contentLines.Add(Lang.Text("Instance.Resource.Item.Info.Author", modEntry.StructureAuthor));

        // 閺勫墽銇氶悧鍫熸拱娣団剝浼?
        if (modEntry.StructureGameVersion is not null) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.GameVersion") + modEntry.StructureGameVersion);

        if (modEntry.StructureDataVersion.HasValue) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.DataVersion") + modEntry.StructureDataVersion.Value);

        // 閺勫墽銇氱亸鍝勵嚟娣団剝浼?
        if (modEntry.LitematicEnclosingSize is not null) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.EnclosingDimensions") + modEntry.LitematicEnclosingSize);

        // 閺勫墽銇氶弬鐟版健閸滃奔缍嬬粔顖滅埠鐠?
        if (modEntry.LitematicTotalBlocks.HasValue)
            contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.TotalBlocks") + Lang.Number(modEntry.LitematicTotalBlocks.Value, "N0"));

        if (modEntry.LitematicTotalVolume.HasValue)
            contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.TotalVolume") + Lang.Number(modEntry.LitematicTotalVolume.Value, "N0"));

        // 閺勫墽銇氶崠鍝勭厵閺佷即鍣?
        if (modEntry.LitematicRegionCount.HasValue) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.RegionCount") + modEntry.LitematicRegionCount.Value);

        contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.FileType",
            Lang.Text("Instance.Resource.Item.Schematic.FileType.Nbt")));
    }

    #endregion

    private void ShowDebugInfo(List<string> contentLines, ModLocalComp.LocalCompFile modEntry)
    {
        var debugInfo = new List<string>();
        if (modEntry.ModId is not null) debugInfo.Add(Lang.Text("Instance.Resource.Item.Info.ModId", modEntry.ModId));
        if (modEntry.Dependencies.Any())
        {
            debugInfo.Add(Lang.Text("Instance.Resource.Item.Info.Dependency"));
            foreach (var Dep in modEntry.Dependencies)
                debugInfo.Add(" - " + Dep.Key + (Dep.Value is null
                    ? Dep.Key
                    : Lang.Text("Instance.Resource.Item.Info.DependencyVersion", Dep.Key, Dep.Value)));
        }

        if (debugInfo.Any())
        {
            contentLines.Add("");
            contentLines.AddRange(debugInfo);
        }
    }

    private void ShowSchematicDialog(List<string> contentLines, ModLocalComp.LocalCompFile modEntry)
    {
        // 閹舵洖濂栭崢鐔烘倞閸ョ偓鏋冩禒鏈电瑝閺勫墽銇氶惂鍓ь潠閹兼粎鍌ㄩ柅澶愩€?
        if (modEntry.Url is null)
            ModMain.MyMsgBox(contentLines.Join("\r\n"), modEntry.Name, Lang.Text("Instance.Resource.Item.Info.Return"));
        else if (ModMain.MyMsgBox(contentLines.Join("\r\n"), modEntry.Name, Lang.Text("Instance.Resource.Item.Info.OpenWebsite"), Lang.Text("Instance.Resource.Item.Info.Return")) == 1)
            ModBase.OpenWebsite(modEntry.Url);
    }

    #endregion

    #region 閹兼粎鍌?

    public bool IsSearching => !string.IsNullOrWhiteSpace(SearchBox.Text);
    private List<ModLocalComp.LocalCompFile> searchResult;
    private CancellationTokenSource _cancelToken;

    public void SearchRun(object sender, EventArgs e)
    {
        var curToken = new CancellationTokenSource();
        var oldToken = Interlocked.Exchange(ref _cancelToken, curToken);
        oldToken?.Cancel();
        oldToken?.Dispose();

        // this exception is ignored
        Dispatcher.BeginInvoke(new Func<Task>(async () =>
        {
            try
            {
                var token = curToken.Token;
                await Task.Delay(350, token);
                if (token.IsCancellationRequested) return;
                if (IsSearching)
                {
                    var searchText = SearchBox.Text;
                    searchResult = await Task.Run(() => GetSearchResult(searchText), token);
                }

                if (token.IsCancellationRequested) return;
                RefreshUI();
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                ModBase.Log(ex, "閹兼粎鍌ㄦ潻鍥┾柤娑擃厼褰傞悽鐔风磽鐢?);
            }
        }));
    }

    private List<ModLocalComp.LocalCompFile> GetSearchResult(string query)
    {
        // 閺嬪嫰鈧姾顕Ч?
        var queryList = new List<ModBase.SearchEntry<ModLocalComp.LocalCompFile>>();
        foreach (var Entry in ModLocalComp.compResourceListLoader.output.AsReadOnly())
        {
            var searchSource = new List<ModBase.SearchSource>();
            searchSource.Add(new ModBase.SearchSource(Entry.Name, 1d));
            searchSource.Add(new ModBase.SearchSource(Entry.FileName, 1d));
            if (Entry.Version is not null) searchSource.Add(new ModBase.SearchSource(Entry.Version, 0.2d));
            if (Entry.Description is not null && !string.IsNullOrEmpty(Entry.Description))
                searchSource.Add(new ModBase.SearchSource(Entry.Description, 0.4d));
            if (Entry.Comp is not null)
            {
                if ((Entry.Comp.RawName ?? "") != (Entry.Name ?? ""))
                    searchSource.Add(new ModBase.SearchSource(Entry.Comp.RawName, 1d));
                if ((Entry.Comp.TranslatedName ?? "") != (Entry.Comp.RawName ?? ""))
                    searchSource.Add(new ModBase.SearchSource(Entry.Comp.TranslatedName, 1d));
                if ((Entry.Comp.Description ?? "") != (Entry.Description ?? ""))
                    searchSource.Add(new ModBase.SearchSource(Entry.Comp.Description, 0.4d));
                searchSource.Add(new ModBase.SearchSource(string.Join("", Entry.Comp.Tags), 0.2d));
            }

            queryList.Add(new ModBase.SearchEntry<ModLocalComp.LocalCompFile>
                { item = Entry, searchSource = searchSource });
        }

        // 鏉╂稖顢戦幖婊呭偍
        return ModBase.Search(queryList, query, ModBase.MaxLocalSearchDepth, 0.35d).Select(r => r.item).ToList();
    }

    #endregion
}
