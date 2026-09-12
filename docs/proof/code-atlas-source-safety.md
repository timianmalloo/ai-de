---
id: proof-code-atlas-source-safety
title: "Proof Pack — Code Atlas opened-object source safety probe"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "atlas-e0-source-safety"
tags: [proof-pack, code-atlas, source-safety, windows, probe]
links:
  - { to: architecture-code-atlas-proposed, rel: relates-to }
  - { to: adr-01M2BBCCAYMNMM1MFH0653Z0XF, rel: relates-to }
  - { to: proof-code-atlas-contract-grounding, rel: depends-on }
review-by: 2027-03-12
review-suggested: []
summary: >-
  Isolated Windows/.NET 10 probe recording bounded opened-object and byte/hash facts under
  synthetic disposable roots. Initial and repaired observations are distinguished; unsupported
  symlink fixtures remain NOT_PROVEN, and this is not native-product acceptance.
---

# Proof Pack — Code Atlas opened-object source safety probe

- **Branch / tree:** `atlas/e0-source-safety` in `C:\Projects\ai-de-atlas-e0-source-safety`.
- **Base readback:** `4d3964116cf45ce310ceec22a95db1563fb893cb` before edits.
- **Identity:** `AGENT_SESSION=atlas-e0-source-safety-gpt55`; `AGENT_NAME=copilot-atlas-source-safety`.
- **Authored paths only:** `spikes/code-atlas-source-reader/CodeAtlas.SourceReaderProbe.csproj`, `Program.cs`, `OpenedSourceReader.cs`, `SourceReaderProbeCases.cs`; `docs/proof/code-atlas-source-safety.md`.
- **Initial raw result (15/0/2):** ignored path `.agents/artifacts/atlas-e0-source-safety-gpt55/source-reader/raw-green.txt`; this is historical, not the repaired result.
- **Worker repair raw result (19/0/2):** `.agents/artifacts/atlas-e0-source-safety-gpt55/source-reader/raw-repair.txt`.
- **Independent final evidence:** `docs/proof/code-atlas-source-safety-join.md` records the separate pinned replay and joined-branch replay. It corrects the earlier implication that `raw-green.txt` named the final result.
- **Not claimed:** integrated/native product E0 completion, production reader acceptance, security approval, or broad K0 rerun.

## Authoritative contracts used

| API | Contract | Source |
|---|---|---|
| `File.OpenHandle` | `SafeFileHandle OpenHandle(string path, FileMode mode = Open, FileAccess access = Read, FileShare share = Read, FileOptions options = None, long preallocationSize = 0)` | <https://learn.microsoft.com/en-us/dotnet/api/system.io.file.openhandle?view=net-10.0> |
| `FileShare` | `None` declines sharing; `Read` allows later reads but not writes/deletes unless the relevant flags are included. | <https://learn.microsoft.com/en-us/dotnet/api/system.io.fileshare?view=net-10.0> |
| `CreateFileW` | Directory handles require native open with `FILE_FLAG_BACKUP_SEMANTICS`; share flags remain in effect until the handle closes. | <https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew> |
| `GetFinalPathNameByHandleW` | Gets the final resolved path for an opened handle; symbolic links resolve to their targets. | <https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfinalpathnamebyhandlew> |
| `GetFileInformationByHandle` | Returns handle metadata; VolumeSerialNumber + FileIndex can determine whether paths map to the same target. | <https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfileinformationbyhandle> |
| `BY_HANDLE_FILE_INFORMATION` | Carries attributes, size, link count, volume serial and file index. | <https://learn.microsoft.com/en-us/windows/win32/api/fileapi/ns-fileapi-by_handle_file_information> |
| Hard links / junctions | Hard links are multiple paths to one file on a volume; junctions are reparse-point directory links. | <https://learn.microsoft.com/en-us/windows/win32/fileio/hard-links-and-junctions> |

## Initial mechanism investigated, before the repair below

This section describes the initial experiment, not the admitted candidate contract. The repair
adds expected root/file identity binding and no-text nonmatch semantics. Neither version proves
actual editor span/range behavior.

