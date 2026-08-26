using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading.Channels;
using AiDe.Core.Dispatch;
using AiDe.Core.Facts;
using Microsoft.Win32.SafeHandles;

namespace AiDe.Core.Terminal;

/// <summary>What a caller must supply to start a real terminal session.</summary>
public sealed record TerminalSessionRequest(
    string SessionId,
    long Generation,
    string CommandLine,
    string? WorkingDirectory,
    int Columns,
    int Rows,
    SessionProcessingClass ProcessingClass);

/// <summary>
/// The real terminal runtime: one ConPTY, one process, one owner loop (ADR-0005).
/// </summary>
/// <remarks>
/// <para><b>Input and output loops are separate</b>, which is a correctness requirement rather than
/// tidiness. ConPTY's pipes are finite: a process producing output faster than we read it blocks on
/// its own write, and if the same loop were responsible for both reading and writing, a large write
/// would deadlock against a full output pipe with neither side able to proceed.</para>
///
/// <para><b>Output is bounded and drops the oldest.</b> The architecture budgets 1 MiB/s of sustained
/// output; a reader that falls behind must not be able to grow the queue without limit. Dropping is
/// therefore a designed state — reported through <see cref="TerminalChunk.Truncated"/> and
/// <see cref="SessionActivity.OutputOverload"/> — because for a terminal, the *newest* output is the
/// interesting output and stalling the process to preserve scrollback would be the wrong trade.</para>
///
/// <para><b>Terminal bytes never leave this object except through <see cref="Output"/>.</b> They are
/// not logged, traced, or attached to telemetry, per the spec's privacy rule. The telemetry below
/// counts bytes; it never carries them.</para>
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class ConPtyTerminalSession : ITerminalSession
{
    /// <summary>Chunks buffered before the oldest is dropped.</summary>
    /// <remarks>
    /// simplify: a fixed chunk count rather than a byte budget; ceiling is roughly 512 x read-buffer
    /// (~2 MiB); upgrade trigger = P2-PERF shows a renderer starving or a memory ceiling breached
    /// under the 1 MiB/s case.
    /// </remarks>
    private const int OutputCapacity = 512;

    private const int ReadBufferSize = 4096;

    private static readonly ActivitySource Telemetry = new("AiDe.Core.Terminal");

    private readonly Channel<TerminalChunk> _output;
    private readonly TaskCompletionSource<SessionExit> _exit =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private readonly CancellationTokenSource _shutdown = new();
    private readonly System.Threading.Lock _stateGate = new();

    private readonly SafeFileHandle _inputWrite;
    private readonly SafeFileHandle _outputRead;
    private readonly FileStream _toProcess;
    private readonly FileStream _fromProcess;

    private IntPtr _console;
    private IntPtr _process;
    private IntPtr _thread;
    private IntPtr _job;

    private SessionActivity _activity = SessionActivity.Starting;
    private bool _truncatedSinceLastChunk;
    private bool _disposed;

    private ConPtyTerminalSession(
        TerminalSessionRequest request,
        IntPtr console,
        IntPtr job,
        ConPtyInterop.ProcessInformation process,
        SafeFileHandle inputWrite,
        SafeFileHandle outputRead)
    {
        SessionId = request.SessionId;
        Generation = request.Generation;
        ProcessingClass = request.ProcessingClass;

        _console = console;
        _job = job;
        _process = process.hProcess;
        _thread = process.hThread;
        _inputWrite = inputWrite;
        _outputRead = outputRead;

        _toProcess = new FileStream(inputWrite, FileAccess.Write, bufferSize: 1, isAsync: false);
        _fromProcess = new FileStream(outputRead, FileAccess.Read, bufferSize: ReadBufferSize, isAsync: false);

        _output = Channel.CreateBounded<TerminalChunk>(new BoundedChannelOptions(OutputCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleWriter = true,
        });
    }

    public string SessionId { get; }

    public long Generation { get; private set; }

    public SessionProcessingClass ProcessingClass { get; }

    public ChannelReader<TerminalChunk> Output => _output.Reader;

    public SessionActivity Activity
    {
        get
        {
            lock (_stateGate)
            {
                return _activity;
            }
        }
    }

    /// <summary>Creates the pseudo console, starts the process inside a kill-on-close job, and pumps.</summary>
    public static Task<ConPtyTerminalSession> StartAsync(
        TerminalSessionRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CommandLine);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.Columns, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.Rows, 1);

        using var span = Telemetry.StartActivity("terminal.start");
        span?.SetTag("session.id", request.SessionId);
        span?.SetTag("session.generation", request.Generation);
        span?.SetTag("session.processing_class", request.ProcessingClass.ToString());

        // Two pipes, four handles. The pseudo console takes the ends it reads from and writes to;
        // we keep the other two. The console's own ends are closed immediately after creation —
        // holding them would keep the output pipe alive after the process ends and the read loop
        // would never see EOF, so the session would never report an exit.
        if (!ConPtyInterop.CreatePipe(out var inputRead, out var inputWrite, IntPtr.Zero, 0))
        {
            throw new IOException("could not create the ConPTY input pipe");
        }

        if (!ConPtyInterop.CreatePipe(out var outputRead, out var outputWrite, IntPtr.Zero, 0))
        {
            inputRead.Dispose();
            inputWrite.Dispose();
            throw new IOException("could not create the ConPTY output pipe");
        }

        var size = new ConPtyInterop.Coord { X = (short)request.Columns, Y = (short)request.Rows };
        ConPtyInterop.ThrowIfFailed(
            ConPtyInterop.CreatePseudoConsole(size, inputRead, outputWrite, 0, out var console),
            "CreatePseudoConsole");

        inputRead.Dispose();
        outputWrite.Dispose();

        var job = ConPtyInterop.CreateKillOnCloseJob();

        ConPtyInterop.ProcessInformation process;
        try
        {
            process = ConPtyInterop.StartAttachedProcess(
                console, request.CommandLine, request.WorkingDirectory);
        }
        catch
        {
            ConPtyInterop.ClosePseudoConsole(console);
            ConPtyInterop.CloseHandle(job);
            inputWrite.Dispose();
            outputRead.Dispose();
            throw;
        }

        // Assign after creation rather than creating suspended-in-job: the window between the two is
        // real but small, and the alternative needs CREATE_SUSPENDED plus a resume, which adds its
        // own failure mode where the process never starts if the assign throws.
        ConPtyInterop.AssignProcessToJobObject(job, process.hProcess);

        var session = new ConPtyTerminalSession(request, console, job, process, inputWrite, outputRead);
        session.StartPumping();
        return Task.FromResult(session);
    }

    public async Task<PtyWriteResult> WriteAsync(
        long expectedGeneration, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // The gate is what makes "compare the generation and write" one indivisible step. Checking
        // outside it would leave a window in which the process is replaced between the check and the
        // write, and a confirmed prompt would land in a session the user never approved.
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Activity == SessionActivity.Ended)
            {
                return PtyWriteResult.Failed;
            }

            if (expectedGeneration != Generation)
            {
                return PtyWriteResult.GenerationChanged;
            }

            await _toProcess.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
            await _toProcess.FlushAsync(cancellationToken).ConfigureAwait(false);

            // Byte COUNT only. The bytes themselves are a prompt and never reach telemetry.
            using var span = Telemetry.StartActivity("terminal.write");
            span?.SetTag("session.id", SessionId);
            span?.SetTag("write.bytes", bytes.Length);
            return PtyWriteResult.Accepted;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (IOException)
        {
            // The pipe is gone: the process died between the generation check and the write. That is
            // a failed delivery, not an unknown one — nothing was accepted.
            return PtyWriteResult.Failed;
        }
        catch (ObjectDisposedException)
        {
            return PtyWriteResult.Failed;
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public Task<SessionExit> WaitForExitAsync(CancellationToken cancellationToken) =>
        _exit.Task.WaitAsync(cancellationToken);

    public ValueTask ResizeAsync(int columns, int rows, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(rows, 1);

        lock (_stateGate)
        {
            if (_activity == SessionActivity.Ended || _console == IntPtr.Zero)
            {
                return ValueTask.CompletedTask;
            }

            // Deliberately does NOT touch Generation: a resize is the same process at a different
            // size, and bumping it would invalidate every in-flight receipt for a window drag.
            ConPtyInterop.ResizePseudoConsole(
                _console, new ConPtyInterop.Coord { X = (short)columns, Y = (short)rows });
        }

        return ValueTask.CompletedTask;
    }

    private void StartPumping()
    {
        // A dedicated long-running thread rather than a pooled task: this loop blocks on a synchronous
        // pipe read for the life of the session, and parking a thread-pool thread there starves the
        // pool for exactly as long as the terminal is open.
        var reader = new Thread(ReadLoop)
        {
            IsBackground = true,
            Name = $"conpty-read-{SessionId}",
        };
        reader.Start();

        _ = Task.Run(WatchForExitAsync);
    }

    private void ReadLoop()
    {
        var buffer = new byte[ReadBufferSize];

        try
        {
            while (!_shutdown.IsCancellationRequested)
            {
                var read = _fromProcess.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                {
                    break; // EOF: the console's write end is closed, so the process is gone.
                }

                lock (_stateGate)
                {
                    if (_activity is SessionActivity.Starting or SessionActivity.Ready)
                    {
                        _activity = SessionActivity.Busy;
                    }
                }

                var chunk = new TerminalChunk(buffer.AsSpan(0, read).ToArray(), _truncatedSinceLastChunk);
                _truncatedSinceLastChunk = false;

                if (!_output.Writer.TryWrite(chunk))
                {
                    // DropOldest means TryWrite effectively always succeeds; a refusal here means the
                    // channel completed under us, which is a shutdown rather than an error.
                    break;
                }

                // The reader is behind if the channel is at capacity, so the NEXT chunk carries the
                // truncation marker — that is where a renderer draws its gap.
                if (_output.Reader.Count >= OutputCapacity)
                {
                    _truncatedSinceLastChunk = true;
                    lock (_stateGate)
                    {
                        if (_activity != SessionActivity.Ended)
                        {
                            _activity = SessionActivity.OutputOverload;
                        }
                    }
                }
            }
        }
        catch (IOException)
        {
            // The pipe broke: the same condition as EOF, reported differently.
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            _output.Writer.TryComplete();
        }
    }

    private async Task WatchForExitAsync()
    {
        try
        {
            // Poll rather than wait on the handle: a WaitHandle wrapper over a process handle we own
            // raw would need its own SafeHandle lifetime, and the resolution that matters here is
            // "before a human notices", not milliseconds.
            while (!_shutdown.IsCancellationRequested)
            {
                if (ConPtyInterop.GetExitCodeProcess(_process, out var code)
                    && code != ConPtyInterop.STILL_ACTIVE)
                {
                    Complete(new SessionExit(code, Killed: false, DateTimeOffset.UtcNow));
                    return;
                }

                await Task.Delay(50, _shutdown.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void Complete(SessionExit exit)
    {
        lock (_stateGate)
        {
            if (_activity == SessionActivity.Ended)
            {
                return;
            }

            _activity = SessionActivity.Ended;
        }

        // Complete the channel first so a reader woken by the exit and then draining finds a
        // completed channel rather than blocking forever on one more read.
        _output.Writer.TryComplete();
        _exit.TrySetResult(exit);
    }

    public async ValueTask DisposeAsync()
    {
        lock (_stateGate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        await _shutdown.CancelAsync().ConfigureAwait(false);

        // Closing the console's handle is what tells the attached process its terminal is gone, so
        // it is the polite way out and must come before the kill.
        if (_console != IntPtr.Zero)
        {
            ConPtyInterop.ClosePseudoConsole(_console);
            _console = IntPtr.Zero;
        }

        if (_process != IntPtr.Zero)
        {
            if (ConPtyInterop.GetExitCodeProcess(_process, out var code)
                && code == ConPtyInterop.STILL_ACTIVE)
            {
                ConPtyInterop.TerminateProcess(_process, 1);
            }
        }

        Complete(new SessionExit(ExitCode: null, Killed: true, DateTimeOffset.UtcNow));

        try
        {
            _toProcess.Dispose();
            _fromProcess.Dispose();
        }
        catch (IOException)
        {
        }

        _inputWrite.Dispose();
        _outputRead.Dispose();

        if (_thread != IntPtr.Zero)
        {
            ConPtyInterop.CloseHandle(_thread);
            _thread = IntPtr.Zero;
        }

        if (_process != IntPtr.Zero)
        {
            ConPtyInterop.CloseHandle(_process);
            _process = IntPtr.Zero;
        }

        // Last: closing the job's final handle is what kills anything the shell left behind, so it
        // must outlive every other cleanup step.
        if (_job != IntPtr.Zero)
        {
            ConPtyInterop.CloseHandle(_job);
            _job = IntPtr.Zero;
        }

        _shutdown.Dispose();
        _writeGate.Dispose();
    }
}
