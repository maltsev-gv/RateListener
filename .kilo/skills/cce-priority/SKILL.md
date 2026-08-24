---
name: cce-priority
description: CCE-first search priority for code exploration
---
Use Code Context Engine tools before any other search method.

Order:
1. session_recall(topic) - past decisions/questions
2. codegraph_explore(query) - symbols, methods, fields, call paths when CodeGraph MCP is available
3. context_search(query) - relevant code chunks, broad discovery, cheap owner lookup
4. expand_chunk - full source when needed
5. related_context - graph neighbors
6. Read/Grep - only for exact known files to edit

Fallback only when CCE unavailable or exact file content required for edits.
