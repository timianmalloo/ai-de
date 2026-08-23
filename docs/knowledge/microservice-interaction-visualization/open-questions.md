---
id: kb-micro-open-questions
title: "Microservice Interaction Visualization — open questions & failure modes"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [open-questions, failure-modes, sampling-bias, tracing]
links:
  - { to: kb-microservice-interaction, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The unsettled questions about trace-derived architecture, the ways this domain fails
  silently, and the sought counter-argument that trace-derived diagrams actively mislead.
---

# Open questions & domain failure modes

## Unresolved by research

1. **Messaging conventions are Development.** `messaging.*` attribute names and span-kind rules can change.
   Any async edge-minting must sit behind an adapter that records the semconv version it was written against.
   *(Verified status [S12]; the mitigation Inferred)*
2. **Does the Aspire Dashboard support any sampler configuration?** For a local tool the right answer is
   100% capture, but the dashboard's in-memory limits mean "keep everything" has a ceiling and no documented
   sampler. *(Flagged — no source found either way, [S19])*
3. **How is confidence modelled across a collection of traces?** One trace is one execution path. An edge
   seen once is not the same claim as an edge seen in every run. The graph needs an occurrence count and a
   scenario set on each observed edge, and the shape of that model is undecided. *(Open)*
4. **How is deep fan-out rendered?** `Task.WhenAll` across N services gives N overlapping activation boxes.
   The timestamps support the reconstruction; no reviewed tool renders it well. *(Open)*
5. **When does absence become actionable?** A declared edge with no observation could be dead code, an
   untested path, or a sampling miss. Distinguishing them needs coverage data the tracing side does not have.
   *(Open)*
6. **Third-party service maps unverified.** Datadog, Dynatrace, New Relic, X-Ray, Application Insights and
   Beyla were characterised from general knowledge, not fetched. *(Flagged)*
7. **Is the reflexion-model citation right?** The vocabulary is well established but the paper was not
   fetched here. Confirm before citing it in a design document. *(Flagged)*

## Known failure modes of this domain

- **Walking the tree instead of the pairs.** The obvious implementation — recurse parent→child — produces a
  service graph that is correct for HTTP and silently missing every message-driven flow. The failure looks
  like a clean, confident, incomplete diagram.
- **Reading a one-hop graph as transitive.** Jaeger's own docs warn that `A–B–C` does not mean a trace
  `A→B→C` exists. The warning is in the docs; the misreading is in the wild. *(Verified, [S20])*
- **Treating a dev-machine trace as the architecture.** One developer running one scenario, possibly with
  feature flags off and a stubbed dependency, produces a picture that is real and unrepresentative.
- **Head sampling on a single local run.** Sampling below 100% risks dropping the only trace of the rare
  path — and rare paths (error handlers, fallbacks, retries) are exactly the architecturally interesting
  ones. *(Verified reasoning from [S14])*
- **`service.name` discipline failure.** Unset `service.name` yields `unknown_service:dotnet` nodes, and the
  graph degrades into a hairball of identically-named boxes. *(Verified, [S6])*
- **Semconv migration blindness.** A parser written against only the stable names silently sees nothing from
  SDKs that have not opted in. *(Verified, [S8][S10])*
- **Cardinality explosion.** High-cardinality span names (user IDs in the name) defeat aggregation and blow
  up metrics; the spec prohibits it and instrumentation does it anyway. *(Verified, [S1])*
- **Unreadable sequence diagrams.** Without loop collapse and depth collapse, a real trace produces a
  diagram nobody can read — which conveys false confidence rather than no confidence.

## Disconfirming views we deliberately sought

**The strongest counter-argument: trace-derived architecture diagrams actively mislead, and their
authority is unearned.**

1. **They show an observation window, not a system.** Sampling, environment and time-of-day all shape what
   appears. Engineers treat the output as ground truth because it came from reality.
2. **Non-transitivity is routinely misread**, and this is documented rather than hypothetical: Jaeger's own
   documentation says the System Architecture graph does not imply transitive paths. *(Verified, [S20])*
3. **"Observed equals declared" may mean the tests are bad.** In reflexion terms, an *absence* is as likely
   to indicate an untested code path as a removed dependency. Trace-based absence is weak evidence, and a UI
   that renders it the same weight as divergence is lying by symmetry.
4. **Sequence diagrams from deep traces are unreadable at scale**, and AppMap's own depth-collapse controls
   are the admission. A filtered diagram conveys understanding it has not earned unless what was filtered is
   visible.
5. **eBPF tools see packets, not intent.** Hubble knows Pod A called Pod B; it cannot know whether that was
   a retry, a health check, or the business transaction. Edges without semantics produce dense, noisy graphs.
6. **Development-machine telemetry is not production architecture.** The Aspire Dashboard is explicitly a
   development tool; using it to characterise a system means accepting dev traffic as representative, which
   for anything with feature flags, A/B routing or environment-specific dependencies it is not.

**How it fared:** every point survives, and together they change the design rather than defeating it. The
correct response is not to abandon trace-derived views but to **refuse the authority they invite**: mark
every runtime edge as observed-with-evidence (count, scenario, date), never render absence and divergence
with equal weight, always show what a filter removed, and default local capture to 100% so at least the
sampling story is honest.

There is one place the objection has less force than it appears. Its whole weight rests on trace-derived
architecture being presented **alone**, as *the* picture. In a design where the runtime graph sits beside a
statically-extracted declared graph, a runtime edge is no longer a claim about the architecture — it is
evidence in a comparison, and the comparison is exactly where its partiality becomes informative rather than
misleading. *That* is the argument for building both sides, and it is the one thing the existing tools
cannot do because each of them only has one side.
