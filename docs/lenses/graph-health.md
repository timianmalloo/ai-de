---
id: lens-graph-health
title: "Lens - graph health"
type: doc
status: accepted
owner: "@maintainers"
tags: [lens, obsidian, dataview, graph-health]
links:
  - { to: lens-graph-structure, rel: relates-to }
review-by: ""
summary: >-
  A read-time Dataview lens over the knowledge graph's health - stale artifacts, missing
  owners, missing freshness SLAs, and review-suggested flags. Derived, never authoritative.
---

# Lens - graph health

> **This is a lens, not a record.** Every number below is *derived* from artifact
> frontmatter at read time. The frontmatter is the truth (V2); if they disagree, the
> frontmatter wins and this page is wrong. Nothing here is load-bearing, and no
> canonical document may depend on a query (M8).
>
> Queries need the **Dataview** plugin. Without it you will see the query source
> instead of a table - which is the honest degradation, not a failure.


## Stale - past their `review-by`

```dataview
TABLE type, owner, review-by AS "due"
FROM "."
WHERE review-by AND date(review-by) < date(today)
SORT review-by ASC
```

## Flagged `review-suggested` (V16 propagation)

```dataview
TABLE type, owner, review-suggested AS "flags"
FROM "."
WHERE review-suggested AND length(review-suggested) > 0
SORT file.name ASC
```

## Missing an owner (V13)

```dataview
TABLE type, status
FROM "."
WHERE !owner
SORT type ASC
```

## Missing a freshness SLA

```dataview
TABLE type, owner
FROM "."
WHERE !review-by OR review-by = ""
SORT type ASC
```

## Draft or superseded

```dataview
TABLE type, status, owner
FROM "."
WHERE status = "draft" OR status = "superseded"
SORT status ASC
```
