# PCL-CE 功能清单

> 调查对象：`D:\a\j\PCL-CE`（Plain Craft Launcher Community Edition，WPF .NET Minecraft 启动器）
> 用途：指导向 Linux（PCL.CE-Linux）移植的功能盘点与排期。
> 调查方法：① 通读语言文件 `PCL.Core\App\Localization\Languages\zh-CN.xaml` 全部 3011 个文案键并按前缀分组（Setup 636 / Instance 582 / Download 420 / Minecraft 338 / Launch 227 / Tools 205 / Crash 141 / Select 118 / Main 58 / Link 53 / Update 29 / LogPage 28 / Watcher 27 / Music 17 …）；② 读 `FormMain.xaml(.cs)` 导航与页面枚举；③ 读 `PCL.Core\App\Config.cs` 全部配置项定义；④ 抽读各 Page 的 XAML 控件与 code-behind；⑤ 读 Modules 各 `Mod*.cs` 的 region/公开方法；⑥ 扫描全部 DllImport/LibraryImport/注册表访问确认 Windows 依赖；⑦ git log 交叉验证。
> 所有条目均有文件路径证据；Windows 专属依赖单独标注。

---

## 优先级 P0（用户点名现在就要移植的，必须全部覆盖）

### P0-1 版本安装（组合安装流程）
- **入口**：下载页各版本列表 → 点"安装" → 安装页 `PageDownloadInstall`
- **涉及文件**：`Plain Craft Launcher 2\Pages\PageDownload\PageDownloadInstall.xaml(.cs)`、`Plain Craft Launcher 2\Pages\PageDownload\ModDownloadLib.cs`（#合并安装 / McInstall / McInstallLoader / McInstallFailedClearFolder）
- **平台依赖**：无 Windows 专属（纯网络 + 文件操作）

| 子功能 | 说明与证据 |
| --- | --- |
| 实例名称输入与校验 | `TextSelectName`（MaxLength 70），PageDownloadInstall.xaml L31 |
| 加载器组合选择（11 种卡片） | Forge / Cleanroom / NeoForge / Fabric / LegacyFabric / Fabric API / LegacyFabric API / LabyMod / OptiFine / OptiFabric / LiteLoader 卡片，可选中可清空（`CardForge`/`CardCleanroom`/`CardNeoForge`/`CardFabric`/`CardLegacyFabric`/`CardFabricApi`/`CardLegacyFabricApi`/`CardLabyMod`/`CardOptiFine`/`CardOptiFabric`/`CardLiteLoader`，PageDownloadInstall.xaml L46-339） |
| 加载器兼容性检查与警告 | `Download.Install.Compat.*`：OptiFine×Forge 互斥、需要 Fabric / LegacyFabric / OptiFine 前置、OptiFabricOriginsRequired、IncompatibleWithLiteLoader 等（zh-CN.xaml） |
| Fabric API / LegacyFabric API 附加安装 | `Download.Install.Warning.FabricApi` / `Warning.LegacyFabricApi`（安装 Fabric 时提示附带 API） |
| OptiFabric / OptiFine 组合警告 | `Download.Install.Warning.OptiFabric` / `Warning.OptiFabricOld` / `Warning.ModOptiFine` |
| 版本隔离策略警告 | `Download.Install.InstanceIsolation.Warning.*`（安装时提示该组合将启用/改变版本隔离，可继续/取消） |
| 开始安装与安装管线 | `BtnStart`（Download.Install.StartDownload）→ `ModDownloadLib.McInstall` / `McInstallLoader`：初始化路径 → 读取并校验 JSON → 处理库与资产 → 保存（ModDownloadLib.cs L3736/L3791/L4188/L4279/L4364/L4562） |
| 安装失败清理 | `McInstallFailedClearFolder`（L3710）：安装失败后清空残留文件夹 |
| 安装状态反馈 | `Download.Install.State.*`（Getting/Loading/NoVersion/NoAvailableVersion/GetVersionListFailed 等） |

### P0-2 游戏下载（原版/各加载器版本列表）
- **入口**：主窗口"下载"标签 → 左栏"版本安装"区（PageDownloadLeft L140-277：ItemClient/ItemOptiFine/ItemForge/ItemNeoForge/ItemCleanroom/ItemFabric/ItemLegacyFabric/ItemLabyMod/ItemLiteLoader）
- **涉及文件**：`Pages\PageDownload\PageDownloadClient|Forge|NeoForge|Cleanroom|Fabric|LegacyFabric|LiteLoader|OptiFine|LabyMod.xaml(.cs)`、`Modules\Minecraft\ModDownload.cs`（版本列表数据）、`Pages\PageDownload\ModDownloadLib.cs`（各下载菜单：McDownloadListItem / ForgeDownloadListItem / NeoForgeDownloadListItem / CleanroomDownloadListItem / FabricDownloadListItem / LegacyFabricDownloadListItem / OptiFineDownloadListItem / LiteLoaderDownloadListItem / LabyModDownloadListItem）
- **平台依赖**：无（注意"服务端批量下载脚本"为 .bat，Windows 专属）

| 子功能 | 说明与证据 |
| --- | --- |
| 原版版本列表 | PageDownloadClient：正式/快照/预览/愚人节/远古分类（`Download.Version.Type.Release/Snapshot/Preview/AprilFools/BeforeRelease/Installer`）、"最新正式/最新开发"分组（`Download.Version.Fresh.*`、`Latest.*`）、发布日期（ReleaseDate）、版本总数（VersionListCount） |
| 版本更新日志查看 | `Download.Version.Changelog` + `ModDownloadLib.McUpdateLogShow`（L599） |
| Forge 版本列表 | 推荐版本标注（`McDownloadForgeRecommendedGet/Refresh`，ModDownloadLib L2547-2596）、Forge 类型筛选（`Download.Version.Forge.Type`）、介绍卡（Forge.Intro.*） |
| NeoForge 版本列表 | PageDownloadNeoForge + `NeoForgeDownloadListItemPreload`（L2627）、介绍卡（NeoForge.Intro.*） |
| Cleanroom 版本列表 | PageDownloadCleanroom + `CleanroomDownloadListItemPreload`（L2755）、介绍卡（Cleanroom.Intro.*） |
| Fabric / LegacyFabric 版本列表 | PageDownloadFabric / PageDownloadLegacyFabric、介绍卡（Fabric.Intro.* / LegacyFabric.Intro.*） |
| OptiFine 版本列表 | 快照标注（`Download.Version.Optifine.Snapshot`）、与 Forge 兼容/不兼容标注（Optifine.CompatibleForge / IncompatibleForge） |
| LiteLoader 版本列表 | PageDownloadLiteLoader + 介绍卡（LiteLoader.Intro.*） |
| LabyMod 版本列表 | 生产版/快照版双列表（`McDownloadLabyModProductionLoaderSave` / `SnapshotLoaderSave`，L3210/L3258）、介绍卡（LabyMod.Intro.*） |
| 下载源切换 | 官方/镜像（BMCLAPI）：`Setup.GameManage.Source.Version(.Mirror)`、`Config.Download.VersionListSource`（ToolDownloadVersion） |
| 仅保存安装器 | 各列表"仅保存"菜单：`McDownloadMenuSave`（L532）、`ForgeSave_Click`、`NeoForgeSave_Click`、`CleanroomSave_Click`、`OptiFineSave_Click`、`LiteLoaderSave_Click`、`McDownloadForgelikeSave`、`Download.Version.SaveAs/SaveInstaller/SelectSaveLocation` |
| 服务端批量下载脚本 | `Download.Version.DownloadServer` + `Minecraft.Download.ServerBatch.*`（生成 .bat 批量下载脚本，ModDownloadLib L492 echo 指令）——**Windows 专属（bat 脚本）** |
| 列表辅助操作 | `Download.Version.OpenWebsite`（打开官网）、`ViewAllVersions`、`AllVersions`、下载服务器选择 |

### P0-3 游戏安装（模组加载器安装与整合包导入）
- **入口**：下载页各版本列表"安装"；实例选择页/游戏文件夹页"导入整合包"；主窗口拖拽 zip/rar/mrpack
- **涉及文件**：`Pages\PageDownload\ModDownloadLib.cs`（McDownloadClient / McDownloadOptiFine / McDownloadLiteLoader / McDownloadForgelikeSave / McDownloadFabricLoaderSave / McDownloadLegacyFabricLoaderSave / McDownloadLabyMod*Save）、`Modules\Minecraft\ModModpack.cs`、`FormMain.xaml.cs`（FileDrag L1179+）
- **平台依赖**：无

