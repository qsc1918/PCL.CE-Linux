# Linux 上窗口全黑 / CPU 内存暴涨 —— 排查档案

> **状态**：**根因已找到并验证**（见第六节）。修复方案待实施。
> **建档原因**：这是移植过程中的核心阻塞问题，信息量大，需跨会话保留。
> 最后更新：2026-10-04

---

## ⭐ 六、根因确认（2026-10-04 21:56 突破）

### 真凶：`FormMain` 构造函数把窗口设成「完全透明」，而恢复透明度的动画没跑

`src/Plain Craft Launcher 2/FormMain.xaml.cs` 构造函数里：

```csharp
Opacity = 0d;     // ← 窗口创建时完全透明
```

透明度靠 `FormMain_Loaded` 里的 "Form Show" 动画组拉回来：

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

**这个动画组在 Linux 上没有生效** ⇒
1. `Opacity` 停在 0 ⇒ **窗口整体透明 ⇒ 看起来一片黑**（背景、标题栏都由系统画，所以标题栏正常）
2. `TransformPos.Y` 停在 60 ⇒ 内容向下偏移
3. `TransformRotate.Angle` 停在 -4° ⇒ 内容倾斜

### 验证（真机截图，铁证）

加一个临时开关跳过 `Opacity = 0d`，其余代码不动：

| 条件 | 颜色数 | 中心像素 | 结果 |
|---|---|---|---|
| 原样 | 1 | `gray(0)` | 全黑 |
| `--opacity1` | **3290** | `srgb(209,209,209)` | **界面正常渲染出来了** ✓ |

截图里能看到：蓝色标题栏、"启动/下载/设置/工具"四个标签、"正在加载版本列表，请稍候" ——
**但画面仍然倾斜 + 下移**（因为旋转/位移动画同样没生效）。

### 启动路径已验证无阻塞（顺序探针）

```
[P3] ctor Opacity 处理完毕
[P8] FormMain_Loaded 进入
[P4] 即将 ShowWindowToTop  →  [P5] 返回
[P6] 即将 AniStart         →  [P7] 返回, Opacity=1
```

⇒ 构造函数、`FormMain_Loaded`、`ShowWindowToTop`、`AniStart()` **全部正常返回**，
UI 线程在启动阶段没有被卡住。

### 仍未解决

1. **UI 线程不处理派发任务**（核心待查）
2. **CPU 113% / 内存 5.3GB 持续暴涨**（即使窗口已正常渲染）

### 22:15 最新进展：问题收敛到"UI 线程不泵消息"

用文件版看门狗（写文件避免 stdout 缓冲丢失）拿到的关键数据：

```
[P9] FormMain_Loaded 即将结束        ×2      ← 启动路径完整跑完（构造→Loaded→ShowWindowToTop→AniStart→末尾）
[UiDump #1] 动画组=1  等待弹窗=1  AniControlEnabled=1  线程数=36
[UiDump] >>> UI 线程任务被执行了!   ← 从未出现
```

**推论链**：

| 环节 | 状态 |
|---|---|
| `FormMain_Loaded` 执行完 | ✅ 正常（P1~P9 全部打印） |
| `AniStart()` 启动动画线程 | ✅ 返回正常 |
| 动画线程调用 `RunInUiWait(AniTimer)` | ❌ 卡住（`Dispatcher.Invoke` 得不到响应） |
| `TimerMainStart()` 的后台线程调用 `RunInUiWait(TimerMain)` | ❌ 同样卡住 |
| `Dispatcher.UIThread.InvokeAsync(...)` 投递任务 | ❌ **从未被执行** |
| 结果 | `AniControlEnabled` 恒 = 1；弹窗队列不清空；"Form Show" 动画不生效 |

**注意**：`TimerMain` 不是 `DispatcherTimer`，而是后台线程 `while(true) { RunInUiWait(TimerMain); Sleep(49); }`。
所以"嵌套泵不跑 DispatcherTimer"这条早期结论**需要修正** —— 真正的问题更基础：
**UI 线程根本不响应来自其他线程的 Dispatcher 投递**。

**新疑点**：UI 线程回到了消息循环却没处理派发任务，且 CPU 持续 114%。
怀疑卡在**消息循环内部的某个同步操作**（布局/渲染循环？）。
下一步可用 `gdb` 附加取原生栈，或在 Avalonia 的渲染/布局入口加探针。


---

## 一、现象

Linux（真机 GNOME+Wayland，以及本地 Xvfb）上运行 PCL：

