using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;
using Entry = AiDe.App.Workbench.SurfaceContentFactory.SurfaceEntry;

namespace AiDe.App.Tests.Kinds;

/// <summary>
/// The WL-S design's D6, as the Owner amended it: every <see cref="Entry"/> subtype, found by reflection, is handled by
/// every reader of the opener. Each reader's arm is a switch expression here whose discard throws
/// <see cref="UnreachableException"/> naming the type, and each reader's real behaviour is checked against its arm. So a
/// fourth subtype fails here, naming each reader that has no arm for it, instead of falling into an <c>is Derived</c>
/// test's "not derived" in silence.
/// </summary>
public sealed class EverySurfaceEntryIsHandledByEveryReaderTests
{
    /// <summary>Every file under <c>src/</c> that names <see cref="Entry"/>: each one is a reader with its arm below.</summary>
    private static readonly string[] Readers = ["CenterEmptyState.cs", "PerspectiveMenu.cs", "SurfaceContentFactory.cs"];

    public static TheoryData<string> Subtypes()
    {
        var data = new TheoryData<string>();
        foreach (var type in SubtypesOf())
        {
            data.Add(type.Name);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Subtypes))]
    public void EachReader_HandlesTheSubtype_WithoutReachingItsUnreachableArm(string subtype)
    {
        var entry = Instance(SubtypesOf().Single(t => t.Name == subtype));

        var unhandled = new (string Reader, Action Arm)[]
            {
                ("PerspectiveMenu", () => PerspectiveMenuDerivesAnOpener(entry)),
                ("CenterEmptyState", () => CenterEmptyStateCountsAViewMenuOffer(entry)),
                ("SurfaceContentFactory", () => FactoryDoor(entry)),
            }
            .Where(r => Record.Exception(r.Arm) is UnreachableException)
            .Select(r => r.Reader)
            .ToList();
        Assert.True(unhandled.Count == 0,
            $"SurfaceEntry.{subtype} reaches the unreachable arm of: {string.Join(", ", unhandled)}. Give each reader its case (D6), then its arm here.");

        // PerspectiveMenu's real behaviour, over one row of this subtype: a derived opener exactly when its arm says so,
        // and Opener refuses every other door with ArgumentException, never another exception.
        var row = new SurfaceContentFactory.SurfaceKind(
            "d6-probe", "D6 probe", "A test-time row.", static (_, _) => new Border(),
            [PerspectiveSet.Factory], SurfaceContentFactory.Instances.Many, entry);
        var derives = PerspectiveMenuDerivesAnOpener(entry);
        Assert.Equal(derives ? 1 : 0, PerspectiveMenu.For(PerspectiveSet.Factory, [row], []).Menus.SelectMany(g => g.Derived).Count());
        var refusal = Record.Exception(() => PerspectiveMenu.Opener(row));
        if (derives)
        {
            Assert.Null(refusal);
        }
        else
        {
            Assert.IsType<ArgumentException>(refusal);
        }
    }

    [Fact]
    public void CenterEmptyState_OffersTheViewMenu_ExactlyWhereItsArmCountsARow()
    {
        foreach (var perspective in PerspectiveSet.All.Where(p => !PerspectiveMenu.Admits(p, AiDe.App.Workbench.Sessions.SessionDocumentSurface.Kind)))
        {
            var copy = CenterEmptyState.CopyFor(perspective, WorkbenchLayout.Default(perspective), _ => false);
            var offered = SurfaceContentFactory.Kinds.Any(k => CenterEmptyStateCountsAViewMenuOffer(k.Entry) && k.Perspectives.Contains(perspective));
            Assert.Equal(offered, copy.Body.Contains("View menu", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void EveryRow_NamesItsDoor_ThroughTheFactorysArm()
    {
        foreach (var row in SurfaceContentFactory.Kinds)
        {
            Assert.Contains(row.Entry.GetType(), SubtypesOf());
            Assert.False(string.IsNullOrWhiteSpace(FactoryDoor(row.Entry)), $"kind '{row.Kind}' names no door");
        }
    }

    [Fact]
    public void TheReadersAreTheOnesThisTestCovers()
    {
        var src = Path.Combine(RepoRoot(), "src");
        var named = Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && File.ReadAllText(f).Contains("SurfaceEntry", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(Readers, named);
    }

    private static bool PerspectiveMenuDerivesAnOpener(Entry entry) => entry switch
    {
        Entry.Derived => true,
        Entry.Verb => false,
        Entry.TurnAction => false,
        _ => throw Unhandled(entry, "PerspectiveMenu"),
    };

    private static bool CenterEmptyStateCountsAViewMenuOffer(Entry entry) => entry switch
    {
        Entry.Derived => true,
        Entry.Verb => false,
        Entry.TurnAction => false,
        _ => throw Unhandled(entry, "CenterEmptyState"),
    };

    private static string FactoryDoor(Entry entry) => entry switch
    {
        Entry.Derived d => "the " + d.Menu + " menu",
        Entry.Verb v => "the verb " + v.CommandId,
        Entry.TurnAction t => "a turn's " + t.Action + " action",
        _ => throw Unhandled(entry, "SurfaceContentFactory"),
    };

    private static UnreachableException Unhandled(Entry entry, string reader) =>
        new($"{reader} has no arm for SurfaceEntry.{entry.GetType().Name}");

    private static IReadOnlyList<Type> SubtypesOf() =>
        [.. typeof(Entry).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Entry))).OrderBy(t => t.Name, StringComparer.Ordinal)];

    /// <summary>One instance of <paramref name="type"/> through its primary constructor: a menu name for a string, an enum's first value.</summary>
    private static Entry Instance(Type type)
    {
        var constructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
        var args = constructor.GetParameters()
            .Select(p => p.ParameterType == typeof(string) ? "_View"
                : p.ParameterType.IsEnum ? Enum.GetValues(p.ParameterType).GetValue(0)
                : p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null)
            .ToArray();
        return (Entry)constructor.Invoke(args);
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
}