| 子功能 | 说明与证据 |
| --- | --- |
| 原版客户端安装 | `McDownloadClient` → `McDownloadClientCore`（ModDownloadLib L68/L135：下载客户端 JSON+jar 并入库） |
| OptiFine 单独安装 | `McDownloadOptiFine`（L610，下载安装器并自动静默安装） |
| LiteLoader 安装 | `McDownloadLiteLoader`（L1293） |
| Forge/NeoForge（Forgelike）安装 | `McDownloadForgelikeSave`（L1621，含"Forgelike 文件/原版文件"处理 region） |
| Fabric / LegacyFabric loader 安装 | `McDownloadFabricLoaderSave`（L2859）/ `McDownloadLegacyFabricLoaderSave`（L2985），可附带 Fabric API / LegacyFabric API / OptiFabric（FabricApiDownloadListItem L3129 / OptiFabricDownloadListItem L3147） |
| LabyMod 安装 | L3210/L3258（生产/快照） |
| 合并安装（多加载器组合） | `#合并安装` region（L3509）：一次安装多个加载器组合（对应 P0-1 卡片式 UI） |
| 整合包导入安装 | `ModModpack.ModpackInstall()`（ModModpack.cs L23/L54）：支持 CurseForge / Modrinth(mrpack) / HMCL / MCBBS / 带启动器的压缩包 / 普通压缩包 / MultiMC 七种来源解析（region 见 L343/L659/L901/L978/L1128/L1219/L1285），自动创建版本并下载依赖 |
| 根目录整合包自动识别 | 添加文件夹时若 mods≥3 且 versions≤3 且有可加装实例且隔离目录无 mods → 自动关闭单版本隔离（PageSelectLeft.xaml.cs L401-469） |
| 安装进度与错误 | `Minecraft.Download.Stage.*`（80 条阶段文案）、`Minecraft.Download.Modpack.*`（47 条）、`Minecraft.Download.Error.*`（37 条） |

### P0-4 版本管理（实例列表与游戏文件夹管理）
- **入口**：启动页"选择实例"按钮（`Launch.Home.SelectInstance`）→ 实例选择页（PageSelectLeft/Right）；启动页左栏
- **涉及文件**：`Pages\PageSelectLeft.xaml.cs`（全量读毕）、`Pages\PageSelectRight.xaml(.cs)`、`Modules\Minecraft\ModFolder.cs`、`Modules\Minecraft\ModInstanceList.cs`、`Modules\Minecraft\McInstance.cs`、`Modules\Minecraft\McInstanceCardType.cs`
- **平台依赖**：无

| 子功能 | 说明与证据 |
| --- | --- |
| 游戏文件夹列表与切换 | `PageSelectLeft.McFolderListUI`（L47-292）：单选列表 + 选中即切换（`States.Game.SelectedFolder`）；下载中禁止切换/添加（L346-349、L727-731） |
| 新建 .minecraft | `Create_Click`（L483）：创建目录结构 + 生成 launcher_profiles.json（`ModFolder.McFolderLauncherProfilesJsonCreate`） |
| 添加现有文件夹 | `Add_Click`（L342）：目录选择器 + 权限检查 + 子目录自动探测（versions 下探）+ 显示名输入 + 重复检测 |
| 导入整合包入口 | 左栏"导入整合包"项 → `ModModpack.ModpackInstall()`（L250-262） |
| 文件夹重命名/恢复默认名 | `Rename_Click` / `Restore_Click`（L329/L669），官方默认文件夹也可改名（Types.RenamedOriginal） |
| 文件夹排序 | 右键上移/下移（L294-318）+ 拖拽排序（`Item_MouseMove/DragEnter/DragOver/DragLeave/Drop`，L741-900） |
| 打开/刷新文件夹 | `Open_Click`（资源管理器）/ `Refresh_Click`（清实例缓存 `PCL.ini InstanceCache` 后强制重载） |
| 从列表移除（可选清理） | `Remove_Click`（L505）：可选删除 PCL.ini 与各版本 PCL 配置目录，或保留 |
| 删除文件夹 | `Delete_Click`（L576）：两次警告确认（第二次为红色警告），唯一文件夹时变为"清空"（保留空目录） |
| 实例列表加载与分类 | `ModInstanceList.mcInstanceListLoader`（加载/分类/排序/缓存 4 个 region）；卡片类型 `McInstanceCardType`：收藏(Star)/常规/API/原版类(OriginalLike)/低使用(Rubbish)/愚人节(Fool)/隐藏(Hidden)/错误(Error) |
| 实例卡片信息 | 图标/名称/描述/是否可加装 Mod/启动次数（`Instance.Overall.Info.LaunchCount.*`）、整合包信息（`Instance.Overall.Info.ModpackVersion`） |
| 实例搜索与隐藏实例 | `Select.Instance.Search.*`（搜索框提示/无结果）；F11 在实例选择页切换显示隐藏实例（FormMain.xaml.cs L748） |
| 实例选择记忆 | `States.Game.SelectedInstance` → 写入所选文件夹 `PCL.ini` 的 `Version` 键（ModSetup.LaunchInstanceSelect L172-176） |
| 版本描述标签 | `Select.Instance.Description.*`：正式版/预发布版/快照/实验性快照/需继承/无权限/旧版/愚人节等 14 种状态描述 |

### P0-5 实例管理（实例总览/设置/操作）
- **入口**：实例选择页实例卡片"实例设置"→ 实例设置页（PageInstanceLeft，副页面 PageType.InstanceSetup）
- **涉及文件**：`Pages\PageInstance\PageInstanceOverall.xaml(.cs)`、`PageInstanceSetup.xaml(.cs)`、`PageInstanceInstall.xaml(.cs)`、`PageInstanceLeft.xaml(.cs)`、`Modules\Minecraft\McInstance.cs`
- **平台依赖**：无

| 子功能 | 说明与证据 |
| --- | --- |
| 实例总览信息卡 | 信息卡（加载动画 + 实例信息/整合包版本/启动次数），PageInstanceOverall.xaml L13-16 |
| 个性化：图标 | 内置图标集（Anvil/GrassBlock/Fabric/NeoForgeFox/Quilt 等 14 种，`Instance.Overall.Icon.*`）+ 自定义图片文件选择（Icon.SelectFile.*） |
| 个性化：重命名/描述/收藏/分类 | `BtnDisplayRename/BtnDisplayDesc/BtnDisplayStar` + 分类选择（Regular/Modable/LessUsed/AprilFools/Hidden，`Instance.Overall.Category.*`） |
| 快捷方式 | 版本文件夹/存档文件夹/mods 文件夹三个直达按钮（PageInstanceOverall.xaml L88-103） |
| 导出启动脚本 | `BtnManageScript`（生成 .bat —— **Windows 专属**，详见 P0-10） |
| 测试游戏 | `BtnManageTest`（启动当前实例验证可用性） |
| 修复文件 | `BtnManageCheck`（RepairFiles：校验并修复实例文件，`Instance.Overall.Repair.*` 含进度/取消/失败详情/可关闭校验提示） |
| 重置实例 | `BtnManageRestore`（`Instance.Overall.Reset.*`：确认弹窗；Quilt 实例不支持 Reset.QuiltUnsupported） |
| 补丁核心 | `BtnManagePatch`（PatchCore：选择补丁文件修改核心，`Instance.Overall.Patch.*`） |
| 删除实例 | `BtnManageDelete`（回收站/永久两种，隔离实例单独提示 `Delete.ConfirmMessageIsolated`） |
| 实例设置页 | PageInstanceSetup：内存（含跟随全局）/Java 选择/版本隔离/游戏窗口标题/左下角版本信息/服务器自动进入/高级（JVM 参数/游戏参数/类路径头/资产验证/预启动指令/渲染器/调试 Log4j2/全局代理/忽略 Java 兼容性/禁用文件校验）（`Instance.Setup.*` 全套键 + `Config.Instance`/`Config.InstanceAuth` 各项） |
| 实例安装页（改装/继承） | PageInstanceInstall：为现有实例修改/重装加载器、原版版本继承选择（`Select.Instance.Description.NeedInherit`、`Instance.Install.Action.StartModify/StartReset`） |
| 分区设置初始化 | 设置左栏每个分组带"初始化/刷新"小按钮（PageSetupLeft.xaml L16/L25/L37/L55/L72/L90/L108/L126/L130/L134/L148），重置对应分区设置（`Setup.Left.Reset.*`） |

### P0-6 内存管理
- **入口**：设置 → 启动 → 游戏内存卡片；实例设置 → 内存
- **涉及文件**：`Pages\PageSetup\PageSetupLaunch.xaml(.cs)`（RamType）、`Pages\PageInstance\PageInstanceSetup.xaml(.cs)`（RamType）、`Modules\Base\ModSetup.cs`（LaunchRamType/VersionRamType）、`PCL.Core\App\Config.cs`（Launch.MemoryAllocationMode/CustomMemorySize、Instance.MemorySolution/CustomMemorySize）、`PCL.Core\Utils\OS\SystemInfo.cs`/`HardwareInfo.cs`
- **平台依赖**：读取系统物理内存（SystemInfo，Linux 可用 /proc/meminfo 替代）

