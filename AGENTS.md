## Agent skills

Project-local skills under `.kilo/skills/`:

Search and context:
- `cce-priority` — CCE-first search priority for code exploration
- `dotnet-context-protection` — files/directories to exclude from context

Workflow routing:
- `matt-pocock-router` — router for planning/improvement/workflow-selection tasks

Engineering workflows (Matt Pocock set):
- `grilling` / `grill-me` / `grill-with-docs` — rigorous questioning
- `planning-clarifier` — critical clarification pass before any plan
- `domain-modeling` — glossary and ADR discipline (`CONTEXT.md`, `docs/adr/`)
- `codebase-design` — deep-module vocabulary and seam design
- `improve-codebase-architecture` — architecture improvement workflow
- `prototype` — prototyping logic/UI
- `tdd` — test-driven development loop
- `diagnosing-bugs` — bug diagnosis loop with feedback-first discipline
- `research` — primary-source research captured as Markdown
- `code-review` — two-axis review (standards + spec) since a fixed point
- `to-prd` / `to-issues` — PRD and vertical-slice issue breakdown (local files by default)
- `handoff` — conversation compaction into a handoff document

Prefer local skills when task matches. Load with `skill` tool before proceeding. For planning-type tasks start with `matt-pocock-router`.

- C# style defaults from `C:\Junie`: follow existing project naming; do not use `_` prefix for private fields; for single-statement `if` / `else` / `foreach` / similar blocks, do not use braces and place statement on next line.
- PowerShell defaults from `C:\Junie`: do not ask for confirmation before routine reads/builds/tests/commands; prefer separate commands over long chains; use `\` in Windows paths; check `Test-Path` before creating directories.
- For repetitive insertions that shift line numbers, such as blank-line insertion, XML doc blocks like `///summary`, or similar scaffolding, apply edits from end of file toward beginning to avoid line drift between iterations.
- PowerShell search/file rule: do not use `Glob` for discovery in this repo — it drops hidden directories and misses recursive cases. Use `Get-ChildItem -Recurse -Force` for listings and `Select-String` for content search instead.
- Reuse known workarounds from `C:\Junie\command_errors.md`, especially for existing-directory creation, fragile PowerShell command chains, slow broad searches, path separator issues, and `HttpClient` reconfiguration errors.

### Interaction rules

- If the user asks a question without an explicit change request, answer only; do not modify code.
- If a change has ambiguity with several valid options, clarify before editing.
- Do not ask for confirmation on routine actions: reading files, running commands, build/test.

### Domain docs

Treat this repo as single-context; use root `CONTEXT.md` and `docs/adr/` if they exist, otherwise proceed silently.

### Runtime shape

