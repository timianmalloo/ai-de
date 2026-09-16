---
id: proof-atlas-audit-capture-repair
title: "Atlas audit capture repair evidence"
type: proof-pack
status: complete
owner: "@timianmalloo"
tags: [atlas, proof, audit]
links:
  - { to: plan-atlas-five-gates, rel: implements }
review-by: 2026-12-15
summary: "Append-only, truthful capture corrections for the 18 current Atlas audit gate failures."
---

# Scope and verdict

G1 is repaired on branch `fix/atlas-audit-capture` from base
`9301207ee8f1164b53f70fb7f2ead91377f8f22a`. The repair changes only the
append-only audit ledger through its official writer and this proof. It does not alter the gate,
watermark, ownership policy, source, tests, lessons, or other derived records.

`python tools/verify-audit-capture.py` initially exited 1 and named exactly 18 current skill
entries. Each original already carried `goal`, `done_when`, and `session`; all 18 lacked both a
`signals` object and a `docs/proof/` artifact. No existing entry superseded any of the 18.

The official command `python docs/ai-forward-pack/scripts/audit-log.py append --from-json -` was
invoked once per row. Each later correction:

- names the original with `supersedes`;
- carries the original prompt, goal, done condition, skill, session, outcome, artifacts, tags,
  tier, and fan-out cap;
- quotes the original summary verbatim inside a correction summary;
- records only `signals.verification_path=false`;
- states that the historical verification path is unrecorded and does not claim that verification
  ran or that acceptance was met.

The original rows remain unchanged. Existing artifact paths are historical references, not new
proof of original behavior.

# Original and corrective IDs

| Original | Corrective superseder |
|---|---|
| `al-01M2BEMRY2982SWFP0WEWD6FRC` | `al-01M2KM5PV2JD44Y5J215YYRV9M` |
| `al-01M2BG4ZRJ8Q8Y39W6ECMADHQ3` | `al-01M2KM5PXZK1Z5Z3PWSFAE4RWJ` |
| `al-01M2BH67QY4NYSHYT5JVGFRC5N` | `al-01M2KM5Q0TYHVK6ZYAT02Y4GTG` |
| `al-01M2BRGXMDZ5XB713SRTYVVBDE` | `al-01M2KM5Q3MDY21J0D4DTRTFKPD` |
| `al-01M2BY4RS1FJX31GH4PBBRKBWG` | `al-01M2KM5Q6PW583327RF69CPHFD` |
| `al-01M2DTEVGX06VN19RH27ZJJBY6` | `al-01M2KM5Q9MPBSHHVGJQE8FVREH` |
| `al-01M2DTKC9EEE5AXEHCW4MRTZBZ` | `al-01M2KM5QCERY4QA6HFYXCF772S` |
| `al-01M2DVY854QS9HRRWMFGB2VC8N` | `al-01M2KM5QFAV90X0F3WVYWT4AQT` |
| `al-01M2GTC866ZCBGC6M9F1AFBQ1H` | `al-01M2KM5QJ717GGWQWNRZEECSCY` |
| `al-01M2GWRJFPHQ8Q46QS4VQJXGZ9` | `al-01M2KM5QN1099YJPJ1TRKNZ3ZW` |
| `al-01M2GXR0CABF4GGPPS30MSKKR4` | `al-01M2KM5QQXGQV1T39C10NXYYF3` |
| `al-01M2GYSBXR9ARHK2T5XZX10D1V` | `al-01M2KM5QTR1D2FMY3WBJW50HXC` |
| `al-01M2H24BQRANRVZ2MP7RJK5WWW` | `al-01M2KM5QXNYSV3JQFPM9YEP69K` |
| `al-01M2HD2H96W7N0434V241A8W7Y` | `al-01M2KM5R0G4JW7DHFRK6D7KYC2` |
| `al-01M2HEKBTRTYSGV5MPTG40X4TG` | `al-01M2KM5R3F5KZXCW74XMNYH4MS` |
| `al-01M2HF46R88ZBE70M8XSRMQQ9F` | `al-01M2KM5R6HRR6GBMCB05XTXW52` |
| `al-01M2HJHKCSHF42RXEAA27BRY7N` | `al-01M2KM5R9DC8PRW6H78MMFDBXX` |
| `al-01M2HNK97556778ZDN9X71G9FN` | `al-01M2KM5RCBJDFK7AG2SB3VYQRC` |

