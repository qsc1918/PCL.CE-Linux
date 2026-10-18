# AGENTS.md — PCL-CE → Avalonia/Linux 移植项目

> 本文件是新 AI 代理的**第一入口**。完整交接文档：**`docs/HANDOVER.md`**（必读，§0 是最新状态，§7 是全部踩坑记录）。
>
> **文档索引**：
> | 文档 | 内容 |
> |---|---|
> | `docs/HANDOVER.md` | **主交接文档**（§0 最新状态与唯一阻塞 / §7 踩坑 1~38 / §12 自检工具） |
> | `docs/LINUX-BLANK-WINDOW.md` | **当前阻塞的专项排查档案**（含真机截图证据，细节最全） |
> | `docs/av12-notes.txt` | Avalonia 12 实测 API 笔记（权威，比 A11 指南新） |
> | `docs/FEATURES.md` | 功能清单与 P1/P2/P3 优先级 |
> | `docs/PORTING-MAPPING.md` | 转换规则表（**GBK 编码**，读取需 `Encoding.GetEncoding(936)`） |
> | `docs/INTEGRATION_PLAN-historical.md` | ⚠️ 已废弃的早期方案（仅代码库对比仍有参考价值） |
> | `PORTING_PLAN.md` | 早期评估报告（A/B/C 分类） |
> | `tools/linux-debug/README.md` | **真机与 Xvfb 调试脚本 + 环境踩坑**（要测 Linux 先看这个） |
> | `docs/img/` | 真机截图证据（全黑 vs 正常渲染） |

## 项目一句话

把 PCL-CE（WPF Minecraft 启动器）**逐字移植**到 Linux 的 Avalonia 12 / .NET 10 —— 只做 API 映射，**绝不重写/重设计 UI**。

## 铁律（用户原话要求，违反即失败）

用户："记住目标是移植pcl ce的代码并不是重写，所以说你也要时时刻刻记着不要重写ui。要保留原有的功能逻辑和风格，这样子的话才是pcl ce启动器，而不是一个披着pcl名字重写的启动器。"

1. **只做 WPF→Avalonia 的 API 名称/签名映射**；布局/颜色/文案/交互与上游一致。
   **上游基线获取方式**（重要：原开发机上的 `D:\a\j\PCL-CE` 你拿不到，请自己克隆）：
   ```bash
   git clone https://github.com/qsc1918/PCL-CE.git
   cd PCL-CE && git checkout eaed9d80adfa67bb6cbdeac0b9ea72de726bcadc
   ```
   固定版本：**分支 `dev`，commit `eaed9d80`**（2026-08-24，`feat(i18n-banner): ... (#3560)`）。
   本仓库所有移植决策都以这个版本为对照基准 —— **请务必 checkout 到该 commit**，不要用 `dev` 最新。
2. 所有非纯映射的改动必须加 `// [port] <原因>` 中文注释。
3. 编译错误只信完整构建：`dotnet build "src\Plain Craft Launcher 2\Plain Craft Launcher 2.csproj" -v q -nologo --no-incremental`。增量构建和子代理自报的"0 错误"会掩盖残留错误。
4. **禁止用 pwsh 批量重写 .cs 文件**（曾把 `public` 写成 `uublic`）；C# 编辑用 read+edit 工具。
5. 频繁 commit；给用户的问题攒到最后批量问。
6. 工具约束：pwsh 读中文日志用 `[System.Text.Encoding]::GetEncoding(936)`（GBK）；`[System.IO.File]` 用绝对路径；.ps1 脚本纯 ASCII。

## 当前状态（2026-10-18）

**一句话**：编译 0 错误、Windows 运行正常、**Linux 能启动且主窗口完整渲染**（原唯一阻塞已修复）。

- ✅ 编译 0 错误（C# 从 10564 清零，XAML 从 792 清零）
- ✅ Windows 运行稳定，`--pagetest` 自检 **59 通过 / 2 失败**（2 项为已知上游缺口），0 FTL / 0 ERR
- ✅ **Linux 完全可用**（2026-10-18 修复，commit `0489c36`）：
  窗口 900x550、标题 `PCL CE Linux`、`Opacity=1`、**48 秒内存恒定 ~260MB**、
  CPU 52%→21.8%、0 异常，界面完整渲染（截图 `docs/img/linux-window-renders-fixed.png`）