| 子功能 | 说明与证据 |
| --- | --- |
| 内存分配模式（自动/自定义） | `Config.Launch.MemoryAllocationMode`（LaunchRamType），自动按系统内存比例（`Setup.Launch.Memory.Auto(.ToolTip)`） |
| 自定义内存大小 | `Config.Launch.CustomMemorySize` + 滑条；显示可用内存（`Memory.AvailableSuffix`）与已用/总内存（`Memory.UsedTotal`） |
| 危险警告 | 32 位 Java 内存上限警告（`Memory.Warning.Java32Bit`）、分配过大警告（`Memory.Warning.TooMuch`） |
| 固定堆大小 | `Config.Launch.LockMemory`（LaunchAdvanceLockMemory）：-Xms=-Xmx，利于 ZGC（配置注释引 issue #3282） |
| 版本级内存覆盖 | `Config.Instance.MemorySolution/CustomMemorySize`（ArgConfig，含"跟随全局"），PageInstanceSetup.RamType（ModSetup.VersionRamType L654） |
| 游戏内存占用显示 | 启动后 `Instance.Setup.Memory.Used`（版本级显示） |

### P0-7 实例隔离（版本隔离）
- **入口**：设置 → 启动 → 版本隔离策略；实例设置 → 版本隔离
- **涉及文件**：`PCL.Core\App\Config.cs`（Launch.IndieSolutionV2、Instance.IndieV1/IndieV2）、`Pages\PageSetup\PageSetupLaunch.xaml(.cs)`、`Pages\PageInstance\PageInstanceSetup.xaml(.cs)`、`Modules\Minecraft\McInstance.cs`（PathIndie/PathInstance）
- **平台依赖**：无

