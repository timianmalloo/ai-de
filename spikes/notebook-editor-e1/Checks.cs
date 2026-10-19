using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;

namespace NotebookEditorE1Spike;

internal sealed record CheckResult(string Name, bool Pass, string Value, string Method, IReadOnlyList<string> Detail);

/// <summary>
/// The six ADR-0043 §7 floor items plus the IME negative control. Each check sets its document
/// through the probe, then edits ONLY through native input, and asserts on bytes (SHA-256).
/// </summary>
internal sealed class Checks(Page page, Window window, IntPtr hwnd)
{
    private const string JapaneseKlid = "00000411";
    private const int HiraganaConversion = 0x0001 | 0x0008; // IME_CMODE_NATIVE | IME_CMODE_FULLSHAPE

    private readonly List<string> _log = [];

    public static readonly string[] All = ["ime", "ime-control", "clipboard", "multiselect", "roundtrip", "latency", "completion-uia"];

    public async Task<CheckResult> RunAsync(string name)
    {
        _log.Clear();
        try
        {
            return name switch
            {
                "ime" => await ImeAsync(useIme: true),
                "ime-control" => await ImeAsync(useIme: false),
                "clipboard" => await ClipboardAsync(),
                "multiselect" => await MultiSelectAsync(),
                "roundtrip" => await RoundTripAsync(),
                "latency" => await LatencyAsync(),
                "completion-uia" => await CompletionUiaAsync(),
                _ => throw new ArgumentOutOfRangeException(nameof(name), name, "unknown check"),
            };
        }
        catch (Exception ex)
        {
            _log.Add(ex.ToString());
            return new CheckResult(name, false, "harness error", "see detail", [.. _log]);
        }
    }

    private void Log(string line) => _log.Add(line);

    private bool Foreground()
    {
        if (Native.Foreground() == hwnd)
        {
            return true;
        }

        window.Activate();
        return Native.BringToFront(hwnd);
    }

    private void RequireForeground()
    {
        if (!Foreground())
        {
            throw new InvalidOperationException("the harness window is not the foreground window; no input was sent");
        }
    }

    private async Task LoadAsync(string text, string mode)
    {
        var info = await page.EvalAsync($"nb.load({Page.Js(text)})");
        await page.EvalAsync($"nb.setMode({Page.Js(mode)})");
        Log($"load: {text.Length} chars, mode {mode}, page reports {info.GetRawText()}");
    }

    /// <summary>A native click puts real focus in the editor; then the caret is placed exactly.</summary>
    private async Task FocusAtAsync(int pos)
    {
        RequireForeground();
        var (x, y) = await page.ScreenPointAsync(0);
        Native.Click(x, y, withControl: false);
        await Task.Delay(150);
        await page.EvalAsync($"nb.setCursor({pos})");
        var focused = await page.EvalAsync("nb.hasFocus()");
        Log($"focus: native click at ({x},{y}); editor hasFocus={focused.GetRawText()}; caret {pos}");
    }

    // ---------------------------------------------------------------- IME

