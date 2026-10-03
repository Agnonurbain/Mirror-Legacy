## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).

## ECC

Before any task, load the matching ECC skill or agent (e.g. `ecc:csharp-testing` / `ecc:tdd-workflow` for tests, `ecc:csharp-reviewer` after C# edits, `ecc:planner` for multi-step features).

ECC's C# rules target generic .NET (xUnit, async/CancellationToken). This is a Godot 4.7.2 .NET game (C#, net8.0) — engine chosen 2026-09-25, replacing Unity: game rules and the whole simulation live engine-free in `src/Core` (NUnit 3 via `dotnet test`, no Godot types), the Godot layer in `game/` stays thin (partial `Node` classes, autoloads, text `.tscn` scenes). The Unity tree was removed at the end of phase G (history in git): never reintroduce MonoBehaviour-style singletons or ScriptableObjects. Static content lives in `game/data/*.json` (loaded and validated by `GameContentLoader`); the conventions are in `BRAIN_CLAUDE/MEMORY.md`.

## godot-ai

Always use the godot-ai MCP tools whenever they are available (the Godot editor open with the plugin in `game/addons`). Verify every gameplay or UI change in the game launched from the editor: `project_run`, `game_manage` (`get_scene_tree`, `get_ui_elements`, `input_mouse` clicks), `logs_read`, then `project_manage stop`. Tests alone are not enough. If the tools are not available, say so instead of silently skipping the check.

## Project

- Pilot docs live in `BRAIN_CLAUDE/` — start with `BRAINSTORMING.md`, remaining work in `NOT_DONE.md`.
- World lore (realms, techniques, paths, Fruitions, map, chronology) and the renaming lexicon: `BRAIN_CLAUDE/LORE.md` — source of truth. Never hardcode proper nouns from the source novel; display names live in data.
- Build, test and smoke-run headless with `./Scripts/dev.sh build | test | smoke | all` (Godot 4.7.2 .NET in `~/Godot`, override with `GODOT_BIN`). `test` needs only the .NET 8 SDK.
