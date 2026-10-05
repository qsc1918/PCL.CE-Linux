# PCL-CE → Avalonia/Linux 移植 · 交接文档（HANDOVER）

> **最新更新：2026-10-05** ｜ 当前 commit：`24e597a`（工作树含未提交的诊断代码，见 §0.7）
> **一句话现状**：编译 0 错误、Windows 运行正常、**Linux 能启动但主窗口全黑**（唯一阻塞）。
>
> 📌 **新代理请先读 §0**（最新状态与当前唯一阻塞），再按需查阅后面章节。
>
> 配套文档（都在本仓库 `docs/` 下）：
> - `LINUX-BLANK-WINDOW.md` — **Linux 全黑窗口专项排查档案**（含真机截图证据，与 §0 互补，细节更全）
> - `av12-notes.txt` — Avalonia 12 实测 API 更正（权威，与 A11 指南不同）
> - `PORTING-MAPPING.md` — 转换规则表（**GBK 编码**，读取需 `Encoding.GetEncoding(936)`）
> - `FEATURES.md` — 功能清单与 P1/P2/P3 优先级（待用户确认）
> - `port-mapping-g4.md` — 早期分批移植的作业说明
> - `img/` — 真机截图证据（修复前后对比）

---

# 0. 最新状态与当前唯一阻塞（2026-10-05）

## 0.1 状态总览

| 项目 | 状态 |
|---|---|
| C# / XAML 编译错误 | ✅ **0**（C# 从 10564 清零，XAML 从 792 清零） |
| Windows 运行 | ✅ 主窗口稳定、无 FTL/ERR |
| **Linux 启动** | ✅ **能启动**（窗口创建成功、900x550、标题 `PCL CE Linux`、`Map State: IsViewable`、日志 0 FTL/0 ERR） |
| **Linux 显示** | ❌ **主窗口客户区全黑**（← **当前唯一阻塞**，见 §0.3） |
| Linux 内存/CPU | ❌ 泄漏：CPU 86~115%，内存涨到 **8.1 GB**（约 150 MB/s） |
| 暂缓功能 | ❌ 用户指定的 8 项未移植（见 §8） |

## 0.2 已经做完的 Linux 攻坚（都有 commit）

按时间顺序，从"完全跑不起来"到"能启动"：

| commit | 修复内容 |
|---|---|
| `37bcd32` | 弹窗 NRE、跨线程 `DispatcherFrame`、ContextMenu `BoxShadow`、缺 `ItemsControl` 模板 |
| `68483ed` | 模板部件查找、null `Text` 校验、按钮方角 |
| `b1f6cb7` | `PageLoginProfile` 的 `*` 行放宽为 `Auto` |
| `90efa3a` | 致命退出路径可用；启动失败时暴露真实原因 |
| `697897e` | 阻止两个 Windows 专属调用在 Linux 上杀死启动 |
| `06cbfc1` | 又三个启动阻塞点；程序改名 `PCL CE Linux` |
| `4909bf3` | 非 Windows 下给窗口补不透明背景（上游 `Background="{x:Null}"` 在 Linux 会全透明） |
| `4b70583` | 启动路径用平台分隔符 |
| **`6f7aaa3`** | **不再在非 Windows 调用 Win32 置顶 API**（← 修掉了"启动即卡死"） |
| `2748494` `af004b6` | 跳过三个 Windows 专属启动步骤 |
| `8187b4d` `7c316e2` | 非 Windows 下配置文件密钥不用 DPAPI |
| `24e597a` | 本地发布脚本，编译期注入 OAuth client id |

**⚠️ 一个重要教训（见 §7 坑 36）**：这些修复是**交替**的 —— 中间有几轮我误判了病因（以为是内存泄漏/动画泄漏），
加探针 → 排除 → 再修。所以历史里有若干 `diag:` 提交和"探针清理"提交，属正常过程。

## 0.3 ⭐ 当前唯一阻塞：Linux 主窗口全黑

### 现象（真机截图实测）

窗口**能创建、能显示、标题正常**，但**客户区一个像素都不画**：

| 观测项 | 数值 |
|---|---|
| 窗口 | 900x550，`0x140000f "PCL CE Linux"`，`Map State: IsViewable` |
| 客户区内容 | **颜色数=1，采样像素 `gray(0)`（纯黑）** |
| 标题栏 | 正常（**由 mutter 画的，不是 Avalonia 画的**） |
| CPU / 内存 | 86~115% / 涨到 8.1 GB |

截图证据：`docs/img/linux-blank-window-before.png`

### 根因（已确认并验证）

`src/Plain Craft Launcher 2/FormMain.xaml.cs` 构造函数里有：

```csharp
Opacity = 0d;     // ← 窗口创建时完全透明
```

透明度靠 `FormMain_Loaded` 里的 **"Form Show" 动画组**拉回来：

```csharp
ModAnimation.AniStart(new[]
{
    ModAnimation.AaCode(() => ModAnimation.AniControlEnabled -= 1, 50),
    ModAnimation.AaOpacity(this, Config.Preference.Theme.WindowOpacity / 1000d + 0.4d, 250, 100),
    ModAnimation.AaDouble(i => TransformPos.Y += i, -TransformPos.Y, 600, 100),
    ModAnimation.AaRotateTransform(i => TransformRotate.Angle += i, -TransformRotate.Angle, 800, 100),
    ...
}, "Form Show");
```

**这个动画组在 Linux 上从未生效** ⇒ `Opacity` 停在 0 ⇒ 窗口整体透明 ⇒ 看起来一片黑。
（同一动画组还负责归零 `TransformPos.Y`=60 和 `TransformRotate.Angle`=-4°，所以即使强行给透明度，画面也是**倾斜+下移**的。）

### 验证（决定性）

加临时开关 `--opacity1` 跳过 `Opacity = 0d`，其余代码不动：

| 条件 | 颜色数 | 中心像素 | 结果 |
|---|---|---|---|
| 原样 | 1 | `gray(0)` | 全黑 |
| `--opacity1` | **3290** | `srgb(209,209,209)` | **界面正常渲染** |

截图：`docs/img/linux-ui-renders-with-opacity1.png`（能看到蓝色标题栏、"启动/下载/设置/工具"四标签、"正在加载版本列表"）

### 更深一层的原因（已定位到这一层，但未修）

用文件版看门狗（`--uidump`，输出到 `<程序目录>/uidump.txt`）拿到：

```
[P1]~[P9] 全部打印            ← 启动路径完整跑完（构造→Loaded→ShowWindowToTop→AniStart→末尾）
[UiDump #1] 动画组=1  等待弹窗=1  AniControlEnabled=1  线程数=36
[UiDump] >>> UI 线程任务被执行了!    ← 从未出现
```

**推论链**：

| 环节 | 状态 |
|---|---|
| `FormMain_Loaded` 执行完 | ✅ 正常 |
| 动画线程 `RunInUiWait(AniTimer)` | ❌ 卡住（`Dispatcher.Invoke` 无响应） |
| 主时钟线程 `RunInUiWait(TimerMain)` | ❌ 同样卡住 |
| `Dispatcher.UIThread.InvokeAsync(...)` 投递 | ❌ **从未被执行** |
| 后果 | `AniControlEnabled` 恒=1；弹窗队列不清空；动画不生效；窗口停在透明 |

⇒ **UI 线程回到了消息循环，却不响应任何来自其他线程的 Dispatcher 投递。**

**注意**：窗口**能渲染**（`--opacity1` 下 3290 色）⇒ Avalonia 的**渲染线程是独立的、在正常跑**，
卡住的只是 UI 线程本身。怀疑 UI 线程卡在消息循环内部的某个同步操作（布局循环？）。

### 下一步排查建议

1. 在真机装 `gdb` 附加取 UI 线程原生栈（`apt-get install gdb`，sudo 密码见 §0.5）
2. 或在 Avalonia 的布局/渲染入口加探针，确认 UI 线程当前在哪个调用里
3. 参考 `docs/LINUX-BLANK-WINDOW.md` 的完整排查记录（含所有已排除项）

### ⚠️ 修正一条早期错误结论

