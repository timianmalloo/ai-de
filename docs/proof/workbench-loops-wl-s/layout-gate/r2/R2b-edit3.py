"""R2b: a Factory host's collapsed Bottom parks its terminal unbuilt (Ruling 88), so the 'every surface realized' set is the expanded zones'."""
import pathlib
p = pathlib.Path('tests/AiDe.App.Tests/ZoneWorkbenchAdapterTests.cs'); s = p.read_bytes().decode('utf-8'); nl = '\r\n' if '\r\n' in s else '\n'
old = "            var before = service.Zones.AllSurfaces().Select(s => s.SurfaceId).ToList();" + nl + "            var leftBefore"
new = ("            // A collapsed zone parks its panes unbuilt (Ruling 88), so the panes drawn are the expanded zones'." + nl
       + "            var before = Enum.GetValues<ZoneId>().Where(z => !service.Zones.Zone(z).Collapsed)" + nl
       + "                .SelectMany(z => service.Zones.Zone(z).Surfaces()).Select(s => s.SurfaceId).ToList();" + nl
       + "            var leftBefore")
assert s.count(old) == 1
p.write_bytes(s.replace(old, new).encode('utf-8'))
