using System.Diagnostics;
using Microsoft.Build.Locator;
using MsBuildTaskSpike;

// ---------------------------------------------------------------------------------------------
// Phase-2 spike D3 — do repository-authored MSBuild TASKS execute when we load a repository?
//
// S2 established that repository-authored ANALYZERS and SOURCE GENERATORS can be prevented from
// running, by stripping AnalyzerReferences from the loaded solution. It left a different question
// open: MSBuildWorkspace still runs MSBuild EVALUATION to load a project, and whether that path
// executes code the repository supplied was never tested.
//
// The principle at stake is absolute: LOADING A REPOSITORY MUST NEVER EXECUTE ITS CODE. So this
// probe is written to be believed only when it can also be shown to work — an "all clear" from an
// instrument that never ran is the failure mode S2 itself nearly shipped (DC-009).
// ---------------------------------------------------------------------------------------------

var here = AppContext.BaseDirectory;
var fixtureRoot = Path.GetFullPath(Path.Combine(here, "..", "..", "..", "fixture"));
var hostileProject = Path.Combine(fixtureRoot, "HostileProject", "HostileProject.csproj");
var hostileTask = Path.Combine(fixtureRoot, "HostileTask", "HostileTask.csproj");

Console.WriteLine("D3 — repository-authored MSBuild task execution");
Console.WriteLine(new string('=', 100));
Console.WriteLine($"fixture     : {hostileProject}");
Console.WriteLine($"markers     : {Markers.Dir}");
Console.WriteLine($"runtime     : {Environment.Version}");
Console.WriteLine(new string('=', 100));
Console.WriteLine();

if (!File.Exists(hostileProject))
{
    Console.WriteLine($"FAIL: fixture not found at {hostileProject}");
    return 2;
}

// ------------------------------------------------------------------ build the attacker's task DLL
// Vector 1 needs a prebuilt assembly, which is what a hostile repository would commit. Vectors 2
// and 3 need nothing but the .csproj, and are the sharper threat for that reason.
Console.WriteLine("SETUP — building the attacker's task assembly (vector 1 only)");
var built = Run("dotnet", $"build \"{hostileTask}\" -c Release --nologo -v quiet", out var setupLog);
Console.WriteLine($"  HostileTask build: {(built ? "ok" : "FAILED — vector 1 will be skipped, 2 and 3 still apply")}");
if (!built) Console.WriteLine("  " + string.Join("\n  ", setupLog.Split('\n').Take(6)));
Console.WriteLine();

// ------------------------------------------------------------------ POSITIVE CONTROL
// If a real build does not produce the markers, the fixture is broken and every later "no marker"
// result is meaningless. This must run BEFORE the real probe, and it must be seen to fire.
// A control that cannot fire in the environment that verifies it is defect class DC-016.
Console.WriteLine("PROBE 0 — POSITIVE CONTROL: does a real `dotnet build` fire the vectors?");
Markers.Clear();
Run("dotnet", $"build \"{hostileProject}\" -c Debug --nologo -v quiet", out _);
Markers.Report("MARKERS AFTER A REAL BUILD:");
var controlFired = Markers.Fired();
Console.WriteLine();

if (controlFired.Count == 0)
{
    Console.WriteLine("** THE FIXTURE IS INERT. A real build produced no markers, so this spike cannot");
    Console.WriteLine("   distinguish 'MSBuildWorkspace is safe' from 'the probe does not work'.");
    Console.WriteLine("   VERDICT: VOID — no conclusion may be drawn.");
    return 3;
}
Console.WriteLine($"  Positive control OK — {controlFired.Count} vector(s) demonstrably CAN fire: {string.Join(", ", controlFired)}");
Console.WriteLine();

// ------------------------------------------------------------------ the actual question
var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
if (instances.Count == 0)
{
    Console.WriteLine("FAIL: MSBuildLocator found no SDK instance.");
    return 2;
}
var chosen = instances.OrderByDescending(i => i.Version).First();
Console.WriteLine($"MSBuild     : {chosen.Name} {chosen.Version} at {chosen.MSBuildPath}");
Console.WriteLine();
MSBuildLocator.RegisterInstance(chosen);

var trustworthy = await TaskExecutionProbe.RunAsync(hostileProject);
Console.WriteLine();
Console.WriteLine(new string('=', 100));

if (!trustworthy)
{
    Console.WriteLine("VERDICT: VOID — the probe could not establish that it observed a real load.");
    return 3;
}

var firedUnderWorkspace = Markers.Fired();
Console.WriteLine(firedUnderWorkspace.Count == 0
    ? "VERDICT: NO repository-authored task executed under MSBuildWorkspace.OpenProjectAsync,\n" +
      "         on a fixture whose vectors are PROVEN to fire under a real build."
    : $"VERDICT: REPOSITORY CODE EXECUTED. Vectors that fired: {string.Join(", ", firedUnderWorkspace)}.\n" +
      "         Loading a repository executes its code. Component 1 cannot proceed on this path\n" +
      "         without a containment decision.");
Console.WriteLine(new string('=', 100));
return firedUnderWorkspace.Count == 0 ? 0 : 1;

static bool Run(string exe, string args, out string log)
{
    var psi = new ProcessStartInfo(exe, args)
    {
        RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
    };
    using var p = Process.Start(psi)!;
    log = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
    p.WaitForExit();
    return p.ExitCode == 0;
}