**`ModMain.TimerMain` 不是 `DispatcherTimer`**，而是后台线程：
```csharp
while (true) { ModBase.RunInUiWait(TimerMain); Thread.Sleep(49); }
```
所以"嵌套消息泵不跑 DispatcherTimer"这条**是错的**。真正的问题更基础：
**UI 线程根本不响应跨线程 Dispatcher 投递**。

## 0.4 临时诊断开关（已加在代码里，**修好后应清理**）

这些开关都在 `FormMain.xaml.cs`，用 `Basics.CommandLineArguments.Contains(...)` 判断，标记为 `[port][TEMP]`：

| 开关 | 作用 |
|---|---|
| `--opacity1` | 跳过 `Opacity = 0d`（验证用，可让界面显示出来） |
| `--skip-eula` | 跳过 EULA 阻塞弹窗（验证用，**已验证不足以解决问题**） |
| `--uidump` | 启动 UI 线程看门狗，每 3 秒把状态/调用栈写到 `<程序目录>/uidump.txt` |
| `--pagetest` | 页面实例化自检（见 §12，Windows 期就在用） |
| `--logintest` | 正版登录链路自检（见 §12） |

另：`FormMain.xaml.cs` 里还有 `[P1]`~`[P9]` 顺序探针（`Console.Error.WriteLine`），
用于定位启动路径卡点。**这些是临时脚手架，问题修复后请一并删除。**

## 0.5 测试环境（关键，能省很多时间）

### 真机（用户的 VMware Linux VM）—— 推荐用这个

```bash
# 从 WSL 连接（WSL 默认网关 = Windows 宿主）
H=172.22.192.1
ssh abc@$H -p 62222          # 密码 abc
# sudo 密码：abc（注意：不是 root！root 用户的密码才是 root）
```

WSL 里已装 `sshpass`，标准写法：

```bash
H=172.22.192.1; export SSHPASS=abc
sshpass -e ssh -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -p 62222 abc@$H "<命令>"
sshpass -e scp -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -P 62222 <本地> abc@$H:/tmp/
```

**真机环境**：Ubuntu 24.04、**GNOME + Wayland**（`mutter` 合成器，`Xwayland :0 -rootless`）、无 .NET SDK。
已装工具：`imagemagick`（`import`）、`x11-utils`（`xwininfo`）。

### 部署 + 运行 + 截图（已验证的完整流程）

```bash
# 1. 本地打包
cd D:/a/j/PCL.CE-Linux/dist && tar czf /d/a/j/pcl-flat.tgz linux-x64-flat

# 2. 传到真机（本地网络很快，55MB 约 1.5 秒）
#    scp 到 /tmp，解压，chmod +x linux-x64-flat/PCL

# 3. 运行（环境变量必须对，少了 XAUTHORITY 连不上 X）
export DISPLAY=:0
export XAUTHORITY=$(ls /run/user/1000/.mutter-Xwaylandauth.* | head -1)
./PCL &
sleep 18

# 4. 截图（找内层真正的 PCL 窗口 —— 外层是 mutter 装饰框架，尺寸 928x616 不是 900x550）
WID=$(xwininfo -root -tree | grep '"PCL' | grep -v mutter | awk '{print $1}' | head -1)
import -window $WID /tmp/shot.png
convert /tmp/shot.png -format "尺寸=%wx%h 颜色数=%[colors]" info:   # 颜色数=1 就是全黑
```

### 本地 WSL 快速复现（不用真机，迭代更快）

```bash
Xvfb :99 -screen 0 1280x800x24 &
xcompmgr -c &              # ← 必须！否则窗口全黑，会把"缺合成器"误当成根因
DISPLAY=:99 ./PCL &
import -window root shot.png
```

### 踩坑记录（环境相关）

- `echo root | sudo -S` 与 `bash -s` 会**抢同一个 stdin** → 报 "no password was provided"。
  改用 `SUDO_ASKPASS`（写个脚本输出密码，`SUDO_ASKPASS=... sudo -A ...`）
- 截图一定要抓**内层窗口**，外层是 mutter 装饰框架
- **诊断输出必须直接写文件**（`File.AppendAllText`）。stdout 重定向到文件 + SIGKILL 会丢缓冲，
  表现为"探针明明执行了却没有输出" —— 这个坑让我浪费了一整轮
- `sudo -S` 的密码在 WSL 里是 `root`，在真机 `abc` 用户下是 `abc`

## 0.6 构建与发布命令（权威）

```bash
# 完整构建（唯一权威的错误检查方式）
dotnet build PCL.CE-Linux.slnx -v q -nologo --no-incremental

# 发布 Linux（两种产物）
#   单文件（给用户）：-o dist/linux-x64
#   非单文件（便于排查）：-p:PublishSingleFile=false -o dist/linux-x64-flat
dotnet publish "src\Plain Craft Launcher 2\Plain Craft Launcher 2.csproj" \
  -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=false -o dist/linux-x64-flat

# 给用户/真机部署时记得 chmod +x
chmod +x dist/linux-x64-flat/PCL
```

`dist/` 已在 `.gitignore`（125MB，勿入库）。

## 0.7 工作树状态（交接时）

- HEAD = `24e597a`，与 `origin/master` 同步
- **未提交**：`FormMain.xaml.cs` 的诊断开关与探针（§0.4）、`docs/LINUX-BLANK-WINDOW.md`、`docs/img/`
- 用户要求：**可以做本地 git 操作，但 push 会弹账号选择框，需用户在场**

## 0.8 建议的接手顺序

1. 读 §0.3 + `docs/LINUX-BLANK-WINDOW.md`，理解当前阻塞
2. 按 §0.5 在真机上跑一遍，亲眼看到全黑 + 用 `--opacity1` 看到界面
3. 按 §0.3「下一步排查建议」查 UI 线程卡在哪
4. 修复后：清理 §0.4 的临时开关 → 跑 §12 的 `--pagetest` 回归 → commit
5. 之后再考虑 §9 的暂缓功能移植

---

## 1. 项目定位与铁律（不可违反）

**目标**：把 PCL-CE（WPF Minecraft 启动器）**逐字（verbatim）移植**到 Linux 上的 Avalonia 12 / .NET 10。

**用户原话（必须时刻遵守）**：
> "然后记住目标是移植pcl ce的代码并不是重写，所以说你也要时时刻刻记着不要重写ui。要保留原有的功能逻辑和风格，这样子的话才是pcl ce启动器，而不是一个披着pcl名字重写的启动器。"

**铁律清单**：
1. **只做 API 名称/签名映射，绝不重新设计 UI**。布局/颜色/文案/交互必须与上游一致。
2. 所有非纯映射的改动**必须**带 `// [port] <原因>` 中文注释。
3. 编译结果**只信**完整构建：`dotnet build ... -v q -nologo --no-incremental`。增量构建和子代理自报的"0 错误"会**掩盖残留错误**（血泪教训）。
4. **禁止用 pwsh 批量读写 .cs 文件**（曾把 `public` 写成 `uublic` 导致语法错误）；C# 编辑一律用 read + edit 工具。
5. 频繁 commit；长文档/代码用批量 write，不要逐行 edit。
6. 所有给用户的问题**攒到最后批量问**，不要逐个打断。

---

## 2. 当前状态总览

> ⚠️ **本节是 2026-09-27 的历史快照**，仅记录"编译清零 + Windows 运行"阶段的成果。
> **最新状态请看 §0.1**。下面表格里所有"待用户 VM 实测"的项目**已经在 2026-10-04/05 完成**，
> 结论是：Linux 能启动但窗口全黑（§0.3）。

