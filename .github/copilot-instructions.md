# Repository Command Safety

- Never run `git status`, `git status --short`, or `git status --porcelain` in this repository unless the user explicitly requests that exact command.
- Inspect changes with path-scoped Git commands such as `git diff --name-only -- <path>` and aggregate or cap output before returning it to chat.
- Never render a full list of generated `.import` files, asset files, Git LFS files, or repeated line-ending warnings in a tool result. Redirect noisy stderr and report counts plus a small sample.
- Keep `core.autocrlf=false` in the repository-local Git configuration. Do not normalize, discard, stage, or restore existing worktree changes as part of status inspection.

# Session Continuation Safety

- When the user asks to continue previous work, do not restore, query, search, summarize, or reopen a previous Copilot chat session.
- Read `Docs/PROJECT_HANDOFF.md` and continue from its `Immediate Continuation Order`. Treat that bounded checkpoint plus the live files as the continuation state.
- Keep continuation discovery path-scoped and output-bounded. Do not reconstruct prior work from full tool logs or terminal transcripts.