- ✅ **原阻塞「全黑窗口 + CPU 120% + 内存 200MB/s 暴涨」根因已查明**：
  字体链在 Linux 上解析失败 → 中文排版产出零长度文本行 → 卡死
  `TextLayout.CreateTextLines()` → UI 线程永不回消息循环 → 动画跑不了 → `Opacity` 停在 0。
  **详见 `docs/LINUX-BLANK-WINDOW.md` 顶部的「⭐ 最终结论」**（该文档前半部分是症状链/误判，别照着改）
- ⚠️ **仍未修**：账号头像不显示、启动页元素重合、启动按钮偶发卡在"正在加载"、实例选择文件夹列表左键
- ⚠️ **正版登录**需要 `PCL_MS_CLIENT_ID` 环境变量（Debug）或构建期密钥库（Release）；仓库不含该密钥，属设计如此
- ❌ 暂缓功能（用户指定 8 项）：CrashAnalysis / ModMusic / ModVideoBack / ModLink / ModSkin / PageTools / ClipboardInterceptor / LazyLoadBehavior

## 下一步（按序）

1. **Linux 功能回归**：窗口已能渲染且稳定，接下来逐项验证基础功能
   （版本列表加载、下载、启动游戏、设置页、切换标签不崩）。
2. **Windows 回归**已做（内存 ~335MB 稳定、CPU 1~4%），改动未破坏 Windows。
3. 修上面「仍未修」的 4 项 Windows/Linux 通用缺陷。
4. 暂缓功能移植（优先级问用户，PageTools 大概率优先）。
5. **字体相关注意**：`LocalizationFontService` 现在按平台分支，
   若以后加自定义字体（FontSelector）务必保证链上至少有一个**当前平台真实存在**的中文能力字体，
   否则会重新触发 Avalonia 的排版死循环（见 `docs/LINUX-BLANK-WINDOW.md`）。
2. 继续排查：账号头像、启动页元素重合、启动按钮状态、文件夹列表左键
3. 暂缓功能移植（优先级问用户，PageTools 大概率优先）

## Linux 调试环境（原开发机专属信息，务必先看）

**真机是用户的 VMware Linux VM**，通过 SSH 访问（从 WSL 连，`172.22.192.1` 是 Windows 宿主）：

```bash
H=172.22.192.1
ssh abc@$H -p 62222        # 密码 abc
# ⚠️ sudo 密码也是 abc（不是 root！root 用户的密码才是 root）
```

WSL 里已装 `sshpass`；真机已装 `imagemagick`（`import`）+ `x11-utils`（`xwininfo`）。

**完整流程、脚本与踩坑见 `tools/linux-debug/README.md`**，要点：
- 运行前必须 `export XAUTHORITY=$(ls /run/user/1000/.mutter-Xwaylandauth.* | head -1)`，否则连不上 X
- 截图要抓**内层窗口**（外层是 mutter 装饰框架，928x616）；判据：`convert ... -format "%[colors]"` **=1 就是全黑**
- **诊断输出必须直接写文件**，不要靠 stdout（重定向到文件 + SIGKILL 会丢缓冲，这个坑白跑过一整轮）

## 快速排雷（Avalonia ↔ WPF 语义差异，HANDOVER §7 有完整清单）

