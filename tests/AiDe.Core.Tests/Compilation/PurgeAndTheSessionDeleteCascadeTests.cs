using System.Text.Json.Nodes;
using AiDe.Core.PromptCompilation;
using AiDe.Core.Sessions;

namespace AiDe.Core.Tests.PromptCompilation;

/// <summary>
/// ADR-0034 test 5 (US-D13): purge deletes <c>envelope-events.jsonl</c> and nothing else; a
/// traversal, a non-segment id and a junctioned session directory are refused before any file is
/// touched; the Session aggregate's own delete cascades to the envelope file by containment — first
/// acquiring it exclusively (refused whole while a writer holds it), deleting it under the held
/// handle, then removing the siblings.
/// </summary>
public sealed class PurgeAndTheSessionDeleteCascadeTests : IDisposable
{
    private readonly string _workspace = Path.Combine(Path.GetTempPath(), "aide-purge-" + Guid.NewGuid().ToString("n")[..8]);
    private readonly SessionConfigStore _sessions;
    private readonly SessionConfig _config;

    public PurgeAndTheSessionDeleteCascadeTests()
    {
        Directory.CreateDirectory(_workspace);
        _sessions = new SessionConfigStore(_workspace, SessionId.New(new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero)));
        _config = _sessions.Create("payments", "w-1", ["claude-code"], DateTimeOffset.UnixEpoch);
    }

    public void Dispose()
    {
        try
        {
            // A junction fixture is removed as a link (never recursed into): the target's files are
            // not the junction's to delete.
            foreach (var link in Directory.Exists(SessionPaths.SessionsRoot(_workspace))
                         ? Directory.EnumerateDirectories(SessionPaths.SessionsRoot(_workspace)).Where(d => new DirectoryInfo(d).Attributes.HasFlag(FileAttributes.ReparsePoint))
                         : [])
            {
                Directory.Delete(link, recursive: false);
            }

            Directory.Delete(_workspace, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        GC.SuppressFinalize(this);
    }

    private string SessionDir => SessionPaths.SessionDirectory(_workspace, _config.SessionId);
    private string EnvelopeFile => Path.Combine(SessionDir, EnvelopeStore.FileName);

    private void WriteTwoEnvelopes()
    {
        using var store = EnvelopeStore.Open(SessionDir, new FixedTime(new DateTimeOffset(2026, 9, 12, 11, 0, 0, TimeSpan.Zero)));
        store.Append(new Opened("e1", "one", _config.SessionId, "claude-code", CompileModes.MechanicalOnly, null, PreCompile.ConstantsFor("1")));
        store.Append(new Opened("e2", "two", _config.SessionId, "claude-code", CompileModes.MechanicalOnly, null, PreCompile.ConstantsFor("1")));
    }

    [Fact]
    public void PurgeRemovesTheEnvelopeFileAndLeavesSessionJsonTheEventsFileAndEverySibling()
    {
        WriteTwoEnvelopes();
        File.WriteAllText(Path.Combine(SessionDir, "layout-host-a.json"), "{}");

        var plan = EnvelopePurge.Resolve(_workspace, _config.SessionId);

        // The confirmation names an identity, never a count alone (DC-120).
        Assert.Equal("payments", plan.Name);
        Assert.Equal(_config.SessionId, plan.SessionId);
        Assert.Equal(Path.GetFullPath(_workspace), plan.WorkspaceRoot);
        Assert.Equal(Path.GetFullPath(EnvelopeFile), plan.ResolvedFilePath);
        Assert.Equal(2, plan.EnvelopeCount);
        Assert.Equal(new DateTimeOffset(2026, 9, 12, 11, 0, 0, TimeSpan.Zero), plan.NewestAt);

        EnvelopePurge.Execute(plan);

        Assert.False(File.Exists(EnvelopeFile));
        Assert.True(File.Exists(SessionPaths.SessionFile(_workspace, _config.SessionId)));
        Assert.True(File.Exists(SessionPaths.EventsFile(_workspace, _config.SessionId)));
        Assert.True(File.Exists(Path.Combine(SessionDir, "layout-host-a.json")));
        Assert.True(Directory.Exists(SessionDir));
    }

    [Fact]
    public void PurgeOfASessionWithNoHistoryResolvesToZeroEnvelopesAndNoNewestAt()
    {
        var plan = EnvelopePurge.Resolve(_workspace, _config.SessionId);
        Assert.Equal(0, plan.EnvelopeCount);
        Assert.Null(plan.NewestAt);
        Assert.False(plan.FileExists);
        EnvelopePurge.Execute(plan);   // nothing to remove is not an error
        Assert.True(File.Exists(SessionPaths.SessionFile(_workspace, _config.SessionId)));
    }

    [Theory]
    [InlineData("..\\..")]
    [InlineData("../..")]
    [InlineData("not-a-session-id")]
    [InlineData("20260912T100000Z-0000aaaa/../20260912T100000Z-0000bbbb")]
    [InlineData("")]
    public void PurgeOfATraversalOrANonSegmentIdIsRefusedBeforeAnyFileIsTouched(string id)
    {
        WriteTwoEnvelopes();
        var before = Directory.EnumerateFileSystemEntries(_workspace, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToList();

        var refused = Assert.Throws<EnvelopeStoreException>(() => EnvelopePurge.Resolve(_workspace, id));
        Assert.Equal(EnvelopeStoreErrorCodes.PurgeRefused, refused.Code);

        Assert.Equal(before, Directory.EnumerateFileSystemEntries(_workspace, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal));
    }

    /// <summary>A junctioned session directory is refused — junctions and symlinks are not followed (a junction fixture).</summary>
    [Fact]
    public void PurgeOfAJunctionedSessionDirectoryIsRefusedBeforeAnyFileIsTouched()
    {
        var elsewhere = Path.Combine(_workspace, "elsewhere");
        Directory.CreateDirectory(elsewhere);
        File.WriteAllText(Path.Combine(elsewhere, EnvelopeStore.FileName), "{}\n");
        var junctionId = SessionId.New(new DateTimeOffset(2026, 9, 12, 12, 0, 0, TimeSpan.Zero));
        var junction = SessionPaths.SessionDirectory(_workspace, junctionId);
        Directory.CreateDirectory(SessionPaths.SessionsRoot(_workspace));

        CreateJunction(junction, elsewhere);
        Assert.True(new DirectoryInfo(junction).Attributes.HasFlag(FileAttributes.ReparsePoint), "the fixture is a reparse point");

        var refused = Assert.Throws<EnvelopeStoreException>(() => EnvelopePurge.Resolve(_workspace, junctionId));
        Assert.Equal(EnvelopeStoreErrorCodes.PurgeRefused, refused.Code);
        Assert.Contains("junction", refused.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(Path.Combine(elsewhere, EnvelopeStore.FileName)));
    }

    [Fact]
    public void PurgeWhileAWriterHoldsTheFileIsRefusedVisibly()
    {
        using var writer = EnvelopeStore.Open(SessionDir);
        var refused = Assert.Throws<EnvelopeStoreException>(() => EnvelopePurge.Resolve(_workspace, _config.SessionId));
        Assert.Equal(EnvelopeStoreErrorCodes.HeldByAnotherWriter, refused.Code);
        Assert.True(File.Exists(EnvelopeFile));
    }

    // ── the Session aggregate's own delete: containment ──

    [Fact]
    public void TheSessionDeleteRemovesTheDirectoryIncludingTheEnvelopeFileAndNothingOutsideIt()
    {
        WriteTwoEnvelopes();
        var sibling = new SessionConfigStore(_workspace, SessionId.New(new DateTimeOffset(2026, 9, 12, 13, 0, 0, TimeSpan.Zero)));
        var other = sibling.Create("other", "w-1", ["claude-code"], DateTimeOffset.UnixEpoch);
        var outside = Path.Combine(_workspace, "README.md");
        File.WriteAllText(outside, "unchanged");
        var otherFile = SessionPaths.SessionFile(_workspace, other.SessionId);
        var otherBytes = File.ReadAllBytes(otherFile);

        _sessions.Delete();

        Assert.False(File.Exists(EnvelopeFile));
        Assert.False(Directory.Exists(SessionDir));
        Assert.Equal("unchanged", File.ReadAllText(outside));
        Assert.Equal(otherBytes, File.ReadAllBytes(otherFile));
    }

    [Fact]
    public void TheSessionDeleteWhileAWriterHoldsTheEnvelopeFileIsRefusedWholeAndNothingIsRemoved()
    {
        WriteTwoEnvelopes();
        using var writer = EnvelopeStore.Open(SessionDir);

        var refused = Assert.Throws<EnvelopeStoreException>(() => _sessions.Delete());
        Assert.Equal(EnvelopeStoreErrorCodes.HeldByAnotherWriter, refused.Code);

        Assert.True(File.Exists(EnvelopeFile));
        Assert.True(File.Exists(SessionPaths.SessionFile(_workspace, _config.SessionId)));
        Assert.True(File.Exists(SessionPaths.EventsFile(_workspace, _config.SessionId)));
    }

    /// <summary>
    /// The delete's ordering is observed: the envelope file is gone before any sibling is removed.
    /// A sibling held open makes the recursive delete fail — and at that point the envelope file is
    /// already gone while <c>session.json</c> is still there, which is the order ADR-0034 rule 6
    /// requires (acquire → delete under the handle → siblings).
    /// </summary>
    [Fact]
    public void TheEnvelopeFileIsGoneBeforeAnySiblingIsRemoved()
    {
        WriteTwoEnvelopes();
        using var held = new FileStream(SessionPaths.EventsFile(_workspace, _config.SessionId), FileMode.Open, FileAccess.Read, FileShare.None);

        Assert.ThrowsAny<IOException>(() => _sessions.Delete());

        Assert.False(File.Exists(EnvelopeFile));
        Assert.True(File.Exists(SessionPaths.EventsFile(_workspace, _config.SessionId)));
    }

    /// <summary>A directory junction (no privilege needed on Windows) or a symlink elsewhere — the fixture the refusal is proven against.</summary>
    private static void CreateJunction(string link, string target)
    {
        if (!OperatingSystem.IsWindows())
        {
            Directory.CreateSymbolicLink(link, target);
            return;
        }

        var mklink = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{target}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        })!;
        mklink.WaitForExit();
        Assert.True(mklink.ExitCode == 0, "mklink /J failed: " + mklink.StandardError.ReadToEnd());
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
