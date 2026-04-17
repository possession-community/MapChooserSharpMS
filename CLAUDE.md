<!-- BEGIN modsharp-knowledge integration -->
## ModSharp reference material

Always-on context (loaded at session start via `@`):
- @refs/modsharp-knowledge/catalog/_index.md   — top-level index
- @refs/modsharp-knowledge/gotchas.md          — small, always worth keeping hot

Browse on demand (do NOT auto-load with `@`):
- refs/modsharp-knowledge/catalog/projects/Sharp.Shared/_index.md
- refs/modsharp-knowledge/catalog/projects/Sharp.Shared/namespaces/
- refs/modsharp-knowledge/catalog/indexes/
- refs/modsharp-knowledge/patterns/

## Workflow when touching ModSharp APIs
1. Start from @refs/modsharp-knowledge/catalog/_index.md to find the right project.
2. Read `refs/modsharp-knowledge/catalog/projects/Sharp.Shared/_index.md` to locate the namespace.
3. Read the relevant `namespaces/<Namespace>.md` file for type signatures.
4. Check `refs/modsharp-knowledge/patterns/` for a verified precedent.
5. Scan `@refs/modsharp-knowledge/gotchas.md` before committing tricky code.

## Notes
- Never `@`-load `catalog/projects/*/namespaces/*.md` directly — some files exceed 60k lines and will blow the context window.
- Pull catalog updates with `git submodule update --remote refs/modsharp-knowledge`.
<!-- END modsharp-knowledge integration -->