    private async Task<CheckResult> ImeAsync(bool useIme)
    {
        var name = useIme ? "ime" : "ime-control";
        var original = Fixtures.NotebookLf;
        var paragraphEnd = original.IndexOf("spaces   ", StringComparison.Ordinal) + "spaces   ".Length;
        var headingText = original.IndexOf("## Code", StringComparison.Ordinal) + "## ".Length;
        var targets = new[] { ("paragraph end", paragraphEnd), ("rich heading text", headingText) };
        var outcomes = new List<bool>();
        var values = new List<string>();

        foreach (var (label, pos) in targets)
        {
            await LoadAsync(original, "rich");
            await FocusAtAsync(pos);
            await page.EvalAsync("nb.clearEvents()");
            var focus = Native.FocusedWindow();
            var (pid, layoutBefore) = Native.LayoutOf(focus);
            Log($"[{label}] focus hwnd 0x{focus:x} in pid {pid}, layout 0x{layoutBefore:x}");

            var ime = IntPtr.Zero;
            var hkl = IntPtr.Zero;
            var loadedHere = false;
            try
            {
                if (useIme)
                {
                    var preloaded = Native.LoadedLayouts();
                    hkl = Native.LoadLayout(JapaneseKlid);
                    loadedHere = !preloaded.Contains(hkl);
                    Native.RequestLayout(focus, hkl);
                    await Task.Delay(400);
                    var layoutAfter = Native.LayoutOf(focus).Layout;
                    (ime, var open) = Native.OpenIme(focus, HiraganaConversion);
                    Log($"[{label}] loaded 0x{hkl:x} (new this run: {loadedHere}); focus layout now 0x{layoutAfter:x}; IME window 0x{ime:x}, open status {open}");
                    await Task.Delay(200);
                }

                RequireForeground();
                Native.TypeLetters("nihongo");
                await Task.Delay(500);
                Native.Key(Native.VkSpace);
                await Task.Delay(500);
                Native.Key(Native.VkReturn);
                await Task.Delay(500);
            }
            finally
            {
                if (useIme)
                {
                    Native.CloseIme(ime);
                    Native.RequestLayout(focus, layoutBefore);
                    await Task.Delay(300);
                    if (loadedHere)
                    {
                        Log($"[{label}] unloaded 0x{hkl:x}: {Native.UnloadLayout(hkl)}");
                    }

                    Log($"[{label}] focus layout restored to 0x{Native.LayoutOf(focus).Layout:x}");
                }
            }

            var doc = await page.DocAsync();
            var events = await page.EvalAsync("nb.events()");
            var starts = events.EnumerateArray().Count(e => e.GetProperty("type").GetString() == "compositionstart");
            var ends = events.EnumerateArray().Where(e => e.GetProperty("type").GetString() == "compositionend")
                .Select(e => e.GetProperty("data").GetString() ?? "").ToList();
            var intact = doc.Length >= original.Length
                && doc.StartsWith(original[..pos], StringComparison.Ordinal)
                && doc.EndsWith(original[pos..], StringComparison.Ordinal);
            var inserted = intact ? doc.Substring(pos, doc.Length - original.Length) : "<surrounding text changed>";
            Log($"[{label}] composition events: {events.GetArrayLength()} (start {starts}, end [{string.Join(",", ends)}]); inserted '{inserted}'; rest of document intact: {intact}");

            bool ok;
            if (useIme)
            {
                RequireForeground();
                Native.Chord(Native.VkControl, 'Z');
                await Task.Delay(300);
                var undone = await page.DocAsync();
                var oneUndo = Page.Sha(undone) == Page.Sha(original);
                Log($"[{label}] one Ctrl+Z restores the original bytes: {oneUndo}");
                ok = intact && starts >= 1 && ends.Count >= 1 && inserted.Length > 0
                    && inserted.All(c => c > 0x7F) && ends[^1] == inserted && oneUndo;
            }
            else
            {
                // The control: the same keystrokes with no IME must NOT look like a composition.
                ok = intact && starts == 0 && inserted.StartsWith("nihongo", StringComparison.Ordinal);
            }

            outcomes.Add(ok);
            values.Add($"{label}: '{inserted}'");
        }

        var method = useIme
            ? "Microsoft Japanese IME (KLID 00000411) switched onto the browser's focus window via WM_INPUTLANGCHANGEREQUEST, opened via WM_IME_CONTROL, then SendInput VK keys 'nihongo', Space (convert), Enter (commit); then one SendInput Ctrl+Z"
            : "negative control: the same SendInput keys with the user's en-US layout and no IME — must insert ASCII with no composition events";
        return new CheckResult(name, outcomes.All(o => o), string.Join("; ", values), method, [.. _log]);
    }

    // ---------------------------------------------------------------- clipboard

