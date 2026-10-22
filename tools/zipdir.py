#!/usr/bin/env python3
"""把若干目录按指定前缀打进一个 zip，并修正 Linux 可执行位。

为什么需要这个脚本：
  1. Git Bash 里没有 zip / 7z（GNU tar 也写不了 zip 格式）。
  2. Windows 的 NTFS 没有真正的 POSIX 权限位，从 Windows 侧打包会让
     PCL / run.sh 丢掉 +x，Linux 用户解压后无法直接执行。
     这里按文件名显式补回 0755。

用法:
  python3 tools/zipdir.py <输出.zip> <前缀>=<源目录> [<前缀>=<源目录> ...]

例:
  python3 tools/zipdir.py dist/release/all.zip \\
      PCL-CE-2.15.0/windows-x64-single=dist/win-x64-single \\
      PCL-CE-2.15.0/linux-x64-single=dist/linux-x64-single

  也支持只给目录（此时用目录名作前缀），便于临时手工打包：
  python3 tools/zipdir.py out.zip dist/linux-x64-single
"""
import os
import sys
import zipfile

# 这些文件名在 zip 里强制 0755（其余沿用原文件的权限）
EXECUTABLES = {"PCL", "run.sh", "PCL.sh"}


def add_tree(zf: zipfile.ZipFile, src: str, prefix: str) -> int:
    src = os.path.normpath(src)
    prefix = prefix.strip("/").replace("\\", "/")
    count = 0
    for root, dirs, files in os.walk(src):
        dirs.sort()
        for name in sorted(files):
            full = os.path.join(root, name)
            rel_inside = os.path.relpath(full, src).replace("\\", "/")
            arcname = f"{prefix}/{rel_inside}" if prefix else rel_inside
            zi = zipfile.ZipInfo.from_file(full, arcname)
            zi.compress_type = zipfile.ZIP_DEFLATED
            if name in EXECUTABLES:
                zi.external_attr = (0o755 << 16) | 0o200000
            with open(full, "rb") as fh, zf.open(zi, "w") as dst:
                while True:
                    chunk = fh.read(1 << 20)
                    if not chunk:
                        break
                    dst.write(chunk)
            count += 1
    return count


def main() -> int:
    if len(sys.argv) < 3:
        print(__doc__)
        return 2
    out = sys.argv[1]
    specs = []
    for arg in sys.argv[2:]:
        if "=" in arg and not os.path.isdir(arg):
            prefix, src = arg.split("=", 1)
        else:
            src = arg
            prefix = os.path.basename(os.path.normpath(src))
        specs.append((prefix, src))

    os.makedirs(os.path.dirname(os.path.abspath(out)) or ".", exist_ok=True)
    total = 0
    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as zf:
        for prefix, src in specs:
            if not os.path.isdir(src):
                print(f"跳过不存在的目录: {src}")
                continue
            n = add_tree(zf, src, prefix)
            print(f"  + {prefix}  <=  {src}  ({n} 个文件)")
            total += n
    size = os.path.getsize(out) / 1048576
    print(f"完成: {out}  {total} 个文件, {size:.1f} MB")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
