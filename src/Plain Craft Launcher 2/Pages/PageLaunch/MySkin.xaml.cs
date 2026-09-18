using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Input;
using SkiaSharp;
using PCL.Core.App.Localization;
using PCL.Core.UI;
using PCL.Network;

namespace PCL;

public partial class MySkin : Grid
{
    // [port] WPF 未区分按下/松开 → Avalonia 单击在松开时触发，使用 PointerReleasedEventArgs
    public delegate void ClickEventHandler(object sender, PointerReleasedEventArgs e);

    // [port] Avalonia 命名字段生成器不为 DropShadowEffect（非 Control）生成字段，手动补齐。
    // BtnSkinSave/BtnSkinRefresh/BtnSkinCape 由命名字段生成器从 axaml 生成（勿手动再声明，否则重复定义）。
    private DropShadowEffect ShadowSkin;

    // 皮肤储存
    private bool isChanging;

    // 点击
    private bool isSkinMouseDown;
    public ModLoader.LoaderTask<ModBase.EqualableList<string>, string> loader;

    public MySkin()
    {
        InitializeComponent();
        // [port] 命名字段生成器不为 ContextMenu 内元素/DropShadowEffect 生成字段（不在本控件命名作用域）→ 手动补齐。
        var cm = this.ContextMenu as ContextMenu;
        if (cm is not null)
            foreach (var it in cm.Items)
                if (it is MyMenuItem mi)
                {
                    if (mi.Name == "BtnSkinSave") BtnSkinSave = mi;
                    else if (mi.Name == "BtnSkinRefresh") BtnSkinRefresh = mi;
                    else if (mi.Name == "BtnSkinCape") BtnSkinCape = mi;
                }
        // [port] Grid.Effect 中的 DropShadowEffect（x:Name=ShadowSkin）非 Control，不生成字段 → 从 Effect 取回
        ShadowSkin = (DropShadowEffect)Effect;
        // [port] Avalonia MenuItem 无 WPF 的 Checked 事件 → 改用 ContextMenu.Opening（菜单打开时刷新保存项的可用状态）
        cm?.Opening += MySkin_ContextMenu_Opening;
        PointerEntered += PanSkin_PointerEntered;
        PointerExited += PanSkin_PointerExited;
        PointerPressed += PanSkin_PointerPressed;
        PointerReleased += PanSkin_PointerReleased;
        // Handles
        BtnSkinSave.Click += BtnSkinSave_Click;
        BtnSkinRefresh.Click += RefreshClick;
        BtnSkinCape.Click += BtnSkinCape_Click;
    }

    public string Address
    {
        get => field;
        set
        {
            field = value;
            // [port] WPF ToolTip 实例属性 → Avalonia 静态附加属性 ToolTip.SetTip
            Avalonia.Controls.ToolTip.SetTip(this, string.IsNullOrEmpty(field)
                ? Lang.Text("Common.State.Loading")
                : Lang.Text("Launch.Skin.Change"));
        }
    }

    // 披风
    public bool HasCape
    {
        get => BtnSkinCape.IsVisible == false;
        set => BtnSkinCape.IsVisible = value ? true : false;
    }

    // 事件
    public event ClickEventHandler? Click;

    // 控件动画
    private void PanSkin_PointerEntered(object sender, PointerEventArgs e)
    {
        ModAnimation.AniStart(ModAnimation.AaOpacity(ShadowSkin, 0.8d - ShadowSkin.Opacity, 200, 100), "Skin Shadow");
    }

    private void PanSkin_PointerExited(object sender, PointerEventArgs e)
    {
        ModAnimation.AniStart(ModAnimation.AaOpacity(ShadowSkin, 0.2d - ShadowSkin.Opacity, 200), "Skin Shadow");
        isSkinMouseDown = false;
        ModAnimation.AniStart(
            ModAnimation.AaScaleTransform(this, 1d - ((ScaleTransform)RenderTransform).ScaleX, 60,
                ease: new ModAnimation.AniEaseOutFluent()), "Skin Scale");
    }

