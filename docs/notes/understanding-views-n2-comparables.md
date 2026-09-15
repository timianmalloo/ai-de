---
id: note-understanding-views-n2-comparables
title: "D-0 Solution/tree view comparables (Architecture-pane indexed artifacts)"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "understanding-views"
tags: [decision-note, understanding-views, comparables, d-0]
links:
  - { to: note-understanding-views-owner-ruling, rel: depends-on }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: adr-0017-primary-view-mode, rel: relates-to }
review-by: 2027-03-14
review-suggested: []
summary: >-
  Named, sourced comparables for D-0 Solution/tree: VS Solution Explorer, Rider/IntelliJ Project views, VS Code Explorer vs C# Dev Kit Solution Explorer, Eclipse Package/Project Explorer, Structurizr model/tree. Distinguishes Architecture-pane indexed tree from ADR-0017 Explorer. Key lesson: show unindexed folders as an explicit state (Rider no-index).
---

# D-0 Solution/tree view comparables (Architecture-pane indexed artifacts)

*Decision note (V17). Read-only research for `plan-understanding-views` N2. Does not specify, design, or freeze a control toolkit.*

- **Kind:** research / comparables (feeds `/specify` for D-0 only)
- **Confidence:** per row below (Verified | Inferred | Flagged)
- **Made during:** N2 domain research, session `understanding-views-comparables`, 2026-09-14
- **Binding admitted-when** (`spec-addendum-c-perspectives` §A5 D-0; owner ruling): Given a workspace, When the tree opens, Then code, data and architecture artifacts are listed by project and folder with a kind glyph, And activating an item reveals it in the Architecture graph or opens its source, And a folder the index has not covered is shown with an "unindexed" state.

## Scope fence (what this is not)

This product already has a **full-window Explorer** (`adr-0017-primary-view-mode`: Workbench | Explorer body swap). D-0 is an **Architecture-pane tree over indexed artifacts**, not a second workspace file explorer and not Code Atlas. Comparables below are mined for *navigator lessons* (grain, unindexed/excluded states, activate semantics). They are not UI to copy wholesale.

**Must not copy from any comparable:** editable tree CRUD (add/rename/delete as the primary job), a second graph store, or treating the Architecture tree as the whole-IDE file manager.

## Comparables table

