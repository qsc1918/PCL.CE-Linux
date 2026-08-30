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
using PCL.Core.UI;
using PCL.Core.UI.Theme;
using PCL.Network;
using PCL.Network.Loaders;
using FileSystem = Microsoft.VisualBasic.FileSystem;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageInstanceSavesDatapack : IRefreshable
{
    #region 鏁版嵁鍖呬俊鎭紦瀛?

    private readonly Dictionary<string, (DateTime CreationTime, long Length)> datapackFileInfoCache = new();

    // 鑾峰彇鏁版嵁鍖呬俊鎭紙甯︾紦瀛橈級
    private (DateTime CreationTime, long Length) GetDatapackFileInfo(string path)
    {
        (DateTime CreationTime, long Length) cacheItem;
        if (datapackFileInfoCache.TryGetValue(path, out cacheItem)) return cacheItem;

        try
        {
            var fileInfo = new FileInfo(path);
            var newItem = (fileInfo.CreationTime, fileInfo.Length);
            if (!datapackFileInfoCache.ContainsKey(path)) datapackFileInfoCache.Add(path, newItem);
            return newItem;
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "鑾峰彇鏁版嵁鍖呬俊鎭け璐? " + path);
            return (DateTime.MinValue, 0L);
        }
    }

    // 椤甸潰鍏抽棴鏃舵竻鐞嗙紦瀛?
    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        datapackFileInfoCache.Clear();
    }

    #endregion

    #region 鍒濆鍖?

    private readonly MyLocalCompItem.SwipeSelect currentSwipSelect;

    public PageInstanceSavesDatapack()
    {
        currentSwipSelect = new MyLocalCompItem.SwipeSelect { TargetFrm = this };

        InitializeComponent();
        Unloaded += Page_Unloaded;
        Loaded += (_, _) => PageOther_Loaded();
        LoaderInit();
        PageExit += UnselectedAllWithAnimation;
        // Handles
        Load.Click += Load_Click;
        BtnManageOpen.Click += BtnManageOpen_Click;
        BtnHintOpen.Click += BtnManageOpen_Click;
        BtnManageSelectAll.Click += BtnManageSelectAll_Click;
        BtnManageInstall.Click += BtnManageInstall_Click;
        BtnHintInstall.Click += BtnManageInstall_Click;
        BtnManageDownload.Click += BtnManageDownload_Click;
        BtnHintDownload.Click += BtnManageDownload_Click;
        BtnManageInfoExport.Click += BtnManageInfoExport_Click;
        Load.StateChanged += (_, _, _) => UnselectedAllWithAnimation();
        SearchBox.PreviewKeyDown += SearchBox_PreviewKeyDown;
        BtnFilterAll.Check += ChangeFilter;
        BtnFilterCanUpdate.Check += ChangeFilter;
        BtnFilterDisabled.Check += ChangeFilter;
        BtnFilterEnabled.Check += ChangeFilter;
        BtnFilterError.Check += ChangeFilter;
        BtnSort.Click += BtnSortClick;
        BtnSelectEnable.Click += BtnSelectEnable_Click;
        BtnSelectDisable.Click += BtnSelectDisable_Click;
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
        res.frm = null;
        res.loaders = new[] { ModComp.CompLoaderType.Minecraft }.ToList();
        res.compPath = Path.Combine(PageInstanceSavesLeft.currentSave, "datapacks");
        res.compType = ModComp.CompType.DataPack;
        return res;
    }

    private bool isLoad;

    public void PageOther_Loaded()
    {
        if (ModMain.frmMain.pageLast.page != FormMain.PageType.CompDetail)
            PanBack.ScrollToHome();
        ModAnimation.AniControlEnabled += 1;
        selectedDatapacks.Clear();
        ReloadDatapackFileList();
        ChangeAllSelected(false);
        ModAnimation.AniControlEnabled -= 1;

        // 闈為噸澶嶅姞杞介儴鍒?
        if (isLoad)
            return;
        isLoad = true;

        ModMain.frmMain.KeyDown += FrmMain_KeyDown;
        // 璋冩暣鎸夐挳杈硅窛锛堣繖鐜╂剰鍎挎病娉曚粠 XAML 鏀癸級
        foreach (MyRadioButton Btn in PanFilter.Children)
            Btn.LabText.Margin = new Thickness(-2, 0d, 8d, 0d);
    }

    /// <summary>
    ///     鍒锋柊鏁版嵁鍖呭垪琛ㄣ€?
    /// </summary>
    public void ReloadDatapackFileList(bool forceReload = false)
    {
        if (LoaderRun(forceReload
                ? ModLoader.LoaderFolderRunType.ForceRun
                : ModLoader.LoaderFolderRunType.RunOnUpdated))
        {
            ModBase.Log("[System] 宸插埛鏂版暟鎹寘鍒楄〃");
            datapackFileInfoCache.Clear();

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
        Refresh();
    }

    void IRefreshable.Refresh()
    {
        RefreshSelf();
    }

    public void Refresh()
    {
        ModMain.frmInstanceSavesDatapack.ReloadDatapackFileList(true);
        ModBase.Log("[Datapack] 鍒锋柊鏁版嵁鍖呭垪琛?);
    }

    private void LoaderInit()
    {
        PageLoaderInit(Load, PanLoad, PanAllBack, null, ModLocalComp.compResourceListLoader,
            _ => LoadUIFromLoaderOutput(), () => ModComp.CompType.DataPack, false);
    }

    private void Load_Click(object sender, MouseButtonEventArgs e)
    {
        if (ModLocalComp.compResourceListLoader.State == ModBase.LoadState.Failed)
            LoaderRun(ModLoader.LoaderFolderRunType.ForceRun);
    }

    public bool LoaderRun(ModLoader.LoaderFolderRunType type)
    {
        var loadPath = Path.Combine(PageInstanceSavesLeft.currentSave, "datapacks");
        return ModLoader.LoaderFolderRun(ModLocalComp.compResourceListLoader, loadPath, type,
            loaderInput: GetRequireLoaderData());
    }

    #endregion

    #region UI 鍖?

    /// <summary>
    ///     宸插姞杞界殑鏁版嵁鍖?UI 缂撳瓨銆侹ey 涓烘暟鎹寘鐨?RawPath銆?
    /// </summary>
    public Dictionary<string, MyLocalCompItem> datapackItems = new();

    /// <summary>
    ///     灏嗗姞杞藉櫒缁撴灉鐨勬暟鎹寘鍒楄〃鍔犺浇涓?UI銆?
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
            }
            else
            {
                // 鏍规嵁缁勪欢绫诲瀷璁剧疆 PanEmpty 鐨勬枃鏈唴瀹?
                TxtEmptyTitle.Text = Lang.Text("Instance.Resource.Datapack.Empty.Title");
                TxtEmptyDescription.Text = Lang.Text("Instance.Resource.Datapack.Empty.Description");

                PanEmpty.Visibility = Visibility.Visible;
                PanBack.Visibility = Visibility.Collapsed;
                return;
            }

            // 淇敼缂撳瓨
            datapackItems.Clear();
            var itemsToShow = ModLocalComp.compResourceListLoader.output.ToList();

            foreach (var DatapackEntity in itemsToShow)
                datapackItems[DatapackEntity.RawPath] = BuildLocalCompItem(DatapackEntity);

            // 鏄剧ず缁撴灉
            ModBase.RunInUi(() =>
            {
                Filter = FilterType.All;
                SearchBox.Text = ""; // 杩欎細瑙﹀彂缁撴灉鍒锋柊锛屾墍浠ラ渶瑕佸湪 DatapackItems 鏇存柊涔嬪悗
                RefreshUI();
                SetSortMethod(SortMethod.CompName);
            });
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "鍔犺浇鏁版嵁鍖呭垪琛?UI 澶辫触",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Saves.Error.OperationFailed"));
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
                Checked = selectedDatapacks.Contains(entry.RawPath)
            };
            newItem.CurrentSwipe = currentSwipSelect;
            newItem.Tags = entry.Tags;
            entry.OnCompUpdate += _ => newItem.Refresh();
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
        sender.Changed += (ss, e) => CheckChanged((MyLocalCompItem)ss, e);

        // 鏂囦欢椤圭殑鐐瑰嚮浜嬩欢锛氬垏鎹㈤€変腑鐘舵€?
        sender.Click += (ss, e) =>
        {
            var s = (MyLocalCompItem)ss;
            s.Checked = !s.Checked;
        };

        // 鍥炬爣鎸夐挳
        var btnOpen = new MyIconButton { LogoScale = 1.05d, SvgIcon = "lucide/folder-open", Tag = sender };
        btnOpen.ToolTip = Lang.Text("Instance.Saves.OpenFileLocation");
        ToolTipService.SetPlacement(btnOpen, PlacementMode.Center);
        ToolTipService.SetVerticalOffset(btnOpen, 30d);
        ToolTipService.SetHorizontalOffset(btnOpen, 2d);
        btnOpen.Click += (sender, e) => Open_Click((MyIconButton)sender, e);

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
        btnDelete.Click += (sender, e) => Delete_Click((MyIconButton)sender, e);

        if (sender.Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine)
        {
            var btnDisable = new MyIconButton { LogoScale = 1d, SvgIcon = "lucide/circle-minus", Tag = sender };
            btnDisable.ToolTip = Lang.Text("Instance.Resource.Disable");
            ToolTipService.SetPlacement(btnDisable, PlacementMode.Center);
            ToolTipService.SetVerticalOffset(btnDisable, 30d);
            ToolTipService.SetHorizontalOffset(btnDisable, 2d);
            btnDisable.Click += (ss, e) => Disable_Click((MyIconButton)ss, e);
            sender.Buttons = new[] { btnCont, btnOpen, btnDisable, btnDelete };
        }
        else if (sender.Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Disabled)
        {
            var btnEnable = new MyIconButton { LogoScale = 1d, SvgIcon = "lucide/circle-check", Tag = sender };
            btnEnable.ToolTip = Lang.Text("Instance.Resource.Enable");
            ToolTipService.SetPlacement(btnEnable, PlacementMode.Center);
            ToolTipService.SetVerticalOffset(btnEnable, 30d);
            ToolTipService.SetHorizontalOffset(btnEnable, 2d);
            btnEnable.Click += (ss, e) => Enable_Click((MyIconButton)ss, e);
            sender.Buttons = new[] { btnCont, btnOpen, btnEnable, btnDelete };
        }
        else
        {
            sender.Buttons = new[] { btnCont, btnOpen, btnDelete };
        }
    }

    /// <summary>
    ///     鍒锋柊鏁翠釜 UI銆?
    /// </summary>
    public void RefreshUI()
    {
        if (PanList is null)
            return;
        var showingDatapacks = (IsSearching ? searchResult : datapackItems.Values.Select(i => i.Entry))
            .Where(m => CanPassFilter(m)).ToList();

        // 瀵规樉绀虹殑鏁版嵁鍖呰繘琛屾帓搴?
        if (showingDatapacks.Any())
        {
            var sortMethod = GetSortMethod(currentSortMethod);
            showingDatapacks.Sort((a, b) => sortMethod(a, b));
        }

        // 閲嶆柊鍒楀嚭鍒楄〃
        ModAnimation.AniControlEnabled += 1;
        if (showingDatapacks.Any())
        {
            PanList.Visibility = Visibility.Visible;
            PanList.Children.Clear();
            foreach (var TargetDatapack in showingDatapacks)
            {
                if (!datapackItems.ContainsKey(TargetDatapack.RawPath))
                    continue;
                var item = datapackItems[TargetDatapack.RawPath];

                // 纭繚鍏冪礌娌℃湁鐖跺鍣紝閬垮厤閲嶅娣诲姞寮傚父
                if (item.Parent is not null) ((Panel)item.Parent).Children.Remove(item);

                ModStyle.MinecraftFormatter.SetColorfulTextLab(item.LabTitle.Text, item.LabTitle,
                    ThemeService.IsDarkMode);
                ModStyle.MinecraftFormatter.SetColorfulTextLab(item.LabInfo.Text, item.LabInfo,
                    ThemeService.IsDarkMode);
                item.Checked = selectedDatapacks.Contains(TargetDatapack.RawPath); // 鏇存柊閫変腑鐘舵€?
                PanList.Children.Add(item);
            }
        }
        else
        {
            PanList.Visibility = Visibility.Collapsed;
        }

        ModAnimation.AniControlEnabled -= 1;
        selectedDatapacks =
            new HashSet<string>(selectedDatapacks.Where(m =>
                showingDatapacks.Any(s => (s.RawPath ?? "") == (m ?? ""))));
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
            var itemSource = (IsSearching ? searchResult : datapackItems.Values.Select(i => i.Entry)).ToArray();
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

            // -----------------
            // 搴曢儴鏍?
            // -----------------

            // 璁℃暟
            var newCount = selectedDatapacks.Count;
            var selected = newCount > 0;
            if (selected)
                LabSelect.Text = Lang.Text("Instance.Resource.SelectedCount", newCount);

            // 鎸夐挳鍙敤鎬?
            if (selected)
            {
                var hasUpdate = false;
                var hasEnabled = false;
                var hasDisabled = false;
                var canFavoriteAndShare = true;


                // 妫€鏌ユ槸鍚︽墍鏈夐€変腑鐨勬暟鎹寘閮芥湁鏈夋晥鐨勯」鐩俊鎭?
                await Task.Run(() =>
                {
                    foreach (var DatapackEntity in ModLocalComp.compResourceListLoader.output)
                        if (selectedDatapacks.Contains(DatapackEntity.RawPath))
                        {
                            if (DatapackEntity.CanUpdate) hasUpdate = true;
                            if (DatapackEntity.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine)
                                hasEnabled = true;
                            else if (DatapackEntity.State == ModLocalComp.LocalCompFile.LocalFileStatus.Disabled)
                                hasDisabled = true;
                            if (DatapackEntity.Comp is null || string.IsNullOrEmpty(DatapackEntity.Comp.Id))
                                canFavoriteAndShare = false;
                        }
                });

                BtnSelectDisable.IsEnabled = hasEnabled;
                BtnSelectEnable.IsEnabled = hasDisabled;
                BtnSelectUpdate.IsEnabled = hasUpdate;
                BtnSelectFavorites.IsEnabled = canFavoriteAndShare;
                BtnSelectShare.IsEnabled = canFavoriteAndShare;
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
                        }, "Datapack Sidebar");
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
                        }, "Datapack Sidebar");
                }
            }
            else
            {
                ModAnimation.AniStop("Datapack Sidebar");
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
    ///     鎵撳紑 datapacks 鏂囦欢澶广€?
    /// </summary>
    private void BtnManageOpen_Click(object sender, EventArgs e)
    {
        try
        {
            var datapackPath = Path.Combine(PageInstanceSavesLeft.currentSave, "datapacks");
            Directory.CreateDirectory(datapackPath);
            ModBase.OpenExplorer(datapackPath);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "鎵撳紑 datapacks 鏂囦欢澶瑰け璐?,
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Saves.Error.OperationFailed"));
        }
    }

    /// <summary>
    ///     鍏ㄩ€夈€?
    /// </summary>
    private void BtnManageSelectAll_Click(object sender, MouseButtonEventArgs e)
    {
        ChangeAllSelected(selectedDatapacks.Count < PanList.Children.Count);
    }

    /// <summary>
    ///     瀹夎鏁版嵁鍖呫€?
    /// </summary>
    private void BtnManageInstall_Click(object sender, MouseButtonEventArgs e)
    {
        var fileList = SystemDialogs.SelectFiles(
            Lang.Text("Instance.Saves.Datapack.Install.FileDialog.Filter"),
            Lang.Text("Instance.Saves.Datapack.Install.FileDialog.Title"));
        if (fileList is null || fileList.Length == 0)
            return;
        InstallDatapackFiles(fileList);
        Refresh();
    }

    /// <summary>
    ///     瀹夎鏁版嵁鍖呮枃浠躲€?
    /// </summary>
    public static void InstallDatapackFiles(IEnumerable<string> filePathList)
    {
        if (!filePathList.Any())
            return;

        var extension = filePathList.First().AfterLast(".").ToLower();

        // 妫€鏌ユ枃浠舵墿灞曞悕
        if (extension != "zip")
        {
            HintService.Hint(Lang.Text("Instance.Resource.Install.UnsupportedFormat", extension, Lang.Text("Download.Comp.Type.DataPack"), "zip"), HintType.Error);
            return;
        }

        // 妫€鏌ュ洖鏀剁珯
        if (filePathList.First().Contains(@":\$RECYCLE.BIN\"))
        {
            HintService.Hint(Lang.Text("Instance.Resource.Install.RestoreFromRecycleBin"), HintType.Error);
            return;
        }

        ModBase.Log($"[System] 鏂囦欢涓?{extension} 鏍煎紡锛屽皾璇曚綔涓烘暟鎹寘瀹夎");

        // 纭瀹夎
        if (!(ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSetup &&
              ModMain.frmMain.PageCurrentSub == FormMain.PageSubType.VersionSavesDatapack))
            if (ModMain.MyMsgBox(Lang.Text("Instance.Saves.Datapack.Install.Message"),
                    Lang.Text("Instance.Saves.Datapack.Install.Title"), Lang.Text("Common.Action.Confirm"),
                    Lang.Text("Common.Action.Cancel")) != 1)
                return;

        // 鎵ц瀹夎
        try
        {
            var datapackFolder = Path.Combine(PageInstanceSavesLeft.currentSave, "datapacks");
            Directory.CreateDirectory(datapackFolder);

            foreach (var FilePath in filePathList)
            {
                var newFileName = ModBase.GetFileNameFromPath(FilePath);
                var destFile = datapackFolder + newFileName;

                if (File.Exists(destFile))
                    if (ModMain.MyMsgBox(Lang.Text("Instance.Resource.Install.OverwriteConfirm.Message", newFileName), Lang.Text("Instance.Resource.Install.OverwriteConfirm.Title"), Lang.Text("Common.Action.Overwrite"), Lang.Text("Common.Action.Cancel")) != 1)
                        continue;

                ModBase.CopyFile(FilePath, destFile);
            }

            if (filePathList.Count() == 1)
                HintService.Hint(Lang.Text("Instance.Resource.Install.SuccessSingle", ModBase.GetFileNameFromPath(filePathList.First())), HintType.Success);
            else
                HintService.Hint(Lang.Text("Instance.Resource.Install.SuccessMultiple", filePathList.Count(), Lang.Text("Download.Comp.Type.DataPack")), HintType.Success);

            // 鍒锋柊鍒楄〃
            if (ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSetup &&
                ModMain.frmMain.PageCurrentSub == FormMain.PageSubType.VersionSavesDatapack)
                if (ModMain.frmInstanceSavesDatapack is not null)
                    ModMain.frmInstanceSavesDatapack.ReloadDatapackFileList(true);
        }

        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "澶嶅埗鏁版嵁鍖呮枃浠跺け璐?,
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Saves.Error.OperationFailed"));
        }
    }

    /// <summary>
    ///     涓嬭浇鏁版嵁鍖呫€?
    /// </summary>
    private void BtnManageDownload_Click(object sender, MouseButtonEventArgs e)
    {
        var datapackPath = Path.Combine(PageInstanceSavesLeft.currentSave, "datapacks");
        Directory.CreateDirectory(datapackPath);
        PageDownloadCompDetail.cachedFolder[ModComp.CompType.DataPack] = datapackPath;
        ModMain.frmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadDataPack);
        PageComp.targetVersion = PageInstanceLeft.McInstance; // 灏嗗綋鍓嶅疄渚嬭缃负绛涢€夊櫒
    }

    /// <summary>
    ///     瀵煎嚭淇℃伅銆?
    /// </summary>
    private void BtnManageInfoExport_Click(object sender, MouseButtonEventArgs e)
    {
        var choice =
            ModMain.MyMsgBox(
                Lang.Text("Instance.Saves.Datapack.Export.Mode.Message"),
                Lang.Text("Instance.Resource.Export.Mode.Title"), Lang.Text("Instance.Resource.Export.Mode.Txt"), Lang.Text("Instance.Resource.Export.Mode.Csv"), Lang.Text("Common.Action.Cancel"));

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
                    "瀵煎嚭鏁版嵁鍖呬俊鎭け璐?,
                    ModBase.LogLevel.Msgbox,
                    userSummary: Lang.Text("Instance.Saves.Error.OperationFailed"));
            }
        }

        ;
        switch (choice)
        {
            case 1: // TXT
            {
                var exportContent = new List<string>();
                foreach (var DatapackEntity in ModLocalComp.compResourceListLoader.output)
                    exportContent.Add(DatapackEntity.FileName);
                ExportText(exportContent.Join("\r\n"),
                    ModBase.GetFolderNameFromPath(PageInstanceSavesLeft.currentSave) + "鐨勬暟鎹寘淇℃伅.txt");
                break;
            }

            case 2: // CSV
            {
                var exportContent = new List<string>();
                exportContent.Add("鏂囦欢鍚?鏁版嵁鍖呭悕绉?鏁版嵁鍖呯増鏈?姝ょ増鏈洿鏂版椂闂?宸ョ▼ ID,鏂囦欢澶у皬锛堝瓧鑺傦級,鏂囦欢璺緞");
                foreach (var DatapackEntity in ModLocalComp.compResourceListLoader.output)
                    exportContent.Add(
                        $"{DatapackEntity.FileName},{DatapackEntity.Comp?.TranslatedName},{DatapackEntity.Version},{DatapackEntity.compFile?.ReleaseDate},{DatapackEntity.Comp?.Id},{GetDatapackFileInfo(DatapackEntity.path).Length},{DatapackEntity.path}");
                ExportText(exportContent.Join("\r\n"),
                    ModBase.GetFolderNameFromPath(PageInstanceSavesLeft.currentSave) + "鐨勬暟鎹寘淇℃伅.csv");
                break;
            }
        }
    }

    #endregion

    #region 閫夋嫨

    /// <summary>
    ///     閫夋嫨鐨勬暟鎹寘鐨勮矾寰勩€?
    /// </summary>
    public HashSet<string> selectedDatapacks = new();

    // 鍗曢」鍒囨崲閫夋嫨鐘舵€?
    public void CheckChanged(MyLocalCompItem sender, ModBase.RouteEventArgs e)
    {
        if (ModAnimation.AniControlEnabled != 0)
            return;
        // 鏇存柊閫夋嫨浜嗙殑鍐呭
        var selectedKey = sender.Entry.RawPath;
        if (sender.Checked)
            selectedDatapacks.Add(selectedKey);
        else
            selectedDatapacks.Remove(selectedKey);
        RefreshBars();
    }

    // 鍒囨崲鎵€鏈夐」鐨勯€夋嫨鐘舵€?
    private void ChangeAllSelected(bool value)
    {
        ModAnimation.AniControlEnabled += 1;
        selectedDatapacks.Clear();
        foreach (var Item in datapackItems.Values)
        {
            var shouldSelected = value && PanList.Children.Contains(Item);
            Item.Checked = shouldSelected;
            if (shouldSelected)
                selectedDatapacks.Add(Item.Entry.RawPath);
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

    private void FrmMain_KeyDown(object sender, KeyEventArgs e)
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
        Unavailable = 4
    }

    /// <summary>
    ///     妫€鏌ヨ鏁版嵁鍖呴」鏄惁绗﹀悎褰撳墠绛涢€夌殑绫诲埆銆?
    /// </summary>
    private bool CanPassFilter(ModLocalComp.LocalCompFile checkingDatapack)
    {
        switch (Filter)
        {
            case FilterType.All:
            {
                return true;
            }
            case FilterType.Enabled:
            {
                return checkingDatapack.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine;
            }
            case FilterType.Disabled:
            {
                return checkingDatapack.State == ModLocalComp.LocalCompFile.LocalFileStatus.Disabled;
            }
            case FilterType.CanUpdate:
            {
                return checkingDatapack.CanUpdate;
            }
            case FilterType.Unavailable:
            {
                return checkingDatapack.State == ModLocalComp.LocalCompFile.LocalFileStatus.Unavailable;
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
        DoSort();
    }

    private enum SortMethod
    {
        FileName,
        CompName,
        CreateTime,
        DatapackFileSize
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
            case SortMethod.CreateTime:
            {
                return Lang.Text("Instance.Resource.Sort.AddTime");
            }
            case SortMethod.DatapackFileSize:
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
                var invalid = items.Where(i => i.Entry is null).ToList();
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
                    userSummary: Lang.Text("Instance.Saves.Error.OperationFailed"));
            }
        }
    }

    private Func<ModLocalComp.LocalCompFile, ModLocalComp.LocalCompFile, int> GetSortMethod(SortMethod method)
    {
        switch (method)
        {
            case SortMethod.FileName:
            {
                return (a, b) => string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase);
            }
            case SortMethod.CompName:
            {
                return (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            }
            case SortMethod.CreateTime:
            {
                return (a, b) =>
                {
                    var aDate = GetDatapackFileInfo(a.path).CreationTime;
                    var bDate = GetDatapackFileInfo(b.path).CreationTime;
                    if (aDate == DateTime.MinValue && bDate == DateTime.MinValue)
                        return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);

                    if (aDate == DateTime.MinValue) return 1;

                    if (bDate == DateTime.MinValue) return -1;
                    return bDate.CompareTo(aDate);
                };
            }
            case SortMethod.DatapackFileSize:
            {
                return (a, b) =>
                {
                    var aSize = GetDatapackFileInfo(a.path).Length;
                    var bSize = GetDatapackFileInfo(b.path).Length;
                    if (aSize == 0L && bSize == 0L)
                        return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);

                    if (aSize == 0L) return 1;

                    if (bSize == 0L) return -1;
                    return bSize.CompareTo(aSize);
                };
            }

            default:
            {
                return (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    #endregion

    #region 涓嬭竟鏍?

    // 鍚敤
    private void BtnSelectEnable_Click(object sender, ModBase.RouteEventArgs e)
    {
        ToggleDatapacks(
            ModLocalComp.compResourceListLoader.output.Where(m => selectedDatapacks.Contains(m.RawPath)).ToList(),
            true);
        ChangeAllSelected(false);
    }

    // 绂佺敤
    private void BtnSelectDisable_Click(object sender, ModBase.RouteEventArgs e)
    {
        ToggleDatapacks(
            ModLocalComp.compResourceListLoader.output.Where(m => selectedDatapacks.Contains(m.RawPath)).ToList(),
            false);
        ChangeAllSelected(false);
    }

    /// <summary>
    ///     鍚敤/绂佺敤鏁版嵁鍖咃紙閫氳繃閲嶅懡鍚嶆枃浠跺す涓?.disabled锛?
    /// </summary>
    private void ToggleDatapacks(IEnumerable<ModLocalComp.LocalCompFile> datapackList, bool isEnable)
    {
        var isSuccessful = true;
        foreach (var DatapackE in datapackList)
        {
            var datapackEntity = DatapackE;
            string newPath = null;

            if (datapackEntity.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine && !isEnable)
                // 绂佺敤 - 娣诲姞 .disabled 鍚庣紑
                newPath = datapackEntity.path + ".disabled";
            else if (datapackEntity.State == ModLocalComp.LocalCompFile.LocalFileStatus.Disabled && isEnable)
                // 鍚敤 - 绉婚櫎 .disabled 鍚庣紑
                newPath = datapackEntity.RawPath;
            else
                continue;

            // 閲嶅懡鍚?
            try
            {
                if (File.Exists(newPath))
                {
                    ModMain.MyMsgBox(Lang.Text("Instance.Saves.Datapack.Replace.FileNameConflict", ModBase.GetFileNameFromPath(newPath)));
                    continue;
                }

                FileSystem.Rename(datapackEntity.path, newPath);
            }
            catch (FileNotFoundException ex)
            {
                ModBase.Log(
                    ex,
                    $"鏈壘鍒伴渶瑕侀噸鍛藉悕鐨勬暟鎹寘锛坽datapackEntity.path ?? "null"}锛?,
                    ModBase.LogLevel.Feedback,
                    userSummary: Lang.Text("Instance.Saves.Error.OperationFailed"));
                ReloadDatapackFileList(true);
                return;
            }
            catch (Exception ex)
            {
                ModBase.Log(ex, $"閲嶅懡鍚嶆暟鎹寘澶辫触锛坽datapackEntity.path ?? "null"}锛?);
                isSuccessful = false;
            }

            // 鏇存敼 Loader 涓殑鍒楄〃
            var newDatapackEntity = new ModLocalComp.LocalCompFile(newPath);
            newDatapackEntity.FromJson(datapackEntity.ToJson());
            if (ModLocalComp.compResourceListLoader.output.Contains(datapackEntity))
            {
                var indexOfLoader = ModLocalComp.compResourceListLoader.output.IndexOf(datapackEntity);
                ModLocalComp.compResourceListLoader.output.RemoveAt(indexOfLoader);
                ModLocalComp.compResourceListLoader.output.Insert(indexOfLoader, newDatapackEntity);
            }

            if (searchResult is not null && searchResult.Contains(datapackEntity))
            {
                var indexOfResult = searchResult.IndexOf(datapackEntity);
                searchResult.Remove(datapackEntity);
                searchResult.Insert(indexOfResult, newDatapackEntity);
            }

            // 鏇存敼 UI 涓殑鍒楄〃
            try
            {
                var newItem = BuildLocalCompItem(newDatapackEntity);
                datapackItems[datapackEntity.RawPath] = newItem;
                var indexOfUi = PanList.Children.IndexOf(PanList.Children.OfType<MyLocalCompItem>()
                    .FirstOrDefault(i => ReferenceEquals(i.Entry, datapackEntity)));
                if (indexOfUi == -1)
                    continue;
                PanList.Children.RemoveAt(indexOfUi);
                PanList.Children.Insert(indexOfUi, newItem);
            }
            catch (Exception ex)
            {
                ModBase.Log(
                    ex,
                    $"鏇存柊 UI 鍒楄〃椤瑰け璐ワ細{datapackEntity.FileName}",
                    ModBase.LogLevel.Hint,
                    userSummary: Lang.Text("Instance.Saves.Error.OperationFailed"));
            }
        }

        Dispatcher.Invoke(() => PanList.UpdateLayout(), DispatcherPriority.Background);

        if (isSuccessful)
        {
            RefreshBars();
        }
        else
        {
            HintService.Hint(Lang.Text("Instance.Saves.Datapack.ToggleWarning"), HintType.Error);
            ReloadDatapackFileList(true);
        }

        LoaderRun(ModLoader.LoaderFolderRunType.UpdateOnly);
    }

    // 鏇存柊
    private void BtnSelectUpdate_Click(object sender, ModBase.RouteEventArgs e)
    {
        var updateList = ModLocalComp.compResourceListLoader.output
            .Where(m => selectedDatapacks.Contains(m.RawPath) && m.CanUpdate).ToList();
        if (!updateList.Any())
            return;
        UpdateResource(updateList);
        ChangeAllSelected(false);
    }

    /// <summary>
    ///     璁板綍姝ｅ湪杩涜鏁版嵁鍖呮洿鏂扮殑 datapacks 鏂囦欢澶硅矾寰勩€?
    /// </summary>
    public static List<string> updatingVersions = new();

    private static bool TryGetSafeDatapackUpdateFileName(ModComp.CompFile file, out string fileName)
    {
        fileName = file.FileName?.Trim() ?? "";
        if (string.IsNullOrEmpty(fileName))
            return false;

        if (!fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return false;

        if (fileName.IndexOfAny(new[] { '\\', '/', ':' }) >= 0)
            return false;

        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return false;

        return fileName == Path.GetFileName(fileName) && fileName != "." && fileName != "..";
    }

    private static bool TryBuildDatapackUpdatePath(string rootPath, string fileName, out string fullPath)
    {
        var fullRootPath = Path.GetFullPath(rootPath);
        if (!fullRootPath.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
            !fullRootPath.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            fullRootPath += Path.DirectorySeparatorChar;

        fullPath = Path.GetFullPath(Path.Combine(fullRootPath, fileName));
        return fullPath.StartsWith(fullRootPath, StringComparison.OrdinalIgnoreCase);
    }

    public void UpdateResource(IEnumerable<ModLocalComp.LocalCompFile> datapackList)
    {
        // 鏇存柊鍓嶈鍛?
        if (!States.Hint.FunctionDatapackUpdate || datapackList.Count() >= 15)
        {
            if (ModMain.MyMsgBox(
                    Lang.Text("Instance.Saves.Datapack.Update.Warning.Message"),
                    Lang.Text("Instance.Saves.Datapack.Update.Warning.Title"), Lang.Text("Instance.Saves.Datapack.Update.Warning.Confirm"), Lang.Text("Common.Action.Cancel"), isWarn: true) == 1)
                States.Hint.FunctionDatapackUpdate = true;
            else
                return;
        }

        try
        {
            // 鏋勯€犱笅杞戒俊鎭?
            datapackList = datapackList.ToList(); // 闃叉鍒锋柊褰卞搷杩唬鍣?
            var fileList = new List<DownloadFile>();
            var fileCopyList = new Dictionary<string, string>();
            var updateEntryList = new List<ModLocalComp.LocalCompFile>();
            var tempRoot = Path.Combine(ModBase.pathTemp, "DownloadedComp");
            var datapackRoot = Path.Combine(PageInstanceSavesLeft.currentSave, "datapacks");
            var skippedUnsafeFileCount = 0;
            foreach (var Entry in datapackList)
            {
                var file = Entry.UpdateFile;
                if (!file.Available)
                    continue;
                if (!TryGetSafeDatapackUpdateFileName(file, out var safeFileName) ||
                    !TryBuildDatapackUpdatePath(tempRoot, safeFileName, out var tempAddress) ||
                    !TryBuildDatapackUpdatePath(datapackRoot, safeFileName, out var realAddress))
                {
                    skippedUnsafeFileCount++;
                    ModBase.Log($"[DatapackUpdate] 宸茶烦杩囦笉瀹夊叏鐨勬暟鎹寘鏇存柊鏂囦欢鍚嶏細{file.FileName}", ModBase.LogLevel.Debug);
                    continue;
                }

                // 娣诲姞鍒颁笅杞藉垪琛?
                fileList.Add(file.ToNetFile(tempAddress, ModComp.DownloadReason.Update,
                    file.RawGameVersions.FirstOrDefault()));
                fileCopyList[tempAddress] = realAddress;
                updateEntryList.Add(Entry);
            }

            if (skippedUnsafeFileCount > 0)
                HintService.Hint(
                    Lang.Text("Instance.Saves.Datapack.Update.UnsafeFilesSkipped", skippedUnsafeFileCount),
                    HintType.Error);
            if (!fileList.Any())
                return;

            // 鏋勯€犲姞杞藉櫒
            var installLoaders = new List<ModLoader.LoaderBase>();
            var finishedFileNames = new List<string>();
            installLoaders.Add(
                new LoaderDownload(Lang.Text("Instance.Saves.Datapack.Update.Task.DownloadFiles"), fileList)
                    { ProgressWeight = updateEntryList.Count * 1.5d });

            installLoaders.Add(new ModLoader.LoaderTask<int, int>(
                Lang.Text("Instance.Saves.Datapack.Update.Task.ReplaceFiles"), _ =>
                {
                    try
                    {
                        foreach (var Entry in updateEntryList)
                            if (File.Exists(Entry.path))
                                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(Entry.path, UIOption.AllDialogs,
                                    RecycleOption.SendToRecycleBin);
                            else
                                ModBase.Log($"[DatapackUpdate] 鏈壘鍒版洿鏂板墠鐨勬暟鎹寘鏂囦欢锛岃烦杩囧瀹冪殑鍒犻櫎锛歿Entry.path}",
                                    ModBase.LogLevel.Debug);

                        foreach (var Entry in fileCopyList)
                        {
                            if (File.Exists(Entry.Value))
                            {
                                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(Entry.Value, UIOption.AllDialogs,
                                    RecycleOption.SendToRecycleBin);
                                ModBase.Log($"[Datapack] 鏇存柊鍚庣殑鏁版嵁鍖呮枃浠跺凡瀛樺湪锛屽皢浼氭妸瀹冩斁鍏ュ洖鏀剁珯锛歿Entry.Value}", ModBase.LogLevel.Debug);
                            }

                            if (Directory.Exists(ModBase.GetPathFromFullPath(Entry.Value)))
                            {
                                File.Move(Entry.Key, Entry.Value);
                                finishedFileNames.Add(ModBase.GetFileNameFromPath(Entry.Value));
                            }
                            else
                            {
                                ModBase.Log($"[Datapack] 鏇存柊鍚庣殑鐩爣鏂囦欢澶瑰凡琚垹闄わ細{Entry.Value}", ModBase.LogLevel.Debug);
                            }
                        }
                    }
                    catch (OperationCanceledException ex)
                    {
                        ModBase.Log(ex, "鏇挎崲鏃х増鏁版嵁鍖呮枃浠舵椂琚富鍔ㄥ彇娑?);
                    }
                }));

            // 缁撴潫澶勭悊
            var loader = new ModLoader.LoaderCombo<IEnumerable<ModLocalComp.LocalCompFile>>(
                Lang.Text("Instance.Saves.Datapack.Update.Task.Title",
                    ModBase.GetFolderNameFromPath(PageInstanceSavesLeft.currentSave)), installLoaders);
            var pathDatapacks = Path.Combine(PageInstanceSavesLeft.currentSave, "datapacks");

            loader.OnStateChanged = _ =>
            {
                switch (loader.State)
                {
                    case ModBase.LoadState.Finished:
                    {
                        switch (finishedFileNames.Count)
                        {
                            case 0:
                            {
                                ModBase.Log("[DatapackUpdate] 娌℃湁鏁版嵁鍖呰鎴愬姛鏇存柊");
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

                ModBase.Log($"[DatapackUpdate] 宸蹭粠姝ｅ湪杩涜鏁版嵁鍖呮洿鏂扮殑鏂囦欢澶瑰垪琛ㄧЩ闄わ細{pathDatapacks}");
                updatingVersions.Remove(pathDatapacks);

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
                        ModBase.Log(ex, "娓呯悊鏁版嵁鍖呮洿鏂扮紦瀛樺け璐?);
                    }
                }, "Clean Datapack Update Cache", ThreadPriority.BelowNormal);
            };

            // 鍚姩鍔犺浇鍣?
            ModBase.Log($"[DatapackUpdate] 寮€濮嬫洿鏂?{datapackList.Count()} 涓暟鎹寘锛歿pathDatapacks}");
            updatingVersions.Add(pathDatapacks);
            loader.Start();
            ModLoader.LoaderTaskbarAdd(loader);
            ModMain.frmMain.BtnExtraDownload.ShowRefresh();
            ModMain.frmMain.BtnExtraDownload.Ribble();
            ReloadDatapackFileList(true);
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "鍒濆鍖栨暟鎹寘鏇存柊澶辫触");
        }
    }

    // 鍒犻櫎
    private void BtnSelectDelete_Click(object sender, ModBase.RouteEventArgs e)
    {
        DeleteDatapacks(ModLocalComp.compResourceListLoader.output.Where(m => selectedDatapacks.Contains(m.RawPath)));
        ChangeAllSelected(false);
    }

    private void DeleteDatapacks(IEnumerable<ModLocalComp.LocalCompFile> datapackList)
    {
        try
        {
            var isSuccessful = true;
            var isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            // 纭闇€瑕佸垹闄ょ殑鏂囦欢
            datapackList = datapackList.SelectMany(target =>
            {
                if (target.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine)
                    return new[] { target.path, target.path + ".disabled" };

                return new[] { target.path, target.RawPath };
            }).Distinct().Where(m => File.Exists(m)).Select(m => new ModLocalComp.LocalCompFile(m)).ToList();

            // 瀹為檯鍒犻櫎鏂囦欢
            foreach (var DatapackEntity in datapackList)
            {
                try
                {
                    if (isShiftPressed)
                        File.Delete(DatapackEntity.path);
                    else
                        Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(DatapackEntity.path,
                            UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
                catch (OperationCanceledException ex)
                {
                    ModBase.Log(ex, "鍒犻櫎鏁版嵁鍖呰涓诲姩鍙栨秷");
                    ReloadDatapackFileList(true);
                    return;
                }
                catch (Exception ex)
                {
                    ModBase.Log(
                        ex,
                        $"鍒犻櫎鏁版嵁鍖呭け璐ワ紙{DatapackEntity.path}锛?,
                        ModBase.LogLevel.Msgbox,
                        userSummary: Lang.Text("Instance.Saves.Error.OperationFailed"));
                    isSuccessful = false;
                }

                // 鍙栨秷閫変腑
                selectedDatapacks.Remove(DatapackEntity.RawPath);
                // 鏇存敼 Loader 鍜?UI 涓殑鍒楄〃
                ModLocalComp.compResourceListLoader.output.Remove(DatapackEntity);
                searchResult?.Remove(DatapackEntity);
                datapackItems.Remove(DatapackEntity.RawPath);
                var indexOfUi = PanList.Children.IndexOf(PanList.Children.OfType<MyLocalCompItem>()
                    .FirstOrDefault(i => i.Entry.Equals(DatapackEntity)));
                if (indexOfUi >= 0)
                    PanList.Children.RemoveAt(indexOfUi);
            }

            RefreshBars();
            if (!isSuccessful)
            {
                HintService.Hint(Lang.Text("Instance.Saves.Datapack.Delete.FileOccupied"), HintType.Error);
                ReloadDatapackFileList(true);
            }
            else if (PanList.Children.Count == 0)
            {
                ReloadDatapackFileList(true);
            }
            else
            {
                RefreshBars();
            }

            if (!isSuccessful)
                return;
            if (isShiftPressed)
            {
                if (datapackList.Count() == 1)
                    HintService.Hint(Lang.Text("Instance.Saves.Datapack.Delete.PermanentSingle", datapackList.Single().FileName), HintType.Success);
                else
                    HintService.Hint(Lang.Text("Instance.Saves.Datapack.Delete.PermanentMultiple", datapackList.Count()), HintType.Success);
            }
            else if (datapackList.Count() == 1)
            {
                HintService.Hint(Lang.Text("Instance.Saves.Datapack.Delete.RecycleSingle", datapackList.Single().FileName), HintType.Success);
            }
            else
            {
                HintService.Hint(Lang.Text("Instance.Saves.Datapack.Delete.RecycleMultiple", datapackList.Count()), HintType.Success);
            }
        }
        catch (OperationCanceledException ex)
        {
            ModBase.Log(ex, "鍒犻櫎鏁版嵁鍖呰涓诲姩鍙栨秷");
            ReloadDatapackFileList(true);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "鍒犻櫎鏁版嵁鍖呭嚭鐜版湭鐭ラ敊璇?,
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Saves.Error.OperationFailed"));
            ReloadDatapackFileList(true);
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
            .Where(m => selectedDatapacks.Contains(m.RawPath) && m.Comp is not null).Select(i => i.Comp).ToList();
        ModComp.CompFavorites.ShowMenu(selected, (Control)sender);
    }

    // 鍒嗕韩
    private void BtnSelectShare_Click(object sender, ModBase.RouteEventArgs e)
    {
        var shareList = ModLocalComp.compResourceListLoader.output
            .Where(m => selectedDatapacks.Contains(m.RawPath) && m.Comp is not null).Select(i => i.Comp.Id).ToHashSet();
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
            var datapackEntry = ((MyLocalCompItem)(sender is MyIconButton iconBtn ? iconBtn.Tag : sender)).Entry;

            // 鍔犺浇澶辫触淇℃伅
            if (datapackEntry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Unavailable)
            {
                ModMain.MyMsgBox(
                    Lang.Text(
                        "Instance.Saves.Datapack.Info.ReadFailed.WithDetail",
                        datapackEntry.FileUnavailableReason.ToString()),
                    Lang.Text("Instance.Saves.Datapack.Info.ReadFailedTitle"));
                return;
            }

            if (datapackEntry.Comp is not null)
            {
                // 璺宠浆鍒版暟鎹寘涓嬭浇椤甸潰
                ModMain.frmMain.PageChange(new FormMain.PageStackData
                {
                    page = FormMain.PageType.CompDetail,
                    additional = (datapackEntry.Comp, new List<string>(), PageInstanceLeft.McInstance.Info.VanillaName,
                        ModComp.CompLoaderType.Minecraft, ModComp.CompType.DataPack, null)
                });
            }
            else
            {
                // 鑾峰彇淇℃伅
                var contentLines = new List<string>();

                if (datapackEntry.Description is not null)
                    contentLines.Add(datapackEntry.Description + "\r\n");
                if (datapackEntry.Authors is not null)
                    contentLines.Add(Lang.Text("Instance.Saves.Datapack.Info.Author") + datapackEntry.Authors);
                contentLines.Add(Lang.Text("Instance.Saves.Datapack.Info.File") + datapackEntry.FileName + "锛? +
                                 ModBase.GetString(GetDatapackFileInfo(datapackEntry.path).Length) + "锛?);
                if (datapackEntry.Version is not null)
                    contentLines.Add(Lang.Text("Instance.Saves.Datapack.Info.Version") + datapackEntry.Version);

                var debugInfo = new List<string>();
                if (datapackEntry.ModId is not null) debugInfo.Add(Lang.Text("Instance.Saves.Datapack.Info.DatapackId") + datapackEntry.ModId);
                if (debugInfo.Any())
                {
                    contentLines.Add("");
                    contentLines.AddRange(debugInfo);
                }

                // 鏄剧ず璇︽儏淇℃伅
                if (datapackEntry.Url is null)
                    ModMain.MyMsgBox(contentLines.Join("\r\n"), datapackEntry.Name, Lang.Text("Instance.Resource.Item.Info.Return"));
                else if (ModMain.MyMsgBox(contentLines.Join("\r\n"), datapackEntry.Name, Lang.Text("Instance.Resource.Item.Info.OpenWebsite"), Lang.Text("Instance.Resource.Item.Info.Return")) == 1)
                    ModBase.OpenWebsite(datapackEntry.Url);
            }
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "鑾峰彇鏁版嵁鍖呰鎯呭け璐?,
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Saves.Error.OperationFailed"));
        }
    }

    // 鎵撳紑鏂囦欢鎵€鍦ㄧ殑浣嶇疆
    public void Open_Click(MyIconButton sender, EventArgs e)
    {
        try
        {
            var listItem = (MyLocalCompItem)sender.Tag;
            ModBase.OpenExplorer(listItem.Entry.path);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "鎵撳紑鏁版嵁鍖呮枃浠朵綅缃け璐?,
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Saves.Error.OperationFailed"));
        }
    }

    // 鍒犻櫎
    public void Delete_Click(MyIconButton sender, EventArgs e)
    {
        var listItem = (MyLocalCompItem)sender.Tag;
        DeleteDatapacks(new[] { listItem.Entry });
    }

    // 鍚敤
    public void Enable_Click(MyIconButton sender, EventArgs e)
    {
        var listItem = (MyLocalCompItem)sender.Tag;
        ToggleDatapacks(new[] { listItem.Entry }, true);
    }

    // 绂佺敤
    public void Disable_Click(MyIconButton sender, EventArgs e)
    {
        var listItem = (MyLocalCompItem)sender.Tag;
        ToggleDatapacks(new[] { listItem.Entry }, false);
    }

    #endregion

    #region 鎼滅储

    public bool IsSearching => !string.IsNullOrWhiteSpace(SearchBox.Text);
    private List<ModLocalComp.LocalCompFile> searchResult;

    public void SearchRun(object sender, EventArgs e)
    {
        try
        {
            if (IsSearching)
            {
                // 鏋勯€犺姹?
                var queryList = new List<ModBase.SearchEntry<ModLocalComp.LocalCompFile>>();
                foreach (var Entry in ModLocalComp.compResourceListLoader.output)
                {
                    var searchSource = new List<ModBase.SearchSource>();
                    searchSource.Add(new ModBase.SearchSource(Entry.Name, 1d));
                    searchSource.Add(new ModBase.SearchSource(Entry.FileName, 1d));
                    if (Entry.Version is not null)
                        searchSource.Add(new ModBase.SearchSource(Entry.Version, 0.2d));
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
                searchResult = ModBase.Search(queryList, SearchBox.Text, ModBase.MaxLocalSearchDepth, 0.35d).Select(r => r.item).ToList();
            }

            RefreshUI();
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "鎼滅储杩囩▼涓彂鐢熷紓甯?);
        }
    }

    #endregion
}
