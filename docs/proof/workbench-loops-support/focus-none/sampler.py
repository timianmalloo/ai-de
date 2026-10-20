"""Read-only foreground/focus sampler (T40). Windowless: run with pythonw.

Every 500 ms appends one CSV row. Calls only GetForegroundWindow, GetWindowThreadProcessId, GetClassNameW,
GetGUIThreadInfo, GetAsyncKeyState, OpenProcess(QUERY_LIMITED) and QueryFullProcessImageNameW. It changes no desktop state.
Usage: pythonw sampler.py <out.csv> <stop-file>. Exits when the stop file exists.
"""
import ctypes
import os
import sys
import time
from ctypes import wintypes

U = ctypes.WinDLL("user32", use_last_error=True)
K = ctypes.WinDLL("kernel32", use_last_error=True)


class GUITHREADINFO(ctypes.Structure):
    _fields_ = [("cbSize", wintypes.DWORD), ("flags", wintypes.DWORD), ("hwndActive", wintypes.HWND),
                ("hwndFocus", wintypes.HWND), ("hwndCapture", wintypes.HWND), ("hwndMenuOwner", wintypes.HWND),
                ("hwndMoveSize", wintypes.HWND), ("hwndCaret", wintypes.HWND), ("rcCaret", wintypes.RECT)]


U.GetForegroundWindow.restype = wintypes.HWND
U.GetWindowThreadProcessId.argtypes = [wintypes.HWND, ctypes.POINTER(wintypes.DWORD)]
U.GetWindowThreadProcessId.restype = wintypes.DWORD
U.GetClassNameW.argtypes = [wintypes.HWND, wintypes.LPWSTR, ctypes.c_int]
U.GetGUIThreadInfo.argtypes = [wintypes.DWORD, ctypes.POINTER(GUITHREADINFO)]
U.GetAsyncKeyState.argtypes = [ctypes.c_int]
K.OpenProcess.argtypes = [wintypes.DWORD, wintypes.BOOL, wintypes.DWORD]
K.OpenProcess.restype = wintypes.HANDLE
K.QueryFullProcessImageNameW.argtypes = [wintypes.HANDLE, wintypes.DWORD, wintypes.LPWSTR, ctypes.POINTER(wintypes.DWORD)]
K.CloseHandle.argtypes = [wintypes.HANDLE]


def cls(h):
    if not h:
        return ""
    b = ctypes.create_unicode_buffer(256)
    return b.value if U.GetClassNameW(h, b, 256) else ""


def pname(pid):
    h = K.OpenProcess(0x1000, False, pid)
    if not h:
        return "?"
    try:
        b = ctypes.create_unicode_buffer(1024)
        n = wintypes.DWORD(1024)
        return os.path.basename(b.value) if K.QueryFullProcessImageNameW(h, 0, b, ctypes.byref(n)) else "?"
    finally:
        K.CloseHandle(h)


def main():
    out, stop = sys.argv[1], sys.argv[2]
    new = not os.path.exists(out)
    f = open(out, "a", buffering=1)
    if new:
        f.write("utc_ms,self_pid,fg_hwnd,fg_proc,fg_pid,fg_class,fg_tid,hwndActive,hwndFocus,focus_class,lbutton\n")
    me = os.getpid()
    while not os.path.exists(stop):
        t = int(time.time() * 1000)
        fg = U.GetForegroundWindow()
        pid = wintypes.DWORD(0)
        tid = U.GetWindowThreadProcessId(fg, ctypes.byref(pid)) if fg else 0
        gi = GUITHREADINFO()
        gi.cbSize = ctypes.sizeof(GUITHREADINFO)
        ok = bool(tid) and U.GetGUIThreadInfo(tid, ctypes.byref(gi))
        act = (gi.hwndActive or 0) if ok else 0
        foc = (gi.hwndFocus or 0) if ok else 0
        lb = U.GetAsyncKeyState(1) & 0xFFFF
        f.write("%d,%d,%d,%s,%d,%s,%d,%d,%d,%s,0x%04X\n" % (
            t, me, fg or 0, pname(pid.value) if fg else "", pid.value, cls(fg), tid, act, foc, cls(foc) if foc else "", lb))
        time.sleep(0.5)
    f.close()


main()
