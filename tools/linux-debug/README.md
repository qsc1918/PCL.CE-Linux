# Linux 调试脚本集

配合 `docs/HANDOVER.md` §0.5 与 `docs/LINUX-BLANK-WINDOW.md` 使用。

## 真机（用户 VMware VM）

连接信息见 `docs/HANDOVER.md` §0.5：

```
H=172.22.192.1        # WSL 默认网关 = Windows 宿主
ssh abc@$H -p 62222   # 密码 abc；sudo 密码也是 abc（不是 root）
```

### `vm-run-and-shot.sh`

在真机上启动 PCL（可带参数）、截图、报告窗口内容与资源占用。

```bash
# 先传到真机
sshpass -e scp -P 62222 vm-run-and-shot.sh abc@$H:/tmp/
# 在真机上执行
sshpass -e ssh -p 62222 abc@$H "bash /tmp/vm-run-and-shot.sh --opacity1 --skip-eula"
```

关键输出：`颜色数`（**=1 就是全黑**）、`中心像素`、日志行数、CPU/内存。

### `vm-diag.sh`

同上的简化版，固定带 `--opacity1 --skip-eula --uidump`，并打印 `uidump.txt`（UI 线程看门狗输出）。

### `vm-thread-analysis.sh`

分析 PCL 各线程的累计 CPU 与状态，用来判断"是不是主线程在空转"。
判据：最忙线程的 `tid == pid` 就是 UI 线程；状态 `R` = 运行中，`S` = 睡眠。

### `measure-cpu-mem.sh`

按时间采样 CPU / 内存 / 线程数。**判据：内存每秒涨 100MB+ 就是命中了泄漏问题**
（正常应该稳定）。用法：`bash measure-cpu-mem.sh [总秒数] [采样间隔]`

### `vm-render-mode-test.sh`

切换 Avalonia 渲染模式（默认 / 软件）跑并截图，用来排除"GL 渲染失败"。
配合程序里的 `PCL_FORCE_SOFTWARE_RENDER=1` 环境变量使用。

### `vm-sudo-askpass.sh`

真机上装包用的 sudo 免交互脚本。**为什么需要它**：`echo 密码 | sudo -S` 和 `bash -s`
会抢同一个 stdin，报 "no password was provided"。用 `SUDO_ASKPASS` 绕开。
（真机 `abc` 用户的 sudo 密码是 `abc`，不是 `root`）

## 本地 WSL 快速复现

### `xvfb-shot.sh`

用 Xvfb 虚拟显示跑 PCL 并截图。**迭代比真机快得多**，适合快速验证改动。

> ⚠️ 脚本里会启动 `xcompmgr`。**不加合成器时窗口必然全黑**，会把"缺合成器"误判成程序 bug。

## 相关

- `tools/av-api-probe/` —— 一个极简 Avalonia 程序，用来实测某个 API 的行为
  （比翻文档快，比在 PCL 里猜准）。用法：`dotnet run --project tools/av-api-probe`

## 部署流程

```bash
# 1. 本地发布 + 打包
cd D:/a/j/PCL.CE-Linux
dotnet publish "src\Plain Craft Launcher 2\Plain Craft Launcher 2.csproj" \
  -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=false -o dist/linux-x64-flat
chmod +x dist/linux-x64-flat/PCL
cd dist && tar czf /d/a/j/pcl-flat.tgz linux-x64-flat

# 2. 传到真机并解压
H=172.22.192.1; export SSHPASS=abc
sshpass -e scp -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -P 62222 /mnt/d/a/j/pcl-flat.tgz abc@$H:/tmp/
sshpass -e ssh -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -p 62222 abc@$H \
  "cd /tmp && tar xzf pcl-flat.tgz && chmod +x linux-x64-flat/PCL"
```

本地网络很快，55MB 的包约 1.5 秒传完。

## 踩坑（务必先看）

