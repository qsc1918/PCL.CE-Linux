# AGENTS.md — PCL-CE → Avalonia/Linux 移植项目

> 本文件是新 AI 代理的**第一入口**。完整交接文档：**`docs/HANDOVER.md`**（必读，含全部踩坑记录与文件清单）。
> Avalonia 12 实测 API 笔记：`docs/av12-notes.txt`。早期评估报告（A/B/C 分类）：`PORTING_PLAN.md`。
> `docs/PORTING-STATUS.md`、`STATUS.md` 为历史快照，**已过时**，不要按它们行动。

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

## 当前状态（2026-09-30，第二轮缺陷修复）

- ✅ 编译 0 错误；`PCL.exe --pagetest` 自检 **59 通过 / 2 失败**（2 项为已知上游缺口）
- ✅ **第一轮（界面/配色）**：整站配色、本地化、内嵌图片、页面实例化崩溃 —— 已修（见下"快速排雷"）
- ✅ **第二轮（用户报告的交互缺陷，2026-09-30）**：
  - 实例设置页左侧列表项**全部点不动** → 同一事件多个 Tunnel 处理器在 Avalonia 下**逆序**执行，复位先于点击（坑 19）
  - 标题栏选项卡 / 最小化 / 关闭 / 返回按钮**点不动**（有动画）→ `IsPointerOver` 含子元素，拖动分支抢走指针（坑 20）
  - 「实例设置」按钮**文字空白** → `TextPadding=36` 超过按钮高度，Avalonia 把文字压没（坑 22）
  - 实例设置页**滚轮异常缓慢** → 滚轮量纲 ±120 vs ±1（坑 23）
  - 点「实例选择」**崩溃**（"加载实例列表失败"）→ 配置里残留旧 `pack://` 图标路径（坑 18 补充）
  - 连带修复：Mod 页 `SetColorfulTextLab` NRE（坑 21）、`MyTextBox` 提示文本 NRE（坑 24）
- ⚠️ 已知未修：`设置→更新`页（上游缺 `Tahoe.png`）、`联机`页（暂缓功能）
- ❌ 未做：Linux 真机测试（**本机 `wsl.exe` 被安全策略拉黑**，只能交给用户 VM）
- ❌ 暂缓功能（用户指定 8 项）：CrashAnalysis / ModMusic / ModVideoBack / ModLink / ModSkin / PageTools / ClipboardInterceptor / LazyLoadBehavior

## 下一步（按序）

1. 用户复测：左侧列表项/标题栏选项卡/最小化/关闭/返回按钮可点；实例设置按钮文字正常；滚轮速度正常；点「实例选择」不崩。
2. Linux 真机测试：把 `dist/linux-x64/`（含 `run.sh`）拷到 Linux VM，`chmod +x PCL run.sh && ./run.sh`。WSL 在本机被安全策略拉黑，无法代跑。
3. 暂缓功能移植（优先级问用户，PageTools 大概率优先）。
4. 清理：根目录 errs*.txt/err_*.txt/build_full.txt 为历史调试产物；`src\PCL.Neo.Core`/`PCL.Neo.UI` 废弃骨架待用户决定删除。

## 快速排雷（Avalonia ↔ WPF 语义差异，HANDOVER §7 有完整清单）

| 症状 | 根因 |
|---|---|
| 控件可见但**点击无反应** | 同一事件多个 Tunnel 处理器**逆序**执行（坑 19） |
| 标题栏按钮点不动、有动画 | `IsMouseDirectlyOver` → 误用 `IsPointerOver`，拖动抢指针（坑 20） |
| 文字空白/被压没 | `TextBlock.Padding` 超过可用尺寸（坑 22） |
| 页面能编译、一打开就崩 | 根元素 `x:Name`（坑 12）、集合属性缺 `<x:Array>` 等价物（坑 13）、`[Content]` 路由（坑 14） |
| 运行期 `FieldAccessException` | `*Property` 字段非 public（坑 15） |
| 显示 `!key!` | 非 UI 线程取本地化（坑 16） |
| 整站配色偏暗 | scRGB 当 sRGB 用（坑 17） |
| 图片加载失败 | `avares://` 大小写、pack URI 残留、旧配置路径（坑 18） |
| 崩溃在某 `.Text` 上 | WPF `TextBlock.Text` 默认 `""`，Avalonia 默认 `null`（坑 21） |
| 滚动异常慢 | 滚轮量纲 ±120 vs ±1（坑 23） |
| 模板部件取到 null | `Template.FindName` 垫片不可靠，改用 `e.NameScope`（坑 24） |

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
