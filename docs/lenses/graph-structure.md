---
id: lens-graph-structure
title: "Lens - graph structure"
type: doc
status: accepted
owner: "@maintainers"
tags: [lens, obsidian, dataview, structure]
links:
  - { to: lens-graph-health, rel: relates-to }
review-by: ""
summary: >-
  A read-time lens over the shape of the knowledge graph - artifacts by type and status, and
  the traceability chains (spec to design to proof). Derived, never authoritative.
---

# Lens - graph structure

> **This is a lens, not a record.** Every number below is *derived* from artifact
> frontmatter at read time. The frontmatter is the truth (V2); if they disagree, the
> frontmatter wins and this page is wrong. Nothing here is load-bearing, and no
> canonical document may depend on a query (M8).
>
> Queries need the **Dataview** plugin. Without it you will see the query source
> instead of a table - which is the honest degradation, not a failure.


## Everything, by type

```dataview
TABLE rows.file.link AS "artifacts"
FROM "."
WHERE type
GROUP BY type
```

## Traceability - designs and what proves them

```dataview
TABLE status, owner, links AS "typed links"
FROM "."
WHERE type = "design"
SORT file.name ASC
```

## Decisions - ADRs and decision notes

```dataview
TABLE type, status, owner
FROM "."
WHERE type = "adr" OR type = "decision-note"
SORT type ASC, file.name ASC
```

## Recently touched

```dataview
TABLE type, status, file.mtime AS "modified"
FROM "."
WHERE type
SORT file.mtime DESC
LIMIT 20
```

## Deeper structural insight

Degree, betweenness, components, orphans and structural gaps are computed
dependency-free by:

```
python3 docs/ai-forward-pack/scripts/obsidian-setup.py --analyze
```

For the interactive version inside Obsidian, use the **Knowledge Graph Analysis**
plugin (local metrics; AI features are opt-in and require your own API key).