| 子功能 | 说明与证据 |
| --- | --- |
| 全局隔离策略 V2 | `Config.Launch.IndieSolutionV2`（LaunchArgumentIndieV2）：全部隔离 / 仅非正式版 / 仅模组化实例 / 自定义（`Setup.Launch.Options.InstanceIsolation.All/NonRelease/ModdedOnly(+ToolTip)/DefaultPolicyHint`） |
| 版本级隔离覆盖 | `Config.Instance.IndieV1`（int，-1 跟随）/`IndieV2`（bool）+ "跟随全局"选项（`Instance.Setup.FollowGlobal`） |
| 隔离目录语义 | 隔离开启时 saves/config/mods/resourcepacks 等落在 `versions\<版本>\`（`McInstance.PathInstance`），否则落在游戏根目录（`McInstance.PathIndie`）；启动参数占位符 `{version_indie}/{verindie}` 输出隔离目录（ModMain.ArgumentReplace L934） |
| 安装时隔离警告 | `Download.Install.InstanceIsolation.Warning.*`（P0-1） |
| 添加文件夹自动调整 | 根目录整合包自动关闭单版本隔离（PageSelectLeft.AddFolder，P0-3） |
| 旧版 V1 兼容 | `VersionArgumentIndie`（int）保留旧三态（关闭/仅隔离/全部?），与 V2 并存迁移 |

### P0-8 模组下载（社区模组搜索与安装）
- **入口**：下载页左栏"社区资源"区 → 模组（ItemMod，Tag=2，PageDownloadLeft.xaml L27）
- **涉及文件**：`Pages\PageDownload\Comp\PageDownloadMod.xaml(.cs)`、`Pages\PageDownload\Comp\PageDownloadCompDetail.xaml(.cs)`、`Modules\Minecraft\ModComp.cs`（CompProject/CompFile/CompDatabase/CompClipboard/CompFavorites/快速下载 各 region）、`Modules\Minecraft\ModCompDependency.cs`、`PCL.Core\Minecraft\ResourceProject\Modrinth\*`、`Curseforge\*`、`ResourceProject\ModDependencyResolver.cs`
- **平台依赖**：无（Modrinth / CurseForge API 网络请求）

| 子功能 | 说明与证据 |
| --- | --- |
| 双源搜索 | Modrinth + CurseForge 双源聚合（`ModComp` 搜索 region L2556 网络请求重试循环），下载源选择 `Config.Download.Comp.CompSourceSolution`（ToolDownloadMod），单源不可用提示（`Download.Comp.List.Source.ModrinthUnavailable/CurseForgeUnavailable`、`Hint.ModrinthUnreachable`） |
| 排序 | 相关性/下载量/关注/最新发布/最近更新（`Download.Comp.Sort.*`） |
| 分类标签过滤 | 数十种分类（`Download.Comp.Category.*`：Mod.Mod/ModRelated/Library/Optimization/Technology/Magic 等），含加载器过滤（`Filter.Loader.*`：Any/AnyShader/VanillaAvailable）与游戏版本过滤（`Filter.Version.*`） |
| 文件名/简介翻译 | mcmod 数据库匹配（`CompDatabase` region L692）、文件名格式选项 `Config.Download.Comp.NameFormatV2`（ToolDownloadTranslateV2，4 种样式 `Setup.GameManage.Community.FilenameFormat.Style0-4`）、简介翻译（`Detail.DescriptionTranslating/TranslationResult`） |
| 剪贴板识别 | `CompClipboard` region（L626）：识别 Modrinth/CurseForge/mcmod 链接自动打开详情（`Detail.Clipboard.Detected.*`、设置项 ReadClipboard） |
| 资源详情页 | 简介（可翻译）/图库/文件列表（按版本分组 Snapshot/Old/Other，Release/Alpha/Beta 标注，`Detail.FileList.*`）、复制链接/名称、mcmod 跳转（`Detail.McMod`） |
| 依赖解析与自动安装 | `ModCompDependency` + `Download.Comp.Dependency.*`（前置缺失警告/批量安装/无依赖继续/解析失败继续），自动安装开关 `Config.Download.Comp.AutoInstallDependencies` |
| 安装到实例 | 自动匹配适用实例（`Detail.FindingApplicableInstance`）/手选实例（`Detail.SelectSaveLocation`）/输入新实例名（`Detail.InputInstanceName`）；无适用实例提示（`Detail.NoApplicableInstance`） |
| 快速下载 | 卡片按钮直接下载（`快速下载` region L3492）+ 行为选项 `Config.Download.Comp.QuickDownloadBehavior`（当前实例/询问实例/询问路径，`Comp.QuickDownload.ChooseMethod.*`） |
| 收藏 | 详情页收藏/取消（`Detail.Favorite/Unfavorite/Favorites.*`） |

### P0-9 社区资源下载（整合包/材质包/光影/数据包/世界 + 收藏夹）
- **入口**：下载页左栏"社区资源"区 → 整合包(ItemPack)/数据包(ItemDataPack)/材质包(ItemResourcePack)/光影(ItemShader)/世界(ItemWorld)/收藏夹(ItemFavorites)（PageDownloadLeft.xaml L44-131）
- **涉及文件**：`Pages\PageDownload\Comp\PageDownloadPack|DataPack|ResourcePack|Shader|World.xaml(.cs)`、`PageDownloadCompFavorites.xaml(.cs)`、`PageDownloadCompDetail.xaml(.cs)`、`Modules\Minecraft\ModComp.cs`、`FormMain.xaml.cs`（世界 zip 导入 L1070-1122）
- **平台依赖**：无

| 子功能 | 说明与证据 |
| --- | --- |
| 整合包下载 | PageDownloadPack：搜索/筛选后直接安装（`Detail.ModpackDownload/PrepareModpackInstall/OriginalModpack`）或下载文件（`Detail.DownloadModpackFile`） |
| 材质包下载 | PageDownloadResourcePack：按分辨率分类（`Category.ResourcePack.Resolution8xOrLower~512xOrHigher`），安装到实例 resourcepacks |
| 光影包下载 | PageDownloadShader：Iris/OptiFine 分类（`Category.Iris/Optifine`）、性能分级（PerformancePotato~High）、路径追踪/PBR 等标签 |
| 数据包下载 | PageDownloadDataPack：安装到实例或存档 datapacks（`List.Loading.DataPack/Search.DataPack`） |
| 世界下载 | PageDownloadWorld：世界分类（`Category.World.*`：生存/创造/冒险/解谜/跑酷/模组世界），`Detail.InstallWorld/SelectWorldInstallLocation/WorldDownload/WorldFile.Filter`，安装时自动解压并校验 level.dat（FormMain 世界导入逻辑同源） |
| 资源详情页复用 | 六类资源共用 PageDownloadCompDetail（文件列表/版本过滤/翻译/收藏），类型键 `Download.Comp.Type.Mod/Modpack/DataPack/ResourcePack/Shader/World/Plugin/Schematic`（含插件与投影预留类型） |
| 收藏夹管理 | PageDownloadCompFavorites：新建/重命名/删除/导入（分享码/文件夹）/分享/备注（`Comp.Favorites.Menu.*`、`Dialog.*`），默认收藏夹保护（`Hint.LastCollection`） |
| 收藏夹批量下载 | 按所选收藏批量匹配加载器/版本下载（`Favorites.LoaderName.BatchDownload(.Suitable)`、`Dialog.BulkDownload.*`、`Hint.SelectedCount`） |
| 文件下载与保存 | `Detail.DownloadFile/DownloadResource` + 保存位置选择/文件筛选器（`Detail.ResourceFile.Filter`） |
| 下载状态反馈 | `Detail.Task.Cancelled/Failed`、`List.Loading.*`、`List.NoResults/NoMatchingResults` |

### P0-10 游戏启动（登录/档案/启动流程/启动参数/进程守护）
- **入口**：主窗口"启动"标签（PageLaunchLeft：版本标签/BtnInstance 选择实例/BtnMore 实例设置/PanLogin 登录面板/BtnLaunch 启动/BtnCancel 取消）+ 右下角悬浮按钮（结束游戏 `Main.Extra.ShutdownMinecraft`、实时日志 `Main.Extra.GameLog`）
- **涉及文件**：`Pages\PageLaunch\PageLaunchLeft.xaml(.cs)`、`PageLaunchRight.xaml(.cs)`、`PageLoginOffline|Ms|Auth|Profile|ProfileSkin.xaml(.cs)`、`Pages\PageLaunch\MyMsgLogin.xaml(.cs)`、`Modules\Minecraft\ModLaunch.cs`、`ModProfile.cs`、`ModWatcher.cs`、`Modules\Minecraft\ModJava.cs`、`Modules\ModMain.cs`（ArgumentReplace/结束游戏）
- **平台依赖**：进程守护用 user32（EnumWindows/GetClassNameW/GetWindowTextW/SetWindowTextW/ShowWindow，ModWatcher.cs L681-697）；GPU 高性能偏好写注册表（ModMain.SetGPUPreference → `HKCU\Software\Microsoft\DirectX\UserGpuPreferences`）；导出启动脚本为 .bat（Windows 专属）；启动器可见性/优先级依赖进程 API

| 子功能 | 说明与证据 |
| --- | --- |
| 启动页布局 | 版本标签 LabVersion、选择实例/实例设置按钮、登录面板容器 PanLogin（按登录类型动态加载 PageLogin*）、启动按钮 BtnLaunch、取消按钮 BtnCancel（PageLaunchLeft.xaml L27-155） |
| 启动进度面板 | 当前步骤/下载库/登录方式/进度百分比/下载速度/冷知识提示（Launch.Status.* + PageLaunchLeft.xaml L69-155，`UiShowLaunchingHint` 控制提示开关） |
| 离线登录 | PageLoginOffline：玩家名（校验规则 `Launch.Account.Profile.Validation.*`）+ UUID 三模式（标准/离线 Legacy/自定义，`Launch.Account.Offline.Uuid.*`） |
| 微软正版登录 | PageLoginMs + MyMsgLogin：设备码或网页轮询（`Launch.Account.LoginDialog.*`：复制代码/重开网页/轮询失败/成功），`Config.Launch.LoginMsAuthType` 切换设备码/网页管理器（`Setup.Launch.Options.MsAuth.DeviceCode/WebManager`） |
| Authlib-Injector 登录 | PageLoginAuth：服务器地址（自动补全 /authserver）、邮箱、密码、注册/忘记密码跳转、预设 LittleSkin（`Launch.Account.Auth.*`、`Preset.LittleSkin`）、Yggdrasil 客户端在 PCL.Core（`Minecraft\IdentityModel\Yggdrasil\*`、`YggdrasilConnect\*`、OAuth/PKCE） |
| 档案管理 | PageLoginProfile：多档案列表/新建/删除/编辑玩家 ID（冷却与重名校验 `Launch.Account.Profile.EditPlayerId.*`）/编辑离线 UUID/修改认证服务器显示名/导入导出/复制 UUID（ModProfile.cs L335-588：CreateProfile/EditProfileId/EditOfflineUuid/EditAuthServerName/RemoveProfile/GetOfflineUuid） |
| 登录要求锁定 | 按实例锁定登录方式（`Config.InstanceAuth.LoginRequirementSolution`：不限/仅正版/仅指定第三方/正版或第三方 + 服务器地址匹配，ModLaunch.McLaunchPrecheck L65-86）；LabyMod 实例强制正版（L65） |
| 启动预检测 | 路径含 `!`/`;` 拒绝、非 ASCII 路径警告（可不再提示）、实例状态检查、档案有效性校验、正版购买提示（无正版档案时，`Launch.Account.Microsoft.Purchase`）、演示模式（--demo，ModLaunch L154-168）、捐助提醒（BETA 版按启动次数弹窗，L91-133） |
| Java 选择 | 启动前自动选择兼容 Java（ModLaunch `#Java 处理` L1815 + ModJava.JavaSelect 支持版本上下限、取消异常、64 位检测 IsGameSet64BitJava），版本级覆盖（详见 P1-1） |
| 启动参数构建 | ModLaunch `#启动参数` region（L2107）：JVM 参数（默认 G1GC 等，`Config.Launch.JvmArgs`）/游戏参数/类路径头（`Instance.Advanced.Classpath`）/资产验证级别/渲染器选择（游戏默认/DirectX12/Vulkan/软件，`Setup.Launch.Advanced.Renderer.*`）/优先 IP 栈（IPv4/IPv6）/预启动指令(+等待)/禁用 Java Launch Wrapper / LegacyFix / LWJGL Unsafe Agent/游戏窗口标题与左下角版本信息（占位符替换 ArgumentReplace：`{version}/{name}/{login}/{user}/{uuid}/{java}/{minecraft}/{path}/{date}/{time}/{hint}/{cave}/{setup:x}/{variable:x}`，ModMain.cs L896-996） |
| 窗口与进程选项 | 窗口尺寸（同启动器/自定义宽高/最大化/全屏，`Setup.Launch.Options.WindowSize.*`）、进程优先级（6 档，`Options.Priority.*`）、启动器可见性（保持/最小化/隐藏+退出恢复/立即关闭含警告，`Options.Visibility.*`） |
| Natives 解压与进程启动 | ModLaunch `#解压 Natives`（L3009）与 `#启动与前后处理`（L3112）：解压本地库、组装命令行、启动进程、后处理（脚本导出/日志收集） |
| 进程守护 | ModWatcher：窗口检测与标题改写（user32）、启动阶段检测（`Watcher.Progress.*`：LWJGL 确认/OpenAL 加载/材质加载/FML 窗口/日志出现/用户设置）、窗口自动最大化（WindowMaximized）、安全软件拦截提示（SecurityBlocked）、游戏窗口标题改写 |
| 崩溃检测与强杀 | `ModWatcher.Crashed`（L699-744：崩溃检测 + 崩溃分析入口，见 P2）+ `Kill`（进程强杀 + taskkill 兜底，L757+）、右下角"结束游戏"按钮（`Main.ShutdownMinecraft.Success`） |
| 导出启动脚本 | `Launch.Status.Title.ExportingScript` + `Minecraft.Launch.ExportScript.*`（生成 LatestLaunch.bat，ModWatcher L727 引用）——**Windows 专属（bat）** |
| GPU 高性能偏好 | `Config.Launch.SetGpuPreference`（默认开）→ 注册表 UserGpuPreferences（ModMain.SetGPUPreference L857-891）——**Windows 专属** |
| 启动后事件 | 自定义事件系统（`Modules\Event\CustomEvent*.cs`：启动游戏/打开网址/下载/写变量/写设置/刷新，含安全确认 `Event.Safety.*`）、社区提示卡与启动日志卡（PageLaunchRight PanHint/PanLog） |

## 优先级 P1（核心体验相关、用户未点名但常用）

