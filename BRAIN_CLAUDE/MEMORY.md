# 🧠 MEMORY.md — Mémoire projet Reflets de Lignée

> **Dernière mise à jour :** 2026-09-25 — migration Unity → **Godot 4.7.2 .NET** (phase G). Toute la simulation tourne sans moteur ; l'arbre Unity disparaît en G5.
> **À chaque reprise :** lire `NOT_DONE.md`, `WORKED_LESSON.md` et, pour le monde, `LORE.md` (source de vérité).

---

## 📌 Contexte projet

- **Nom** : Reflets de Lignée : Les Chroniques du Miroir (*Mirror Chronicles*)
- **Type** : gestion de clan xianxia sur des générations, combat tactique au tour par tour
- **Le joueur** : une conscience piégée dans un miroir de bronze ancestral ; il guide un clan (le **clan Mo**) sans incarner personne
- **Moteur** : **Godot 4.7.2 .NET** (C#, .NET 8), choisi le 2026-09-25 à la place d'Unity (jamais installé ni compilé)
- **Plateforme visée** : PC (Steam) ; aucune console prévue
- **Langues** : code et commentaires en anglais ; docs, logs de pilotage et **textes du joueur en français**
- **Sauvegarde** : Ironman, `user://ironman_save.json`, à chaque début d'année
- **Propriété intellectuelle** : aucun nom propre du roman source dans le jeu ; lexique de renommage en `LORE.md` §13

---

## 📁 Arborescence

```
Mirror-Legacy/
├── src/Core/              # MirrorChronicles.Core — toute la simulation, sans moteur (net8.0)
│   ├── Session/           # GameSession (racine de composition), GameContext, GameClock, sauvegardes
│   ├── Clan/              # ClanManager (seule porte de la mort), registre, génétique, parenté, mariages, karma, héritage
│   ├── Characters/        # Échelle de puissance, rangs, percées, orifice, tâches, cultivation, vieillissement, blessures
│   ├── Economy/           # Ressources, tâches annuelles, bâtiments
│   ├── Diplomacy/         # Factions, alliances, espionnage, mariages
│   ├── Events/            # Bus d'événements par session, événements aléatoires et d'histoire
│   ├── Mirror/            # Le miroir (interventions, Graines de Sceau, Pulsation de Qi) et la déduction
│   ├── Combat/            # Grille, unités, actions, initiative, IA, batailles
│   ├── Presentation/      # Modèles d'écran testables (domaine du clan, chronique)
│   └── Data/              # Données sérialisées et contenu (GameContent, chargeur JSON)
├── tests/Core.Tests/      # NUnit 3 (dotnet test) — mêmes domaines que src/Core
├── game/                  # Projet Godot : project.godot, scenes/*.tscn (texte), scripts/*.cs, data/*.json
├── Scripts/dev.sh         # build | test | smoke | screenshot | all
├── BRAIN_CLAUDE/          # Pilotage (ce dossier)
└── graphify-out/          # Graphe de connaissances (graphify)
```

---

## 🏗️ Architecture

| Élément | Rôle |
|---|---|
| **`GameSession`** | Construit tous les systèmes dans un ordre fixe et joue le tour : Événements (tâches, IA des factions, événement aléatoire, mariages) → Percées → Héritage (naissances, examens d'orifice) → nouvelle année (vieillissement, bâtiments, `OnYearStarted`). |
| **`GameContext`** | Ce que partagent les systèmes d'une partie : bus, journal, hasard à graine, horloge, contenu (lecture seule). |
| **`GameEventBus`** | Un bus par partie ; les réactions s'exécutent dans l'ordre de construction (déterministe). |
| **`ClanManager.Kill`** | Seule porte de la mort : la liste des vivants et la succession sont à jour avant `OnCharacterDied`. |
| **Contenu** | `game/data/*.json` (clan, noms, équilibrage, factions, événements, histoire, techniques, Qi), chargé et validé par `GameContentLoader` ; jamais modifié par une partie. |
| **Savoir** | `World/KnowledgeBase` : faits typés (`FactKind`) révélés par une source, implications branchables (`WorldKnowledge`) ; les règles demandent `Knows(...)`, les nouvelles sources appellent `Reveal(...)`. |
| **Trous du lore** | Chaque donnée porte `provenance` et `interpretedFields` ; aucune valeur d'interprétation dans le code (tout dans `balance.json`) ; `./Scripts/dev.sh gaps` liste ce qui reste à confirmer. |
| **Monde** | `World/FruitionRegistry` : l'état des 63 lignées dans une partie (statuts du lore, les autres tirés d'une source de hasard propre au monde, `WorldRandom(seed)`). |
| **Techniques** | `TechniqueLibrary` : ce que le clan sait (catalogue appris + déductions du miroir, sauvegardées entières) et la méthode de chaque membre ; `TechniqueRules` pour les règles pures du grade. |
| **Sauvegarde** | `GameData` 2.3 (état complet : techniques, Qi, lignées, savoir), `SaveSerializer` (Newtonsoft, énumérations par nom) ; `ToSaveData` est un instantané détaché ; les sauvegardes v1 à 2.2 se chargent et sont mises à niveau. |
| **Couche Godot** | `GameRoot` (autoload : contenu, partie, sauvegarde), `ClanDomain` (écran principal) ; les scripts ne font que lier les modèles de `Presentation`. |

---

## 📝 Conventions de code

- **Namespaces** : `MirrorChronicles.<Domaine>` pour Core ; `MirrorChronicles.Game` pour la couche Godot.
- **Pas de singleton ni de moteur dans Core** : dépendances par constructeur, `GameContext` partagé.
- **Hasard** : toujours `ctx.Rng` (ou `field.Rng` en combat), identifiants via `Rng.NextId()` — une graine rejoue la même partie.
- **Règles** : constantes nommées dans des classes pures (`PowerLadder`, `BreakthroughRules`, `SpiritualOrificeRules`, `TaskRules`, `TechniqueRules`, `FoundationRules`, `PurpleMansionRules`…) ; les valeurs réglables vont dans `balance.json`.
- **Noms affichés** : dans les données ou en français dans `Presentation`/`RankCatalog` ; jamais de nom du roman.
- **Journal** : préfixe `[Système]` via `IGameLog` (développement) ; la `Chronicle` raconte au joueur.
- **Sauvegarde** : ne jamais renommer un champ ou une valeur d'énumération sauvegardés.
- **C#** : `ImplicitUsings` et `Nullable` désactivés (code porté non annoté) ; commentaires XML sur les API publiques.
- **Tests** : TDD (commit RED puis GREEN), un seul assert, noms `Méthode_Résultat_QuandCondition` (voir `TEST.md`).

---

## 🎨 Direction artistique (rappel)

| Élément | Valeur |
|---|---|
| **Esthétique** | SHUIMO (peinture à l'encre chinoise) avec contours nets |
| **Fond** | Parchemin beige/crème `#F5E6C8` |
| **Encre principale** | Noir profond `#1A1A2E` |
| **Accent chaud** | Or/Bronze `#C9A959` |
| **Accent froid** | Bleu jade `#3D8B8B` |
| **Danger/Sang** | Rouge cinabre `#C62828` |
| **Qi/Spirituel** | Violet impérial `#6A0DAD` |
| **❌ NE PAS faire** | Flat design, Material Design, menus modernes |

L'écran actuel utilise le thème Godot par défaut (placeholder).

---

## 📐 Règles métier clés (état du code)

| Règle | Valeur |
|---|---|
| **Royaumes** | 7 (Respiration Embryonnaire → Immortel Doré) avec sous-niveaux (`LORE.md` §5) |
| **Orifice spirituel** | héréditaire (D3) : 0,3 % / 35 % / 50 % selon les parents (`balance.json`) |
| **Mortels** | 60-80 ans ; ni cultivation ni percée ; tâches limitées |
| **Techniques** | grade 1-7+ : vitesse ×0,6 à ×2,2 (`balance.json`) et plafond de royaume (`LORE.md` §2.2) ; entrer en Culture du Qi absorbe une portion du Qi de la méthode ; récolte du Qi dès l'Œil du Sommet |
| **Fondation** | formée depuis le Qi de sa méthode, une portion absorbée ; Partenaires Dao = autres fondations de la lignée ; en consommer un scelle la progression |
| **Manoir Pourpre** | 4 épreuves (Montée, Manifestation 6 ans, Grand Vide, Illusions) ; 5 capacités divines, la 4e au Seuil d'Immortalité ; stade = nombre de capacités |
| **Cœur Dao** | tempérament héréditaire ; aligné ×1,2, sinon ×0,9 à partir de la Fondation |
| **Âges** | aucune tâche avant 6 ans ; un enfant mortel ne fait que se reposer jusqu'à 16 ans |
| **Clan de départ** | 5 membres (`clan.json`) |
| **Mariage** | 18-40 ans, parenté interdite sur 3 générations, 30 %/an |
| **Naissances** | mère de 16 à 45 ans, 25 %/an |
| **Phases** | Gestion → Événements → Percées → Héritage |
| **Combat** | grille 10×10, initiative, 6 actions, 5 IA ; Qi régénéré seulement sur Qi concentré |
| **Relations** | -100 à +100 |

---

## 🧭 Où en est le projet

Voir `NOT_DONE.md`. En bref (2026-09-25) : L0-L4 faits (L4 : lignées, fondations, Manoir Pourpre) ; migration Godot faite, écrans G6 à venir ; équilibrage B1-B4 ouvert ; L4d (serments) et L4b (routes du Noyau d'Or : forge, positions, Main Gauche) faits le 2026-09-26 ; suites en L4e. L5 (carte `regions.json`, factions du lore, écran « Carte du monde ») fait le 2026-09-26 ; suites en L5b.

---

## 📋 Fichiers de suivi

| Fichier | Usage |
|---|---|
| `BRAINSTORMING.md` | Point d'entrée — ordre de lecture |
| `LORE.md` | Monde, royaumes, voies, Fruitions, carte, chronologie, lexique — **source de vérité** |
| `PLAN.md` | Conception du jeu et changelog |
| `DONE.md` / `NOT_DONE.md` | Avancement — **mis à jour à chaque étape** |
| `WORKED_LESSON.md` | Bugs et leçons — **mis à jour à chaque session** |
| `TEST.md` | Règles de tests |
| `PACKAGE.md` | Dépendances (Godot, .NET, NuGet) |
| `CLI.md` | Commandes de développement |
