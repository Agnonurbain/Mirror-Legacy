# Graph Report - Mirror-Legacy  (2026-09-25)

## Corpus Check
- 105 files · ~75,146 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1492 nodes · 2185 edges · 92 communities (83 shown, 9 thin omitted)
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 43 edges (avg confidence: 0.82)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `253a6234`
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
- [[_COMMUNITY_Community 61|Community 61]]
- [[_COMMUNITY_Community 62|Community 62]]
- [[_COMMUNITY_Community 63|Community 63]]
- [[_COMMUNITY_Community 64|Community 64]]
- [[_COMMUNITY_Community 65|Community 65]]
- [[_COMMUNITY_Community 66|Community 66]]
- [[_COMMUNITY_Community 67|Community 67]]
- [[_COMMUNITY_Community 68|Community 68]]
- [[_COMMUNITY_Community 69|Community 69]]
- [[_COMMUNITY_Community 70|Community 70]]
- [[_COMMUNITY_Community 71|Community 71]]
- [[_COMMUNITY_Community 72|Community 72]]
- [[_COMMUNITY_Community 73|Community 73]]
- [[_COMMUNITY_Community 74|Community 74]]
- [[_COMMUNITY_Community 75|Community 75]]
- [[_COMMUNITY_Community 76|Community 76]]
- [[_COMMUNITY_Community 77|Community 77]]
- [[_COMMUNITY_Community 78|Community 78]]
- [[_COMMUNITY_Community 79|Community 79]]
- [[_COMMUNITY_Community 80|Community 80]]
- [[_COMMUNITY_Community 81|Community 81]]
- [[_COMMUNITY_Community 82|Community 82]]
- [[_COMMUNITY_Community 83|Community 83]]
- [[_COMMUNITY_Community 84|Community 84]]
- [[_COMMUNITY_Community 85|Community 85]]
- [[_COMMUNITY_Community 86|Community 86]]
- [[_COMMUNITY_Community 87|Community 87]]
- [[_COMMUNITY_Community 88|Community 88]]
- [[_COMMUNITY_Community 89|Community 89]]
- [[_COMMUNITY_Community 90|Community 90]]
- [[_COMMUNITY_Community 91|Community 91]]

## God Nodes (most connected - your core abstractions)
1. `PowerLadderTests` - 26 edges
2. `MarriageMatchmakerTests` - 24 edges
3. `AudioManager` - 22 edges
4. `ClanDomainUI` - 22 edges
5. `WorldMapUI` - 22 edges
6. `KinshipRulesTests` - 18 edges
7. `Test` - 18 edges
8. `PowerLadder` - 17 edges
9. `RankCatalogTests` - 17 edges
10. `SpiritualOrificeRulesTests` - 17 edges

## Surprising Connections (you probably didn't know these)
- `Unity 2022.3 LTS (README stack, outdated)` --conceptually_related_to--> `Unity 6000.4.2f1 (Unity 6.4)`  [AMBIGUOUS]
  README.md → ProjectSettings/ProjectVersion.txt
- `Unity CI/CD workflow (unity-ci.yml)` --references--> `Unity 6000.4.2f1 (Unity 6.4)`  [EXTRACTED]
  .github/workflows/unity-ci.yml → ProjectSettings/ProjectVersion.txt
- `CI test job (EditMode + PlayMode tests)` --shares_data_with--> `claude-output/result.json`  [EXTRACTED]
  .github/workflows/unity-ci.yml → BRAIN_CLAUDE/CLI.md
- `CI test job (EditMode + PlayMode tests)` --conceptually_related_to--> `Edit Mode tests (NUnit, pure logic)`  [INFERRED]
  .github/workflows/unity-ci.yml → BRAIN_CLAUDE/TEST.md
- `CI test job (EditMode + PlayMode tests)` --conceptually_related_to--> `Play Mode tests (UnityTest, runtime)`  [INFERRED]
  .github/workflows/unity-ci.yml → BRAIN_CLAUDE/TEST.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Headless Unity test automation (CI, wrapper, bridge, hooks)** — workflows_unity_ci_test_job, brain_claude_cli_unity_claude_commands, scripts_unity_claude, claudebridge_claudetestrunner, brain_claude_test_batchmode_test_commands, brain_claude_test_pre_commit_hook [INFERRED 0.85]
