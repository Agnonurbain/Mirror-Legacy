# Reflets de Lignée — Les Chroniques du Miroir

RPG 2D de gestion de clan multigénérationnel inspiré de *The Mirror Legacy (Xuanjian Xianzu)*, avec cultivation Xianxia, combat tactique tour par tour, diplomatie entre factions et transmission sur 10+ générations.

## Stack

- **Godot 4.7.2 .NET** (moteur depuis le 2026-09-25, en remplacement d'Unity)
- **C# / .NET 8** — toute la simulation, sans dépendance au moteur
- **Newtonsoft.Json** — sauvegardes (JSON, énumérations écrites par nom)
- **NUnit 3** — tests via `dotnet test`

## Architecture

```
src/Core/            # Simulation sans moteur (MirrorChronicles.Core)
├── Session/         # GameSession (racine de composition, phases), GameContext, sauvegardes
├── Clan/            # ClanManager, BloodRegistry, génétique, parenté, mariages, karma, héritage
├── Characters/      # Échelle de puissance, rangs, cultivation, percées, orifice, vieillissement, blessures
├── Economy/         # Ressources, tâches, bâtiments
├── Diplomacy/       # Factions, alliances, espionnage, mariages
├── Events/          # Bus d'événements par session, événements aléatoires et d'histoire
├── Mirror/          # Le miroir (le joueur) et la déduction de techniques
└── Data/            # Données sérialisées : personnages, sauvegarde, factions…
tests/Core.Tests/    # Tests NUnit, dont des parties de 100 ans sans moteur
game/                # Projet Godot : scènes .tscn (texte), scripts C# minces, data/*.json
```

## Commandes

```bash
./Scripts/dev.sh test    # tests de la simulation (seul le SDK .NET 8 est requis)
./Scripts/dev.sh build   # compile Core, les tests et l'assemblage Godot
./Scripts/dev.sh smoke   # lance le jeu sans affichage et échoue à la moindre erreur
./Scripts/dev.sh all     # les trois
```

Godot 4.7.2 .NET est attendu dans `~/Godot/` (sinon, définir `GODOT_BIN`).

## Documentation interne

Le dossier [BRAIN_CLAUDE/](BRAIN_CLAUDE/) contient toute la documentation de pilotage du projet :

- [BRAINSTORMING.md](BRAIN_CLAUDE/BRAINSTORMING.md) — point d'entrée et priorités
- [PLAN.md](BRAIN_CLAUDE/PLAN.md) — architecture complète
- [DONE.md](BRAIN_CLAUDE/DONE.md) / [NOT_DONE.md](BRAIN_CLAUDE/NOT_DONE.md) — avancement
- [WORKED_LESSON.md](BRAIN_CLAUDE/WORKED_LESSON.md) — bugs résolus
- [PACKAGE.md](BRAIN_CLAUDE/PACKAGE.md) — dépendances Unity
- [TEST.md](BRAIN_CLAUDE/TEST.md) — règles de tests

## Statut

**Migration vers Godot terminée** (2026-09-25). Toute la simulation tourne sans moteur et est couverte par plus de 500 tests, dont des parties de 100 ans ; l'écran du domaine du clan se joue sous Godot. Suite : les écrans à venir (miroir, bâtiments, diplomatie, bataille) et les phases du lore — voir [NOT_DONE.md](BRAIN_CLAUDE/NOT_DONE.md).

---

*Ancien portage web React/TS (prototype Google AI Studio) archivé dans [.archive/web-port/](.archive/web-port/).*
