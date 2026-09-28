#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
修正 PageComp 的 [Content] 内容路由（Avalonia 与 WPF 语义差异导致的填充期 NRE）。

问题
----
上游 WPF 在 PageComp 类上标 [ContentProperty("SearchTags")]，让子类 XAML 写成：
    <local:PageComp x:Name="Content" PageType="Mod">
        <local:MyComboBoxItem ... />   <!-- 期望进入 SearchTags（下拉框项） -->
    </local:PageComp>

WPF 按"根标签类型"路由子元素，所以 PageComp 自己的 PageComp.xaml 里
`<local:MyScrollViewer x:Class="PCL.PageComp">` 的子元素走 MyScrollViewer.Content，不受影响。

Avalonia 改为按"实例类型"路由：一旦在 SearchTags 上标 [Content]，
PageComp 自身 XAML 的根子元素（StackPanel）也会被塞进 SearchTags →
填充期访问尚未初始化的 ComboSearchTag → NullReferenceException
（HANDOVER §7 坑 3 的同一类问题，当时修了 8 个控件，漏了 PageComp）。

修法
----
1) 移除 SearchTags 上的 [Content]（PageComp 自身根子元素恢复走 MyScrollViewer.Content）；
2) 6 个子类 XAML 显式写成 <local:PageComp.SearchTags> ... </local:PageComp.SearchTags>。
   子类填充时 PageComp 已完整构造完毕，ComboSearchTag 非 null，故读取 Items 安全。

用法：
    python tools/fix-pagecomp-content.py --dry-run
    python tools/fix-pagecomp-content.py
"""
import io
import os
import sys

ROOT = os.path.normpath(os.path.join(
    os.path.dirname(os.path.abspath(__file__)), "..",
    "src", "Plain Craft Launcher 2", "Pages", "PageDownload", "Comp"))

FILES = [
    "PageDownloadMod.axaml",
    "PageDownloadPack.axaml",
    "PageDownloadDataPack.axaml",
    "PageDownloadResourcePack.axaml",
    "PageDownloadShader.axaml",
    "PageDownloadWorld.axaml",
]

PROP_OPEN = "<local:PageComp.SearchTags>"
PROP_CLOSE = "</local:PageComp.SearchTags>"


def indent_of(line):
    return line[: len(line) - len(line.lstrip(" \t"))]


def transform(text):
    lines = text.splitlines(keepends=True)
    if any(PROP_OPEN in l for l in lines):
        return text, 0

    open_idx = None
    for i, l in enumerate(lines):
        s = l.strip()
        if s.startswith("<local:PageComp") and s.endswith(">"):
            open_idx = i
            break
    if open_idx is None:
        raise SystemExit("未找到 <local:PageComp ...> 开标签")

    close_idx = None
    for j in range(open_idx + 1, len(lines)):
        if lines[j].strip() == "</local:PageComp>":
            close_idx = j
            break
    if close_idx is None:
        raise SystemExit("未找到 </local:PageComp>")

    eol = "\r\n" if lines[open_idx].endswith("\r\n") else "\n"
    # 属性元素比 <local:PageComp> 深一级（与子元素同级）
    prop_indent = indent_of(lines[open_idx]) + "    "

    out = lines[: open_idx + 1]
    out.append(prop_indent + PROP_OPEN + eol)
    out.extend(lines[open_idx + 1: close_idx])
    out.append(prop_indent + PROP_CLOSE + eol)
    out.extend(lines[close_idx:])
    return "".join(out), 1


def main():
    dry = "--dry-run" in sys.argv
    total = 0
    for rel in FILES:
        path = os.path.join(ROOT, rel)
        with io.open(path, "r", encoding="utf-8", newline="") as f:
            original = f.read()
        updated, n = transform(original)
        total += n
        print("%-34s 包裹 %d 处" % (rel, n))
        if dry and n:
            for k, l in enumerate(updated.splitlines()[:14], 1):
                print("    %3d | %s" % (k, l))
            print()
        if not dry and updated != original:
            with io.open(path, "w", encoding="utf-8", newline="") as f:
                f.write(updated)
    print("\n合计：%d 处%s" % (total, "（dry-run，未写入）" if dry else "已写入"))


if __name__ == "__main__":
    main()
