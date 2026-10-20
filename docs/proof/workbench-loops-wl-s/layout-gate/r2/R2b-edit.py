"""R2b conversion: replace each by-hand command+render with the user's route (helpers of FloatRenderTests)."""
import pathlib
def sub(path, old, new, count=1):
    p = pathlib.Path(path); s = p.read_text(encoding='utf-8')
    assert s.count(old) == count, (path, old[:60], s.count(old))
    p.write_text(s.replace(old, new), encoding='utf-8', newline='')
    
cb = 'tests/AiDe.App.Tests/Workbench/CollapsedBottomTests.cs'
raw = pathlib.Path(cb).read_bytes().decode('utf-8'); nl = '\r\n' if '\r\n' in raw else '\n'
def j(*ls): return nl.join(ls)
docked = j('                Assert.True(shell.Factory.Service.Apply(new LayoutOperation.SetStackState(ZonesToTree.BottomStackId, StackState.Docked)).Applied);',
           '                shell.Factory.Adapter.Render();')
sub(cb, docked, '                FloatRenderTests.ExpandByTheRail(shell.Factory, ZoneId.Bottom);', 3)
sub(cb, j('                Assert.True(shell.Factory.Service.Apply(new LayoutOperation.SetStackState(ZonesToTree.BottomStackId, StackState.Collapsed)).Applied);',
          '                shell.Factory.Adapter.Render();'),
    '                FloatRenderTests.CollapseByTheRoute(shell.Factory, ZonesToTree.BottomStackId);')

cs = 'tests/AiDe.App.Tests/Sessions/ConsoleSplitPlacementTests.cs'
raw = pathlib.Path(cs).read_bytes().decode('utf-8'); nl = '\r\n' if '\r\n' in raw else '\n'
sub(cs, nl.join(['            Assert.True(shell.Factory.Service.Apply(new LayoutOperation.SetStackState(ZonesToTree.LeftStackId, StackState.Collapsed)).Applied);',
                 '            shell.Factory.Adapter.Render();']),
    '            FloatRenderTests.CollapseByTheRoute(shell.Factory, ZonesToTree.LeftStackId);')
