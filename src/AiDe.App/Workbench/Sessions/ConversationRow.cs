using System.ComponentModel;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Workbench.Sessions;

/// <summary>
/// One item of a turn's conversation as the reply side binds it (Ruling 82) — a prose, reasoning
/// or tool item of <see cref="TurnView.Items"/>, updated <b>in place</b> when the next snapshot
/// re-derives the item at its position (DS-1 Q10 restated for items: a status change never
/// re-templates, so focus inside a detail survives every snapshot), with the one per-item view
/// state — the disclosure's open flag — living here, never on a container (spike Q13).
/// </summary>
/// <remarks>
/// Every facet is one derivation of <see cref="Item"/> (DM7); the template binds flat paths and
/// re-reads them all on the empty-name raise. An event item never becomes a row: the fold renders
/// those as event lines (<see cref="TurnItem.FoldedEvents"/>).
/// </remarks>
public sealed class ConversationRow : INotifyPropertyChanged
{
    private ConversationItem _item;
    private bool _isOpen;

    public ConversationRow(ConversationItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _item = item;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The projection this row renders. Setting it raises every bound facet at once.</summary>
    public ConversationItem Item
    {
        get => _item;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_item, value) || _item.Equals(value))
            {
                return;
            }

            _item = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }

    /// <summary>The reasoning or detail disclosure's state — the row's, so a recycled container never carries another turn's (Q13).</summary>
    public bool IsOpen
    {
        get => _isOpen;
        set
        {
            if (_isOpen == value)
            {
                return;
            }

            _isOpen = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsOpen)));
        }
    }

    // ── the facets the template binds ──

    public bool IsReasoning => _item is ConversationItem.Reasoning;
    public bool IsTool => _item is ConversationItem.Tool;

    /// <summary>The prose's markdown source, or the thought's plain text.</summary>
    public string Text => _item.Row.Text;

    /// <summary>The tool's kind as a word (<i>read · edit · execute · search</i>); empty when the wire stated none.</summary>
    public string Kind => (_item as ConversationItem.Tool)?.Kind ?? string.Empty;

    public string Title => (_item as ConversationItem.Tool)?.Title ?? string.Empty;

    public ToolStatus Status => (_item as ConversationItem.Tool)?.Status ?? ToolStatus.Interrupted;

    /// <summary><i>running · done · failed · interrupted</i>.</summary>
    public string StatusWord => IsTool ? ConversationItems.StatusWord(Status) : string.Empty;

    public bool IsRunning => Status == ToolStatus.Running && IsTool;

    /// <summary><i>Detail of Read x</i> — the disclosure's name (A11-6: the title verbatim).</summary>
    public string DetailName => "Detail of " + Title;

    /// <summary><i>Detail of Read x, input and result</i> — the opened region's name.</summary>
    public string DetailRegionName => DetailName + ", input and result";

    /// <summary>The detail's text: <c>input</c>, the input, then <c>result</c>, the output — the mockup's pre; an interrupted call says no result came.</summary>
    public string Detail
    {
        get
        {
            if (_item is not ConversationItem.Tool tool)
            {
                return string.Empty;
            }

            var input = tool.Input.Length > 0 ? tool.Input : "(no input)";
            var result = tool.Output.Length > 0 ? tool.Output
                : tool.Status == ToolStatus.Interrupted ? "(no result: the lane ended before the tool answered)"
                : tool.Status == ToolStatus.Running ? string.Empty
                : "(no output)";
            return "input\n" + input + (result.Length > 0 ? "\nresult\n" + result : string.Empty);
        }
    }
}
