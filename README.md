# Reflets de Lignée — Les Chroniques du Miroir

RPG 2D de gestion de clan multigénérationnel inspiré de *The Mirror Legacy (Xuanjian Xianzu)*, avec cultivation Xianxia, combat tactique tour par tour, diplomatie entre factions et transmission sur 10+ générations.

## Stack

- **Unity 2022.3 LTS** (2D Core template)
- **C#** — logique métier, systèmes de jeu, IA de combat
- **Newtonsoft.Json** — sérialisation des sauvegardes (Ironman, auto-save annuelle)
- **New Input System** — entrées PC + mobile
- **NUnit** — tests Edit Mode + Play Mode

## Architecture

```
Assets/_Project/
├── Scripts/
│   ├── Core/          # GameManager, TimeManager, SaveSystem
│   ├── Clan/          # ClanManager, BloodRegistry, GeneticSystem, LegacySystem
│   ├── Characters/    # Cultivation, Breakthrough, Aging, MentalStability, Wound, Ascension
│   ├── Combat/        # TacticalCombat, Grid, Actions, AI strategies
│   ├── Mirror/        # MirrorSystem (interface joueur), DeductionEngine
│   ├── Diplomacy/     # FactionManager, MarriageSystem, AllianceSystem
│   ├── Economy/       # ResourceManager, TaskAssignmentSystem
│   ├── Events/        # GameEvents (bus), EventManager (aléatoire)
│   └── Data/          # POCOs : CharacterData, GameData, Enums
├── Tests/             # EditMode + PlayMode NUnit
├── Scenes/            # MainMenu, ClanDomain, TacticalCombat, WorldMap
└── Art/, Audio/, Prefabs/
```

## Documentation interne

Le dossier [BRAIN_CLAUDE/](BRAIN_CLAUDE/) contient toute la documentation de pilotage du projet :

- [BRAINSTORMING.md](BRAIN_CLAUDE/BRAINSTORMING.md) — point d'entrée et priorités
- [PLAN.md](BRAIN_CLAUDE/PLAN.md) — architecture complète
- [DONE.md](BRAIN_CLAUDE/DONE.md) / [NOT_DONE.md](BRAIN_CLAUDE/NOT_DONE.md) — avancement
- [WORKED_LESSON.md](BRAIN_CLAUDE/WORKED_LESSON.md) — bugs résolus
- [PACKAGE.md](BRAIN_CLAUDE/PACKAGE.md) — dépendances Unity
- [TEST.md](BRAIN_CLAUDE/TEST.md) — règles de tests

## Statut

**Phase 1 — Infrastructure** (en cours). 34 fichiers C# implémentés (boucle annuelle, clan, cultivation, combat, percée, système Miroir). Le projet Unity n'est pas encore bootstrappé — voir [NOT_DONE.md](BRAIN_CLAUDE/NOT_DONE.md#-bloquant--infra-unity--compilation).

---

*Ancien portage web React/TS (prototype Google AI Studio) archivé dans [.archive/web-port/](.archive/web-port/).*
