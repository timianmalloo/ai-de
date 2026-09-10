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

- License: MIT (all 26 packages; read from the `license` field of each package in the
  `npm ci --omit=dev` install tree, not asserted)
- License copy: [`src/AiDe.App/Web/vendor/LICENSES-codemirror.txt`](src/AiDe.App/Web/vendor/LICENSES-codemirror.txt)
  — every package's own `LICENSE` file, copied verbatim
- Manifest (hashes, versions, build command): [`src/AiDe.App/Web/vendor/vendor-manifest.json`](src/AiDe.App/Web/vendor/vendor-manifest.json)
- Built from: `spikes/codemirror-composer/package-lock.json` (sha256 `fc3edc2235a59fd4838c737f5c92c393538ce2220ee81b0067e4e4d750c1b11f`)
- Build command, one-off and recorded, run in `spikes/codemirror-composer`:

```
npm ci --omit=dev
npx --yes esbuild@0.28.2 lib.mjs --bundle --format=esm --minify --legal-comments=eof --outfile=../../src/AiDe.App/Web/vendor/codemirror-composer.bundle.mjs
```

**`--legal-comments=eof` is applied and emits nothing, measured.** The 26 installed packages
contain zero `@license`/`@preserve` markers and no bang-comment inside the bundle's import graph, so
the bundle built with the flag is byte-identical to the bundle built without it. The flag is kept
because the day an upstream package adds one, it must survive — but it discharges no obligation
today, and treating it as though it did is how a notice requirement goes unmet while looking handled.

**Nothing in this is part of the build.** No npm, node or esbuild step exists in `AiDe.sln`, MSBuild
or CI; the build copies the committed bytes verbatim and `tools/verify-vendored-assets.py` re-reads
their hashes on every push.

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
- `@codemirror/search` 6.7.2
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
- `codemirror` 6.0.2
- `crelt` 1.0.7
- `style-mod` 4.1.3
- `w3c-keyname` 2.2.8
