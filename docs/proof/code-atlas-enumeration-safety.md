---
id: proof-code-atlas-enumeration-safety
title: "Proof Pack — Code Atlas opened-directory enumeration safety probe"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "atlas-live-reader-enumeration"
tags: [proof-pack, code-atlas, directory-enumeration, windows, probe]
links:
  - { to: architecture-code-atlas-proposed, rel: relates-to }
  - { to: adr-01M2BBCCAYMNMM1MFH0653Z0XF, rel: relates-to }
  - { to: proof-code-atlas-source-safety, rel: relates-to }
review-by: 2027-03-12
review-suggested: []
summary: >-
  Isolated Windows/.NET 10 probe for safe ordinary local directory enumeration. It verifies
  no-follow reparse refusal before listing, root identity binding, handle-budget accounting, bounded
  cancellation, and explicit NOT_PROVEN symlink limits. It is not production admission.
---

# Proof Pack — Code Atlas opened-directory enumeration safety probe

- **Branch:** `atlas/live-reader-enumeration`.
- **Baseline readback:** `054b8b56381e4cf42717c2a8012f2e64a116e10d`.
- **Identity:** `AGENT_SESSION=atlas-live-enumeration-gpt55`; `AGENT_NAME=copilot-atlas-enumeration`.
- **Authored files only:** `spikes/code-atlas-directory-enumeration/CodeAtlas.DirectoryEnumerationProbe.csproj`, `Program.cs`, `OpenedDirectoryEnumerator.cs`, `DirectoryEnumerationProbeCases.cs`; `docs/proof/code-atlas-enumeration-safety.md`.
- **Raw outputs retained:** `.agents/artifacts/atlas-live-enumeration-gpt55/directory-enumeration/raw-red.txt`, `raw-green.txt`, `raw-review-red.txt`, `raw-review-green.txt`, `raw-final-red.txt`, `raw-final-green.txt`.
- **TRX:** not applicable. This probe is a `dotnet run` executable, not a test runner.
- **Not claimed:** product reader admission, integrated native E0 completion, source declaration identity, broad K0 rerun, or new filesystem authority.

## Contract and fixture list written before implementation

- **Scope:** ordinary local directories only. Device and UNC-like inputs are refused before I/O; no real UNC connection is attempted.
- **Candidate mechanism:** open root and ancestor directories with native `CreateFileW` + `FILE_FLAG_BACKUP_SEMANTICS`; inspect roots/components with no-follow `FILE_FLAG_OPEN_REPARSE_POINT`; use `GENERIC_READ` with read sharing so managed enumeration works while delete/rename sharing is withheld.
- **Root authorization:** callers validate against a previously captured `RootBinding` of opened root volume serial, file index and final path. Capturing a fresh binding is a fixture setup step only; it is not validation of an old grant.
- **Limits:** product candidates are entries 25,000, depth 64, descriptors 128. Probe values are entries 5/50, depth 1/3, descriptors 2/16.
- **Required cases:** normal enumeration; direct root and target reparse refusal before listing; parent-listed junction refusal with no target-name leak; root replacement before enumeration; root/ancestor relocation blocked and succeeding after release with expected Win32 cause; hardlink listed only as ordinary file name; entry/depth/descriptor limits; cancellation after start and pre-cancel; missing/empty directories; relative escape/device/UNC/ADS refusal before I/O.
- **Not acceptance:** this is not production admission and not a substitute for later Security/Test review.

## Mechanism observed

1. `CaptureRootBinding(root)` opens the ordinary root with no-follow inspection, rejects reparse roots, then captures the followed root's volume serial, file index and final path.
2. `Enumerate(root, relativeDirectory, binding, ct)` refuses fully qualified, device, UNC-like and colon/ADS identifiers before path traversal.
3. It opens the root no-follow first. A reparse root returns `Unverifiable` with zero entries.
4. It then holds the followed root handle and compares it to the supplied binding. A replaced root returns `Unverifiable` with zero entries.
5. It inspects each target path component no-follow. A direct target junction returns `Unverifiable` before listing target contents.
6. Parent-listed reparse entries are emitted as `Unverifiable` and are not descended into. Outside target names are not emitted.
7. Root and ancestor mutation helpers require expected sharing/access failure codes: Win32 low word 5, 32 or 33. The final run observed Win32 32 and then proved the same move succeeded after release.
8. The probe measures peak held handles. Descriptor-limit cases assert the measured peak stays within the configured budget.
9. Cancellation after enumeration starts returns `Canceled` with zero entries, and immediate mutation after cancel proves handle release.

## Red-first evidence

Two red stages were observed:

| Stage | Raw file | Result |
|---|---|---|
| Unsafe stub | `raw-red.txt` | `SUMMARY|passed=2|failed=10|not_proven=2`; failures included empty stub listing, relocation not blocked, missing refusals and missing limits. |
| Consolidated review harness against old enumerator | `raw-review-red.txt` | Compile red: missing `CaptureRootBinding`, missing `afterEntryObserved`, and no `PeakHeldHandles`. |
| Root-binding mutation | `raw-final-red.txt` | `SUMMARY|passed=28|failed=1|not_proven=2`; disabling the root-binding guard made replacement-root enumeration leak entries and fail `root binding guard blocks replacement-root leak`. |

The unsafe-stub red and root-binding mutation red are semantic controls. The missing-API compile red is retained only as harness-evolution history; it is not semantic safety evidence.

## Commands

| Command | Result |
|---|---|
| `python docs\ai-forward-pack\scripts\pack-doctor.py --root .` | 10 PASS, 2 WARN, 1 pre-existing knowledge-graph FAIL. No install performed. |
| `dotnet restore spikes\code-atlas-directory-enumeration\CodeAtlas.DirectoryEnumerationProbe.csproj --source "%USERPROFILE%\.nuget\packages" --nologo --verbosity:minimal` | Restored SDK-only probe project in 47 ms. The probe project has no package references. |
| `dotnet run --project spikes\code-atlas-directory-enumeration\CodeAtlas.DirectoryEnumerationProbe.csproj --no-restore` | Final consolidated run: `SUMMARY|passed=29|failed=0|not_proven=2|duration_ms=55`; runtime `10.0.11`; OS `Microsoft Windows NT 10.0.26200.0`. |

## Final per-case disposition

| Case | Outcome |
|---|---|
| Hardlink | PASS. Listed only as an ordinary file name; no source custody or content claim. |
| Ordinary local listing while handles are held | PASS. `Complete`; 4 entries; peak 4 handles; root identity example `vol=861e054a`, file index recorded in raw output. Peak assertion added. |
| Approved-root isolation | PASS. Expected in-root entries were present; escape target names were absent. |
| Root relocation while held | PASS. Blocked with `IOException`, `hresult=0x80070020`, Win32 32; after-release move succeeded. |
| Ancestor relocation while held | PASS. Blocked with `IOException`, `hresult=0x80070020`, Win32 32; after-release move succeeded. |
| Old binding against replaced root | PASS. `Unverifiable`, zero entries, detail `opened root identity differs from authorized binding`. |
| Empty directory | PASS. `Complete`, zero entries. |
| Missing directory | PASS. Typed `Unavailable`, zero entries; public detail is `native open failed`; native code is `2`; no absolute path is in the public result detail. |
| Direct target junction | PASS. `Unverifiable`, zero entries, refused before target listing. |
| Reparse root | PASS. `Unverifiable`, zero entries, refused before listing. |
| Parent-listed junction | PASS. Target names did not leak; junction entry was marked `Unverifiable`. |
| File symlink outside | NOT_PROVEN. Host symlink privilege limitation retained: `0x80070522`. |
| Directory symlink inside/outside/cycle | NOT_PROVEN. Host symlink privilege limitation retained: `0x80070522`. |
| Entry limit | PASS. `LimitExceeded`, 5 entries, peak 3 handles. Immediate root move after return succeeded. |
| Depth limit | PASS. `LimitExceeded`, peak 3 handles. Immediate ancestor move after return succeeded. |
| Descriptor limit | PASS. Direct target-acquisition limit: `LimitExceeded`, zero entries, peak 1. Recursive descriptor exhaustion: `LimitExceeded`, one entry, peak 3 under cap 3. Immediate mutations after both returns succeeded. |
| Cancellation after start | PASS. `Canceled`, zero entries, peak 3 handles. Immediate root move after return succeeded. |
| Pre-cancel | PASS. `Canceled`, zero entries, peak 0. |
| Relative escape | PASS. `Refused`, zero entries. |
| ADS/colon syntax | PASS. `Refused`, zero entries before I/O. |
| Device path | PASS. `Refused`, zero entries before I/O. |
| UNC-like input | PASS. `Refused`, zero entries before I/O. |

## Residuals for independent review

- Symlink fixtures remain NOT_PROVEN and must be run on a host that can create symlinks, or the production input domain must categorically refuse them.
- This probe uses synthetic disposable roots only. It does not exercise repository stores, MSBuild, user workspaces, product IPC, or production UI. It proves manifest-bound ordinary local directory behavior with detected reparse paths excluded and no outside names/counts, not an absolute race-free filesystem-opening guarantee.
- This is Windows/.NET 10 behavior on this host only. It does not claim POSIX portability.
- Parent should send this proof and the four probe source files to independent Security/Test admission before any production reader use.

## Budget ledger

Budget was 25 total wrapper/leaf calls for this investigation. Before this cleanup, 22/25 were used. This cleanup uses 3 calls through final status. Total observed consumption is 25/25, with 0 remaining.
