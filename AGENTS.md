# AGENTS.md — PCL-CE → Avalonia/Linux 移植项目

> 本文件是新 AI 代理的**第一入口**。完整交接文档：**`docs/HANDOVER.md`**（必读，含全部踩坑记录与文件清单）。
> Avalonia 12 实测 API 笔记（权威，比 A11 指南新）：`docs/av12-notes.txt`。早期评估报告（A/B/C 分类）：`PORTING_PLAN.md`。功能清单：`docs/FEATURES.md`。

## 项目一句话

把 PCL-CE（WPF Minecraft 启动器）**逐字移植**到 Linux 的 Avalonia 12 / .NET 10 —— 只做 API 映射，**绝不重写/重设计 UI**。

## 铁律（用户原话要求，违反即失败）

用户："记住目标是移植pcl ce的代码并不是重写，所以说你也要时时刻刻记着不要重写ui。要保留原有的功能逻辑和风格，这样子的话才是pcl ce启动器，而不是一个披着pcl名字重写的启动器。"

1. **只做 WPF→Avalonia 的 API 名称/签名映射**；布局/颜色/文案/交互与上游一致。上游基线：`D:\a\j\PCL-CE`（逐字副本，可随时 diff 对照）。
2. 所有非纯映射的改动必须加 `// [port] <原因>` 中文注释。
3. 编译错误只信完整构建：`dotnet build "src\Plain Craft Launcher 2\Plain Craft Launcher 2.csproj" -v q -nologo --no-incremental`。增量构建和子代理自报的"0 错误"会掩盖残留错误。
4. **禁止用 pwsh 批量重写 .cs 文件**（曾把 `public` 写成 `uublic`）；C# 编辑用 read+edit 工具。
5. 频繁 commit；给用户的问题攒到最后批量问。
6. 工具约束：pwsh 读中文日志用 `[System.Text.Encoding]::GetEncoding(936)`（GBK）；`[System.IO.File]` 用绝对路径；.ps1 脚本纯 ASCII。

## 当前状态（2026-10-01，第三轮缺陷修复）

- ✅ 编译 0 错误；`PCL.exe --pagetest` 自检 **59 通过 / 2 失败**（2 项为已知上游缺口），**0 FTL / 0 ERR**
- ✅ **第三轮（用户报告的 13 项，2026-10-01）**：
  - 点**下载**选项卡约 1 秒崩溃 → `MyTextBox` 给 FluentValidation 传 null 模型（坑 31）
  - 点**设置**选项卡整个 UI 冻结 → `MyComboBox` 模板部件 NRE + 弹窗系统损坏（坑 30 / 28 / 29）
  - 右键菜单（实例文件夹列表）一出现就崩溃 → `BoxShadow` 语法错（坑 27）
  - 弹窗"继续/取消"点不动、关不掉 → `PanMsg` 上的拖动抢指针（**上一轮 FormDragMove 修复已覆盖**）
  - 点"测试游戏"/正版验证弹出**致命异常窗口**并退出 → `MyMsgBox` 的 `(int)null` + 跨线程 `DispatcherFrame`（坑 28 / 29）
  - **选择档案界面一个账号都没有** → `ItemsControl` 缺模板（坑 25）+ `*` 行塌陷（坑 26）
  - "开始重置"按钮变**方角** → `ElementName` 指向根 `x:Name` 的绑定失效（坑 32）
  - 点**工具**选项卡只切栏不换内容 → `PageTools` 属暂缓功能（预期）
- ⚠️ **仍未修（需继续排查）**：账号头像不显示（皮肤头缓存未生成，见下）、启动游戏时界面元素重合、启动按钮偶发卡在"正在加载"、实例选择左侧文件夹列表左键无法切换文件夹
- ⚠️ **正版登录**需要 `PCL_MS_CLIENT_ID` 环境变量（Debug 构建）或构建期密钥库（Release）；仓库不含该密钥，属设计如此
- ❌ 未做：Linux 真机测试（**本机 `wsl.exe` 被安全策略拉黑**，只能交给用户 VM）
- ❌ 暂缓功能（用户指定 8 项）：CrashAnalysis / ModMusic / ModVideoBack / ModLink / ModSkin / PageTools / ClipboardInterceptor / LazyLoadBehavior

## 下一步（按序）

1. 用户复测本轮 13 项（重点：下载/设置选项卡不再崩、档案列表能列出账号、弹窗按钮可点、方角恢复圆角）。
2. 继续排查：账号头像（`pathTemp\Cache\Skin\Head\*.png` 缓存只在打开皮肤页时生成）、启动页元素重合、启动按钮状态、文件夹列表左键。
3. Linux 真机测试：`dist/linux-x64/`（含 `run.sh`）拷到 Linux VM，`chmod +x PCL run.sh && ./run.sh`。
4. 暂缓功能移植（优先级问用户，PageTools 大概率优先）。

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
