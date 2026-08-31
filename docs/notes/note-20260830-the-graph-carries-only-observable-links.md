---
id: "note-20260830-the-graph-carries-only-observable-links"
title: "The graph carries only observable links — docs and code are expected to be orthogonal"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "phase-3"
tags: [decision-note, graph, knowledge, evidence, provenance]
links:
  - { to: adr-0001-derived-evidence-views, rel: relates-to }
links-suggested: []
review-by: 2027-02-28
review-suggested: []
summary: >-
  Asked whether documentation should be joined to code, the answer is no: the graph carries only
  links that are declared somewhere observable, and docs and code being orthogonal is a useful
  property rather than a gap to close by inference.
---

# The graph carries only observable links

**Decided by the user, 2026-08-30**, in answer to a Core proposal to build a code↔knowledge join.

> "It's ok if docs and code are not linkable and orthogonal, they will tend to be orthogonal which is
> why pruning the graph on one or the other is a meaningful cut. Do not infer — the graph should only
> be on observable links/relationships."

## What prompted the question

Once the knowledge extractor landed, both halves of the graph existed in one store for the first
time, and the obvious next move was to join them: *which ADR governs which namespace?* Core measured
before proposing and found the honest answer: **no knowledge link in any measured repository targets
a code symbol.** Every `to:` in every document names another document id.

So a join could only have been built by inferring one — matching a document's title or id against a
namespace, or reading prose for names that look like types.

## The decision, and why it is the right one

**No inference.** A link enters the graph only when something in the repository *declares* it.

The reasoning that makes this more than caution: **orthogonality is information.** If docs and code
were joined by guesswork, filtering to one or the other would return a blurred set whose membership
depended on how good the guess was that day. Because they are separate, "show me the knowledge" and
"show me the code" are exact cuts — and the `node_class` dimension makes each a single question
rather than a list of type names to recognise.

An inferred edge also cannot be *un*-learned by a reader. It renders identically to a declared one,
and the only signal that it was a guess is a status label most people do not read. This codebase has
already paid for that twice — `depends_on` joined on a predicate alone and produced 7,426 false
Verified edges, and `uses_table` turned the sentence *"we update the record"* into a table called
`the`. Both were inference wearing a declaration's clothes.

## What this rules in

The join becomes buildable the moment it becomes **declared** — which is a change to how documents
are written, not to any reader:

```yaml
links:
  - { to: TheTerrace.Features.Fixtures, rel: governs }
```

The frontmatter reader already parses arbitrary `to`/`rel` pairs, so a link like that would enter the
graph today with no code change at all. Whether to adopt that convention is a documentation decision,
open and unforced.

## What this rules out, permanently

- Matching document titles or ids against namespaces, type names or file paths.
- Reading prose for identifiers that look like code.
- Any "probably relates to" edge, however it is labelled.

## Applied once already: `review-by` on code

The knowledge reader records a document's `review-by` date and raises a health finding when it has
passed — 460 of them on this repository. The obvious symmetry is to do the same for code, so a stale
class or a long-untouched namespace announces itself.

**No**, on this rule. Nothing in a C# file, a Bicep template or a SQL script *declares* when it
should next be re-read. A date could only be manufactured — from last-modified time, from churn, from
a heuristic about age — and the result would render identically to the dates documents actually
declare. A reader could not tell the two apart, which is precisely the failure this note exists to
prevent.

A code artifact gets a review date the moment something in the repository writes one down. Until
then, the absence is accurate.

## The general rule this states

**An edge in this graph is a claim that something in the repository says so.** Where nothing says so,
the correct output is no edge and — where a reader looked and found nothing — a disclosure saying it
looked. A missing edge is a gap a person can close by writing one down. An invented edge is a wrong
answer that looks exactly like a right one.
