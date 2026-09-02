---
id: api-aide-core-upgrade
title: "API: AiDe.Core.Upgrade"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Core.Upgrade: 13 types, 18 members, 90% carrying a summary doc comment.
---

# API: `AiDe.Core.Upgrade`

**13 public types · 18 public members · 90% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `InstalledVersion`

*record* — `DaemonInstallation.cs`

One daemon build on disk.

## `DaemonInstallation`

*class* — `DaemonInstallation.cs`

Side-by-side daemon builds, and the pointer that says which one runs.

**Remarks.** **Side-by-side is what makes rollback possible at all.** An installer that overwrote the
previous build would leave nothing to go back to: the store could be restored from its snapshot
and there would be no binary able to read it. Keeping the old directory is cheap; recreating it
after a failed upgrade means downloading a build during an incident.





**Repointing is one small atomic write, and it is the commit point.** Everything before
it — unpacking, migrating, gating — is reversible by doing nothing. A pointer file replaced by
rename means a process reading it during an upgrade sees the old version or the new one, never a
partial name, and never a directory that is still being written.





**Pruning never removes the current build or the one before it.** The previous build is
not history — it is the rollback target, and reclaiming its disk is trading an incident's
recovery path for a few megabytes.

| Member | Summary |
|---|---|
| `string VersionsDirectory { get; } = Path.Combine(root, "versions")` | Where builds live, one directory per version. |
| `string? Current` | The version currently pointed at, or `null` before the first install. |
| `IReadOnlyList<InstalledVersion> Installed` | Every build on disk, newest name last. |
| `string DirectoryFor(string version)` | The directory a version lives in, whether or not it exists yet. |
| `InstalledVersion Install(string version, string sourceDirectory)` | Places a build beside the others. Does **not** make it current. |
| `void Repoint(string version)` | Makes  the one that runs. The commit point. |
| `IReadOnlyList<string> Prune(int keep)` | Removes old builds, keeping the newest  plus whatever is current. |

### `string? Current`

The version currently pointed at, or `null` before the first install.

**Remarks.** Null rather than a guess. "Nothing is installed yet" and "the pointer names something that is
gone" are both states a supervisor must handle, and inventing a plausible version here would
send it to launch a directory that may not exist.

### `InstalledVersion Install(string version, string sourceDirectory)`

Places a build beside the others. Does **not** make it current.

**Remarks.** Installing and repointing are separate because everything between them is where an upgrade
decides whether to keep going. A combined step would make the commit happen before the gate
had a chance to refuse.

### `IReadOnlyList<string> Prune(int keep)`

Removes old builds, keeping the newest  plus whatever is current.

**Remarks.** The current build is protected explicitly rather than by relying on it being among the
newest: after a rollback the current version is an *older* one, which is precisely when
a naive "keep the newest N" would delete the build that is running.

## `HealthCheck`

*record* — `HealthGate.cs`

One named check and what it found.

**Remarks.** The detail is not decoration. "The upgrade failed" is not actionable; "expected schema v4, found
v3" is, and it is the difference between a rollback someone can explain and one they can only
observe.

| Member | Summary |
|---|---|
| `HealthCheck Pass(string name, string detail)` | **(gap)** |
| `HealthCheck Fail(string name, string detail)` | **(gap)** |

## `HealthGateResult`

*record* — `HealthGate.cs`

A gate run: whether it passed, what it ran, and how long it took.

## `GateStep`

*record* — `HealthGate.cs`

One check the gate will run.

## `HealthGate`

*class* — `HealthGate.cs`

The fast subset that decides whether a freshly migrated store may be kept.

**Remarks.** **Fast is the specification, not an aspiration.** Full restore/replay equality is
asynchronous verification — P1-PERF measured a 50k-edge replay against a 15-minute RTO while this
gate has a 60-second budget. Putting the slow check inside the fast gate is the contradiction the
council review caught in the v1 architecture, so the budget is **enforced**: a gate that
merely documented one would pass a fifteen-minute replay and the contradiction would be back.





**It stops at the first failure.** Later checks assume earlier ones held — an integrity
sample over a store whose schema check just failed reports nonsense — so continuing produces a
cascade whose first entry is the only real one.





**It reports what it ran.** A gate's green result is evidence that the gate passed, not
that its contents passed. Naming every check is what makes the difference inspectable rather than
a matter of trust.

