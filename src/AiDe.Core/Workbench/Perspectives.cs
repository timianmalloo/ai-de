namespace AiDe.Core.Workbench;

/// <summary>
/// One perspective — a place to work (spec <c>workbench-loops-ux</c> §B1; ADR-0045). A row of data,
/// never a class with behaviour: the shell projects it, the rail lists it, the catalog derives its
/// command from it. Every row's body is a docking host (ADR-0045 §1(b): the full-window body is
/// retired, and with it the body column — <c>docs/design/workbench-loops-wl-s.md</c> D5).
/// </summary>
/// <param name="Id">The stable id (<c>factory</c> · <c>review</c> · <c>understand</c>); what the switch event and the layout slot name.</param>
/// <param name="Title">The word the rail, the menu and the window title show.</param>
/// <param name="Order">1-based position in the rail and the digit of the bound gesture (<c>Ctrl+1/2/3</c>, D1's decision).</param>
/// <param name="CommandId">The catalog command that activates it — derived into <see cref="WorkbenchCommandCatalog.All"/> from this row.</param>
/// <param name="Description">What the perspective is for, as the palette's hint says it.</param>
/// <param name="Landing">
/// Where focus lands after a switch to this perspective (spec §C5; DESIGN.md's landing row): the
/// zone whose active tab takes focus — Factory lands on its Left (the session document, Ruling 83),
/// Review and Understand on their Center. A zone the arrangement has emptied or collapsed falls back
/// to the Center's active tab (the empty copy when the Center is empty), then the body's active
/// surface. Stated on the row because the view's own notion of "active" after a body is first
/// parented is whichever pane control realized last (measured, SH-4.1), which is not a landing
/// anyone chose.
/// </param>
public sealed record Perspective(
    string Id,
    string Title,
    int Order,
    string CommandId,
    string Description,
    ZoneId Landing)
{
    /// <summary>The bound single-stroke gesture, spelled from the row (US-C10; <c>Ctrl+</c> the rail digit).</summary>
    public string Gesture => $"Ctrl+{Order}";
}

/// <summary>
/// The closed Perspective set (ADR-0030's mechanism, its rows superseded by ADR-0045 §1): three rows
/// in rail order, and the order a kind-open the active perspective does not admit is routed in.
/// Pure data; constructible in any test.
/// </summary>
/// <remarks>
/// <b>Why a static row set and not an enum.</b> The perspective commands must be catalog rows in
/// Core, derived from the set — or the catalog carries a hand-listed copy that a new perspective
/// would silently miss. Rows in Core let the catalog, the rail, the menu radio, the palette and the
/// routed open all read ONE definition; ADR-0045's re-row was exactly that: the rows here changed,
/// and every derived surface followed. <b>Tests</b> is a reserved name with no row (Ruling 54).
/// </remarks>
public static class PerspectiveSet
{
    // simplify: Factory lands on the Left, where the session document docks (Ruling 83), until the
    // notebook kind exists. Ceiling: ADR-0045 §1(a)'s "Factory: Center = notebook". Upgrade trigger:
    // T11 docks `notebook` in the Center and flips this to ZoneId.Center in the same change
    // (docs/design/workbench-loops-wl-s.md D2).
    public static Perspective Factory { get; } = new(
        "factory", "Factory", 1, "perspective.factory",
        "Make the change: session documents, terminals, prompt drafts, and the fleet's coordination panes. The perspective the tool starts in.",
        Landing: ZoneId.Left);

    /// <summary>
    /// Review a change against its intent. It admits no surface kind until WL-5 lands the change
    /// packet (the WL-S design's D3): an honest empty host, never a borrowed kind.
    /// </summary>
    public static Perspective Review { get; } = new(
        "review", "Review", 2, "perspective.review",
        "Review a change against its intent. Change packets, test evidence and the rigor backlog open here as they arrive.",
        Landing: ZoneId.Center);

    public static Perspective Understand { get; } = new(
        "understand", "Understand", 3, "perspective.understand",
        "Understand the code and its architecture: the graph, the tree, the evidence, and the class, sequence, context and join views.",
        Landing: ZoneId.Center);

    /// <summary>The rows, in rail order: Factory · Review · Understand (spec §B1).</summary>
    public static IReadOnlyList<Perspective> All { get; } = [Factory, Review, Understand];

    /// <summary>The perspective the shell starts in (US-C1).</summary>
    public static Perspective Initial => Factory;

    /// <summary>
    /// Where a kind-open the active perspective does not admit is routed (US-C3): the first of these
    /// that admits the kind. "The reading host wins a shared kind" (ADR-0045 §1(a);
    /// <c>note-addendum-c-inadmissible-kind-routing</c>): the requester of a shared kind is a reading
    /// surface, so Understand is tried first, then Review, then Factory.
    /// </summary>
    public static IReadOnlyList<Perspective> RoutingOrder { get; } = [Understand, Review, Factory];

    /// <summary>The row whose command has this id, or null — the one lookup the controller needs.</summary>
    public static Perspective? ByCommandId(string commandId) =>
        All.FirstOrDefault(p => string.Equals(p.CommandId, commandId, StringComparison.Ordinal));
}
