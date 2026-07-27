---
name: "Continue Project"
description: "Continue Project Mannequin from its bounded handoff without restoring previous chat history"
agent: "agent"
---

Do not restore, query, search, summarize, or reopen any previous Copilot chat or
session history. Read [the project handoff](../../Docs/PROJECT_HANDOFF.md) and
treat its `Immediate Continuation Order` plus the live workspace files as the
complete continuation state.

Follow the repository instructions, including the ban on `git status` and
unbounded command output. Briefly state the current checkpoint, then continue
the active implementation slice through focused validation. Do not stop at a
plan unless a genuine blocker requires user input.