| 观测项 | 数值 |
|---|---|
| 窗口 | **创建成功**：900x550，标题 `PCL CE Linux`，`Map State: IsViewable` |
| 窗口内容 | **纯黑**，整窗只有 1 种颜色，采样像素 `gray(0)` |
| 标题栏 | **正常显示**（由 mutter 绘制，不是 Avalonia 画的） |
| CPU | 86% ~ 111% |
| 内存 | 持续暴涨，实测 8.1 GB（占系统 49.6%），约 150 MB/s |
| 日志 | 跑到 100~118 行后**停止增长**（停在"无可用 Minecraft 实例"附近） |
| FTL / ERR | 0 / 0 |

⇒ 表现为"窗口在、但一片空白、程序还在吃 CPU 和内存"。

---

## 二、根因机制（已确认）

### 2.1 UI 线程卡在模态弹窗的嵌套消息泵里

用 `Environment.StackTrace` 在动画回调处抓到的**决定性调用栈**：

```
at PCL.ModAnimation.AniTimer(Int32 deltaTick)
at PCL.ModAnimation.<>c__DisplayClass9_1.<AniStart>b__1()
at Avalonia.Threading.Dispatcher.PushFrame(DispatcherFrame frame)
at PCL.ModMain.<>c__DisplayClass81_0.<PumpMsgBoxFrame>b__0()      ← 嵌套泵在这里
at Avalonia.Threading.Dispatcher.PushFrame(DispatcherFrame frame)
at Avalonia.Threading.Dispatcher.MainLoop(...)                    ← 外层正常主循环
```

`PumpMsgBoxFrame`（`src/Plain Craft Launcher 2/Modules/ModMain.cs`）的实现：

```csharp
private static void PumpMsgBoxFrame(MyMsgBoxConverter converter)
{
    if (ModBase.RunInUi())
    {
        MyMsgBoxTick();
        frmMain?.DragStop();
        Dispatcher.UIThread.PushFrame(converter.WaitFrame);   // ← 阻塞在这里等用户点击
        return;
    }
    ModBase.RunInUiWait(() => { ...; Dispatcher.UIThread.PushFrame(converter.WaitFrame); });
}
```

### 2.2 关键差异：Avalonia 的嵌套泵不跑渲染，也不跑定时器

**实测对照**（同一进程、同一时刻）：

| 能力 | 外层主循环 | 嵌套消息泵（`PushFrame`） |
|---|---|---|
| 消息队列 / `InvokeAsync` 任务 | 执行 | **执行** |
| 动画帧 `AniTimer` | 执行 | **执行**（所以 CPU/内存暴涨） |
| **渲染 tick** | 执行 | **不执行** ❌ |
| **`DispatcherTimer`** | 执行 | **不执行** ❌ |

证据：
- 在动画回调里打栈，看到 `PumpMsgBoxFrame` 在栈上（说明动画是在嵌套泵里跑的）
- 用 `DispatcherTimer` 做的截图诊断（7 秒后触发）**一次都没触发**；改成后台线程 + `InvokeAsync` 也拿不到渲染结果
- 窗口自始至终没有一帧被绘制

**⇒ 弹窗永远画不出来 → 用户点不到 → 永远等下去 → 动画在泵里空转 → CPU/内存暴涨。**

### 2.3 这是移植引入的框架差异，不是 PCL 的 bug

上游 WPF 的 `Dispatcher.PushFrame` **会**继续渲染，所以同样的代码在 Windows 上正常。
移植到 Avalonia 后这个前提不成立 —— **这是必须解决的核心移植问题**。

---

## 三、已排除的原因

| 怀疑 | 验证方式 | 结论 |
|---|---|---|
| 缺窗口合成器 | 真机有 mutter 合成器；Xvfb 下加 `xcompmgr -c` 后**背景能画出来了**（`gray(36)` vs `gray(0)`） | ❌ 不是根因（但无合成器时确实会更黑） |
| GL/GPU 渲染失败 | 强制 `X11PlatformOptions.RenderingMode = [X11RenderingMode.Software]` | ❌ 仍全黑 |
| EULA 弹窗是唯一阻塞点 | 加 `--skip-eula` 跳过 EULA 弹窗 | ❌ **仍全黑，CPU 111%、内存 1.9GB**（日志 100→118 行，确实走得更远） |
| 弹窗弹得太早（早于首帧） | 把 EULA 弹窗延后 3 秒 | ❌ 仍全黑 |
| `ExtendClientAreaToDecorationsHint` | 临时改 `False` | ❌ 仍全黑 |
| 窗口 `Opacity` 停在 0 | 查 `AniControlEnabled` / `AaOpacity` 逻辑 | ❌ `Window.Opacity` 默认 1，动画不跑也不会变透明 |

