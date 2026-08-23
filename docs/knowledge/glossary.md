---
id: glossary
title: "AI-DE Glossary"
type: glossary
status: accepted
owner: "@timianmalloo"
phase: ""
tags: [glossary, wpf]
links: []
review-by: 2026-11-21
review-suggested: []
summary: >-
  Governed definitions for the small set of terms shared across the AI-DE starter code and project documentation.
---

# AI-DE Glossary

Only terms used by multiple code or documentation surfaces enter this initial
glossary. Business-domain vocabulary is absent because no product domain has
been specified.

## AI-DE

**Definition.** The repository and current Windows desktop starter name.

**Not to be confused with.** An acronym expansion or runtime AI capability.
Neither is recorded in the current code or documentation.

**Anchored by.** [`README.md`](../../README.md),
[`docs/architecture.md`](../architecture.md), and
`src/AiDe.App/ViewModels/MainWindowViewModel.cs:5`.

**Confidence.** **Verified** as a proper name; any expansion is **Flagged**.

## MVVM seam

**Definition.** The current limited separation between the WPF View
(`MainWindow`) and an immutable presentation object
(`MainWindowViewModel`).

**Not to be confused with.** A complete reactive MVVM architecture. There is
no domain Model, command layer, or property-change notification.

**Anchored by.** `src/AiDe.App/MainWindow.xaml:15-17` and
`src/AiDe.App/ViewModels/MainWindowViewModel.cs:3-16`.

**Confidence.** **Verified**.

## ViewModel

**Definition.** A presentation-facing type that exposes values consumed by a
View. The only current ViewModel is `MainWindowViewModel`.

**Not to be confused with.** A domain model or service. The current type
contains get-only display values and no business behavior.

**Anchored by.** `src/AiDe.App/ViewModels/MainWindowViewModel.cs:3-16` and
`tests/AiDe.App.Tests/ViewModels/MainWindowViewModelTests.cs:7-15`.

**Confidence.** **Verified**.

## WPF application

**Definition.** A Windows Presentation Foundation executable targeting
`net10.0-windows` with WPF enabled. In this repository, that executable is
`AiDe.App`.

**Not to be confused with.** A web application, service, or cross-platform UI.

**Anchored by.** `src/AiDe.App/AiDe.App.csproj:4-8`,
`src/AiDe.App/App.xaml:1-5`, and [`docs/architecture.md`](../architecture.md).

**Confidence.** **Verified**.

## Maintenance

- Add a term only when at least two artifacts need the same meaning.
- Link specifications, designs, and investigations to this glossary with
  `uses-term`.
- Re-check these definitions when the first product feature adds a domain,
  service, persistence, or runtime AI boundary.