    private async Task<CheckResult> ClipboardAsync()
    {
        var saved = Clipboard.ContainsText() ? Clipboard.GetText() : null;
        try
        {
            await LoadAsync(string.Empty, "rich");
            await FocusAtAsync(0);

            var data = new DataObject();
            data.SetData(DataFormats.UnicodeText, Fixtures.ClipboardText);
            data.SetData(DataFormats.Html, CfHtml(Fixtures.ClipboardHtml));
            Clipboard.SetDataObject(data, copy: true);
            Log($"Windows clipboard set: UnicodeText ({Fixtures.ClipboardText.Length} chars, CRLF) + HTML Format");

            RequireForeground();
            Native.Chord(Native.VkControl, 'V');
            await page.WaitAsync("nb.docLength() > 0", TimeSpan.FromSeconds(3));
            var pasted = await page.DocAsync();
            var expected = Fixtures.ClipboardText.Replace("\r\n", "\n", StringComparison.Ordinal);
            var pasteOk = pasted == expected;
            Log($"paste: doc sha {Page.Sha(pasted)}; expected (CRLF->LF, the editor's line model) sha {Page.Sha(expected)}; equal {pasteOk}; html leaked {pasted.Contains("RICH", StringComparison.Ordinal)}");

            var seq = Native.ClipboardSequence();
            RequireForeground();
            Native.Chord(Native.VkControl, 'A');
            await Task.Delay(150);
            Native.Chord(Native.VkControl, 'C');
            var changed = await Until(() => Native.ClipboardSequence() != seq, TimeSpan.FromSeconds(3));
            var copied = Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;
            var copyOk = changed && copied.Replace("\r\n", "\n", StringComparison.Ordinal) == pasted;
            Log($"copy: clipboard sequence changed {changed}; clipboard text {copied.Length} chars, CRLF pairs {Count(copied, "\r\n")}, byte-identical to the original Windows text {copied == Fixtures.ClipboardText}; equals doc modulo CRLF {copyOk}");

            await LoadAsync(string.Empty, "rich");
            await FocusAtAsync(0);
            RequireForeground();
            Native.Chord(Native.VkControl, 'V');
            await page.WaitAsync("nb.docLength() > 0", TimeSpan.FromSeconds(3));
            var repasted = await page.DocAsync();
            var fixedPoint = Page.Sha(repasted) == Page.Sha(pasted);
            Log($"re-paste of the editor's own copy: sha {Page.Sha(repasted)}; fixed point {fixedPoint}");

            var pass = pasteOk && copyOk && fixedPoint && !pasted.Contains("RICH", StringComparison.Ordinal);
            return new CheckResult("clipboard", pass,
                $"paste={pasteOk}, copy={copyOk}, doc->clipboard->doc fixed point={fixedPoint}, clipboard out CRLF={copied == Fixtures.ClipboardText}",
                "Windows clipboard (UnicodeText + HTML Format) -> SendInput Ctrl+V; SendInput Ctrl+A, Ctrl+C -> Clipboard.GetText; SendInput Ctrl+V into an empty document; SHA-256 compare",
                [.. _log]);
        }
        finally
        {
            if (saved is not null)
            {
                Clipboard.SetText(saved);
            }
        }
    }

