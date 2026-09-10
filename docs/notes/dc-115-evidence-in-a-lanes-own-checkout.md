---
id: note-dc-115-evidence-in-a-lanes-own-checkout
title: "DC-115 — verifying a lane's evidence in the checkout it committed it in"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "2"
tags: [conductor, watcher, scoring, proof-pack, worktree, dc-115, phase-2]
links:
  - { to: plan-conductor-programme, rel: refines }
  - { to: spec-conductor, rel: relates-to }
review-by: 2027-03-09
summary: >-
  A governed lane in a linked worktree scored Not Scored for a Proof Pack it had committed on
  its own branch, because the verifier read the canonical repository path — the parent checkout,
  on another branch. The control verifies across the session's checkouts, admitting the lane's
  tree only when git's own .git pointer confirms it belongs to the bound repository. Records
  which of the three candidates was taken, why the other two were not, and what is still
  uncontrolled.
---

# DC-115 — verifying a lane's evidence in the checkout it committed it in

## The false measurement

A governed lane provisioned by spec §6.4 works in a linked worktree on its own branch. It commits
its Proof Pack there and declares the path on `episode-close`. `ClosedEpisodeScoring.EvidenceFor`
then verified that path against `SessionBinding.Repository.CanonicalPath` — an **identity**,
normalised so every worktree of one repository groups into one cohort. Handed to `File.Exists`, that
value names the **parent checkout**: a different working tree, on a different branch, which does not
contain the file.

The episode scored:

```
Not Scored — no minimum verification path
```

Every layer was individually correct — `ProofPackVerifier` honestly answered `NotFound` about the
path it was asked about — and the composed statement, *this agent produced no evidence*, was false.
The scorecard made a claim about the agent when the true claim was about where somebody looked.

## The decision

**Taken: candidate (a), verify against the session's checkouts, with the checkout observed rather
than claimed.**

`ProofPackVerifier.VerifyInCheckouts` folds a verdict per root: `Verified` beats `NotFound` beats
`Unverifiable`. Finding it anywhere is finding it; "we looked in a real checkout and it is not there"
is a fact about the evidence and outranks "we could not look", which is a fact about the product; and
looking nowhere claims nothing. The fold is order-independent, so a caller cannot get it wrong by
ordering it wrong, and the three-state verdict survives — it is not collapsed to a bool.

`ClosedEpisodeScoring.CheckoutsOf` decides which roots are legitimate. The repository is always one.
The lane's own tree is admitted **only when `IRepositoryLocator` reads its `.git` pointer and finds
the repository the session is bound to** — the same filesystem fact `RepositoryCorrection` already
resolves, so the two agree by construction rather than by coincidence.

That gate is not decoration. `worktree.path` is composed by the registrant, like every other
registration attribute. Admitting it on the claim alone would let a session keep an honest
`repo.path` — so its board partition and leaderboard cohort look right — while pointing evidence
verification at any directory on the machine. The claim is checked; it is never trusted.

Containment is unchanged and is applied **per root**: each candidate path is checked whole against
one root, must land inside that root's tree, and must sit under its `docs/proof/`. Several roots is
several complete checks, never a widened one.

**Not taken: candidate (b), ask git for the blob (`git -C <tree> cat-file -e <branch>:<path>`).**
It would also cover a tree that no longer exists, which (a) does not. It is declined because
`AiDe.Core` does not shell out — stated as a design constraint on `FileSystemRepositoryLocator`,
which reads the `.git` pointer file precisely to avoid a process launch on the ingest path — and
because a process per declared artifact on an idempotent sweep is a cost paid on every pass, forever.

**Not taken: candidate (c), route `Unverifiable` through `EpisodeEvidence`.** `EpisodeEvidence`
carries a bool, so "we could not look" still collapses into "there was none" at that boundary. Fixing
it changes what a scorecard says about episodes this node never touched, which is a scoring-semantics
change and out of this phase's scope. The existing test
`AnUnverifiableRepositoryIsNotEvidenceOfAbsence` continues to pin the limit at the verifier, where
the truth still exists.

## What is still not controlled

- **A lane whose tree was released before the scoring sweep ran.** The branch keeps the commit, but
  the working tree is gone, the locator answers "unknown", and the verdict falls back to the parent
  checkout's honest `NotFound`. Candidate (b) is the fix and it is declined above.
- **The `Unverifiable` collapse at `EpisodeEvidence`** — candidate (c), untouched.
- **The asymmetry between the two evidence doors.** `AuditLogEpisodeSource.HasProofPackArtifact`
  credits a declared path by substring and never touches the filesystem, so an audit-imported episode
  is evidenced by *naming* a path while a governed one must have the file present in a checkout. Two
  doors, two definitions of "has evidence", and the stricter one still applies to the lane the
  product itself drove.

DC-115 therefore moves to **partially-controlled**, not controlled.

## The generalisation

*A canonical identity is not a path.* The moment a value normalised for grouping is handed to
`File.Exists`, ask which of the several real directories it now names — and whether the one it names
is the one holding the thing you are looking for.
