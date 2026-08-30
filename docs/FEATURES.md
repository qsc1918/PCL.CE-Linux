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

<!-- 待续 -->