1. Open the root directory with `CreateFileW`, `GENERIC_READ`, share mode `0`, `OPEN_EXISTING`, `FILE_FLAG_BACKUP_SEMANTICS`; hold it until after read/hash/decode and final metadata checks.
2. Open each intermediate directory ancestor the same way and hold those handles too. Root handle alone was not enough.
3. Open the file with `File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan)` and keep it open while reading, hashing and checking after metadata.
4. Before reading, reject relative escapes, reparse-point components and final paths outside the held root final path.
5. Read bytes from the same `SafeFileHandle` with `RandomAccess.Read`, hash the same byte buffer with SHA-256, and decode that same buffer with strict UTF-8/BOM UTF-16 handling.
6. Compare before/after root and file identity/metadata. If identity, length, attributes, link count or last-write metadata changes, return `ReadUnstable`.
7. Activate source spans only when the live hash equals the indexed hash. Hash mismatch is `Changed`; null hash is `Unverifiable`; hard link count greater than one is `Unverifiable`; invalid decode is `UnsupportedEncoding`; too large is `TooLargeToVerify`; cancellation before open is `Canceled`.

## Red-first evidence

| Step | Command / result | Oracle |
|---|---|---|
| Missing implementation red | `dotnet run --project spikes\code-atlas-source-reader\CodeAtlas.SourceReaderProbe.csproj --no-restore` failed with `CS0246: The type or namespace name 'OpenedSourceReader' could not be found`. | The case harness could not pass before the reader existed. |
| Candidate native red | First implemented directory handle with desired access `0`. Probe output: `FAIL|root rename blocked while root handle held`; `FAIL|ancestor rename blocked while ancestor handle held`. | If metadata-only directory handles protected root/ancestor replacement, those moves would have been blocked. They were not. |
| Holding mechanism green | Changing directory handles to `GENERIC_READ` with share mode `0` turned root and ancestor relocation cases green. | The case fails if either directory move succeeds while the handle is held. |

## Initial commands and observed results

| Command | Result |
|---|---|
| `python docs\ai-forward-pack\scripts\pack-doctor.py --root .` | 10 PASS, 2 WARN, 1 pre-existing FAIL (`knowledge graph`); also warned Windows uses `python`, not `python3`, and Copilot settings are long-context/high-effort. |
| `dotnet restore spikes\code-atlas-source-reader\CodeAtlas.SourceReaderProbe.csproj --source "%USERPROFILE%\.nuget\packages" --nologo --verbosity:minimal` | Restored SDK-only probe project in 47 ms. No package references and no manifest/dependency additions. Source was bounded to the local NuGet package folder; no network source was configured for this restore command. |
| `dotnet run --project spikes\code-atlas-source-reader\CodeAtlas.SourceReaderProbe.csproj --no-restore` | Final green run: `SUMMARY|passed=15|failed=0|not_proven=2|duration_ms=45`; runtime `10.0.11`; OS `Microsoft Windows NT 10.0.26200.0`. |

Initial pre-review raw output (15/0/2, retained as history):

```text
SUMMARY|passed=15|failed=0|not_proven=2|duration_ms=45
RUNTIME|10.0.11
OS|Microsoft Windows NT 10.0.26200.0
PASS|normal indexed hash activates spans|observed
PASS|line movement changes hash and disables spans|observed
PASS|invalid utf8 is unsupported|observed
PASS|oversize is not prefix verified|observed
PASS|precanceled read reports canceled|observed
PASS|root rename blocked while root handle held|observed
PASS|ancestor rename blocked while ancestor handle held|observed
PASS|file rename blocked while file handle held|observed
PASS|file write blocked while file handle held|observed
PASS|hardlink count is unverifiable|observed
NOT_PROVEN|file symlink outside is unverifiable|IOException
NOT_PROVEN|directory symlink inside/outside/cycle|IOException
PASS|junction outside is unverifiable|observed
PASS|relative escape is refused|observed
PASS|ads path is refused by hash mismatch or unavailable|observed
PASS|device path is refused|observed
PASS|unc-like relative path is refused|observed
```

## Fixture matrix and disposition