| 项目 | 状态 |
|---|---|
| C# 编译错误 | ✅ 0（从 10564 清零） |
| XAML (AVLN) 编译错误 | ✅ 0（从 792 清零） |
| Windows 运行 | ✅ 主窗口稳定存活，`responding=True`，日志无任何 FTL/ERR/Exception |
| 本地化 | ✅ `当前 UI 语言: zh-CN` + `本地化 启动成功` |
| 生命周期 | ✅ 依赖检查→公告→更新检查→遥测→优雅关闭全走通（最新日志 573 行干净） |
| 透明窗口问题 | ✅ **已确认正常** — 窗口渲染正常（用户截图 + 离屏渲染均正常），原"透明"是崩溃前的瞬时状态 |
| 页面实例化 NRE | ✅ **已修（2026-09-28）** — 根元素 `x:Name`（坑 12）、集合属性 `x:Array`（坑 13）、`[Content]` 路由（坑 14） |
| 实例选择/设置崩溃 | ✅ **已修（2026-09-28）** — `AvaloniaProperty` 字段可见性（坑 15）+ 坑 13；`--pagetest` 4 项真实导航全通过 |
| 本地化 `!key!` | ✅ **已修（2026-09-28）** — 非 UI 线程取不到资源（坑 16） |
| 配色整体偏暗 | ✅ **已修（2026-09-28）** — scRGB/sRGB 混淆（坑 17）；离屏渲染采样与上游窗口一致 |
| 内嵌图片加载 | ✅ **已修（2026-09-28）** — avares 大小写 + AssetLoader 取流（坑 18） |
| 已知未修 | ⚠️ `设置→更新`页（上游缺 `Tahoe.png`，见坑 18）；`联机`页（暂缓功能） |
| Linux 发布产物 | ✅ 已产出 — `dist/linux-x64/PCL`（单文件）+ `dist/linux-x64-flat/`（非单文件） |
| **Linux 启动** | ✅ **已完成（2026-10-04/05）** — 能启动，0 FTL/0 ERR |
| **Linux 显示** | ❌ **未解决** — 窗口全黑，见 §0.3 |
| 暂缓功能 | ❌ 用户指定的 8 项暂未移植（见 §8） |

**Git 提交历史（最新在前）**：
```
a07dfb8 runtime: localization working, zero runtime errors, app stays open
0c1027f runtime: fix animation per-frame errors (dynamic cannot resolve extension methods)
e84605b runtime: thread-safe resource lookup, localization resources packaged, brush cast mapping
2b72012 runtime bring-up: fix populate-time NRE and brush type mismatches
74b0d48 XAML compile wave complete: project builds with 0 errors
b01e0f4 XAML compile wave: exclude Markdown.axaml; move BlurBorder/MotdRenderer stubs to PCL.Core
102af8d MyListItem: ColumnGap via ColumnDefinitions[5]
e8cd191 MyMsgMarkdown: declare : Grid base for Avalonia name-field codegen
3c36db0 clean up temporary error-partition files
037c8e5 MyMsgMarkdown: restore axaml to codegen; degrade MarkdownViewer to SelectableTextBlock
```

---

## 3. 仓库结构与关键路径

```
D:\a\j\PCL.CE-Linux\          ← 本仓库（移植工作区）
├── src\
│   ├── Plain Craft Launcher 2\   ← 主工程（AssemblyName=PCL, RootNamespace=PCL）
│   ├── PCL.Core\                 ← 核心库（EnableDefaultItems=false, 手动 Include）
│   └── PCL.Core.SourceGenerators\← 原样保留
├── docs\                         ← 本文档所在地
├── tools\                        ← 批量修复脚本 + linux-debug/（真机与 Xvfb 调试脚本）
└── 上游基线：见下方「如何获取上游」

构建输出：src\Plain Craft Launcher 2\bin\Debug\net10.0\PCL.exe (+ PCL.dll 5.0MB)
运行日志：同目录 PCL\Log\Launch-*.log（**GBK 编码**）
```

### 如何获取上游基线（重要）

原开发机上的 `D:\a\j\PCL-CE` 是**本地克隆**，接手的人拿不到。请自己克隆并**固定到同一个 commit**：

```bash
git clone https://github.com/qsc1918/PCL-CE.git
cd PCL-CE
git checkout eaed9d80adfa67bb6cbdeac0b9ea72de726bcadc
```

- 仓库：`https://github.com/qsc1918/PCL-CE.git`，分支 `dev`
- 固定版本：**`eaed9d80`**（2026-08-24，`feat(i18n-banner): 在语言设置横幅中添加翻译平台入口 (#3560)`）
- ⚠️ **务必 checkout 到该 commit**，不要用 `dev` 最新 —— 本仓库所有移植决策都以这个版本为对照基准。
  用别的版本做 diff 会看到大量无关差异。

---

## 4. 构建与运行（权威命令）

```powershell
# 完整构建（唯一权威的错误检查方式）
dotnet build "D:\a\j\PCL.CE-Linux\src\Plain Craft Launcher 2\Plain Craft Launcher 2.csproj" -v q -nologo --no-incremental

# 运行
& "D:\a\j\PCL.CE-Linux\src\Plain Craft Launcher 2\bin\Debug\net10.0\PCL.exe"

# 读运行日志（GBK；运行中要 FileShare.ReadWrite）
$p = "...\PCL\Log\Launch-最新.log"
$lines = [System.IO.File]::ReadAllLines($p, [System.Text.Encoding]::GetEncoding(936))
$lines | Select-String "FTL|未捕获|InvalidCast|RuntimeBinder|NullReference|FileNotFound|UriFormat|启动失败|ERR!"

# 检查进程是否存活/响应
Get-Process PCL | Select-Object Id, Responding, MainWindowTitle
```

**环境事实**：
- .NET 10 SDK 在 Windows 主机可用；Avalonia 12.0.4 已在 nuget 缓存。
- **本机 WSL 被安全策略拉黑**（`wsl.exe` 在 Program Blacklist）→ §9.2 的 WSL headless 冒烟**无法在本机执行**，只能用静态校验 + 用户 VM 实测替代（见 §9.2）。
- pwsh 工具按 GBK 读无 BOM 的 .ps1 → **脚本必须纯 ASCII 或带 BOM**。
- pwsh 的 `Get-Content` 显示中文为乱码（仅显示问题）；`[System.IO.File]` API 用进程 CWD 而非 PowerShell 位置 → **一律绝对路径**。
- 上游源文件编码混杂（GBK/UTF-8），改文件前先校验。

---

## 5. 关键文件与改动清单

### 5.1 工程文件

**`src\Plain Craft Launcher 2\Plain Craft Launcher 2.csproj`**：
- `AssemblyName=PCL`，`RootNamespace=PCL`，`StartupObject=PCL.Program`
- 包引用：Avalonia / Avalonia.Desktop / Avalonia.Themes.Fluent 12.0.4；SkiaSharp **3.119.4**（从 3.119.0 升，修 NU1605 降级冲突）
- `EnableDefaultItems=true`（**必须**：false 会让 Avalonia 命名字段生成器整体静默失效 → 数千"字段不存在"级联错误）；排除统一用 `<Compile Remove>` / `<AvaloniaXaml Remove>`
- `DeferredExcludes`（用户指定暂缓）：CrashAnalysis / ModMusic / ModVideoBack / ModLink / ModSkin / PageTools / ClipboardInterceptor / LazyLoadBehavior
- `<AvaloniaXaml Remove="Resources\Markdown.axaml" />`（Markdown.Avalonia 主题，Avalonia 12 无法解析其 StyleKey，792 个 AVLN2000 的来源）
- `<AvaloniaXaml Remove="Pages\PageTools\**" />`（.cs 已排除但 .axaml 仍会被 glob 收集报 Unable to find type）

**`src\PCL.Core\PCL.Core.csproj`**：
- `EnableDefaultItems=false` + 手动 `<Compile Include="**\*.cs" Exclude="...">`
- 语言文件三重打包（关键！见 §7 坑 6）：
  - `<AvaloniaXaml ... Exclude="...zh-CN.xaml 等 7 个">`（阻止 XamlIl 编译）
  - `<AvaloniaResource Include="App/Localization/Languages/**/*.xaml" />`
  - `<EmbeddedResource ... LogicalName="PCL.Core.App.Localization.Languages.%(Filename)%(Extension)" />`（回退用）

### 5.2 移植支撑文件（新增）

| 文件 | 内容 |
|---|---|
| `src\PCL.Core\UI\Controls\Stubs.cs` | `BlurBorder : Border` + `MotdRenderer : Control`（namespace PCL.Core.UI.Controls，放这里 XAML 的 clr-namespace 才能解析 assembly=PCL.Core） |
| `src\Plain Craft Launcher 2\Stubs.cs` | namespace PCL：ModVideoBack/ModMusic/ModSkin 桩 + `MyColorExtensions.ToBrush()/ToColor()` + `ControlTemplateExtensions.FindName()` |
| `src\Plain Craft Launcher 2\ControlResourceHelper.cs` | `SetResourceReference(this AvaloniaObject, AvaloniaProperty, string)` 扩展 — 用 `target.Bind(property, ctrl.GetResourceObservable(key))` 实现 WPF 动态资源语义（主题切换感知）。**调用点必须写 `this.` 前缀**（扩展方法需要接收者） |

