using System.IO;
using System.Text.RegularExpressions;
using Xunit.Abstractions;

namespace AiDe.App.Tests.Perspectives;

/// <summary>
/// <b>The class gate for DC-NEW-wl-t21-float-d (T21 n9).</b> T21 node 2's <c>FloatPane_FloatsTheFocusedPane_...</c> ran
/// <c>controller.Execute("workbench.floatPane")</c> on a hand-built controller, then called <c>adapter.Render()</c> itself,
/// then read the floating windows. It passed while the palette drew 0 windows: the test supplied the render that no
/// product route performed (<c>WorkbenchController.LayoutApplied</c> was wired to nothing).
/// </summary>
/// <remarks>
/// <para><b>The rule (V1).</b> A test that issues a layout command and then reads a view oracle reaches the command by the
/// user's route, and does not render the adapter by hand between the command and the oracle. A by-hand render before any
/// command is setup and is allowed; so is one after the last oracle (a deliberate second render). The four token sets:</para>
/// <list type="bullet">
/// <item><b>view oracle</b> (<see cref="Oracle"/>): what reads the drawn layout: the docking manager's layout tree or
/// floating windows, a floating window's hosted content, the drawn-id set helper, the Center's drawn empty copy, a drawn
/// rail. <b>Not an oracle:</b> <c>Adapter.ContentFor</c> (the adapter's content cache: a surface's lifetime, not the drawn
/// arrangement) and reads of a content control's own state.</item>
/// <item><b>layout command</b> (<see cref="Command"/>): what changes the layout model without the route: a
/// <c>workbench.*</c> command id executed on a controller that is not a <c>DockHost</c>'s, or a non-<c>AddSurface</c>
/// <c>LayoutOperation</c> applied to the service directly. <c>AddSurface</c> only opens panes: setup.</item>
/// <item><b>route</b> (<see cref="Route"/>): the command palette over a <c>DockHost</c>'s controller, or
/// <c>Controller.Execute</c> on it (<c>host.Controller</c>, <c>shell.Execute</c>, <c>Factory.Controller</c>). A command so
/// reached is drawn by <c>LayoutApplied</c>, so it is not a layout command in this rule's sense.</item>
/// <item><b>by-hand render</b> (<see cref="Render"/>): <c>Adapter.Render()</c> / <c>adapter.Render()</c> called by the test.</item>
/// </list>
/// <para><b>What the scan sees.</b> Text order inside one test, with same-file helpers and the test's own local functions
/// and lambdas inlined at their call sites, to <see cref="HelperDepth"/> levels. A by-hand render wrapped in a helper is
/// therefore still a render (the evasion rule, DC-NEW-wl-t30-gate-evasion). <b>Residual:</b> a render or a command hidden in
/// another file's helper, or passed in as a delegate parameter, is not seen; nor is a render reached through a method
/// the test does not call by name. Ordering is textual, not control flow.</para>
/// <para>A test that cannot meet the rule without a src change is named in <see cref="Exceptions"/> with its reason. That
/// list may only shrink. A test that fails once it takes the route is a product finding: it stays failing; it is never listed.</para>
/// </remarks>
public sealed class LayoutOraclesDriveTheCommandRouteTests(ITestOutputHelper output)
{
    /// <summary>How many levels of same-file helper calls the scan follows.</summary>
    private const int HelperDepth = 3;

    /// <summary>Reads the drawn layout: the manager's layout tree or floating windows, hosted-content checks, the drawn-id helper.</summary>
    private static readonly Regex Oracle = new(
        @"\.Manager\.(?:Layout|FloatingWindows)\b|\.FloatingWindows\b|\bHostedBy\(|\bDrawn\(|\.CenterEmpty\(|\.RailVisible\(",
        RegexOptions.Compiled);

    /// <summary>A layout command off the route: a <c>workbench.*</c> id executed on a non-DockHost controller, or a non-AddSurface operation applied to the service.</summary>
    private static readonly Regex Command = new(
        @"(?<!\.Controller)(?<!\bshell)(?<!\bShell)\.Execute\(""workbench\.\w+""|\.Apply\(\s*new\s+LayoutOperation\.(?!AddSurface\b)\w+",
        RegexOptions.Compiled);

    /// <summary>The user's route: the palette, or the Execute of a DockHost's own controller or of the shell.</summary>
    private static readonly Regex Route = new(
        @"new\s+CommandPalette\(|\.Controller\.Execute\b|\b[sS]hell\.Execute\(|\bshell\.Execute\(",
        RegexOptions.Compiled);

