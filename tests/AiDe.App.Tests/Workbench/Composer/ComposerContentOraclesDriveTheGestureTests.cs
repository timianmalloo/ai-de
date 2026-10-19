using System.IO;
using System.Text.RegularExpressions;
using Xunit.Abstractions;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// <b>The class gate for DC-NEW-wl-t28-compiled (Ruling 181 (3), T28 n3).</b> Ruling 96's control asserted the compiled
/// field's content on a fixture it had set up directly: no typing, no submit, so <c>"" == ""</c> held and the state the
/// user's gesture reaches (the draft consumed on <c>Admit</c>) was never on the path. The rule: a test that reads a
/// user-visible composer field's content — <c>CompiledView</c>, <c>_compiled.Text</c>, the compiled field found by its
/// read-only box or by its UIA name, or <see cref="ComposerGesture.Field"/> / <see cref="ComposerGesture.Heading"/> —
/// drives the user's gesture: the product's composition (a <c>SessionDocumentSurface</c>) <b>and</b> a
/// <see cref="ComposerGesture"/> verb, and it does not set the draft or open the disclosure by assignment. A test that is
/// deliberately pre-admit is named in <see cref="PreAdmit"/> with its reason; that list may only shrink.
/// </summary>
/// <remarks>
/// A source scan, so it sees what the text says: a test reads content when its own body, or a same-file helper it calls,
/// carries a reader token. A reader hidden behind another file's helper is not seen (<see cref="ComposerGesture"/> is the
/// one such file, and its verbs are what the rule requires).
/// </remarks>
public sealed class ComposerContentOraclesDriveTheGestureTests(ITestOutputHelper output)
{
    /// <summary>Reads a user-visible composer field's content (the compiled field, its heading, its UIA name).</summary>
    private static readonly Regex Reader = new(
        @"\.CompiledView\b|_compiled\.Text\b|ComposerGesture\.(?:Field|Heading)\(|CompiledPromptLiveName|""Compiled prompt — exactly what will be sent""",
        RegexOptions.Compiled);

    /// <summary>
    /// The compiled field found by its read-only box. A content read only when the variable it binds has its
    /// <c>.Text</c> read: a layout measure of the same box (its height) reads no content.
    /// </summary>
    private static readonly Regex BoxLookup = new(
        @"var\s+(\w+)\s*=\s*Logical<TextBox>\([^)]*\)\.Single\(\s*\w+\s*=>\s*\w+\.IsReadOnly\s*&&\s*\w+\.MaxHeight\s*<=\s*ComposerSurface\.CompiledPromptMaxHeight",
        RegexOptions.Compiled);

    /// <summary>The product's composition: the composer inside a <c>SessionDocumentSurface</c>.</summary>
    private static readonly Regex Composition = new(@"ComposerGesture\.Drive\(|new SessionDocumentSurface\(|ThreadFixtures\.Document\(", RegexOptions.Compiled);

    /// <summary>The user's gesture, through the shared helper.</summary>
    private static readonly Regex Gesture = new(@"ComposerGesture\.(?:Type|CtrlEnter|CtrlEnterThenWait|Open)\(", RegexOptions.Compiled);

    /// <summary>State set by changing the fixture directly: the draft's words, its shape or template, the disclosure's open state.</summary>
    private static readonly Regex FixtureMutation = new(@"\.Draft\.(?:Set\w+|SwitchTo|UseTemplate)\(|\.CompiledPromptOpen\s*=(?!=)", RegexOptions.Compiled);

