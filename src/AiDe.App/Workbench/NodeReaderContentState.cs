namespace AiDe.App.Workbench;

/// <summary>
/// What the Explorer reader's content area is showing (Ruling 93). One value per rendered state,
/// so a test reads the state the operator sees rather than inferring it from the visual tree.
/// </summary>
public enum NodeReaderContentState
{
    /// <summary>No content asked for yet: the area names the gesture that renders it.</summary>
    Idle,

    /// <summary>The node-content query is in flight.</summary>
    Loading,

    /// <summary>Source, read-only and highlighted by the authority's language tag.</summary>
    Code,

    /// <summary>Plain text (a .txt/.log node), read-only.</summary>
    Text,

    /// <summary>Markdown rendered by the WPF prose renderer; links are text, never controls.</summary>
    Markdown,

    /// <summary>HTML rendered in the sandbox host — script disabled, no navigation, no network.</summary>
    Html,

    /// <summary>HTML shown as highlighted source because the sandbox could not be asserted.</summary>
    HtmlFallback,

    /// <summary>No inline content: the authority's shortfall sentence, verbatim.</summary>
    None,

    /// <summary>The query threw; the area says so with the reason.</summary>
    Failed,
}
