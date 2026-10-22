#!/usr/bin/env bash
# PCL.CE-Linux 一键构建 / 打包脚本
#
# 用法：
#   tools/build-release.sh                  全部目标 + 四合一 zip（发版用）
#   tools/build-release.sh --linux          只要 Linux（单文件 + 非单文件）
#   tools/build-release.sh --windows        只要 Windows（单文件 + 非单文件）
#   tools/build-release.sh --single         只要两个平台的单文件版
#   tools/build-release.sh --flat           只要两个平台的非单文件版
#   tools/build-release.sh --no-bundle      构建但不打 zip
#   tools/build-release.sh --clean          先清空输出目录再构建（更彻底，但会触发批量删除保护时会被拦）
#
# 产物：
#   dist/linux-x64-single/PCL              Linux 单文件（约 120MB，绿色版）
#   dist/win-x64-single/PCL.exe            Windows 单文件
#   dist/linux-x64-flat/                   Linux 非单文件（排查用，可单独换 DLL）
#   dist/win-x64-flat/                     Windows 非单文件
#   dist/release/PCL-CE-<版本>-all.zip     ★ 四个版本打成一个 zip（发版给用户）
#
# ⚠️ 铁律：绝不要把 dotnet 的输出用管道接到 grep/head！
#    `dotnet publish ... | head` 会让 head 提前关闭管道，SIGPIPE 直接杀掉构建，
#    产物不完整（表现为运行时报 The application to execute does not exist）。
#    本脚本统一写日志再回看。
#
# ⚠️ 数据目录（README 里必须写对，已读 Paths.cs 确认）：
#    Windows → <程序目录>\PCL\      （exe 叫 PCL.exe，不与目录名冲突）
#    Linux   → <程序目录>/PCL-Data/ （可执行文件就叫 PCL，与目录名冲突时才改名）

set -euo pipefail
cd "$(dirname "$0")/.."

PROJ="src/Plain Craft Launcher 2/Plain Craft Launcher 2.csproj"
META="src/Plain Craft Launcher 2/metadata.json"
LINUX_RID="linux-x64"
WIN_RID="win-x64"
LOG="dist/_build.log"
mkdir -p dist/release
: > "$LOG"

# ---------- 版本号（读 metadata.json，避免手写过期） ----------
VER=$(python3 -c "
import json,sys
m=json.load(open(sys.argv[1],encoding='utf-8'))
v=m.get('version',{})
print(v.get('base','0.0.0')+(v.get('suffix') or '')+('-'+str(v['code']) if v.get('code') else ''))
" "$META")
echo "版本: $VER"

# ---------- 参数 ----------
BUILD_LINUX=0; BUILD_WINDOWS=0; WANT_SINGLE=1; WANT_FLAT=1; BUNDLE=1; CLEAN=0
if [ $# -eq 0 ]; then BUILD_LINUX=1; BUILD_WINDOWS=1; fi
for a in "$@"; do
  case "$a" in
    --linux)     BUILD_LINUX=1 ;;
    --windows)   BUILD_WINDOWS=1 ;;
    --single)    WANT_SINGLE=1; WANT_FLAT=0; BUILD_LINUX=1; BUILD_WINDOWS=1 ;;
    --flat)      WANT_FLAT=1; WANT_SINGLE=0; BUILD_LINUX=1; BUILD_WINDOWS=1 ;;
    --no-bundle) BUNDLE=0 ;;
    --clean)     CLEAN=1 ;;
    -h|--help)   sed -n '2,26p' "$0"; exit 0 ;;
    *) echo "未知参数: $a（--help 看用法）"; exit 1 ;;
  esac
done

# 准备输出目录：默认原地覆盖（dotnet publish 会覆盖同名文件）。
# 不默认 rm -rf 是因为平面版目录有 300+ 文件，某些环境（沙箱/IDE）的批量删除保护会拦住，
# 而且删了再建会让增量构建失效。需要彻底干净时用 --clean。
prepare_out() {
  if [ "$CLEAN" = "1" ] && [ -d "$1" ]; then rm -rf "$1"; fi
  mkdir -p "$1"
}

run_publish() { # $1=描述 其余=dotnet publish 参数
  local desc="$1"; shift
  echo ">>> $desc"
  if ! dotnet publish "$PROJ" "$@" >>"$LOG" 2>&1; then
    echo "!!! 构建失败: $desc"
    tail -30 "$LOG"
    exit 1
  fi
}

write_run_sh() { # $1=目标目录
  cat > "$1/run.sh" <<'EOF'
#!/usr/bin/env bash
# PCL CE Linux 启动脚本：强制 UTF-8 locale，避免中文显示异常
cd "$(dirname "$0")"
export LC_ALL=${LC_ALL:-zh_CN.UTF-8}
export LANG=${LANG:-zh_CN.UTF-8}
exec ./PCL "$@"
EOF
  chmod +x "$1/run.sh"
}