- **Command-pattern combat actions** — brain_claude_memory_command_pattern, combat_icombataction, combat_attackaction, combat_moveaction, combat_defendaction, combat_techniqueaction, combat_itemaction, combat_fleeaction [EXTRACTED 1.00]
- **Annual four-phase game loop** — brain_claude_plan_annual_cycle, assets_project_scripts_core_timemanager_cs_core_timemanager, economy_taskassignmentsystem, events_eventmanager, diplomacy_factionmanager [EXTRACTED 1.00]

## Communities (92 total, 9 thin omitted)

### Community 0 - "Data Models & Balancing"
Cohesion: 0.06
Nodes (31): float, int, int, string, int, List, StoryTriggerType, string (+23 more)

### Community 1 - "Victory, Karma & Ascension"
Cohesion: 0.17
Nodes (4): CharacterData, DeathCause, string, ClanKarmaSystem

### Community 2 - "Combat Actions (Command)"
Cohesion: 0.05
Nodes (26): CombatUnit, GridCell, CombatUnit, GridCell, CombatUnit, GridCell, CombatUnit, GridCell (+18 more)

### Community 3 - "Test Infra & Asmdefs"
Cohesion: 0.16
Nodes (9): CharacterData, Element, CharacterData, Element, Test, GeneticSystem, MirrorChronicles.Clan, GeneticSystemTests (+1 more)