    /// <summary>A member declared at class level (file-scoped namespace: four spaces).</summary>
    private static readonly Regex Member = new(@"^    (?:public|private|internal|protected)\b[^\n]*?(\w+)\s*(?:<[^>\n]*>)?\s*\(", RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// The pre-admit content oracles, each with its reason. <b>This list may only shrink</b>: an entry whose test now
    /// drives the gesture, or no longer exists, fails the gate until it is removed.
    /// </summary>
    private static readonly Dictionary<string, string> PreAdmit = new(StringComparer.Ordinal)
    {
        ["Composer/TheWriterKeepsItsRoomTests.cs::TheEditorHostIsNeverSmallerThanTheCompiledViewAndTheCompiledViewKeepsToItsCeiling"] =
            "pre-admit layout: the ~30-line count of CompiledView is the non-vacuity of a height measure on a live draft; no submit is in the claim",
        ["Sessions/TheWriterKeepsItsRoomTests.cs::UnderAShortWindow_TheEditorGivesWayToItsFloor_NeverBelow130"] =
            "pre-admit layout: the ~30-line count of CompiledView is the non-vacuity of the floors under a short window, on a live draft before any send",
        ["Sessions/TheDecorationTheRowTheBytesAndTheColumnAgreeTests.cs::ForOnePromptTheDecorationTheRowTheBytesAndTheColumnAgree"] =
            "pre-admit live view: the disclosure is read BEFORE the send and compared with the bytes the lane then receives; the post-submit field is TheCompiledFieldAfterASubmitTests'",
    };

    /// <summary>The gate on the tree: every composer content oracle drives the gesture, or is named pre-admit.</summary>
    [Fact]
    public void EveryComposerFieldContentOracle_DrivesTheUsersGesture_OrIsNamedPreAdmit()
    {
        var root = Path.Combine(RepoRoot(), "tests", "AiDe.App.Tests");
        var files = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(f => Path.GetFileName(f) is not (nameof(ComposerGesture) + ".cs") and not (nameof(ComposerContentOraclesDriveTheGestureTests) + ".cs"))
            .ToList();

        var findings = new List<Finding>();
        var readers = 0;
        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            var scan = Scan(relative, File.ReadAllText(file));
            readers += scan.Readers;
            findings.AddRange(scan.Findings);
        }

        var refused = findings.Where(f => !PreAdmit.ContainsKey(f.Key)).ToList();
        var stale = PreAdmit.Keys.Where(k => findings.All(f => f.Key != k)).ToList();
        output.WriteLine($"{files.Count} files · {readers} tests read composer field content · {findings.Count} do not drive the gesture · {PreAdmit.Count} named pre-admit");
        findings.ForEach(f => output.WriteLine($"  {(PreAdmit.ContainsKey(f.Key) ? "pre-admit" : "REFUSED  ")} {f}"));
        stale.ForEach(k => output.WriteLine($"  STALE     {k}: named pre-admit, but it drives the gesture or no longer exists — remove the entry"));

        Assert.True(readers > 0, "the scan found no composer content oracle at all — the gate reads nothing");
        Assert.True(refused.Count == 0, $"{refused.Count} composer content oracle(s) do not drive the user's gesture (DC-NEW-wl-t28-compiled):\n{string.Join("\n", refused)}");
        Assert.True(stale.Count == 0, $"stale pre-admit entries: {string.Join(", ", stale)}");
    }

    /// <summary>
    /// The gate can fail: the shape of Ruling 96's original control (the real document, the field read by its box, no
    /// typing, no submit) is refused; so are a bare composer with its draft set and a content read behind a helper. A
    /// layout measure of the same box, and a gesture-driven oracle, pass.
    /// </summary>
    [Fact]
    public void TheGate_RefusesABareComposerContentAssertion_AndAcceptsAGestureDrivenOne()
    {
        var refused = Scan("Planted.cs", Planted);
        refused.Findings.ForEach(f => output.WriteLine(f.ToString()));
        Assert.Equal(4, refused.Readers);
        Assert.Equal(
            ["Planted.cs::Ruling96Original", "Planted.cs::BareComposer", "Planted.cs::BehindAHelper"],
            refused.Findings.Select(f => f.Key));
        Assert.Contains("a ComposerGesture verb", refused.Findings[0].Missing, StringComparison.Ordinal);
        Assert.Contains("the draft or the disclosure set directly", refused.Findings[1].Missing, StringComparison.Ordinal);
        Assert.Contains("a SessionDocumentSurface", refused.Findings[1].Missing, StringComparison.Ordinal);
    }

    /// <summary>One test method that reads composer field content without driving the gesture.</summary>
    internal sealed record Finding(string Key, string Token, string Missing)
    {
        public override string ToString() => $"{Key}: reads composer field content ({Token}) — {Missing}";
    }