- `echo root | sudo -S` 与 `bash -s` 会**抢同一个 stdin** → 报 "no password was provided"。
  改用 `SUDO_ASKPASS`（写脚本输出密码，`SUDO_ASKPASS=... sudo -A ...`）。
- 截图要抓**内层窗口**：外层是 mutter 的装饰框架（928x616），内层才是 PCL（900x550）。
  `xwininfo -root -tree | grep '"PCL' | grep -v mutter`
- 运行前必须设 `XAUTHORITY=$(ls /run/user/1000/.mutter-Xwaylandauth.* | head -1)`，否则连不上 X。
- **诊断输出直接写文件**，不要靠 stdout：重定向到文件 + `SIGKILL` 会丢缓冲。

---

## 一键构建 / 打包（2026-10-22 更新）

脚本：`tools/build-release.sh`（Git Bash / WSL / Linux）、`tools/build-release.cmd`（Windows 双击，
检测到 bash 会自动转调，没有 bash 也能跑）、`tools/zipdir.py`（打包用，Git Bash 没有 zip 命令）。

```bash
tools/build-release.sh                    # 全部 4 个版本 + 四合一 zip（发版用）
tools/build-release.sh --linux --single   # 只出 Linux 单文件
tools/build-release.sh --windows          # 只出 Windows（单文件 + 平面）
tools/build-release.sh --no-bundle        # 构建但不打 zip
tools/build-release.sh --clean            # 先清空输出目录再构建
```

产物：

| 路径 | 说明 |
|---|---|
| `dist/release/PCL-CE-<版本>-all.zip` | **四合一发布包**：`windows-x64-single/`、`windows-x64-flat/`、`linux-x64-single/`、`linux-x64-flat/` + 总 README |
| `dist/linux-x64-single/PCL` | Linux 单文件，约 120MB |
| `dist/win-x64-single/PCL.exe` | Windows 单文件，约 120MB |
| `dist/linux-x64-flat/`、`dist/win-x64-flat/` | 平面版，排查时用（可单独替换某个 DLL） |

每个版本目录里都有 `README.md`（各自说明数据目录和运行方式），Linux 版还带 `run.sh`
（强制 `LC_ALL=zh_CN.UTF-8`）。

### ⚠️ 数据目录（写文档时别再搞错）

`PCL.Core/App/Paths.cs` 的规则：

```csharp
_data = Path.Combine(DefaultDirectory, "PCL");
// 仅非 Windows 且 <程序目录>/PCL 是「文件」时（可执行文件就叫 PCL），才退让为 PCL-Data
if (!OperatingSystem.IsWindows() && File.Exists(_data))
    _data = Path.Combine(DefaultDirectory, "PCL-Data");
```

| 系统 | 数据目录 | 原因 |
|---|---|---|
| Windows | `<程序目录>\PCL\` | exe 叫 `PCL.exe`，不与目录名冲突 |
| Linux | `<程序目录>/PCL-Data/` | 可执行文件就叫 `PCL`，同名目录会冲突 |

### ⚠️ 构建时的坑（脚本已规避）

1. **不要把 `dotnet publish` 的输出管道接 `grep`/`head`** —— `head` 提前关管道会 SIGPIPE
   杀掉构建，产物不完整（运行时报 `The application to execute does not exist`）。
   脚本统一写 `dist/_build.log`。
2. **默认不 `rm -rf` 输出目录**，靠 `dotnet publish` 原地覆盖。平面版目录有 300+ 文件，
   `rm -rf` 会触发沙箱/IDE 的批量删除保护而中断构建。要彻底干净用 `--clean`。
3. **Windows 单文件发布残留 `.pdb`**，脚本已删。
4. **从 Windows 打 zip 会丢 Linux 可执行位**（NTFS 没有真正的 POSIX 权限位），
   `tools/zipdir.py` 对 `PCL` / `run.sh` 显式写 0755。
