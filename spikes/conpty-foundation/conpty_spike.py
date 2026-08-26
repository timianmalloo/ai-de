"""Spike: CreatePseudoConsole availability and create/close lifecycle (stdlib only).

Contract established: the ConPTY API exists on this host and a pseudo console can be
created over anonymous pipes and closed cleanly. It does NOT establish I/O semantics —
the architecture's requirement for separate input/output service loops rests on the
documented contract and is exercised in Phase 2, not here.

Run: python spikes/conpty-foundation/conpty_spike.py
"""
import ctypes
import ctypes.wintypes as wt
import struct
import sys

k32 = ctypes.windll.kernel32

def fail(msg: str) -> None:
    print(f"FAIL — {msg}")
    sys.exit(1)

if not hasattr(k32, "CreatePseudoConsole"):
    fail("kernel32 does not export CreatePseudoConsole")

# COORD is a packed (SHORT X, SHORT Y) passed by value.
class COORD(ctypes.Structure):
    _fields_ = [("X", ctypes.c_short), ("Y", ctypes.c_short)]

HPCON = ctypes.c_void_p

in_read, in_write = wt.HANDLE(), wt.HANDLE()
out_read, out_write = wt.HANDLE(), wt.HANDLE()
if not k32.CreatePipe(ctypes.byref(in_read), ctypes.byref(in_write), None, 0):
    fail(f"CreatePipe(stdin) error {k32.GetLastError()}")
if not k32.CreatePipe(ctypes.byref(out_read), ctypes.byref(out_write), None, 0):
    fail(f"CreatePipe(stdout) error {k32.GetLastError()}")

hpc = HPCON()
k32.CreatePseudoConsole.restype = ctypes.c_int32  # HRESULT
hr = k32.CreatePseudoConsole(COORD(80, 25), in_read, out_write, 0, ctypes.byref(hpc))
if hr != 0:
    fail(f"CreatePseudoConsole HRESULT=0x{hr & 0xFFFFFFFF:08X}")

print(f"PASS C1-CREATE — CreatePseudoConsole(80x25) HRESULT=0x{hr:08X}, HPCON={hpc.value:#x}")

resize = getattr(k32, "ResizePseudoConsole", None)
if resize is not None:
    hr2 = resize(hpc, COORD(120, 40))
    print(f"PASS C2-RESIZE — ResizePseudoConsole(120x40) HRESULT=0x{hr2 & 0xFFFFFFFF:08X}"
          if hr2 == 0 else f"FAIL C2-RESIZE — HRESULT=0x{hr2 & 0xFFFFFFFF:08X}")
else:
    print("INFO C2-RESIZE — ResizePseudoConsole not exported")

k32.ClosePseudoConsole(hpc)
for h in (in_read, in_write, out_read, out_write):
    k32.CloseHandle(h)
print("PASS C3-CLOSE — ClosePseudoConsole and pipe handles closed cleanly")
print("ALL CASES PASS")
