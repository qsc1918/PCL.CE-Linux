using System.Collections.Specialized;
using System.IO;
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
using PCL.Core.App.Localization;
using PCL.Core.UI;

namespace PCL;

public partial class PageInstanceSaves : IRefreshable
{
    private readonly DispatcherTimer fileSystemRefreshTimer;
    private readonly DispatcherTimer searchTimer;
    private FileSystemWatcher fileSystemWatcher;
    private bool isLoad;

    private object quickPlayFeature = false;

    private List<string> saveFolders = new();
    private string worldPath;

    public PageInstanceSaves()
    {
        InitializeComponent();
        fileSystemRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100d) };
        searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100d) };
        Loaded += PageSetupLaunch_Loaded;
        Unloaded += Page_Unloaded;
        fileSystemRefreshTimer.Tick += FileSystemRefreshTimer_Tick;
        searchTimer.Tick += SearchTimer_Tick;
        SearchBox.TextChanged += SearchRun;
    }

    void IRefreshable.Refresh()
    {
        RefreshSelf();
    }

    private void RefreshSelf()
    {
        Refresh();
        CheckQuickPlay();
    }

    public static void Refresh()
    {
        if (ModMain.frmInstanceSaves is not null)
            ModMain.frmInstanceSaves.Reload();
        ModMain.frmInstanceLeft.ItemWorld.Checked = true;
        HintService.Hint(Lang.Text("Instance.Saves.Status.Refreshing"), log: false);
    }

    private void PageSetupLaunch_Loaded(object sender, RoutedEventArgs e)
    {
        // 閲嶅鍔犺浇閮ㄥ垎
        PanBack.ScrollToHome();
        worldPath = PageInstanceLeft.McInstance.PathIndie + @"saves\";
        if (!Directory.Exists(worldPath))
            Directory.CreateDirectory(worldPath);
        Reload();

        // 闈為噸澶嶅姞杞介儴鍒?
        if (isLoad)
            return;
        isLoad = true;
        CheckQuickPlay();

        // 鍒濆鍖栨枃浠剁郴缁熺洃瑙嗗櫒鍜屾帓搴忔寜閽?
        SetupFileSystemWatcher();
        BtnSort.Click += BtnSortClick;
        SetSortMethod(_currentSortMethod);
    }

    private string GetFolderNameFromPath(string fullPath)
    {
        return string.IsNullOrEmpty(fullPath) ? "" :
            fullPath.EndsWith(@"\") ? new DirectoryInfo(fullPath).Parent?.Name : new DirectoryInfo(fullPath).Name;
    }

    private string GetFileNameFromPath(string fullPath)
    {
        return Path.GetFileName(fullPath);
    }

    private void SetupFileSystemWatcher()
    {
        if (fileSystemWatcher is not null) fileSystemWatcher.Dispose();

        // 纭繚鐩綍瀛樺湪
        if (!Directory.Exists(worldPath))
            Directory.CreateDirectory(worldPath);

        fileSystemWatcher = new FileSystemWatcher();
        fileSystemWatcher.Path = worldPath;
        fileSystemWatcher.IncludeSubdirectories = false;
        fileSystemWatcher.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.LastWrite;

        fileSystemWatcher.Created += OnFileSystemChanged;
        fileSystemWatcher.Deleted += OnFileSystemChanged;
        fileSystemWatcher.Renamed += OnFileSystemChanged;

        fileSystemWatcher.EnableRaisingEvents = true;
    }

    private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
    {
        fileSystemRefreshTimer.Stop();
        fileSystemRefreshTimer.Start();
    }

    private void FileSystemRefreshTimer_Tick(object sender, EventArgs e)
    {
        fileSystemRefreshTimer.Stop();
        ModBase.RunInUi(() => Reload(), true);
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        if (fileSystemWatcher is not null)
        {
            fileSystemWatcher.Created -= OnFileSystemChanged;
            fileSystemWatcher.Deleted -= OnFileSystemChanged;
            fileSystemWatcher.Renamed -= OnFileSystemChanged;
            fileSystemWatcher.Dispose();
            fileSystemWatcher = null;
        }

        fileSystemRefreshTimer.Stop();
        searchTimer.Stop();
    }

    /// <summary>
    ///     纭繚褰撳墠椤甸潰涓婄殑淇℃伅宸叉纭樉绀恒€?
    /// </summary>
    public void Reload()
    {
        ModAnimation.AniControlEnabled += 1;
        PanBack.ScrollToHome();
        LoadFileList();
        ModAnimation.AniControlEnabled -= 1;
    }

    private void RefreshUI()
    {
        try
        {
            if (IsSearching)
            {
                var resultCount = _searchResult is null ? 0 : _searchResult.Count;
                PanListBack.Title = Lang.Text("Instance.Saves.SearchResultTitle", resultCount.ToString());
            }
            else
            {
                PanListBack.Title = Lang.Text("Instance.Saves.SaveListTitle", saveFolders.Count.ToString());
            }

            if (saveFolders.Count == 0)
            {
                PanNoWorld.Visibility = Visibility.Visible;
                PanContent.Visibility = Visibility.Collapsed;
                PanNoWorld.UpdateLayout();
            }
            else
            {
                PanNoWorld.Visibility = Visibility.Collapsed;
                PanContent.Visibility = Visibility.Visible;
                PanContent.UpdateLayout();

                var showingSaves = (IsSearching ? _searchResult : saveFolders).ToList();

                if (showingSaves.Any())
                {
                    var sortMethod = GetSortMethod(_currentSortMethod);
                    showingSaves.Sort((a, b) => sortMethod(a, b));
                }

                ModAnimation.AniControlEnabled += 1;
                PanList.Children.Clear();

                foreach (var curFolder in showingSaves)
                {
                    // 妫€鏌ユ枃浠跺す鏄惁浠嶇劧瀛樺湪
                    if (!Directory.Exists(curFolder)) continue;

                    var saveLogo = Path.Combine(curFolder, "icon.png");
                    var tmpCurFolder = curFolder;
                    if (File.Exists(saveLogo))
                    {
                        var target =
                            $@"{PageInstanceLeft.McInstance.PathInstance}PCL\ImgCache\{ModBase.GetStringMD5(saveLogo)}.png";
                        ModBase.CopyFile(saveLogo, target);
                        saveLogo = target;
                    }
                    else
                    {
                        saveLogo = ModBase.pathImage + "Icons/NoIcon.png";
                    }

                    var worldItem = new MyListItem
                    {
                        Logo = saveLogo,
                        Title = GetFolderNameFromPath(curFolder),
                        Info =
                            Lang.Text("Instance.Saves.CreationTime", Lang.Date(Directory.GetCreationTime(curFolder), "d"), Lang.Date(Directory.GetLastWriteTime(curFolder), "d")),
                        Type = MyListItem.CheckType.Clickable
                    };
                    worldItem.Click += (_, _) => ModMain.frmMain.PageChange(new FormMain.PageStackData
                        { page = FormMain.PageType.VersionSaves, additional = (null, null, null, ModComp.CompLoaderType.Any, ModComp.CompType.Any, tmpCurFolder) });

                    var btnOpen = new MyIconButton
                    {
                        SvgIcon = "lucide/folder-open",
                        ToolTip = Lang.Text("Common.Action.Open")
                    };
                    btnOpen.Click += (_, _) => ModBase.OpenExplorer(tmpCurFolder);
                    var btnDelete = new MyIconButton
                    {
                        SvgIcon = "lucide/trash-2",
                        ToolTip = Lang.Text("Common.Action.Delete")
                    };
                    btnDelete.Click += (_, _) =>
                    {
                        worldItem.IsEnabled = false;
                        worldItem.Info = Lang.Text("Instance.Saves.Deleting");
                        ModBase.RunInNewThread(() =>
                        {
                            try
                            {
                                FileSystem.DeleteDirectory(tmpCurFolder, UIOption.OnlyErrorDialogs,
                                    RecycleOption.SendToRecycleBin);
                                HintService.Hint(Lang.Text("Instance.Saves.DeletedToRecycleBin"));
                                ModBase.RunInUiWait(() => RemoveItem(worldItem));
                            }
                            catch (Exception ex)
                            {
                                ModBase.Log(
                                    ex,
                                    Lang.Text("Instance.Saves.DeleteFailed"),
                                    ModBase.LogLevel.Hint,
                                    userSummary: Lang.Text("Instance.Saves.DeleteFailed"));
                                ModBase.RunInUiWait(() => Reload());
                            }
                        });
                    };
                    var btnCopy = new MyIconButton
                    {
                        SvgIcon = "lucide/copy",
                        ToolTip = Lang.Text("Common.Action.Copy")
                    };
                    btnCopy.Click += (_, _) =>
                    {
                        try
                        {
                            if (Directory.Exists(tmpCurFolder))
                            {
                                Clipboard.SetFileDropList(new StringCollection { tmpCurFolder });
                                HintService.Hint(Lang.Text("Instance.Saves.CopiedToClipboard"));
                                HintService.Hint(Lang.Text("Instance.Saves.CopyPasteWarning"));
                            }
                            else
                            {
                                HintService.Hint(Lang.Text("Instance.Saves.FolderNotFound"));
                            }
                        }
                        catch (Exception ex)
                        {
                            ModBase.Log(
                                ex,
                                Lang.Text("Instance.Saves.CopyFailed"),
                                ModBase.LogLevel.Hint,
                                userSummary: Lang.Text("Instance.Saves.CopyFailed"));
                        }
                    };
                    var btnInfo = new MyIconButton
                    {
                        SvgIcon = "lucide/info",
                        ToolTip = Lang.Text("Instance.Saves.Details")
                    };
                    btnInfo.Click += (_, _) => ModMain.frmMain.PageChange(new FormMain.PageStackData
                        { page = FormMain.PageType.VersionSaves, additional = (null, null, null, ModComp.CompLoaderType.Any, ModComp.CompType.Any, tmpCurFolder) });

                    var btnLaunch = new MyIconButton
                    {
                        SvgIcon = "lucide/play",
                        ToolTip = Lang.Text("Instance.Saves.QuickPlay")
                    };
                    btnLaunch.Click += (_, _) =>
                    {
                        var worldName = GetFileNameFromPath(tmpCurFolder);
                        var launchOptions = new ModLaunch.McLaunchOptions
                        {
                            WorldName = worldName,
                            instance = PageInstanceLeft.McInstance
                        };
                        ModLaunch.McLaunchStart(launchOptions);
                        ModMain.frmMain.PageChange(new FormMain.PageStackData { page = FormMain.PageType.Launch });
                    };
                    if ((bool)quickPlayFeature)
                        worldItem.Buttons = new[] { btnOpen, btnDelete, btnCopy, btnInfo, btnLaunch };
                    else
                        worldItem.Buttons = new[] { btnOpen, btnDelete, btnCopy, btnInfo };

                    PanList.Children.Add(worldItem);
                }

                ModAnimation.AniControlEnabled -= 1;
            }
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                Lang.Text("Instance.Saves.RefreshUiFailed"),
                ModBase.LogLevel.Hint,
                userSummary: Lang.Text("Instance.Saves.RefreshUiFailed"));
        }
    }

    private void CheckQuickPlay()
    {
        try
        {
            var cur = new ModLaunch.LaunchArgument(PageInstanceLeft.McInstance);
            quickPlayFeature = cur.HasArguments("--quickPlaySingleplayer");
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "妫€鏌ュ瓨妗ｅ揩鎹峰惎鍔ㄥけ璐?,
                ModBase.LogLevel.Hint,
                userSummary: Lang.Text("Instance.Saves.Error.OperationFailed"));
        }
    }

    private void LoadFileList()
    {
        try
        {
            ModBase.Log("[World] 鍒锋柊瀛樻。鏂囦欢");
            saveFolders.Clear();
            if (Directory.Exists(worldPath))
                saveFolders = Directory.EnumerateDirectories(worldPath).ToList();
            else
                saveFolders = new List<string>();

            if (ModBase.modeDebug)
                ModBase.Log("[World] 鍏卞彂鐜?" + saveFolders.Count + " 涓瓨妗ｆ枃浠跺す", ModBase.LogLevel.Debug);
            PanList.Children.Clear();
            CheckQuickPlay();

            if (ModBase.modeDebug)
            {
                if ((bool)quickPlayFeature)
                    ModBase.Log("[World] 璇ュ疄渚嬫敮鎸佸瓨妗ｅ揩鎹峰惎鍔?, ModBase.LogLevel.Debug);
                else
                    ModBase.Log("[World] 璇ュ疄渚嬩笉鏀寔瀛樻。蹇嵎鍚姩", ModBase.LogLevel.Debug);
            }

            RefreshUI(); // 纭繚UI鍒锋柊
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                Lang.Text("Instance.Saves.LoadListFailed"),
                ModBase.LogLevel.Hint,
                userSummary: Lang.Text("Instance.Saves.LoadListFailed"));
        }
    }

    private void RemoveItem(MyListItem item)
    {
        if (PanList.Children.IndexOf(item) == -1)
            return;
        PanList.Children.Remove(item);
        RefreshUI();
    }

    private void BtnOpenFolder_Click(object sender, MouseButtonEventArgs e)
    {
        ModBase.OpenExplorer(worldPath);
    }

    private void BtnPaste_Click(object sender, MouseButtonEventArgs e)
    {
        var files = Clipboard.GetFileDropList();
        var loaders = new List<ModLoader.LoaderBase>();
        loaders.Add(new ModLoader.LoaderTask<int, int>("Copy saves", _ =>
        {
            var copied = 0;
            foreach (var i in files)
                try
                {
                    if (Directory.Exists(i))
                    {
                        if (Directory.Exists(worldPath + GetFolderNameFromPath(i)))
                        {
                            HintService.Hint(Lang.Text("Instance.Saves.DuplicateFolder", GetFolderNameFromPath(i)));
                        }
                        else
                        {
                            ModBase.CopyDirectory(i, worldPath + GetFolderNameFromPath(i));
                            copied += 1;
                        }
                    }
                    else
                    {
                        HintService.Hint(Lang.Text("Instance.Saves.SourceNotFolder"));
                    }
                }
                catch (Exception ex)
                {
                    ModBase.Log(
                        ex,
                        Lang.Text("Instance.Saves.PasteFolderFailed"),
                        ModBase.LogLevel.Hint,
                        userSummary: Lang.Text("Instance.Saves.PasteFolderFailed"));
                }

            if (copied > 0)
                HintService.Hint(Lang.Text("Instance.Saves.PastedCount", copied.ToString()), HintType.Success);
            ModBase.RunInUi(() => Reload());
        }));
        var loader = new ModLoader.LoaderCombo<int>($"{PageInstanceLeft.McInstance.Name} - {Lang.Text("Instance.Saves.CopySave")}", loaders)
            { OnStateChanged = ModDownloadLib.LoaderStateChangedHintOnly };
        loader.Start(1);
        ModLoader.LoaderTaskbarAdd(loader);
        ModMain.frmMain.BtnExtraDownload.ShowRefresh();
        ModMain.frmMain.BtnExtraDownload.Ribble();
    }

    private void BtnDownloadNew_Click(object sender, MouseButtonEventArgs e)
    {
        ModMain.frmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadWorld);
        PageComp.targetVersion = PageInstanceLeft.McInstance; // 灏嗗綋鍓嶅疄渚嬭缃负绛涢€夊櫒
    }

    #region 鎼滅储鍜屾帓搴?

    private SortMethod _currentSortMethod = SortMethod.FileName;
    private List<string> _searchResult;

    public bool IsSearching => !string.IsNullOrWhiteSpace(SearchBox.Text);

    private enum SortMethod
    {
        FileName,
        CreateTime,
        ModifyTime
    }

    private string GetSortName(SortMethod method)
    {
        switch (method)
        {
            case SortMethod.FileName:
            {
                return Lang.Text("Instance.Saves.SortFileName");
            }
            case SortMethod.CreateTime:
            {
                return Lang.Text("Instance.Saves.SortCreateTime");
            }
            case SortMethod.ModifyTime:
            {
                return Lang.Text("Instance.Saves.SortModifyTime");
            }

            default:
            {
                return Lang.Text("Instance.Saves.SortFileName");
            }
        }
    }

    private void SetSortMethod(SortMethod target)
    {
        _currentSortMethod = target;
        BtnSort.Text = Lang.Text("Instance.Saves.SortBy", GetSortName(target));
        RefreshUI();
    }

    private void BtnSortClick(object sender, EventArgs e)
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

    private void SearchRun(object sender, EventArgs e)
    {
        searchTimer.Stop();
        searchTimer.Start();
    }

    private void SearchTimer_Tick(object sender, EventArgs e)
    {
        searchTimer.Stop();
        PerformSearch();
    }

    private void PerformSearch()
    {
        try
        {
            if (IsSearching)
            {
                var queryList = new List<ModBase.SearchEntry<string>>();
                foreach (var saveFolder in saveFolders)
                {
                    var folderName = GetFolderNameFromPath(saveFolder);
                    var searchSource = new List<ModBase.SearchSource>();
                    searchSource.Add(new ModBase.SearchSource(folderName, 1d));
                    queryList.Add(new ModBase.SearchEntry<string> { item = saveFolder, searchSource = searchSource });
                }

                _searchResult = ModBase.Search(queryList, SearchBox.Text, ModBase.MaxLocalSearchDepth, 0.35d).Select(r => r.item).ToList();
            }
            else
            {
                _searchResult = null;
            }

            RefreshUI();
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, Lang.Text("Instance.Saves.SearchError"));
        }
    }

    private Func<string, string, int> GetSortMethod(SortMethod method)
    {
        switch (method)
        {
            case SortMethod.FileName:
            {
                return (a, b) => string.Compare(GetFolderNameFromPath(a), GetFolderNameFromPath(b),
                    StringComparison.OrdinalIgnoreCase);
            }
            case SortMethod.CreateTime:
            {
                return (a, b) => Directory.GetCreationTime(b).CompareTo(Directory.GetCreationTime(a));
            }
            case SortMethod.ModifyTime:
            {
                return (a, b) => Directory.GetLastWriteTime(b).CompareTo(Directory.GetLastWriteTime(a));
            }

            default:
            {
                return (a, b) => string.Compare(GetFolderNameFromPath(a), GetFolderNameFromPath(b),
                    StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    #endregion
}