### Community 4 - "Conventions & Event Bus"
Cohesion: 0.09
Nodes (17): CharacterData, CultivationRealm, DeathCause, GamePhase, Golden rules (EN code / FR docs, mandatory patterns), Code conventions (MirrorChronicles.<Module> namespaces, XML docs, OnEnable/OnDisable subscriptions), Command pattern (ICombatAction), Observer pattern (C# events bus) (+9 more)

### Community 5 - "Story Events"
Cohesion: 0.06
Nodes (19): CharacterData, CultivationRealm, DeathCause, GamePhase, HashSet, List, StoryEventData, StoryTriggerType (+11 more)

### Community 6 - "Headless Unity CLI"
Cohesion: 0.15
Nodes (9): 📋 Commandes disponibles, 📋 Configuration, Développement quotidien, ❌ JAMAIS, 🛡️ Règles de sécurité impératives, ✅ TOUJOURS, 🎮 Unity 6 + Claude Code (Ubuntu/Linux), 🔄 Workflow recommandé (+1 more)

### Community 7 - "World Map & Diplomacy"
Cohesion: 0.11
Nodes (10): CharacterData, CharacterData, GridCell, Divine Interventions (Qi Pulse, Ancestral Shield, Mirror Judgment), Wound lethality model (vitality thresholds, Dao Wound), WL-007: Wounds not persisted on CharacterData, MirrorChronicles.Characters, WoundSystem (+2 more)

### Community 8 - "Annual Cycle & Random Events"
Cohesion: 0.16
Nodes (6): CharacterData, CultivationRealm, Test, TestCase, MirrorChronicles.Tests.EditMode, SpiritualOrificeRulesTests

### Community 9 - "BRAIN_CLAUDE Workflow"
Cohesion: 0.50
Nodes (3): End-of-session doc update order, Session start reading order, Input System (com.unity.inputsystem)

### Community 10 - "Mirror Powers & Combat Units"
Cohesion: 0.19
Nodes (5): CharacterData, CombatUnit, CultivationRealm, int, MirrorSystem

### Community 11 - "Tasks, Births & Espionage"
Cohesion: 0.14
Nodes (11): CharacterData, GamePhase, List, TaskType, Child birth via ClanManager.GenerateChild (#14), Missing task types: Study, Teaching, Diplomacy, Espionage (#15), Phase 1 placeholder UI + TaskAssignment UI (#12-13), UGUI + TextMeshPro (com.unity.ugui) (+3 more)

### Community 12 - "PlayMode Tests & Worked Lessons"
Cohesion: 0.14
Nodes (9): CharacterData, Dictionary, double, Random, SetUp, Test, FixedRandom, MarriageMatchmakerTests (+1 more)

### Community 13 - "Wounds & Techniques"
Cohesion: 0.06
Nodes (25): CombatUnit, GridCell, TechniqueData, int, GamePhase, int, List, string (+17 more)

### Community 14 - "Save System (Newtonsoft)"
Cohesion: 0.07
Nodes (28): 5.1 Respiration Embryonnaire (6 chakras), 5.2 Culture du Qi (9 niveaux célestes), 5.3.1 Percée, 5.3.2 Progression, 5.3.3 Partenaires Dao, 5.3.4 Blocage au Manoir Pourpre, 5.3.5 Phénomènes, 5.3 Établissement des Fondations (4 stades) (+20 more)

### Community 15 - "Xianxia Rules & Aging"
Cohesion: 0.11
Nodes (7): CultivationRealm, QiPhase, Test, TestCase, TrialKind, MirrorChronicles.Tests.EditMode, PowerLadderTests

### Community 16 - "Combat AI Strategies"
Cohesion: 0.17
Nodes (12): CombatUnit, Dictionary, GridCell, List, AggressiveStrategy, BerserkerStrategy, CautiousStrategy, CombatAI (+4 more)

### Community 17 - "Audio Manager"
Cohesion: 0.12
Nodes (12): bool, CharacterData, CultivationRealm, float, GamePhase, IEnumerator, int, List (+4 more)

### Community 18 - "Clan Domain UI"
Cohesion: 0.12
Nodes (10): Button, CharacterData, DeathCause, GameObject, GamePhase, List, TMP_Text, Transform (+2 more)

### Community 19 - "Claude Test Runner"
Cohesion: 0.11
Nodes (15): int, MenuItem, string, ClaudeBridge, ClaudeTestRunner, TestListener, DateTime, ICallbacks (+7 more)

### Community 20 - "VFX, Pooling & Camera Shake"
Cohesion: 0.07
Nodes (23): bool, Color, Element, float, GameObject, Vector3, bool, IEnumerator (+15 more)

### Community 21 - "World Map UI"
Cohesion: 0.07
Nodes (18): Color, MenuItem, string, Transform, Vector2, Button, Color, FactionData (+10 more)

### Community 22 - "VFX Manager"
Cohesion: 0.20
Nodes (11): CharacterData, float, Func, int, IReadOnlyList, List, Random, MarriageMatchmaker (+3 more)

### Community 23 - "Claude UI Builder"
Cohesion: 0.06
Nodes (21): CharacterData, GameObject, List, CultivationRealm, int, RandomEventType, string, CultivationRealm (+13 more)

### Community 24 - "Clan Manager"
Cohesion: 0.16
Nodes (5): CharacterData, DeathCause, GamePhase, ClanManager, MirrorChronicles.Clan

### Community 25 - "Building System"
Cohesion: 0.18
Nodes (4): BuildingType, BuildingData, BuildingSystem, MirrorChronicles.Economy

### Community 26 - "Breakthrough Tests"
Cohesion: 0.18
Nodes (7): BreakthroughOutcome, CharacterData, CultivationRealm, Test, TestCase, BreakthroughRulesTests, MirrorChronicles.Tests.EditMode

### Community 27 - "Deduction Engine"
Cohesion: 0.25
Nodes (7): Dictionary, Element, List, TechniqueData, FragmentData, DeductionEngine, TechniqueType

### Community 28 - "Mental Stability"
Cohesion: 0.10
Nodes (12): CharacterData, CultivationRealm, DeathCause, GameObject, SetUp, Test, MentalStabilitySystem, MirrorChronicles.Characters (+4 more)

### Community 30 - "Turn Manager"
Cohesion: 0.26
Nodes (5): CombatUnit, List, MirrorChronicles.Combat, TurnManager, Queue

### Community 31 - "Blood Registry"
Cohesion: 0.22
Nodes (4): CharacterData, List, BloodRegistry, MirrorChronicles.Clan

### Community 32 - "Breakthrough System"
Cohesion: 0.17
Nodes (8): CharacterData, CultivationRealm, Test, TestCase, CultivationPath, CultivationSubPath, MirrorChronicles.Tests.EditMode, RankCatalogTests

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
Cohesion: 0.21
Nodes (7): MonoBehaviour, MirrorChronicles.UI, UIThemeManager, UIThemeTag, UIThemeData, UIThemeRole, UIThemeTag

### Community 38 - "UI Theme Tags"
Cohesion: 0.20
Nodes (6): MenuItem, string, Transform, Vector2, ClaudeUIBuilder, TextAlignmentOptions

### Community 40 - "Community 40"
Cohesion: 0.20
Nodes (4): FactionData, GamePhase, List, FactionManager

### Community 41 - "Community 41"
Cohesion: 0.07
Nodes (28): 1. Core — Boucle principale, 2. Mirror — Interface joueur, 3. Clan — Lignée, 4. Characters — Individus, 5. Combat — Tactique tour par tour, 6. Diplomacy — Monde extérieur, 7. Economy — Ressources & tâches, 8. Events — Aléatoire + scénarisé (+20 more)

### Community 42 - "Community 42"
Cohesion: 0.23
Nodes (6): CharacterData, Dictionary, SetUp, Test, KinshipRulesTests, MirrorChronicles.Tests.EditMode

### Community 43 - "Community 43"
Cohesion: 0.09
Nodes (23): 1. Ai-je modifié une static class / pure function ?, 1. Edit Mode Tests (NUnit pur, rapides), 2. Ai-je modifié un MonoBehaviour ?, 2. Play Mode Tests (UnityTest, plus lents), 3. Ai-je modifié un event `GameEvents` ?, 4. Ai-je modifié un POCO (`CharacterData`, `GameData`, etc.) ?, 5. Ai-je modifié un `Singleton` Manager ?, 6. Ai-je modifié un ScriptableObject ? (+15 more)

### Community 44 - "Community 44"
Cohesion: 0.11
Nodes (19): Bootstrap du projet Unity, Build en ligne de commande (Linux/Mac/Windows), Build Settings — Scenes in Build, Changelog des packages, Commandes de référence, Configuration manuelle, Installation, 📦 PACKAGE.md — Registre des packages Unity (+11 more)

### Community 45 - "Community 45"
Cohesion: 0.10
Nodes (21): 🐛 Bugs identifiés dans le code existant (audit 2026-04-14), 📖 Comment utiliser ce fichier, 📐 Leçons d'architecture & design, 📝 Template pour nouvelles entrées, WL-001 — `deceased.RootElement` n'existe pas, WL-002 — `MirrorSystem.ConsumeMirrorPower` n'existe pas, WL-003 — `Element.Ice` absent de l'enum `Element`, WL-004 — `EventManager.GenerateYearlyEvent` n'existe pas + `TriggerYearlyEvent` est privée (+13 more)

### Community 46 - "Community 46"
Cohesion: 0.13
Nodes (9): AdvancementStep, CharacterData, CultivationRealm, double, int, QiPhase, TrialKind, MirrorChronicles.Characters (+1 more)

### Community 47 - "Community 47"
Cohesion: 0.33
Nodes (5): IEnumerator, SimulationPlayModeTest, UnitySetUp, UnityTearDown, UnityTest

### Community 48 - "Community 48"
Cohesion: 0.18
Nodes (6): CharacterData, double, int, IReadOnlyList, MirrorChronicles.Characters, SpiritualOrificeRules

### Community 49 - "Community 49"
Cohesion: 0.15
Nodes (13): 🏗️ Architecture & Structure, 🧬 Characters — Individus, 👥 Clan — Lignée, ⚔️ Combat — Tactique tour par tour, 🎯 Core — Boucle principale, 📦 Data — POCOs, 🌍 Diplomacy — Monde extérieur, ✅ DONE.md — Ce qui a été fait (+5 more)

### Community 50 - "Community 50"
Cohesion: 0.22
Nodes (6): CharacterData, CultivationRealm, QiPhase, string, MirrorChronicles.Characters, RankCatalog

### Community 51 - "Community 51"
Cohesion: 0.17
Nodes (12): 📌 Contexte projet, 📝 Conventions de code (rappel), 🏗️ Design patterns utilisés, 🎨 Direction artistique (rappel), 📋 Fichiers de suivi, 📁 Layout du projet (cible), 🧠 MEMORY.md — Mémoire projet Reflets de Lignée, 🎮 Pitch du jeu (résumé) (+4 more)

### Community 52 - "Community 52"
Cohesion: 0.31
Nodes (7): CharacterData, Func, HashSet, int, List, KinshipRules, MirrorChronicles.Clan

### Community 53 - "Community 53"
Cohesion: 0.17
Nodes (11): 0. Décisions et conventions, 10. Ordres et lieux sacrés (renommés), 12.1 Compléments historiques (📚), 12. Chronologie (renommée), 1. Vue d'ensemble — comment les pièces s'emboîtent, 4. Orifice spirituel, mortels et Graines de Sceau, 8. Familles nommées (renommées), 9. Personnages historiques et légendaires (renommés) (+3 more)

### Community 54 - "Community 54"
Cohesion: 0.22
Nodes (7): BreakthroughOutcome, CharacterData, float, int, TrialKind, BreakthroughRules, MirrorChronicles.Characters

### Community 55 - "Community 55"
Cohesion: 0.20
Nodes (10): 🔴 BLOQUANT — Infra Unity & Compilation, 📐 Design patterns non encore implémentés, 📝 Liste de vérification pour passer à Phase 2, 🧹 Nettoyage, 🚧 NOT_DONE.md — Ce qui reste à faire, 🟢 Priorité BASSE — Phase 4 (Polish & Art), 🟠 Priorité HAUTE — Finir Phase 1, 🟡 Priorité MOYENNE — Finir Phase 2 (Combat & Events) (+2 more)

### Community 56 - "Community 56"
Cohesion: 0.50
Nodes (3): IReadOnlyList, CharacterNames, MirrorChronicles.Clan

### Community 57 - "Community 57"
Cohesion: 0.25
Nodes (8): ⚠️ 5 priorités absolues (Phase 2), 🧠 BRAINSTORMING.md — Point d'entrée central, 📝 En fin de session — Mettre à jour, En résumé :, 📖 Ordre de lecture optimal (début de session), 📖 Ordre de lecture optionnel (selon le besoin), 🔑 Règles d'or (rappel), 🎯 État ultra-court du projet

### Community 58 - "Community 58"
Cohesion: 0.22
Nodes (6): CharacterData, FactionData, Phase 3 - World & Diplomacy, EspionageSystem, MirrorChronicles.Diplomacy, EspionageResult

### Community 59 - "Community 59"
Cohesion: 0.10
Nodes (12): CharacterData, DeathCause, CharacterData, Func, GamePhase, Random, Key Xianxia business rules, Six cultivation realms (Embryonic -> DaoEmbryo) (+4 more)

### Community 60 - "Community 60"
Cohesion: 0.24
Nodes (6): CharacterData, Dictionary, SetUp, Test, MarriageSystemTests, MirrorChronicles.Tests.EditMode

### Community 61 - "Community 61"
Cohesion: 0.18
Nodes (11): 6.1 Principe, 6.2 Origine : trois générations de Fruitions, 6.3 Les Cinq Manifestations (Cinq Vertus), 6.4 Groupes de Fruitions (vue complète, complétée par le wiki), 6.5 Fondations immortelles isolées, 6.6 Deux Dualités — détail, 6.7 Cinq Vertus, Douze Qi, Tonnerres, Fusion Ancienne, Chamans — détail, 6.8 Statuts de Fruition (état du monde au début du jeu) (+3 more)

### Community 62 - "Community 62"
Cohesion: 0.20
Nodes (10): 11.1 Combinatoire d'un seul cultivateur, 11.2 Raccourcis et leur prix (tous tirés du lore), 11.3 Les dilemmes du clan (reviennent à chaque génération), 11.4 Un monde vivant, 11.5 Le miroir — le rôle du joueur, 11.6 Générations et héritage, 11.7 Rejouabilité, 11.8 Ce que l'on s'interdit (+2 more)

### Community 63 - "Community 63"
Cohesion: 0.22
Nodes (9): 13.1 Clan du joueur et personnages du départ, 13.2 États et régions, 13.3 Sectes, portes, ordres, 13.4 Familles, 13.5 Personnages, 13.6 Chakras, techniques, objets, termes propres au roman, 13.7 Ajouts du 2026-09-24 tirés du wiki (✅ validé), 13.8 Ajouts de la passe complète du wiki (✅ validé) (+1 more)

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
Cohesion: 0.25
Nodes (6): Dictionary, GridCell, List, GridSystem, MirrorChronicles.Combat, TerrainType

### Community 69 - "Community 69"
Cohesion: 0.15
Nodes (15): claude-output/result.json, Headless Unity safety rules, unity-claude.sh command set (Compile, Build*, Run*Tests, FindAssets, ClearCache, ScanLegacyPackages, AutoMigrate), Assembly Definitions, Unity Test Framework (com.unity.test-framework), Unity -batchmode -runTests CLI commands, Edit Mode tests (NUnit, pure logic), Play Mode tests (UnityTest, runtime) (+7 more)

### Community 70 - "Community 70"
Cohesion: 0.33
Nodes (4): MenuItem, string, ClaudeBridge, ClaudeCodeBridge

### Community 71 - "Community 71"
Cohesion: 0.24
Nodes (6): Dictionary, MenuItem, ClaudeBridge, UpmMigrator, detected, locations

### Community 72 - "Community 72"
Cohesion: 0.25
Nodes (8): 3.1 Dao Immortel — quête orthodoxe de l'immortalité par l'harmonie du cœur, de l'essence et la recherche de la vérité, 3.2 Dao du Diable — survie, pillage, usage des forces maléfiques, 3.3 Dao Bouddhiste — soulager la souffrance, transcender la réincarnation, maîtriser destin et karma, 3.4 Dao Démoniaque — voies des êtres démoniaques (bêtes, plantes), lignée et transformation naturelle, 3.5 Dao du Sceau Chamanique — invoquer des puissances supérieures par rituels et talismans, 3.6 Dao Divin — le sixième Dao (📚, absent de `Miror.txt`), 3.7 Rangs des autres Dao (📚) — des équivalences asymétriques, 3. Les cinq Voies de cultivation

### Community 73 - "Community 73"
Cohesion: 0.21
Nodes (5): CharacterData, DeathCause, GamePhase, int, BreakthroughSystem

### Community 76 - "Community 76"
Cohesion: 0.33
Nodes (6): 2.1 Principe, 2.2 Tableau des grades, 2.3 Les trois catégories de méthodes de Qi, 2.4 Méthodes de cultivation du Qi connues (renommées), 2.5 Qi spirituel, collecte et substitution (📚), 2. Techniques et grades

### Community 77 - "Community 77"
Cohesion: 0.40
Nodes (5): 7.1 États et grandes régions, 7.2 Les trois sectes et les portes, 7.3 Autour du clan, 7.4 Autres puissances et lieux (📚 carte traduite du wiki), 7. Carte du monde (renommée)

### Community 78 - "Community 78"
Cohesion: 0.17
Nodes (6): CharacterData, DeathCause, Annual 4-phase cycle (Management -> Events -> Breakthrough -> Inheritance), MirrorChronicles.Characters, LegacySystem, MirrorChronicles.Clan

### Community 81 - "Community 81"
Cohesion: 0.23
Nodes (9): WL-001: deceased.RootElement does not exist (use Affinity), WL-002: MirrorSystem.ConsumeMirrorPower does not exist (ConsumePower), WL-003: Element.Ice missing from Element enum, WL-004: EventManager.GenerateYearlyEvent missing / TriggerYearlyEvent private, WL-005: private FactionManager.ProcessYearlyFactionAI called from test, WL-100: Unity -> Web -> Unity pivot debt, MirrorChronicles.Data, MirrorChronicles.Mirror (+1 more)

### Community 82 - "Community 82"
Cohesion: 0.31
Nodes (4): AdvancementStep, CharacterData, CultivationSystem, MirrorChronicles.Characters

### Community 83 - "Community 83"
Cohesion: 0.31
Nodes (3): CharacterData, CultivationRealm, AscensionSystem

### Community 84 - "Community 84"
Cohesion: 0.25
Nodes (6): CLAUDE.md / master prompt (source of truth), WL-101: Factions hardcoded instead of ScriptableObjects, FactionData, MirrorChronicles.Data, MirrorChronicles.Data, MirrorChronicles.Diplomacy

### Community 85 - "Community 85"
Cohesion: 0.39
Nodes (5): CharacterData, IReadOnlyList, TaskType, MirrorChronicles.Characters, TaskRules

### Community 86 - "Community 86"
Cohesion: 0.25
Nodes (3): CharacterData, DeathCause, VictoryConditionSystem

### Community 87 - "Community 87"
Cohesion: 0.36
Nodes (3): Test, MirrorChronicles.Tests.EditMode, TaskRulesTests

### Community 88 - "Community 88"
Cohesion: 0.25
Nodes (6): Player as consciousness trapped in ancestral bronze mirror, Ironman save (yearly auto-save JSON), MirrorChronicles.Characters, MirrorChronicles.Mirror, Reflets de Lignee: Les Chroniques du Miroir (Mirror Chronicles), The Mirror Legacy (Xuanjian Xianzu) - inspiration

### Community 89 - "Community 89"
Cohesion: 0.40
Nodes (4): float, IEnumerator, int, GameSimulationTest

### Community 90 - "Community 90"
Cohesion: 0.33
Nodes (4): Clan Karma (per-generation passive bonus), Victory: 10 generations + Dao Embryo ascension, MirrorChronicles.Clan, MirrorChronicles.Core

## Ambiguous Edges - Review These
- `MarriageSystem.cs` → `Key Xianxia business rules`  [AMBIGUOUS]
  BRAIN_CLAUDE/MEMORY.md · relation: implements
- `Unity 6000.4.2f1 (Unity 6.4)` → `Unity 2022.3 LTS (README stack, outdated)`  [AMBIGUOUS]
  README.md · relation: conceptually_related_to

## Knowledge Gaps
- **493 isolated node(s):** `ClaudeBridge`, `ClaudeBridge`, `string`, `ClaudeBridge`, `string` (+488 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **9 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `MarriageSystem.cs` and `Key Xianxia business rules`?**
  _Edge tagged AMBIGUOUS (relation: implements) - confidence is low._
- **What is the exact relationship between `Unity 6000.4.2f1 (Unity 6.4)` and `Unity 2022.3 LTS (README stack, outdated)`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `Edit Mode tests (NUnit, pure logic)` connect `Community 69` to `Combat Actions (Command)`, `Test Infra & Asmdefs`, `Community 78`, `Community 81`, `Community 59`, `Mental Stability`?**
  _High betweenness centrality (0.086) - this node is a cross-community bridge._
- **Why does `unity-claude.sh command set (Compile, Build*, Run*Tests, FindAssets, ClearCache, ScanLegacyPackages, AutoMigrate)` connect `Community 69` to `Claude Test Runner`, `Headless Unity CLI`, `Community 70`, `Community 71`?**
  _High betweenness centrality (0.041) - this node is a cross-community bridge._
- **Why does `WorldMapUI` connect `World Map UI` to `UI Theme Data`?**
  _High betweenness centrality (0.038) - this node is a cross-community bridge._
- **What connects `ClaudeBridge`, `ClaudeBridge`, `string` to the rest of the system?**
  _499 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Data Models & Balancing` be split into smaller, more focused modules?**
  _Cohesion score 0.06006006006006006 - nodes in this community are weakly interconnected._