---
name: planning-clarifier
description: "Use when user asks for planning, implementation plan, refactoring plan, roadmap, decomposition, migration plan, rollout plan, design plan, or task breakdown. Before producing plan, force critical clarification: ask what is unclear, propose stronger alternative, surface objections, assumptions, risks, missing constraints, validation criteria, and cheaper options."
---

When task is about planning, do not jump straight to plan.

First, run a short challenge pass against request. Explicitly cover these questions in substance, even if wording varies:

- What is still unclear or underspecified?
- Can better approach be proposed than direct request suggests?
- Are there objections, contradictions, or hidden tradeoffs?
- Which assumptions am I making?
- Which constraints are missing: scope, deadlines, compatibility, performance, security, ownership, rollout, support?
- What could make this plan unnecessary, too expensive, or too risky?
- What is smallest viable version of plan?
- How will success be validated?
- What should be done now, later, or explicitly rejected?

If answers can be inferred from repository, current task, or surrounding discussion, infer them and state them. Do not ask user questions that can be answered by local evidence.

If critical ambiguity remains, start response with concise section:

## Clarifications Needed

List only blockers that materially change plan.

If ambiguity is non-blocking, continue and start response with concise section:

## Planning Challenge

Include:

1. Unclear points.
2. Better alternative if one exists.
3. Objections, risks, and tradeoffs.
4. Assumptions taken.
5. Validation criteria.

Then provide plan.

Plan must be actionable, ordered, and explicit about:

- target outcome;
- scope boundaries;
- affected files, modules, or systems when known;
- validation steps;
- rollback or safety notes when relevant.

Do not pad output. Challenge pass should be short but mandatory.