    private void PanSkin_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        isSkinMouseDown = true;
        ModAnimation.AniStart(
            ModAnimation.AaScaleTransform(this, 0.9d - ((ScaleTransform)RenderTransform).ScaleX, 60,
                ease: new ModAnimation.AniEaseOutFluent()), "Skin Scale");
    }

    private void PanSkin_PointerReleased(object sender, PointerReleasedEventArgs e)
    {
        ModAnimation.AniStart(
            ModAnimation.AaScaleTransform(this, 1d - ((ScaleTransform)RenderTransform).ScaleX, 60,
                ease: new ModAnimation.AniEaseOutFluent()), "Skin Scale");
        if (!isSkinMouseDown) return;
        isSkinMouseDown = false;
        Click?.Invoke(sender, e);
    }

    // 保存皮肤
    public void BtnSkinSave_Click(object sender, RoutedEventArgs e)
    {
        Save(loader);
    }

    public static async void Save(ModLoader.LoaderTask<ModBase.EqualableList<string>, string> loader)
    {
        var address = loader.output;
        if (loader.State != ModBase.LoadState.Finished)
        {
            HintService.Hint(Lang.Text("Launch.Skin.Fetching"), HintType.Error);
            if (loader.State != ModBase.LoadState.Loading)
                loader.Start();
            return;
        }

        try
        {
            var fileAddress = await SystemDialogs.SelectSaveFileAsync(Lang.Text("Launch.Skin.SaveDialog.Title"),
                ModBase.GetFileNameFromPath(address),
                Lang.Text("Launch.Skin.SaveDialog.Filter"));
            if (!fileAddress.Contains(@"\")) return;
            File.Delete(fileAddress);
            if (address.StartsWith(ModBase.pathImage))
            {
                var image = new MyBitmap(address);
                image.Save(fileAddress);
            }
            else
            {
                ModBase.CopyFile(address, fileAddress);
            }

            HintService.Hint(Lang.Text("Launch.Skin.SaveSuccess"), HintType.Success);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                Lang.Text("Launch.Skin.Save.Error"),
                ModBase.LogLevel.Hint,
                userSummary: Lang.Text("Launch.Skin.Save.Error"));
        }
    }

    private void MySkin_ContextMenu_Opening(object sender, EventArgs e)
    {
        BtnSkinSave.IsEnabled = string.IsNullOrEmpty(Address);
    }

    /// <summary>
    ///     载入皮肤。
    /// </summary>
    public void Load()
    {
        try
        {
            // 检查文件存在
            Address = loader.output;
            if (string.IsNullOrEmpty(Address))
                throw new Exception("皮肤加载器 " + loader.name + " 没有输出");
            if (!Address.StartsWith(ModBase.pathImage) && !File.Exists(Address))
                throw new FileNotFoundException("皮肤文件未找到", Address);
            // 加载
            MyBitmap image;
            try
            {
                image = new MyBitmap(Address);
            }
            catch (Exception ex) // #2272
            {
                ModBase.Log(
                    ex,
                    Lang.Text("Launch.Skin.Load.Error.Corrupted", Address),
                    ModBase.LogLevel.Hint,
                    userSummary: Lang.Text("Launch.Skin.Load.Error.Corrupted", Address));
                File.Delete(Address);
                return;
            }

            ImgBack.Tag = Address;
            // 大小检查
            var scale = (int)Math.Round(image.pic.Width / 64d);
            if (image.pic.Width < 32 || image.pic.Height < 32)
            {
                ImgFore.Source = null;
                ImgBack.Source = null;
                throw new Exception("图片大小不足，长为 " + image.pic.Height + "，宽为 " + image.pic.Width);
            }

            MyBitmap skinHead = null;
            // 头发层（附加层）
            if (image.pic.Width >= 64 && image.pic.Height >= 32)
            {
                if (image.pic.GetPixel(1, 1).Alpha == 0 ||
                    image.pic.GetPixel(image.pic.Width - 1, image.pic.Height - 1).Alpha == 0 ||
                    image.pic.GetPixel(image.pic.Width - 2, (int)Math.Round(image.pic.Height / 2d - 2d)).Alpha == 0 ||
                    (image.pic.GetPixel(1, 1) != image.pic.GetPixel(scale * 41, scale * 9) &&
                     image.pic.GetPixel(image.pic.Width - 1, image.pic.Height - 1) !=
                     image.pic.GetPixel(scale * 41, scale * 9) &&
                     image.pic.GetPixel(image.pic.Width - 2, (int)Math.Round(image.pic.Height / 2d - 2d)) !=
                     image.pic.GetPixel(scale * 41, scale * 9))) // 如果图片中有任何透明像素（避免纯色白底）
                    // 或是头部颜色和透明区均不一样
                {
                    ImgFore.Source = (Bitmap)image.Clip(scale * 40, scale * 8, scale * 8, scale * 8);
                    skinHead = image.Clip(scale * 40, scale * 8, scale * 8, scale * 8);
                }
                else
                {
                    ImgFore.Source = null;
                }
            }
            else
            {
                ImgFore.Source = null;
            }

            // 脸层
            ImgBack.Source = (Bitmap)image.Clip(scale * 8, scale * 8, scale * 8, scale * 8);
            // 用于显示档案列表头像的图片
            var skinHeadId = Address.Between(new[] { Address.Contains("Images/Skins/") ? "Skins/" : @"Skin\" }[0],
                ".png");
            var cachePath = ModBase.pathTemp + $@"Cache\Skin\Head\{skinHeadId}.png";
            ModProfile.selectedProfile.SkinHeadId = skinHeadId;
            ModProfile.SaveProfile();
            // [port] System.Drawing(GDI+) 不可用于 Linux → 改用 SkiaSharp 合成头像（等价：NearestNeighbor 缩放）。
            var completeHead = new SKBitmap(56, 56);
            using (var g = new SKCanvas(completeHead))
            {
                g.Clear(SKColors.Transparent);
                var paint = new SKPaint { FilterQuality = SKFilterQuality.None, IsAntialias = false };
                g.DrawBitmap(image.pic, new SKRect(scale * 8, scale * 8, scale * 16, scale * 16),
                    new SKRect(4, 4, 52, 52), paint);
                if (ImgFore.Source is not null)
                    g.DrawBitmap(image.pic, new SKRect(scale * 40, scale * 8, scale * 48, scale * 16),
                        new SKRect(0, 0, 56, 56), paint);
            }

            if (!Directory.Exists(ModBase.pathTemp + @"Cache\Skin\Head"))
                Directory.CreateDirectory(ModBase.pathTemp + @"Cache\Skin\Head");
            using (var img = SKImage.FromBitmap(completeHead))
            using (var data = img.Encode(SKEncodedImageFormat.Png, 100))
            using (var fs = new FileStream(cachePath, FileMode.Create))
                data.SaveTo(fs);
            ModBase.Log("[Skin] 载入头像成功：" + loader.name);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                Lang.Text("Launch.Skin.Load.Error.Avatar", $"{(Address ?? "null")},{loader.name}"),
                ModBase.LogLevel.Hint,
                userSummary: Lang.Text("Launch.Skin.Load.Error.Avatar", $"{(Address ?? "null")},{loader.name}"));
        }
    }

    // [port] ScaleToSize 为 System.Drawing(GDI+) 私有辅助且未被调用 → 移除（Linux 无 GDI+）。
    /// <summary>
    ///     清空皮肤。
    /// </summary>
    public void Clear()
    {
        Address = "";
        ImgFore.Source = null;
        ImgBack.Source = null;
    }

    // 刷新缓存
    public void RefreshClick(object sender, RoutedEventArgs e)
    {
        RefreshCache(loader);
    }

    /// <summary>
    ///     刷新皮肤缓存。
    /// </summary>
    public static void RefreshCache(ModLoader.LoaderTask<ModBase.EqualableList<string>, string> sender = null)
    {
        var hasLoaderRunning =
            PageLaunchLeft.skinLoaders.Any(skinLoader => skinLoader.State == ModBase.LoadState.Loading);

        if (ModMain.frmLaunchLeft is not null && hasLoaderRunning)
            // 由于 Abort 不是实时的，暂时不会释放文件，会导致删除报错，故只能取消执行
            HintService.Hint(Lang.Text("Launch.Skin.Refresh.Busy"));
        else
            // 清空缓存
            // 刷新控件
            ModBase.RunInThread(() =>
            {
                try
                {
                    HintService.Hint(Lang.Text("Launch.Skin.Refreshing"));
                    ModBase.Log("[Skin] 正在清空皮肤缓存");
                    if (Directory.Exists(ModBase.pathTemp + @"Cache\Skin"))
                        ModBase.DeleteDirectory(ModBase.pathTemp + @"Cache\Skin");
                    if (Directory.Exists(ModBase.pathTemp + @"Cache\Uuid"))
                        ModBase.DeleteDirectory(ModBase.pathTemp + @"Cache\Uuid");
                    ModBase.IniClearCache(ModBase.pathTemp + @"Cache\Skin\IndexMs.ini");
                    ModBase.IniClearCache(ModBase.pathTemp + @"Cache\Skin\IndexAuth.ini");
                    ModBase.IniClearCache(ModBase.pathTemp + @"Cache\Uuid\Mojang.ini");
                    foreach (var SkinLoader in sender is not null
                                 ? new[] { sender }
                                 : new[] { PageLaunchLeft.skinLegacy, PageLaunchLeft.skinMs })
                        SkinLoader.WaitForExit(isForceRestart: true);
                    HintService.Hint(Lang.Text("Launch.Skin.RefreshSuccess"), HintType.Success);
                }
                catch (Exception ex)
                {
                    ModBase.Log(
                        ex,
                        Lang.Text("Launch.Skin.Refresh.Error"),
                        ModBase.LogLevel.Msgbox,
                        userSummary: Lang.Text("Launch.Skin.Refresh.Error"));
                }
            });
    }

    /// <summary>
    ///     在更换正版皮肤后，刷新正版皮肤。
    /// </summary>
    /// <param name="skinAddress">新的正版皮肤完整地址。</param>
    public static void ReloadCache(string skinAddress)
    {
        // 更新缓存
        // 刷新控件
        // 完成提示
        ModBase.RunInThread(() =>
        {
            try
            {
                ModBase.WriteIni(ModBase.pathTemp + @"Cache\Skin\IndexMs.ini", ModProfile.selectedProfile.Uuid,
                    skinAddress);
                ModBase.Log($"[Skin] 已写入皮肤地址缓存 {ModProfile.selectedProfile.Uuid} -> {skinAddress}");
                PageLaunchLeft.skinMs.WaitForExit(isForceRestart: true);
                HintService.Hint(Lang.Text("Launch.Skin.ChangeSuccess"), HintType.Success);
            }
            catch (Exception ex)
            {
                ModBase.Log(
                    ex,
                    Lang.Text("Launch.Skin.Change.Error.MsRefresh"),
                    ModBase.LogLevel.Feedback,
                    userSummary: Lang.Text("Launch.Skin.Change.Error.MsRefresh"));
            }
        });
    }

    public void BtnSkinCape_Click(object sender, RoutedEventArgs e)
    {
        // 检查条件，获取新披风
        if (isChanging)
        {
            HintService.Hint(Lang.Text("Launch.Skin.Cape.Changing"));
            return;
        }

        if (ModLaunch.mcLoginMsLoader.State == ModBase.LoadState.Failed)
        {
            HintService.Hint(Lang.Text("Launch.Skin.Cape.LoginFailed"), HintType.Error);
            return;
        }

        HintService.Hint(Lang.Text("Launch.Skin.Cape.FetchingList"));
        isChanging = true;
        // 开始实际获取
        ModBase.RunInNewThread(() =>
        {
            try
            {
                // 获取登录信息
                if (ModLaunch.mcLoginMsLoader.State != ModBase.LoadState.Finished)
                    ModLaunch.mcLoginMsLoader.WaitForExit(ModProfile.GetLoginData());
                if (ModLaunch.mcLoginMsLoader.State != ModBase.LoadState.Finished)
                {
                    HintService.Hint(Lang.Text("Launch.Skin.Cape.LoginFailed"), HintType.Error);
                    return;
                }

                var accessToken = ModLaunch.mcLoginMsLoader.output.AccessToken;
                var uuid = ModLaunch.mcLoginMsLoader.output.Uuid;
                var skinData = (JsonObject)ModBase.GetJson(ModLaunch.mcLoginMsLoader.output.ProfileJson);
                foreach (var itemSkin in skinData["capes"].AsArray())
                {
                    if (itemSkin["url"] is null)
                        continue;
                    var localFile = $@"{ModBase.pathTemp}Cache\Capes\{itemSkin["alias"]}.png";
                    var capeFrontFile = $@"{ModBase.pathTemp}Cache\Capes\{itemSkin["alias"]}-front.png";
                    if (File.Exists(localFile) && File.Exists(capeFrontFile))
                    {
                        itemSkin["url"] = capeFrontFile;
                        continue;
                    }

                    FileDownloader.DownloadByLoader(itemSkin["url"].ToString(), localFile);
                    // [port] System.Drawing(GDI+) → SkiaSharp：截取披风正面 (1,0,11,17) 区域并保存。
                    var capeFrontSrc = new SKRect(1, 0, 12, 17);
                    var capeImage = new MyBitmap(localFile);
                    var capeFront = new SKBitmap(11, 17);
                    using (var gra = new SKCanvas(capeFront))
                    {
                        gra.Clear(SKColors.Transparent);
                        gra.DrawBitmap(capeImage.pic, capeFrontSrc, new SKRect(0, 0, 11, 17),
                            new SKPaint { FilterQuality = SKFilterQuality.None });
                    }
                    using (var img = SKImage.FromBitmap(capeFront))
                    using (var data = img.Encode(SKEncodedImageFormat.Png, 100))
                    using (var fs = new FileStream(capeFrontFile, FileMode.Create))
                        data.SaveTo(fs);
                    itemSkin["url"] = capeFrontFile;
                }

                // 获取玩家的所有披风
                int? selId = null;
                ModBase.RunInUiWait(() =>
                {
                    try
                    {
                        var selectionControl = new List<IMyRadio>
                        {
                            new MyListItem
                            {
                                Title = Lang.Text("Launch.Skin.Cape.None"),
                                Info = "Null"
                            }
                        };
                        selectionControl.AddRange(from Cape in skinData["capes"].AsArray()
                            let CapeAlias = Cape["alias"].ToString()
                            let CapeName = _GetCapeDisplayName(CapeAlias)
                            let state = Cape["state"]
                            let active = state is not null && state.ToString().ToUpper().Equals("ACTIVE")
                            select new MyListItem
                            {
                                Title = CapeName,
                                Info = Cape["alias"].ToString(),
                                Checked = active,
                                Type = MyListItem.CheckType.RadioBox,
                                Logo = (string)Cape["url"],
                                LogoScale = 0.8d
                            });

                        selId = ModMain.MyMsgBoxSelect(selectionControl, Lang.Text("Launch.Skin.Cape.SelectTitle"),
                            Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel"));
                    }
                    catch (Exception ex)
                    {
                        ModBase.Log(
                            ex,
                            Lang.Text("Launch.Skin.Cape.Error.List"),
                            ModBase.LogLevel.Feedback,
                            userSummary: Lang.Text("Launch.Skin.Cape.Error.List"));
                    }
                });
                if (selId is null)
                    return;
                // 发送请求
                var result = Requester.Fetch("https://api.minecraftservices.com/minecraft/profile/capes/active",
                    new FetchParam
                    {
                        Method = selId is 0 ? "DELETE" : "PUT",
                        Content = selId is 0
                            ? ""
                            : new JsonObject { ["capeId"] = skinData["capes"][(int)(selId - 1)]["id"]?.ToString() }.ToJsonString(),
                        ContentType = "application/json",
                        Headers = new Dictionary<string, string> { { "Authorization", "Bearer " + accessToken } }
                    }
                );
                if (result.Contains("\"errorMessage\""))
                    HintService.Hint(
                        Lang.Text("Launch.Skin.Cape.ChangeFailedWithReason",
                            ((JsonObject)ModBase.GetJson(result))["errorMessage"]), HintType.Error);
                else
                    HintService.Hint(Lang.Text("Launch.Skin.Cape.ChangeSuccess"), HintType.Success);
            }
            catch (Exception ex)
            {
                ModBase.Log(
                    ex,
                    Lang.Text("Launch.Skin.Cape.ChangeFailed"),
                    ModBase.LogLevel.Hint,
                    userSummary: Lang.Text("Launch.Skin.Cape.ChangeFailed"));
            }
            finally
            {
                isChanging = false;
            }
        }, "Cape Change");
    }

    private static string _GetCapeDisplayName(string capeAlias)
    {
        var safeName = capeAlias
            .Replace("-", "")
            .Replace(" ", "")
            .Replace("'", "");
        var key = $"Launch.Skin.Cape.Name.{safeName}";
        var name = Lang.Text(key);
        if (name == $"!{key}!" || name == key)
            return capeAlias;
        return name;
    }
}
