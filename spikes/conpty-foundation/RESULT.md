# Spike result — conpty-foundation

- **Run:** 2026-08-26 · Windows 11 Pro 10.0.26200 · Python 3 (stdlib ctypes)
- **Command:** `python spikes/conpty-foundation/conpty_spike.py`
- **Exit:** 0 (ALL CASES PASS)

## Captured output

```
PASS C1-CREATE — CreatePseudoConsole(80x25) HRESULT=0x00000000, HPCON=0x253f5600c60
PASS C2-RESIZE — ResizePseudoConsole(120x40) HRESULT=0x00000000
PASS C3-CLOSE — ClosePseudoConsole and pipe handles closed cleanly
```
(HPCON value varies per run.)

## Contract established (cases only)

1. `kernel32!CreatePseudoConsole`, `ResizePseudoConsole`, and `ClosePseudoConsole` are
   available on this Windows host and complete a create → resize → close lifecycle over
   anonymous pipes with `HRESULT` 0 (C1–C3).
2. **Not established here:** input/output servicing semantics. The architecture's requirement
   for separate input and output service loops (deadlock avoidance) rests on the documented
   ConPTY contract and is exercised by the Phase-2 terminal runtime, not this spike.
