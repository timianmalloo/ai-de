---
id: kb-azure-open-questions
title: "Azure Architecture Visualization — open questions & failure modes"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [azure, open-questions, failure-modes]
links:
  - { to: kb-azure-cloud-architecture, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  What the research could not settle about Azure architecture extraction, how this domain
  reliably fails, and the strongest counter-argument actively sought — that auto-generated
  cloud diagrams are inventory rather than architecture.
---

# Open questions & domain failure modes

## Unresolved by research

1. **Where does the service annotation live?** `metadata service = '…'` is valid at file level, but Bicep
   has no arbitrary typed resource-level decorator. If one module deploys resources for two services, a
   file-level annotation is ambiguous. Settled by deciding, and recording, either "a module *is* a service"
   or "ownership comes from a tag, not from `metadata`". *(Open; Bicep grammar Verified [S10])*
2. **How are cross-module edges recovered?** When a root file passes module A's output into module B's
   parameters, an implicit dependency exists between the modules but not between the individual resources
   inside them. Module-by-module extraction misses it; recovering it means tracing param/output chains
   through the compiled ARM JSON. Cost unknown. *(Open, Inferred)*
3. **What happens with no parameters file?** Production deployments always have one; a repository snapshot
   may not. Do we require a default/example parameters file, or emit a partial graph with explicit
   `unresolved` nodes? The second is more honest and more work. *(Open)*
4. **Is the icon licence compatible with a diagram-generating product?** The grant covers "architectural
   diagrams, training materials, or documentation". A tool that embeds and serves the icons as part of its
   own output may or may not sit inside that. This needs a real answer before shipping, not an opinion.
   *(Open; the licence text itself is Verified [S4])*
5. **How do we reconcile declared and live state?** A Bicep-derived graph and a Resource-Graph-derived graph
   will legitimately differ — deployment lag, drift, out-of-band changes. Which is authoritative for which
   question is undecided. *(Open; ARG's eventual consistency Verified [S5])*
6. **Would we emit `catalog-info.yaml`?** Backstage's `Resource`/`Component` model fits, but `spec.owner` is
   required and cannot be derived from Bicep alone without the service annotation or an ownership tag.
   *(Open; Backstage model Verified [S23][S24])*
7. **Unverified competitors.** Lucidscale's Azure depth (URL failed to resolve), Brainboard and
   Multicloud-diagrams were not directly fetched. Any claim about them is Flagged.

## Known failure modes of this domain

- **Inventory masquerading as architecture.** A diagram of every resource in a subscription is accurate and
  useless. It includes NSG rules, diagnostic settings, private endpoints and DNS zones — plumbing, not
  decisions. Azure's own reference architectures are architecture *because they are selective*.
- **Silent incompleteness.** Unpaginated Resource Graph queries return 1000 rows and no error. A Bicep
  extractor that skips `existing`, loops and conditionals returns a well-formed graph quietly missing whole
  subsystems. Both fail in the success-shaped direction, which is the expensive direction.
- **Guessing at unresolved parameters.** When a resource name is `'${prefix}-sql'`, the temptation is to
  substitute a plausible prefix. That converts an unknown into a wrong fact with no error bar.
- **Stitching by name-matching.** Where the service annotation is missing, inferring ownership from name
  similarity produces confident, undetectable mis-attribution. If it must be done, it must be labelled
  low-confidence at the node, not silently promoted.
- **Icon and shape drift.** Azure ships icon-set revisions (currently V24); third-party shape libraries lag,
  so a diagram can be simultaneously current and visually wrong about which service it depicts.
- **Runtime-only topology mistaken for the whole picture.** Application Map shows what calls what among
  *instrumented* components and nothing else — the inverse of the inventory failure, equally partial.

## Disconfirming views we deliberately sought

**The strongest counter-argument: auto-generated cloud diagrams are inventory, not architecture, and they
mislead precisely because they are accurate.**

The case, as it stands up:

1. *Architecture is intentional; inventory is accidental.* A resource list reflects what was deployed,
   including everything the platform required rather than everything the designer chose. The result is
   simultaneously correct and unusable.
2. *C4 already scopes this out.* A deployment diagram shows systems and containers on infrastructure — a
   `networkSecurityGroups/securityRules` child resource is a property of infrastructure, not a deployment
   node. Promoting it to a first-class node is noise that hides signal. *(Verified, [S22])*
3. *A tool in this space makes the argument against itself.* InfraMap justifies its existence relative to
   `terraform graph` by showing only "the resources that are most important/relevant" — conceding that the
   complete graph is the wrong artifact. *(Verified, [S28])*
4. *The inverse failure exists too.* Application Map shows logical service topology with no infrastructure
   at all. Neither inventory nor runtime topology is architecture. *(Verified, [S20])*
5. *No authority resolves it.* Microsoft publishes no taxonomy of which resource types are architectural, so
   the curation policy cannot be outsourced to a standard.

**How it fared:** it survives, and it is the most useful finding in this base. It does not argue against
extracting Azure resources into the graph — extraction is cheap and the data is genuinely useful for impact
queries and drift detection. It argues that **the curation layer is the product**: the policy deciding which
resource types become architectural nodes, which fold into a parent, and which are elided entirely. Without
that policy, this project reproduces the exact problem it set out to solve, with better tooling.

The one thing that weakens it: the argument assumes a single view. A graph store with *queries over* it can
serve both audiences — the full inventory for "what breaks if I change this", a curated projection for "show
me the architecture" — a possibility the diagram-first tools do not have. That does not remove the need for
the curation policy; it relocates it from extraction time to view time, where it is cheaper to change.