- Single `net10.0-windows` (C# 14) WPF desktop app in `RateListener.csproj`; entry point `App.xaml.cs`, main window `OverviewWindow.xaml`.
- Hand-rolled MVVM: `ViewModels/` holds `ObservableObject`, `ViewModelBase`, `OverviewViewModel`, `ListenerSettingsViewModel`; relay commands live in `Service/RelayCommand.cs`; XAML value converters live in `Converters/`.
- Rates pipeline: providers implement `Providers/IRatesProvider.cs` (`BccFxProvider`, `BccStableProvider`, `CifraBankProvider`, `FfinProvider`); HTTP calls go through Flurl.Http, JSON through Newtonsoft.Json, model mapping through Mapster.
- Support layer in `Helpers/`: `Logger`, `ConfigHelper`, `CacheHelper`, `JsonHelper`, `RequestServiceHelper`; user settings persist via `Properties/Settings`.
- No database, no test project: validate changes with `dotnet build RateListener.sln` plus a manual app run for touched areas.

## Context Engine (CCE)

This project uses Code Context Engine for intelligent code retrieval and cross-session memory.

### Searching the codebase

**Use indexed retrieval instead of reading files directly** when exploring the codebase, answering questions about code, or understanding how things work. Prefer `codegraph_explore` for actionable symbol/method/field/flow retrieval when CodeGraph is available; use `context_search` for broad discovery and cheaper owner lookup.

When to use `context_search`:
- Broad discovery when you do not yet know exact symbols
- Cheap first-pass owner lookup for fields, properties, DTO members, or config keys
- Any time you would otherwise read files just to get bearings

When to use `codegraph_explore`:
- Symbol, method, field, or flow lookup when you need actionable source
- Behavior questions, multi-symbol traces, and change-planning
- Call-path or blast-radius questions

Other tools:
- `expand_chunk` for full source of a compressed result
- `related_context` for what calls/imports a function
- `session_recall` to recall past decisions

### CCE-first operating rule

- Treat indexed retrieval as default path for code exploration in this repo.
- Preferred discovery order: `session_recall` for prior decisions, `codegraph_explore` for methods, symbols, fields, flow, and change-planning when CodeGraph is available, `context_search` for broad discovery and cheap owner lookup, `expand_chunk` for full source, `related_context` for graph neighbors, then direct file reads only for exact known files you must edit or verify.
- Do not use broad read/search passes as first move when CCE can answer same question with indexed results and lower token cost.

### MCP startup and recovery

Servers configured in `.kilo/kilo.jsonc`: `context-engine-ratelistener-kilo` (cce.exe serve) and `codegraph-ratelistener-kilo` (codegraph serve --mcp).

- If one of these servers is unavailable, or its search tools are not exposed in current agent session, try to start and recover that server before using fallback search path.
- Assume the client should spawn the server binaries automatically from `kilo.jsonc`. Do not push manual startup onto user before first recovery attempt.
- Recovery checklist: attempt live tool call, verify tool exposure for that server, check `kilo.jsonc` server entry, verify local server binary/process responds, then use index recovery (`index_status`, `reindex`) if server is up but index is stale.
- If recovery fails, tell user which server is unavailable, name likely layer: tool exposure, MCP handshake, local binary launch, or index state, and ask user to start that server manually.

### Fallback rule

- Use ordinary workspace read/search tools only after CCE failed, or when exact file content is required for edit/patch validation.
- When falling back, say that fallback is happening because CCE was unavailable or insufficient for that step.

### Tool selection: CCE vs CodeGraph

- Use `context_search` for cheap first pass, broad discovery, and owner lookup when you only need a lightweight answer.
- If CodeGraph MCP is available and project is indexed (`.codegraph/` exists at repo root), use `codegraph_explore` for method lookup, behavior questions, multi-symbol traces, and edit planning where call paths or blast radius matter.
- Do not trust exact file-path lookups blindly from either MCP. If result drifts and you are about to edit, read the exact file directly.
- Both are first-class; do not fall back to grep/Read while either is available.

If the CodeGraph MCP server is not exposed in the session, or there is no `.codegraph/` directory, skip it entirely — indexing is the user's decision.

### Cross-session memory

Call `session_recall("topic phrase")` before answering non-trivial questions.
Call `record_decision(decision="...", reason="...")` after making choices.
Call `record_code_area(file_path="...", description="...")` after meaningful work.

Skip recording for trivial reads, formatting changes, or one-off lookups.

### Token limit handling

When hitting output token limits: continue seamlessly in a new request — pick up exactly where the previous response left off. No need to summarise or re-explain; just continue the task.

### Output style

Respond in compressed style. Drop articles (a, an, the) in prose. Use sentence fragments over full sentences. Use short synonyms (fix not resolve, check not investigate). Pattern: [thing] [action] [reason]. [next step]. No filler, hedging, pleasantries, trailing summaries, or restating what the user said. One sentence if one sentence is enough.

When suggesting code changes, show only the changed lines with 3 lines of context. Never rewrite entire files. Multiple changes in one file: show each change separately. Never echo back unchanged code the user already has.

Code blocks, file paths, commands, error messages: always written in full. Security warnings and destructive action confirmations: use full clarity.

### Useful references

Load these when working in the matching area:

- `.kilo/skills/cce-priority/SKILL.md` — indexed-retrieval priority order.
- `.kilo/skills/dotnet-context-protection/SKILL.md` — context exclusion rules for .NET artifacts.
- `.kilo/skills/<skill>/SKILL.md` — local skill instructions; companion files live alongside (e.g. `DESIGN-IT-TWICE.md`, `DEEPENING.md`, `ADR-FORMAT.md`, `CONTEXT-FORMAT.md`, `HTML-REPORT.md`, `LOGIC.md`, `UI.md`, `mocking.md`, `tests.md`).
