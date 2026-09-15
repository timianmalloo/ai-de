using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace AiDe.App.Workbench.Understanding;

/// <summary>A synchronous native host; admission begins only after activation.</summary>
internal sealed class AtlasLoadingHost : ContentControl
{
    private readonly AtlasWorkspaceOwner? _owner;
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private CancellationTokenSource? _activation;
    private IDisposable? _registration;
    private long _generation;

    internal AtlasLoadingHost(AtlasWorkspaceOwner? owner)
    {
        _owner = owner;
        AutomationProperties.SetName(this, "Code Atlas");
        _status.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
        SetResourceReference(BackgroundProperty, "SurfaceBrush");
        ShowStatus(owner is null ? "ATLAS-HOST-NO-WORKSPACE: open a workspace." : "Loading Code Atlas.");
        Loaded += async (_, _) => await ActivateAsync();
        Unloaded += (_, _) => Deactivate();
    }

    internal string StatusText => _status.Text;
    internal AtlasReaderView? ReaderView { get; private set; }

    internal Task ActivateAsync() => _owner is null ? Task.CompletedTask : _owner.Track(ActivateCoreAsync());

    private async Task ActivateCoreAsync()
    {
        Deactivate();
        var generation = _generation;
        var owner = _owner!;
        var workspace = owner.Generation;
        try
        {
            _activation = CancellationTokenSource.CreateLinkedTokenSource(owner.Token);
            var token = _activation.Token;
            _registration = owner.Register(Deactivate);
            ShowStatus("Loading Code Atlas.");
            var lease = await owner.AdmitAsync(token).ConfigureAwait(true);
            if (token.IsCancellationRequested || generation != _generation || workspace != owner.Generation)
                return;
            ReaderView = new AtlasReaderView(lease, owner);
            Content = ReaderView;
            await ReaderView.LoadAsync(token).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            if (generation == _generation)
            {
                Deactivate();
                ShowStatus("ATLAS-HOST-CANCELED: activation canceled.");
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            Trace.TraceError("code=ATLAS-HOST-UNAVAILABLE exception.type={0}", ex.GetType().FullName);
            if (generation == _generation)
            {
                Deactivate();
                ShowStatus("ATLAS-HOST-UNAVAILABLE: admission unavailable.");
            }
        }
    }

    internal void Deactivate()
    {
        ++_generation;
        var activation = _activation;
        var registration = _registration;
        var view = ReaderView;
        _activation = null;
        _registration = null;
        ReaderView = null;
        try { activation?.Cancel(); }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            Trace.TraceError("code=ATLAS-HOST-INVALIDATE phase=activation exception.type={0}", ex.GetType().FullName);
        }
        finally
        {
            activation?.Dispose();
            registration?.Dispose();
            try { view?.Deactivate(); }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                Trace.TraceError("code=ATLAS-HOST-INVALIDATE phase=reader-view exception.type={0}", ex.GetType().FullName);
            }
            finally { ShowStatus("ATLAS-HOST-INACTIVE: source cleared."); }
        }
    }

    private void ShowStatus(string text)
    {
        _status.Text = text;
        Content = _status;
    }
}