| 功能名 | 入口（页面/菜单） | 涉及文件 | 依赖的平台 API / 备注 |
| --- | --- | --- | --- |
| Java 管理 | 设置→Java；实例设置→Java 选项 | `Pages\PageSetup\PageSetupJava.xaml(.cs)`、`Modules\Minecraft\ModJava.cs`、`PCL.Core\Minecraft\Java\JavaManager.cs`、`JavaService.cs`、`JavaEntry.cs`、`Java\Scanner\RegistryJavaScanner.cs`、`MicrosoftStoreJavaScanner.cs`、`PathEnvironmentScanner.cs`、`WhereCommandScanner.cs`、`DefaultPathsScanner.cs`、`Java\UserPreference\*.cs` | **Windows 专属**：注册表 HKLM 扫描、Microsoft Store 检测、`where` 命令、PE 头解析（PeHeaderParser）；Linux 需替换扫描器（PATH/常用目录/sdkman 等）；支持手动添加/启用禁用/详情查看/自动选择（版本上下限匹配）/版本级选择与相对路径 Java/64 位检测 |
| Java 运行时下载确认 | Java 缺失时弹窗引导 | `ModJava.JavaDownloadConfirm`/`GetJavaDownloadLoader`（L343/L362）、`Modules\Network\Loaders\LoaderDownload.cs` | 无；下载对应版本运行时并安装 |
| 游戏实时日志页 | 右下角"游戏日志"悬浮按钮（PageType.GameLog） | `Pages\PageLogLeft.xaml(.cs)`、`PageLogRight.xaml(.cs)`、`Modules\Minecraft\McLogFilter.cs` | 无；自动滚动/清空/导出日志/导出堆栈/结束进程/最大行数滑条（PageLogRight.xaml L39-78）；AccessToken 与用户名脱敏（McLogFilter）；多实例日志切换（LogPage.Left.InstancesTitle） |
| 下载任务管理页 | 右下角"任务管理"悬浮按钮（PageType.TaskManager） | `Pages\PageSpeedLeft.xaml(.cs)`、`PageSpeedRight.xaml(.cs)`、`Modules\Network\Facade\ModNet.cs` | 无；任务列表/进度/实时速度图/线程数（仅调试模式显示，子代理验证）/取消；下载中锁定文件夹切换/添加/删除（PageSelectLeft L346/L486/L727） |
| 存档管理 | 实例设置→存档（PageType.VersionSaves） | `Pages\PageInstance\PageInstanceSaves.xaml(.cs)`、`PageInstanceSaves\PageInstanceSavesLeft|SavesInfo.xaml(.cs)`、`PCL.Core\Minecraft\Saves\SaveManager.cs`、`SaveInfo.cs`、`NbtFileHandler.cs` | 无；列表搜索/排序（创建时间/文件名/修改时间）/复制备份/删除（回收站）/打开位置/快捷运行 QuickPlay（直接带参进存档）/拖拽导入 zip 存档（SaveImportHelper.GetSaveRootDirectory 校验） |
| 存档详情与编辑 | 存档详情子页 | `PCL.Core\Minecraft\Saves\Parsing\*`（6 个分版本解析器）、`Saves\Editing\*`（Pre261SaveEditor/Version261PlusSaveEditor）、`Saves\DataVersionBoundaries.cs` | 无；查看/修改游戏模式、难度、作弊开关、锁难度、种子、游玩时长、出生点、版本提示；Chunkbase 种子查询跳转（`Instance.Saves.Info.Chunkbase.*`） |
| 存档数据包管理 | 存档详情→数据包子页 | `Pages\PageInstance\PageInstanceSaves\PageInstanceSavesDatapack.xaml(.cs)`、`Instance.Saves.Datapack.*` 键 | 无；列表/启用禁用/删除（回收站）/从文件安装/更新（下载替换，不安全文件跳过提示） |
| 本地资源管理（模组/材质/光影/数据包） | 实例设置→对应子页 | `Pages\PageInstance\PageInstanceCompResource.xaml(.cs)`（一个类复用 5 个子页）、`MyLocalModItem.xaml(.cs)`、`Modules\Minecraft\ModLocalComp.cs`、`ModJarInJar.cs` | 无；启用禁用（改后缀）/删除（回收站+永久）/从文件安装（对话框）/过滤（全部/启用/禁用/重复/错误/可更新）/更新检查与一键更新（对比变更/下载替换）/详情（作者/版本/ModID/前置/链接/内嵌 Jar-in-Jar）/导出资源清单（Txt/Csv）/多选+全选+分享/检查模组（版本要求/重复/前置缺失，结果仅供参考提示） |
| 投影文件管理 | 实例设置→投影（VersionSchematic） | `PageInstanceCompResource`（Schematic 模式）、`Instance.Resource.Schematic.*`、`Instance.Resource.Item.Schematic.*` | 无；支持 Sponge/NBT/MCEdit 三格式（.litematic/.nbt/.schematic/.schem）；详情显示尺寸/包围盒/总方块数/区域数/游戏版本/创建修改时间；"下载模组"入口（litematica） |
| 已禁用模组专页 | 模组异常时引导 | `Pages\PageInstance\PageInstanceModDisabled.xaml(.cs)`、`Instance.Resource.Mod.Disabled.*` | 无；列出禁用模组并引导下载或切换实例 |
| 截图管理 | 实例设置→截图 | `Pages\PageInstance\PageInstanceScreenshot.xaml(.cs)`、`Instance.Screenshot.*` | 无；查看/复制到剪贴板/删除/打开文件夹/快捷操作 |
| 服务器管理 | 实例设置→服务器 | `Pages\PageInstance\PageInstanceServer.xaml(.cs)`、`ServerCard.xaml(.cs)`、`Controls\MinecraftServer.xaml(.cs)`、`MinecraftServerQuery.xaml(.cs)`、`PCL.Core\Link\McPing\*`、`PCL.Core\UI\Controls\MotdRenderer.xaml.cs`、`PCL.Core\Minecraft\ServerAddressResolver.cs` | 无（MOTD 渲染为 WPF 文字绘制；SRV 解析用自实现 DNS：`IO\Net\Dns\DnsQuery.cs`、`WeightedDnsRacerClient.cs`，可配 DoH）；servers.dat 读写/添加删除重命名/刷新在线状态（MOTD/人数/延迟/图标）/复制地址/自动进入服务器（`Config.InstanceAuth`+`VersionServerEnter`）/服务器卡片快捷启动 |

