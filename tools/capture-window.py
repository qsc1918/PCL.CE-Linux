#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""截取 PCL 窗口并采样关键区域颜色，用于与原版截图做客观比对。"""
import ctypes
import ctypes.wintypes as wt
import os
import subprocess
import sys
import time

from PIL import Image

user32 = ctypes.windll.user32
gdi32 = ctypes.windll.gdi32

SRCCOPY = 0x00CC0020
DIB_RGB_COLORS = 0


class BITMAPINFOHEADER(ctypes.Structure):
    _fields_ = [("biSize", wt.DWORD), ("biWidth", wt.LONG), ("biHeight", wt.LONG),
                ("biPlanes", wt.WORD), ("biBitCount", wt.WORD), ("biCompression", wt.DWORD),
                ("biSizeImage", wt.DWORD), ("biXPelsPerMeter", wt.LONG),
                ("biYPelsPerMeter", wt.LONG), ("biClrUsed", wt.DWORD), ("biClrImportant", wt.DWORD)]


class BITMAPINFO(ctypes.Structure):
    _fields_ = [("bmiHeader", BITMAPINFOHEADER), ("bmiColors", wt.DWORD * 3)]


def find_pcl_windows():
    """枚举所有可见顶层窗口，返回标题含 PCL 的 (hwnd, rect) 列表。"""
    found = []

    @ctypes.WINFUNCTYPE(wt.BOOL, wt.HWND, wt.LPARAM)
    def cb(hwnd, _):
        if not user32.IsWindowVisible(hwnd):
            return True
        n = user32.GetWindowTextLengthW(hwnd)
        if n == 0:
            return True
        buf = ctypes.create_unicode_buffer(n + 1)
        user32.GetWindowTextW(hwnd, buf, n + 1)
        title = buf.value
        if "PCL" in title:
            r = wt.RECT()
            user32.GetWindowRect(hwnd, ctypes.byref(r))
            found.append((hwnd, title, (r.left, r.top, r.right, r.bottom)))
        return True

    user32.EnumWindows(cb, 0)
    return found


def grab(rect):
    l, t, r, b = rect
    w, h = r - l, b - t
    hdc = user32.GetDC(0)
    mem = gdi32.CreateCompatibleDC(hdc)
    bmp = gdi32.CreateCompatibleBitmap(hdc, w, h)
    gdi32.SelectObject(mem, bmp)
    gdi32.BitBlt(mem, 0, 0, w, h, hdc, l, t, SRCCOPY)

    bi = BITMAPINFO()
    bi.bmiHeader.biSize = ctypes.sizeof(BITMAPINFOHEADER)
    bi.bmiHeader.biWidth = w
    bi.bmiHeader.biHeight = -h  # 自上而下
    bi.bmiHeader.biPlanes = 1
    bi.bmiHeader.biBitCount = 32
    buf = ctypes.create_string_buffer(w * h * 4)
    gdi32.GetDIBits(mem, bmp, 0, h, buf, ctypes.byref(bi), DIB_RGB_COLORS)

    img = Image.frombuffer("RGBA", (w, h), buf, "raw", "BGRA", 0, 1).convert("RGB")
    gdi32.DeleteObject(bmp)
    gdi32.DeleteDC(mem)
    user32.ReleaseDC(0, hdc)
    return img


def main():
    exe = sys.argv[1] if len(sys.argv) > 1 else None
    if exe:
        subprocess.Popen([exe], cwd=os.path.dirname(exe))
        time.sleep(22)

    wins = find_pcl_windows()
    if not wins:
        print("未找到 PCL 窗口")
        return
    for hwnd, title, rect in wins:
        print("窗口 %s rect=%s" % (title.strip()[:40], rect))
    hwnd, title, rect = max(wins, key=lambda x: (x[2][2] - x[2][0]) * (x[2][3] - x[2][1]))
    img = grab(rect)
    w, h = img.size
    print("截图尺寸 %dx%d" % (w, h))
    out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "tmp", "port-window.png")
    img.save(out)
    print("已保存", out)

    # 采样：标题栏（顶部中间）、内容区、左侧栏
    pts = {
        "标题栏": (int(w * 0.45), 22),
        "内容区": (int(w * 0.62), int(h * 0.62)),
        "左侧栏": (int(w * 0.08), int(h * 0.60)),
        "内容区2": (int(w * 0.80), int(h * 0.30)),
    }
    for k, (x, y) in pts.items():
        if 0 <= x < w and 0 <= y < h:
            px = img.getpixel((x, y))
            print("   %-8s (%4d,%4d) -> #%02x%02x%02x" % (k, x, y, px[0], px[1], px[2]))


if __name__ == "__main__":
    main()
