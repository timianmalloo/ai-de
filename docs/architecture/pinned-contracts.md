---
id: architecture-pinned-contracts
title: "Pinned contracts registry"
type: architecture
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [contracts, pinned, template-schema, weave, loomkeeper, governance]
links:
  - { to: design-watcher-weave-score, rel: relates-to }
  - { to: design-watcher-coordination-contract, rel: relates-to }
  - { to: note-addendum-b-ratification, rel: depends-on }
  - { to: architecture-agent-plane, rel: relates-to }
review-by: 2026-12-10
summary: >-
  One page naming every pinned contract in this repository — id, version, home document, and
  evolution rule. Created by Ruling 29 because template-schema/1's validator ships in Phase 1 and
  enforces a shape, and a contract enforced in code with no documented shape is a shape asserted
  from code. It LINKS to weave/1 and loomkeeper/1 where they already live; it does not move or
  restate them.
---

# Pinned contracts registry

A **pinned contract** is a versioned shape that code enforces and another party depends on. Pinning
it means the version is a value in the code, a change to the shape is a change to the version, and
the two can be read side by side rather than one silently replacing the other.

**This page is an index, not a home.** Two of the three contracts below were pinned before it
existed and their definitions stay where they are. A registry that copied a definition would be a
second place to change, and the copy is the one that goes stale.

| Contract | Version | Home document | Enforced by | Evolution rule |
| --- | --- | --- | --- | --- |
| `weave/1` | 1 | [`docs/design/watcher-weave-score.md`](../design/watcher-weave-score.md) | `ScoreSchema.Weave1.Version`, asserted pinned (A6) | A change to a dimension, weight, posture or floor is a **contract change**, not a re-score. |
| `loomkeeper/1` | 1 | [`docs/design/watcher-coordination-contract.md`](../design/watcher-coordination-contract.md) | `CoordContract.Version`; a record carrying a different or missing version is **rejected and counted** (A6) | A schema change is a contract change, not a silent re-parse. |
| `template-schema/1` | 1 | this page (§ below) | `TemplateContract.SchemaVersion`, `AiDe.Core.Sessions.TemplateLoader` | **Additive within schema 1**; unknown frontmatter is **preserved, never rejected**; a breaking change means `template-schema/2` loading side by side. |

---

## `template-schema/1`

**What it governs.** The frontmatter of a prompt template file — Addendum B §B3.1's format, §B7's
pinned-schema clause.

**Frontmatter format and parser.** YAML, read by **YamlDotNet** (`PackageReference` in
`src/AiDe.Core/AiDe.Core.csproj`, version pinned centrally in `Directory.Packages.props`), scoped to
`src/AiDe.Core/Sessions/TemplateFrontmatterReader.cs` and used nowhere else in `src/`. The format is
YAML because §B3.1's own specimen uses **multi-line plain scalars** and **flow mappings inside a
block sequence** — exactly the upgrade trigger the repository's two hand-rolled subset readers
record for themselves (`KnowledgeFrontmatter.cs`, `BoundedContextMap.cs`). A third subset reader was
refused; so was JSON frontmatter, which would have extended §B3.1 rather than read it. Migrating the
two existing readers onto this dependency is a **recorded next step**, not part of the slice that
introduced it.

**The document never chooses a type.** Any node carrying an explicit YAML tag is **refused** before
anything is built, and the frontmatter is then read through the representation model, where every
value is text and the loader picks the type from the schema. There is no deserializer to configure,
so no later setting can turn a tag into a CLR type.

### The fields, and what is a constraint versus a preserved unknown

**`min` and `tier_default` are schema-1 CONSTRAINTS, not preserved-unknown fields.** Both appear in
§B3.1's own specimen, which is the document this schema was pinned from; treating them as unknown
would mean the schema's example carries fields the schema has never heard of, and a declared `min`
would be silently dropped. They are typed members of the model — `TemplateField.Min` and
`PromptTemplate.TierDefault`.

| Key | Level | Required | Meaning |
| --- | --- | --- | --- |
| `id` | template | yes | Catalog identity. Same-id templates resolve by source precedence. |
| `version` | template | yes | The template's own version; positive integer. Independent of the schema version. |
| `intent` | template | yes | What the prompt is for (§B4's intent). |
| `audience` | template | yes | Who reads the compiled prompt (§B4's audience). |
| `when_to_use` | template | **yes — load-blocking** | The picker tooltip headline. |
| `why` | template | **yes — load-blocking** | The tooltip detail. |
| `tier_default` | template | no — **schema-1 constraint** | The ceremony tier the template suggests. |
| `fields[].name` | field | yes | The wire name; body slots and recorded values key on it. |
| `fields[].type` | field | yes | `text`, `list` or `mentions`. |
| `fields[].required` | field | no | Whether the form engine blocks send without it. |
| `fields[].hint` | field | no | Inline guidance. |
| `fields[].min` | field | no — **schema-1 constraint** | Minimum item count for a `list` field. |
| anything else | template | n/a | **Preserved** in `PromptTemplate.UnknownFrontmatter`, never rejected. |

**Declared is not enforced.** `min` is validated as *well-formed* here — a positive integer, on a
list field — and compared against real values by the form engine (F4). Phase 1 counts nothing
against it. The same distinction the goal block already carries for `fan_out_cap` and `budget`:
**validated, not enforced.**

### What fails load

`when_to_use` or `why` missing or blank · no `id` · no positive `version` · no `intent` or
`audience` · an unknown field type · a malformed `fields` block · two fields with one name · a body
slot naming no declared field · a `min` that is not a positive integer or sits on a non-list field ·
an explicit YAML tag · malformed YAML · **two templates in one source claiming one id**.

A template that fails load is **not dropped**. It becomes a disabled catalog entry carrying its
errors, so a broken file is visible rather than absent. The catalog *view* that renders it is Phase 3
(R22); the model carries the failure now, because a view cannot restore what the model discarded.

### Sources and precedence

`personal > workspace > pack > built-in`. **Fixed now, though Phase 1 registers only workspace and
built-in** (Ruling 26 cut i), so registering pack or personal later is data — constructing a source —
rather than a renegotiation of which one wins. An override is **badged in the model**
(`CatalogEntry.IsOverride`, `ShadowedSourceIds`).

### Evolution

Additive within schema 1: new optional keys may be added, and any key this reader does not name is
preserved. A change that would make a previously valid template invalid, or change what an existing
key means, is **`template-schema/2`** — a new version loading side by side with this one, the
discipline `weave/1` and `loomkeeper/1` already follow.