| Fixture | Outcome | Confidence |
|---|---|---|
| Normal file, indexed hash matches live bytes | `IndexedMatch`; spans may activate. | Verified |
| Same file with leading newline under old indexed hash | `Changed`; spans do not activate on moved bytes. | Verified |
| Invalid UTF-8 bytes | `UnsupportedEncoding`; no old anchors retained. | Verified |
| File over admitted byte bound | `TooLargeToVerify`; no prefix hash represented as full-file identity. | Verified |
| Pre-canceled token | `Canceled` before open. | Verified |
| Root directory rename while root handle held | Blocked only after directory handle used `GENERIC_READ` + share `0`; desired access `0` was disproven. | Verified |
| Intermediate ancestor rename while ancestor handle held | Blocked only after each ancestor directory handle used `GENERIC_READ` + share `0`; root-only protection is insufficient. | Verified |
| File rename/write while file handle held | Blocked with `FileShare.Read`. | Verified |
| Hard link | `nNumberOfLinks > 1` led to `Unverifiable`; no custody claim made. | Verified |
| Junction outside root | Reparse component led to `Unverifiable`; no content returned. | Verified |
| Relative escape, device path, UNC-like absolute path | `Refused` before read, or did not match indexed source. | Verified |
| Alternate data stream path | Did not produce `IndexedMatch`; accepted implementation must explicitly refuse ADS if it is outside input space. | Verified for no span activation; exact ADS policy still required. |
| File symlink outside root | Creation/read fixture returned `IOException`; not counted as pass. | NOT_PROVEN |
| Directory symlink inside/outside/cycle | Creation/read fixture returned `IOException`; not counted as pass. | NOT_PROVEN |

## Residual risks and security-review handoff

- This is a synthetic probe, not product integration. Production use still needs independent security review.
- Symlink fixtures are not proven on this machine. The production contract must either execute them on a machine that permits symlinks or explicitly refuse symlink input space before file open.
- ADS behavior did not activate spans, but the accepted contract should refuse `:` stream syntax explicitly before path resolution.
- The probe establishes Windows/.NET 10 behavior for this host. It does not claim POSIX portability.
- Telemetry was limited to console proof lines. Production instrumentation still needs OTel-shaped spans, stable result codes and no raw source in telemetry.

## Consolidated repair batch after independent reviews

This section supersedes the earlier 15 passed / 0 failed / 2 NOT_PROVEN run for the repaired probe code. The earlier red/green history remains evidence, not the final oracle.

### Review blockers repaired

| Finding | Repair | Evidence |
|---|---|---|
| Earlier ancestor handles leaked if later acquisition failed. | OpenAncestors disposes the acquired set on exception and rethrows. | PASS|partial ancestor failure releases prior handle|observed; the first acquired directory was renamed immediately after failed read without GC/finalizer help. |
| Mid-read OperationCanceledException escaped the result contract. | Read catches OCE only when the supplied token is canceled and returns Canceled with Text=null. | PASS|canceled after open reports canceled|actual=Canceled;...text=null;detail=canceled during read. |
| Changed/Unverifiable returned live text. | Only IndexedMatch returns text. Mismatch and missing-hash cases assert Text=null. | hash mismatch, same bytes after file replacement, and missing indexed hash cases all passed with 	ext=null. |
| ADS was accepted as a non-match rather than refused. | Relative file-id policy now refuses fully qualified paths, UNC/device prefixes and any : stream syntax before resolution. | PASS|ads path is refused exactly with no text|actual=Refused. |
| Mutation helpers accepted any IO/Unauthorized error. | Helpers now require Win32 low-word 5, 32 or 33, prove no destination collision, and prove move/write succeeds after handle release. | Root, ancestor, file rename/write cases all reported hresult=0x80070020;win32=32 plus after-release success. |
| Case wording overclaimed span correctness. | Case labels now say bytes/hash/text activation. Source spans remain in the E0 candidate, not in this probe. | Final raw labels:
ormal indexed bytes/hash returns text; line movement no longer appears as span proof. |
| Same bytes after object replacement could activate old anchors. | Added IndexedSourceBinding with opened root/file identity plus SHA-256 hash. Same bytes under a new file object returns Changed with no text. | PASS|same bytes after file replacement are Changed with no text|actual=Changed;detail=opened identity differs from indexed binding. |
| Symlink setup failure was too coarse. | NOT_PROVEN cases now carry exact HResult/message and remain uncounted. | hresult=0x80070522, required privilege not held. |

