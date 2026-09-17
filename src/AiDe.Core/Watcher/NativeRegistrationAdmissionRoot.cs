using System.Diagnostics;

namespace AiDe.Core.Watcher;

/// <summary>Internal enrollment only; production trusted-context composition is not yet wired.</summary>
internal sealed class NativeRegistrationAdmissionRoot : IDisposable
{
    private static readonly ActivitySource Activities = new("AiDe.NativeAdmission");
    private readonly SqliteWatcherObservationStore _store;
    private readonly NoticeAdmissionCoordinator _coordinator;
    internal Action<NativeAdmissionFaultPoint>? Fault { get; set; }

    internal NativeRegistrationAdmissionRoot(SqliteWatcherObservationStore store)
    {
        _store = store;
        _coordinator = new(store);
    }

    internal RegistrationAdmissionResult Admit(
        TrustedRegistrar registrar, string operationId, HarnessRegistration registration,
        NativeRegistrationContext context, long recordedAtMilliseconds)
    {
        using var activity = Activities.StartActivity("native.admission");
        var started = Stopwatch.GetTimestamp();
        try
        {
            var input = NativeAdmissionCodec.Bind(operationId, registration, context);
            var result = registrar.AdmitNative(_store, _coordinator, input, recordedAtMilliseconds, Fault);
            activity?.SetTag("native.admission.replayed", result.Replayed);
            activity?.SetTag("native.notice.required", result.Admission.CorrectionCode != "NONE");
            return result;
        }
        catch (WatcherException exception)
        {
            activity?.SetTag("error.type", exception.Code);
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
        finally { activity?.SetTag("native.admission.duration_ms", Stopwatch.GetElapsedTime(started).TotalMilliseconds); }
    }

    public void Dispose() => _coordinator.Dispose();
}