| 实例导出（整合包） | 实例设置→导出 | `Pages\PageInstance\PageInstanceExport.xaml(.cs)`、`Modules\Minecraft\ModModpack.cs`、`Instance.Export.*` 键 | 无；导出 CurseForge 兼容 zip 或 Modrinth mrpack（`Advanced.Modrinth`）；细粒度内容选项（模组/存档/截图/配置/游戏设置/重要数据/地图/结构/服务器/EMI/JEI/Patchouli/Replay 等 20+，`Instance.Export.Option.*`）；导出配置文件（排除/额外/附带文件规则，`Instance.Export.Config.*`）；导出前网络可达性校验（`NetCheck.*`）；附带你启动器选项（`Option.Launcher(.Custom)`） |
| 启动器更新 | 设置→更新；右下角"重启安装"按钮 | `Pages\PageSetup\PageSetupUpdate.xaml(.cs)`、`Modules\Updates\UpdateManager.cs`、`UpdateService.cs`（PCL.Core\App\Essentials）、`UpdatesWrapperModel.cs`、`UpdatesMirrorChyanModel.cs`、`EnumUpdateChannel.cs`、`PCL.Core\Utils\Diff\BsDiff.cs` | 自替换可执行文件（`Update.Task.RestartInstall`、命令行 update 参数自替换）为 Windows 部署方式，Linux 需改造；检查更新/通道（正式/Beta/Dev——Dev 隐藏需输入确认句）/自动更新行为 4 种（仅通知/下载并通知/下载并安装/禁用）/更新日志查看/Mirror 酱 CDK（加密存储）/BsDiff 增量 + SHA256 校验（`Update.Error.Sha256Mismatch`）/.NET 运行时缺失检测 |
| 公告系统 | 启动时弹窗 | `Modules\Updates\AnnouncementService.cs`、`VersionAnnouncementDataModel.cs`、`Update.CommunityNotice.*` | 无；公告分级（仅重要/全部/禁用，`Setup.Misc.System.Announcement.*`，禁用需确认） |
| 反馈中心 | 设置→问题反馈 | `Pages\PageSetup\PageSetupFeedback.xaml(.cs)`、`Setup.Feedback.*` 键 | 无；反馈列表（状态：待处理/处理中/已完成/已拒绝/重复/忽略等 8 种）/查看详情/提交新反馈（跳转浏览器）/提交前版本检查（`Unavailable.NotLatest`） |
| 多语言 | 设置→启动器语言 | `Pages\PageSetup\PageSetupLauncherLanguage.xaml(.cs)`、`PCL.Core\App\Localization\LocalizationService.cs`、`LocalizationLanguage.cs`、`LocalizationFormatConverter.cs`、`LocalizationFontService.cs`、语言文件 `Languages\zh-CN.xaml` 等 7 种 | 无；UI 语言（auto 跟随系统/手动）、区域格式（auto/跟随语言/手动）、翻译贡献横幅（跳转网站/提交 PR）、重置确认 |
| 主题与外观 | 设置→个性化→基础 | `Pages\PageSetup\PageSetupUI.xaml(.cs)`（CardLauncher）、`Modules\UI\Theme\ThemeManager.cs`、`PCL.Core\UI\Theme\ThemeService.cs`、`SystemThemeHelper.cs`、`ToneProfile*.cs`、`LabColor.cs` | 系统深浅色跟随读注册表（`SystemThemeHelper.cs` L19，**Windows 专属**，Linux 需读桌面环境设置）；深浅色模式（跟随系统/亮/暗）×4 主题色板（猫蓝/天蓝/崩溃蓝/HMCL 蓝暗彩蛋）/自定义主题 HSL+色相渐变/窗口透明度/锁定窗口大小/彩色底部填充/动画帧率上限 |
| 自定义主页 | 设置→个性化→主页；启动页右侧 | `Pages\PageSetup\PageSetupUI.xaml(.cs)`（CardCustom）、`Pages\PageLaunch\PageLaunchRight.xaml(.cs)`（PanCustom）、`ModSetup.UiCustomType`（L386-441）、`Main.FileDrag.Homepage*` | 无；主页类型（无/预设/本地 XAML/联网 URL）+ 13 种预设（每日整合包/回声洞/杂志/MC 新闻/MC 官方动态/皮肤/更新摘要/今日要闻/知识/番剧/公告/手册/市场/简洁主页，`Setup.Ui.Homepage.Preset.*`）/刷新/不可信主页警告（`States.Hint.UntrustedHomepage`）/联网主页教程与下载 |
| 新闻详情页 | 主页新闻卡片→查看详情 | `Pages\PageHomepage\PageHomepageNewsView.xaml(.cs)`、`PCL.Core\ViewModel\Homepage\NewsViewModel.cs`、`Model\Homepage\News\*` | 无；新闻列表与正文渲染（含图片加载 ImageLoaderHelper） |
| 设置导入导出 | 设置→杂项→系统 | `Pages\PageSetup\PageSetupLauncherMisc.xaml(.cs)`（`Setup.Misc.System.ExportSettings/ImportSettings.*`） | 无；导出设置到 ini 文件/导入并重启生效；敏感项（代理凭据/Mirror 酱 CDK）加密存储（`ConfigSource.SharedEncrypt`，`PCL.Core\App\Configuration\Storage\EncryptedFileConfigStorage.cs`） |
| 网络设置 | 设置→杂项→网络 | `PageSetupLauncherMisc.xaml(.cs)`（Network 卡）、`ModSetup.SystemHttpProxy*`（L607-647）、`PCL.Core\IO\Net\Http\HttpProxyManager.cs` | 系统代理读取注册表 ProxyEnable/ProxyServer（HttpProxyManager.cs L114-115，**Windows 专属**，Linux 需读环境变量）；不使用/系统代理/自定义代理（地址+用户名+密码）/DoH 开关（`Config.Network.EnableDoH`）/代理无效与不受信警告 |
| 文件拖拽导入 | 主窗口任意位置拖入文件 | `FormMain.xaml.cs`（FrmMain_Drop L913 / FileDrag L990-1181）、`PageInstanceCompResource.InstallMods/InstallCompFiles` | OLE 拖放（WPF 内建 + `PCL.Core\Utils\OS\DragHelper.cs` shell32/ole32 增强，**Windows 专属部分可裁剪**）；支持：XAML→安装主页、`authlib-injector:yggdrasil-server:` URI→新建第三方档案、jar/litemod/disabled/old 批量→安装模组、litematic/nbt/schematic/schem→安装投影、在对应实例子页拖 zip→安装世界/材质/光影、zip/rar/mrpack→安装整合包 |
| 首次启动与合规流程 | 首次运行弹窗 | `FormMain.xaml.cs`（EULA L252-258）、`Main.Eula.*`、`Main.Telemetry.*`、`Application.EnvironmentWarning.*`、`Application.xaml.cs`（启动 Logo SplashScreen L73） | 环境警告含 Windows 版本/32 位/System32 运行/Temp 文件夹检测（**Windows 专属**，`PCL.Core\Utils\OS\SystemInfo.cs`）；EULA 与遥测同意记忆（`States.System.LauncherEula`、注册表 `HKCU\Software\PCL`，TelemetryService.cs L192）；CI/调试特殊版本提示（`Main.SpecialVersion.*`） |
| 弹窗与提示基础设施 | 全局 | `Modules\ModMain.cs`（MyMsgBox 5 类：文本/选择/输入/登录/Markdown，L228-663）、`Controls\MyMsg\*`、`Modules\UI\HintService.cs`、`PCL.Core\UI\MsgBoxWrapper.cs`、`SystemDialogs.cs`（文件/文件夹系统对话框）、`ToastNotification.cs` | 系统对话框为 Win32 通用对话框（Linux 需替换）；WinRT Toast 通知仅基础设施（见 P3）；此组是所有页面共用的强耦合基础设施 |

## 优先级 P2（次要）

