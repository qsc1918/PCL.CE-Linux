using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Microsoft.VisualBasic.FileIO;
using PCL.Core.App;
using SearchOption = System.IO.SearchOption;
using PCL.Core.App.Localization;
using PCL.Core.UI;

namespace PCL;

public partial class PageInstanceScreenshot : IRefreshable
{
    private bool _AppendLock;
    private int _Offset;

    private List<string> fileList = new();

    private bool isLoad;
    private string screenshotPath;

    public PageInstanceScreenshot()
    {
        InitializeComponent();
        Loaded += PageSetupLaunch_Loaded;
        PanBack.ScrollChanged += RequireAppend;
        BtnOpenFolder.Click += BtnOpenFolder_Click;
        BtnOpenFolderTop.Click += BtnOpenFolder_Click;
    }

    void IRefreshable.Refresh()
    {
        RefreshSelf();
    }

    private void RefreshSelf()
    {
        var ignore = RefreshAsync();
    }

    public static async Task RefreshAsync()
    {
        if (ModMain.frmInstanceScreenshot is not null)
            await ModMain.frmInstanceScreenshot.ReloadAsync();
        ModMain.frmInstanceLeft.ItemScreenshot.Checked = true;
        HintService.Hint(Lang.Text("Instance.Saves.Status.Refreshing"), log: false);
    }

    private void PageSetupLaunch_Loaded(object sender, RoutedEventArgs e)
    {
        // 閲嶅鍔犺浇閮ㄥ垎
        PanBack.ScrollToHome();
        screenshotPath = PageInstanceLeft.McInstance.PathIndie + @"screenshots\";
        if (!Directory.Exists(screenshotPath))
            Directory.CreateDirectory(screenshotPath);
        Dispatcher.BeginInvoke(new Func<Task>(ReloadAsync));

        // 闈為噸澶嶅姞杞介儴鍒?
        if (isLoad)
            return;
        isLoad = true;
    }

    /// <summary>
    ///     纭繚褰撳墠椤甸潰涓婄殑淇℃伅宸叉纭樉绀恒€?
    /// </summary>
    public async Task ReloadAsync()
    {
        ModAnimation.AniControlEnabled += 1;
        PanBack.ScrollToHome();
        await LoadFileListAsync();
        ModAnimation.AniControlEnabled -= 1;
    }

    private void RefreshTip()
    {
        if (fileList.Count.Equals(0))
        {
            PanNoPic.Visibility = Visibility.Visible;
            PanContent.Visibility = Visibility.Collapsed;
        }
        else
        {
            PanNoPic.Visibility = Visibility.Collapsed;
            PanContent.Visibility = Visibility.Visible;
        }
    }
    
