> ⚠️ **历史文档，已废弃（存档仅供参考）**

> 这是项目**早期（2026-08 末）**的整合方案草稿 v0.1，当时设想的路线是
> "以 PCL.Neo next 的骨架为基座、吸收 PCL.Neo main 的 Core 服务层"。

> **实际采用的方案不是这个** —— 最终选择了更彻底的路线：
> **把 PCL-CE 逐字（verbatim）移植到 Avalonia**，保留原仓库的目录结构和代码组织，
> 只做 WPF→Avalonia 的 API 映射。原因见 `AGENTS.md` 的铁律（用户明确要求"移植而不是重写"）。

> 保留本文档是因为它对三个候选代码库的**现状对比**（规模、构建状态、测试情况）仍有参考价值。

---

# PCL 跨平台整合方案（草稿 v0.1）

> 目标：以 PCL.Neo (next) 的 Avalonia 12 + .NET 10 骨架为基座，吸收 PCL.Neo (main) 的跨平台核心服务资产，
> 移植 PCL-CE 的功能逻辑，形成用户自有新仓库，在 Linux 上原生运行。

## 1. 三个代码库的现状（已验证）

| 代码库 | 技术栈 | 规模 | 构建状态 | 测试 | 定位 |
|---|---|---|---|---|---|
| PCL-CE (dev) | WPF, net10.0-windows | 78k 行 C# + 9.5k 行 XAML (主程序) + 38k 行 (Core) | ✅ 0 错误 | 无 | 功能最全的当前版本，但绑死 Windows |
| PCL.Neo main | Avalonia **11.3.10**, net10.0 | 17k 行 C# + 1.3k 行 axaml | ✅ 0 错误 | ❌ 42 中 32 失败（弃坑中间态） | 资产来源：Core 服务层 (MIT) |
| PCL.Neo next | Avalonia **12.0.4**, net10.0 | ~500 行骨架 | ✅ Win + WSL 均 0 错误 | - | **新仓库基座** |

## 2. 已确认的架构决策

1. **基座 = PCL.Neo next 分支结构**：`src/PCL.Neo`（App 入口+视图）、`src/PCL.Neo.Core`（业务核心）、
   `src/PCL.Neo.UI`（NeoTheme 控件库）、`src/PCL.Neo.Desktop`（桌面启动器）、`tests/`。
   理由：唯一在 Linux 上验证过构建的组合；Avalonia 12 是当前主线方向。
2. **Core 服务层优先从 Neo main 移植**（MIT 协议，无法律负担），按"移植矩阵"选择性吸收，
   失败测试相关的部分先修后用或重写。
3. **业务逻辑从 PCL-CE 移植**：启动管线、下载、版本管理、Java 管理等，按移植矩阵解耦
   WPF（MyMsgBox/Hint → 服务接口/事件）与 Windows API。
4. **UI 全部基于 NeoTheme 新写**，视觉参照 CE 的 XAML（布局/配色/交互），不直接复制 WPF XAML。
5. **命名空间保持 `PCL.Neo.*`**（避免与上游两仓库的大规模改名冲突，方便对照移植）；
   仓库名由用户决定。

## 3. 移植矩阵（已完成盘点）

- ✅ PCL.Neo main Core 资产清单 → `PCL.Cross/docs/inventory-neo-main.md`
  （约 8,600 行几乎整体迁入，仅 2 处 Avalonia 引用已拆除；UI 层不迁移）
- ✅ PCL-CE 移植矩阵 → `PCL.Cross/docs/porting-matrix-ce.md`
  （约 60% 业务逻辑可低摩擦移植；Top5 优先：Core 纯逻辑包 → 配置/IO → 下载栈 → 崩溃分析 → 实例解析）

## 4. 里程碑

- M0 ✅ 三库构建基线验证（本文件 §1）
- M1 ✅ 新仓库骨架：PCL.Cross（占位名，git main 分支 bbf2942，171 文件）
  - Core 服务合并，Windows + WSL 双端构建 0 错误
  - AppSettings 去除 Avalonia.Styling（自研 AppTheme），SkiaExtension 去除 Avalonia.Bitmap，移除 SevenZip/AudioDemo
- M2 ✅ 基础服务验证与落地
  - M2a ✅ Core 冒烟测试（9/9 双端通过；修掉 ConfigurationManager 反序列化 Bug）
  - M2b ✅ 批次1 移植（commit 4417e23，+10,391 行）：CE Core 的 Minecraft 认证/存档/资源、IO（含 DoH DNS）、Logging、Utils 纯逻辑全家
    - 拆除法：HttpSenderExtension 极简复刻（签名兼容）、DoH 系统代理、Files.cs 去 WPF（剪贴板/对话框方法移除）、Lang 直串回退
    - 批次2 待办：App/{Paths,Basics,Localization去WPF,IoC}、IO/Storage 缓存栈、NetworkService、LoaderDownload 下载栈
