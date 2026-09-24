# Graph Report - Mirror-Legacy  (2026-09-24)

## Corpus Check
- 92 files · ~41,793 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1210 nodes · 1787 edges · 67 communities (62 shown, 5 thin omitted)
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 45 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `86f050f1`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- [[_COMMUNITY_Data Models & Balancing|Data Models & Balancing]]
- [[_COMMUNITY_Victory, Karma & Ascension|Victory, Karma & Ascension]]
- [[_COMMUNITY_Combat Actions (Command)|Combat Actions (Command)]]
- [[_COMMUNITY_Test Infra & Asmdefs|Test Infra & Asmdefs]]
- [[_COMMUNITY_Conventions & Event Bus|Conventions & Event Bus]]
- [[_COMMUNITY_Story Events|Story Events]]
- [[_COMMUNITY_Headless Unity CLI|Headless Unity CLI]]
- [[_COMMUNITY_World Map & Diplomacy|World Map & Diplomacy]]
- [[_COMMUNITY_Annual Cycle & Random Events|Annual Cycle & Random Events]]
- [[_COMMUNITY_BRAIN_CLAUDE Workflow|BRAIN_CLAUDE Workflow]]
- [[_COMMUNITY_Mirror Powers & Combat Units|Mirror Powers & Combat Units]]
- [[_COMMUNITY_Tasks, Births & Espionage|Tasks, Births & Espionage]]
- [[_COMMUNITY_PlayMode Tests & Worked Lessons|PlayMode Tests & Worked Lessons]]
- [[_COMMUNITY_Wounds & Techniques|Wounds & Techniques]]
- [[_COMMUNITY_Save System (Newtonsoft)|Save System (Newtonsoft)]]
- [[_COMMUNITY_Xianxia Rules & Aging|Xianxia Rules & Aging]]
- [[_COMMUNITY_Combat AI Strategies|Combat AI Strategies]]
- [[_COMMUNITY_Audio Manager|Audio Manager]]
- [[_COMMUNITY_Clan Domain UI|Clan Domain UI]]
- [[_COMMUNITY_Claude Test Runner|Claude Test Runner]]
- [[_COMMUNITY_VFX, Pooling & Camera Shake|VFX, Pooling & Camera Shake]]
- [[_COMMUNITY_World Map UI|World Map UI]]
- [[_COMMUNITY_VFX Manager|VFX Manager]]
- [[_COMMUNITY_Claude UI Builder|Claude UI Builder]]
- [[_COMMUNITY_Clan Manager|Clan Manager]]
- [[_COMMUNITY_Building System|Building System]]
- [[_COMMUNITY_Breakthrough Tests|Breakthrough Tests]]
- [[_COMMUNITY_Deduction Engine|Deduction Engine]]
- [[_COMMUNITY_Mental Stability|Mental Stability]]
- [[_COMMUNITY_Resource Manager|Resource Manager]]
- [[_COMMUNITY_Turn Manager|Turn Manager]]
- [[_COMMUNITY_Blood Registry|Blood Registry]]
- [[_COMMUNITY_Breakthrough System|Breakthrough System]]
- [[_COMMUNITY_Scene Bootstrapper|Scene Bootstrapper]]
- [[_COMMUNITY_Building Data|Building Data]]
- [[_COMMUNITY_Game Manager|Game Manager]]
- [[_COMMUNITY_Time Manager|Time Manager]]
- [[_COMMUNITY_UI Theme Data|UI Theme Data]]
- [[_COMMUNITY_UI Theme Tags|UI Theme Tags]]
- [[_COMMUNITY_Ubuntu Setup Script|Ubuntu Setup Script]]
- [[_COMMUNITY_Community 40|Community 40]]
- [[_COMMUNITY_Community 41|Community 41]]
- [[_COMMUNITY_Community 42|Community 42]]
- [[_COMMUNITY_Community 43|Community 43]]
- [[_COMMUNITY_Community 44|Community 44]]
- [[_COMMUNITY_Community 45|Community 45]]
- [[_COMMUNITY_Community 46|Community 46]]
- [[_COMMUNITY_Community 47|Community 47]]
- [[_COMMUNITY_Community 48|Community 48]]
- [[_COMMUNITY_Community 49|Community 49]]
- [[_COMMUNITY_Community 50|Community 50]]
- [[_COMMUNITY_Community 51|Community 51]]
- [[_COMMUNITY_Community 52|Community 52]]
- [[_COMMUNITY_Community 53|Community 53]]
- [[_COMMUNITY_Community 54|Community 54]]
- [[_COMMUNITY_Community 55|Community 55]]
- [[_COMMUNITY_Community 56|Community 56]]
- [[_COMMUNITY_Community 57|Community 57]]
- [[_COMMUNITY_Community 58|Community 58]]
- [[_COMMUNITY_Community 59|Community 59]]
- [[_COMMUNITY_Community 60|Community 60]]
- [[_COMMUNITY_Community 62|Community 62]]
- [[_COMMUNITY_Community 64|Community 64]]
- [[_COMMUNITY_Community 65|Community 65]]
- [[_COMMUNITY_Community 66|Community 66]]
- [[_COMMUNITY_Community 67|Community 67]]
- [[_COMMUNITY_Community 68|Community 68]]