    private static string[] allowedSuffix = { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.webp", "*.tiff" };
    
    private async Task LoadFileListAsync()
    {
        ModBase.Log("[Screenshot] 鍒锋柊鎴浘鏂囦欢");
        fileList.Clear();
        if (Directory.Exists(screenshotPath))
        {
            fileList = allowedSuffix
                .SelectMany(suffix => Directory.EnumerateFiles(screenshotPath, suffix, SearchOption.TopDirectoryOnly))
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToList();
        }
        PanList.Children.Clear();
        RefreshTip();
        //FileList = FileList.Where(e => !e.ContainsF(@"\debug\")).ToList(); // 鎺掗櫎璧勬簮鍖呰皟璇曡緭鍑?
        //FileList.Sort((a, b) => new FileInfo(a).CreationTime > new FileInfo(b).CreationTime);
        ModBase.Log("[Screenshot] 鍏卞彂鐜?" + fileList.Count + " 涓埅鍥炬枃浠?);
        if (fileList.Count == 0)
            return;
        await ListAppendAsync(20, 0);
    }

    private void RequireAppend(object sender, ScrollChangedEventArgs e)
    {
        if (fileList.Count != 0 && !_AppendLock && PanBack.VerticalOffset + PanBack.ViewportHeight >= PanBack.ExtentHeight)
        {
            Dispatcher.BeginInvoke(new Func<Task>(async () => await ListAppendAsync()));
        }
    }

    private async Task ListAppendAsync(int count = 20, int offset = -1)
    {
        _AppendLock = true;
        if (offset == -1)
        {
            if (_Offset * count > fileList.Count)
                return;
            offset = _Offset + 1;
            _Offset += 1;
        }
        else
        {
            _Offset = offset;
        }

        if (count * offset > fileList.Count)
            return;
        for (int j = count * offset, loopTo = count * (offset + 1) - 1; j <= loopTo; j++)
        {
            if (j >= fileList.Count)
                break;
            var i = fileList.ElementAt(j);
            try
            {
                if (!File.Exists(i))
                    continue; // 鏂囦欢鍦ㄥ姞杞介€斾腑娑堝け浜?
                if (File.GetAttributes(i).HasFlag(FileAttributes.Hidden))
                    continue; // 闅愯棌鏂囦欢
                if (new FileInfo(i).Length == 0L)
                    continue; // 绌烘枃浠?
                var myCard = new MyCard
                {
                    Margin = new Thickness(7),
                    Tag = i,
                    ToolTip = i.Replace(screenshotPath, "") // 閫傞厤楂樻竻鎴浘妯＄粍
                };
                var grid = new Grid();
                myCard.Children.Add(grid);

                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(9d) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(120d) });
                grid.RowDefinitions.Add(new RowDefinition());

                // 鍥剧墖
                var image = new Image();
                image.Source = await Task.Run(() =>
                {
                    var bitmapImage = new BitmapImage();
                    var loadSource = i;
                    using (var fs = new FileStream(loadSource, FileMode.Open, FileAccess.Read))
                    {
                        bitmapImage.BeginInit();
                        bitmapImage.DecodePixelHeight = 200;
                        bitmapImage.DecodePixelWidth = 400;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.StreamSource = fs;
                        bitmapImage.EndInit();
                        bitmapImage.Freeze();
                    }

                    return bitmapImage;
                });
                image.Stretch = Stretch.Uniform; // 浣垮浘鐗囪嚜閫傚簲鎺т欢澶у皬
                image.Cursor = Cursors.Hand;
                image.MouseLeftButtonDown += (sender, e) =>
                {
                    try
                    {
                        Basics.OpenPath(i);
                    }
                    catch (Exception ex)
                    {
                        ModBase.Log(
                            ex,
                            Lang.Text("Instance.Screenshot.OpenFailed"),
                            ModBase.LogLevel.Hint,
                            userSummary: Lang.Text("Instance.Screenshot.OpenFailed"));
                    }
                }; // 浣跨敤绯荤粺榛樿绋嬪簭鎵撳紑
                Grid.SetRow(image, 1);
                grid.Children.Add(image);

                // 鎸夐挳
                var stackPanel = new StackPanel();
                stackPanel.Orientation = Orientation.Horizontal;
                stackPanel.HorizontalAlignment = HorizontalAlignment.Center;
                stackPanel.Margin = new Thickness(3d, 5d, 3d, 5d);
                Grid.SetRow(stackPanel, 2);
                grid.Children.Add(stackPanel);

                var btnOpen = new MyIconTextButton
                {
                    Name = "BtnOpen",
                    Text = Lang.Text("Common.Action.Open"),
                    LogoScale = 0.8d,
                    SvgIcon = "lucide/folder-open",
                    Tag = i
                };
                btnOpen.Click += (s, ev) => BtnOpen_Click((MyIconTextButton)s, ev);
                stackPanel.Children.Add(btnOpen);
                var btnDelete = new MyIconTextButton
                {
                    Name = "BtnDelete",
                    Text = Lang.Text("Common.Action.Delete"),
                    LogoScale = 0.8d,
                    SvgIcon = "lucide/trash-2",
                    Tag = i
                };
                btnDelete.Click += (s, ev) => BtnDelete_Click((MyIconTextButton)s, ev);
                stackPanel.Children.Add(btnDelete);
                var btnCopy = new MyIconTextButton
                {
                    Name = "BtnCopy",
                    Text = Lang.Text("Common.Action.Copy"),
                    LogoScale = 0.8d,
                    SvgIcon = "lucide/copy",
                    Tag = i
                };
                btnCopy.Click += (s, ev) => BtnCopy_Click((MyIconTextButton)s, ev);
                stackPanel.Children.Add(btnCopy);
                PanList.Children.Add(myCard);
                myCard.Opacity = 0d;
                ModAnimation.AniStart(new[] { ModAnimation.AaOpacity(myCard, 1d, 200) });
            }
            catch (Exception ex)
            {
                ModBase.Log(ex, $"[Screenshot] 鍒涘缓 {i} 鎴浘棰勮澶辫触锛屽浘鍍忓彲鑳芥崯鍧?);
            }
        }

        _AppendLock = false;
    }

    private void RemoveItem(string path)
    {
        try
        {
            foreach (var i in PanList.Children)
                if (((MyCard)i).Tag.Equals(path))
                {
                    PanList.Children.Remove((Control)i);
                    break;
                }

            fileList.Remove(path);
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "鏈兘鎵惧埌瀵瑰簲 UI");
        }
    }

    private string GetPathFromSender(MyIconTextButton sender)
    {
        return (string)sender.Tag;
    }

    private void BtnOpen_Click(MyIconTextButton sender, EventArgs e)
    {
        ModBase.OpenExplorer(GetPathFromSender(sender));
    }

    private void BtnDelete_Click(MyIconTextButton sender, EventArgs e)
    {
        var path = GetPathFromSender(sender);
        try
        {
            FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            RemoveItem(path);
            RefreshTip();
            HintService.Hint(Lang.Text("Instance.Screenshot.Deleted"));
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                Lang.Text("Instance.Screenshot.DeleteFailed"),
                ModBase.LogLevel.Hint,
                userSummary: Lang.Text("Instance.Screenshot.DeleteFailed"));
        }
    }

    private void BtnCopy_Click(MyIconTextButton sender, EventArgs e)
    {
        var imagePath = GetPathFromSender(sender);
        if (File.Exists(imagePath))
        {
            var tryTime = 0;
            while (tryTime <= 5)
                try
                {
                    ModBase.Log("[Screenshot] 灏濊瘯澶嶅埗" + imagePath + "鍒板壀璐存澘");
                    Clipboard.SetImage(new BitmapImage(new Uri(imagePath)));
                    HintService.Hint(Lang.Text("Instance.Screenshot.CopiedToClipboard"));
                    tryTime = 6;
                    return;
                }
                catch (Exception ex)
                {
                    tryTime += 1;
                    ModBase.Log(ex, $"[Screenshot]绗?{tryTime} 娆″鍒跺皾璇曞け璐?);
                }

            HintService.Hint(Lang.Text("Instance.Screenshot.CopyFailed"), HintType.Error);
        }
        else
        {
            HintService.Hint(Lang.Text("Instance.Screenshot.FileNotFound"));
        }
    }

    private void BtnOpenFolder_Click(object sender, MouseButtonEventArgs e)
    {
        if (!Directory.Exists(screenshotPath))
            Directory.CreateDirectory(screenshotPath);
        ModBase.OpenExplorer(screenshotPath);
    }
}