| 症状 | 根因 |
|---|---|
| **数据有、界面空**（列表一项都不显示） | 控件模板缺失（本应用无主题，模板全靠手写）→ 坑 25 |
| 控件可见但**点击无反应** | 同一事件多个 Tunnel 处理器**逆序**执行 → 坑 19 |
| 标题栏/弹窗按钮点不动、有动画 | `IsMouseDirectlyOver` → 误用 `IsPointerOver`，拖动抢指针 → 坑 20 |
| 弹窗点了没结果就崩 | `(int)object?` 强转 null → 坑 28；跨线程 `DispatcherFrame` → 坑 29 |
| 右键菜单一出现就崩 | `BoxShadow` 用了逗号分隔 → 坑 27 |
| 切某页即崩 | 该页 `Loaded` 里的输入验证/模板部件 → 坑 30 / 31 |
| 文字空白/被压没 | `TextBlock.Padding` 超过可用尺寸 → 坑 22 |
| 按钮变方角 | `ElementName` 指向根 `x:Name` 的绑定失效 → 坑 32 |
| 页面能编译、一打开就崩 | 根元素 `x:Name`（坑 12）、集合属性缺 `<x:Array>` 等价物（坑 13）、`[Content]` 路由（坑 14） |
| 运行期 `FieldAccessException` | `*Property` 字段非 public（坑 15） |
| 显示 `!key!` | 非 UI 线程取本地化（坑 16） |
| 整站配色偏暗 | scRGB 当 sRGB 用（坑 17） |
| 图片加载失败 | `avares://` 大小写、pack URI 残留、旧配置路径（坑 18） |
| 崩溃在某 `.Text` 上 | WPF `TextBlock.Text`/`TextBox.Text` 默认 `""`，Avalonia 默认 `null`（坑 21 / 31） |
| 滚动异常慢 | 滚轮量纲 ±120 vs ±1（坑 23） |
| 模板部件取到 null | `Template.FindName` 垫片不可靠，改用 `e.NameScope`（坑 24 / 30） |

**遇到"能编译但一打开/一点就崩或没反应"时，先跑 `PCL.exe --pagetest`**（HANDOVER §12），不要靠猜。

## 高频坑速查（详见 HANDOVER.md §7）

- 资源画刷是 `ImmutableSolidColorBrush` → 用 `ISolidColorBrush`/`IBrush`，不能强转
- `dynamic` 不能绑定扩展方法（动画每帧崩的根因）
- Avalonia 不为 ColumnDefinition/RowDefinition/Transform/Effect/几何对象生成 x:Name 字段
- keyed Style/DataTemplate 进 `Resources`，选择器样式进 `Styles`
- 语言 xaml 必须 Exclude 出 AvaloniaXaml + EmbeddedResource 三重打包（否则 AssetLoader 找不到）
- `EnableDefaultItems` 主工程必须 true（否则命名字段生成器静默失效），PCL.Core 必须 false
- **根元素 `x:Name` 语义与 WPF 不同**：WPF 绑到控件自身，Avalonia 生成独立字段且在名字作用域建立前赋值 → 恒 null。在 code-behind 用 `=> this` 属性替代，axaml 内绑定改 `RelativeSource AncestorType=local:XXX`（详见 HANDOVER §7 坑 12）
- **Avalonia 没有 `x:Array`**：集合属性要用 `<sys:List x:TypeArguments="local:T">`（`xmlns:sys` 的 assembly **必须是 `netstandard`**），否则填充期 NRE（坑 13）
- **`[Content]` 按实例类型路由**：根子元素会被误塞进内容属性 → 填充期 NRE；子类需显式写 `<local:X.Prop>`（坑 14）
- **`*Property` 字段必须 public**：Avalonia 编译期 XAML 直接访问字段，private → 运行期 `FieldAccessException`（坑 15）
- **非 UI 线程取不到本地化**：`Lang.Text` 会退化成 `!key!`；已加线程安全快照兜底（坑 16）
- **scRGB 是线性空间**：`FromScRgb` 须过"线性→sRGB"传递函数；sRGB 数值不能当线性用（坑 17）
- **`avares://` 大小写敏感**，且不能用 `FileStream` 打开，须经 `AssetLoader.Open`（坑 18）
- 模板必需部件：TextBox 要 `PART_TextPresenter`，ScrollBar 要 `PART_LineUp/DownButton`、`PART_PageUp/DownButton`
- UI 对象只能 UI 线程碰（ResourceExtensions 已做线程安全缓存）
- **页面"能编译但一打开就崩"时，先跑 `PCL.exe --pagetest`**（HANDOVER §12），不要靠猜