## God Nodes (most connected - your core abstractions)
1. `ClanDomainUI` - 23 edges
2. `AudioManager` - 22 edges
3. `MarriageMatchmakerTests` - 22 edges
4. `WorldMapUI` - 22 edges
5. `KinshipRulesTests` - 20 edges
6. `SaveSystem` - 16 edges
7. `Test` - 16 edges
8. `ClaudeUIBuilder` - 15 edges
9. `CombatUnit` - 15 edges
10. `TacticalCombatManager` - 15 edges

## Surprising Connections (you probably didn't know these)
- `Unity 2022.3 LTS (README stack, outdated)` --conceptually_related_to--> `Unity 6000.4.2f1 (Unity 6.4)`  [AMBIGUOUS]
  README.md → ProjectSettings/ProjectVersion.txt
- `CI test job (EditMode + PlayMode tests)` --conceptually_related_to--> `Edit Mode tests (NUnit, pure logic)`  [INFERRED]
  .github/workflows/unity-ci.yml → BRAIN_CLAUDE/TEST.md
- `CI test job (EditMode + PlayMode tests)` --conceptually_related_to--> `Play Mode tests (UnityTest, runtime)`  [INFERRED]
  .github/workflows/unity-ci.yml → BRAIN_CLAUDE/TEST.md
- `Unity CI/CD workflow (unity-ci.yml)` --references--> `Unity 6000.4.2f1 (Unity 6.4)`  [EXTRACTED]
  .github/workflows/unity-ci.yml → ProjectSettings/ProjectVersion.txt
- `CI test job (EditMode + PlayMode tests)` --shares_data_with--> `claude-output/result.json`  [EXTRACTED]
  .github/workflows/unity-ci.yml → BRAIN_CLAUDE/CLI.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Headless Unity test automation (CI, wrapper, bridge, hooks)** — workflows_unity_ci_test_job, brain_claude_cli_unity_claude_commands, scripts_unity_claude, claudebridge_claudetestrunner, brain_claude_test_batchmode_test_commands, brain_claude_test_pre_commit_hook [INFERRED 0.85]
- **Command-pattern combat actions** — brain_claude_memory_command_pattern, combat_icombataction, combat_attackaction, combat_moveaction, combat_defendaction, combat_techniqueaction, combat_itemaction, combat_fleeaction [EXTRACTED 1.00]
- **Annual four-phase game loop** — brain_claude_plan_annual_cycle, assets_project_scripts_core_timemanager_cs_core_timemanager, economy_taskassignmentsystem, events_eventmanager, diplomacy_factionmanager [EXTRACTED 1.00]

## Communities (67 total, 5 thin omitted)

### Community 0 - "Data Models & Balancing"
Cohesion: 0.07
Nodes (25): float, int, CultivationRealm, int, RandomEventType, string, int, List (+17 more)

### Community 1 - "Victory, Karma & Ascension"
Cohesion: 0.06
Nodes (15): CharacterData, CultivationRealm, CharacterData, DeathCause, string, CharacterData, DeathCause, Clan Karma (per-generation passive bonus) (+7 more)

