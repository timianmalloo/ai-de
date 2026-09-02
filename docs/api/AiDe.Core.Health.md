---
id: api-aide-core-health
title: "API: AiDe.Core.Health"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Core.Health: 5 types, 7 members, 42% carrying a summary doc comment.
---

# API: `AiDe.Core.Health`

**5 public types · 7 public members · 42% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `IRevisionProbe`

*interface* — `FreshnessProber.cs`

Reads the repository's current revision for a scope, independent of the watcher.

## `FreshnessDrift`

*record* — `FreshnessProber.cs`

*No doc comment on this type.* **(gap)**

## `FreshnessProber`

*class* — `FreshnessProber.cs`

Detects silent watcher loss by comparing what the repository says to what the store indexed.

**Remarks.** The SRE review found the staleness metric self-referential: it measured against the daemon's own
last known event, so a dead watcher reads as perfectly fresh while the graph rots. Staleness has
to be measured against the repository, which is what this does — an independent probe, not a
second opinion from the same source.

| Member | Summary |
|---|---|
| `string DriftIncidentClass = "freshness.drift"` | **(gap)** |
| `IReadOnlyList<FreshnessDrift> Probe(IEnumerable<string> scopeIds, DateTimeOffset now)` | Probes each scope and raises an incident for every divergence found. |

## `HealthIncident`

*record* — `HealthIncidentSidecar.cs`

*No doc comment on this type.* **(gap)**

## `HealthIncidentSidecar`

*class* — `HealthIncidentSidecar.cs`

The durable incident channel, deliberately a small file **outside** the workspace database.

**Remarks.** The SRE review found the original design circular: disk-full, WAL-full and corruption move the
store to read-only, yet those are exactly the failures that must be recorded — an incident store
inside the database cannot record the failure that broke the database. Incidents therefore live
here, deduplicated by {class, scope} with an occurrence count so a flapping condition cannot
flood out the one incident that mattered, and unacknowledged incidents are evicted last.

| Member | Summary |
|---|---|
| `void Record(string incidentClass, string scopeId, string message, DateTimeOffset now)` | Records an occurrence, collapsing onto an existing incident of the same class+scope. |
| `void Acknowledge(string incidentClass, string scopeId)` | **(gap)** |
| `IReadOnlyList<HealthIncident> Read()` | **(gap)** |
| `IReadOnlyList<HealthIncident> Unacknowledged()` | **(gap)** |
| `string Describe()` | **(gap)** |