| # | Comparable | One row is | Unindexed / excluded / unloaded | Activate does | Licence / source | Confidence |
|---|---|---|---|---|---|---|
| 1 | **Visual Studio Solution Explorer** | Solution → project / solution folder → project item (file, dependency, nested symbol when enabled) | **Show All Files** toggles visibility of **unloaded projects**; solution filters hide unloaded by default; unloaded stay in the solution but are not loaded for build/analysis | Preview / open in code editor; sync with active document; context build/NuGet/etc. | Proprietary (Visual Studio). [Solutions & projects](https://learn.microsoft.com/en-us/visualstudio/ide/solutions-and-projects-in-visual-studio); [Use Solution Explorer](https://learn.microsoft.com/en-us/visualstudio/ide/use-solution-explorer); [Filtered solutions / Show All Files](https://learn.microsoft.com/en-us/visualstudio/ide/filtered-solutions) | **Verified** (official Learn docs opened) |
| 2 | **JetBrains Rider Explorer** (Solution view) | Solution node → projects / items per `.sln`; separate **File System** view is disk hierarchy under the solution directory | Default = included-in-solution only. **Show All Files** adds on-disk items in **yellow**; items not indexed during analysis carry a **"no index"** label; unloaded projects excluded from build/inspection but still openable/searchable; external items marked **attached** | Open / preview in editor (single- or double-click configurable); Always Select Opened File | Proprietary (Rider). [Explorer tool window](https://www.jetbrains.com/help/rider/Project_Tool_Window.html) | **Verified** (official Rider help opened) — **strongest unindexed analog** |
| 3 | **IntelliJ IDEA Project tool window** | Project / module / content-root folder / file (Packages and Scope views change emphasis) | **Excluded** folders: ignored by completion, navigation, inspection; orange excluded-root icon; Project view can show/hide Excluded Files; scopes (Project Files, Open Files, user scopes) filter the tree | Open / preview in editor; Mark Directory As changes category | Proprietary (IntelliJ). [Project tool window](https://www.jetbrains.com/help/idea/project-tool-window.html); [Content roots / Excluded](https://www.jetbrains.com/help/idea/content-roots.html) | **Verified** (official IDEA help opened) |
| 4a | **VS Code Explorer** | Workspace folder → file/folder on disk | Hidden via `files.exclude` / `explorer.excludeGitIgnore` — **omission**, not an "unindexed" glyph | Single-click preview tab or open editor; Outline/Timeline are sibling sections | VS Code product: MIT. [User interface / Explorer](https://code.visualstudio.com/docs/getstarted/userinterface) | **Verified** — **file explorer**, not a solution/index tree |
| 4b | **C# Dev Kit Solution Explorer** (VS Code) | Solution → solution folders → projects → project files / references | Docs describe load/close solution and solution folders; **no documented "unindexed folder" state** for paths the language service has not covered | Double-click / select opens file or project file in editor; Build/Rebuild/Clean on solution/project | Extension on Marketplace (`ms-dotnettools.csdevkit`); requires C# Dev Kit. [Project management](https://code.visualstudio.com/docs/csharp/project-management) | **Verified** for solution vs file split; **Flagged** on unindexed semantics (not documented) |
| 5 | **Eclipse Project Explorer / Package Explorer** | Project Explorer: workbench resources (project/folder/file). Package Explorer: Java element hierarchy from build paths (source folders, libraries, types) | **Closed projects** remain in the navigator but are not editable and their resources do not appear as open workbench content; working sets and filters restrict what is shown (filter = hide, not an unindexed badge) | Open file for editing; Link with Editor; Close/Open Project | EPL (Eclipse). [Project Explorer](https://help.eclipse.org/2026-09/topic/org.eclipse.platform.doc.user/reference/ref-27.htm); [Package Explorer](https://help.eclipse.org/2026-09/topic/org.eclipse.jdt.doc.user/reference/views/ref-view-package-explorer.htm); [Closing projects](https://help.eclipse.org/2026-09/topic/org.eclipse.platform.doc.user/tasks/tasks-47.htm); [Working sets](https://help.eclipse.org/2026-09/topic/org.eclipse.platform.doc.user/concepts/cworkset.htm) | **Verified** (Eclipse 2026-09 help opened) |
| 6 | **Structurizr** model + **tree exploration** | Model element: person / softwareSystem / container / component, or deploymentNode / infrastructureNode / instances; tree exploration is a hierarchical visualisation of that model (not a workspace disk tree) | **No workspace "unindexed folder" concept** — the tree shows only what is in the authored model; gaps are absent nodes, not badged folders | Diagram / exploration navigation over the model; not "open source file" as primary | Lite: free OSS single-user; Cloud/on-prem paid. [DSL language](https://docs.structurizr.com/dsl/language); [Explorations (incl. tree)](https://docs.structurizr.com/ui/explorations/); [UI](https://docs.structurizr.com/ui) | **Verified** that a code+infra model tree exists; **Flagged** that it does **not** satisfy D-0's unindexed-folder admitted-when |

## Lessons D-0 must take (not UI to copy)

1. **Unindexed is a first-class visible state — never silent omission.** Rider documents yellow + **"no index"** for on-disk items the analyser has not indexed. VS Code Explorer's `files.exclude` *hides* paths; that fails §A5. Owner ruling: degrade to unindexed / "not recorded", never a plausible empty tree. **[Verified lesson from Rider; binding from owner ruling]**
2. **Grain is project/folder/artifact with a kind glyph, not a graph cluster.** VS / Rider / C# Dev Kit solution views are project-and-folder listings; `OverviewAsync` clusters are the wrong grain (owner ruling). Kind glyphs (file type / element kind icons) are universal in these trees. **[Verified]**
3. **Activate reveals elsewhere — it does not invent a third store.** IDE trees open the editor; D-0's admitted-when is Architecture graph neighbourhood **or** open source. None of the IDE trees maintain a second architecture graph; Structurizr's tree is a projection of one model. Do not add a second graph store (Ruling 53 / owner ruling). **[Verified / Inferred mapping to our activate paths]**
4. **Architecture-pane tree ≠ workspace file explorer.** VS Code and ADR-0017 Explorer are disk/workspace browsers. Rider explicitly splits **Solution** view vs **File System** view — the closest product acknowledgment that logical/indexed structure and disk tree are different jobs. D-0 is the former, docked in Architecture. **[Verified]**
5. **Excluded / unloaded / closed are related but not identical to unindexed.** IntelliJ Excluded = deliberately ignored by analysis. VS unloaded / Eclipse closed = present but not in the active analysis set. D-0's "folder the index has not covered" is closest to Rider's **no index**, not to user-chosen exclusion. **[Inferred distinction; do not collapse the states in the spec]**
6. **No single architecture tool fully matches code + data + architecture artifacts *and* unindexed folders.** Structurizr covers multi-kind architecture (code containers/components + infra deployment nodes) in one model/tree, but has no index-coverage badge for workspace folders. Treat Structurizr as the multi-kind hierarchy comparable; treat Rider as the unindexed-state comparable. **[Verified gap → Flagged residual]**

## What we must not copy

| Anti-pattern | Where it appears | Why it fails D-0 |
|---|---|---|
| Editable tree as primary job (New/Add/Delete/Rename files) | VS, Rider, IntelliJ, Eclipse, C# Dev Kit, VS Code Explorer | D-0 is a navigator over indexed artifacts, not a project system |
| Hiding unknown folders | VS Code `files.exclude`, working-set filters used as the only signal | Violates admitted-when unindexed state |
| File-system tree as the Architecture surface | VS Code Explorer; Rider File System view; ADR-0017 Explorer | Wrong host and wrong grain; Explorer already exists |
| Second authored graph / model store for the tree | Structurizr workspace-as-source-of-truth if duplicated beside our SQLite facts | Ruling 53 / owner ruling: one store |
| Using `OverviewAsync` clusters as rows | (internal anti-pattern named by owner ruling) | Wrong grain; no unindexed folders |

## Residual unknowns (Flagged)

| Unknown | Severity | Cheapest next probe |
|---|---|---|
| Exact visual treatment VS uses today for "on disk but not in project" under Show All Files (beyond unloaded projects) | Minor | Open a VS 2022/2026 instance on a project with an orphan file; screenshot — docs page for classic ghosted icons was not found (404 on a guessed Learn URL) |
| C# Dev Kit behaviour when a folder exists under a project but is excluded from the `.csproj` / not yet analysed | Major for parity claims only | Spike against Dev Kit docs release notes or a minimal `.csproj` with an unlisted folder — not required to admit D-0 if Rider lesson is adopted |
| Structurizr tree exploration row chrome (icons, click → diagram selection) | Nit | Open a non-encrypted public tree exploration once passphrase/public sample available; model hierarchy already Verified from DSL |

## Design implications for N3 `/specify` (handoff only — not a spec)

- Declare grain: one tree node = one indexed artifact **or** one unindexed folder (owner ruling E7).
- Require an explicit **unindexed** state in hard states (with empty / loading / error / no-workspace).
- Activate → existing `GraphAsync`/`DescribeAsync` neighbourhood **or** `NodeContentAsync` / admitted `codeviewer` — no third reveal path.
- Keep D-0 in the Architecture docking host; do not replace or subsume ADR-0017 Explorer.
- Do not freeze WPF `TreeView` vs alternatives here (N7 Spike Protocol).

## Sources opened this run

1. https://learn.microsoft.com/en-us/visualstudio/ide/solutions-and-projects-in-visual-studio  
2. https://learn.microsoft.com/en-us/visualstudio/ide/use-solution-explorer  
3. https://learn.microsoft.com/en-us/visualstudio/ide/filtered-solutions  
4. https://www.jetbrains.com/help/rider/Project_Tool_Window.html  
5. https://www.jetbrains.com/help/idea/project-tool-window.html  
6. https://www.jetbrains.com/help/idea/content-roots.html  
7. https://code.visualstudio.com/docs/getstarted/userinterface  
8. https://code.visualstudio.com/docs/csharp/project-management  
9. https://help.eclipse.org/2026-09/topic/org.eclipse.platform.doc.user/reference/ref-27.htm  
10. https://help.eclipse.org/2026-09/topic/org.eclipse.jdt.doc.user/reference/views/ref-view-package-explorer.htm  
11. https://help.eclipse.org/2026-09/topic/org.eclipse.platform.doc.user/tasks/tasks-47.htm  
12. https://help.eclipse.org/2026-09/topic/org.eclipse.platform.doc.user/concepts/cworkset.htm  
13. https://docs.structurizr.com/dsl/language  
14. https://docs.structurizr.com/ui/explorations/  
15. https://docs.structurizr.com/ui  
16. Repo: `docs/notes/understanding-views-owner-ruling.md`; `docs/specs/addendum-c-perspectives.md` §A5; `docs/adr/0017-primary-view-mode.md`