    private static string CfHtml(string fragment)
    {
        const string header = "Version:0.9\r\nStartHTML:{0:D10}\r\nEndHTML:{1:D10}\r\nStartFragment:{2:D10}\r\nEndFragment:{3:D10}\r\n";
        const string pre = "<html><body><!--StartFragment-->";
        const string post = "<!--EndFragment--></body></html>";
        var headerLength = string.Format(System.Globalization.CultureInfo.InvariantCulture, header, 0, 0, 0, 0).Length;
        var startFragment = headerLength + Encoding.UTF8.GetByteCount(pre);
        var endFragment = startFragment + Encoding.UTF8.GetByteCount(fragment);
        var endHtml = endFragment + Encoding.UTF8.GetByteCount(post);
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, header, headerLength, endHtml, startFragment, endFragment)
            + pre + fragment + post;
    }

    // ---------------------------------------------------------------- multi-selection

    private async Task<CheckResult> MultiSelectAsync()
    {
        var original = Fixtures.NotebookLf;
        int[] at =
        [
            original.IndexOf("Intro", StringComparison.Ordinal) + 5,
            original.IndexOf("\"b\"", StringComparison.Ordinal),
            original.IndexOf("Last heading", StringComparison.Ordinal),
        ];
        await LoadAsync(original, "rich");
        await FocusAtAsync(0);

        for (var i = 0; i < at.Length; i++)
        {
            RequireForeground();
            var (x, y) = await page.ScreenPointAsync(at[i]);
            Native.Click(x + 1, y, withControl: i > 0);
            await Task.Delay(250);
        }

        var heads = (await page.EvalAsync("nb.selections()")).EnumerateArray().Select(r => r[1].GetInt32()).Order().ToArray();
        var cursorsOk = heads.SequenceEqual(at.Order());
        Log($"cursors after click + 2 Ctrl+clicks: [{string.Join(",", heads)}], wanted [{string.Join(",", at.Order())}]");

        RequireForeground();
        Native.TypeLetters("xyz");
        await Task.Delay(400);
        var edited = await page.DocAsync();
        var expected = original;
        foreach (var p in at.OrderDescending())
        {
            expected = expected.Insert(p, "xyz");
        }

        var editOk = Page.Sha(edited) == Page.Sha(expected);
        Log($"typed 'xyz' at every cursor: sha {Page.Sha(edited)}, expected {Page.Sha(expected)}: {editOk}");

        RequireForeground();
        Native.Chord(Native.VkControl, 'Z');
        await Task.Delay(300);
        var undone = await page.DocAsync();
        var undoOk = Page.Sha(undone) == Page.Sha(original);
        Log($"one Ctrl+Z: sha {Page.Sha(undone)} vs original {Page.Sha(original)}: {undoOk}");

        Native.Chord(Native.VkControl, 'Y');
        await Task.Delay(300);
        var redone = await page.DocAsync();
        var redoOk = Page.Sha(redone) == Page.Sha(expected);
        Log($"one Ctrl+Y: equals the edited bytes {redoOk}");

        return new CheckResult("multiselect", cursorsOk && editOk && undoOk && redoOk,
            $"cursors={cursorsOk}, 3-cursor edit={editOk}, one undo restores exact bytes={undoOk}, one redo={redoOk}",
            "SendInput click + 2 Ctrl+clicks at coordsAtPos (rich projection, cursors in prose, a JSON fence and a heading), SendInput 'xyz', Ctrl+Z, Ctrl+Y; SHA-256 compare",
            [.. _log]);
    }

    // ---------------------------------------------------------------- language/whitespace round-trip

    private async Task<CheckResult> RoundTripAsync()
    {
        var cases = new[] { ("LF", Fixtures.NotebookLf, true), ("CRLF", Fixtures.NotebookCrlf, true), ("mixed", Fixtures.NotebookMixed, false) };
        var outcomes = new List<string>();
        var allOk = true;
        foreach (var (label, text, exact) in cases)
        {
            var want = Page.Sha(text);
            var info = await page.EvalAsync($"nb.load({Page.Js(text)})");
            var disclosed = info.GetProperty("mixed").GetBoolean();
            await page.EvalAsync("nb.setMode('rich')");
            var decorated = (await page.EvalAsync("nb.decorationCount()")).GetInt32();
            var fences = (await page.EvalAsync("nb.fences()")).EnumerateArray().Select(f => f.GetProperty("info").GetString() ?? "").ToArray();
            var fencesOk = fences.SequenceEqual(Fixtures.ExpectedFenceInfo);
            Log($"[{label}] page: {info.GetRawText()}; rich decorations {decorated}; fence info [{string.Join("|", fences)}] expected [{string.Join("|", Fixtures.ExpectedFenceInfo)}]");

            var toggleOk = true;
            for (var i = 0; i < 3; i++)
            {
                await page.EvalAsync("nb.setMode('source')");
                var sourceDecorations = (await page.EvalAsync("nb.decorationCount()")).GetInt32();
                var inSource = Page.Sha(await page.DocAsync());
                await page.EvalAsync("nb.setMode('rich')");
                var inRich = Page.Sha(await page.DocAsync());
                toggleOk &= sourceDecorations == 0 && (!exact || (inSource == want && inRich == want));
            }

            var path = Path.Combine(Path.GetTempPath(), $"nb-e1-{label}.md");
            await File.WriteAllBytesAsync(path, Encoding.UTF8.GetBytes(await page.DocAsync()));
            var savedSha = Page.Sha(await File.ReadAllBytesAsync(path));
            var navigated = await page.NavigateAsync();
            await page.EvalAsync($"nb.load({Page.Js(Encoding.UTF8.GetString(await File.ReadAllBytesAsync(path)))})");
            var reloaded = Page.Sha(await page.DocAsync());
            File.Delete(path);
            Log($"[{label}] 3 toggles source<->rich hold the bytes and source has 0 decorations: {toggleOk}; saved file sha {savedSha}; page reloaded {navigated}; reloaded doc sha {reloaded}; original {want}");

            bool ok;
            if (exact)
            {
                ok = !disclosed && decorated > 0 && fencesOk && toggleOk && savedSha == want && reloaded == want;
            }
            else
            {
                // No single line separator reproduces mixed endings: the floor is disclosure, not silence.
                ok = disclosed && decorated > 0 && fencesOk && toggleOk;
            }

            allOk &= ok;
            outcomes.Add($"{label}: {(ok ? "pass" : "FAIL")}{(exact ? (savedSha == want ? " (byte-exact)" : " (bytes changed)") : (disclosed ? " (disclosed as lossy)" : " (NOT disclosed)"))}");
        }

        return new CheckResult("roundtrip", allOk, string.Join("; ", outcomes),
            "load fixture -> rich (decorations > 0, fence info strings read from the syntax tree) -> 3x source<->rich -> save UTF-8 file -> reload page -> load file; SHA-256 compare at every step",
            [.. _log]);
    }

    // ---------------------------------------------------------------- 100k latency

    private async Task<CheckResult> LatencyAsync()
    {
        var text = Fixtures.Large();
        await LoadAsync(text, "rich");
        var mid = text.IndexOf("\n\n", text.Length / 2, StringComparison.Ordinal);
        var start = text.IndexOf("\n\n", 20, StringComparison.Ordinal);
        var positions = new[] { ("near start", start), ("middle", mid), ("end", text.Length) };
        var runs = new List<LatencyRun>();
        foreach (var (label, pos) in positions)
        {
            await FocusAtAsync(pos);
            var run = await LatencyHarness.RunAsync(page, label, keys: 150, TimeSpan.FromMilliseconds(50), Foreground);
            runs.Add(run);
            Log($"[{label}] keys {run.Keys}, samples {run.PageMs.Length}, doc grew {run.DocDelta}; page p50 {LatencyHarness.Percentile(run.PageMs, 50):F1} p95 {LatencyHarness.Percentile(run.PageMs, 95):F1} max {run.PageMs.DefaultIfEmpty(double.NaN).Max():F1} ms; host p95 {LatencyHarness.Percentile(run.HostMs, 95):F1} ms; event-timing entries >=16ms {run.EventTimingMs.Length}, max {run.EventTimingMs.DefaultIfEmpty(0).Max():F0}");
        }

        var all = runs.SelectMany(r => r.PageMs).ToArray();
        var host = runs.SelectMany(r => r.HostMs).ToArray();
        var keys = runs.Sum(r => r.Keys);
        var complete = all.Length == keys && runs.All(r => r.DocDelta == r.Keys);
        var p95 = LatencyHarness.Percentile(all, 95);
        var hostP95 = LatencyHarness.Percentile(host, 95);
        var over50 = runs.Sum(r => r.EventTimingMs.Count(v => v > 50));
        Log($"document {text.Length} chars; all positions: {all.Length} samples of {keys} keys; page p95 {p95:F1} ms; host p95 {hostP95:F1} ms; event-timing entries > 50 ms: {over50}");

        return new CheckResult("latency", complete && p95 < 50,
            $"p95 {p95:F1} ms page (keydown->next frame), {hostP95:F1} ms host (SendInput->paint stamp received), n={all.Length}/{keys}, doc {text.Length} chars",
            "SendInput letters at 50 ms cadence, 150 per position (near start, middle, end) of a rich-projection notebook; page/latency-harness.mjs stamps keydown.timeStamp and the first task after the next animation frame",
            [.. _log]);
    }

    // ---------------------------------------------------------------- completion popup UIA

    private async Task<CheckResult> CompletionUiaAsync()
    {
        var original = Fixtures.NotebookLf;
        var pos = original.IndexOf("spaces   ", StringComparison.Ordinal) + "spaces   ".Length;
        var root = await Task.Run(() => AutomationElement.FromHandle(hwnd));
        var steps = new List<(string Step, bool Ok)>();

        // open
        await LoadAsync(original, "rich");
        await FocusAtAsync(pos);
        RequireForeground();
        Native.TypeAt();
        Native.TypeLetters("sp");
        var opened = await page.WaitAsync("nb.completion().listbox", TimeSpan.FromSeconds(3));
        var list = await UntilUia(() => FindList(root), TimeSpan.FromSeconds(5));
        var items = await Task.Run(() => Items(list));
        var names = await Task.Run(() => items?.Cast<AutomationElement>().Select(e => e.Current.Name).ToArray() ?? []);
        var dom = await page.EvalAsync("nb.completion()");
        var selected = await Task.Run(() => SelectedIndex(items));
        var listLabel = await Task.Run(() => list is null ? "NOT FOUND" : $"'{list.Current.Name}' {list.Current.ControlType.ProgrammaticName}");
        var listName = await Task.Run(() => list?.Current.Name);
        Log($"open: DOM listbox {opened}; DOM {dom.GetRawText()}");
        Log($"open: UIA list {listLabel}; items [{string.Join(" | ", names)}]; UIA selected index {selected}");
        var editor = await Task.Run(() => root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "Notebook")));
        var editorLabel = await Task.Run(() => editor is null ? "NOT FOUND" : $"'{editor.Current.Name}' {editor.Current.ControlType.ProgrammaticName}");
        Log($"editor: UIA {editorLabel}");
        steps.Add(("open: listbox named, options, first selected in DOM and UIA",
            opened && listName == "Completions" && names.Length >= 2 && selected == 0
            && dom.GetProperty("selectedIndex").GetInt32() == 0 && dom.GetProperty("activedescendant").GetString() == dom.GetProperty("selectedId").GetString()
            && editor is not null));

        // move
        RequireForeground();
        Native.Key(Native.VkDown);
        await Task.Delay(300);
        dom = await page.EvalAsync("nb.completion()");
        var movedUia = await Task.Run(() => SelectedIndex(Items(list)));
        Log($"ArrowDown: DOM {dom.GetRawText()}; UIA selected index {movedUia}");
        steps.Add(("ArrowDown moves DOM and UIA selection to option 1",
            dom.GetProperty("selectedIndex").GetInt32() == 1 && movedUia == 1 && dom.GetProperty("activedescendant").GetString() == dom.GetProperty("selectedId").GetString()));

        // dismiss
        RequireForeground();
        Native.Key(Native.VkEscape);
        steps.Add(("Escape: no listbox, no aria-activedescendant/controls, no UIA list or selected item, focus on the editor",
            await DismissedAsync(root, "Escape")));

        // zero matches
        await LoadAsync(original, "rich");
        await FocusAtAsync(pos);
        RequireForeground();
        Native.TypeAt();
        Native.TypeLetters("zzq");
        await Task.Delay(700);
        steps.Add(("zero matches: nothing open, nothing active", await DismissedAsync(root, "zero matches")));

        // accept
        await LoadAsync(original, "rich");
        await FocusAtAsync(pos);
        RequireForeground();
        Native.TypeAt();
        Native.TypeLetters("sp");
        await page.WaitAsync("nb.completion().listbox", TimeSpan.FromSeconds(3));
        await Task.Delay(150);
        Native.Key(Native.VkReturn);
        await Task.Delay(300);
        var accepted = await page.DocAsync();
        var applied = accepted.Contains("spaces   @spec/workbench-loops", StringComparison.Ordinal);
        Log($"accept: applied '@spec/workbench-loops' {applied}");
        steps.Add(("Enter accepts and leaves nothing active", applied && await DismissedAsync(root, "accept")));

        foreach (var (step, ok) in steps)
        {
            Log($"{(ok ? "PASS" : "FAIL")} {step}");
        }

        return new CheckResult("completion-uia", steps.All(s => s.Ok),
            string.Join("; ", steps.Select(s => $"{s.Step.Split(':')[0]}={(s.Ok ? "pass" : "FAIL")}")),
            "SendInput '@sp' / ArrowDown / Escape / '@zzq' / Enter; System.Windows.Automation over the WebView2 window (List 'Completions', ListItem SelectionItem.IsSelected, FocusedElement) cross-checked with the DOM's aria-activedescendant",
            [.. _log]);
    }

    private async Task<bool> DismissedAsync(AutomationElement root, string label)
    {
        var domGone = await page.WaitAsync("!nb.completion().listbox && nb.completion().activedescendant === null", TimeSpan.FromSeconds(2));
        var dom = await page.EvalAsync("nb.completion()");
        var uiaGone = await UntilUia(() => FindList(root) is null ? root : null, TimeSpan.FromSeconds(3)) is not null;
        var dangling = await Task.Run(() => root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem))
            .Cast<AutomationElement>().Count(IsSelected));
        var (focusName, focusType) = await Task.Run(() =>
        {
            var focused = AutomationElement.FocusedElement;
            return (focused?.Current.Name, focused?.Current.ControlType.ProgrammaticName);
        });
        var focusOnEditor = focusName == "Notebook";
        Log($"{label}: DOM {dom.GetRawText()}; UIA list gone {uiaGone}; selected ListItems left {dangling}; UIA focus '{focusName}' {focusType}");
        return domGone && uiaGone && dangling == 0 && focusOnEditor
            && dom.GetProperty("controls").ValueKind == JsonValueKind.Null;
    }

    private static AutomationElement? FindList(AutomationElement root) =>
        root.FindFirst(TreeScope.Descendants, new AndCondition(
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.List),
            new PropertyCondition(AutomationElement.NameProperty, "Completions")));

    private static AutomationElementCollection? Items(AutomationElement? list) =>
        list?.FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem));

    private static int SelectedIndex(AutomationElementCollection? items)
    {
        if (items is null)
        {
            return -1;
        }

        for (var i = 0; i < items.Count; i++)
        {
            if (IsSelected(items[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsSelected(AutomationElement e) =>
        e.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var p) && ((SelectionItemPattern)p).Current.IsSelected;

    /// <summary>Polls a UIA probe on a worker thread: a UIA client on the provider window's own UI thread can deadlock.</summary>
    private static async Task<T?> UntilUia<T>(Func<T?> probe, TimeSpan timeout) where T : class
    {
        var deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            var value = await Task.Run(probe);
            if (value is not null || DateTime.UtcNow > deadline)
            {
                return value;
            }

            await Task.Delay(100);
        }
    }

    private static async Task<bool> Until(Func<bool> probe, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!probe())
        {
            if (DateTime.UtcNow > deadline)
            {
                return false;
            }

            await Task.Delay(100);
        }

        return true;
    }

    private static int Count(string text, string part)
    {
        var n = 0;
        for (var i = text.IndexOf(part, StringComparison.Ordinal); i >= 0; i = text.IndexOf(part, i + part.Length, StringComparison.Ordinal))
        {
            n++;
        }

        return n;
    }
}