### Final repair command

dotnet run --project spikes\code-atlas-source-reader\CodeAtlas.SourceReaderProbe.csproj --no-restore

### Final repair raw output

`	ext
SUMMARY|passed=19|failed=0|not_proven=2|duration_ms=51
RUNTIME|10.0.11
OS|Microsoft Windows NT 10.0.26200.0
PASS|normal indexed bytes/hash returns text|actual=IndexedMatch;expected=IndexedMatch;text=present;detail=IndexedMatch
PASS|hash mismatch returns Changed with no text|actual=Changed;expected=Changed;text=null;detail=Changed
PASS|same bytes after file replacement are Changed with no text|actual=Changed;expected=Changed;text=null;detail=opened identity differs from indexed binding
PASS|missing indexed hash is unverifiable with no text|actual=Unverifiable;expected=Unverifiable;text=null;detail=Unverifiable
PASS|invalid utf8 is unsupported|actual=UnsupportedEncoding;expected=UnsupportedEncoding;text=null;detail=Unable to translate bytes [FF] at index 5 from specified code page to Unicode.
PASS|oversize is not prefix verified|actual=TooLargeToVerify;expected=TooLargeToVerify;text=null;detail=2051 bytes exceeds 1024
PASS|precanceled read reports canceled|actual=Canceled;expected=Canceled;text=null;detail=canceled before open
PASS|canceled after open reports canceled|actual=Canceled;expected=Canceled;text=null;detail=canceled during read
PASS|root rename blocked while root handle held|observed;IOException;hresult=0x80070020;win32=32; destination absent before; after-release move succeeded
PASS|ancestor rename blocked while ancestor handle held|observed;IOException;hresult=0x80070020;win32=32; destination absent before; after-release move succeeded
PASS|partial ancestor failure releases prior handle|observed
PASS|file rename blocked while file handle held|observed;IOException;hresult=0x80070020;win32=32; destination absent before; after-release move succeeded
PASS|file write blocked while file handle held|observed;IOException;hresult=0x80070020;win32=32; after-release write succeeded
PASS|hardlink count is unverifiable|actual=Unverifiable;expected=Unverifiable;text=null;detail=file has more than one hard link
NOT_PROVEN|file symlink outside is unverifiable|IOException;hresult=0x80070522;message=A required privilege is not held by the client. : 'C:\Projects\ai-de-atlas-e0-source-safety\spikes\code-atlas-source-reader\bin\Debug\net10.0\probe-runs\249cf101a645475f849930c3f223737b\src\OutsideLink.cs'.
NOT_PROVEN|directory symlink inside/outside/cycle|IOException;hresult=0x80070522;message=A required privilege is not held by the client. : 'C:\Projects\ai-de-atlas-e0-source-safety\spikes\code-atlas-source-reader\bin\Debug\net10.0\probe-runs\249cf101a645475f849930c3f223737b\inside-link'.
PASS|junction outside is unverifiable|observed
PASS|relative escape is refused|observed
PASS|ads path is refused exactly with no text|actual=Refused;expected=Refused;text=null;detail=file id is outside the relative source-file domain
PASS|device path is refused|observed
PASS|unc-like relative path is refused|observed
`

### Residuals after repair

- File symlink outside root: **NOT_PROVEN** because this host lacks the privilege to create the symlink ( x80070522).
- Directory symlink inside/outside/cycle: **NOT_PROVEN** for the same setup reason.
- Program exit success is not full safety acceptance; the raw summary carries
ot_proven=2 and security review remains required before production reader use.
- The probe is still synthetic Windows/.NET 10 evidence only. It does not admit product integration or POSIX behavior.

### Budget ledger

Original checkpoint was 30 calls including preflight/clarification. Previously reported consumption was 14/30. This repair segment used 13 harness tool invocations through commit, so reported checkpoint consumption is 27/30, with 3 remaining. If the coordinator counts each parallel view inside the wrapper separately, the checkpoint is effectively exhausted; no further work should be added without Owner renewal.