    /// <summary>The adapter rendered by the test itself.</summary>
    private static readonly Regex Render = new(@"\b[aA]dapter2?\.Render\(\)", RegexOptions.Compiled);

    /// <summary>A member declared at class level (file-scoped namespace: four spaces).</summary>
    private static readonly Regex Member = new(@"^    (?:public|private|internal|protected)\b[^\n]*?(\w+)\s*(?:<[^>\n]*>)?\s*\(", RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>A local function or a lambda held in a local: <c>void Name(...) {</c> / <c>=&gt;</c>, <c>var name = (...) =&gt;</c>, <c>Action&lt;..&gt; name =</c>.</summary>
    private static readonly Regex LocalFunction = new(
        @"^[ ]{8,}(?:(?:async\s+)?(?:static\s+)?(?:void|Task|bool|string|int|[A-Z]\w*(?:<[^>\n]*>)?)\s+(\w+)\s*\([^)\n]*\)\s*(?=\{|=>)|(?:var|Action(?:<[^>\n]*>)?|Func<[^>\n]*>)\s+(\w+)\s*=\s*(?:async\s*)?(?:\([^)\n]*\)|\w+)\s*(?==>))",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Tests that cannot meet the rule without a src change, each with its reason. <b>This list may only shrink</b>: an
    /// entry that now meets the rule, or no longer exists, fails the gate until it is removed.
    /// </summary>
    private static readonly Dictionary<string, string> Exceptions = new(StringComparer.Ordinal)
    {
    };

    /// <summary>The gate on the tree: every layout oracle after a layout command is reached by the route, or the test is named.</summary>
    [Fact]
    public void EveryLayoutOracleAfterALayoutCommand_IsReachedByTheUsersRoute_OrIsNamedAnException()
    {
        var root = Path.Combine(RepoRoot(), "tests", "AiDe.App.Tests");
        var files = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(f => Path.GetFileName(f) != nameof(LayoutOraclesDriveTheCommandRouteTests) + ".cs")
            .ToList();

        var findings = new List<Finding>();
        var commandThenOracle = 0;
        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            var scan = Scan(relative, File.ReadAllText(file));
            commandThenOracle += scan.CommandThenOracle;
            findings.AddRange(scan.Findings);
        }

        var refused = findings.Where(f => !Exceptions.ContainsKey(f.Key)).ToList();
        var stale = Exceptions.Keys.Where(k => findings.All(f => f.Key != k)).ToList();
        output.WriteLine($"{files.Count} files · {commandThenOracle} tests read the drawn layout after a layout command · {findings.Count} render by hand between · {Exceptions.Count} named exceptions");
        findings.ForEach(f => output.WriteLine($"  {(Exceptions.ContainsKey(f.Key) ? "exception" : "REFUSED  ")} {f}"));
        stale.ForEach(k => output.WriteLine($"  STALE     {k}: named an exception, but it meets the rule or no longer exists — remove the entry"));

        Assert.True(refused.Count == 0, $"{refused.Count} test(s) read the drawn layout after a layout command that no user route reaches (DC-NEW-wl-t21-float-d):\n{string.Join("\n", refused)}");
        Assert.True(stale.Count == 0, $"stale exceptions: {string.Join(", ", stale)}");
    }

    /// <summary>
    /// The gate can fail, and it sees through a helper: node 2's shape is refused; so is the same test with its render
    /// wrapped in a local function, in a class helper, and in a helper that calls a helper. Setup renders, a routed
    /// command, a render after the last oracle, and a command the route reaches, pass.
    /// </summary>
    [Fact]
    public void TheGate_RefusesNode2sShape_InAnyWrapping_AndAcceptsTheRoute()
    {
        var scan = Scan("Planted.cs", Planted);
        scan.Findings.ForEach(f => output.WriteLine(f.ToString()));
        Assert.Equal(
            ["Planted.cs::Node2Original", "Planted.cs::RenderInALocalFunction", "Planted.cs::RenderInAClassHelper", "Planted.cs::RenderTwoHelpersDeep", "Planted.cs::ServiceOperationThenRender", "Planted.cs::RailAfterACollapse"],
            scan.Findings.Select(f => f.Key));
        Assert.Equal(7, scan.CommandThenOracle);
    }

    /// <summary>One test that reads the drawn layout after a layout command with a by-hand render between.</summary>
    internal sealed record Finding(string Key, string Command, string Oracle)
    {
        public override string ToString() => $"{Key}: command `{Command}` then a by-hand adapter render, then the drawn layout is read (`{Oracle}`)";
    }

    /// <summary>The gate over one file's source.</summary>
    internal static (int CommandThenOracle, List<Finding> Findings) Scan(string relativePath, string source)
    {
        var lines = source.Split('\n');
        var code = string.Join('\n', lines.Select(l => l.TrimStart().StartsWith("//", StringComparison.Ordinal) ? string.Empty : l));
        var members = Member.Matches(code).Select(m => (Name: m.Groups[1].Value, m.Index)).ToList();
        var bodies = members.Select((m, i) => (m.Name, Body: code[m.Index..(i + 1 < members.Count ? members[i + 1].Index : code.Length)], m.Index)).ToList();
        var classHelpers = bodies.Where(b => !IsTest(lines, code, b.Index)).GroupBy(b => b.Name).ToDictionary(g => g.Key, g => g.First().Body);

        var commandThenOracle = 0;
        var findings = new List<Finding>();
        foreach (var (name, body, _) in bodies.Where(b => IsTest(lines, code, b.Index)))
        {
            var callables = new Dictionary<string, string>(classHelpers);
            callables.Remove(name);
            var stripped = ExtractLocals(body, callables);
            var flat = Flatten(stripped, callables, HelperDepth);

            var command = Command.Match(flat);
            if (!command.Success)
            {
                continue;
            }

            var oracle = Oracle.Matches(flat).FirstOrDefault(o => o.Index > command.Index);
            if (oracle is null)
            {
                continue;
            }

            commandThenOracle++;
            var between = Render.Matches(flat).Any(r => r.Index > command.Index && r.Index < LastOracleAfter(flat, command.Index));
            if (between)
            {
                findings.Add(new Finding($"{relativePath}::{name}", command.Value.Trim(), oracle.Value.Trim()));
            }
        }

        return (commandThenOracle, findings);
    }

    /// <summary>The index of the last view oracle after <paramref name="after"/>; a render after it reaches no oracle.</summary>
    private static int LastOracleAfter(string flat, int after) =>
        Oracle.Matches(flat).Where(o => o.Index > after).Select(o => o.Index).DefaultIfEmpty(after).Max();

    /// <summary>Removes the test's local functions and lambdas from its text and adds them to the callables.</summary>
    private static string ExtractLocals(string body, Dictionary<string, string> callables)
    {
        var chars = body.ToCharArray();
        foreach (Match m in LocalFunction.Matches(body))
        {
            var local = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
            var start = m.Index + m.Length;
            var end = ExtentEnd(body, start);
            if (end <= start || local.Length == 0)
            {
                continue;
            }

            callables[local] = body[start..end];
            for (var i = m.Index; i < end; i++)
            {
                if (chars[i] != '\n')
                {
                    chars[i] = ' ';
                }
            }
        }

        return new string(chars);
    }

    /// <summary>The end of a block (<c>{ ... }</c>) or an expression body (<c>=&gt; ... ;</c>) that starts at <paramref name="start"/>.</summary>
    private static int ExtentEnd(string text, int start)
    {
        var i = start;
        while (i < text.Length && char.IsWhiteSpace(text[i]))
        {
            i++;
        }

        if (i < text.Length && text[i] == '=' && i + 1 < text.Length && text[i + 1] == '>')
        {
            i += 2;
            while (i < text.Length && char.IsWhiteSpace(text[i]))
            {
                i++;
            }
        }

        var depth = 0;
        for (var j = i; j < text.Length; j++)
        {
            switch (text[j])
            {
                case '{' or '(' or '[':
                    depth++;
                    break;
                case '}' or ')' or ']':
                    depth--;
                    if (depth == 0 && text[j] == '}' && text[i] == '{')
                    {
                        return j + 1;
                    }

                    if (depth < 0)
                    {
                        return j;
                    }

                    break;
                case ';' when depth == 0:
                    return j + 1;
            }
        }

        return text.Length;
    }

    /// <summary>The text with each call to a known helper followed by the helper's text, to <paramref name="depth"/> levels.</summary>
    private static string Flatten(string text, Dictionary<string, string> callables, int depth)
    {
        if (depth == 0 || callables.Count == 0)
        {
            return text;
        }

        var names = string.Join('|', callables.Keys.Select(Regex.Escape));
        return Regex.Replace(text, $@"\b({names})\s*\(", m => m.Value + " " + Flatten(callables[m.Groups[1].Value], callables, depth - 1) + " ");
    }

    /// <summary>A member is a test when a <c>[Fact]</c> or <c>[Theory]</c> attribute sits in the lines just above it.</summary>
    private static bool IsTest(string[] lines, string code, int index)
    {
        var line = code[..index].Count(c => c == '\n');
        for (var i = line - 1; i >= 0 && i >= line - 12; i--)
        {
            var text = lines[i].Trim();
            if (text.StartsWith("[Fact", StringComparison.Ordinal) || text.StartsWith("[Theory", StringComparison.Ordinal))
            {
                return true;
            }

            if (text.Length == 0 || text == "}" || text.EndsWith(';') || text.EndsWith('{'))
            {
                return false;
            }
        }

        return false;
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AiDe.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }

    /// <summary>Six refused shapes and three accepted ones, as a test file would carry them.</summary>
    private const string Planted = """
        namespace Planted;

        public sealed class PlantedTests
        {
            /// <summary>T21 node 2's test (650d65c2): a hand-built controller, the render by hand, the floating windows read.</summary>
            [Fact]
            public void Node2Original() =>
                Sta.Pump(create: () => new DockingManager(), configure: w => { }, body: async (main, manager) =>
                {
                    var adapter = new WorkbenchAdapter(manager, service, s => new TextBox());
                    adapter.Render();
                    var controller = new WorkbenchController(service, announcer);
                    controller.Execute("workbench.floatPane");
                    adapter.Render();
                    var window = Assert.Single(manager.FloatingWindows);
                    Assert.True(FloatRenderTests.HostedBy(content, window));
                });

            [Fact]
            public void RenderInALocalFunction() =>
                Sta.Pump(create: () => new DockingManager(), configure: w => { }, body: async (main, manager) =>
                {
                    void Redraw() => adapter.Render();
                    controller.Execute("workbench.floatPane");
                    Redraw();
                    Assert.Single(manager.FloatingWindows);
                });

            [Fact]
            public void RenderInAClassHelper()
            {
                controller.Execute("workbench.closePane");
                Redraw(adapter);
                Assert.Contains("x", Drawn(host));
            }

            [Fact]
            public void RenderTwoHelpersDeep()
            {
                controller.Execute("workbench.maximizePane");
                Settle(adapter);
                Assert.Contains("x", Drawn(host));
            }

            [Fact]
            public void ServiceOperationThenRender()
            {
                host.Service.Apply(new LayoutOperation.CloseSurface("x"));
                host.Adapter.Render();
                Assert.DoesNotContain("x", Drawn(host));
            }

            [Fact]
            public void RailAfterACollapse()
            {
                shell.Factory.Service.Apply(new LayoutOperation.SetStackState("left", StackState.Collapsed));
                shell.Factory.Adapter.Render();
                Assert.True(shell.Factory.Rails.RailVisible(ZoneId.Left));
            }

            [Fact]
            public void SetupRenderOnly()
            {
                host.Service.Apply(new LayoutOperation.AddSurface("c", surface));
                host.Adapter.Render();
                var palette = new CommandPalette(host.Controller.Execute, said);
                palette.Open();
                Assert.Contains("x", Drawn(host));
            }

            [Fact]
            public void RoutedCommandThenASecondRenderAfterTheLastOracle()
            {
                var palette = new CommandPalette(host.Controller.Execute, said);
                host.Controller.Execute("workbench.floatPane");
                Assert.Single(host.Manager.FloatingWindows);
                host.Adapter.Render();
            }

            [Fact]
            public void ACommandWithNoOracle()
            {
                controller.Execute("workbench.floatPane");
                adapter.Render();
                Assert.True(service.Current.Floating.Count == 1);
            }

            [Fact]
            public void CommandThenOracleWithNoRender()
            {
                shell.Factory.Service.Apply(new LayoutOperation.SetStackState("s", StackState.Floating));
                Assert.Single(manager.FloatingWindows);
            }

            private static void Redraw(WorkbenchAdapter adapter) => adapter.Render();

            private static void Settle(WorkbenchAdapter adapter) => Redraw(adapter);
        }
        """;
}