### 5.3 运行时核心修复（按文件）

| 文件 | 修复 |
|---|---|
| `Modules\Base\ModBase.cs` | `MyColor(object)` 增加 ISolidColorBrush 分支（原只认 SolidColorBrush，dynamic `.A` 在 ImmutableSolidColorBrush 上炸）；新增 ctor `MyColor(double newA, IBrush brush)` |
| `Modules\Base\ModAnimation.cs` ~L149 | dynamic 调 SetResourceReference（扩展方法 dynamic 绑不了）→ 强类型：`var colorTargets=(object[])anim.obj; ... ((AvaloniaObject)colorTargets[0]).SetResourceReference((AvaloniaProperty)colorTargets[1], resourceKey)` |
| `Controls\AnimatedBackgroundGrid.cs` | BackgroundBrushProperty/AnimatableBrush/BackgroundBrush：SolidColorBrush→IBrush |
| `Controls\MyCard.cs` | `override IBrush AnimatableBrush` |
| `Controls\MyIconButton/MyLoading/MyListItem` | `Foreground` 类型改 IBrush（StyledProperty + Register 泛型） |
| 8 个控件（MyButton/MyCheckBox/MyExtraTextButton/MyHint/MyIconTextButton/MyListItem/MyRadioBox/MyRadioButton） | 移除 `[Content] public InlineCollection Inlines => LabXxx.Inlines;` 上的 [Content]（见 §7 坑 3） |
| `Controls\MyExtraButton.xaml.cs` | `RectProgress = PanProgress.Clip as RectangleGeometry;`（几何对象不在 NameScope，FindNameScope().Find() 拿 null）+ setter 空保护 |
| `PCL.Core\App\ResourceExtensions.cs` | 重写：ConcurrentDictionary 缓存 + 非 UI 线程返回 null（不 marshal）+ ActualThemeVariant 变化清缓存 |
| `PCL.Core\App\Localization\LocalizationService.cs` | 新增 `_OpenLanguageStream(code)`：先试 `AssetLoader.Open(avares://...)`，失败回退 `assembly.GetManifestResourceStream("PCL.Core.App.Localization.Languages.{code}.xaml")` |
| `PCL.Core\App\Localization\LocalizationFontService.cs` | `_ApplicationPackUri`：`pack://application:,,,/` → `avares://PCL/`（WPF pack URI 在 Avalonia 抛 UriFormatException "Invalid port specified"） |

### 5.4 Application.axaml（作为全部页面翻译范本）

- 4 个带 x:Key 的 `<Style>` 从 `Application.Styles` 移除（Avalonia：Styles 只放选择器样式）；`ComboBoxItemTemplateWithDelete` DataTemplate 移入 `Application.Resources`
- `Easing="CircleEaseOut"` → `"CircularEaseOut"`（WPF 缓动名无效）
- ScrollBar RepeatButtons → `Name="PART_PageUpButton"/"PART_PageDownButton"`（模板部件名，无 RoutedCommands）
- MyScrollViewer 滚动条绑定 → `ScrollBarMaximum.Y` / `Offset.Y (TwoWay)` / `Viewport.Height`，`RelativeSource TemplatedParent`
- MyTextBox 模板补 `<TextPresenter x:Name="PART_TextPresenter" Text="{TemplateBinding Text, Mode=TwoWay}" PasswordChar RevealPassword SelectionStart SelectionEnd CaretIndex TextAlignment TextWrapping />`（TextBox 必需部件）
- 根 `<Grid ToolTip.Tip="{TemplateBinding ToolTip}">` → `<Grid>`（模板根不能这样绑定 ToolTip）

### 5.5 批量修复脚本（tools/）

`fix-axaml-bulk.ps1`（`ToolTip="`→`ToolTip.Tip="`；删 `RenderOptions.BitmapScalingMode`/`ShadowDepth`/`TextFormattingMode`；`Visibility`→`IsVisible`；transform/effect 标签上的 `x:Name` 剥离；`AllowDrop=`→`DragDrop.AllowDrop=`）、`fix-empty-assembly.ps1`（空 `assembly=` → `assembly=PCL`）。

---

## 6. Avalonia 12 差异速查（实测权威版）

> 更全的见 `docs/av12-notes.txt`。此处为最高频条目：

| WPF | Avalonia 12 |
|---|---|
| `PasswordBox` | `TextBox` + `PasswordChar` |
| `Visibility.Collapsed` | `IsVisible="False"` |
| `Application.Current.Clipboard` | `TopLevel.GetTopLevel(c)?.Clipboard` |
| `DragEventArgs.Data` / `e.Operations` | `e.DataTransfer`（同步 IDataTransfer）+ `e.DragEffects`；**无** `e.Data`/`e.Operations`/`DragDropOperations` |
| 选择器 `local:Type` | `local\|Type`（竖线） |
| keyed Style 放 Styles | keyed 项进 `Resources`；选择器样式进 `Styles`（如 `<Grid.Styles>`） |
| `CircleEaseOut` 等缓动 | `CircularEaseIn/Out/InOut` |
| `ScrollViewer.VerticalScrollBarMaximum/Value/ViewportSize` | `Offset`(Vector) / `Extent`(Size) / `Viewport`(Size) / `ScrollBarMaximum`(Size) |
| TextBox 模板 | 必须含 `PART_TextPresenter`（TextPresenter） |
| ScrollBar 模板 | `PART_LineUpButton/PART_LineDownButton/PART_PageUpButton/PART_PageDownButton`（无 RoutedCommands） |
| 模板里绑定 Foreground/FontSize/FontFamily | 继承属性，无需绑定 |
| 资源画刷强转 `SolidColorBrush`/`Brush` | 资源是 `ImmutableSolidColorBrush` → 用 `ISolidColorBrush`/`IBrush` |
| `SetResourceReference` | 无内置 → ControlResourceHelper 扩展（见 §5.2） |
| `ColumnDefinition`/`RowDefinition`/Transform/Effect/`RectangleGeometry` 的 `x:Name` | **不生成字段** → 给所属 Grid/元素命名，按 `ColumnDefinitions[index]` / `RenderTransform[0]` 取 |
| code-behind 命名字段生成 | 要求 partial 类声明 Avalonia 基类（如 `: Grid`） |
| `Mouse.LeftButton` 等静态查询 | 无 → PointerPressed/Released 事件跟踪私有 bool |
| `pack://application:,,,/` | `avares://PCL/` |

**跨线程**：UI 对象（含 `Application.Resources`/`ActualThemeVariant`）只能 UI 线程碰 → ResourceExtensions 已做线程安全。

**C# 语言规则**：`dynamic` **不能绑定扩展方法**（ModAnimation 每帧崩溃的根因）。

---

## 7. 踩坑记录（血泪史，新代理必读）