# Conservation checks

The 18 originals were serialized as canonical JSON with sorted keys, compact separators, and the
table order above, then joined with newline bytes. SHA-256 before the writes:

`d88c0cd11cfd29f03d4fb0e7c30587b190661623bb8be327a03895b6b7261cb2`

The same measurement after the writes returned the same digest. The post-write census returned
`original_count=18`, `existing_superseders=18`, `missing_signals=18`, and
`missing_proof_artifacts=18`. This proves that the historical rows were not rewritten; the last two
counts remain true properties of the originals, while each now has one later truthful superseder.

The preparatory merge normalization was separately read from Git objects
`4473f260^1` (`a2135b7f`), `4473f260^2` (`901320c4`), and `4473f260`. The accepted fingerprint
excluded only `id` and `renumbered_from`. Comparison returned `alias_count=33`,
`missing_payload_count=0`, and `tracked_reference_alias_count=0`. The observed alias map is:

| Normalized alias | Retained ID |
|---|---|
| `al-01M2K2Z42Y0PX2P7TF8V3DXRQC` | `al-01M2GSWMFVBW7QM3J32VN5P909` |
| `al-01M2K2Z42YECJFCSNFF4WVE7F3` | `al-01M2GS9KKEXPF2018MXVSYP8XM` |
| `al-01M2K2Z42YHN83FCY652JAMAH9` | `al-01M2H4TJZCNBFCMPEFFHY7195C` |
| `al-01M2K2Z42YRRM2V2MPB133R06H` | `al-01M2GNTFZ2WXX6ST5SJQQ52Z79` |
| `al-01M2K2Z42YVFYACP4WYV7J9AGP` | `al-01M2GPXF95DJBM8HW4KVPMF43F` |
| `al-01M2K7KFT30NH34E0P7J62HHCY` | `al-01M2JZV6Y0AQ5F0V6RPCYHPE8G` |
| `al-01M2K7KFT32MY71700NCRDEDY4` | `al-01M2GNTFZ2WXX6ST5SJQQ52Z79` |
| `al-01M2K7KFT350XM549JBDNAQEHG` | `al-01M2GSWMFVBW7QM3J32VN5P909` |
| `al-01M2K7KFT35HRK1FT4TEEP5JHJ` | `al-01M2GS9KKEXPF2018MXVSYP8XM` |
| `al-01M2K7KFT38BE1BCDWE979X8FN` | `al-01M2GPXF95DJBM8HW4KVPMF43F` |
| `al-01M2K7KFT3GYQCTDNC5KNKV5Z7` | `al-01M2K0HMS28QHK6YS111NHS324` |
| `al-01M2K7KFT3K4X43S9N0R6WTS3F` | `al-01M2K1R3MANATTDTCV8NM55GTE` |
| `al-01M2K7KFT3MT0XDNRYHV2YJHXM` | `al-01M2H4TJZCNBFCMPEFFHY7195C` |
| `al-01M2K7KFT3Q3A4MZRSAEDZ8B51` | `al-01M2K1VDRTC41MCETH988RDHZV` |
| `al-01M2K7KFT3QH80KHESJ1MNXX8X` | `al-01M2K1GV0102SQXRWRP2TW82QH` |
| `al-01M2K7KFT3T1QYF21MAF5PZR40` | `al-01M2K0VCANA626N5V72W3X88P2` |
| `al-01M2K7KFT3YGSJPC7PHD3B9AN7` | `al-01M2K0R0V50JN4ZHTXQ1Z901ZV` |
| `al-01M2K7KFT3Z2V1ZETZQVNH0B3E` | `al-01M2K151CMPGVH9GDD51JHVA7Y` |
| `al-01M2K7KFT3ZJQYSDS1WJFZD1WZ` | `al-01M2K1GCQT3GRHWYW1RWC3B49Y` |
| `al-01M2KDTRBH0RTF2RB89446Q3P2` | `al-01M2GS9KKEXPF2018MXVSYP8XM` |
| `al-01M2KDTRBH4MSHPNF2W1RAH53X` | `al-01M2K0R0V50JN4ZHTXQ1Z901ZV` |
| `al-01M2KDTRBH4QSDN8M8X7J66XA3` | `al-01M2K151CMPGVH9GDD51JHVA7Y` |
| `al-01M2KDTRBHC5SHX53CDYFRYC73` | `al-01M2K1GV0102SQXRWRP2TW82QH` |
| `al-01M2KDTRBHG1Q3H7048P8XBMCX` | `al-01M2K0HMS28QHK6YS111NHS324` |
| `al-01M2KDTRBHHCMJX72BAAVHM7KG` | `al-01M2JZV6Y0AQ5F0V6RPCYHPE8G` |
| `al-01M2KDTRBHHR9CN3EKD4T2DBPX` | `al-01M2GPXF95DJBM8HW4KVPMF43F` |
| `al-01M2KDTRBHJRS9WTMMT5FK50T4` | `al-01M2H4TJZCNBFCMPEFFHY7195C` |
| `al-01M2KDTRBHNZFHSPKGM276PHT6` | `al-01M2K1VDRTC41MCETH988RDHZV` |
| `al-01M2KDTRBHPCYRBEADH2HCSKFS` | `al-01M2K0VCANA626N5V72W3X88P2` |
| `al-01M2KDTRBHSFWFSA8TDRXW9P1A` | `al-01M2GNTFZ2WXX6ST5SJQQ52Z79` |
| `al-01M2KDTRBHYS8ZYMM7VX2K7NCG` | `al-01M2GSWMFVBW7QM3J32VN5P909` |
| `al-01M2KDTRBHZAEGCQPGF0VGAYV2` | `al-01M2K1GCQT3GRHWYW1RWC3B49Y` |
| `al-01M2KDTRBHZQ8R3PRPV1HWBAFW` | `al-01M2K1R3MANATTDTCV8NM55GTE` |