    /// <summary>The gate over one file's source: the content-reading test methods, and those that do not drive the gesture.</summary>
    internal static (int Readers, List<Finding> Findings) Scan(string relativePath, string source)
    {
        var code = string.Join('\n', source.Split('\n').Select(l => l.TrimStart().StartsWith("//", StringComparison.Ordinal) ? string.Empty : l));
        var lines = source.Split('\n');
        var members = Member.Matches(code).Select(m => (Name: m.Groups[1].Value, m.Index)).ToList();
        var bodies = members.Select((m, i) => (m.Name, Body: code[m.Index..(i + 1 < members.Count ? members[i + 1].Index : code.Length)], m.Index)).ToList();

        var readers = 0;
        var findings = new List<Finding>();
        foreach (var (name, body, index) in bodies.Where(b => IsTest(lines, code, b.Index)))
        {
            var helpers = bodies.Where(h => h.Name != name && !IsTest(lines, code, h.Index) && Regex.IsMatch(body, $@"\b{Regex.Escape(h.Name)}\s*\(")).Select(h => h.Body).ToList();
            var reach = string.Join('\n', [body, .. helpers]);
            var token = helpers.Prepend(body).Select(ContentRead).FirstOrDefault(t => t is not null);
            if (token is null)
            {
                continue;
            }

            readers++;
            var missing = new List<string>();
            if (!Composition.IsMatch(reach))
            {
                missing.Add("no product composition (a SessionDocumentSurface)");
            }

            if (!Gesture.IsMatch(reach))
            {
                missing.Add("no user gesture (a ComposerGesture verb)");
            }

            if (FixtureMutation.Match(body) is { Success: true } mutation)
            {
                missing.Add($"the draft or the disclosure set directly ({mutation.Value.Trim()})");
            }

            if (missing.Count > 0)
            {
                findings.Add(new Finding($"{relativePath}::{name}", token, string.Join("; ", missing)));
            }
        }

        return (readers, findings);
    }

    /// <summary>The first composer content read in one member's text, or null.</summary>
    private static string? ContentRead(string text)
    {
        if (Reader.Match(text) is { Success: true } reader)
        {
            return reader.Value;
        }

        return BoxLookup.Matches(text)
            .Select(m => m.Groups[1].Value)
            .FirstOrDefault(box => Regex.IsMatch(text, $@"\b{Regex.Escape(box)}\.Text\b")) is { } read
                ? $"{read}.Text of the compiled field's box"
                : null;
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

    /// <summary>Three refused shapes and one accepted one, as a test file would carry them.</summary>
    private const string Planted = """
        namespace Planted;

        public sealed class PlantedTests
        {
            /// <summary>Ruling 96's original control (2a89cf0b): the real document, the field by its box, nothing typed or sent.</summary>
            [Theory]
            [InlineData(0)]
            public void Ruling96Original(int count)
            {
                Sta.Run(() =>
                {
                    using var document = ThreadFixtures.Document(Turns(count));
                    var composer = document.Composer;
                    var box = Logical<TextBox>(composer).Single(b => b.IsReadOnly && b.MaxHeight <= ComposerSurface.CompiledPromptMaxHeight + 1);
                    Assert.Equal(composer.Gate.RenderView(composer.Draft).Text, box.Text);
                });
            }

            [Fact]
            public void BareComposer()
            {
                var surface = new ComposerSurface();
                surface.Draft.SetFreeFormText("words");
                Assert.Equal("words", surface.CompiledView);
            }

            [Fact]
            public void BehindAHelper()
            {
                var surface = new ComposerSurface();
                Assert.Equal(string.Empty, Read(surface));
            }

            [Fact]
            public void LayoutOnly()
            {
                using var document = ThreadFixtures.Document(Turns(1));
                var compiled = Logical<TextBox>(document.Composer).Single(box => box.IsReadOnly && box.MaxHeight <= ComposerSurface.CompiledPromptMaxHeight + 1);
                Assert.True(compiled.ActualHeight >= 0);
            }

            [Fact]
            public void GestureDriven()
            {
                ComposerGesture.Drive(fixture, async (window, document) =>
                {
                    ComposerGesture.Type(document.Composer, "words");
                    ComposerGesture.CtrlEnter(document.Composer);
                    Assert.Equal("words", ComposerGesture.Field(document.Composer).Text);
                });
            }

            private static string Read(ComposerSurface surface) => surface.CompiledView;
        }
        """;
}
