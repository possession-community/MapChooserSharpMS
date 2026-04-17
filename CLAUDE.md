 <!-- BEGIN modsharp-knowledge integration -->
## ModSharp reference material

When you work on anything ModSharp-related, read these on demand
(do NOT use `@` auto-loading — some catalog files are huge and
`@`-prefixed paths in Markdown have caused Claude Code to fail to
start in this repo's integration history):

- `refs/modsharp-knowledge/catalog/_index.md` — top-level index, read this first
- `refs/modsharp-knowledge/gotchas.md` — known pitfalls, skim before committing tricky code
- `refs/modsharp-knowledge/catalog/projects/Sharp.Shared/_index.md` — main consumer-facing API
- `refs/modsharp-knowledge/catalog/projects/Sharp.Shared/namespaces/` — per-namespace type details
- `refs/modsharp-knowledge/catalog/indexes/` — cross-cutting indexes (by-attribute, by-interface, entry-points, generated-types)
- `refs/modsharp-knowledge/patterns/` — verified usage patterns

## Workflow when touching ModSharp APIs
1. Start from `refs/modsharp-knowledge/catalog/_index.md` to find the right project.
2. Read `refs/modsharp-knowledge/catalog/projects/Sharp.Shared/_index.md` to locate the namespace.
3. Read the relevant `namespaces/<Namespace>.md` file for type signatures.
4. Check `refs/modsharp-knowledge/patterns/` for a verified precedent.
5. Scan `refs/modsharp-knowledge/gotchas.md` before committing tricky code.

## Keeping the catalog current
- Before starting substantial ModSharp work, sanity-check the catalog age.
  If it looks stale, or if the user mentions an API that isn't in the
  catalog, suggest refreshing the submodule before proceeding:

      git submodule update --remote refs/modsharp-knowledge
      git commit -m "Update modsharp-knowledge submodule"

- Always commit the submodule pointer bump as its own commit so the
  refresh is easy to audit and revert.
- Do not silently refresh without telling the user — the new catalog
  may change what APIs are visible.

## Notes
- Never load per-namespace files unnecessarily — `_global.md` alone exceeds 60k lines.
- All the paths above are plain, do NOT prefix any of them with `@`.
  <!-- END modsharp-knowledge integration -->