1. **增量构建/子代理构建掩盖错误** — 只有主会话的 `--no-incremental` 完整构建可信。子代理说"0 错误"≠0 错误。
2. **编码损坏事故** — pwsh 全文件重写把 `public` 写成 `uublic`（p→u），产生 CS1519/CS1002。修复：`git checkout --` 两个文件。此后 C# 编辑只用 read+edit 工具。已验证无 `uublic|urivate` 残留。
3. **`[Content] Inlines` populate 期 NRE** — Avalonia 按**实例类型**路由 XAML 根子元素（WPF 按**根标签类型**），`LabText` 在 `!XamlIlPopulate` 阶段还是 null → 8 个控件的 `[Content] public InlineCollection Inlines => LabXxx.Inlines;` 移除 [Content]。
4. **ImmutableSolidColorBrush** — 资源字典里的画刷是 immutable，`dynamic .A` 抛 RuntimeBinderException（ThemeManager 静态 ctor 崩）、强转 Brush 抛 InvalidCastException → 全链路改 ISolidColorBrush/IBrush + MyColor 构造分支。
5. **动画每帧 RuntimeBinderException** — 日志曾 8135 行/次 → dynamic 调扩展方法（§6）→ 强类型后降到 377 → 最终 304~573 行干净。
6. **本地化三层坑** — ① 语言 xaml 没打包 → AvaloniaResource；② Avalonia targets 仍把它们收集进 XamlIl 编译（URI 带 Build:/Populate: 前缀，AssetLoader.Open 找不到）→ Exclude + EmbeddedResource（LogicalName 稳定）回退；③ 字体服务用 WPF pack URI → UriFormatException → avares://PCL/。
7. **MSBuild `**` 在 Windows 展开为反斜杠路径** — `Exclude` 写正斜杠会静默不匹配；裸 `<Item Exclude="..."/>` 元素非法（MSB4232，需 Include/Update/Remove）。
8. **Avalonia 命名字段生成器** — ① 依赖 EnableDefaultItems=true；② 要求 code-behind 声明 Avalonia 基类；③ 不为 ColumnDefinition/RowDefinition/Transform/Effect/几何对象生成字段。csc 报"字段不存在"= 对应 axaml 未完成 Avalonia 化。
9. **跨线程 VerifyAccess** — Lang.Text 在加载器线程调 TryFindResource → `Application.get_ActualThemeVariant` 抛 InvalidOperationException → ResourceExtensions 非	UI 线程直接返回 null + 缓存。
10. **Markdown.axaml 792 错** — Markdown.Avalonia 主题样式，必须同时从 AvaloniaResource **和** AvaloniaXaml 排除。
11. **`Unable to resolve type ... assembly=PCL`** — 空的 `xmlns:local="clr-namespace:PCL;assembly="`（4 个文件）；桩类要放到被引用的程序集（BlurBorder/MotdRenderer → PCL.Core）。
12. **根元素 `x:Name` 与 WPF 语义不同（2026-09-28 修复，崩溃级）** — WPF 中 `<Grid x:Name="PanBack">`（MyListItem 等控件根元素）生成的字段**绑定到控件自身**（`this.PanBack = target;`，target 即 PCL.MyListItem），故 `PanBack.Children` == `this.Children`。
    Avalonia 命名字段生成器**另生成一个 `Grid PanBack` 字段**，并在 `!XamlIlPopulate` 阶段（Populate 之前、名字作用域尚未建立）执行 `PanBack = __thisNameScope__?.Find<Grid>("PanBack")` → 该值**恒为 null**。
    当子元素的属性 setter 在 Populate 期间就访问它（如 `MyListItem.Buttons` 里 `PanBack.Children.Add(...)`），必抛 NullReferenceException。
    **实测崩溃点**：`PageInstanceLeft.axaml` 实例化（`FormMain.PageChange` ← `PageLaunchLeft.BtnMore_Click`）→ 点"实例设置"即崩、程序退出。
    **修法（已用）**：从 axaml 根元素**删掉 `x:Name`**，在 code-behind 加属性 `private MyListItem PanBack => this;`（还原上游语义）；同时 axaml 内凡 `ElementName=PanBack` 的绑定改为 `RelativeSource={RelativeSource AncestorType=local:MyListItem}` 并补 `xmlns:local`。
    注意：生成器会与同名手写成员冲突（CS0102），所以必须删 x:Name，不能只加手写字段。
    **排查命令**：`for cls in <控件名>; do grep -c "\bPanBack\b" Controls/$cls.xaml.cs; done` — 根元素带 `x:Name` 且 .cs 有引用的控件都可能踩雷（MyListItem 已修；MyIconButton 的 PanBack 是**嵌套** Border，安全）。

13. **Avalonia 没有 `x:Array`（崩溃级，2026-09-28 修复）** — 上游用 `<x:Array Type="{x:Type local:MyIconButton}">` 给 `IEnumerable<MyIconButton>` 属性（`MyListItem.Buttons`）赋值。移植时误判并**删掉了包裹层**，直接把子元素内联进属性元素 → **编译能过，但运行期 `!XamlIlPopulate` 对尚未初始化的集合字段做添加 → NRE**（`PageInstanceLeft.axaml:20`，点"实例设置"即崩）。
    - Avalonia 至今不支持 `x:Array`（维护者原话："Arrays aren't currently supported. For now you can use `System.Collections.ArrayList` or `System.Collections.Generic.List`"）。
    - **正确等价写法**（实测）：
      ```xml
      xmlns:sys="clr-namespace:System.Collections.Generic;assembly=netstandard"
      <local:MyListItem.Buttons>
          <sys:List x:TypeArguments="local:MyIconButton">
              <local:MyIconButton ... />
          </sys:List>
      </local:MyListItem.Buttons>
      ```
      ⚠️ `assembly` **必须是 `netstandard`**；写 `System.Runtime` 会报 `Unable to resolve type List from namespace ... assembly=System.Runtime`。`Application.axaml` 已占用 `xmlns:sys`（指向 `System`），故那里改用前缀 `sysgen`。
    - 修复脚本：`tools/fix-buttons-collection.py`（37 处，5 个文件）。
    - **诊断手法**：`--pagetest` 自检（见 §12）。反向对照（把包裹层删掉重跑）能精确复现同一 NRE 堆栈。

14. **`[Content]` 按"实例类型"路由（崩溃级，2026-09-28 修复）** — 与坑 3 同源，但这次漏了 `PageComp`。上游 `[ContentProperty("SearchTags")]`，移植版改成属性级 `[Content]`：
    - WPF 按**根标签类型**路由：`PageComp.xaml` 的根是 `<local:MyScrollViewer>`，其子元素走 `MyScrollViewer.Content`，不受影响；子类 `<local:PageComp>…</local:PageComp>` 的子元素才进 `SearchTags`。
    - Avalonia 按**实例类型**路由：`[Content]` 一挂，PageComp **自身**根子元素（StackPanel）也被塞进 `SearchTags` → 填充期访问未初始化的 `ComboSearchTag` → NRE（`PageComp.axaml:12`）。
    - **修法**：删掉 `[Content]`，并让 6 个子类 XAML 显式写 `<local:PageComp.SearchTags>…</local:PageComp.SearchTags>`。脚本：`tools/fix-pagecomp-content.py`。
    - 全仓 `[Content]` 仅 3 处，另两处（`AnimationGroup.Children`、`CustomEventCollection.Events`）getter 非 null，安全。

15. **AvaloniaProperty 字段必须对 XAML 生成代码可见（崩溃级，2026-09-28 修复）** — 上游 `private static readonly DependencyProperty` 没问题，因为 WPF 的 XAML 走 CLR 属性 setter；**Avalonia 的编译期 XAML 会直接访问 `XxxProperty` 字段**，跨类访问 private → 运行期 `FieldAccessException`（不是编译错误！）。
    - 实测受害：`MyPageRight.PanScrollProperty`（`PageSelectRight` 实例化即崩 → 点"实例选择"后程序无响应）、`MyImage.CornerRadiusProperty`（`PageHomepageNewsView`/`PageSetupAbout`）。
    - **修法**：提升为 `public`。**扫描命令**：找非 public 的 `*Property` 字段（排除 getter 非 null 的实例字段）。

16. **非 UI 线程取本地化 → `!key!`（2026-09-28 修复）** — `Lang.Text` 的两条查找路径都经 Avalonia 资源宿主（`Application.Resources`/`ActualThemeVariant`，仅 UI 线程）；加载/启动线程上 `ResourceExtensions` 直接返回 null、`_LifecycleSafeFindResource` 吞掉 VerifyAccess，于是 DEBUG 下退化成 `!key!`（表现为"当前步骤: !Minecraft.Launch.Stage.WaitWindow!"、"启动成功"提示变成 `!Minecraft.Launch.Success!`）。
    - **修法**：`LocalizationService` 额外维护一份线程安全的 `ConcurrentDictionary<string,string>` 文本快照（语言字典是纯 XML 键值对，跨线程读安全），`Lang.Text` 最后兜底查它。
    - 注意：**语言键在 zh-CN.xaml 里是存在的**，别去查语言文件——要查的是"为什么查不到"。

