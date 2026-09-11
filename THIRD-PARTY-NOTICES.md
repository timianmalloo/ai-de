# Third-Party Notices

## AI-Forward Pack

This repository includes installed copies of the AI-Forward Pack and Agent
Knowledge Pack from:

- Source: <https://github.com/timianmalloo/ai-forward>
- Source commit: `07488efcf0a7282c6737120fec7262eba26acb27`
- Bundle version: `2026.08.16.2`
- Bundle revision: `45`
- License: Apache License 2.0
- License copy: [`docs/ai-forward-pack/LICENSE`](docs/ai-forward-pack/LICENSE)

The installed material includes:

- `.claude/`
- `.github/agents/`
- `.github/instructions/`
- `.github/prompts/`
- `.github/workflows/docs-health.yml`
- `docs/ai-forward-pack/`
- `docs/audit/`
- `docs/index.html`
- the managed AI-Forward Pack blocks in `AGENTS.md` and `CLAUDE.md`

Installation relocated files into tool-specific directories, added Copilot
frontmatter wrappers, inserted managed blocks, and pinned the active GitHub
Actions references. Those repository-specific changes do not change the
Apache-2.0 license of the underlying pack material.

## CodeMirror 6 (vendored, `src/AiDe.App/Web/vendor/`)

The shell ships a **single pre-built ES-module bundle** of CodeMirror 6, served to WebView2 over a
virtual host. It is a *distributed copy*, so the MIT licence's notice requirement applies to it — and
that requirement is discharged **here and by the committed licence texts**, not by the bundler.

**This is the PRODUCTION bundle, built once in node F4 (Ruling 33).** It replaces the F1 spike
bundle. The spike's sha256 `ee3d19a4…` was spike evidence and is not a production pin; the pin is
the manifest's, below. Security condition **C18** re-fired C1–C8 in full for this new dependency
set, and **C19** added a recurring advisory scan.

- License: MIT (all 24 packages; read from the `license` field of each package in the
  `npm ci --omit=dev` install tree, not asserted)
- License copy: [`src/AiDe.App/Web/vendor/LICENSES-codemirror.txt`](src/AiDe.App/Web/vendor/LICENSES-codemirror.txt)
  — every package's own `LICENSE` file, copied verbatim
- Manifest (hashes, versions, build command): [`src/AiDe.App/Web/vendor/vendor-manifest.json`](src/AiDe.App/Web/vendor/vendor-manifest.json)
- Built from: `vendor-src/composer-bundle/package-lock.json` (sha256 `1291dd0ab9daf0e21b980a832d58dceded72900d87a364818ae0b50c9df1f696`)
- Bundle sha256: `0e743c3564e14752e3f2938da50cd45e64e604225b5007fd208d0c3f76318762` (558198 bytes)
- Build command, one-off and recorded, run in `vendor-src/composer-bundle`:

```
npm ci --omit=dev
npx --yes esbuild@0.28.2 composer-entry.mjs --bundle --format=esm --minify --legal-comments=eof --outfile=../../src/AiDe.App/Web/vendor/codemirror-composer.bundle.mjs
```

**The input set is narrower than the spike's, and the narrowing is Security C7.** Dropped:
`codemirror` (the meta-package) and `@codemirror/lang-javascript` (the source viewer's, which F4
does not render). Added: `@codemirror/autocomplete` (the mention picker's `CompletionSource`) and
`@codemirror/commands` (history and `standardKeymap`). 26 packages → 24.
**`standardKeymap` rather than `defaultKeymap` is Security C11 expressed in the input set:**
`defaultKeymap` binds Ctrl-Enter to `insertBlankLine`, `standardKeymap` binds it to nothing, so the
accelerator the shell owns is not also a page command.

**`--legal-comments=eof` is applied and emits nothing, measured.** The 24 installed packages
contain zero `@license`/`@preserve` markers and no bang-comment inside the bundle's import graph, so
the bundle built with the flag is byte-identical to the bundle built without it. The flag is kept
because the day an upstream package adds one, it must survive — but it discharges no obligation
today, and treating it as though it did is how a notice requirement goes unmet while looking handled.

**Reproducible, measured rather than claimed.** The same install and the same pinned bundler
invocation reproduced these bytes exactly on 2026-09-11.

**Advisory posture (C19).** `npm audit --package-lock-only` over the committed lockfile reported
**0 vulnerabilities at every severity** on 2026-09-11, recorded at
`vendor-src/composer-bundle/advisory-scan.json`. *A one-off scan is a point-in-time observation — the recurring
step is the control, not the stored output:* `tools/verify-vendored-advisories.py` re-runs it and is
wired into `build.yml`.

**Nothing in this is part of the build.** No npm, node or esbuild step exists in `AiDe.sln`, MSBuild
or CI's build path; the build copies the committed bytes verbatim and
`tools/verify-vendored-assets.py` re-reads their hashes on every push.

The bundled packages:

- `@codemirror/autocomplete` 6.20.3
- `@codemirror/commands` 6.11.0
- `@codemirror/lang-css` 6.3.1
- `@codemirror/lang-html` 6.4.12
- `@codemirror/lang-javascript` 6.2.5
- `@codemirror/lang-json` 6.0.2
- `@codemirror/lang-markdown` 6.5.2
- `@codemirror/language` 6.12.4
- `@codemirror/legacy-modes` 6.5.4
- `@codemirror/lint` 6.9.7
- `@codemirror/state` 6.7.4
- `@codemirror/view` 6.43.11
- `@lezer/common` 1.5.2
- `@lezer/css` 1.3.6
- `@lezer/highlight` 1.2.3
- `@lezer/html` 1.3.13
- `@lezer/javascript` 1.5.4
- `@lezer/json` 1.0.3
- `@lezer/lr` 1.4.10
- `@lezer/markdown` 1.7.2
- `@marijn/find-cluster-break` 1.0.4
- `crelt` 1.0.7
- `style-mod` 4.1.3
- `w3c-keyname` 2.2.8
