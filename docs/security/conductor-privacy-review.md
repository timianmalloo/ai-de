---
id: privacy-review-conductor
title: "AI-DE Conductor — privacy review and provider record"
type: privacy-review
status: in-review
owner: "@timianmalloo"
phase: "1"
tags: [privacy, conductor, external-processing, provider-record, egress, anthropic, basis]
links:
  - { to: privacy-review-ai-native-ide, rel: refines }
  - { to: spec-conductor, rel: documents }
  - { to: plan-conductor-front-door, rel: relates-to }
review-by: 2026-12-09
summary: >-
  The provider record and supersession the parent review requires before ExternalProcessing may be
  enabled. Written after Privacy tripped its hard veto on the front-door slice and the human
  answered the three questions no agent could. Eight fields, each a cited value or an explicit
  dated "not published"; one field is deliberately left OPEN pending an observation only the
  operator can make.
---

# AI-DE Conductor — privacy review and provider record

> **Why this document exists.** `privacy-review-ai-native-ide` states: *"The initial product remains
> **direct-egress deny by default**. It does not call model providers or attach derived data to a
> model."* The Conductor spec **refines** the AI-native IDE spec, so that posture governed the
> Conductor until superseded — and **Phase 1 already ran a real governed run against a live
> subscription account.** The egress was not introduced by the composer; it had simply never had a
> recorded basis. A Privacy & Data Governance review convened on the front-door slice tripped its
> hard veto on exactly this, and its words are the standard this document is held to:
> **"An authorization is an input to the record, not a substitute for it. Risk-small is not
> basis-written."**

## Supersession

This record supersedes `privacy-review-ai-native-ide`'s `ExternalProcessing` disposition **for the
Conductor only**. The parent's rule that external processing *"requires a human-approved provider
record"* is **not** relaxed — it is **discharged**, by this document. Every other surface remains
under the parent posture.

## Scope, and the two narrowings that are load-bearing

**This basis is written for a single-operator desktop deployment in which the authorizer, the data
subject, and the reader of the compiled view are one person.** Confirmed by the operator on
2026-09-10: *"and yes its a single-operator desktop."*

> **VOID TRIGGER.** This basis is **void** for any multi-user, shared-workspace or unattended
> deployment, **which returns to the human.** It is also void the moment any text can reach the
> prompt that the human did not personally read in the compiled view — conductor-drafted replies,
> mention or template expansion, graph-query results, or any assist. (The second half is Ruling 44's
> trigger, restated here because the two share one assumption.)

**The authorization covers the operator's own data and repository content they control. It does not
extend to a third party's personal data inside an attached file** — a colleague's commit email, a
customer fixture, an issue export. *Single-operator shortens the consent chain for the operator's
data; it does nothing for data about someone who is not in the room.* No additional control is
required at this scale: C14(e)(iii)'s pre-read affirmation, C15's no-late-binding guarantee, and the
operator's read of the compiled view are the compensating controls.

## The provider record

**Provider:** Anthropic · **Tier:** Claude **Max** consumer subscription (not the API; no API key)
· **Path:** Claude Code driven over ACP by AI-DE.

All rows fetched live on **2026-09-10**. Each carries the source's own stated effective date, and
rows resting on an undated page are marked as such.

