"""R2: Status line of DC-NEW-wl-t21-float-d and the README section."""
import pathlib
p = pathlib.Path('docs/lessons/defect-classes.md'); s = p.read_bytes().decode('utf-8'); nl = '\r\n' if '\r\n' in s else '\n'
old = ("  `ComposerContentOraclesDriveTheGestureTests`. Residual: a render hidden in another file's helper, and the oracles" + nl
       + "  `ContentFor` and `ActiveSurfaceId`, are not seen by the gate (named in its summary comment).")
new = ("  `ComposerContentOraclesDriveTheGestureTests`. Residual: a render hidden in another file's helper is not seen by the gate" + nl
       + "  (named in its summary comment). Owner R2 settled (T21 n10, 2026-10-20): `ContentFor` and `ActiveSurfaceId` read the drawn" + nl
       + "  tree, so they are now view oracles in the gate. R2a counted 6 tests that read one after a layout command with a by-hand" + nl
       + "  render between; all 6 now take the user's route (`layout-gate/r2/R2b-sweep.txt`), none a finding.")
assert s.count(old) == 1
p.write_bytes(s.replace(old, new).encode('utf-8'))
r = pathlib.Path('docs/proof/workbench-loops-wl-s/layout-gate/README.md'); t = r.read_bytes().decode('utf-8'); n2 = '\r\n' if '\r\n' in t else '\n'
t = t.rstrip() + n2 + n2 + ("## R2 (T21 n10): ContentFor and ActiveSurfaceId are view oracles" + n2 + n2
  + "Both read `Manager.Layout` (then the parked content), so the Owner ruled them view state. R2a counted, by a line scan that does not use the gate's regexes (`r2/R2a-search.py`, `r2/R2a-count.txt`), the App tests that read either one after a layout command with a by-hand render between: **count = 6**. R2b added both reads to the gate's oracle pattern; red named exactly those 6 (`r2/R2b-red.txt`); each now takes the route, with no rename (`r2/R2b-sweep.txt`); the gate is green (`r2/R2b-green.txt`). A mutation that unwires `LayoutApplied` turns 1 of the 6 red (`r2/R2b-mutation-red.txt`), and the sweep says why the other 5 are not sensitive to it. Residual unchanged: a command or render in another file's helper (`ChecklistHarness`, `ComposedCoding`) is not seen by the gate or by R2a." + n2)
r.write_bytes(t.encode('utf-8'))