17. **scRGB ≠ sRGB（整站配色偏暗，2026-09-28 修复）** — 两处色彩空间误用，合起来让所有主题色比上游暗一截：
    - `ScRgbCompat.FromScRgb` 把入参当 sRGB 分数直接 `v*255`。但 WPF `Color.FromScRgb` 的入参是 **scRGB 线性值**，须先过"线性→sRGB"传递函数再量化。
    - `LabColor.FromWpfColor` 传 `(double)color.R/255`，命中的是 `FromRgb(double…)` 重载 = `ColourSpace.RgbLinear`，等于把 sRGB 数值当线性用。上游传的是 `color.ScR`（真线性）。改用语义明确的**字节重载** `FromRgb(byte…)`。
    - **实测**：标题栏 `ColorBrush3` 从 `#0637C3` → `#2A80E3`；离屏渲染主界面采样内容区 `#0A1225`、侧栏 `#27282C`，与上游窗口的 `#0A1225`/`#27282E` 一致。`Color → LabColor → Color` 往返现在 Δ=0。
    - **判据**：如果你看到某个颜色 ≈ 原版颜色的"线性化"结果（明显更暗），就是这个问题。

18. **`avares://` 大小写敏感 + 不能用 FileStream 读（2026-09-28 修复）** — Avalonia 按清单资源名**精确匹配**，Windows 文件系统不区分大小写但资源名区分：
    - 20 处 `avares://PCL/images/...` 而目录是 `Images/` → `FileNotFoundException`。脚本：`tools/check-avares-case.py`（可 `--fix`）。
    - `ModBase.pathImage` 仍是 `pack://application:,,,/Plain Craft Launcher 2;component/Images/`，且 `MyBitmap` 对它用 `new FileStream(...)` → 必然失败。已改为 `avares://PCL/Images/` + 经 `AssetLoader.Open` 取流（实例方块图标因此恢复正常）。
    - `PageSetupAbout`/`PageSetupUpdate`/`MinecraftServer` 还有 12 处 WPF pack 语法残留，已转 `avares://PCL/...`。
    - **已知上游缺口（未改）**：`PageSetupUpdate.axaml:150` 引用 `Images/Icons/Tahoe.png`，**上游仓库里也不存在**（两版 Images 均为 47 个文件）。WPF 对缺失图片静默不渲染，Avalonia 直接抛异常 → "设置 → 更新"页当前实例化失败。属上游数据缺口，改文案会偏离上游，留给用户定夺。
    - **补充（2026-09-30）**：历史配置兼容。旧版本把 `pathImage` 存成 pack URI 后写进了实例配置（`versions/<实例>/PCL/config.v1.yml` 的 `Logo`），改成 avares 后这些旧值不再匹配资源前缀 → `MyBitmap` 用 FileStream 打开 pack URI → IOException 从 `MyListItem.UpdateLogo` 的属性变更回调逃逸 → **打开实例列表即崩**（用户报的"加载实例列表失败 + 崩溃"）。修法：`MyBitmap._NormalizeImagePath` 归一化已知 WPF pack 前缀。

19. **同一事件上的多个 Tunnel 处理器是「逆序」调用（点击失效，2026-09-30 修复）** — Avalonia 对同一 `RoutedEvent` + 同一 `RoutingStrategies` 的多个处理器，**后注册的先执行**（实测：注册顺序 1→2，调用顺序 2→1）；WPF 是按注册顺序。
    - 受害点：`MyListItem` 把「点击处理」`Button_PointerReleased` 与「复位」`Button_PointerExited` 都挂在 `PointerReleasedEvent` 的 Tunnel 上（上游 WPF 依赖"点击先于复位"）。Avalonia 下复位先跑 → `isMouseDown` 被清零 → 点击处理器直接 `return` → **实例设置页左侧所有列表项全部点不动**，日志里连"按下单选列表项"都不会出现。
    - **修法**：复位改为 `Dispatcher.UIThread.Post` 异步执行，保证点击处理一定先完成。
    - **排查命令**：`grep -rn "RoutingStrategies.Tunnel" --include=*.cs` — 检查同一事件上是否有多个 Tunnel 处理器且存在顺序依赖。

20. **WPF `IsMouseDirectlyOver` 无 Avalonia 等价物（标题栏按钮全部点不动，2026-09-30 修复）** — 上游 `FormDragMove` 用 `IsMouseDirectlyOver`（**仅元素自身**，不含子元素）决定是否 `DragMove()`。移植版换成了 `IsPointerOver`（**包含子元素**），于是点击标题栏内的选项卡/最小化/关闭/返回按钮时也会命中拖动分支，`BeginMoveDrag` **捕获指针** → 按钮收不到 `PointerReleased` → 点击全部失效（只有按下/悬停动画）。
    - **修法**：改为 `ReferenceEquals(e.Source, sender)`，还原"直接在标题栏上"的语义。

21. **WPF `TextBlock.Text` 默认 `string.Empty`，Avalonia 默认 `null`（崩溃级，2026-09-30 修复）** — 上游 `ModStyle.SetColorfulTextLab` 只判 `lab is null`，不判 `text`；调用方会直接传 `LabInfo.Text`（该文本可能从未赋值）。Avalonia 下传入 null → `foreach (var c in text)` NRE → **"加载 Mod 列表 UI 失败"**。
    - **修法**：`text ??= string.Empty;`。**排查方向**：凡是把 `.Text` 当非空字符串用的地方都要留意。

22. **WPF `TextBlock.Padding` 可以超过可用尺寸，Avalonia 会把文字压没（按钮文字空白，2026-09-30 修复）** — 上游 `BtnMore` 是 `Height="35"` 配 `TextPadding="36"`（`TextPadding` 直接映射到 `LabText.Padding`）。WPF 下文字仍居中可见；Avalonia 下内容区高度被压成 0，**文字完全不绘制** → "实例设置按钮文字空白"。
    - **修法**：`MyButton.ApplyTextPadding()` 按控件实际可用尺寸收敛内边距（至少给文字留 1.5 倍字号），并在 `SizeChanged` 时重算。
    - **验证手法**：打印 `请求内边距 / 实际内边距 / 文字区尺寸` 即可确认（实测收敛为 `36,7.75,36,7.75`，文字区 124x32）。

23. **滚轮量纲不同（滚动异常缓慢，2026-09-30 修复）** — WPF `MouseWheelEventArgs.Delta` 每格 **±120**；Avalonia `PointerWheelEventArgs.Delta` 每格 **±1**。上游 `PerformVerticalOffsetDelta(-e.Delta)` 直接照用会让滚动距离只有 1/120。
    - **修法**：`MyScrollViewer` 中换算回 WPF 量纲（`* 120`），`DeltaMult` 语义不变。

24. **`Template.FindName` 垫片不可靠（崩溃级，2026-09-30 修复）** — `Stubs.cs` 里的垫片是 `control.FindNameScope()?.Find(name)`；模板刚应用时可能落到外层（页面）名称作用域，取不到模板内元素并**静默返回 null**。
    - 受害点：`MyTextBox.labHint` 恒为 null → `OnApplyTemplate` 里 `labHint.Text` NRE，且 `HintText` 提示文本永不显示。
    - **修法**：在 `OnApplyTemplate` 里用 `e.NameScope.Find<T>(...)` 取部件并缓存（与 `MyScrollViewer` 取 `PART_VerticalScrollBar` 的写法一致），惰性 getter 仅作兜底。

---

25. **应用没有引入 Avalonia 主题，模板全靠手写（漏一个控件就整块空白，2026-10-01 修复）** — `Application.axaml` 里手写了 MyScrollBar / MyScrollViewer / MyTextBox / MyComboBox / ToggleButton / ComboBoxItem / ContextMenu / MyMenuItem / Calendar 等模板，但**漏了 `ItemsControl`**。`ItemsControl` 是 `TemplatedControl`，没有模板就不会实例化 `ItemsPresenter` → 一个子项都不渲染。
    - 实测：档案列表 `ItemCount=4`，但 `ItemsControl` 的**视觉子树为 0**，界面完全空白（对应"选择档案界面一个账号都没有"）。
    - **修法**：补上等价模板 `<Style Selector="ItemsControl">` + `<ControlTemplate><ItemsPresenter Name="PART_ItemsPresenter" ItemsPanel="{TemplateBinding ItemsPanel}" /></ControlTemplate>`（`ItemsPanel` 有内置默认值 StackPanel，无需再设）。
    - **排查提示**：凡是"数据有、界面空"的列表，先确认对应控件模板是否存在于 `Application.axaml`。