| Member | Summary |
|---|---|
| `HealthGateResult Run(IReadOnlyList<GateStep> steps)` | **(gap)** |

## `MigrationState`

*enum* — `MigrationJournal.cs`

Where a migration had got to when it was last written down.

## `MigrationRecord`

*record* — `MigrationJournal.cs`

One migration, as recorded on disk.

## `MigrationJournal`

*class* — `MigrationJournal.cs`

The durable note that says whether a migration is in flight.

**Remarks.** **It exists for the case where nothing gets to run.** Power loss, a kill, a bug that
takes the process out mid-migration: no cleanup handler fires, no finally block completes. What
survives is what was already on disk, so recovery has to be something the *next* start can
do from a file — and that file has to be written before the risky part, not after.





**Written by atomic replace, never in place.** A journal updated in place can be found
half-written by exactly the crash it exists to survive. Writing a temporary file and renaming it
means a reader sees either the old record or the new one, never a torn one.





**Readers must share delete for that to hold on Windows**, which is the part that is
easy to get wrong: a rename over a file another handle has open fails unless that handle opened
with `Delete`. With the ordinary `File.ReadAllText`, a concurrent
reader does not see a torn file — it makes the *writer* throw instead.





**Latest state only, not a log.** The journal answers one question — "is a migration in
flight?" — and an append-only file would turn that read into a parse, with the recovery path
depending on correctly interpreting a history.





**A torn or unreadable journal reads as "nothing in flight".** The alternative is that
the recovery path is the thing that crashes after a crash. That is a deliberate bias: an
unreadable journal means we cannot prove a migration was running, and proceeding as though one
was would restore a snapshot over a store that may be fine.

| Member | Summary |
|---|---|
| `MigrationRecord? Read()` | The current record, or `null` when there is nothing to recover. |
| `void Begin(string migrationId, int fromSchema, int toSchema, string snapshotPath)` | Records that a migration is about to run. Call before touching the store. |
| `void Complete()` | Records that the migration finished and its gate passed. |
| `void RolledBack()` | Records that the migration was undone. |

## `StoreSnapshot`

*class* — `UpgradeCoordinator.cs`

Copies the store aside so a migration can be undone.

**Remarks.** **Copied, never moved.** Renaming the store into place as a snapshot would leave a window in
which no store exists at all — which is the one state a crash must never find, and the exact
instant this whole mechanism is defending.

| Member | Summary |
|---|---|
| `string Capture(string storePath, string directory)` | Copies  into . |
| `void Restore(string snapshotPath, string storePath)` | Puts a snapshot back. |

## `UpgradeResult`

*record* — `UpgradeCoordinator.cs`

What an upgrade attempt did.

## `RecoveryResult`

*record* — `UpgradeCoordinator.cs`

What a startup recovery found.

## `UpgradeCoordinator`

*class* — `UpgradeCoordinator.cs`

The upgrade choreography: snapshot, migrate, gate, and either keep it or put it back.

**Remarks.** **The ordering is the design.** An upgrade that fails halfway is worse than one that
never started: a store migrated to a schema the running binary cannot read is a workspace nobody
can open, with the user's evidence inside it. So the snapshot is taken before anything changes,
the journal is written before the migration runs, and the point of no return — deleting the
snapshot — comes only after the gate has passed.





**Rollback is not "do not migrate".** It is "undo a migration that already happened",
which is why a snapshot exists at all rather than a decision to proceed.





**Recovery is a separate entry point** (`RecoverIfIncomplete`) because the
case it handles is the one where this class never got to finish. Nothing in the failure path can
be relied on to run at the moment of a power loss, so the next start reads the journal and
completes the sentence.

| Member | Summary |
|---|---|
| `UpgradeResult Run(` | Runs one migration behind its gate. |
| `RecoveryResult RecoverIfIncomplete(string storePath, string workDirectory)` | Undoes a migration that was interrupted before it could finish. |

### `RecoveryResult RecoverIfIncomplete(string storePath, string workDirectory)`

Undoes a migration that was interrupted before it could finish.

**Remarks.** Called at startup, before the store is opened. A journal in
`Started` means a process died between "about to migrate" and
"migrated and verified" — so the store may be anything, and the only thing known to be good
is the snapshot.
