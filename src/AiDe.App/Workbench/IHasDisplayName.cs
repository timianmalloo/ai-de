namespace AiDe.App.Workbench;

/// <summary>
/// A surface content element that carries a user-chosen display name, distinct from the model's
/// title. The adapter reads it when projecting a pane's tab caption, so a rename applied to a live
/// session persists across re-renders (which reuse the same content instance, DC-029) without a
/// change to the Core layout model. A null or empty name means "use the model title".
/// </summary>
public interface IHasDisplayName
{
    /// <summary>The user-chosen tab caption, or null/empty to fall back to the model title.</summary>
    string? DisplayName { get; }
}
