## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).

## ECC

Before any task, load the matching ECC skill or agent (e.g. `ecc:csharp-testing` / `ecc:tdd-workflow` for tests, `ecc:csharp-reviewer` after C# edits, `ecc:planner` for multi-step features).

ECC's C# rules target generic .NET (xUnit, async/CancellationToken, `dotnet build`). This is a Unity 6.4 project: the conventions in `BRAIN_CLAUDE/MEMORY.md` win — NUnit via Unity Test Framework, MonoBehaviour singletons, OnEnable/OnDisable event subscriptions, C# 9.

## Project

- Pilot docs live in `BRAIN_CLAUDE/` — start with `BRAINSTORMING.md`, remaining work in `NOT_DONE.md`.
- Compile and test headless with `./Scripts/unity-claude.sh Compile | RunEditModeTests | RunPlayModeTests` (result in `claude-output/result.json`). Requires the Unity 6000.4.2f1 editor (`UNITY_PATH`).
