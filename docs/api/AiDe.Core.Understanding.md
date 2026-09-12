---
id: api-aide-core-understanding
title: "API: AiDe.Core.Understanding"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Core.Understanding: 3 types, 23 members, 8% carrying a summary doc comment.
---

# API: `AiDe.Core.Understanding`

**3 public types · 23 public members · 8% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `AtlasIdentity`

*class* — `AtlasIdentity.cs`

Revision-independent Atlas logical identity for a source-declared type or supported source
member. Opaque caller tokens are preserved ordinally; the encoding never normalizes text.

| Member | Summary |
|---|---|
| `string Value { get; }` | **(gap)** |
| `AtlasIdentity ForType(` | **(gap)** |
| `AtlasIdentity ForMember(` | **(gap)** |
| `bool Equals(AtlasIdentity? other)` | **(gap)** |
| `bool Equals(object? obj)` | **(gap)** |
| `int GetHashCode()` | **(gap)** |
| `string ToString()` | **(gap)** |
| `bool operator ==(AtlasIdentity? left, AtlasIdentity? right)` | **(gap)** |
| `bool operator !=(AtlasIdentity? left, AtlasIdentity? right)` | **(gap)** |

## `AtlasSourceBindingMismatch`

*enum* — `AtlasSourceBinding.cs`

*No doc comment on this type.* **(gap)**

## `AtlasSourceBinding`

*class* — `AtlasSourceBinding.cs`

Manifest-bound source observation identity. The five source tokens are opaque ordinal values;
the content hash is the canonical `sha256:` plus 64 lowercase hexadecimal characters.

| Member | Summary |
|---|---|
| `string ManifestIdentity { get; }` | **(gap)** |
| `string ManifestFileIdentity { get; }` | **(gap)** |
| `string PolicyIdentity { get; }` | **(gap)** |
| `string RootIdentity { get; }` | **(gap)** |
| `string FileIdentity { get; }` | **(gap)** |
| `string ContentHash { get; }` | **(gap)** |
| `AtlasSourceBinding Create(` | **(gap)** |
| `bool Matches(` | **(gap)** |
| `AtlasSourceBindingMismatch CompareTo(` | **(gap)** |
| `bool Equals(AtlasSourceBinding? other)` | **(gap)** |
| `bool Equals(object? obj)` | **(gap)** |
| `int GetHashCode()` | **(gap)** |
| `bool operator ==(AtlasSourceBinding? left, AtlasSourceBinding? right)` | **(gap)** |
| `bool operator !=(AtlasSourceBinding? left, AtlasSourceBinding? right)` | **(gap)** |
