#!/usr/bin/env bash
# PCL.CE-Linux 一键构建脚本
#
# 用法：
#   tools/build-release.sh                # 全部目标
#   tools/build-release.sh --linux        # 只要 Linux（单文件 + 非单文件）
#   tools/build-release.sh --windows      # 只要 Windows（单文件 + 非单文件）
#   tools/build-release.sh --single       # 只要两个平台的单文件版（发版用）
#   tools/build-release.sh --flat         # 只要两个平台的非单文件版（排查用）
#   tools/build-release.sh --windows --single    # 组合：只要 Windows 单文件
#
# 产物位置：
#   dist/linux-x64-single/PCL              Linux 单文件（约 120MB，绿色版，解压即用）
#   dist/win-x64-single/PCL.exe            Windows 单文件
#   dist/linux-x64-flat/                   Linux 非单文件（方便替换单个 DLL 排查）
#   dist/win-x64-flat/                     Windows 非单文件
#   dist/release/*.tar.gz                  打好包的 Linux 发布件（含 run.sh + README）
#
# ⚠️ 铁律：绝不要把 dotnet 的输出用管道接到 grep/head！
#    `dotnet publish ... | head` 会让 head 提前关闭管道，SIGPIPE 直接杀掉构建，
#    产物不完整（表现为运行时报 The application to execute does not exist）。
#    本脚本统一写日志再回看。

set -euo pipefail
cd "$(dirname "$0")/.."

PROJ="src/Plain Craft Launcher 2/Plain Craft Launcher 2.csproj"
LINUX_RID="linux-x64"
WIN_RID="win-x64"
LOG="dist/_build.log"
mkdir -p dist/release
: > "$LOG"

# ---------- 参数 ----------
BUILD_LINUX=0; BUILD_WINDOWS=0; WANT_SINGLE=1; WANT_FLAT=1
if [ $# -eq 0 ]; then BUILD_LINUX=1; BUILD_WINDOWS=1; fi
for a in "$@"; do
  case "$a" in
    --linux)   BUILD_LINUX=1 ;;
    --windows) BUILD_WINDOWS=1 ;;
    --single)  WANT_SINGLE=1; WANT_FLAT=0; [ "$BUILD_LINUX$BUILD_WINDOWS" = "11" ] || { BUILD_LINUX=1; BUILD_WINDOWS=1; } ;;
    --flat)    WANT_FLAT=1; WANT_SINGLE=0; [ "$BUILD_LINUX$BUILD_WINDOWS" = "11" ] || { BUILD_LINUX=1; BUILD_WINDOWS=1; } ;;
    *) echo "未知参数: $a"; echo "用法: $0 [--linux] [--windows] [--single] [--flat]"; exit 1 ;;
  esac
done

run_publish() { # $1=描述 其余=dotnet publish 参数
  local desc="$1"; shift
  echo ">>> $desc"
  if ! dotnet publish "$PROJ" "$@" >>"$LOG" 2>&1; then
    echo "!!! 构建失败: $desc"
    tail -30 "$LOG"
    exit 1
  fi
}

# ---------- Linux ----------
if [ "$BUILD_LINUX" = "1" ]; then
  if [ "$WANT_SINGLE" = "1" ]; then
    out="dist/${LINUX_RID}-single"
    rm -rf "$out"
    run_publish "Linux 单文件 ($LINUX_RID)" -c Release -r "$LINUX_RID" --self-contained true \
      -p:PublishSingleFile=true -o "$out"
    chmod +x "$out/PCL"
    rm -f "$out"/*.pdb
    cat > "$out/run.sh" <<'EOF'
#!/usr/bin/env bash
# PCL CE Linux 启动脚本：强制 UTF-8 locale，避免中文显示异常
cd "$(dirname "$0")"
export LC_ALL=${LC_ALL:-zh_CN.UTF-8}
export LANG=${LANG:-zh_CN.UTF-8}
exec ./PCL "$@"
EOF
    chmod +x "$out/run.sh"
    cat > "$out/README.md" <<'EOF'
# PCL CE - Linux

## 运行
```bash
chmod +x PCL      # 解压后首次需要
./PCL
# 或使用启动脚本（自动设置 UTF-8 locale）
./run.sh
```
单个可执行文件，无需安装 .NET 运行时。

## 环境要求
- x64 Linux（Ubuntu + X11 / Wayland(XWayland) 已验证）
- 需要至少一款中文字体：Noto Sans CJK SC / Source Han Sans / 文泉驿 /
  Droid Sans Fallback 任一即可。应用会按该顺序自动查找。
  缺少中文字体时中文显示会异常。
- 建议解压到非临时目录（如 /opt/pcl-ce 或 ~/pcl-ce）。在 /tmp 等临时目录
  运行会弹"环境警告"提示，且临时目录可能被系统清理导致存档/设置丢失。

## 数据与日志
- 首次运行在可执行文件同级目录创建 `PCL-Data/`
- 日志在 `PCL-Data/Log/`，排错请附上最新 `Launch-*.log`
EOF
    tar czf dist/release/PCL-Linux-x64-single.tar.gz -C dist "$(basename "$out")"
    echo "    发布包: dist/release/PCL-Linux-x64-single.tar.gz"
  fi
  if [ "$WANT_FLAT" = "1" ]; then
    out="dist/${LINUX_RID}-flat"
    rm -rf "$out"
    run_publish "Linux 非单文件 ($LINUX_RID)" -c Release -r "$LINUX_RID" --self-contained true \
      -p:PublishSingleFile=false -o "$out"
    chmod +x "$out/PCL"
  fi
fi

# ---------- Windows ----------
if [ "$BUILD_WINDOWS" = "1" ]; then
  if [ "$WANT_SINGLE" = "1" ]; then
    out="dist/${WIN_RID}-single"
    rm -rf "$out"
    run_publish "Windows 单文件 ($WIN_RID)" -c Release -r "$WIN_RID" --self-contained true \
      -p:PublishSingleFile=true -o "$out"
    rm -f "$out"/*.pdb
    cat > "$out/README.md" <<'EOF'
# PCL CE - Windows

## 运行
双击 `PCL.exe` 即可。单个可执行文件，无需安装 .NET 运行时。

## 数据与日志
- 首次运行在可执行文件同级目录创建 `PCL-Data/`
- 日志在 `PCL-Data/Log/`
EOF
  fi
  if [ "$WANT_FLAT" = "1" ]; then
    out="dist/${WIN_RID}-flat"
    rm -rf "$out"
    run_publish "Windows 非单文件 ($WIN_RID)" -c Release -r "$WIN_RID" --self-contained true \
      -p:PublishSingleFile=false -o "$out"
  fi
fi

# ---------- 汇总 ----------
echo
echo "================ 构建完成 ================"
for d in dist/linux-x64-single dist/linux-x64-flat dist/win-x64-single dist/win-x64-flat; do
  [ -d "$d" ] || continue
  exe="$d/PCL"; [ -f "$exe" ] || exe="$d/PCL.exe"
  printf "  %-28s %-10s %s\n" "$d" "$(du -sh "$d" | cut -f1)" "$(basename "$exe")"
done
[ -d dist/release ] && [ -n "$(ls -A dist/release 2>/dev/null)" ] && { echo; echo "发布包："; ls -lh dist/release/; }
echo "完整构建日志: $LOG"
