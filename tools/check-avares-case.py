#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
校验 avares://PCL/... 资源路径与磁盘文件的大小写是否一致。

Avalonia 的 AssetLoader 按清单里的资源名精确匹配（区分大小写），
所以 `avares://PCL/images/Blocks/Anvil.png` 在磁盘上是 `Images/Blocks/Anvil.png` 时会
抛 FileNotFoundException（Windows 文件系统不区分大小写，但 Avalonia 资源名区分）。

用法：
    python tools/check-avares-case.py            # 只报告
    python tools/check-avares-case.py --fix      # 修正为磁盘上的真实大小写
"""
import io
import os
import re
import sys

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "src"))
APP_DIR = os.path.join(ROOT, "Plain Craft Launcher 2")
CORE_DIR = os.path.join(ROOT, "PCL.Core")

# 每个 avares 前缀对应哪个程序集根目录
ASSEMBLY_ROOTS = {
    "PCL": APP_DIR,
    "PCL.Core": CORE_DIR,
}

PATTERN = re.compile(r'avares://(PCL\.Core|PCL)/([^"\'\s>)]+)')


def build_index(root):
    """建立 小写相对路径 -> 真实相对路径 的索引。"""
    index = {}
    for dirpath, dirnames, filenames in os.walk(root):
        dirnames[:] = [d for d in dirnames if d not in ("obj", "bin", ".git")]
        for fn in filenames:
            full = os.path.join(dirpath, fn)
            rel = os.path.relpath(full, root).replace("\\", "/")
            index[rel.lower()] = rel
    return index


def main():
    do_fix = "--fix" in sys.argv
    indexes = {k: build_index(v) for k, v in ASSEMBLY_ROOTS.items()}

    targets = []
    for base in (APP_DIR, CORE_DIR):
        for dirpath, dirnames, filenames in os.walk(base):
            dirnames[:] = [d for d in dirnames if d not in ("obj", "bin", ".git", "Generated")]
            for fn in filenames:
                if fn.endswith((".axaml", ".xaml", ".cs")):
                    targets.append(os.path.join(dirpath, fn))

    bad = 0
    missing = 0
    for path in targets:
        with io.open(path, "r", encoding="utf-8", errors="replace", newline="") as f:
            text = f.read()
        if "avares://" not in text:
            continue

        def repl(m):
            nonlocal bad, missing
            asm, rel = m.group(1), m.group(2)
            rel = rel.split("?")[0].split("#")[0]
            real = indexes.get(asm, {}).get(rel.lower())
            if real is None:
                missing += 1
                print("  [缺失] %s: avares://%s/%s" % (os.path.relpath(path, ROOT), asm, rel))
                return m.group(0)
            if real != rel:
                bad += 1
                print("  [大小写] %s: avares://%s/%s  ->  %s"
                      % (os.path.relpath(path, ROOT), asm, rel, real))
                return "avares://%s/%s" % (asm, real)
            return m.group(0)

        updated = PATTERN.sub(repl, text)
        if do_fix and updated != text:
            with io.open(path, "w", encoding="utf-8", newline="") as f:
                f.write(updated)

    print("\n大小写不符：%d 处；磁盘上找不到：%d 处%s"
          % (bad, missing, "（已修正）" if do_fix else "（dry-run）"))


if __name__ == "__main__":
    main()
