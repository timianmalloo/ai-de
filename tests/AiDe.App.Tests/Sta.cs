using System.Windows;
using System.Windows.Threading;

namespace AiDe.App.Tests;

/// <summary>
/// The one STA harness. Runs a body on a single-threaded-apartment thread and reports honestly.
/// </summary>
/// <remarks>
/// <para><b>Why one.</b> Thirty-two test files each declared their own, under three names
/// (<c>OnSta</c>, <c>OnStaThread</c>, <c>OnUiThread</c>) with five different timeouts. Eighteen
/// wrapped assertion failures in <c>InvalidOperationException("STA work failed", …)</c> and thirteen
/// rethrew them correctly, so the right form and the wrong form sat side by side in one directory
/// with nothing marking which was which. Writing a new test meant copying one at random, and the
/// design session — having spent that day arguing you cannot tell a thing by looking at it — read
/// several and copied a broken one (DC-079).</para>
///
/// <para><b>What that cost.</b> A failing assertion printed <c>System.InvalidOperationException :
/// STA work failed</c> with the real sentence demoted to an inner exception. That reads as a flaky
/// harness, and the response to flakiness is a re-run rather than an investigation, so a real
/// finding got triaged away by someone behaving correctly on the information they were given
/// (DC-078).</para>
///
/// <para><b>The rule this encodes.</b> An assertion failure is rethrown <b>as itself</b>; only a
/// genuine infrastructure failure gets a wrapper, because only there is the wrapper a true
/// statement. Everything else — the timeout, the join, the apartment state — is the same in every
/// caller and now has one place to be wrong in.</para>
///
/// <para><b>Timeouts are passed through rather than unified.</b> A test that waited five seconds and
/// one that waited sixty were making different bets about what they were driving; collapsing them
/// would be a behaviour change wearing a refactor's clothes.</para>
/// </remarks>
public static class Sta
{
    /// <summary>The default a caller gets when it does not care.</summary>
    public const int DefaultTimeoutSeconds = 60;

