# FOUNDATION — provenance of the vendored base-pack docs

The seven docs below are **vendored copies** from the base **Agent Knowledge Pack**, frozen into
this bundle so it is self-contained. They *will* diverge from the base over time; this manifest
makes divergence **visible** instead of surprising, in both directions.

**Check:** `python3 scripts/foundation-check.py` verifies the vendored files against the hashes
below (drift = an uncatalogued edit). Add `--base <path-to-base-pack>` to also compare against
the base copies. After an intentional vendored edit, update the known-divergence list below and
run `--update` to refresh the hashes. Hashes are sha256 (first 16 hex) over **normalized**
content (CRLF→LF, trailing whitespace stripped) so line endings never masquerade as drift.

| Vendored doc | Role | Vendored hash (normalized) |
|---|---|---|
| `agent-body-of-knowledge.md` | The reasoning constitution | `a9ea4361904db488` |
| `agent-rules-of-the-road.md` | Tiers, gates, the loop | `ac648e2dade4bdce` |
| `agent-persona-catalog.md` | The persona roster's source | `eaed01f23beb50a4` |
| `layered-optimized-architecture.md` | LOA — AI-integrated architecture | `05fbe0cf47f89c78` |
| `engineering-governance.md` | SDLC lenses around the code | `513c4a28581dc94f` |
| `testing-strategy.md` | The proof discipline | `c6351b2f55ee543d` |
| `csharp-style-guide.md` | C# house style | `17e7d5f02faf740e` |

## Known intentional divergences (vendored ≠ base, by design)

| Doc | Pack-side change | Status in base pack |
|---|---|---|
| `agent-body-of-knowledge.md` | Latest-stable-SDK default policy (currently .NET 10 LTS / C# 14; previews excluded; repo pin wins) | **pending back-port** |
| `testing-strategy.md` | Stale version/example references removed; tracks the latest-SDK policy | **pending back-port** |
| `csharp-style-guide.md` | §2.6 constant-on-the-left comparisons added; §2.2 example fixed to match | **pending back-port** |
| `agent-rules-of-the-road.md`, `agent-body-of-knowledge.md`, `csharp-style-guide.md` | **Deployment-path correction (FR-045).** The base docs' deployment map named `.github/instructions/{knowledge,csharp,loa,tests}.instructions.md` and a `.github/knowledge/` directory. This pack deploys one `<docname>.instructions.md` per knowledge doc and has no `.github/knowledge/`, so six of those paths did not exist - in documents the managed block loads every session. Corrected to the real paths. | Base is unchanged; the base's map reflects a different install layout. |

Everything else matches the base at vendoring time (engineering-governance differs only in line
endings, which normalization ignores). When the base pack absorbs a back-port, re-vendor the doc
here, clear its row above, and run `foundation-check.py --update`.
