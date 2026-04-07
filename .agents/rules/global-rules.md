---
trigger: always_on
---

# Codex Project Rules

Use these rules when working in this Unity project. Prefer the repository's existing patterns over broad rewrites.

## Operating Principles
- Act as a practical coding collaborator: inspect the project, make scoped changes, verify them, and report clearly.
- Preserve user work. Do not revert unrelated changes or regenerate project files unless the task requires it.
- Keep changes minimal and local to the request.
- Prefer built-in editor/project workflows, available MCP resources, and project skills when they fit the task.
- Use shell commands for inspection, validation, and safe automation when they are the most direct option. Prefer `rg`/`rg --files` for search.
- Do not invent MCP tools, Unity APIs, package names, or file paths. Verify them from local context first.

## Project Skills
- Check `.agents/skills/` before work that matches a project skill.
- When a relevant skill exists, read its `SKILL.md` and follow it unless the current codebase clearly requires a different approach.
- If a skill is stale or conflicts with the project, explain the conflict and keep the task scoped. Update the skill only when the user asks or the task directly depends on it.

## Unity And C# Standards
- Keep code compiling without new warnings where practical.
- Use simple, readable C# and small focused classes.
- Do not add new Unity packages, NuGet dependencies, or third-party assets unless explicitly requested.
- Put editor-only code under an `Editor/` folder or guard it with `#if UNITY_EDITOR`.
- Avoid fragile reflection and internal Unity APIs unless there is no stable alternative.
- Maintain existing version guards such as `UNITY_6000_0_OR_NEWER` when editing nearby code.
- Follow `.agents/rules/unity-code-style.md` for C# style.

## Assets And Project Files
- Follow `.agents/skills/unity-asset-organization/SKILL.md` when organizing or renaming Unity assets.
- Preserve `.meta` files and GUIDs. Move Unity assets with their `.meta` files when filesystem moves are necessary.
- Do not edit generated Unity files or package lock files unless the requested change requires it.
- Keep `Resources/` usage minimal; prefer direct references or Addressables for larger dynamic-loading needs.

## Validation
- Run the smallest useful validation for the change: compile check, targeted tests, Unity Test Runner, or focused static inspection.
- If Unity or tests cannot be run in the current environment, state that clearly and report what was checked instead.
- Before final response, review the relevant diff and check that unrelated working tree changes were not modified.

## Reporting
- Keep final reports concise.
- Include what changed, which validation was run, and any remaining risk.
- Mention unrelated pre-existing changes only when they affect the task or explain why they were left untouched.
