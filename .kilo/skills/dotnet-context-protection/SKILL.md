---
name: dotnet-context-protection
description: Files and directories to exclude from context to prevent token bloat in this .NET WPF repo
---
# .NET Context Protection

Exclude these from any broad read, listing, or indexing pass. Never load them into context unless the task explicitly targets them.

## Build and output artifacts
- `bin/`, `obj/`, `artifacts/`, `dist/`, `out/`
- `msbuild.binlog`, `*.binlog`

## IDE and tooling state
- `.git/`, `.vs/`, `.idea/`
- `*.DotSettings.user`, `Folder.DotSettings.user`

## Generated code
- `*.Designer.cs` (WPF designer), `*.g.cs`, `*.generated.cs`
- Exception: `Properties/Resources.Designer.cs` is committed source here — read only when editing resources.

## Binary and media files
- `*.dll`, `*.exe`, `*.pdb`
- `*.png`, `*.jpg`, `*.ico` — never read as text; reference by path only.

## Data and logs
- `*.log`, `*.tmp`, `*.temp`, `*.db`, `*.sqlite`, `*.mdf`, `*.ldf`
- JSON/XML/CSV/TXT data files over 1MB

## Lock and cache directories
- `node_modules/`, `packages/`, `.codegraph/`, `.kilo/node_modules/`

## Practice
1. Prefer CCE indexed retrieval (`context_search`, `codegraph_explore`) over directory-wide reads — see `cce-priority`.
2. When listing manually, use `Get-ChildItem -Recurse -Force` with `-Exclude` for these paths.
3. Read config files (`App.config`, `.csproj`) directly but do not paste them wholesale into answers.
