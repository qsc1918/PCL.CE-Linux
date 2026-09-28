#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
恢复 <local:MyListItem.Buttons> 的集合包裹层（上游 WPF 用 <x:Array>，Avalonia 需改用 <sys:List>）。

背景
----
上游 WPF：
    <local:MyListItem.Buttons>
        <x:Array Type="{x:Type local:MyIconButton}">
            <local:MyIconButton ... />
        </x:Array>
    </local:MyListItem.Buttons>

`Buttons` 的声明类型是 `IEnumerable<MyIconButton>`（C# 侧有 20+ 处用 `new[] {...}` 赋值，不能改类型）。
移植时误判「Avalonia 无 x:Array」而删掉包裹层、直接把 MyIconButton 内联进 Buttons 属性元素。
结果：编译能过，但运行期 Avalonia 的 !XamlIlPopulate 会对尚未初始化的集合字段做添加 →
NullReferenceException（页面实例化即崩，如 PageInstanceLeft.axaml:20）。

Avalonia 实况（12.0.4 实测）
--------------------------
* `x:Array` 至今不支持（维护者原话："Arrays aren't currently supported"）。
* 正确等价写法：
      xmlns:sys="clr-namespace:System.Collections.Generic;assembly=netstandard"
      <sys:List x:TypeArguments="local:MyIconButton"> ... </sys:List>
  ⚠ assembly 必须是 `netstandard`；写成 `System.Runtime` 会报
    "Unable to resolve type List from namespace ... assembly=System.Runtime"。

用法：
    python tools/fix-buttons-collection.py --dry-run
    python tools/fix-buttons-collection.py
"""
import io
import os
import sys

OPEN_TAG = "<local:MyListItem.Buttons>"
CLOSE_TAG = "</local:MyListItem.Buttons>"
LIST_TAG_TMPL = '<{p}:List x:TypeArguments="local:MyIconButton">'
LIST_END_TMPL = "</{p}:List>"
GEN_NS = "clr-namespace:System.Collections.Generic;assembly=netstandard"

STALE_COMMENT_MARKERS = [
    "x:Array（Type 属性）在 Avalonia 无对应",
]

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "src", "Plain Craft Launcher 2")
FILES = [
    "Application.axaml",
    "Pages/PageInstance/PageInstanceLeft.axaml",
    "Pages/PageDownload/PageDownloadLeft.axaml",
    "Pages/PageSetup/PageSetupLeft.axaml",
    "Pages/PageInstance/PageInstanceSaves/PageInstanceSavesLeft.axaml",
]

# 候选前缀（按优先级）。Application.axaml 已占用 sys（指向 System），需避让。
PREFIX_CANDIDATES = ["sys", "sysgen", "syslist", "sysg"]


def indent_of(line: str) -> str:
    return line[: len(line) - len(line.lstrip(" \t"))]


def is_stale_comment(line: str) -> bool:
    s = line.strip()
    return s.startswith("<!--") and any(m in s for m in STALE_COMMENT_MARKERS)


def pick_prefix(text: str) -> str:
    """选一个尚未被占用的命名空间前缀。"""
    for p in PREFIX_CANDIDATES:
        if ('xmlns:%s=' % p) not in text:
            return p
    raise SystemExit("没有可用的命名空间前缀")


def ensure_xmlns(lines, prefix: str):
    decl = 'xmlns:%s="%s"' % (prefix, GEN_NS)
    if any(decl in l for l in lines):
        return lines, False
    for i, l in enumerate(lines):
        if "xmlns:x=" in l:
            base = indent_of(l)
            eol = "\r\n" if l.endswith("\r\n") else "\n"
            lines.insert(i + 1, base + decl + eol)
            return lines, True
    raise SystemExit("未找到 xmlns:x 声明行")


def transform(text: str):
    prefix = pick_prefix(text)
    lines = text.splitlines(keepends=True)
    lines, xmlns_added = ensure_xmlns(lines, prefix)
    list_tag = LIST_TAG_TMPL.format(p=prefix)
    list_end = LIST_END_TMPL.format(p=prefix)

    out = []
    i = 0
    wrapped = 0
    while i < len(lines):
        line = lines[i]
        if is_stale_comment(line):
            i += 1  # 删除过时注释
            continue
        if line.strip() != OPEN_TAG:
            out.append(line)
            i += 1
            continue

        base_indent = indent_of(line)
        inner = []
        j = i + 1
        while j < len(lines) and lines[j].strip() != CLOSE_TAG:
            inner.append(lines[j])
            j += 1
        if j >= len(lines):
            raise SystemExit("未找到匹配的 " + CLOSE_TAG)
        inner = [l for l in inner if not is_stale_comment(l)]

        out.append(line)
        if any(":List" in l or l.strip().startswith("<x:Array") for l in inner):
            out.extend(inner)  # 已有包裹层，跳过
        else:
            eol = "\r\n" if line.endswith("\r\n") else "\n"
            pad = base_indent + "    "
            out.append(pad + list_tag + eol)
            out.extend(inner)
            out.append(pad + list_end + eol)
            wrapped += 1
        out.append(lines[j])
        i = j + 1
    return "".join(out), wrapped, xmlns_added, prefix


def main():
    dry = "--dry-run" in sys.argv
    total = 0
    for rel in FILES:
        path = os.path.normpath(os.path.join(ROOT, rel))
        with io.open(path, "r", encoding="utf-8", newline="") as f:
            original = f.read()
        updated, wrapped, xmlns_added, prefix = transform(original)
        total += wrapped
        print("%-62s 包裹 %d 个块%s (前缀 %s)"
              % (rel, wrapped, "，补 xmlns" if xmlns_added else "", prefix))
        if dry and wrapped:
            before = original.splitlines()
            after = updated.splitlines()
            for k in range(min(len(before), len(after))):
                if before[k] != after[k]:
                    for m in range(max(0, k - 1), min(len(after), k + 5)):
                        print("    %4d | %s" % (m + 1, after[m]))
                    break
            print()
        if not dry and updated != original:
            with io.open(path, "w", encoding="utf-8", newline="") as f:
                f.write(updated)
    print("\n合计：%d 个块%s" % (total, "（dry-run，未写入）" if dry else "已写入"))


if __name__ == "__main__":
    main()
