# Node actuals (GO19): planned against actual, the basis for each next cap

Owner ruling 2026-10-20: a node's cap is the **median** tokens and calls of the last 3-5 nodes of the same shape,
**plus 25%**, with the basis written in the brief. One row is added at each join. Tokens and seconds are the harness
`usage` the Leader received (`subagent_tokens`, `tool_uses`, `duration_ms`); "not recorded" where the Leader did not
receive it. Shapes: **measure** (read-only, full App runs), **gate** (a tool or check plus its fixture, no App
suite), **gate+sweep** (a test or gate change plus converted tests and App suites), **docs+test**.

| Node | Shape | Cap (calls / tokens) | Actual calls | Actual tokens | Seconds | Ratio (tokens) |
|---|---|---|---|---|---|---|
| T21 n9 | gate+sweep | 120 / 200k | 70 | 260,387 | 1,929 | 1.30 |
| T21 n10 | gate+sweep | 60 / 100k | 50 | 158,509 | 1,364 | 1.59 |
| T21 n11 | gate+sweep | 40 / 80k | not recorded | about 82k | not recorded | 1.03 |
| T36 | measure | 120 / 200k | 42 | 103k (node-reported) | 6,492 | 0.52 |
| T38 | gate | 40 / 80k | 41 | 121,296 | 1,486 | 1.52 |
| T37 | gate+sweep | 60 / 120k | 116 | 199,206 (incl. the +25k fixture resume) | 2,662 | 1.66 |
| T39 | gate | 20 / 40k | 29 | 102,891 | 760 | 2.57 |
| T40 | measure | 60 / 120k (kick withdrawn to finish N=10; hard stop 150k) | 43 | 112,675 | 4,838 | 0.94 |
| T41 | gate+sweep | 88 / 224k (GO19, first calibrated cap) | 31 | 104,970 | 1,365 | 0.47 |

## Caps from this table (median + 25%)

- **gate** (T38, T39; 2 nodes, below the 3-node floor, so Inferred): median 35 calls / about 112k -> **44 calls / 140k**.
- **gate+sweep** (the last 5: T21 n9, n10, n11, T37, T41): median 60 calls (n9, n10, T37, T41 recorded) / about 158.5k -> **75 calls / 198k**.
- **measure** (T36, T40; 2 nodes, so Inferred): median 43 calls / about 108k -> **54 calls / 135k**.

Every cap above is planned; the next join's row is the check on it.
