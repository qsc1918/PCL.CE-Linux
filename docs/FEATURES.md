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

<!-- 待续 -->
