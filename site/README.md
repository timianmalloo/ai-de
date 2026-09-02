# `site/` — the public presence

Three static pages, no build step, no dependencies, no network calls.

| File | What it is |
|---|---|
| `index.html` | The landing page: the thesis and the counted facts behind it. |
| `model.html` | Idea one — the repository as one model with fifteen surfaces over it. |
| `collaboration.html` | Idea two — the board, the leaderboard and the ledger. |
| `assets/site.css` | The design layer. Tokens copied by value from `DESIGN.md`, with three extensions marked in the file. |
| `assets/site.js` | The three interactive demos. |

## Previewing it

Open `site/index.html` in a browser. Everything on the three pages works from a `file://` path,
including the demos.

**The `docs/` links will 404 locally.** They are written for the published layout, where the
workflow serves `site/` at `/` and `docs/` at `/docs/`. In the repository those two folders are
siblings, so the paths only resolve after assembly. This is the deliberate trade: the alternative
is absolute URLs, which break every preview and every fork.

## The rules the demos implement

`assets/site.js` reimplements three shipped rules in JavaScript so the page runs offline. **The C#
is the authority; if they disagree, the JavaScript is wrong.**

| Demo | Reimplements |
|---|---|
| Weave scorecard | `WeaveScorer` — `src/AiDe.Core/Watcher/WeaveScore.cs` |
| Leaderboard comparability | `LeaderboardComposer` — `src/AiDe.Core/Watcher/Leaderboard.cs` |
| Injection flag | `GraderInjectionScanner.Shapes` — `src/AiDe.Core/Watcher/MessageBoard.cs` |

If you change one of those rules in C#, change it here too. There is no test binding the two
together, which is a known gap recorded in `docs/reviews/site-craft-gate.md`.

## Craft

The deterministic craft detector runs over this folder:

```bash
python docs/ai-forward-pack/scripts/ui-craft-gate.py site --markdown
```

The current disposition of its findings is in `docs/reviews/site-craft-gate.md`. A clean run is a
floor, not a verdict — it cannot tell you whether the copy is true.
