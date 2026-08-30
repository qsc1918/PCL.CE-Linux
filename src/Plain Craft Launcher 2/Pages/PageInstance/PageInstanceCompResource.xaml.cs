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
    #region 妯＄粍淇℃伅缂撳瓨

    // 妯＄粍淇℃伅缂撳瓨 - 瑙ｅ喅鎺掑簭鏃堕噸澶嶅垱寤篎ileInfo瀵艰嚧鐨勬€ц兘闂
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

    // 鑾峰彇妯＄粍淇℃伅锛堝甫缂撳瓨锛?
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
            ModBase.Log(ex, "鑾峰彇妯＄粍淇℃伅澶辫触: " + path);
            return (DateTime.MinValue, 0L);
        }
    }

    // 椤甸潰鍏抽棴鏃舵竻鐞嗙紦瀛?
    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        modFileInfoCache.Clear();
    }

    #endregion

    #region 鍒濆鍖?

    private readonly ModComp.CompType currentCompType = ModComp.CompType.Mod;

    private readonly MyLocalCompItem.SwipeSelect currentSwipSelect;

    public PageInstanceCompResource(ModComp.CompType loadCompType)
    {
        currentCompType = loadCompType;
        CurrentFolderPath = ""; // 纭繚鏂囦欢澶硅矾寰勮閲嶇疆涓烘牴鐩綍
        currentSwipSelect = new MyLocalCompItem.SwipeSelect { TargetFrm = this };

        // 姝よ皟鐢ㄦ槸璁捐鍣ㄦ墍蹇呴渶鐨勩€?
        InitializeComponent();

        // 鍦?InitializeComponent() 璋冪敤涔嬪悗娣诲姞浠讳綍鍒濆鍖栥€?

        if (new[] { ModComp.CompType.Shader, ModComp.CompType.ResourcePack, ModComp.CompType.Schematic }.Contains(
                currentCompType))
        {
            BtnSelectEnable.Visibility = Visibility.Collapsed;
            BtnSelectDisable.Visibility = Visibility.Collapsed;
        }

        // 鎶曞奖鏂囦欢绠＄悊椤甸殣钘忎笅杞芥寜閽?
        if (currentCompType == ModComp.CompType.Schematic)
        {
            BtnManageDownload.Visibility = Visibility.Collapsed;
            BtnHintDownload.Visibility = Visibility.Collapsed;
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

        // 闈為噸澶嶅姞杞介儴鍒?
        if (isLoad)
            return;
        isLoad = true;

        // 妫€鏌ユ槸鍚︿负鍘熺悊鍥剧鐞嗙晫闈笖棣栨鎵撳紑
        if (currentCompType == ModComp.CompType.Schematic && !States.Hint.SchematicFirstTime)
            // 鏄剧ず棣栨鎵撳紑鎻愮ず
            ModBase.RunInUi(() =>
            {
                ModMain.MyMsgBox(Lang.Text("Instance.Saves.Folder.DoubleClickHint.Message"), Lang.Text("Instance.Saves.Folder.DoubleClickHint.Title"), Lang.Text("Common.Action.GotIt"));
                States.Hint.SchematicFirstTime = true;
            }, true);

        ModMain.frmMain.KeyDown += FrmMain_KeyDown;
        // 璋冩暣鎸夐挳杈硅窛锛堣繖鐜╂剰鍎挎病娉曚粠 XAML 鏀癸級
        foreach (MyRadioButton Btn in PanFilter.Children)
            Btn.LabText.Margin = new Thickness(-2, 0d, 8d, 0d);
    }

    /// <summary>
    ///     鍒锋柊 Mod 鍒楄〃銆?
    /// </summary>
    public void ReloadCompFileList(bool forceReload = false)
    {
        if (LoaderRun(forceReload
                ? ModLoader.LoaderFolderRunType.ForceRun
                : ModLoader.LoaderFolderRunType.RunOnUpdated))
        {
            ModBase.Log($"[System] 宸插埛鏂?{currentCompType} 鍒楄〃");
            modFileInfoCache.Clear();

            ModBase.RunInUi(() =>
            {
                Filter = FilterType.All;
                PanBack.ScrollToHome();
                SearchBox.Text = "";
            });
        }
    }

    // 寮哄埗鍒锋柊
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
        // 寮哄埗鍒锋柊
        try
        {
            ModComp.compProjectCache.Clear();
            ModComp.compFilesCache.Clear();
            File.Delete(ModBase.pathTemp + @"Cache\LocalComp.json");
            ModBase.Log("[CompResource] 鐢变簬鐐瑰嚮鍒锋柊鎸夐挳锛屾竻鐞嗘湰鍦板伐绋嬩俊鎭紦瀛?);
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "寮哄埗鍒锋柊鏃舵竻鐞嗘湰鍦板伐绋嬩俊鎭紦瀛樺け璐?);
        }

        switch (whichPage)
        {
            case ModComp.CompType.Mod:
            {
                if (ModMain.frmInstanceMod is not null)
                    ModMain.frmInstanceMod.ReloadCompFileList(true); // 鏃犻渶 Else锛岃繕娌″姞杞藉埛涓鐨勬柊
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

    private void Load_Click(object sender, MouseButtonEventArgs e)
    {
        if (ModLocalComp.compResourceListLoader.State == ModBase.LoadState.Failed)
            LoaderRun(ModLoader.LoaderFolderRunType.ForceRun);
    }

    public bool LoaderRun(ModLoader.LoaderFolderRunType type)
    {
        string loadPath;
        if (string.IsNullOrEmpty(CurrentFolderPath))
            // 鍔犺浇鏍圭洰褰?
            loadPath = PageInstanceLeft.McInstance.PathIndie +
                       (PageInstanceLeft.McInstance.Info.HasLabyMod
                           ? Path.Combine("labymod-neo", "fabric", PageInstanceLeft.McInstance.Info.VanillaName)
                           : "") + ModLocalComp.GetPathNameByCompType(currentCompType) + @"\";
        else
            // 鍔犺浇褰撳墠鏂囦欢澶?
            loadPath = CurrentFolderPath;
        return ModLoader.LoaderFolderRun(ModLocalComp.compResourceListLoader, loadPath, type,
            loaderInput: GetRequireLoaderData());
    }

    #endregion

    #region 鏂囦欢澶瑰鑸?

    /// <summary>
    ///     褰撳墠鏄剧ず鐨勬枃浠跺す璺緞銆傜┖瀛楃涓茶〃绀烘牴鐩綍銆?
    /// </summary>
    public string CurrentFolderPath { get; set; } = "";

    /// <summary>
    ///     杩涘叆鎸囧畾鐨勬枃浠跺す銆?
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
            ModBase.Log($"[鍘熺悊鍥綸 杩涘叆鏂囦欢澶癸細{folderPath}");

            ModLoader.LoaderFolderRun(ModLocalComp.compResourceListLoader, folderPath,
                ModLoader.LoaderFolderRunType.ForceRun, loaderInput: GetRequireLoaderData());
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "杩涘叆鏂囦欢澶瑰け璐?,
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
        }
    }

    /// <summary>
    ///     杩涘叆鎸囧畾鏂囦欢澶广€?
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
                "杩涘叆鏂囦欢澶瑰け璐?,
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
        }
    }

    /// <summary>
    ///     杩斿洖涓婄骇鏂囦欢澶广€?
    /// </summary>
    private void GoBackToParentFolder()
    {
        if (string.IsNullOrEmpty(CurrentFolderPath))
            return;

        try
        {
            // 鑾峰彇鏍硅矾寰?
            var rootPath = PageInstanceLeft.McInstance.PathIndie +
                           (PageInstanceLeft.McInstance.Info.HasLabyMod
                               ? Path.Combine("labymod-neo", "fabric", PageInstanceLeft.McInstance.Info.VanillaName)
                               : "") + ModLocalComp.GetPathNameByCompType(currentCompType) + @"\";
            rootPath = Path.GetFullPath(rootPath.TrimEnd('\\'));

            // 鑾峰彇鐖剁骇璺緞
            var parentPath = Directory.GetParent(CurrentFolderPath)?.FullName;

            // 濡傛灉鐖剁骇璺緞灏辨槸鏍硅矾寰勬垨鑰呯埗绾ц矾寰勪笉鍦ㄦ牴璺緞鑼冨洿鍐咃紝鍒欒繑鍥炴牴鐩綍
            if (parentPath is null || parentPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase) ||
                !parentPath.StartsWith(rootPath + @"\", StringComparison.OrdinalIgnoreCase))
                CurrentFolderPath = "";
            else
                CurrentFolderPath = parentPath;
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "璺緞澶勭悊澶辫触");
            // 鍙戠敓閿欒鏃剁洿鎺ヨ繑鍥炴牴鐩綍
            CurrentFolderPath = "";
        }

        ModBase.Log($"[鍘熺悊鍥綸 杩斿洖涓婄骇鏂囦欢澶癸細{(string.IsNullOrEmpty(CurrentFolderPath) ? "鏍圭洰褰? : CurrentFolderPath)}");

        // 閲嶆柊鍔犺浇褰撳墠鏂囦欢澶圭殑鍐呭
        string loadPath;
        if (string.IsNullOrEmpty(CurrentFolderPath))
            // 杩斿洖鍒版牴鐩綍
            loadPath = PageInstanceLeft.McInstance.PathIndie +
                       (PageInstanceLeft.McInstance.Info.HasLabyMod
                           ? Path.Combine("labymod-neo", "fabric", PageInstanceLeft.McInstance.Info.VanillaName)
                           : "") + ModLocalComp.GetPathNameByCompType(currentCompType) + @"\";
        else
            // 鍔犺浇褰撳墠鏂囦欢澶?
            loadPath = CurrentFolderPath;

        // 寮哄埗鍒锋柊UI鐘舵€?
        // 纭繚鎸夐挳鐘舵€佹纭?
        ModBase.RunInUi(() =>
            BtnManageBack.Visibility =
                !string.IsNullOrEmpty(CurrentFolderPath) ? Visibility.Visible : Visibility.Collapsed);

        // 寤惰繜涓€甯у悗鍐嶅姞杞斤紝纭繚UI鐘舵€佸凡鏇存柊
        ModBase.RunInUi(
            () => ModLoader.LoaderFolderRun(ModLocalComp.compResourceListLoader, loadPath,
                ModLoader.LoaderFolderRunType.ForceRun, loaderInput: GetRequireLoaderData()), true);
    }

    #endregion

    #region UI 鍖?

    /// <summary>
    ///     宸插姞杞界殑 Mod UI 缂撳瓨锛屼笉纭繚鎸夋樉绀洪『搴忔帓鍒椼€侹ey 涓?Mod 鐨?RawPath銆?
    /// </summary>
    public Dictionary<string, MyLocalCompItem> modItems = new();

    /// <summary>
    ///     灏嗗姞杞藉櫒缁撴灉鐨?Mod 鍒楄〃鍔犺浇涓?UI銆?
    /// </summary>
    private void LoadUIFromLoaderOutput()
    {
        try
        {
            // 鍒ゆ柇搴旇鏄剧ず鍝竴涓〉闈?
            if (ModLocalComp.compResourceListLoader.output.Any())
            {
                PanBack.Visibility = Visibility.Visible;
                PanEmpty.Visibility = Visibility.Collapsed;
                PanSchematicEmpty.Visibility = Visibility.Collapsed;
            }
            else
            {
                // 妫€鏌ユ槸鍚︿负鎶曞奖鏂囦欢绫诲瀷涓攕chematics鏂囦欢澶逛笉瀛樺湪
                if (currentCompType == ModComp.CompType.Schematic)
                {
                    var schematicsPath = PageInstanceLeft.McInstance.PathIndie + @"schematics\";
                    if (!Directory.Exists(schematicsPath))
                    {
                        PanSchematicEmpty.Visibility = Visibility.Visible;
                        PanEmpty.Visibility = Visibility.Collapsed;
                        PanBack.Visibility = Visibility.Collapsed;
                        return;
                    }
                }

                // 鏍规嵁缁勪欢绫诲瀷璁剧疆PanEmpty鐨勬枃鏈唴瀹?
                if (currentCompType == ModComp.CompType.Schematic)
                {
                    // 妫€鏌ユ槸鍚﹀湪瀛愭枃浠跺す涓?
                    if (!string.IsNullOrEmpty(CurrentFolderPath))
                    {
                        // 瀛愭枃浠跺す涓虹┖鐨勬彁绀?
                        TxtEmptyTitle.Text = Lang.Text("Instance.Resource.EmptyFolder.Title");
                        TxtEmptyDescription.Text = Lang.Text("Instance.Resource.EmptyFolder.Description");
                    }
                    else
                    {
                        // 鏍圭洰褰曚负绌虹殑鎻愮ず
                        TxtEmptyTitle.Text = Lang.Text("Instance.Resource.Empty.Title");
                        TxtEmptyDescription.Text = Lang.Text("Instance.Resource.Empty.Description");
                    }
                }
                else
                {
                    TxtEmptyTitle.Text = Lang.Text("Instance.Resource.Empty.Title");
                    TxtEmptyDescription.Text = Lang.Text("Instance.Resource.Empty.DescriptionWithDownload");
                }

                // 濡傛灉褰撳墠鍦ㄥ瓙鏂囦欢澶逛腑锛屾樉绀鸿繑鍥炰笂涓€绾ф寜閽?
                if (!string.IsNullOrEmpty(CurrentFolderPath))
                    BtnHintBack.Visibility = Visibility.Visible;
                else
                    BtnHintBack.Visibility = Visibility.Collapsed;

                PanEmpty.Visibility = Visibility.Visible;
                PanBack.Visibility = Visibility.Collapsed;
                PanSchematicEmpty.Visibility = Visibility.Collapsed;
                return;
            }

            // 淇敼缂撳瓨
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
            // 鏄剧ず缁撴灉
            ModBase.RunInUi(() =>
            {
                Filter = FilterType.All;
                SearchBox.Text = ""; // 杩欎細瑙﹀彂缁撴灉鍒锋柊锛屾墍浠ラ渶瑕佸湪 ModItems 鏇存柊涔嬪悗锛岃瑙?#3124 鐨勮棰?
                RefreshUI();
                SetSortMethod(SortMethod.CompName);
            });
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                $"鍔犺浇 {currentCompType} 鍒楄〃 UI 澶辫触",
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
            ModBase.Log(ex, $"鍒涘缓 UI 椤瑰け璐ワ細{entry.RawPath}");
            throw;
        }
    }

    private void BuildLocalCompItemBtnHandler(MyLocalCompItem sender, EventArgs e)
    {
        // 鐐瑰嚮浜嬩欢
        sender.Changed += (ss, ee) => CheckChanged((MyLocalCompItem)ss, ee);
        if (sender.Entry.IsFolder)
        {
            // 鏂囦欢澶归」鐨勭偣鍑讳簨浠讹細鍙屽嚮杩涘叆鏂囦欢澶癸紝鍗曞嚮鍒囨崲閫変腑鐘舵€?
            var lastClickTime = DateTime.MinValue;
            sender.Click += (sss, _) =>
            {
                var ss = (MyLocalCompItem)sss;
                var currentTime = DateTime.Now;
                var timeDiff = (currentTime - lastClickTime).TotalMilliseconds;

                if (timeDiff <= 300d)
                    // 300ms鍐呭弻鍑伙紝杩涘叆鏂囦欢澶?
                    EnterFolderWithCheck(ss.Entry.ActualPath);
                else
                    // 鍗曞嚮鍒囨崲閫変腑鐘舵€?
                    ss.Checked = !ss.Checked;

                lastClickTime = currentTime;
            };
        }
        else
        {
            // 鏂囦欢椤圭殑鐐瑰嚮浜嬩欢锛氬垏鎹㈤€変腑鐘舵€?
            sender.Click += (sss, _) =>
            {
                var ss = (MyLocalCompItem)sss;
                ss.Checked = !ss.Checked;
            };
        }

        // 鍥炬爣鎸夐挳
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
    ///     鍒锋柊鏁翠釜 UI銆?
    /// </summary>
    public void RefreshUI()
    {
        if (PanList is null)
            return;
        var showingMods = (IsSearching ? searchResult : modItems.Values.Select(i => i.Entry))
            .Where(m => CanPassFilter(m)).ToList();

        // 瀵规樉绀虹殑璧勬簮杩涜鎺掑簭锛岀‘淇濇枃浠跺す缃《
        if (showingMods.Any())
        {
            var sortMethod = GetSortMethod(currentSortMethod);
            showingMods.Sort((a, b) => sortMethod(a, b));
        }

        // 閲嶆柊鍒楀嚭鍒楄〃
        ModAnimation.AniControlEnabled += 1;
        if (showingMods.Any())
        {
            PanList.Visibility = Visibility.Visible;
            PanList.Children.Clear();
            foreach (var TargetMod in showingMods)
            {
                if (!modItems.ContainsKey(TargetMod.RawPath))
                    continue;
                var item = modItems[TargetMod.RawPath];

                // 纭繚鍏冪礌娌℃湁鐖跺鍣紝閬垮厤閲嶅娣诲姞寮傚父
                if (item.Parent is not null) ((Panel)item.Parent).Children.Remove(item);

                ModStyle.MinecraftFormatter.SetColorfulTextLab(item.LabTitle.Text, item.LabTitle,
                    ThemeService.IsDarkMode);
                ModStyle.MinecraftFormatter.SetColorfulTextLab(item.LabInfo.Text, item.LabInfo,
                    ThemeService.IsDarkMode);
                item.Checked = selectedMods.Contains(TargetMod.RawPath); // 鏇存柊閫変腑鐘舵€?
                PanList.Children.Add(item);
            }
        }
        else
        {
            PanList.Visibility = Visibility.Collapsed;
        }

        ModAnimation.AniControlEnabled -= 1;
        selectedMods =
            new HashSet<string>(selectedMods.Where(m => showingMods.Any(s => (s.RawPath ?? "") == (m ?? ""))));
        RefreshBars();
    }

    /// <summary>
    ///     鍒锋柊椤舵爮鍜屽簳鏍忔樉绀恒€?
    /// </summary>
    public void RefreshBars()
    {
        Dispatcher.BeginInvoke(new Func<Task>(async () =>
        {
            // -----------------
            // 椤堕儴鏍?
            // -----------------

            // 璁℃暟
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
            // 鏄剧ず
            BtnFilterAll.Text = IsSearching ? Lang.Text("Instance.Resource.Filter.SearchResult") : Lang.Text("Instance.Resource.Filter.AllWithCount", anyCount);
            BtnFilterCanUpdate.Text = Lang.Text("Instance.Resource.Filter.UpdatableWithCount", updateCount);
            BtnFilterCanUpdate.Visibility = Filter == FilterType.CanUpdate || updateCount > 0
                ? Visibility.Visible
                : Visibility.Collapsed;
            BtnFilterEnabled.Text = Lang.Text("Instance.Resource.Filter.EnabledWithCount", enabledCount);
            BtnFilterEnabled.Visibility = Filter == FilterType.Enabled || (enabledCount > 0 && enabledCount < anyCount)
                ? Visibility.Visible
                : Visibility.Collapsed;
            BtnFilterDisabled.Text = Lang.Text("Instance.Resource.Filter.DisabledWithCount", disabledCount);
            BtnFilterDisabled.Visibility = Filter == FilterType.Disabled || disabledCount > 0
                ? Visibility.Visible
                : Visibility.Collapsed;
            BtnFilterError.Text = Lang.Text("Instance.Resource.Filter.ErrorWithCount", unavalialeCount);
            BtnFilterError.Visibility = Filter == FilterType.Unavailable || unavalialeCount > 0
                ? Visibility.Visible
                : Visibility.Collapsed;
            // 鏌ユ壘閲嶅椤圭洰
            var duplicateItems = await Task.Run(() => itemSource.GroupBy(m =>
            {
                if (m.Comp is null) return ":Nothing:";

                return m.Comp.Id;
            }).Where(g => g.Count() > 1 && g.First().Comp is not null).SelectMany(g => g).ToList());
            BtnFilterDuplicate.Text = Lang.Text("Instance.Resource.Filter.DuplicateWithCount", duplicateItems.Count);
            BtnFilterDuplicate.Visibility = Filter == FilterType.Duplicate || duplicateItems.Any()
                ? Visibility.Visible
                : Visibility.Collapsed;

            // 杩斿洖鎸夐挳鏄剧ず鎺у埗锛堝湪瀛愭枃浠跺す涓椂鏄剧ず锛?
            if (!string.IsNullOrEmpty(CurrentFolderPath))
                BtnManageBack.Visibility = Visibility.Visible;
            else
                BtnManageBack.Visibility = Visibility.Collapsed;

            // -----------------
            // 搴曢儴鏍?
            // -----------------

            // 璁℃暟
            var newCount = selectedMods.Count;
            var selected = newCount > 0;
            if (selected)
                LabSelect.Text = Lang.Text("Instance.Resource.SelectedCount", newCount); // 鍙栨秷鎵€鏈夐€夋嫨鏃朵笉鏇存柊鏁板瓧
            // 鎸夐挳鍙敤鎬?
            if (selected)
            {
                var hasUpdate = false;
                var hasEnabled = false;
                var hasDisabled = false;
                var canFavoriteAndShare = true; // 鏄惁鍙互鏀惰棌鍜屽垎浜?


                // 妫€鏌ユ槸鍚︽墍鏈夐€変腑鐨勮祫婧愰兘鏈夋湁鏁堢殑椤圭洰淇℃伅锛堝嵆宸插畬鎴愯仈缃戞洿鏂帮級
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

                // 閽堝鎶曞奖鍘熺悊鍥鹃殣钘忓垎浜?鏇存柊 鏀惰棌鎸夐挳
                if (currentCompType == ModComp.CompType.Schematic)
                {
                    BtnSelectUpdate.Visibility = Visibility.Collapsed;
                    BtnSelectFavorites.Visibility = Visibility.Collapsed;
                    BtnSelectShare.Visibility = Visibility.Collapsed;
                }
                else
                {
                    BtnSelectUpdate.Visibility = Visibility.Visible;
                    BtnSelectFavorites.Visibility = Visibility.Visible;
                    BtnSelectShare.Visibility = Visibility.Visible;

                    // 鏍规嵁鏄惁宸插姞杞介」鐩俊鎭潵鍚敤/绂佺敤鏀惰棌鍜屽垎浜寜閽?
                    BtnSelectFavorites.IsEnabled = canFavoriteAndShare;
                    BtnSelectShare.IsEnabled = canFavoriteAndShare;
                }
            }

            // 鏇存柊鏄剧ず鐘舵€?
            if (ModAnimation.AniControlEnabled == 0)
            {
                PanListBack.Margin = new Thickness(0d, 0d, 0d, selected ? 95 : 15);
                if (selected)
                {
                    // 浠呭湪鏁伴噺澧炲姞鏃舵挱鏀惧嚭鐜?璺宠穬鍔ㄧ敾
                    if (bottomBarShownCount >= newCount)
                    {
                        bottomBarShownCount = newCount;
                        return;
                    }

                    bottomBarShownCount = newCount;
                    // 鍑虹幇/璺宠穬鍔ㄧ敾
                    CardSelect.Visibility = Visibility.Visible;
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
                    // 涓嶉噸澶嶆挱鏀鹃殣钘忓姩鐢?
                    if (bottomBarShownCount == 0)
                        return;
                    bottomBarShownCount = 0;
                    // 闅愯棌鍔ㄧ敾
                    ModAnimation.AniStart(
                        new[]
                        {
                            ModAnimation.AaOpacity(CardSelect, -CardSelect.Opacity, 90),
                            ModAnimation.AaTranslateY(CardSelect, -10 - TransSelect.Y, 90,
                                ease: new ModAnimation.AniEaseInFluent(ModAnimation.AniEasePower.Weak)),
                            ModAnimation.AaCode(() => CardSelect.Visibility = Visibility.Collapsed, after: true)
                        }, "Mod Sidebar");
                }
            }
            else
            {
                ModAnimation.AniStop("Mod Sidebar");
                bottomBarShownCount = newCount;
                if (selected)
                {
                    CardSelect.Visibility = Visibility.Visible;
                    CardSelect.Opacity = 1d;
                    TransSelect.Y = -25;
                }
                else
                {
                    CardSelect.Visibility = Visibility.Collapsed;
                    CardSelect.Opacity = 0d;
                    TransSelect.Y = -10;
                }
            }
        }));
    }

    private int bottomBarShownCount;

    #endregion

    #region 绠＄悊

    /// <summary>
    ///     鎵撳紑 Mods 鏂囦欢澶广€?
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

            // 濡傛灉褰撳墠鍦ㄥ瓙鏂囦欢澶逛腑锛屽垯鎵撳紑褰撳墠瀛愭枃浠跺す锛涘惁鍒欐墦寮€鏍圭洰褰?
            if (string.IsNullOrEmpty(CurrentFolderPath))
                // 鎵撳紑鏍圭洰褰?
                compFilePath = PageInstanceLeft.McInstance.PathIndie +
                               (PageInstanceLeft.McInstance.Info.HasLabyMod
                                   ? Path.Combine("labymod-neo", "fabric", PageInstanceLeft.McInstance.Info.VanillaName)
                                   : "") + ModLocalComp.GetPathNameByCompType(currentCompType) + @"\";
            else
                // 鎵撳紑褰撳墠瀛愭枃浠跺す
                compFilePath = CurrentFolderPath.EndsWith(@"\") ? CurrentFolderPath : CurrentFolderPath + @"\";
            Directory.CreateDirectory(compFilePath);
            ModBase.OpenExplorer(compFilePath);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "鎵撳紑 Mods 鏂囦欢澶瑰け璐?,
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
        }
    }


    /// <summary>
    ///     鍏ㄩ€夈€?
    /// </summary>
    private void BtnManageSelectAll_Click(object sender, MouseButtonEventArgs e)
    {
        ChangeAllSelected(selectedMods.Count < PanList.Children.Count);
    }

    /// <summary>
    ///     瀹夎 Mod銆?
    /// </summary>
    private void BtnManageInstall_Click(object sender, MouseButtonEventArgs e)
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
    ///     灏濊瘯瀹夎 Mod銆?
    ///     杩斿洖杈撳叆鐨勬枃浠舵槸鍚︿负涓€涓?Mod 鏂囦欢锛屼粎鐢ㄤ簬鍒ゆ柇鎷栨嫿琛屼负銆?
    /// </summary>
    public static bool InstallMods(IEnumerable<string> filePathList)
    {
        if (!filePathList.Any()) return false;

        // 1. Check file extension
        var firstFile = filePathList.First();
        var extension = firstFile.Split('.').LastOrDefault()?.ToLower();
        string[] allowedExtensions = { "jar", "litemod", "disabled", "old" };

        if (!allowedExtensions.Contains(extension)) return false;

        LogWrapper.Info("[System] 鏂囦欢鏍煎紡涓?jar/litemod锛屽皾璇曞畨瑁呬负 Mod");

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
            LogWrapper.Error(ex, "鎷疯礉鏂囦欢澶辫触");
        }
    }

    /// <summary>
    ///     瀹夎缁勪欢鏂囦欢锛圡od銆佽祫婧愬寘銆佸厜褰卞寘銆佹姇褰辨枃浠剁瓑锛夈€?
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

        // 妫€鏌ュ洖鏀剁珯锛氬洖鏀剁珯涓殑鏂囦欢鏈夐敊璇殑鏂囦欢鍚?
        if (filePathList.First().Contains(@":\$RECYCLE.BIN\"))
        {
            HintService.Hint(Lang.Text("Instance.Resource.Install.RestoreFromRecycleBin"), HintType.Error);
            return;
        }

        // 鑾峰彇骞舵鏌ョ洰鏍囧疄渚?
        var targetInstance = ModInstanceList.McMcInstanceSelected;
        if (ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSetup)
            targetInstance = PageInstanceLeft.McInstance;

        // 鏍规嵁缁勪欢绫诲瀷璁剧疆鐩稿叧鍙傛暟
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

        // 妫€鏌ユ枃浠舵墿灞曞悕
        if (!validExtensions.Contains(extension))
        {
            HintService.Hint(Lang.Text("Instance.Resource.Install.UnsupportedFormat", extension, compTypeName, string.Join(", ", validExtensions)),
                HintType.Error);
            return;
        }

        ModBase.Log($"[System] 鏂囦欢涓?{extension} 鏍煎紡锛屽皾璇曚綔涓簕compTypeName}瀹夎");

        // 妫€鏌ュ疄渚嬪吋瀹规€?
        if (compType == ModComp.CompType.Mod && (ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSelect ||
                                                 targetInstance is null || !targetInstance.Modable))
        {
            HintService.Hint(Lang.Text("Instance.Resource.Install.SelectModableInstance"));
            return;
        }

        // 纭瀹夎
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

        // 鎵ц瀹夎
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

            // 鍒锋柊鍒楄〃
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
                $"澶嶅埗{compTypeName}鏂囦欢澶辫触",
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
        }
    }

    /// <summary>
    ///     鑾峰彇褰撳墠鐨勭粍浠惰祫婧愮鐞嗙獥浣撱€?
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

    private void BtnManageInfoExport_Click(object sender, MouseButtonEventArgs e)
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
                    "瀵煎嚭璧勬簮淇℃伅澶辫触",
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
                ExportText(exportContent.Join("\r\n"), PageInstanceLeft.McInstance.Name + "宸插畨瑁呯殑璧勬簮淇℃伅.txt");
                break;
            }

            case 2: // CSV
            {
                var exportContent = new List<string>();
                exportContent.Add("鏂囦欢鍚?璧勬簮鍚嶇О,璧勬簮鐗堟湰,姝ょ増鏈洿鏂版椂闂?Mod ID,瀵瑰簲骞冲彴宸ョ▼ ID,鏂囦欢澶у皬锛堝瓧鑺傦級,鏂囦欢璺緞,鍐呭祵妯＄粍");
                foreach (var ModEntity in ModLocalComp.compResourceListLoader.output)
                    exportContent.Add(
                        $"{ModEntity.FileName},{ModEntity.Comp?.TranslatedName},{ModEntity.Version},{ModEntity.compFile?.ReleaseDate},{ModEntity.ModId},{ModEntity.Comp?.Id},{GetModFileInfo(ModEntity.path).Length},{ModEntity.path},{string.Join(";", _FlattenEmbeddedNames(ModEntity.EmbeddedMods))}");
                ExportText(exportContent.Join("\r\n"), PageInstanceLeft.McInstance.Name + "宸插畨瑁呯殑璧勬簮淇℃伅.csv");
                break;
            }
        }
    }

    private static void _AppendEmbeddedForExport(List<string> lines, List<ModLocalComp.LocalCompFile> mods, int depth)
    {
        var indent = new string('\t', depth);
        foreach (var mod in mods)
        {
            var line = indent + "鈹?" + (mod.Name ?? mod.ModId ?? mod.FileName);
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
    ///     涓嬭浇 Mod銆?
    /// </summary>
    private void BtnManageDownload_Click(object sender, MouseButtonEventArgs e)
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

        PageComp.targetVersion = PageInstanceLeft.McInstance; // 灏嗗綋鍓嶅疄渚嬭缃负绛涢€夊櫒
    }

    /// <summary>
    ///     涓嬭浇鎶曞奖Mod鎸夐挳鐐瑰嚮浜嬩欢銆?
    /// </summary>
    private void BtnSchematicDownloadMod_Click(object sender, MouseButtonEventArgs e)
    {
        ModMain.frmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadMod);
        PageComp.targetVersion = PageInstanceLeft.McInstance; // 灏嗗綋鍓嶅疄渚嬭缃负绛涢€夊櫒
    }

    /// <summary>
    ///     瀹炰緥閫夋嫨鎸夐挳鐐瑰嚮浜嬩欢銆?
    /// </summary>
    private void BtnSchematicVersionSelect_Click(object sender, MouseButtonEventArgs e)
    {
        ModMain.frmMain.PageChange(FormMain.PageType.Launch);
        ModMain.frmMain.PageChange(FormMain.PageType.InstanceSelect);
    }

    #endregion

    #region 閫夋嫨

    /// <summary>
    ///     閫夋嫨鐨?Mod 鐨勮矾寰勶紙涓嶅惈 .disabled 鍜?.old锛夈€?
    /// </summary>
    public HashSet<string> selectedMods = new();

    // 鍗曢」鍒囨崲閫夋嫨鐘舵€?
    public void CheckChanged(MyLocalCompItem sender, ModBase.RouteEventArgs e)
    {
        if (ModAnimation.AniControlEnabled != 0)
            return;
        // 鏇存柊閫夋嫨浜嗙殑鍐呭
        var selectedKey = sender.Entry.RawPath;
        if (sender.Checked)
            selectedMods.Add(selectedKey);
        else
            selectedMods.Remove(selectedKey);
        RefreshBars();
    }

    // 鍒囨崲鎵€鏈夐」鐨勯€夋嫨鐘舵€?
    private void ChangeAllSelected(bool value)
    {
        ModAnimation.AniControlEnabled += 1;
        selectedMods.Clear();
        foreach (var Item in modItems.Values)
        {
            // #4992锛孧od 浠庤繃婊ゅ櫒鐪嬪彲鑳戒笉搴斿湪鍒楄〃涓紝浣嗗洜涓哄垰鍒囨崲鐘舵€佹墍浠ヤ緷鐒朵繚鐣欏湪鍒楄〃涓紝鎵€浠ュ簲璇ヤ粠鍒楄〃 UI 鍒ゆ柇锛岃€岄潪浠庤繃婊ゅ櫒鍒ゆ柇
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

    private void FrmMain_KeyDown(object sender, KeyEventArgs e) // 鑻ョ洃鍚嚜宸辩殑浜嬩欢鍒欏湪杩涘叆椤甸潰鍚庨渶鐐瑰嚮鍙充晶鎺т欢鎵嶅彲鐩戝惉鍒?(#4311)
    {
        if (!ReferenceEquals(ModMain.frmMain.pageRight, this))
            return;
        if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && e.Key == Key.A)
            ChangeAllSelected(true);
    }

    private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Ctrl + A 浼氳鎼滅储妗嗘崟鑾凤紝瀵艰嚧鏃犳硶鍏ㄩ€夛紝鎵€浠ュ湪鎸変笅 Ctrl + A 鏃惰浆绉荤劍鐐逛互渚挎崟鑾?
        if (SearchBox.Text.Any())
            return;
        if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && e.Key == Key.A)
            PanBack.Focus();
    }

    #endregion

    #region 绛涢€?

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
    ///     妫€鏌ヨ Mod 椤规槸鍚︾鍚堝綋鍓嶇瓫閫夌殑绫诲埆銆?
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

    // 鐐瑰嚮绛涢€夐」瑙﹀彂鐨勬敼鍙?
    private void ChangeFilter(MyRadioButton sender, bool raiseByMouse)
    {
        Filter = (FilterType)Convert.ToInt32(sender.Tag);
        RefreshUI();
        DoSort();
    }

    #endregion

    #region 鎺掑簭

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

                // 灏嗗瓙鍏冪礌杞崲涓哄彲鎺掑簭鐨勫垪琛?
                var items = PanList.Children.OfType<MyLocalCompItem>().ToList();
                var method = GetSortMethod(currentSortMethod);

                // 鍒嗙鏈夋晥鍜屾棤鏁堥」锛堜繚鎸佸師濮嬬浉瀵归『搴忥級
                var invalid = items.Where(i =>
                    i.Entry is null || (currentSortMethod == SortMethod.TagNums && i.Entry.Comp is null &&
                                        !i.Entry.IsFolder)).ToList();
                var valid = items.Except(invalid).ToList();
                // 浠呭鏈夋晥椤硅繘琛屾帓搴?
                valid.Sort((x, y) => method(x.Entry, y.Entry));
                // 鍚堝苟淇濇寔鏃犳晥椤圭殑鍘熷椤哄簭
                items = valid.Concat(invalid).ToList();

                // 鎵归噺鏇存柊UI鍏冪礌
                PanList.Children.Clear();
                items.ForEach(i => PanList.Children.Add(i));
            }

            catch (Exception ex)
            {
                ModBase.Log(
                    ex,
                    "鎵ц鎺掑簭鏃跺嚭閿?,
                    ModBase.LogLevel.Hint,
                    userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
            }
        }
    }

    private Func<ModLocalComp.LocalCompFile, ModLocalComp.LocalCompFile, int> GetSortMethod(SortMethod method)
    {
        // 閫氱敤鐨勬枃浠跺す缃《姣旇緝鍑芥暟
        int folderFirstCompare(ModLocalComp.LocalCompFile a, ModLocalComp.LocalCompFile b)
        {
            if (a.IsFolder && !b.IsFolder)
                return -1;
            if (!a.IsFolder && b.IsFolder)
                return 1;
            return 0; // 鐩稿悓绫诲瀷锛岄渶瑕佽繘涓€姝ユ瘮杈?
        }

        ;

        switch (method)
        {
            case SortMethod.FileName:
            {
                return (a, b) =>
                {
                    // 鏂囦欢澶瑰缁堟帓鍦ㄦ渶鍓嶉潰
                    var folderResult = folderFirstCompare(a, b);
                    if (folderResult != 0)
                        return folderResult;
                    // 濡傛灉閮芥槸鏂囦欢澶规垨閮芥槸鏂囦欢锛屽垯鎸夋枃浠跺悕鎺掑簭
                    return string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase);
                };
            }
            case SortMethod.CompName:
            {
                return (a, b) =>
                {
                    // 鏂囦欢澶瑰缁堟帓鍦ㄦ渶鍓嶉潰
                    var folderResult = folderFirstCompare(a, b);
                    if (folderResult != 0)
                        return folderResult;
                    // 濡傛灉閮芥槸鏂囦欢澶规垨閮芥槸鏂囦欢锛屽垯鎸夎祫婧愬悕绉版帓搴?
                    return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                };
            }
            case SortMethod.TagNums:
            {
                return (a, b) =>
                {
                    // 鏂囦欢澶瑰缁堟帓鍦ㄦ渶鍓嶉潰
                    var folderResult = folderFirstCompare(a, b);
                    if (folderResult != 0)
                        return folderResult;
                    // 濡傛灉閮芥槸鏂囦欢澶癸紝鍒欐寜鍚嶇О鎺掑簭
                    if (a.IsFolder && b.IsFolder)
                        return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                    // 濡傛灉閮芥槸鏂囦欢锛屽垯鎸夋爣绛炬暟閲忔帓搴忥紙鏍囩澶氱殑鍦ㄥ墠锛?
                    if (!a.IsFolder && !b.IsFolder)
                    {
                        // 瀹夊叏妫€鏌ワ紝纭繚Comp涓嶄负绌?
                        var aTagCount = a.Comp?.Tags?.Count ?? 0;
                        var bTagCount = b.Comp?.Tags?.Count ?? 0;
                        return bTagCount.CompareTo(aTagCount);
                    }

                    // 鐞嗚涓婁笉浼氬埌杈捐繖閲岋紝浣嗕负浜嗗畨鍏ㄨ捣瑙?
                    return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                };
            }
            case SortMethod.CreateTime:
            {
                return (a, b) =>
                {
                    // 鏂囦欢澶瑰缁堟帓鍦ㄦ渶鍓嶉潰
                    var folderResult = folderFirstCompare(a, b);
                    if (folderResult != 0)
                        return folderResult;
                    // 濡傛灉閮芥槸鏂囦欢澶规垨閮芥槸鏂囦欢锛屽垯鎸夊垱寤烘椂闂存帓搴忥紙鏂扮殑鍦ㄥ墠锛?
                    var aPath = a.IsFolder ? a.ActualPath : a.path;
                    var bPath = b.IsFolder ? b.ActualPath : b.path;
                    var aDate = GetModFileInfo(aPath).CreationTime;
                    var bDate = GetModFileInfo(bPath).CreationTime;
                    if (aDate == DateTime.MinValue && bDate == DateTime.MinValue)
                        return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);

                    if (aDate == DateTime.MinValue) return 1; // 鍑洪敊鐨勬枃浠舵帓鍦ㄥ悗闈?

                    if (bDate == DateTime.MinValue) return -1;
                    return bDate.CompareTo(aDate);
                };
            }
            case SortMethod.ModFileSize:
            {
                return (a, b) =>
                {
                    // 鏂囦欢澶瑰缁堟帓鍦ㄦ渶鍓嶉潰
                    var folderResult = folderFirstCompare(a, b);
                    if (folderResult != 0)
                        return folderResult;
                    // 濡傛灉閮芥槸鏂囦欢澶癸紝鍒欐寜鍚嶇О鎺掑簭
                    if (a.IsFolder && b.IsFolder)
                        return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                    // 濡傛灉閮芥槸鏂囦欢锛屽垯鎸夋枃浠跺ぇ灏忔帓搴忥紙澶х殑鍦ㄥ墠锛?
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

                    // 鐞嗚涓婁笉浼氬埌杈捐繖閲岋紝浣嗕负浜嗗畨鍏ㄨ捣瑙?
                    return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                };
            }

            default:
            {
                return (a, b) =>
                {
                    // 鏂囦欢澶瑰缁堟帓鍦ㄦ渶鍓嶉潰
                    var folderResult = folderFirstCompare(a, b);
                    if (folderResult != 0)
                        return folderResult;
                    // 濡傛灉閮芥槸鏂囦欢澶规垨閮芥槸鏂囦欢锛屽垯鎸夊悕绉版帓搴?
                    return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                };
            }
        }
    }

    #endregion

    #region 涓嬭竟鏍?

    // 鍚敤 / 绂佺敤
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
            var modEntity = ModE; // 浠呯敤浜庡幓闄よ凯浠ｅ彉閲忔棤娉曚慨鏀圭殑闄愬埗
            string newPath = null;
            if (modEntity.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine && !isEnable)
                // 绂佺敤
                newPath = modEntity.path + (File.Exists(modEntity.path + ".old") ? ".old" : ".disabled");
            else if (modEntity.State == ModLocalComp.LocalCompFile.LocalFileStatus.Disabled && isEnable)
                // 鍚敤
                newPath = modEntity.RawPath;
            else
                continue;
            // 閲嶅懡鍚?
            try
            {
                if (File.Exists(newPath))
                {
                    if (File.Exists(modEntity.path))
                    {
                        // 鍚屾椂瀛樺湪涓や釜鍚嶇О鐨?Mod
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
                        // 宸茬粡閲嶅懡鍚嶈繃浜?
                        ModBase.Log("[Mod] Mod 鐨勭姸鎬佸凡琚垏鎹?, ModBase.LogLevel.Debug);
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
                    $"鏈壘鍒伴渶瑕侀噸鍛藉悕鐨?Mod锛坽modEntity.path ?? "null"}锛?,
                    ModBase.LogLevel.Feedback,
                    userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
                ReloadCompFileList(true);
                return;
            }
            catch (Exception ex)
            {
                ModBase.Log(ex, $"閲嶅懡鍚?Mod 澶辫触锛坽modEntity.path ?? "null"}锛?);
                isSuccessful = false;
            }

            // 鏇存敼 Loader 涓殑鍒楄〃
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

            // 鏇存敼 UI 涓殑鍒楄〃
            try
            {
                var newItem = BuildLocalCompItem(newModEntity);
                modItems[modEntity.RawPath] = newItem;
                var indexOfUi = PanList.Children.IndexOf(PanList.Children.OfType<MyLocalCompItem>()
                    .FirstOrDefault(i => ReferenceEquals(i.Entry, modEntity)));
                if (indexOfUi == -1)
                    continue; // 鍥犱负鏈煡鍘熷洜 Mod 鐨勭姸鎬佸凡缁忓垏鎹㈠畬浜?
                PanList.Children.RemoveAt(indexOfUi);
                PanList.Children.Insert(indexOfUi, newItem);
            }
            catch (Exception ex)
            {
                ModBase.Log(
                    ex,
                    $"鏇存柊 UI 鍒楄〃椤瑰け璐ワ細{modEntity.FileName}",
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

    // 鏇存柊
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
    ///     璁板綍姝ｅ湪杩涜 Mod 鏇存柊鐨?mods 鏂囦欢澶硅矾寰勩€?
    /// </summary>
    public static List<string> updatingVersions = new();

    public void UpdateResource(IEnumerable<ModLocalComp.LocalCompFile> modList)
    {
        // 鏇存柊鍓嶈鍛?
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
            // 鏋勯€犱笅杞戒俊鎭?
            modList = modList.ToList(); // 闃叉鍒锋柊褰卞搷杩唬鍣?
            var fileList = new List<DownloadFile>();
            var fileCopyList = new Dictionary<string, string>();
            foreach (var Entry in modList)
            {
                var file = Entry.UpdateFile;
                if (!file.Available)
                    continue;
                // 纭鏇存柊鍚庣殑鏂囦欢鍚?
                var currentReplaceName = Entry.compFile.FileName.Replace(".jar", "").Replace(".old", "")
                    .Replace(".disabled", "");
                var newestReplaceName = Entry.UpdateFile.FileName.Replace(".jar", "").Replace(".old", "")
                    .Replace(".disabled", "");
                var currentSegs = currentReplaceName.Split('-').ToList();
                var newestSegs = newestReplaceName.Split('-').ToList();
                var shortened = false;
                while (true) // 绉婚櫎鍓嶅鐩稿悓閮ㄥ垎锛堜笉鑳界Щ闄ゆ墍鏈夌浉鍚岄」锛岃繖浼氬鑷翠緥濡?1.2-forge-2 鍜?1.3-forge-3 涓棿鐨?forge 琚幓鎺夛紝瀵艰嚧灏濊瘯鏇挎崲 1.2-2锛?
                {
                    if (!currentSegs.Any() || !newestSegs.Any())
                        break;
                    if ((currentSegs.First() ?? "") != (newestSegs.First() ?? ""))
                        break;
                    currentSegs.RemoveAt(0);
                    newestSegs.RemoveAt(0);
                    shortened = true;
                }

                while (true) // 绉婚櫎鍚庡鐩稿悓閮ㄥ垎
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

                // 娣诲姞鍒颁笅杞藉垪琛?
                var tempAddress = ModBase.pathTemp + @"DownloadedComp\" +
                                  Entry.FileName.Replace(currentReplaceName, newestReplaceName);
                var realAddress = ModBase.GetPathFromFullPath(Entry.path) +
                                  Entry.FileName.Replace(currentReplaceName, newestReplaceName);
                fileList.Add(file.ToNetFile(tempAddress, ModComp.DownloadReason.Update));
                fileCopyList[tempAddress] = realAddress;
            }

            // 鏋勯€犲姞杞藉櫒
            var installLoaders = new List<ModLoader.LoaderBase>();
            var finishedFileNames = new List<string>();
            installLoaders.Add(new LoaderDownload(Lang.Text("Instance.Resource.Update.Task.DownloadFiles"), fileList)
                { ProgressWeight = modList.Count() * 1.5d }); // 姣忎釜 Mod 闇€瑕?1.5s
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
                            ModBase.Log($"[CompUpdate] 鏈壘鍒版洿鏂板墠鐨勮祫婧愭枃浠讹紝璺宠繃瀵瑰畠鐨勫垹闄わ細{Entry.path}", ModBase.LogLevel.Debug);

                    foreach (var Entry in fileCopyList)
                    {
                        if (File.Exists(Entry.Value))
                        {
                            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(Entry.Value, UIOption.AllDialogs,
                                RecycleOption.SendToRecycleBin);
                            ModBase.Log($"[Mod] 鏇存柊鍚庣殑璧勬簮鏂囦欢宸插瓨鍦紝灏嗕細鎶婂畠鏀惧叆鍥炴敹绔欙細{Entry.Value}", ModBase.LogLevel.Debug);
                        }

                        if (Directory.Exists(ModBase.GetPathFromFullPath(Entry.Value)))
                        {
                            File.Move(Entry.Key, Entry.Value);
                            finishedFileNames.Add(ModBase.GetFileNameFromPath(Entry.Value));
                        }
                        else
                        {
                            ModBase.Log($"[Mod] 鏇存柊鍚庣殑鐩爣鏂囦欢澶瑰凡琚垹闄わ細{Entry.Value}", ModBase.LogLevel.Debug);
                        }
                    }
                }
                catch (OperationCanceledException ex)
                {
                    ModBase.Log(ex, "鏇挎崲鏃х増璧勬簮鏂囦欢鏃惰涓诲姩鍙栨秷");
                }
            }));
            // 缁撴潫澶勭悊
            var loader =
                new ModLoader.LoaderCombo<IEnumerable<ModLocalComp.LocalCompFile>>(
                    Lang.Text("Instance.Resource.Update.Task.Title", PageInstanceLeft.McInstance.Name), installLoaders);
            var pathMods = PageInstanceLeft.McInstance.PathIndie +
                           (PageInstanceLeft.McInstance.Info.HasLabyMod
                               ? Path.Combine("labymod-neo", "fabric", PageInstanceLeft.McInstance.Info.VanillaName)
                               : "") + ModLocalComp.GetPathNameByCompType(currentCompType) + @"\";
            loader.OnStateChanged = _ =>
            {
                // 缁撴灉鎻愮ず
                switch (loader.State)
                {
                    case ModBase.LoadState.Finished:
                    {
                        switch (finishedFileNames.Count)
                        {
                            case 0: // 涓€鑸槸鐢变簬 Mod 鏂囦欢琚崰鐢紝鐒跺悗鐜╁涓诲姩鍙栨秷
                            {
                                ModBase.Log("[CompUpdate] 娌℃湁璧勬簮琚垚鍔熸洿鏂?);
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

                ModBase.Log($"[CompUpdate] 宸蹭粠姝ｅ湪杩涜璧勬簮鏇存柊鐨勬枃浠跺す鍒楄〃绉婚櫎锛歿pathMods}");
                updatingVersions.Remove(pathMods);
                // 娓呯悊缂撳瓨
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
                        ModBase.Log(ex, "娓呯悊璧勬簮鏇存柊缂撳瓨澶辫触");
                    }
                }, "Clean Comp Update Cache", ThreadPriority.BelowNormal);
            };
            // 鍚姩鍔犺浇鍣?
            ModBase.Log($"[CompUpdate] 寮€濮嬫洿鏂?{modList.Count()} 涓祫婧愶細{pathMods}");
            updatingVersions.Add(pathMods);
            loader.Start();
            ModLoader.LoaderTaskbarAdd(loader);
            ModMain.frmMain.BtnExtraDownload.ShowRefresh();
            ModMain.frmMain.BtnExtraDownload.Ribble();
            ReloadCompFileList(true);
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "鍒濆鍖栬祫婧愭洿鏂板け璐?);
        }
    }

    // 鍒犻櫎
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
            // 纭闇€瑕佸垹闄ょ殑鏂囦欢
            // 鏂囦欢澶瑰彧闇€瑕佸垹闄よ嚜韬?
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
            // 瀹為檯鍒犻櫎鏂囦欢
            foreach (var ModEntity in modList)
            {
                // 鍒犻櫎
                try
                {
                    if (ModEntity.IsFolder)
                    {
                        // 鍒犻櫎鏂囦欢澶?
                        if (isShiftPressed)
                            Directory.Delete(ModEntity.ActualPath, true);
                        else
                            Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(ModEntity.ActualPath,
                                UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
                    }
                    // 鍒犻櫎鏂囦欢
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
                    ModBase.Log(ex, "鍒犻櫎璧勬簮琚富鍔ㄥ彇娑?);
                    ReloadCompFileList(true);
                    return;
                }
                catch (Exception ex)
                {
                    ModBase.Log(
                        ex,
                        $"鍒犻櫎璧勬簮澶辫触锛坽ModEntity.path}锛?,
                        ModBase.LogLevel.Msgbox,
                        userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
                    isSuccessful = false;
                }

                // 鍙栨秷閫変腑
                selectedMods.Remove(ModEntity.RawPath);
                // 鏇存敼 Loader 鍜?UI 涓殑鍒楄〃
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
                ReloadCompFileList(true); // 鍒犻櫎浜嗗叏閮ㄩ」鐩?
            }
            else
            {
                RefreshBars();
            }

            // 鏄剧ず缁撴灉鎻愮ず
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
            ModBase.Log(ex, "鍒犻櫎璧勬簮琚富鍔ㄥ彇娑?);
            ReloadCompFileList(true);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "鍒犻櫎璧勬簮鍑虹幇鏈煡閿欒",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
            ReloadCompFileList(true);
        }

        LoaderRun(ModLoader.LoaderFolderRunType.UpdateOnly);
    }

    // 鍙栨秷閫夋嫨
    private void BtnSelectCancel_Click(object sender, ModBase.RouteEventArgs e)
    {
        ChangeAllSelected(false);
    }

    // 鏀惰棌
    private void BtnSelectFavorites_Click(object sender, ModBase.RouteEventArgs e)
    {
        var selected = ModLocalComp.compResourceListLoader.output
            .Where(m => selectedMods.Contains(m.RawPath) && m.Comp is not null).Select(i => i.Comp).ToList();
        ModComp.CompFavorites.ShowMenu(selected, (Control)sender);
    }

    // 鍒嗕韩
    private void BtnSelectShare_Click(object sender, ModBase.RouteEventArgs e)
    {
        var shareList = ModLocalComp.compResourceListLoader.output
            .Where(m => selectedMods.Contains(m.RawPath) && m.Comp is not null).Select(i => i.Comp.Id).ToHashSet();
        ModBase.ClipboardSet(ModComp.CompFavorites.GetShareCode(shareList));
        ChangeAllSelected(false);
    }

    #endregion

    #region 鍗曚釜璧勬簮椤?

    // 璇︽儏
    public void Info_Click(object sender, EventArgs e)
    {
        try
        {
            var modEntry = ((MyLocalCompItem)(sender is MyIconButton iconButton ? iconButton.Tag : sender)).Entry;
            // 鍒ゆ柇璇?LabyMod 鏄惁鏀寔瀹夎 Fabric Mod
            var moddedLabyMod = PageInstanceLeft.McInstance.Info.HasLabyMod && PageInstanceLeft.McInstance.Modable;
            // 鍔犺浇澶辫触淇℃伅
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
                // 璺宠浆鍒?Mod 涓嬭浇椤甸潰
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
                // 瀵逛簬鍘熺悊鍥炬枃浠讹紝浣跨敤寮傛鍔犺浇閬垮厤UI鍗￠】
                if (modEntry.path.EndsWithF(".litematic", true) || modEntry.path.EndsWithF(".schem", true) ||
                    modEntry.path.EndsWithF(".schematic", true) || modEntry.path.EndsWithF(".nbt", true))
                {
                    ShowSchematicInfoAsync(modEntry);
                    return;
                }

                // 鑾峰彇淇℃伅
                var contentLines = new List<string>();

                // 妫€鏌ユ槸鍚︿负鏂囦欢澶?
                if (modEntry.IsFolder)
                {
                    // 澶勭悊鏂囦欢澶硅鎯?
                    var folderPath = modEntry.ActualPath;
                    if (Directory.Exists(folderPath))
                    {
                        var fileCount = 0;
                        try
                        {
                            // 鏍规嵁褰撳墠璧勬簮绫诲瀷璁＄畻鏂囦欢鏁伴噺
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
                    // 澶勭悊鏅€氭枃浠惰鎯?
                    if (modEntry.Description is not null)
                        contentLines.Add(modEntry.Description + "\r\n");
                    if (modEntry.Authors is not null)
                        contentLines.Add(Lang.Text("Instance.Resource.Item.Info.Author", modEntry.Authors));
                    contentLines.Add(Lang.Text("Instance.Resource.Item.Info.File", modEntry.FileName, ModBase.GetString(GetModFileInfo(modEntry.path).Length)));
                    if (modEntry.Version is not null)
                        contentLines.Add(Lang.Text("Instance.Resource.Item.Info.Version", modEntry.Version));

                    // 鍘熺悊鍥炬枃浠剁殑璇︽儏淇℃伅宸查€氳繃寮傛鏂规硶澶勭悊
                }

                // 鍙湁鏅€氭枃浠舵墠鏄剧ず璋冭瘯淇℃伅
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

                // 鏄剧ず璇︽儏淇℃伅
                if (modEntry.IsFolder)
                {
                    // 鏂囦欢澶瑰彧鏄剧ず鍩烘湰淇℃伅锛屼笉鎻愪緵鎼滅储鍔熻兘
                    ModMain.MyMsgBox(contentLines.Join("\r\n"), modEntry.Name, Lang.Text("Instance.Resource.Item.Info.Return"));
                }
                else
                {
                    // 鑾峰彇鐢ㄤ簬鎼滅储鐨?Mod 鍚嶇О
                    var modOriginalName = modEntry.Name.Replace(" ", "+");
                    var modSearchName = modOriginalName.Substring(0, 1);
                    for (int i = 1, loopTo = modOriginalName.Count() - 1; i <= loopTo; i++)
                    {
                        var isLastLower = modOriginalName[i - 1].ToString().ToLower()
                            .Equals(modOriginalName[i - 1].ToString());
                        var isCurrentLower = modOriginalName[i].ToString().ToLower()
                            .Equals(modOriginalName[i].ToString());
                        if (isLastLower && !isCurrentLower)
                            // 涓婁竴涓瓧姣嶄负灏忓啓锛岃繖涓€涓瓧姣嶄负澶у啓
                            modSearchName += "+";
                        modSearchName += modOriginalName[i].ToString();
                    }

                    modSearchName = modSearchName.Replace("++", "+").Replace("pti+Fine", "ptiFine");
                    // 鏄剧ず
                    if (currentCompType == ModComp.CompType.Schematic || !Lang.IsChineseMainland)
                    {
                        // 鎶曞奖鍘熺悊鍥炬枃浠舵垨闈炰腑鏂囧尯鍩熶笉鏄剧ず鐧剧鎼滅储閫夐」
                        if (modEntry.Url is null)
                            ModMain.MyMsgBox(contentLines.Join("\r\n"), modEntry.Name, Lang.Text("Instance.Resource.Item.Info.Return"));
                        else if (ModMain.MyMsgBox(contentLines.Join("\r\n"), modEntry.Name, Lang.Text("Instance.Resource.Item.Info.OpenWebsite"), Lang.Text("Instance.Resource.Item.Info.Return")) ==
                                 1) ModBase.OpenWebsite(modEntry.Url);
                    }
                    // 鍏朵粬璧勬簮绫诲瀷淇濈暀鐧剧鎼滅储鍔熻兘
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
                "鑾峰彇璧勬簮璇︽儏澶辫触",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
        }
    }

    // 鎵撳紑鏂囦欢鎵€鍦ㄧ殑浣嶇疆
    public void Open_Click(MyIconButton sender, EventArgs e)
    {
        try
        {
            var listItem = (MyLocalCompItem)sender.Tag;
            // 瀵逛簬鏂囦欢澶逛娇鐢ㄥ疄闄呰矾寰勶紝瀵逛簬鏂囦欢浣跨敤鍘熻矾寰?
            var targetPath = listItem.Entry.IsFolder ? listItem.Entry.ActualPath : listItem.Entry.path;
            ModBase.OpenExplorer(targetPath);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "鎵撳紑璧勬簮鏂囦欢浣嶇疆澶辫触",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
        }
    }

    // 鍒犻櫎
    public void Delete_Click(MyIconButton sender, EventArgs e)
    {
        var listItem = (MyLocalCompItem)sender.Tag;
        DeleteMods(new[] { listItem.Entry });
    }

    // 鍚敤 / 绂佺敤
    public void ED_Click(MyIconButton sender, EventArgs e)
    {
        var listItem = (MyLocalCompItem)sender.Tag;
        EDMods(new[] { listItem.Entry }, listItem.Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Disabled);
    }

    /// <summary>
    ///     寮傛鏄剧ず鍘熺悊鍥捐鎯呬俊鎭紝閬垮厤UI鍗￠】
    /// </summary>
    private void ShowSchematicInfoAsync(ModLocalComp.LocalCompFile modEntry)
    {
        // 鏄剧ず鍔犺浇鎻愮ず
        HintService.Hint(Lang.Text("Instance.Resource.Item.Info.LoadingDetail"));

        // 鍦ㄥ悗鍙扮嚎绋嬩腑鍔犺浇NBT鏁版嵁
        // 纭繚 NBT 鏁版嵁宸插姞杞?

        // 鍦?UI 绾跨▼涓樉绀鸿鎯?
        // 鏋勫缓璇︽儏淇℃伅


        // 鏍规嵁鏂囦欢绫诲瀷鏄剧ず璇︾粏淇℃伅

        // 鏄剧ず璋冭瘯淇℃伅

        // 鏄剧ず璇︽儏瀵硅瘽妗?


        // 璁板綍閿欒鏃ュ織浣嗕笉鏄剧ず閿欒鎻愮ず锛屽洜涓洪€氱敤鐨勬枃浠剁姸鎬佹鏌ュ凡缁忓鐞嗕簡
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
                            "鏄剧ず鍘熺悊鍥捐鎯呭け璐?,
                            ModBase.LogLevel.Feedback,
                            userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
                    }
                });
            }
            catch (Exception ex)
            {
                ModBase.Log(
                    ex,
                    "鍔犺浇鍘熺悊鍥?NBT 鏁版嵁澶辫触",
                    ModBase.LogLevel.Feedback,
                    userSummary: Lang.Text("Instance.Resource.Error.OperationFailed"));
            }
        });
    }

    #region 鍘熺悊鍥炬枃浠惰缁嗕俊鎭樉绀?

    /// <summary>
    ///     鏄剧ず Litematic 鏂囦欢鐨勮缁嗕俊鎭?
    /// </summary>
    private void ShowLitematicDetails(List<string> contentLines, ModLocalComp.LocalCompFile modEntry)
    {
        contentLines.Add("");
        contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.DetailInfo"));

        // 鏄剧ず鍘熷鍚嶇О锛堜粠 NBT Metadata/Name 璇诲彇锛?
        if (modEntry.LitematicOriginalName is not null) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.OriginalName") + modEntry.LitematicOriginalName);

        // 鏄剧ず鐗堟湰淇℃伅
        if (modEntry.LitematicVersion.HasValue) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.Version") + modEntry.LitematicVersion.Value);

        // 鏄剧ず灏哄淇℃伅
        if (modEntry.LitematicEnclosingSize is not null) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.EnclosingSize") + modEntry.LitematicEnclosingSize);

        // 鏄剧ず鏂瑰潡鍜屼綋绉粺璁?
        if (modEntry.LitematicTotalBlocks.HasValue)
            contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.TotalBlocks") + Lang.Number(modEntry.LitematicTotalBlocks.Value, "N0"));

        if (modEntry.LitematicTotalVolume.HasValue)
            contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.TotalVolume") + Lang.Number(modEntry.LitematicTotalVolume.Value, "N0"));

        // 鏄剧ず鍖哄煙鏁伴噺
        if (modEntry.LitematicRegionCount.HasValue) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.RegionCount") + modEntry.LitematicRegionCount.Value);

        // 鏄剧ず鏃堕棿淇℃伅
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
    ///     鏄剧ず Schem 鏂囦欢鐨勮缁嗕俊鎭?
    /// </summary>
    private void ShowSchemDetails(List<string> contentLines, ModLocalComp.LocalCompFile modEntry)
    {
        contentLines.Add("");
        contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.DetailInfo"));

        // 鏄剧ず鍘熷鍚嶇О锛堜粠 NBT Metadata/Name 璇诲彇锛?
        if (modEntry.SchemOriginalName is not null) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.OriginalName") + modEntry.SchemOriginalName);

        // 鏄剧ず鐗堟湰淇℃伅
        if (modEntry.StructureGameVersion is not null) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.GameVersion") + modEntry.StructureGameVersion);

        if (modEntry.SpongeVersion.HasValue) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.SpongeVersion") + modEntry.SpongeVersion.Value);

        if (modEntry.StructureDataVersion.HasValue) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.DataVersion") + modEntry.StructureDataVersion.Value);

        // 鏄剧ず灏哄淇℃伅
        if (modEntry.LitematicEnclosingSize is not null) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.EnclosingDimensions") + modEntry.LitematicEnclosingSize);

        // 鏄剧ず鏂瑰潡鍜屼綋绉粺璁?
        if (modEntry.LitematicTotalBlocks.HasValue)
            contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.TotalBlocks") + Lang.Number(modEntry.LitematicTotalBlocks.Value, "N0"));

        if (modEntry.LitematicTotalVolume.HasValue)
            contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.TotalVolume") + Lang.Number(modEntry.LitematicTotalVolume.Value, "N0"));

        // 鏄剧ず鍖哄煙鏁伴噺
        if (modEntry.LitematicRegionCount.HasValue) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.RegionCount") + modEntry.LitematicRegionCount.Value);

        contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.FileType",
            Lang.Text("Instance.Resource.Item.Schematic.FileType.Sponge")));
    }

    /// <summary>
    ///     鏄剧ず Schematic 鏂囦欢鐨勮缁嗕俊鎭?
    /// </summary>
    private void ShowSchematicDetails(List<string> contentLines, ModLocalComp.LocalCompFile modEntry)
    {
        contentLines.Add("");
        contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.DetailInfo"));

        // 鏄剧ず灏哄淇℃伅
        if (modEntry.LitematicEnclosingSize is not null) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.Size") + modEntry.LitematicEnclosingSize);

        // 鏄剧ず鏂瑰潡鍜屼綋绉粺璁?
        if (modEntry.LitematicTotalBlocks.HasValue)
            contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.TotalBlocks") + Lang.Number(modEntry.LitematicTotalBlocks.Value, "N0"));

        if (modEntry.LitematicTotalVolume.HasValue)
            contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.TotalVolume") + Lang.Number(modEntry.LitematicTotalVolume.Value, "N0"));

        contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.FileType",
            Lang.Text("Instance.Resource.Item.Schematic.FileType.Mcedit")));
    }

    /// <summary>
    ///     鏄剧ず NBT 缁撴瀯鏂囦欢鐨勮缁嗕俊鎭?
    /// </summary>
    private void ShowNbtDetails(List<string> contentLines, ModLocalComp.LocalCompFile modEntry)
    {
        contentLines.Add("");
        contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.DetailInfo"));

        // 鏄剧ず浣滆€呬俊鎭?
        if (modEntry.StructureAuthor is not null) contentLines.Add(Lang.Text("Instance.Resource.Item.Info.Author", modEntry.StructureAuthor));

        // 鏄剧ず鐗堟湰淇℃伅
        if (modEntry.StructureGameVersion is not null) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.GameVersion") + modEntry.StructureGameVersion);

        if (modEntry.StructureDataVersion.HasValue) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.DataVersion") + modEntry.StructureDataVersion.Value);

        // 鏄剧ず灏哄淇℃伅
        if (modEntry.LitematicEnclosingSize is not null) contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.EnclosingDimensions") + modEntry.LitematicEnclosingSize);

        // 鏄剧ず鏂瑰潡鍜屼綋绉粺璁?
        if (modEntry.LitematicTotalBlocks.HasValue)
            contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.TotalBlocks") + Lang.Number(modEntry.LitematicTotalBlocks.Value, "N0"));

        if (modEntry.LitematicTotalVolume.HasValue)
            contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.TotalVolume") + Lang.Number(modEntry.LitematicTotalVolume.Value, "N0"));

        // 鏄剧ず鍖哄煙鏁伴噺
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
        // 鎶曞奖鍘熺悊鍥炬枃浠朵笉鏄剧ず鐧剧鎼滅储閫夐」
        if (modEntry.Url is null)
            ModMain.MyMsgBox(contentLines.Join("\r\n"), modEntry.Name, Lang.Text("Instance.Resource.Item.Info.Return"));
        else if (ModMain.MyMsgBox(contentLines.Join("\r\n"), modEntry.Name, Lang.Text("Instance.Resource.Item.Info.OpenWebsite"), Lang.Text("Instance.Resource.Item.Info.Return")) == 1)
            ModBase.OpenWebsite(modEntry.Url);
    }

    #endregion

    #region 鎼滅储

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
                ModBase.Log(ex, "鎼滅储杩囩▼涓彂鐢熷紓甯?);
            }
        }));
    }

    private List<ModLocalComp.LocalCompFile> GetSearchResult(string query)
    {
        // 鏋勯€犺姹?
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

        // 杩涜鎼滅储
        return ModBase.Search(queryList, query, ModBase.MaxLocalSearchDepth, 0.35d).Select(r => r.item).ToList();
    }

    #endregion
}