    /// <summary>Runs <paramref name="body"/> on an STA thread and returns its value.</summary>
    public static T Run<T>(Func<T> body, int timeoutSeconds = DefaultTimeoutSeconds)
    {
        T result = default!;
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try { result = body(); }
            catch (Exception ex) { failure = ex; }
        });

        thread.SetApartmentState(ApartmentState.STA);

        // BACKGROUND, or a hung body outlives the whole test run. `new Thread(...)` defaults to a
        // FOREGROUND thread, and a live foreground thread keeps the process alive — so when the Join
        // below times out, the assertion reports the timeout correctly and the thread carries on,
        // and testhost.exe never exits. It then holds the test assembly's DLLs open, so the NEXT
        // build fails with MSB3027 "the file is locked by testhost" and the next `dotnet test`
        // appears to hang. That happened twice in one day, cost a CI-length wait each time, and
        // presented as a build or infrastructure problem rather than as a test that did not finish.
        //
        // A background thread cannot hold the process open. Nothing else changes: the Join still
        // waits the same time and the assertion below still says the same thing.
        thread.IsBackground = true;
        thread.Start();

        var finished = thread.Join(TimeSpan.FromSeconds(timeoutSeconds));

        Rethrow(failure);

        // AFTER the failure check, deliberately. A body that threw usually also finished, but when
        // both happen the exception is the more informative of the two — reporting the timeout first
        // would bury it exactly the way the wrappers did.
        Assert.True(finished, $"the STA thread did not finish within {timeoutSeconds}s");

        return result;
    }

    /// <summary>Runs <paramref name="body"/> on an STA thread.</summary>
    public static void Run(Action body, int timeoutSeconds = DefaultTimeoutSeconds) =>
        Run<object?>(() => { body(); return null; }, timeoutSeconds);

    /// <summary>
    /// Runs an async body on an STA thread with a <b>pumping dispatcher</b> and a real window.
    /// </summary>
    /// <remarks>
    /// WebView2 initialisation and a page's message pump both need a running dispatcher, and the
    /// awaits inside the body must resume on it — so the synchronization context is installed before
    /// anything that could capture it. Two files had near-identical copies of this; the subtle half
    /// is the ordering, which is why it is worth having once.
    /// </remarks>
    /// <param name="content">
    /// Wraps the subject in the window's content when the test needs siblings — a focus test needs
    /// a button on either side of the canvas to have anywhere to tab to. Defaults to the subject
    /// itself.
    /// </param>
    /// <param name="configure">
    /// Adjusts the window before it is shown. A test driving synthesized input needs the window
    /// on-screen and activated, because <c>SendInput</c> goes to the FOREGROUND window and an
    /// off-screen one would swallow every key — failing for a reason unrelated to its subject.
    /// </param>
    public static void Pump<T>(
        Func<T> create,
        Func<Window, T, Task> body,
        Func<T, UIElement>? content = null,
        Action<Window>? configure = null,
        int timeoutSeconds = DefaultTimeoutSeconds)
        where T : UIElement
    {
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            T? subject = default;
            Window? window = null;

            try
            {
                subject = create();

                window = new Window
                {
                    Content = content is null ? subject : content(subject),
                    Width = 640,
                    Height = 480,
                    Left = -10000,
                    Top = -10000,
                    ShowInTaskbar = false,
                    ShowActivated = false,
                };

                configure?.Invoke(window);
                window.Show();

                var dispatcher = Dispatcher.CurrentDispatcher;
                SynchronizationContext.SetSynchronizationContext(
                    new DispatcherSynchronizationContext(dispatcher));

                var done = false;
                _ = Task.Run(() => { }).ContinueWith(async _ =>
                {
                    try { await body(window, subject); }
                    catch (Exception ex) { failure = ex; }
                    finally { done = true; }
                }, TaskScheduler.FromCurrentSynchronizationContext());

                var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(timeoutSeconds);
                while (!done && DateTime.UtcNow < deadline)
                {
                    dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));
                    Thread.Sleep(10);
                }

                if (!done) failure ??= new TimeoutException("the pumped STA body did not complete");
            }
            catch (Exception ex)
            {
                failure ??= ex;
            }
            finally
            {
                try
                {
                    window?.Close();
                    (subject as IDisposable)?.Dispose();
                    Dispatcher.CurrentDispatcher.InvokeShutdown();
                }
                catch (Exception ex) when (ex is not OutOfMemoryException)
                {
                    // Teardown must not replace a real finding with a cleanup error. If the body
                    // already failed, that failure is the one worth reporting.
                    failure ??= ex;
                }
            }
        });

        thread.SetApartmentState(ApartmentState.STA);

        // Background for the same reason as Run above, and more sharply here: this thread owns a
        // Window and a pumping Dispatcher, so a body that never completes leaves a live message loop
        // behind. As a foreground thread that loop would keep testhost.exe alive indefinitely.
        thread.IsBackground = true;
        thread.Start();

        Assert.True(
            thread.Join(TimeSpan.FromSeconds(timeoutSeconds + 30)),
            "the pumped STA thread did not finish");

        Rethrow(failure, "this test needs a real window and the WebView2 runtime");
    }

    /// <summary>
    /// Rethrows a captured failure, wrapping <b>only</b> what a wrapper would describe truthfully.
    /// </summary>
    /// <remarks>
    /// This is the whole point of the class. An <c>XunitException</c> carries the sentence the test
    /// author wrote so a failure would be legible; wrapping it reports a real defect as a broken
    /// machine and sends the reader to re-run instead of to investigate. Anything else really is the
    /// environment, and there the wrapper is a true statement worth adding.
    /// </remarks>
    private static void Rethrow(Exception? failure, string? environmentHint = null)
    {
        if (failure is null) return;

        if (failure is Xunit.Sdk.XunitException) throw failure;

        if (environmentHint is not null)
        {
            // Not a skip: a missing runtime is a broken environment, and a quiet pass would restore
            // the false-success these tests exist to prevent (DC-012/DC-016).
            throw new InvalidOperationException($"{environmentHint}. {failure.Message}", failure);
        }

        throw new InvalidOperationException("the STA body threw", failure);
    }
}