These aliases were not restored or reminted.

# Executed checks

| Command | Exit | Observed result |
|---|---:|---|
| `python tools/verify-audit-capture.py` before repair | 1 | Exactly 18 current rows lacked evidence capture. |
| `python tools/verify-audit-capture.py` after repair | 0 | `270` checked skill entries carry goal, done condition, and evidence; `431` are frozen pre-existing debt. |
| `python tools/verify-audit-capture.py --self-test` | 0 | All 14 guards pass, including later-only supersession, noncompliant superseder refusal, multi-hop resolution, cycle termination, honest zero, and ULID coverage. |
| `python tools/verify-audit-log.py` | 0 | `850` audit entries and `165` change entries; zero duplicate IDs; `1,015` entries total. |
| `git diff --check` | 0 | No whitespace errors. |

The official writer also updated `docs/audit/audit-data.js`. That generated file is intentionally
left uncommitted for Conductor's final regeneration, as required by the repair ledger.
The lane close is recorded as `al-01M2KMBRT3C6N9B5KWN0WY595G` with this proof path and the
executed verification signals.

# Defect recurrence for Conductor

- Class: a skill episode can contain a goal and done condition while omitting every verification
  capture signal, leaving the episode Not Scored.
- Sweep: the current gate identified 18 instances; every instance had the same missing-evidence
  shape and no existing superseder.
- Derive: silence cannot distinguish an unrecorded verification path from an explicitly absent
  path. Historical silence also cannot support retrospective execution or acceptance claims.
- Prevent: the existing audit-capture gate and its self-test already enforce the class, including
  truthful false signals and compliant later superseders. No gate or watermark change is needed.

Conductor should serialize this recurrence under the existing DC-104 capture class. No shared
lesson-register write was made in this lane.

# Residuals

This proof establishes G1 only. It does not clear the other four Atlas gates, final integrated
qualification, Release, independent veto review, main publication, or E1/E2 continuation.
`AIDE_SESSION` and `AIDE_CONTRACT_LOG` were absent in this shell, so no Loomkeeper episode-close
event was emitted; the committed proof path is the durable evidence surface.