write_linux_readme() { # $1=目录 $2=single|flat
  {
    echo "# PCL CE for Linux"
    echo
    echo "版本 $VER"
    echo
    echo "## 运行"
    echo
    if [ "$2" = "single" ]; then
      echo '单个可执行文件（约 120MB），无需安装 .NET 运行时。'
    else
      echo '非单文件版（目录里有一堆 `.dll`），无需安装 .NET 运行时。'
      echo '它和单文件版功能完全一样，只是拆成了多个文件，方便排查问题时单独替换某个 DLL。'
    fi
    echo
    echo '```bash'
    echo 'chmod +x PCL run.sh     # 解压后首次需要（zip 一般已保留权限）'
    echo './PCL'
    echo '# 或者用启动脚本（自动设置 UTF-8 locale，推荐）'
    echo './run.sh'
    echo '```'
    echo
    echo '## 数据目录'
    echo
    echo '首次运行会在**可执行文件同级目录**创建 `PCL-Data/`：'
    echo
    echo '```'
    echo 'PCL            <- 可执行文件'
    echo 'run.sh'
    echo 'PCL-Data/      <- 配置、日志、缓存、截图都在这'
    echo '  Log/         <- 日志，排错时请附上最新的 Launch-*.log'
    echo '```'
    echo
    echo '> Linux 上目录名是 `PCL-Data` 而不是 `PCL`：可执行文件本身就叫 `PCL`，'
    echo '> 同名目录会冲突，程序自动退让成 `PCL-Data`。Windows 版则是 `PCL\`。'
    echo
    echo '## 环境要求'
    echo
    echo '- x64 Linux'
    echo '- 需要至少一款中文字体，任一即可：Noto Sans CJK SC / Source Han Sans SC(CN) /'
    echo '  WenQuanYi Micro Hei(Zen Hei) / AR PL UMing CN / Droid Sans Fallback。'
    echo '  程序会按上面的顺序自动查找。**系统完全没有中文字体时界面文字会显示异常。**'
    echo '- 建议解压到非临时目录（如 `/opt/pcl-ce` 或 `~/pcl-ce`）。'
    echo '  放在 `/tmp` 下运行会弹「环境警告」，且临时目录可能被系统清理导致存档/设置丢失。'
    echo '- 已验证：Ubuntu + X11 / Wayland(XWayland)。'
  } > "$1/README.md"
}

write_windows_readme() { # $1=目录 $2=single|flat
  {
    echo "# PCL CE for Windows"
    echo
    echo "版本 $VER"
    echo
    echo "## 运行"
    echo
    if [ "$2" = "single" ]; then
      echo '双击 `PCL.exe` 即可。单个可执行文件（约 120MB），无需安装 .NET 运行时。'
    else
      echo '双击 `PCL.exe` 即可。非单文件版（目录里有一堆 `.dll`），无需安装 .NET 运行时。'
      echo '它和单文件版功能完全一样，只是拆成了多个文件，方便排查问题时单独替换某个 DLL。'
    fi
    echo
    echo '## 数据目录'
    echo
    echo '首次运行会在**可执行文件同级目录**创建 `PCL\` 文件夹：'
    echo
    echo '```'
    echo 'PCL.exe        <- 可执行文件'
    echo 'PCL\           <- 配置、日志、缓存、截图都在这'
    echo '  Log\         <- 日志，排错时请附上最新的 Launch-*.log'
    echo '```'
    echo
    echo '> 注意是 `PCL\` 文件夹，**不是 `PCL-Data`**。`PCL-Data` 只在 Linux 上出现'
    echo '> （Linux 可执行文件就叫 `PCL`，与同名目录冲突，程序才退让成 `PCL-Data`）。'
    echo
    echo '## 环境要求'
    echo
    echo '- 64 位 Windows 10 1809 (build 17763) 或更高'
    echo '- 建议解压到非系统目录，不要直接在压缩包的临时目录里运行'
  } > "$1/README.md"
}