| # | Field | Value | Source · date |
| --- | --- | --- | --- |
| 1 | **Purpose / basis** | User-directed composition and dispatch of prompts to an agent, by the operator, for their own repository work. **Basis: the informed acceptance of the operator, who is also the data subject** (2026-09-10). Under the parent review's own classification this is `ExternalProcessing`, now permitted for the Conductor by this record. **EEA legal-basis mapping is Flagged** — the Privacy Policy §10 table lists *Consent* and *Legitimate interests* against "improve the Services … including model training", and the column mapping could not be cleanly extracted. Not load-bearing for a US-declared operator. | Privacy Policy §10 · **2026-07-08** |
| 2 | **Permitted data classes** | Prompt text the operator typed or pasted; goal-block field values; and — **only when `AttachEnabled` is true (C21, default false)** — the contents of files the operator individually picked, bounded by C14 (≤32 KiB/file, ≤128 KiB/send, ≤5 files) and filtered by C14(e)(iv)'s categorical refusal set. **Refused:** anything the page supplied, any path the page named, and every C14(e)(iv) class. |  |
| 3 | **Processor / subprocessor role** | **Anthropic is a CONTROLLER, not a processor, for consumer tiers.** *"If you live in the EEA, UK or Switzerland …, the data controller … is Anthropic Ireland, Limited. If you live outside …, the data controller … is Anthropic PBC."* The Privacy Policy explicitly *"does not apply to content that we process on behalf of customers of our business offerings"*, and the DPA *"is incorporated into … the Anthropic Commercial Terms of Service"* — **no consumer DPA exists.** | Privacy Policy §9, §1 · **2026-07-08**; DPA (undated) |
| 4 | **Residency / transfer mechanism** | *"your personal data is transferred to our servers in the **US**, or to **other countries outside the EEA and the UK**"* — **no named region set, and no consumer region commitment.** Residency controls (`inference_geo`, workspace geo) are **Claude API / Console only**; consumer Free/Pro/Max is out of scope. Transfer: **adequacy decisions and EU/UK/Swiss Standard Contractual Clauses**, plus statutory derogations. **EU-US Data Privacy Framework: NOT PUBLISHED as a mechanism** — the string appears **0 times** in both the Privacy Policy and the DPA. | Privacy Policy §5 · **2026-07-08**; data-residency doc (undated) |
| 5 | **User jurisdiction** | **US — declared by the operator, 2026-09-10.** *Not inferred.* See "Why nothing reads the locale" below. | Operator |
| 6 | **Training posture** | Contract language is **opt-out**: *"We may use your Inputs and Outputs to train and improve Anthropic AI models, **unless you opt out through your account settings**."* **Claude Code does not differ** — training applies *"including when you use Claude Code from these accounts"* for Free/Pro/Max. **Opt-out does not stop** training use for conversations flagged in safety review, or for materials submitted as feedback. | Privacy Policy §2 · **2026-07-08**; Consumer Terms · **2025-10-08**; Claude Code data-usage (**undated**) |
| 6a | **The account's actual toggle state** | **OPEN — see "The one open field".** The **default state for a new signup is NOT PUBLISHED**; Anthropic states only that users *"select your preference in the signup process."* | — |
| 7 | **Retention** | **Training ON:** *"we may retain your data in a de-identified format for **up to 5 years** in our model training pipelines."* **Training OFF:** *"Users who don't allow data use for model improvement: **30-day retention period**"* (stated for Claude Code, consumer plans). **Trust & safety:** if flagged, inputs and outputs **up to 2 years**, classification scores **up to 7 years**. **Feedback:** thumbs-up/down conversations **5 years**; Claude Code `/feedback`, `/bug`, `/share` transcripts **5 years**. **Legal hold** overrides all of the above. | Retention article · **2026-07-01**; Claude Code data-usage (**undated**) |
| 8 | **Deletion / rights path** | Deleting a conversation removes it *"from your chat history **immediately**"* and *"from our back-end storage systems **within 30 days**."* Turning training off stops future use of previous and new chats; **data already inside in-progress training runs or trained models remains.** Rights requests by contacting Anthropic; the policy states rights *"are limited"* and may be declined with a lawful reason. | Retention article · **2026-07-01**; Privacy Policy §4/§7 · **2026-07-08** |
| 9 | **Repository-policy authorization** | **Granted by the operator, 2026-09-10**, verbatim: *"For me personally all that is fine, we may want to have an 'opt-in' choice in the tool (in a settings) so that a case where that may not be ok we can restrict."* The opt-in is built as **C21**. | Operator |

### Explicitly not published — recorded as such, which is how this rule is meant to work

Privacy's disposition rule, adopted here: *"'Unknown fails closed' cannot mean 'blocked forever' —
that makes the rule unusable against any provider who does not publish. It means the field records
**not published by the provider as of \<date\>, checked at \<URL\>**, the **consequence** is named,
and the **acceptance is recorded as the human's** with the residual stated."* **A field that is
silently plausible instead of explicitly unknown is the failure mode.**

| Not published, as of 2026-09-10 | Consequence — what cannot be promised |
| --- | --- |
| The **default state** of the model-improvement toggle for a new consumer signup | We cannot state the posture from the tier alone. **Resolved by observing the account** — see below. |
| Any **processing-region commitment** for consumer tiers | We cannot promise where processing occurs beyond *"US, or other countries outside the EEA and the UK."* |
| A **named list of processing countries** for consumer traffic | The subprocessor list is behind an authenticated Trust Center. |
| **EU-US DPF certification status** | Absent from Anthropic's own documents; not confirmed against the Commerce list. Not load-bearing for a US-declared operator. |
| A **consumer DPA / processor terms** | None exists. Anthropic is a **controller**, so the operator is not a controller-using-a-processor and cannot impose processing instructions. |
| **Maximum retention for undeleted claude.ai chats with training off** | The two primary sources use "30 days" differently — the Claude Code doc as a retention period, the consumer retention article only as the back-end window **after deletion**. **Do not read row 7 as "chats are deleted after 30 days."** |

