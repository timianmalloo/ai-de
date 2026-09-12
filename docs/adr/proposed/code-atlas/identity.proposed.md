---
id: adr-01M2BBCC9EHCWVR1R4ZCZ7502T
title: "PROPOSED — Atlas logical identities and declaration/source versions"
type: adr
status: proposed
owner: "@timianmalloo"
phase: "atlas-proposed-architecture"
tags: [code-atlas, proposed, identity, roslyn, provenance]
links:
  - { to: architecture-code-atlas-proposed, rel: refines }
  - { to: spec-addendum-e-code-atlas, rel: refines }
  - { to: proof-code-atlas-contract-grounding, rel: depends-on }
  - { to: note-atlas-e1-identity, rel: depends-on }
review-by: 2026-12-12
summary: >-
  Separates logical files/types/members from revision-bound declaration/content observations.
  Scope-qualified compiler identity prevents project collisions; partials and moved spans remain explicit.
---

# PROPOSED: logical identity is not a source version

- **Date / author:** 2026-09-12 / `atlas-architecture-astra`.
- **Deciders:** separate Owner and independent architecture gates pending.
- **Native ID provenance:** Conductor-supplied allocator result after `verify-id-allocators.py`
  observed 37 ADRs/no current duplicate; not independently allocated here. Registration pending.
  This semantic filename does not reserve a numbered global ADR.
- **Status:** PROPOSED, not accepted; no Core source admission.

## Context and evidence

Integrated candidate E at `a50329b2` requires real physical files and independently addressable members.
K0 at `e4229845` reports executed Roslyn 4.14.0.0 results:

1. `M:Same.Widget.Save(System.Int32)` differs from the string overload.
2. The same type/member documentation IDs collide in two project scopes.
3. Inserting lines preserves documentation IDs while identifier spans move.
4. Partial types have multiple syntax references. Partial method definition and implementation
   share a logical declaration ID but have different syntax references.
5. Nonexistent documentation-ID lookup returns null. This does **not** establish null-ID creation.

These are observed probe results recorded by another worker, not tests executed by this author.
Current `has_member` is display-only UML text. Its cap and omitted-member disclosure cannot
become an identity contract by parsing it.

## Proposed decision

Use versioned canonical tuple identities:

| Identity | Key |
|---|---|
| File | WorkspaceId, RepositoryRootId/worktree, normalized root-relative path. |
| Compilation scope | Workspace/root, project logical identity, TFM, declared compilation variant. |
| Type | Identity-schema, compilation scope, language, type kind, compiler documentation ID. |
| Member | Identity-schema, compilation scope, language, member kind, canonical declaration ID. |
| Declaration observation | Logical symbol ID, file/content binding, syntax occurrence/role, extractor version. |
| Content observation | File ID, exact byte hash/algorithm/length, decoding contract, observation/read state. |

No hash, revision, location or display signature enters the logical symbol identity. Compilation
inputs/options/ref hashes belong to observations; do not let every source change mint a new scope.
Same-file inclusion in multiple projects creates separate semantic scope memberships, not duplicate
physical files. Rename does not imply logical continuity; an explicit evidenced correspondence may
link old/new file identities. Branch names and Roslyn transient project GUIDs are not stable keys.

Canonical encoding is unambiguous and schema-versioned. If hashed for storage, full tuples are
retained and equality checked on collision. Root-specific filesystem case/normalization is part
of the registered path policy; unconditional lowercase is forbidden.

## Declaration/span contract

- Collect all partial type declarations and partial member definition/implementation parts.
  Store role and location; deduplicate identical syntax occurrences; user chooses when ambiguous.
- `identifierSpan`, `declarationSpan`, `bodySpan` have distinct meanings. Body is nullable.
  Syntax spans are zero-based half-open UTF-16 offsets over exactly the decoded hashed bytes.
  Human line/column is derived, never a key or a byte offset.
- Source hashes and decoder identity must match before activating a stored span.
  Changed source can be opened explicitly as live, without an old highlight.
- Supported E-0 declarations include methods/overloads, constructors, properties/indexers/accessors,
  fields/constants, events/explicit accessors, operators/conversions/destructors **only as each
  kind's contract is tested and advertised**. The probed mandatory methods/constructors/properties/
  accessors/partials are required for the walking skeleton, not optional future behavior.
- Implicit/synthesized/metadata-only/local/lambda/local-function/unresolved/malformed cases carry
  typed unsupported/unavailable states. A syntax-only occurrence can have a version-bound anchor,
  but not a fabricated stable member ID. Null-ID creation is a required additional test.

## Alternatives

| Alternative | Why rejected |
|---|---|
| Path + line as member key | A harmless line insertion changes identity and breaks comparison/history. |
| Global Roslyn documentation ID | Executed project collision disproves global uniqueness. |
| Fully displayed name or parsed `has_member` | Display signatures/caps are presentation, not semantic identity. |
| Content-addressed logical symbols | Conflates stable logical identity with observed version; every edit becomes a new entity. |
| One declaration per member | Loses partial definitions/implementations and produces arbitrary source selection. |

## Consequences, cost and controls

Positive: stable cross-view selection, overload distinction, explicit history and honest partials.
Cost: scope-qualified keys and multiple declaration observations increase retained metadata;
counts must distinguish logical symbols from declarations. No raw source retention is implied.
Generic/explicit-interface and remaining member-kind behavior requires compiler tests before support.

Required red-first controls: `ScopedSymbolsAndMovedDeclarations`; two projects/TFMs with matching
names; overload/constructor/property/accessor/partial fixtures; CRLF/non-BMP/encoding round-trip;
missing lookup and actual null-ID creation distinguished; no display parsing. A mutation that
removes scope, inserts revision into logical ID or keeps only the first partial must fail.

**LOA:** P1/P2/P5/P7/P9; T0 compiler/rules. C4 typed records, C8 named identity pattern,
C9 anti-conflation controls. Proposed design conformance, not implementation certification.
**Rollback:** old display/type query contracts remain unchanged; new IDs are versioned under Atlas
capability. Disable that capability rather than reinterpret historical IDs or migrate them blindly.