26. **`Grid` 的 `*` 行在"可用高度无限"时 Avalonia 算 0，WPF 按内容撑开（2026-10-01）** — `PageLoginProfile` 的滚动列表行是 `*`，而它被放进 `PageLaunchLeft.PanLogin`（`VerticalAlignment="Center"`，按内容自适应高度）→ 无约束 → `*` 塌成 0 → `MyScrollViewer` 高 0 → 列表不渲染。已把该行改为 `Auto`（该页宿主本就按内容自适应，`*` 无实际作用）。
    - **判据**：页面高度恰好等于"固定行之和"、`*` 行内容不可见，就是这个坑。

27. **`BoxShadow` 是空格分隔，逗号只分隔"多个阴影"（崩溃级，2026-10-01 修复）** — `Application.axaml` 写成 `BoxShadow="0 2 4 0, #66000000"`，被当成两个阴影；第一个 `0 2 4 0` 只有 4 个 token，按 `offsetX offsetY blur color` 解析时把末位 `0` 当颜色 → `FormatException: Invalid color string: '0'`。
    - 该模板在**首次弹出 ContextMenu** 时实例化 → "右键菜单一出现就崩溃"。
    - 正确写法：`offsetX offsetY [blur] [spread] color`（3~6 个 token，空格分隔），如 `0 2 4 0 #66000000`。

28. **`(int)converter.Result` 在结果为 null 时抛 NRE（崩溃级，2026-10-01 修复）** — `ModMain.MyMsgBox` / `MyMsgBoxMarkdown` 直接 `return (int)converter.Result;`。`Result` 是 `object`，当弹窗没能产生结果时（关闭流程中 `PushFrame` 直接返回、后台线程投递失败）为 null，`(int)(object)null` 抛 `NullReferenceException` → FTL → 强制关闭。改为 `converter.Result is int r ? r : 0`。（`MyMsgBoxSelect` 用 `(int?)`、`MyMsgBoxInput` 用 `?.ToString()`，天然安全。）
    - **排查提示**：日志里 `普通弹框返回：null` 紧跟 FTL，就是这个。

29. **`DispatcherFrame` 构造函数会 `VerifyAccess`（跨线程弹窗必崩，2026-10-01 修复）** — `MyMsgBoxConverter.WaitFrame = new(true)` 是**字段初始化器**。WPF 的 `DispatcherFrame` 绑定"当前线程"的 Dispatcher、构造不校验；Avalonia 的会执行 `Dispatcher.VerifyAccess()` → 任何**后台线程**调用 `MyMsgBox`（MS 登录、下载报错）都在 `new MyMsgBoxConverter` 这一步就崩。
    - 另：后台线程调用时原实现也不会执行 `MyMsgBoxTick()`（弹窗永不显示），且 `Dispatcher.UIThread.PushFrame` 本身就要求 UI 线程。
    - **修法**：`WaitFrame` 改为惰性创建；新增 `PumpMsgBoxFrame(converter)`，非 UI 线程时把"创建弹窗 + 泵送模态帧"整体投递到 UI 线程（`RunInUiWait`），调用线程阻塞等待弹窗关闭。

30. **`MyComboBox` / `MyMenuItem` 的模板部件同样取不到（2026-10-01 修复，坑 24 的扩展）** — 都走 `Template.FindName` 垫片。`MyComboBox.OnApplyTemplate` 的 NRE 会以 Feedback 级日志弹出消息框 → 连带触发弹窗系统 → 表现为"点设置选项卡后整个 UI 冻结"。统一改为缓存 `e.NameScope` 并从中取部件。

31. **`MyTextBox.GetValidateResult` 给 FluentValidation 传 null 模型（崩溃级，2026-10-01 修复，坑 21 的扩展）** — `rule.Validate(Text)`，`Text` 为 null（Avalonia 的 `TextBox.Text` 默认 null）→ `Cannot pass a null model to Validate`。**点下载选项卡约 1 秒后崩溃**就是这条（`PageDownloadInstall.Init()` 设置 `ValidateRules`）。改为 `Text ?? string.Empty`。

32. **`ElementName` 指向"根元素 x:Name"的绑定取不到值（2026-10-01 修复，坑 12 的扩展）** — `MyExtraTextButton` 用 `CornerRadius="{Binding Bounds.Height, ..., ElementName=PanBack, Mode=OneWay}"`；`ElementName` 指向根元素 `x:Name` 在 Avalonia 下解析不到 → 转换器拿到非数值返回 0 → **按钮变方角**。`$parent[local:类型]` 与 `ReflectionBinding` 实测也无效。该模板高度是固定值 `Height="52"`，上游 `ActualHeight * 0.4` 恒为 `20.8`，故直接写常量（渲染结果与上游一致）。**排查提示**：凡 `ElementName=` 指向本文件根元素 `x:Name` 的绑定都要怀疑。

33. **非 Windows 下调用 Win32 置顶 API 会让启动永久卡死（2026-10-04 修复，commit `6f7aaa3`）** — `FormMain.ShowWindowToTop()` 直接调 `ModMain.SetForegroundWindow(ModBase.frmHandle)`（P/Invoke `user32.dll`）。Linux 上该调用**不抛异常、直接挂住 UI 线程**，表现为"程序启动后卡在 4 秒处、日志停止、窗口空白"。
    - **修法**：`if (!OperatingSystem.IsWindows()) Activate(); else ModMain.SetForegroundWindow(...)`。
    - **教训**：**所有 P/Invoke 在非 Windows 上都可能是"静默挂起"而不是抛异常**。排查启动卡死时优先怀疑它们。

34. **上游 `Window.Background="{x:Null}"` 在 Linux 上是"真透明"（2026-10-04 修复，commit `4909bf3`）** — 上游这是配合 WPF `AllowsTransparency="False"` 用的，那时窗口底由系统提供。Avalonia 下 `Background=null` 意味着客户区**真的不画底**，Linux（无 alpha 合成）上整个窗口变成透明一片。
    - **修法**：构造函数里给非 Windows 补一个跟随主题的不透明背景。

35. **配置文件密钥用 DPAPI 会让 Linux 启动失败（2026-10-04 修复，commit `8187b4d`/`7c316e2`）** — DPAPI 是 Windows 专属。非 Windows 下改为明文存储密钥。

36. **⚠️ 排查方法论：别急着下结论，尤其是"内存泄漏"（2026-10-04 血泪）** — 这轮排查中我**连续误判了三次**病因：
    - 先以为是**动画泄漏**（`aniGroups` 无限增长）→ 加探针 → 排除
    - 又以为是**内存泄漏** → 加探针 → 排除
    - 再以为是 **`/proc/meminfo` 被死循环读** → 加探针 → 发现那是 .NET GC 在 Linux 上的正常行为，**是结果不是原因**
    - 最后才定位到：**UI 线程卡住 → 动画在嵌套消息泵里空转 → 才导致 CPU/内存暴涨**
    **可复用的排查手法（按顺序）**：
    1. **`strace -f -p <pid> -e trace=openat,read,write,futex`** —— 系统调用几乎为零 = 纯用户态空转，不是 I/O 问题
    2. **`/proc/<pid>/task/*/stat` 排序找最忙线程**，比对 `tid == pid` 判断是不是主线程
    3. **在可疑位置打 `Environment.StackTrace`** —— 拿到调用栈就能一击定位（本轮靠它找到 `PumpMsgBoxFrame`）
    4. **用顺序探针（`[P1]`~`[P9]`）二分定位卡点**，比读代码猜快得多
    5. **⚠️ 诊断输出必须直接写文件**（`File.AppendAllText`）。stdout 重定向到文件 + `SIGKILL` 会**丢缓冲**，
       表现为"探针明明执行了却没输出" —— 这个坑让我白跑了一整轮（以为探针没执行，其实是被 kill 时丢了）
    6. **对照实验**：先用一个已知正常的 X 程序（如 `xclock`）验证截图链路本身没问题，
       再怀疑被测程序 —— 否则会把"环境问题"和"程序问题"混在一起
    7. **环境变量要对**：Xvfb 里不装合成器（`xcompmgr`）时窗口全黑，会把"缺合成器"误判成根因

