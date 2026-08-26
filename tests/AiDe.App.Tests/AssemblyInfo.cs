// Test parallelization is disabled for the same reason as AiDe.Core.Tests, plus one specific to WPF.
//
// These tests create real WPF Windows on dedicated STA threads. Run in parallel, several classes
// show windows and tear down Dispatchers concurrently, and the test host crashes mid-run — which
// presents as tests silently DISAPPEARING from the count rather than as a failure, because the run
// aborts after the passes it already recorded. That is a success-shaped failure: "Passed! 27" with
// 21 tests never executed.
//
// Registered as defect class DC-008 (test-observable global state leaking between parallel classes);
// this is its second instance, which per CI4 means the first control was too narrow — it was applied
// to one test project when the cause was not project-specific.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
