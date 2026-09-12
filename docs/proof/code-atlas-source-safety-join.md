---
id: proof-code-atlas-source-safety-join
title: "Code Atlas source-safety join - independent bounded replay"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "atlas-e0-source-safety-join"
tags: [code-atlas, proof-pack, windows, source-safety]
links:
  - { to: proof-code-atlas-source-safety, rel: documents }
  - { to: note-atlas-isolated-authoring, rel: depends-on }
  - { to: architecture-code-atlas-proposed, rel: relates-to }
review-by: 2026-12-12
summary: >-
  Independent pinned and joined-branch runs observed 19 passed, zero failed and two not-proven
  symlink cases. Records final review conditions, corrects the historical raw-result pointer,
  and does not promote a successful process exit into native-product acceptance.
---

# Result and immutable provenance

The original worker's `fcbb3474` was repaired by `37a7585e` and normalized by
`8db3335c84f228cb001a4fd0a70bbaa34eb56cfe`. Only the four assigned probe project/source files
and its proof were added relative to worker base `4d396411`.

The Conductor observed a clean worker tree and that exact five-file delta. Independent replay in
`atlas/e0-csharp-review` pinned to `8db3335c` returned **19 PASS / 0 FAIL / 2 NOT_PROVEN**, 51 ms.
The three commits were then cherry-picked, without importing their newer main ancestry, as
`d8bc7fad`, `c4511956`, and `0c1b3cf1` into `conductor/code-atlas`. Replay on that joined branch
returned the same case counts in 55 ms. Both ran .NET runtime `10.0.11` on Windows
`10.0.26200.0`. These are individual elapsed measurements, not a latency percentile or budget claim.

Both replays used:

```powershell
dotnet restore spikes\code-atlas-source-reader\CodeAtlas.SourceReaderProbe.csproj --source "$env:USERPROFILE\.nuget\packages" -p:NuGetAudit=false --nologo --verbosity quiet
dotnet run --project spikes\code-atlas-source-reader\CodeAtlas.SourceReaderProbe.csproj --no-restore
```

Restore was restricted to the local source shown and package auditing was disabled for this
SDK-only probe. No project package reference, network source, provider call or privilege change
was introduced.

## Observed cases and their limits

| Claim / case group | Independent observation | Oracle / limit | Confidence |
|---|---|---|---|
| Exact indexed bytes/hash | `IndexedMatch`, text present | Real fixture bytes supply the expected hash; this is not a range/highlight oracle | Verified |
| Hash mismatch, missing hash, same-byte file replacement | `Changed` / `Unverifiable`, text null | Replacement retains bytes but changes opened-file identity; binding, not hash alone, rejects it | Verified for these cases |
| Invalid UTF-8 and oversized file | `UnsupportedEncoding` / `TooLargeToVerify`, text null | Invalid byte and over-bound inputs cannot pass as normal source | Verified |
| Cancellation before open and after file open | `Canceled`, text null | Actual supplied token is canceled; no claim of interrupting an already-blocked OS syscall | Verified |
| Root, ancestor and file rename; file write | `IOException`, `0x80070020`, Win32 `32` | Destination absent before; identical move/write succeeds after handle release | Verified |
| Later ancestor acquisition fails | Earlier ancestor can immediately rename | No GC/finalizer assistance; detects retained share-denying handles | Verified |
| Hard link and outside junction | `Unverifiable`, no text released | Extra link count / detected reparse cannot produce indexed text | Verified for these cases |
| Real alternate data stream | Exact `Refused`, text null | Stream actually exists; refusal is not an incidental hash mismatch or missing file | Verified |
| Relative escape, device and UNC-like file IDs | Refusal cases pass | Restricted relative file-ID domain, not arbitrary absolute-path support | Verified within the probe domain |
| File and directory symlink fixtures | `NOT_PROVEN`, `0x80070522` | Creation requires a privilege absent here; no configuration or privilege change attempted | Unverified, explicitly unsupported evidence |

The raw summaries were:

```text
SUMMARY|passed=19|failed=0|not_proven=2|duration_ms=51
SUMMARY|passed=19|failed=0|not_proven=2|duration_ms=55
RUNTIME|10.0.11
OS|Microsoft Windows NT 10.0.26200.0
```

The full independent and joined outputs were captured in this CLI session's
`files/atlas-source-safety-independent.txt` and `files/atlas-source-safety-joined.txt`.
The observation table and summaries above are the durable result. The original worker's
`raw-green.txt` is the **initial 15/0/2 run**, not the final repair evidence. Its
`raw-repair.txt` and this join record supersede that earlier pointer's final-result implication.

## Review disposition

| Reviewer | Final disposition | Condition retained |
|---|---|---|
| Security `3facf06b-883c-4039-a461-51b93a236f11` | PASS-WITH-CONDITIONS for bounded evidence / narrowed candidate direction | Only manifest-bound `IndexedSourceBinding` or equivalent; helper hash-only overload is not old-anchor authority; no text on nonmatch; categorical ADS/device/UNC/reparse exclusion and no broad symlink claim |
| Test Architect `e8c73a03-3fa2-4a77-8d17-68cdf80188b1` | PASS-WITH-CONDITIONS for repaired oracles | Historical raw pointer corrected by this final replay record; actual ranges and native-product behavior remain separate proof |
| C# `6cd38766-f012-4367-bb92-ceef198f26f1` | PASS on the two targeted repairs | Partial acquisition disposal and typed requested cancellation checked; no product admission implied |

Initial red evidence remains bounded: the worker observed metadata-only directory handles allow
root/ancestor moves before the `GENERIC_READ` sharing correction. The Conductor did not rerun
every earlier mutation and does not relabel missing-class compilation as proof of a safety invariant.

**The process exited 0 even with two NOT_PROVEN cases.** The Conductor parsed the result counts
and retained the incomplete disposition; exit 0 is not a full safety gate. Any future automated
admission must inspect those counts and its exact supported input space.

## What this checkpoint does not admit

No product reader, real workspace, editor source range, host/factory/IPC/store wiring, migration,
main integration or E0 completion is claimed. The Owner must select the candidate's precise
restricted input contract; that candidate must preserve these invariants and pass its own
inventory/identity/range/native proofs. Symlink behavior on this host remains unproven.