- M3 ✅ 启动管线冒烟跑通（headless，WSL 实测）：
  - Mojang 官方源在线安装 JRE 到 Linux（FetchJavaOnline：LZMA 下载 + SHA1 校验 + exec 位 + java -version 实跑）
  - 1.21.1 版本 JSON 解析 + 完整启动命令构建（natives-linux 在 classpath、windows 系 jar 被排除、主类/用户名/assetIndex 齐全）
  - 修复 Neo main 遗留规则求值反转 Bug（RuleEvaluator 标准顺序求值）+ macOS→osx 映射
- M3+ ✅ 达成：Linux headless 真实启动 Minecraft（commit 277e475，E2E 冒烟 3/3）
  - VanillaInstaller：客户端/库并行下载（SHA1 校验，58 文件）→ natives 双格式解压（8 个 .so）→ 资产索引 →
    按 javaVersion.component 装 JRE（1.21.1 → delta/Java 21.0.7）→ 进程拉起 →
    日志到达 [Setting user:, Backend library: LWJGL]
  - 本轮修复的 Neo main 遗留 Bug：规则求值反转、远程清单大小写敏感反序列化恒 null、
    natives 分类符缺连字符（nativeslinux→natives-linux）、ArgumentList 下手工引号破坏路径
  - 已知待办：两套规则求值器合并到 RuleEvaluator；游戏无显示退出为预期（完整画面启动留给 VM 阶段验证）
- M3++ ✅ GameMonitor 日志驱动监控（89c80a0）：stdout/stderr 行事件 + 退出码 + 8 类崩溃特征检测，
  替代 ModWatcher；Directories/Files 平台守卫清理（CA1416 清零）；E2E 冒烟 3/3 双端验证
- M3++ ✅ Fabric 版本链路跑通（5710434，WSL E2E 实测）：原版安装 → Fabric meta profile → loader 库 →
  Java 21 拉起 → 日志命中 [Fabric Loader, Setting user:]。本轮再修 3 处继承版本缺陷：
  InheritsFrom 丢失、Arguments 合并被覆盖、Fabric maven 风格库不识别（name 推导回退）
  - VanillaInstaller 增加完整 assets 下载能力（InstallAssetsAsync，含 SHA1 去重）
  - 说明：loader 安装选择自研轻量实现（公开 meta API），CE 的 ModDownload 链（UI loader 回调耦合）降为功能对照参考
- M4 ✅ 骨架达成（088005f）+ 补全（4773f1d）：NeoTheme 接入、Fabric 安装按钮、内存上下限设置、
  日志自动滚动、跨线程 UI 分发（Dispatcher）；WSL Xvfb 无头 UI 冒烟 [SMOKE-UI] UI-OK 双轮通过
  —— **UI 结构验证不依赖 VM，VM 只做人机交互体验测试**
  - 待办：版本隔离/更多设置项、皮肤/主题切换
- M5 ✅ 部分达成（7d66fdb）：Forge/NeoForge 安装器（官方 installer --installClient 跨平台方案，
  WSL Forge 1.21.1 E2E 2m58s 通过）+ 安装按钮入 UI + CI 工作流（win+ubuntu 矩阵 + Linux 冒烟）
  - M5 待办：账户持久化（离线/微软/Yggdrasil）、整合包导入、Mod 管理页
- M5 ✅✅ 全功能主链达成（daf1603）：
  - 版本：原版/Fabric/Forge/NeoForge 安装启动（E2E 全覆盖）
  - 账户：离线持久化 + 微软设备码 + Yggdrasil（含 authlib-injector 注入）
  - 管理：Mod 列表/启停/删除、内存设置、整合包导入（Modrinth 完整 / CF 骨架）
  - 质量：15 测试（原版/Fabric/Forge/整合包四条 E2E 冒烟）+ 双平台 CI
  - M5 待办：整合包版本隔离、CF API 外部文件导入、皮肤预览、联机页
- M2b 批次2（可选）：App 基础设施 + IO/Storage 缓存栈 + NetworkService
- M5 账户/登录、Mod 管理、整合包等高级功能

## 5. 开放问题

- 新仓库名称（用户：回来再定；本地目录现为 PCL.Cross 占位，改名零成本）
- Neo main Core 依赖 Avalonia 11 包 → ✅ 已解决（2 处引用拆除，Core 现为纯 .NET）
- WebView2 相关功能（登录/公告页）在 Linux 的替代方案（CefGlue? 内置浏览器视图? 纯 API 登录?）

---

## 附：当时涉及的代码库（供追溯）

| 代码库 | 远程地址 | 说明 |
|---|---|---|
| PCL-CE | `https://github.com/qsc1918/PCL-CE.git` | **本项目的上游基线**，固定 `eaed9d80`（分支 dev） |
| PCL.Neo | `https://github.com/PCL-Community/PCL.Neo.git` | 早期设想的资产来源（MIT），**未采用** |
| PCL.Neo next | 同上（同仓库另一分支/目录） | 早期设想的基座，**未采用** |
| PCL.Cross / PCL-cross | （无远程，本地目录） | 早期实验，**未采用** |