write_bundle_readme() { # $1=目录
  {
    echo "# PCL CE $VER — 全部平台"
    echo
    echo '四个版本功能完全一致，按你的系统挑一个就行。'
    echo
    echo '| 目录 | 系统 | 形态 | 说明 |'
    echo '|---|---|---|---|'
    echo '| `windows-x64-single/` | Windows x64 | 单文件 | **推荐**。只有一个 `PCL.exe`，双击即用 |'
    echo '| `windows-x64-flat/` | Windows x64 | 多文件 | 功能相同，拆成多个 DLL，排查问题时可单独替换 |'
    echo '| `linux-x64-single/` | Linux x64 | 单文件 | **推荐**。只有一个 `PCL`，`chmod +x PCL` 后 `./PCL` |'
    echo '| `linux-x64-flat/` | Linux x64 | 多文件 | 功能相同，拆成多个 DLL，排查问题时可单独替换 |'
    echo
    echo '所有版本都是**自包含**的，不需要另外安装 .NET 运行时。'
    echo
    echo '## 数据目录在哪'
    echo
    echo '| 系统 | 位置 |'
    echo '|---|---|'
    echo '| Windows | 可执行文件同级的 `PCL\` 文件夹 |'
    echo '| Linux | 可执行文件同级的 `PCL-Data/` 文件夹 |'
    echo
    echo '日志都在里面的 `Log/` 子目录，遇到问题请附上最新的 `Launch-*.log`。'
    echo
    echo '> Linux 的目录名是 `PCL-Data` 而不是 `PCL`，因为可执行文件本身就叫 `PCL`，'
    echo '> 同名目录会冲突，程序自动退让。Windows 上没有这个冲突，所以是 `PCL\`。'
    echo
    echo '## Linux 额外要求'
    echo
    echo '需要至少一款中文字体，任一即可：Noto Sans CJK SC / Source Han Sans / WenQuanYi /'
    echo 'AR PL UMing / Droid Sans Fallback。程序会按顺序自动查找，**完全没有中文字体时'
    echo '界面文字会显示异常**。'
    echo
    echo '另外建议解压到非临时目录（如 `/opt/pcl-ce`、`~/pcl-ce`）；放在 `/tmp` 下运行'
    echo '会弹「环境警告」，且临时目录可能被系统清理，导致存档/设置丢失。'
    echo
    echo '各目录内还有更详细的 `README.md`。'
  } > "$1/README.md"
}

# ============ Linux ============
if [ "$BUILD_LINUX" = "1" ]; then
  if [ "$WANT_SINGLE" = "1" ]; then
    out="dist/${LINUX_RID}-single"; prepare_out "$out"
    run_publish "Linux 单文件 ($LINUX_RID)" -c Release -r "$LINUX_RID" --self-contained true \
      -p:PublishSingleFile=true -o "$out"
    chmod +x "$out/PCL"; rm -f "$out"/*.pdb
    write_run_sh "$out"; write_linux_readme "$out" single
  fi
  if [ "$WANT_FLAT" = "1" ]; then
    out="dist/${LINUX_RID}-flat"; prepare_out "$out"
    run_publish "Linux 非单文件 ($LINUX_RID)" -c Release -r "$LINUX_RID" --self-contained true \
      -p:PublishSingleFile=false -o "$out"
    chmod +x "$out/PCL"; rm -f "$out"/*.pdb
    write_run_sh "$out"; write_linux_readme "$out" flat
  fi
fi

# ============ Windows ============
if [ "$BUILD_WINDOWS" = "1" ]; then
  if [ "$WANT_SINGLE" = "1" ]; then
    out="dist/${WIN_RID}-single"; prepare_out "$out"
    run_publish "Windows 单文件 ($WIN_RID)" -c Release -r "$WIN_RID" --self-contained true \
      -p:PublishSingleFile=true -o "$out"
    rm -f "$out"/*.pdb; write_windows_readme "$out" single
  fi
  if [ "$WANT_FLAT" = "1" ]; then
    out="dist/${WIN_RID}-flat"; prepare_out "$out"
    run_publish "Windows 非单文件 ($WIN_RID)" -c Release -r "$WIN_RID" --self-contained true \
      -p:PublishSingleFile=false -o "$out"
    rm -f "$out"/*.pdb; write_windows_readme "$out" flat
  fi
fi

# ============ 四合一 zip ============
if [ "$BUNDLE" = "1" ] && [ "$BUILD_LINUX" = "1" ] && [ "$BUILD_WINDOWS" = "1" ] \
   && [ "$WANT_SINGLE" = "1" ] && [ "$WANT_FLAT" = "1" ]; then
  # 不再复制到 staging 目录：直接按「前缀=源目录」映射写进 zip。
  # 好处是不需要 600MB 的临时副本，也省掉一次大目录删除（会触发批量删除保护）。
  rd="dist/_bundle_readme"
  rm -rf "$rd" && mkdir -p "$rd"
  write_bundle_readme "$rd"
  echo ">>> 打包四合一 zip（约 600MB，压缩需要几分钟）"
  python3 tools/zipdir.py "dist/release/PCL-CE-$VER-all.zip" \
    "PCL-CE-$VER=$rd" \
    "PCL-CE-$VER/windows-x64-single=dist/${WIN_RID}-single" \
    "PCL-CE-$VER/windows-x64-flat=dist/${WIN_RID}-flat" \
    "PCL-CE-$VER/linux-x64-single=dist/${LINUX_RID}-single" \
    "PCL-CE-$VER/linux-x64-flat=dist/${LINUX_RID}-flat"
  rm -rf "$rd"
fi

# ---------- 汇总 ----------
echo
echo "================ 构建完成 ================"
for d in dist/linux-x64-single dist/linux-x64-flat dist/win-x64-single dist/win-x64-flat; do
  [ -d "$d" ] || continue
  printf "  %-28s %-8s\n" "$d" "$(du -sh "$d" | cut -f1)"
done
if [ -n "$(ls -A dist/release 2>/dev/null)" ]; then
  echo
  echo "发布包："
  ls -lh dist/release/
fi
echo "完整构建日志: $LOG"
