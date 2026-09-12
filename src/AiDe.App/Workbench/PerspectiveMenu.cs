using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>
/// One menu of the derived contribution: its catalog rows, then its derived "New/Show &lt;Title&gt;"
/// rows, in the order the menu shows them.
/// </summary>
public sealed record PerspectiveMenuGroup(
    string Menu,
    IReadOnlyList<WorkbenchCommand> Catalog,
    IReadOnlyList<WorkbenchCommand> Derived)
{
    public IEnumerable<WorkbenchCommand> Commands => Catalog.Concat(Derived);
}

/// <summary>
/// The menu contribution of one perspective — what the menu bar, the palette and the rail read —
/// DERIVED from the join of the command catalog and the kind rows' allow-lists (ADR-0030 rule 3;
/// Addendum C §B3, Ruling 55b). Never a per-perspective list.
/// </summary>
/// <remarks>
/// <para><b>The rule, in three sets.</b> (1) Perspective-independent entries: every catalog command
/// whose <see cref="CommandScope"/> is <see cref="CommandScopeKind.Global"/> — the entry verbs and
/// workspace verbs in File, the perspective radio and the status line in View, the diagnostics
/// report in Help. (2) Body-conditional entries: a catalog command whose scope names what it needs
/// — a docking host, or an admitted surface kind — evaluated against the active perspective's body
/// and allow-list. (3) Allow-list-derived entries: one per admitted kind whose
/// row says its opener is derived — "New &lt;Title&gt;" for a many-instance kind, "Show
/// &lt;Title&gt;" for a one-instance kind — under the menu the row names.</para>
///
/// <para><b>Why one function and not a table in the builder.</b> The US-C4 mutation test appends a
/// test-time kind row admitted only by Architecture and asserts its entry appears there and nowhere
/// else with no edit to the builder. A table cannot pass that; a join can.</para>
///
/// <para><b>Structurally inapplicable is absent; transiently unavailable is disabled.</b> A command
/// this function does not offer is not in the menu or the palette at all (PS-M3). Whether an
/// offered command can run right now (a focused pane, an open workspace) is the controller's answer
/// when it runs, never a second filter here.</para>
/// </remarks>
public sealed class PerspectiveMenu
{
    /// <summary>The six top-level names, in bar order (DESIGN.md PS-M1).</summary>
    public static IReadOnlyList<string> MenuOrder { get; } = ["_File", "_Edit", "_View", "_Window", "_Prompt", "_Help"];

    /// <summary>The id prefix of a derived "New &lt;Title&gt;" opener; the kind follows it.</summary>
    public const string NewCommandPrefix = "surface.new.";

    /// <summary>The id prefix of a derived "Show &lt;Title&gt;" opener; the kind follows it.</summary>
    public const string ShowCommandPrefix = "surface.show.";

    private PerspectiveMenu(Perspective perspective, IReadOnlyList<PerspectiveMenuGroup> menus)
    {
        Perspective = perspective;
        Menus = menus;
        Commands = [.. menus.SelectMany(m => m.Commands)];
    }

    public Perspective Perspective { get; }

    /// <summary>The non-empty menus, in bar order. An empty menu is absent, never rendered empty.</summary>
    public IReadOnlyList<PerspectiveMenuGroup> Menus { get; }

    /// <summary>Every command the menu bar offers, flat, in the order the bar shows them — the palette's rows (US-C4 b3).</summary>
    public IReadOnlyList<WorkbenchCommand> Commands { get; }

    /// <summary>The contribution of <paramref name="perspective"/> over the product's rows.</summary>
    public static PerspectiveMenu For(Perspective perspective) =>
        For(perspective, SurfaceContentFactory.Kinds, WorkbenchCommandCatalog.All);

    /// <summary>
    /// The contribution of <paramref name="perspective"/> over the given rows — the seam the
    /// mutation test uses to append a kind row without editing the product's list.
    /// </summary>
    public static PerspectiveMenu For(
        Perspective perspective,
        IReadOnlyList<SurfaceContentFactory.SurfaceKind> kinds,
        IReadOnlyList<WorkbenchCommand> catalog)
    {
        ArgumentNullException.ThrowIfNull(perspective);
        ArgumentNullException.ThrowIfNull(kinds);
        ArgumentNullException.ThrowIfNull(catalog);

        var menus = new List<PerspectiveMenuGroup>();

        foreach (var menu in MenuOrder)
        {
            var fromCatalog = catalog
                .Where(c => string.Equals(c.Menu, menu, StringComparison.Ordinal) && Offered(perspective, c.Scope, kinds))
                .ToList();

            var derived = kinds
                .Where(k => k.Entry is SurfaceContentFactory.SurfaceEntry.Derived d
                    && string.Equals(d.Menu, menu, StringComparison.Ordinal)
                    && k.Perspectives.Contains(perspective))
                .Select(Opener)
                .ToList();

            if (fromCatalog.Count > 0 || derived.Count > 0)
            {
                menus.Add(new PerspectiveMenuGroup(menu, fromCatalog, derived));
            }
        }

        return new PerspectiveMenu(perspective, menus);
    }

