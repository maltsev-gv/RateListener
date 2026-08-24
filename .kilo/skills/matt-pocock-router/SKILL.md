---
name: matt-pocock-router
description: Route tasks to the right local Matt Pocock skill and keep the workflow consistent.
---

# Matt Pocock router

Read this source of truth first:

- `AGENTS.md`

Then select the most specific local skill for the task.

## Primary routing

- Planning, requirement shaping, and repo workflow selection: `grill-with-docs` or `grill-me`
- Bugfixes and regressions: `diagnosing-bugs`
- Architecture and design seams: `codebase-design` or `improve-codebase-architecture`
- Research from primary sources: `research`
- Review: `code-review`
- Test-first implementation: `tdd`

## Default chains

- Unclear requirements: `grill-with-docs` -> `to-prd` -> `to-issues`
- Fast plan stress test: `grill-me` -> `to-prd` -> `to-issues`
- Bug/regression: `diagnosing-bugs` -> `tdd`
- Architecture/design: `domain-modeling` -> `codebase-design` -> `improve-codebase-architecture`
- Research-driven planning: `research` -> `domain-modeling` or `to-prd`
- Review: `code-review`
- Context transfer: `handoff`

This repo has no configured issue tracker. `to-prd` and `to-issues` write local markdown files instead of publishing tickets unless the user explicitly asks to publish to GitHub (`gh` CLI is available; remote is `github.com/maltsev-gv/RateListener`).

If a task fits more than one skill, prefer the most specific one and keep the chain short.