### Community 2 - "Combat Actions (Command)"
Cohesion: 0.05
Nodes (28): CombatUnit, GridCell, CombatUnit, GridCell, CombatUnit, GridCell, CombatUnit, GridCell (+20 more)

### Community 3 - "Test Infra & Asmdefs"
Cohesion: 0.08
Nodes (18): CharacterData, CultivationRealm, CharacterData, Element, CharacterData, Element, Test, Assembly Definitions (+10 more)

### Community 4 - "Conventions & Event Bus"
Cohesion: 0.10
Nodes (16): CharacterData, CultivationRealm, DeathCause, GamePhase, Golden rules (EN code / FR docs, mandatory patterns), Code conventions (MirrorChronicles.<Module> namespaces, XML docs, OnEnable/OnDisable subscriptions), MVC pattern (data POCOs vs managers vs UI), Observer pattern (C# events bus) (+8 more)

### Community 5 - "Story Events"
Cohesion: 0.07
Nodes (16): MenuItem, string, Transform, Vector2, GameObject, GamePhase, List, StoryEventData (+8 more)

### Community 6 - "Headless Unity CLI"
Cohesion: 0.05
Nodes (29): MenuItem, MenuItem, string, Dictionary, MenuItem, claude-output/result.json, 📋 Commandes disponibles, 📋 Configuration (+21 more)

### Community 7 - "World Map & Diplomacy"
Cohesion: 0.16
Nodes (10): Color, MenuItem, string, Transform, Vector2, WorldMap scene (#42), Scenes: MainMenu, ClanDomain, TacticalCombat, WorldMap, ClaudeBridge (+2 more)

### Community 8 - "Annual Cycle & Random Events"
Cohesion: 0.21
Nodes (6): CultivationRealm, GamePhase, List, RandomEventType, EventManager, RandomEventData

### Community 9 - "BRAIN_CLAUDE Workflow"
Cohesion: 0.50
Nodes (3): End-of-session doc update order, Session start reading order, Input System (com.unity.inputsystem)

### Community 10 - "Mirror Powers & Combat Units"
Cohesion: 0.09
Nodes (14): CharacterData, CharacterData, GridCell, Divine Interventions (Qi Pulse, Ancestral Shield, Mirror Judgment), Player as consciousness trapped in ancestral bronze mirror, Wound lethality model (vitality thresholds, Dao Wound), WL-007: Wounds not persisted on CharacterData, MirrorChronicles.Characters (+6 more)

### Community 11 - "Tasks, Births & Espionage"
Cohesion: 0.09
Nodes (16): CharacterData, FactionData, CharacterData, GamePhase, List, TaskType, Child birth via ClanManager.GenerateChild (#14), Missing task types: Study, Teaching, Diplomacy, Espionage (#15) (+8 more)

### Community 12 - "PlayMode Tests & Worked Lessons"
Cohesion: 0.17
Nodes (7): CharacterData, Dictionary, Random, SetUp, Test, MarriageMatchmakerTests, MirrorChronicles.Tests.EditMode

### Community 13 - "Wounds & Techniques"
Cohesion: 0.08
Nodes (19): int, GamePhase, int, List, string, Memento pattern - save slots (#101), Newtonsoft JSON (com.unity.nuget.newtonsoft-json), Script Execution Order configuration (+11 more)

### Community 14 - "Save System (Newtonsoft)"
Cohesion: 0.15
Nodes (9): CharacterData, CultivationRealm, DeathCause, GamePhase, HashSet, List, StoryEventData, StoryTriggerType (+1 more)

### Community 15 - "Xianxia Rules & Aging"
Cohesion: 0.31
Nodes (5): Dictionary, GridCell, List, GridSystem, TerrainType

### Community 16 - "Combat AI Strategies"
Cohesion: 0.17
Nodes (12): CombatUnit, Dictionary, GridCell, List, AggressiveStrategy, BerserkerStrategy, CautiousStrategy, CombatAI (+4 more)

### Community 17 - "Audio Manager"
Cohesion: 0.10
Nodes (14): bool, CharacterData, CultivationRealm, float, GamePhase, IEnumerator, int, List (+6 more)

### Community 18 - "Clan Domain UI"
Cohesion: 0.11
Nodes (11): Button, CharacterData, DeathCause, GameObject, GamePhase, List, TaskType, TMP_Text (+3 more)

### Community 19 - "Claude Test Runner"
Cohesion: 0.11
Nodes (15): int, MenuItem, string, ClaudeBridge, ClaudeTestRunner, TestListener, DateTime, ICallbacks (+7 more)

### Community 20 - "VFX, Pooling & Camera Shake"
Cohesion: 0.07
Nodes (23): bool, Color, Element, float, GameObject, Vector3, bool, IEnumerator (+15 more)

### Community 21 - "World Map UI"
Cohesion: 0.13
Nodes (8): Button, Color, FactionData, GameObject, List, TMP_Text, Transform, WorldMapUI

### Community 22 - "VFX Manager"
Cohesion: 0.20
Nodes (11): CharacterData, float, Func, int, IReadOnlyList, List, Random, MarriageMatchmaker (+3 more)

### Community 23 - "Claude UI Builder"
Cohesion: 0.21
Nodes (5): CharacterData, GameObject, List, TacticalCombatManager, CombatState

### Community 24 - "Clan Manager"
Cohesion: 0.17
Nodes (5): CharacterData, DeathCause, GamePhase, ClanManager, MirrorChronicles.Clan

### Community 25 - "Building System"
Cohesion: 0.18
Nodes (4): BuildingType, BuildingData, BuildingSystem, MirrorChronicles.Economy

### Community 26 - "Breakthrough Tests"
Cohesion: 0.20
Nodes (8): CharacterData, CultivationRealm, GameObject, SetUp, TearDown, Test, BreakthroughSystem, BreakthroughSystemTests

### Community 27 - "Deduction Engine"
Cohesion: 0.25
Nodes (7): Dictionary, Element, List, TechniqueData, FragmentData, DeductionEngine, TechniqueType

### Community 28 - "Mental Stability"
Cohesion: 0.10
Nodes (12): CharacterData, CultivationRealm, DeathCause, GameObject, SetUp, TearDown, Test, MentalStabilitySystem (+4 more)

### Community 30 - "Turn Manager"
Cohesion: 0.26
Nodes (5): CombatUnit, List, MirrorChronicles.Combat, TurnManager, Queue

### Community 31 - "Blood Registry"
Cohesion: 0.22
Nodes (4): CharacterData, List, BloodRegistry, MirrorChronicles.Clan

### Community 32 - "Breakthrough System"
Cohesion: 0.20
Nodes (8): CombatUnit, GridCell, TechniqueData, MirrorChronicles.Combat, TechniqueAction, FragmentData, MirrorChronicles.Data, TechniqueData

### Community 33 - "Scene Bootstrapper"
Cohesion: 0.33
Nodes (4): MenuItem, string, ClaudeBridge, ClaudeSceneBootstrapper

### Community 34 - "Building Data"
Cohesion: 0.29
Nodes (4): BuildingType, CultivationRealm, BuildingData, MirrorChronicles.Data

### Community 35 - "Game Manager"
Cohesion: 0.33
Nodes (3): GameManager, MirrorChronicles.Core, GameState

### Community 37 - "UI Theme Data"
Cohesion: 0.38
Nodes (3): UIThemeManager, UIThemeData, UIThemeTag

### Community 38 - "UI Theme Tags"
Cohesion: 0.40
Nodes (4): MonoBehaviour, MirrorChronicles.UI, UIThemeTag, UIThemeRole

### Community 40 - "Community 40"
Cohesion: 0.20
Nodes (4): FactionData, GamePhase, List, FactionManager

### Community 41 - "Community 41"
Cohesion: 0.07
Nodes (28): 1. Core — Boucle principale, 2. Mirror — Interface joueur, 3. Clan — Lignée, 4. Characters — Individus, 5. Combat — Tactique tour par tour, 6. Diplomacy — Monde extérieur, 7. Economy — Ressources & tâches, 8. Events — Aléatoire + scénarisé (+20 more)

### Community 42 - "Community 42"
Cohesion: 0.21
Nodes (6): CharacterData, Dictionary, SetUp, Test, KinshipRulesTests, MirrorChronicles.Tests.EditMode

### Community 43 - "Community 43"
Cohesion: 0.09
Nodes (23): 1. Ai-je modifié une static class / pure function ?, 1. Edit Mode Tests (NUnit pur, rapides), 2. Ai-je modifié un MonoBehaviour ?, 2. Play Mode Tests (UnityTest, plus lents), 3. Ai-je modifié un event `GameEvents` ?, 4. Ai-je modifié un POCO (`CharacterData`, `GameData`, etc.) ?, 5. Ai-je modifié un `Singleton` Manager ?, 6. Ai-je modifié un ScriptableObject ? (+15 more)

### Community 44 - "Community 44"
Cohesion: 0.11
Nodes (19): Bootstrap du projet Unity, Build en ligne de commande (Linux/Mac/Windows), Build Settings — Scenes in Build, Changelog des packages, Commandes de référence, Configuration manuelle, Installation, 📦 PACKAGE.md — Registre des packages Unity (+11 more)

### Community 45 - "Community 45"
Cohesion: 0.11
Nodes (19): 🐛 Bugs identifiés dans le code existant (audit 2026-04-14), 📖 Comment utiliser ce fichier, 📐 Leçons d'architecture & design, 📝 Template pour nouvelles entrées, WL-001 — `deceased.RootElement` n'existe pas, WL-002 — `MirrorSystem.ConsumeMirrorPower` n'existe pas, WL-003 — `Element.Ice` absent de l'enum `Element`, WL-004 — `EventManager.GenerateYearlyEvent` n'existe pas + `TriggerYearlyEvent` est privée (+11 more)

### Community 46 - "Community 46"
Cohesion: 0.19
Nodes (5): CharacterData, CombatUnit, CultivationRealm, int, MirrorSystem

### Community 47 - "Community 47"
Cohesion: 0.06
Nodes (26): CharacterData, DeathCause, IEnumerator, State Machine pattern (GameState, GamePhase, CombatState), Unity Test Framework (com.unity.test-framework), Annual 4-phase cycle (Management -> Events -> Breakthrough -> Inheritance), Phase 2 - Combat & Events, Play Mode tests (UnityTest, runtime) (+18 more)

### Community 48 - "Community 48"
Cohesion: 0.18
Nodes (8): CLAUDE.md / master prompt (source of truth), Ironman save (yearly auto-save JSON), WL-101: Factions hardcoded instead of ScriptableObjects, MirrorChronicles.Characters, FactionData, MirrorChronicles.Data, MirrorChronicles.Data, MirrorChronicles.Diplomacy

### Community 49 - "Community 49"
Cohesion: 0.15
Nodes (13): 🏗️ Architecture & Structure, 🧬 Characters — Individus, 👥 Clan — Lignée, ⚔️ Combat — Tactique tour par tour, 🎯 Core — Boucle principale, 📦 Data — POCOs, 🌍 Diplomacy — Monde extérieur, ✅ DONE.md — Ce qui a été fait (+5 more)

### Community 50 - "Community 50"
Cohesion: 0.24
Nodes (4): CharacterData, CultivationRealm, DeathCause, AgingAndDeathSystem

### Community 51 - "Community 51"
Cohesion: 0.17
Nodes (12): 📌 Contexte projet, 📝 Conventions de code (rappel), 🏗️ Design patterns utilisés, 🎨 Direction artistique (rappel), 📋 Fichiers de suivi, 📁 Layout du projet (cible), 🧠 MEMORY.md — Mémoire projet Reflets de Lignée, 🎮 Pitch du jeu (résumé) (+4 more)

### Community 52 - "Community 52"
Cohesion: 0.31
Nodes (7): CharacterData, Func, HashSet, int, List, KinshipRules, MirrorChronicles.Clan

### Community 53 - "Community 53"
Cohesion: 0.31
Nodes (3): CharacterData, CultivationRealm, AscensionSystem

### Community 54 - "Community 54"
Cohesion: 0.33
Nodes (4): Key Xianxia business rules, Six cultivation realms (Embryonic -> DaoEmbryo), MirrorChronicles.Characters, MirrorChronicles.Combat

### Community 55 - "Community 55"
Cohesion: 0.22
Nodes (9): 🔴 BLOQUANT — Infra Unity & Compilation, 📐 Design patterns non encore implémentés, 📝 Liste de vérification pour passer à Phase 2, 🧹 Nettoyage, 🚧 NOT_DONE.md — Ce qui reste à faire, 🟢 Priorité BASSE — Phase 4 (Polish & Art), 🟠 Priorité HAUTE — Finir Phase 1, 🟡 Priorité MOYENNE — Finir Phase 2 (Combat & Events) (+1 more)

### Community 56 - "Community 56"
Cohesion: 0.50
Nodes (3): IReadOnlyList, CharacterNames, MirrorChronicles.Clan

### Community 57 - "Community 57"
Cohesion: 0.25
Nodes (8): ⚠️ 5 priorités absolues (Phase 2), 🧠 BRAINSTORMING.md — Point d'entrée central, 📝 En fin de session — Mettre à jour, En résumé :, 📖 Ordre de lecture optimal (début de session), 📖 Ordre de lecture optionnel (selon le besoin), 🔑 Règles d'or (rappel), 🎯 État ultra-court du projet

### Community 58 - "Community 58"
Cohesion: 0.50
Nodes (4): int, string, FactionTemplate, FactionPersonality

### Community 59 - "Community 59"
Cohesion: 0.12
Nodes (9): CharacterData, Func, GamePhase, Random, Phase 3 - World & Diplomacy, AllianceSystem, MirrorChronicles.Diplomacy, MarriageSystem (+1 more)

### Community 62 - "Community 62"
Cohesion: 0.40
Nodes (4): float, IEnumerator, int, GameSimulationTest

### Community 64 - "Community 64"
Cohesion: 0.40
Nodes (5): Unity bootstrap via temporary Universal 2D project merge, WL-008: Repository was not a valid Unity project, Unity 6000.4.2f1 (Unity 6.4), Unity 2022.3 LTS (README stack, outdated), Unity CI/CD workflow (unity-ci.yml)

### Community 65 - "Community 65"
Cohesion: 0.40
Nodes (5): Architecture, Documentation interne, Reflets de Lignée — Les Chroniques du Miroir, Stack, Statut

### Community 66 - "Community 66"
Cohesion: 0.50
Nodes (3): ECC, graphify, Project

### Community 68 - "Community 68"
Cohesion: 1.00
Nodes (3): Thematic parchment/bronze/ink UI (#61), Phase 4 - Advanced realms, Art & Polish, Shuimo ink-painting art direction

## Ambiguous Edges - Review These
- `MarriageSystem.cs` → `Key Xianxia business rules`  [AMBIGUOUS]
  BRAIN_CLAUDE/MEMORY.md · relation: implements
- `Unity 6000.4.2f1 (Unity 6.4)` → `Unity 2022.3 LTS (README stack, outdated)`  [AMBIGUOUS]
  README.md · relation: conceptually_related_to

## Knowledge Gaps
- **383 isolated node(s):** `ClaudeBridge`, `ClaudeBridge`, `string`, `ClaudeBridge`, `string` (+378 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **5 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `MarriageSystem.cs` and `Key Xianxia business rules`?**
  _Edge tagged AMBIGUOUS (relation: implements) - confidence is low._
- **What is the exact relationship between `Unity 6000.4.2f1 (Unity 6.4)` and `Unity 2022.3 LTS (README stack, outdated)`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `Edit Mode tests (NUnit, pure logic)` connect `Test Infra & Asmdefs` to `Combat Actions (Command)`, `Headless Unity CLI`, `Community 47`, `Community 59`, `Mental Stability`?**
  _High betweenness centrality (0.141) - this node is a cross-community bridge._
- **Why does `AudioManager` connect `Audio Manager` to `UI Theme Tags`?**
  _High betweenness centrality (0.075) - this node is a cross-community bridge._
- **Why does `DeductionEngine` connect `Deduction Engine` to `UI Theme Tags`, `Community 47`?**
  _High betweenness centrality (0.066) - this node is a cross-community bridge._
- **What connects `ClaudeBridge`, `ClaudeBridge`, `string` to the rest of the system?**
  _389 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Data Models & Balancing` be split into smaller, more focused modules?**
  _Cohesion score 0.07126436781609195 - nodes in this community are weakly interconnected._