    /// <summary>
    /// The derived opener of a kind row: "New &lt;title&gt;" for a many-instance kind, "Show
    /// &lt;title&gt;" for a one-instance kind — the title lower-cased after the verb, in the
    /// catalog's own voice ("New terminal", "Close surface"). No gesture: nothing binds one, and an
    /// unbound chord is shown nowhere (PS-M4).
    /// </summary>
    /// <exception cref="ArgumentException">The row's entry is a catalog verb — nothing is derived for it.</exception>
    public static WorkbenchCommand Opener(SurfaceContentFactory.SurfaceKind row)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (row.Entry is not SurfaceContentFactory.SurfaceEntry.Derived entry)
        {
            throw new ArgumentException($"kind '{row.Kind}' is opened by a catalog verb; it has no derived opener", nameof(row));
        }

        var noun = char.ToLowerInvariant(row.Title[0]) + row.Title[1..];
        var (prefix, verb, hint) = row.Instances == SurfaceContentFactory.Instances.Many
            ? (NewCommandPrefix, "New", $"Opens a new {noun} pane. {row.Summary}")
            : (ShowCommandPrefix, "Show", $"Shows the {noun} pane, opening it if it is not open. {row.Summary}");

        return new WorkbenchCommand(
            prefix + row.Kind,
            $"{verb} {noun}",
            Gesture: string.Empty,
            nameof(LayoutOperation.AddSurface),
            hint,
            entry.Menu,
            CommandScope.Admits(row.Kind));
    }

    /// <summary>
    /// Splits a derived opener id into its verb and kind; false for any other id. What the
    /// controller reads so one case handles every derived entry.
    /// </summary>
    public static bool TryParseOpener(string? commandId, out bool showExisting, out string kind)
    {
        showExisting = false;
        kind = string.Empty;

        if (string.IsNullOrEmpty(commandId))
        {
            return false;
        }

        if (commandId.StartsWith(ShowCommandPrefix, StringComparison.Ordinal))
        {
            showExisting = true;
            kind = commandId[ShowCommandPrefix.Length..];
            return kind.Length > 0;
        }

        if (commandId.StartsWith(NewCommandPrefix, StringComparison.Ordinal))
        {
            kind = commandId[NewCommandPrefix.Length..];
            return kind.Length > 0;
        }

        return false;
    }

    /// <summary>Whether <paramref name="perspective"/> admits <paramref name="kind"/> into its body (§A7).</summary>
    public static bool Admits(Perspective perspective, string kind) =>
        Admits(perspective, kind, SurfaceContentFactory.Kinds);

    public static bool Admits(Perspective perspective, string kind, IReadOnlyList<SurfaceContentFactory.SurfaceKind> kinds) =>
        kinds.Any(k => string.Equals(k.Kind, kind, StringComparison.Ordinal) && k.Perspectives.Contains(perspective));

    /// <summary>
    /// Where a kind-open lands (US-C3): the active perspective if it admits the kind, else the
    /// first of <see cref="PerspectiveSet.RoutingOrder"/> that does, else null — a kind no
    /// perspective admits, which the build test makes unreachable.
    /// </summary>
    public static Perspective? Resolve(string kind, Perspective active) =>
        Resolve(kind, active, SurfaceContentFactory.Kinds);

    public static Perspective? Resolve(string kind, Perspective active, IReadOnlyList<SurfaceContentFactory.SurfaceKind> kinds)
    {
        ArgumentNullException.ThrowIfNull(active);

        if (Admits(active, kind, kinds))
        {
            return active;
        }

        return PerspectiveSet.RoutingOrder.FirstOrDefault(p => Admits(p, kind, kinds));
    }

    /// <summary>The offered commands whose title or hint contains <paramref name="term"/> — the palette's filter.</summary>
    public IEnumerable<WorkbenchCommand> Search(string term) => WorkbenchCommandCatalog.Search(Commands, term);

    // A three-op interpreter over the rule SHAPES a scope can take — never a switch over a
    // perspective or a kind, so adding either needs no case here (the Patterns Expert's reading).
    private static bool Offered(Perspective perspective, CommandScope scope, IReadOnlyList<SurfaceContentFactory.SurfaceKind> kinds) =>
        scope.Kind switch
        {
            CommandScopeKind.Global => true,
            CommandScopeKind.DockHost => perspective.Body == PerspectiveBody.DockHost,
            CommandScopeKind.Admits => Admits(perspective, scope.SurfaceKind ?? string.Empty, kinds),
            _ => false,
        };
}