37. **Git Bash 下 `tar -f D:/path` 会把路径当远程主机（2026-10-04）** — 报 `Cannot connect to D: resolve failed`。
    改用 `/d/a/j/...` 形式，或加 `--force-local`。

38. **shell 会解释反引号（2026-10-04）** — 用 `python3 -c "...`反引号`..."` 写含 Markdown 反引号的文本时，
    shell 会先做命令替换导致语法错误。**含反引号/特殊字符的文本一律用 Write 工具写文件**，不要走 `python3 -c`。

## 8. 暂缓功能清单（用户指定，文件保留在仓库）

`DeferredExcludes`（见 §5.1）：崩溃分析(CrashAnalysis)、音乐(ModMusic)、视频背景(ModVideoBack)、联机(ModLink)、换皮肤(ModSkin)、工具页(PageTools)、剪贴板行为(ClipboardInterceptor)、LazyLoadBehavior。
另：Markdown 公告（Markdown.axaml/MarkdownViewer 已降级为 SelectableTextBlock，MyMsgMarkdown 已恢复编译）。
MySkin 和 MyMsgMarkdown **已重新启用**（不在排除清单里）。

---

## 9. 下一步任务（优先级排序）

> ⚠️ **本节的 1~4 项已在 2026-10-01 ~ 10-05 完成**（Linux 冒烟、真机测试都做了），
> **当前唯一阻塞是 §0.3 的 Linux 全黑窗口**。下面保留历史记录供追溯。

0. **🔴 当前最优先：修 Linux 全黑窗口** —— 见 §0.3。修完后清理 §0.4 的临时诊断开关，
   再跑 §12 的 `--pagetest` 做回归。
1. ~~验证透明窗口问题~~（✅ 2026-09-28 已闭环）
2. ~~用户报告的一批缺陷~~（✅ 2026-09-28 已修）— 实例设置崩溃、实例选择无响应、本地化 `!key!`、整站配色偏暗、内嵌图片加载。详见 §7 坑 13~18 与 §12。
3. ~~WSL headless 冒烟~~（✅ 已完成）— **注意**：本机 WSL 当时被安全策略拉黑，但后来恢复了，现在 WSL 可用（见 §0.5）。
   - 单文件：`dotnet publish ... -o dist/linux-x64` → `dist/linux-x64/PCL`（ELF x86-64，120MB）
   - 非单文件：追加 `-p:PublishSingleFile=false -o dist/linux-x64-flat`
   - **注意**：不要用 `-p:OutputType=Exe` 全局覆盖，会把 `PCL.Core.SourceGenerators` 也改成 Exe → `CS5001 程序不包含入口点`
4. ~~Linux 真机测试~~（✅ 已完成，2026-10-04）— 已接入用户 VMware VM（SSH 见 §0.5），
   复现并定位了全黑窗口问题
5. **暂缓功能移植**（用户确认优先级后）— PageTools（工具页）大概率是下一个大块
6. ~~清理~~（✅ 2026-10-01 已完成）

## 10. 待用户确认的问题（攒批问）

1. B 类偏差（有 [port] 注释的语义近似）是否接受？
2. C 类 Linux 降级（如 HLSL 着色器模糊跳过、SystemDialogs 异步化）清单是否接受？
3. FEATURES.md 的 P1/P2/P3 优先级是否认可？
4. ~~`PCL.Neo.Core`/`PCL.Neo.UI` 两个废弃骨架是否删除？~~ → ✅ 已删除（2026-10-01）
5. ~~透明窗口现状~~ → ✅ 已确认正常（2026-09-28）

## 11. 工作方法约定（延续）

- 逻辑零改动；事件签名与 XAML 对齐（先改 XAML 事件名，再改 C# 签名）。
- 共享文件（`Stubs.cs`、`Application.axaml`、`ControlResourceHelper.cs`）多代理都改 → 编辑前先 read 检查现状。
- 子代理并行分片时：每个只改自己负责的文件，构建由主会话统一跑；子代理提示里必须写明铁律。
- 新增桩成员先在 Stubs.cs 里 grep 确认不重复。
- commit 信息用英文短句，一次修复一个 commit。
- **遇到"页面能编译但一打开就崩"时，先跑 `PCL.exe --pagetest`**（见 §12），不要靠猜。

---

## 12. 移植期自检工具（`--pagetest`，2026-09-28 新增）

**用法**：`PCL.exe --pagetest`（正常启动不受影响；逻辑在 `FormMain.RunPageInstantiationSelfTest`）。

在 UI 线程上依次执行并把结果写入运行日志（`[PageTest]` 前缀）：

| # | 检查项 | 作用 |
|---|---|---|
| 1 | 直接构造各页面 Left/Right 控件 | 抓 XAML 填充期异常（命名字段为 null、集合属性填充、字段可见性） |
| 2 | `Left.PageGet(subType)` 全子类型 | 覆盖惰性创建的右侧页面 |
| 3 | 真实 `PageChange(pageType)` 导航 | 复现用户点击（含 `PageChangeActual` 全链路） |
| 4 | 后台线程 `Lang.Text` | 回归 `!key!`（坑 16） |
| 5 | 7 个颜色 `Color→LabColor→Color` 往返 | 回归 scRGB（坑 17），要求 Δ≤1 |
| 6 | `MyBitmap` 加载内嵌图片 | 回归 avares 取流（坑 18） |
| 7 | 打印主题状态与关键画刷 | 排查配色 |
| 8 | 渲染主界面到 `PCL/Log/ui-render.png` | **目视/像素比对**（无桌面环境时尤其有用） |
| 9 | 列表项**逻辑链路**：直接 `SetChecked` | 区分"处理器链路坏"还是"输入投递坏" |
| 10 | 列表项**输入链路**：合成 `PointerPressed`+`PointerReleased` 投递 | **回归坑 19**（Tunnel 处理器逆序）。修复前 pageID 不变，修复后切换成功 |
| 11 | 视觉切换检查（`PanMainRight.Child`） | `SetChecked` 只改 pageID，内容替换在延时动画里 |
| 12 | 按钮文字度量（`BtnMore`） | **回归坑 22**（内边距收敛 + 文字区尺寸） |

⚠️ **不要用 `InputHitTest` 判断控件可点性**：实测它对 `PanForm` / `PanTitleMain` 等子树会误报"不可达"（连能正常点击的启动页按钮也报不可达），与本应用真实指针投递结果不一致。判断可点性请用第 10 项的**合成事件**方式。

**当前基线（2026-09-28，commit 9e00ef6）**：59 项通过 / 2 项失败，两项均为已知上游缺口：
- `PageSetupLeft.PageGet(3)` = `SetupLink`（联机页，暂缓功能，未实现）
- `PageSetupLeft.PageGet(8)` = `SetupUpdate`（引用上游也不存在的 `Tahoe.png`，见坑 18）

**注意**：`PageSubType` 各段（Download*/Setup*/Version*）**数值重叠**，`Enum.ToString()` 可能显示同值的其它段名字——日志里同时打印了数值以便核对。

**另：`--logintest`** —— 单独开关，启动后用当前配置的 `PCL_MS_CLIENT_ID` 真跑一遍正版登录（构造 `new McLoginMs()` 启动 `mcLoginMsLoader`），并打印读到的客户端 ID、加载器状态与 FTL/ERR 计数。用于验证 Azure 应用是否配置正确。注意它会**自动打开浏览器**到 `microsoft.com/link`。已验证：设备码请求返回 200、解析出授权页、无异常。

**配套脚本**（都在 `tools/`）：
- `fix-buttons-collection.py` — 坑 13（x:Array → sys:List）
- `fix-pagecomp-content.py` — 坑 14（PageComp.SearchTags 显式包裹）
- `check-avares-case.py` — 坑 18（avares 路径大小写，`--fix` 可写回）
- `capture-window.py` — 采样活动窗口像素（注意：沙箱进程的桌面与应用不同，普通截屏看不到应用窗口；优先用第 8 项自渲染）

---

*本文档由上一任代理在交接时生成。若与 git 历史冲突，以 git 为准。*