## The one open field, and it is the operator's to close

**Row 6a.** Because the toggle's default is not published, the record must carry **the account's
observed setting**, not a tier-level assumption. That is a one-minute observation only the account
holder can make:

> Open **`claude.ai/settings/data-privacy-controls`**, read the model-improvement setting, and record
> the value and the date here.

Until it is recorded, **the honest posture is the contract's**: training **may** occur, retention
**up to 5 years de-identified**. If the toggle is off, retention drops to the 30-day figure in row 7
and this record should say so with the observation date.

**This does not block the Conductor.** The basis, authorization, purpose and data classes are
recorded; row 6a determines *which* published retention figure applies, not *whether* the egress is
permitted.

## Why nothing in the product reads the machine locale

The operator suggested deriving residency from machine locale. **It is not a weak proxy — it is a
category error**, and Privacy was explicit that the failure mode is worse than an empty field:

> Gate 4's field is *"residency / transfer mechanism"*: a property of **Anthropic's infrastructure**
> plus the legal instrument for the cross-border hop. **No measurement on this machine can observe
> either.** Deriving it from locale would write a **fabricated** value into a record whose rule is
> *"unknown fields fail closed"* — and **a fabricated value does not fail closed, it passes.**

For **user jurisdiction**, locale is weaker still: a language/format preference, not a location, and
`en-US` is the default install everywhere. Timezone and OS region are better hints and both are
wrong for a traveller. And **auto-deriving jurisdiction means the product infers the operator's
location from device signals — itself a collection decision. A declared field asks; an inferred one
profiles.**

**Both fields live in this document. Nothing at runtime reads them.** *The cheapest privacy control
is not collecting it.* Any Phase-1 code that reads locale, timezone or OS region for a residency or
jurisdiction purpose is over-collection with no consumer, and is a defect.

## A finding this research surfaced, recorded because it is adjacent and real

**Claude Code stores session transcripts locally in plaintext** under `~/.claude/projects/`, for
**30 days by default** (`cleanupPeriodDays`). AI-DE drives Claude Code, so **prompt text and any
attached file contents also land there in cleartext on this machine**, outside `.aide/` and outside
anything this repository's `.gitignore` governs. It is machine-local and not repository-bound, so it
is not an egress finding — but it is a **retention surface neither this record nor the parent review
previously named**, and the operator should know it exists. *Source: Claude Code data-usage
(undated).*

## Re-verification

**Review-by 2026-12-09 — a 90-day interval, chosen on observed cadence rather than convention:**
twelve Privacy Policy versions since 2023-07, five in the twelve months to 2026-07 — roughly one
change every two to three months — and **one of them (2025-09-28) changed exactly the two fields
this record turns on.** A 90-day review catches a change before it is a version stale.

**Re-verify immediately on any of:** an in-app terms-acceptance prompt · a new dated version at the
Privacy Policy updates article · **a plan change** — consumer → Team/Enterprise flips role, terms and
retention all at once · a Claude Code release note touching telemetry or feedback.

**Net finding from the version history:** the training and retention terms **have not moved since
2025-09-28**.

## Residual risks accepted with this record

- **Whether "not published" is an acceptable value for the training-posture default is a regulatory
  judgement Privacy declined to make**, and it is recorded as the operator's acceptance rather than
  an agent's clearance.
- **C21 is per-session and operator-writable**, therefore a **default with a safe initial state, not
  an enforceable policy**. A deployment that must *prevent* attach needs a non-session-overridable
  layer — **Phase 2**.
- **Once sent, nothing is retractable.** Provider-side deletion is outside the product's reach; the
  affirmation in C14(e)(iii) is the last moment anything is decidable.
- **C14(e)(iv)'s refusal set is incomplete by construction** and must never be described as
  *"secrets cannot be attached."*
- **The API-key exception is a second egress class** with different third-party terms and needs its
  own record before it is enabled alongside attach.
- **The repository's third-party identifiers were not enumerated**, so the scope narrowing above is
  stated rather than measured.