**注意**：方案 B 验证说明 **EULA 只是"最先撞上的那个"阻塞弹窗，不是唯一问题** ——
`MyMsgBox` 在代码里有 **174 个调用点、分布在 45 个文件**，其中大半是阻塞型。

---

## 四、修复方案 A（已设计，未实施）

### 目标

让模态弹窗等待期间**保持渲染和定时器运转**，同时**保持调用方看到的同步语义不变**
（`MyMsgBox` 返回 `int`，调用方靠返回值决策，例如 EULA 选"拒绝"要退出程序）。

### 约束

1. **不能改成异步 API** —— 174 个调用点都依赖同步返回，改动量等同方案 C（已否决）
2. **只改一处** —— `ModMain.PumpMsgBoxFrame`，让所有调用点自动受益
3. **必须处理重入** —— 等待期间可能再弹一个窗，依赖现有的 `WaitingMyMsgBox` 队列
4. **`MsgBoxWrapper_OnShow`（WPF 兼容层）也走这条路**，要一起回归

### 实现方向（待验证）

用**能驱动渲染的等待循环**替换 `Dispatcher.UIThread.PushFrame(converter.WaitFrame)`：

- 手动泵消息队列（`Dispatcher.UIThread.RunJobs()`）
- **显式触发渲染**（关键 —— 嵌套泵不跑渲染 tick，必须自己驱动）
- 直到 `converter.WaitFrame.Continue == false`（用户点了按钮）

备选：改用 Avalonia 的 `TopLevel.RequestAnimationFrame` 驱动渲染，
或把等待挂到外层主循环上（需要研究 Avalonia 是否有公开 API 支持"渲染一次"）。

### 验证标准

在真机上跑起来后：窗口**能看到界面内容**（截图颜色数远大于 1），
CPU 回落到正常水平，内存不再暴涨，弹窗可点击。

---

## 五、排查环境与方法（可复用）

### 真机（VMware Linux）

```
WSL 默认网关 = Windows 宿主 = 172.22.192.1
ssh abc@172.22.192.1 -p 62222          # 密码 abc
sudo 密码：abc（不是 root！）
```

```bash
H=172.22.192.1; export SSHPASS=abc
sshpass -e ssh -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -p 62222 abc@$H "<命令>"
sshpass -e scp -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -P 62222 <本地> abc@$H:/tmp/
```

- 真机环境：Ubuntu 24.04、**GNOME + Wayland**（mutter 合成器）、无 .NET SDK
- 已装工具：`imagemagick`（`import`）、`x11-utils`（`xwininfo`）
- flat 构建部署在真机 `/tmp/linux-x64-flat/`

### 部署流程（已验证，55MB 包约 1.5 秒传完）

```bash
cd D:/a/j/PCL.CE-Linux/dist && tar czf /d/a/j/pcl-flat.tgz linux-x64-flat
# scp 到真机 /tmp，解压，chmod +x linux-x64-flat/PCL
```

### 运行 + 截图（关键：环境变量要对）

```bash
export DISPLAY=:0
export XAUTHORITY=$(ls /run/user/1000/.mutter-Xwaylandauth.* | head -1)   # 少了这个连不上 X
./PCL &
sleep 18
# 找内层真正的 PCL 窗口（外层是 mutter 的装饰框架）
WID=$(xwininfo -root -tree | grep '"PCL' | grep -v mutter | awk '{print $1}' | head -1)
import -window $WID /tmp/shot.png
convert /tmp/shot.png -format "尺寸=%wx%h 颜色数=%[colors]" info:      # 颜色数=1 就是全黑
```

### 本地 Xvfb 复现（更快，适合快速迭代）

```bash
Xvfb :99 -screen 0 1280x800x24 &
xcompmgr -c &                      # ← 必须！否则窗口全黑，会误判
DISPLAY=:99 ./PCL &
import -window root shot.png
```

### 踩坑记录

- `echo root | sudo -S` 与 `bash -s` 会**抢同一个 stdin** → 报 "no password was provided"。
  改用 `SUDO_ASKPASS`（写个脚本输出密码，`SUDO_ASKPASS=... sudo -A ...`）
- 截图一定要抓**内层窗口**，外层是 mutter 装饰框架（尺寸是 928x616，不是 900x550）
- 不装 `xcompmgr` 时 Xvfb 里窗口全黑，会把"缺合成器"误当成根因
