using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AiDe.Core.Watcher;
using Xunit.Abstractions;

namespace AiDe.Core.Tests.Watcher;

[Trait("Platform", "Linux")]
public sealed class PortableRegistrationPublicationTests(ITestOutputHelper output)
{
    [Fact]
    public void SameOwnerUpdatesTheOrdinaryJson()
    {
        using var fixture = new Fixture();
        var first = RegistrationPublisher.Publish(fixture.Root, Notice("session-1", "first"));
        var second = RegistrationPublisher.Publish(fixture.Root, Notice("session-1", "second"));
        Assert.Equal(first, second);
        using var json = JsonDocument.Parse(File.ReadAllBytes(second));
        Assert.Equal("second", json.RootElement.GetProperty("reason").GetString());
        Assert.Equal(RegistrationPublisher.GeneratedBy,
            json.RootElement.GetProperty(RegistrationPublisher.GeneratedByField).GetString());
        Assert.Empty(Directory.EnumerateFiles(fixture.Root, "*.jsonl"));
        Assert.Empty(Directory.EnumerateFiles(fixture.Registration, ".notice-*"));
    }

    [Theory]
    [InlineData("root")]
    [InlineData("registration")]
    [InlineData("target")]
    [InlineData("ancestor")]
    public void SymlinkCannotRedirectPublication(string position)
    {
        using var fixture = new Fixture();
        var sentinel = Path.Combine(fixture.Outside, "session-1.json");
        File.WriteAllText(sentinel, "{\"sessionId\":\"session-1\",\"reason\":\"outside\"}");
        var expected = File.ReadAllBytes(sentinel);
        var root = fixture.Root;
        switch (position)
        {
            case "root":
                Directory.Delete(fixture.Root);
                Directory.CreateSymbolicLink(fixture.Root, fixture.Outside);
                break;
            case "registration":
                Directory.CreateSymbolicLink(fixture.Registration, fixture.Outside);
                break;
            case "target":
                Directory.CreateDirectory(fixture.Registration);
                File.CreateSymbolicLink(Path.Combine(fixture.Registration, "session-1.json"), sentinel);
                break;
            case "ancestor":
                Directory.CreateSymbolicLink(Path.Combine(fixture.Root, "alias"), fixture.Outside);
                root = Path.Combine(fixture.Root, "alias", "new-child");
                break;
        }
        var failure = Record.Exception(() => RegistrationPublisher.Publish(root, Notice("session-1")));
        output.WriteLine($"position={position}; outsideEntries={Directory.GetFileSystemEntries(fixture.Outside, "*", SearchOption.AllDirectories).Length}; sentinelUnchanged={expected.AsSpan().SequenceEqual(File.ReadAllBytes(sentinel))}");
        var error = Assert.IsType<WatcherException>(failure);
        Assert.Equal("COORD_NATIVE_CONTEXT", error.Code);
        Assert.Equal(expected, File.ReadAllBytes(sentinel));
        Assert.Single(Directory.EnumerateFileSystemEntries(fixture.Outside));
    }

    [Theory]
    [InlineData("a:b", "a?b")]
    [InlineData("a/b", "a-b")]
    [InlineData("session-1", "other-session")]
    public void KnownDifferentOwnerIsNeverOverwritten(string session, string otherOwner)
    {
        using var fixture = new Fixture();
        Directory.CreateDirectory(fixture.Registration);
        var path = Path.Combine(fixture.Registration, StandingPublisher.FileNameFor(session));
        File.WriteAllText(path, JsonSerializer.Serialize(new { sessionId = otherOwner, reason = "retained" }));
        var bytes = File.ReadAllBytes(path);
        Assert.Throws<WatcherException>(() => RegistrationPublisher.Publish(fixture.Root, Notice(session)));
        Assert.Equal(bytes, File.ReadAllBytes(path));
        Assert.Empty(Directory.EnumerateFiles(fixture.Registration, ".notice-*"));
    }

    [Theory]
    [InlineData("relative")]
    [InlineData("/")]
    [InlineData("//server/share")]
    [InlineData("\\\\?\\C:\\device")]
    [InlineData("nul\0path")]
    [InlineData("/dev/NUL")]
    public void InvalidRootsRefuseBeforeAnyFileEffect(string root)
    {
        var error = Assert.Throws<WatcherException>(() => RegistrationPublisher.Publish(root, Notice("session-1")));
        Assert.StartsWith("COORD_", error.Code, StringComparison.Ordinal);
        Assert.DoesNotContain(root, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FailedTargetClosesAllDescriptorsAndLeavesNoTemporaryFiles()
    {
        using var fixture = new Fixture();
        Directory.CreateDirectory(Path.Combine(fixture.Registration, "session-1.json"));
        Assert.Throws<WatcherException>(() => RegistrationPublisher.Publish(fixture.Root, Notice("session-1")));
        var before = Directory.GetFileSystemEntries("/proc/self/fd").Length;
        for (var attempt = 0; attempt < 16; attempt++)
            Assert.Throws<WatcherException>(() => RegistrationPublisher.Publish(fixture.Root, Notice("session-1")));
        Assert.Equal(before, Directory.GetFileSystemEntries("/proc/self/fd").Length);
        Assert.Empty(Directory.EnumerateFiles(fixture.Registration, ".notice-*"));
        Directory.Delete(Path.Combine(fixture.Registration, "session-1.json"));
        Assert.True(File.Exists(RegistrationPublisher.Publish(fixture.Root, Notice("session-1"))));
    }

    [Fact]
    public void OtherProcessReplacingRootCannotSelectTheWriteDirectory()
    {
        using var fixture = new Fixture();
        using var publication = RegistrationPublicationUnix.Open(fixture.Root);
        var held = Path.Combine(fixture.Base, "held");
        Run("/bin/mv", fixture.Root, held);
        Run("/bin/ln", "-s", fixture.Outside, fixture.Root);
        publication.Publish("session-1.json", "session-1", Bytes("session-1"));
        Assert.True(File.Exists(Path.Combine(held, "registration", "session-1.json")));
        Assert.Empty(Directory.EnumerateFileSystemEntries(fixture.Outside));
    }

    [Fact]
    public void CompetingScopeRefusesAndLockIsReleasedOnDispose()
    {
        using var fixture = new Fixture();
        using (RegistrationPublicationUnix.Open(fixture.Root))
            Assert.Throws<WatcherException>(() => RegistrationPublicationUnix.Open(fixture.Root));
        Assert.True(File.Exists(RegistrationPublisher.Publish(fixture.Root, Notice("session-1"))));
    }

    private static RegistrationNotice Notice(string session, string reason = "because") =>
        new(session, "sent", "used", reason);

    private static byte[] Bytes(string session) =>
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { sessionId = session, reason = "because" }));

    private static void Run(string executable, params string[] arguments)
    {
        var start = new ProcessStartInfo(executable) { UseShellExecute = false };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        Assert.True(process.WaitForExit(5000));
        Assert.Equal(0, process.ExitCode);
    }

    private sealed class Fixture : IDisposable
    {
        internal string Base { get; } = Path.Combine(Path.GetTempPath(), "aide-p25-" + Guid.NewGuid().ToString("N"));
        internal string Root => Path.Combine(Base, "root");
        internal string Outside => Path.Combine(Base, "outside");
        internal string Registration => Path.Combine(Root, RegistrationPublisher.DirectoryName);

        internal Fixture()
        {
            Directory.CreateDirectory(Root);
            Directory.CreateDirectory(Outside);
        }

        public void Dispose() => Directory.Delete(Base, recursive: true);
    }
}