| 功能名 | 入口（页面/菜单） | 涉及文件 | 依赖的平台 API / 备注 |
| --- | --- | --- | --- |
| 功能隐藏 | 设置→个性化→功能隐藏 | `Pages\PageSetup\PageSetupUI.xaml(.cs)`（CardSwitch）、`Config.Preference.Hide.*`（Config.cs L425-465）、`PageSetupUI.HiddenRefresh` | 无；隐藏主页面（下载/设置/工具）、12 个设置子页、工具子页、9 类实例子页、特定功能（版本选择/模组更新检查/功能隐藏本身/实例管理入口/联机入口等）；临时禁用提示（`UiHiddenFeature.TemporaryHint`、`Main.HiddenFeature.*`） |
| 标题栏自定义 | 设置→个性化→标题栏 | `PageSetupUI.xaml(.cs)`（CardLogo）、`ModSetup.UiLogoType/UiLogoText/UiLogoLeft`（L469-588）、`FormMain.xaml`（ShapeTitleLogo/LabTitleLogo/ImageTitleLogo/CELogo） | 无；Logo 类型（无/默认/文本/图片/HMCL 彩蛋）、自定义文本、自定义图片（PCL\Logo.png）、Logo 居左 |
| 字体设置 | 设置→个性化→字体 | `PageSetupUI.xaml(.cs)`（CardFont）、`Controls\FontSelector.xaml(.cs)`、`ModSetup.UiFont`（L369）、`PCL.Core\App\Localization\LocalizationFontService.cs`、`Config.Preference.Font/MotdFont` | 系统字体枚举（Linux 可用 fontconfig）；全局字体 + MOTD 字体两项 |
| 高级材质（界面模糊） | 设置→个性化→基础→高级材质 | `PageSetupUI.xaml(.cs)`（PanBlurValue）、`ModSetup.UiBlur*`（L444-466）、`PCL.Core\UI\Effects\OptimizedBlurEffect.cs`、`AdaptiveBlurEffect.cs`、`EnhancedBlurEffect.cs`、`SamplingBlurProcessor.cs` | WPF ShaderEffect（自实现采样模糊）；启用/半径/采样率/方法（盒式/高斯） |
| 日志设置与历史 | 设置→日志 | `Pages\PageSetup\PageSetupLog.xaml(.cs)`、`Setup.Log.*` 键 | 无；实时日志最大行数/当前日志导出/全部日志导出/按筛选导出/清理历史日志（确认）/打开日志目录/日志临时存储说明 |
| 杂项系统设置 | 设置→杂项→系统 | `PageSetupLauncherMisc.xaml(.cs)`、`Config.System.*`（DisableHardwareAcceleration/Telemetry/MaxGameLog/AnimationFpsLimit） | 禁用硬件加速（重启生效）/动画帧率上限/遥测开关/公告级别/最大实时日志行/"停止使用 PCL CE"引导（导出数据+cmd 延迟自删，**Windows 专属**，子代理验证） |
| 调试选项 | 设置→杂项→调试（Config.Debug） | `PageSetupLauncherMisc.xaml(.cs)`、`ModSetup.SystemDebugMode/SystemDebugAnim`（L595-605） | 无；调试模式（`Main.DebugMode.Hint`）/动画速度/随机延迟（Debug.AddRandomDelay）/跳过复制（DontCopy）/允许受限功能（AllowRestrictedFeature） |
| 环境检测警告 | 启动时弹窗 | `Application.EnvironmentWarning.*` 键、`PCL.Core\Utils\OS\SystemInfo.cs`、`HardwareInfo.cs` | **Windows 专属**：系统位数/System32 目录运行/Temp 路径/Windows 版本过旧/社交软件文件夹（QQ/微信同步盘）检测 |
| 下载与社区资源设置 | 设置→游戏管理 | `Pages\PageSetup\PageSetupGameManage.xaml(.cs)`、`Config.Download.*` | 无；下载线程数（默认 63，超限警告）/速度限制档位（ModSetup.ToolDownloadSpeed L201）/下载源（文件/版本列表）/自动选择实例/修复 authlib（ToolFixAuthLib）/下载目标文件夹/社区资源源/忽略 Quilt/自动安装前置/文件名格式/本地模组名样式/快速下载行为/剪贴板读取/正式版与快照更新通知/游戏语言/启动器语言跟随 |
| 实例服务器认证设置 | 实例设置→服务器选项 | `PageInstanceSetup`（Server 卡）、`Config.InstanceAuth.*`（Config.cs L634-641）、`Instance.Setup.Server.*` 键 | 无；认证服务器地址（自动格式化/HTTP 警告/SSL 警告）/认证服务器名/注册地址/自动进入服务器/锁定登录方式（三种）/LittleSkin 覆盖确认 |
| 服务器查询工具 | 实例服务器页/工具箱 | `Controls\MinecraftServerQuery.xaml(.cs)`、`Tools.ServerQuery.*` 键、`PCL.Core\Link\McPing\LegacyMcPingService.cs` | 无；GameMaster Query 协议查询（UDP），超时/无效响应等错误处理 |
| 快捷方式创建 | 工具→实用工具 | `Pages\PageTools\PageToolsTest.xaml(.cs)`（Shortcut 卡）、`Tools.Test.Shortcut.*` | **Windows 专属**：.lnk 快捷方式（WScript.Shell COM，子代理验证）；桌面/开始菜单/自选位置 |
| 自定义文件下载 | 工具→实用工具 | `PageToolsTest`（CustomDownload 卡，L30-96） | 无；输入 URL/UserAgent/文件名/保存位置直接下载并打开 |
| 缓存清理工具 | 工具→实用工具 | `PageToolsTest`（BtnClear，L12）、`Tools.Test.Clean.*` | 无；清理启动器缓存（需先关闭游戏/等待下载完成），含下载缓存与临时文件 |
| 启动次数查看 | 工具→实用工具 | `PageToolsTest`（LaunchCount，L24）、`Tools.Test.LaunchCount.*`、`States.System.LaunchCount` | 无；显示累计启动次数弹窗 |
| 皮肤获取/头像生成/成就图生成 | 工具→实用工具 | `PageToolsTest`（Skin/Avatar/Achievement 卡，L98-232）、`Tools.Test.Skin/Avatar/Achievement.*` | 无；按玩家名抓取皮肤保存/按尺寸生成头像（含头发层）/自定义物品 ID 与两行文本生成成就图（预览+保存） |
| 关于页 | 设置→关于 | `Pages\PageSetup\PageSetupAbout.xaml(.cs)`、`Setup.About.*` | 无；版本信息/社区与原版作者致谢/贡献者列表（在线拉取）/开源许可/法律条款/特别感谢/连点彩蛋（ bored 对话与 200 次成就，`EasterEgg.*`） |
| 崩溃分析与导出崩溃报告【**用户明确暂不移植**】 | 游戏崩溃时自动弹窗；实例管理 | `Modules\Minecraft\CrashAnalysis\`（34 文件：CrashAnalyzer/CrashDetector/CrashRuleCatalog/CrashRegex/CrashLogCollector/CrashReportExporter/CrashDialogPresenter 等）、`Modules\Minecraft\ModWatcher.cs`（Crashed L699）、`Crash.*` 文案 141 条 | 无平台硬依赖（读日志文件+正则+导出报告）；含 35+ 崩溃原因规则库、Jar-in-Jar 重复解析、环境信息报告导出、自动/手动两种入口（`Crash.Dialog.Title.Auto/Manual`）、关闭开关（`LaunchAdvanceDisableCrashAnalysis`） |

## 优先级 P3（边缘/娱乐/Windows 专属）

| 功能名 | 入口（页面/菜单） | 涉及文件 | 依赖的平台 API / 备注 |
| --- | --- | --- | --- |
| 联机大厅【**用户明确暂不移植**】 | 工具→联机；设置→联机设置 | `Pages\PageTools\PageToolsGameLink.xaml(.cs)`、`PageToolsLeft.xaml(.cs)`、`Pages\PageSetup\PageSetupGameLink.xaml(.cs)`、`Modules\ModLink.cs`、`Modules\ModWebServer.cs`、`PCL.Core\Link\`（EasyTier\Lobby\McPing\Natayark\Scaffolding 共 60 文件） | **Windows/外部件依赖**：下载并运行 EasyTier CLI（`Link.Mod.DownloadEasyTier`）、iphlpapi（ModLink.cs L125）、Natayark ID OAuth（本地 127.0.0.1 回调服务器 NaidCallbackServer，ModWebServer.cs L112-307）；创建/加入大厅、房间码、成员列表、MOTD、NAT 类型测试（TCP/UDP）、IPv6、中继服务器选择、协议偏好（TCP/UDP）、端口猜测打洞、大厅公告、EULA 同意与撤销 |
| 换皮肤/披风【**用户明确暂不移植**】 | 启动页皮肤预览；档案页→皮肤/披风 | `Pages\PageLaunch\MySkin.xaml(.cs)`、`PageLoginProfileSkin.xaml(.cs)`、`Modules\Minecraft\ModSkin.cs`、`Launch.Skin.*` 文案（60+ 条） | 无平台硬依赖（Mojang/Authlib API 网络请求）；皮肤上传（Steve/Alex 模型选择）、披风列表选择（30 种命名披风）、刷新皮肤、另存皮肤/头像、档案皮肤预览 3D 渲染 |
| 启动器背景图/视频背景【**用户明确暂不移植**】 | 设置→个性化→背景 | `Pages\PageSetup\PageSetupUI.xaml(.cs)`（CardBackground）、`Modules\ModVideoBack.cs`、`ModSetup.UiBackground*`（L240-366）、`FormMain.xaml`（ImgBack/VideoBack L112-117）、`Setup.Ui.Background.*` 键 | MediaElement 视频播放（WPF，Linux 需替换方案）；背景图文件夹管理（刷新/清理/打开目录）、8 种适配模式（智能/居中/适应/拉伸/平铺/四角）、模糊、不透明度、彩色遮罩、轮换、视频自动暂停（联动游戏状态 IsGaming） |
| 音乐播放器【**用户明确暂不移植**】 | 右下角音乐悬浮按钮；设置→个性化→音乐 | `Modules\ModMusic.cs`、`PageSetupUI.xaml(.cs)`（CardMusic）、`FormMain.xaml`（BtnExtraMusic L252-254）、`Music.*` 文案 | **Windows 专属**：NAudio `WaveOutEvent` 输出（ModMusic.cs L16）；扫描文件夹/播放暂停切歌/随机/音量/启动游戏自动暂停或播放/SMTC 设置项（**注意：SMTC 实现缺失**，仅 `Config.Preference.Music.EnableSMTC` 且 PageSetupUI.xaml.cs L604 `// DisableSMTCSupport()` 被注释，全仓库无 Windows.Media 代码） |
| 愚人节彩蛋 | 启动页（4 月 1 日自动启用） | `Modules\ModMain.cs`（isAprilEnabled/TimerFool L665-820）、`FormMain`（BtnExtraApril 白旗）、`ModSetup.UiLogoType` case 4（HMCL 标题）、`Minecraft.Fool.*` 文案、`McInstanceCardType.Fool` | 无；启动按钮躲避鼠标物理动画、投降白旗提示、HMCL 标题与帮助按钮彩蛋、愚人节版本自动分类 |
| 幸运测试 | 工具→实用工具 | `PageToolsTest`（Luck 按钮）、`Tools.Test.Luck.*` | 无；随机运势打趣 |
| 手动崩溃测试 | 工具→实用工具（隐藏按钮） | `PageToolsTest`（BtnCrash，XAML L17-20）、`Tools.Test.Crash.*`、`Crash.Reason.ManualDebugCrash` | 无；触发一次真实崩溃以验证崩溃分析链路（开发/验证用） |
| WinRT Toast 通知基础设施 | 系统通知 | `PCL.Core\Utils\WinRT\WinRTInterop.cs`（combase.dll）、`IToastNotification*`、`UI\ToastNotification.cs`、`Utils\OS\AumidHelper.cs`（注册表 AppUserModelId） | **Windows 专属**（WinRT/COM + 注册表 AUMID）；当前主要服务于通知能力预留 |
| 任务栏集成 | 主窗口任务栏图标 | `FormMain.xaml`（TaskbarItemInfo L17-19）、`ModLoader.LoaderTaskbarProgressRefresh`（ModMain.cs L111） | **Windows 专属**：任务栏缩略图裁剪与进度态；Linux 无对应（可静默降级） |
| 单实例互斥与 RPC | 进程间通信 | `PCL.Core\App\Essentials\SingleInstanceService.cs`、`RpcService.cs`、`RpcFunction.cs`、`RpcProperty.cs`、`IO\PipeComm.cs`、`KernelInterop.cs`（GetNamedPipeClientProcessId） | **Windows 专属**：命名管道通信（Linux 可换 Unix Domain Socket）；防止多开、跨进程调用（更新重启后参数接力） |
| 遥测上报 | 遥测开关（默认关） | `PCL.Core\App\Essentials\TelemetryService.cs`、`Config.System.Telemetry` | 无硬依赖；EULA 同意记录读注册表 `HKCU\Software\PCL`（L192，Windows 专属路径） |
| 窗口特效与 DPI | 主窗口渲染层 | `PCL.Core\Utils\OS\WindowInterop.cs`（dwmapi/user32/shcore）、`UI\Controls\BlurBorder.cs`、`FormMain.xaml`（WindowChrome 圆角）、`UI\Animation\Clock\WinMMClock.cs`（winmm 高精度计时） | **Windows 专属**：DWM 圆角/DPI 感知/winmm 时钟；WPF 本体的窗口非客户区定制在 Linux（Avalonia/gtk）需整体替代方案 |
| 短路径与兼容 API | 文件处理底层 | `Modules\Base\ModBase.cs`（GetShortPathNameA，kernel32，L1557）、`Application.xaml.cs`（SetDllDirectoryA，L186） | **Windows 专属**：8.3 短路径与 DLL 目录重定向；Linux 无需 |

## 发现备注（影响移植顺序的发现）

1. **页面注册中心模式是导航骨架的根**：四大标签页的子页均由左栏页的 `PageGet(pageID)` 惰性创建（`PageDownloadLeft`/`PageSetupLeft`/`PageInstanceLeft`/`PageToolsLeft`/`PageInstanceSavesLeft`），页面种类集中在 `FormMain.PageType/PageSubType` 两个枚举（FormMain.xaml.cs L1363-1470），页面实例缓存在 `ModMain.frm*` 静态字段（ModMain.cs L26-91）。移植时应先复刻"枚举 + PageGet + 静态引用"骨架，再逐页填充。
2. **全局强耦合基础设施**（几乎所有页面依赖，需最先可用）：`ModLoader` 任务系统（下载/安装/启动全部异步流程的载体）、`ModNet` 下载管理（`HasDownloadingTask` 锁定文件夹切换/添加/删除等多处 UI）、`MyMsgBox` 弹窗体系（5 种类型 + 主窗体队列 tick）、`HintService` 顶部提示、`ConfigService` 配置系统（含 SharedEncrypt 加密项：代理地址/密码/Mirror 酱 CDK）、`ThemeManager` 主题刷新、`IRefreshable` 页面刷新接口。
3. **一个类复用五个资源页**：`PageInstanceCompResource` 同时充当模组/材质包/光影/投影/数据包五个实例子页（ModMain.cs L76-82 同类多实例），移植时抽成参数化组件可省大量工作。
4. **`ModComp` 是社区资源域核心**：搜索/详情/收藏夹/翻译数据库/剪贴板识别/快速下载全部在此，被下载页与实例资源页共用；依赖解析在 `ModCompDependency`。注意 ModComp.cs L2908 有 TODO：`移除龙猫写的直接下载，换用提醒用户手动下载相关模组`（指缺失前置的直接下载逻辑，未来行为可能变化）。
5. **档案（ModProfile）三处共用**：启动预检测的登录要求锁定、实例服务器认证、联机大厅预检（ModLink.LobbyPrecheck）都读写同一档案列表；换肤入口在档案皮肤页与工具箱两处。
6. **崩溃分析没有删除迹象**：全仓库无"移除/删除崩溃分析"的 TODO、注释或 issue 引用；git 近期提交仍在增强它（`978d2db4 feat(mod): 内嵌模组(Jar-in-Jar)自动解析与崩溃导出 #3378`、`12bb0ea2 #3409`、`99a9c0ed #3405`、`13f5ed46 #3406`、`4304061c #3404`、`435fee1c refactor(crash) #3163`）。仅存在用户级关闭开关（`LaunchAdvanceDisableCrashAnalysis`）与对应提示（`Watcher.Crash.DetectedDisabled`）。"疑似打算删掉"的说法在本仓库不成立；不移植即直接裁剪 ModWatcher.Crashed 的分析分支即可，崩溃检测与强杀本身仍属 P0。
7. **SMTC 是空壳**：音乐播放器的"系统媒体传输控制"只有设置项（`UiMusicSMTC` 默认 true）和被注释的 `// DisableSMTCSupport()`（PageSetupUI.xaml.cs L604），全仓库无实现代码；音乐播放器移植评估时可直接忽略该项。
8. **Windows 专属依赖热点清单**（移植阻断点，按风险排序）：① `ModWatcher` 窗口检测/标题改写（user32，核心体验）② 系统文件/文件夹对话框（SystemDialogs，到处使用）③ Java 扫描器（注册表/MS Store/where）④ 导出启动脚本与服务端批量脚本（.bat 格式）⑤ 系统代理与深浅色注册表读取 ⑥ 单实例命名管道 ⑦ 更新自替换 exe ⑧ NAudio 音频 ⑨ 快捷方式 COM ⑩ WinRT Toast/AUMID ⑪ GPU 偏好注册表 ⑫ dwmapi/winmm 窗口特效。①②③④⑤⑥ 需要替代实现，⑦-⑫ 可先降级或裁剪。
9. **配置层可整体平移**：全部设置项集中在 `PCL.Core\App\Config.cs`（含实例级 ArgConfig 覆盖与 Local/SharedEncrypt/GameInstance 三种存储源标记），`ModSetup` 只挂观察器；语言文件 3011 键与配置键（如 `UiLogoType`）一一对应，是移植验收的现成对照表。
10. **下载安装与启动共享同一任务管线**：`ModDownloadLib.McInstall` 产出的加载器任务与启动时的库/资产补全（`ModLaunch`）走同一套 `LoaderBase`/`LoaderCombo`，下载限速/线程（ModSetup.ToolDownloadThread/Speed）作用于全局 ModNet；移植时不要把"下载"与"安装"拆成两套体系。
11. **整包导入来源广**：ModModpack 支持七种来源（CurseForge/Modrinth/HMCL/MCBBS/带启动器压缩包/普通压缩包/MultiMC），其中"普通压缩包"与"带启动器压缩包"为启发式解析，Linux 移植时优先保证前四种标准格式。
12. **存档编辑有版本边界**：PCL.Core 的存档解析器按版本分 6 档（1.3-1.8.9 / 1.9-1.12.2 / 1.13-1.15.2 / 1.16-1.21.1 / 261+ / Pre-1.3），编辑器分两档（Pre261/261+）；这是纯 NBT 逻辑，可整体移植且无 Windows 依赖。

## 附录：主窗口页面枚举（移植导航对照）

| PageType | 值 | 页面 | 子页（PageSubType） |
| --- | --- | --- | --- |
| Launch | 0 | 启动（PageLaunchLeft/Right） | — |
| Download | 1 | 下载（PageDownloadLeft） | Install(1)/Mod(2)/Pack(3)/DataPack(4)/ResourcePack(5)/Shader(6)/World(7)/CompFavorites(8)/Client(9)/OptiFine(10)/Forge(11)/NeoForge(12)/Cleanroom(13)/Fabric(14)/LiteLoader(16)/LabyMod(17)/LegacyFabric(18) |
| Setup | 2 | 设置（PageSetupLeft） | Launch(0)/UI(1)/GameManage(2)/About(4)/Log(5)/Feedback(6)/GameLink(7)/Update(8)/Java(9)/LauncherMisc(10)/LauncherLanguage(11) |
| Tools | 3 | 工具（PageToolsLeft） | GameLink(1)/Test(3) |
| InstanceSelect | 5 | 实例选择（PageSelectLeft/Right，副页面） | — |
| TaskManager | 6 | 任务管理（PageSpeedLeft/Right，副页面） | — |
| InstanceSetup | 7 | 实例设置（PageInstanceLeft） | Overall(0)/Setup(1)/Export(2)/World(3)/Screenshot(4)/Mod(5)/ModDisabled(6)/ResourcePack(7)/Shader(8)/Schematic(9)/Install(10)/Server(11) |
| CompDetail | 8 | 资源工程详情（PageDownloadCompDetail，副页面） | — |
| GameLog | 10 | 实时日志（PageLogLeft/Right，副页面） | — |
| VersionSaves | 12 | 存档管理（PageInstanceSavesLeft） | SavesInfo(0)/SavesDatapack(1) |

---
*统计：P0 章节 10 项（细分功能 108 条）；P1 25 条；P2 18 条；P3 13 条；全文档功能条目共 164 条。调查基线：zh-CN.xaml 3011 键 + Config.cs 全部配置项 + 24 个 Page 目录 + 88 个 Mod/模块文件。*
