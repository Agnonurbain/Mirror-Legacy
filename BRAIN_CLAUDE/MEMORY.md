# 🧠 MEMORY.md — Mémoire projet Reflets de Lignée

> **Dernière session :** 2026-04-14 — Pivot décidé : retour complet à Unity/C# (abandon du portage web).
> **Prochain rappel :** Lire `NOT_DONE.md` et `WORKED_LESSON.md` avant chaque session.

---

## 📌 Contexte projet

- **Nom** : Reflets de Lignée : Les Chroniques du Miroir (*Mirror Chronicles*)
- **Inspiration** : *The Mirror Legacy (Xuanjian Xianzu)*
- **Auteur** : Aymeric
- **Type** : RPG de Gestion de Clan & Tactique 2D
- **Moteur** : Unity 6.4 LTS (6000.4.x) + C#
- **Plateformes** : PC (Steam) + Mobile (iOS/Android)
- **Perspective** : 2D top-down / isométrique pour le domaine, grille 2D pour le combat
- **Langue code** : Anglais strict (classes, variables, commentaires XML)
- **Langue docs/logs** : Français
- **Univers** : Xianxia (cultivation spirituelle chinoise)
- **Mode** : Ironman par défaut (1 seule sauvegarde, auto à fin d'année)

---

## 📁 Layout du projet (cible)

```
Mirror-Legacy/
├── Assets/
│   └── _Project/
│       ├── Scripts/                # Code C# (actuel — 34 fichiers, 3432 lignes)
│       │   ├── Core/               # GameManager, TimeManager, SaveSystem
│       │   ├── Mirror/             # MirrorSystem, DeductionEngine
│       │   ├── Clan/               # ClanManager, BloodRegistry, GeneticSystem, LegacySystem
│       │   ├── Characters/         # Cultivation, Breakthrough, Aging, MentalStability, Wound, Ascension
│       │   ├── Combat/             # TacticalCombatManager, Grid, Turn, AI, Actions
│       │   ├── Diplomacy/          # FactionManager, MarriageSystem, AllianceSystem
│       │   ├── Economy/            # ResourceManager, TaskAssignmentSystem
│       │   ├── Events/             # EventManager, GameEvents (bus global)
│       │   ├── Data/               # POCOs + Enums (CharacterData, GameData, etc.)
│       │   └── Tests/              # GameSimulationTest
│       ├── ScriptableObjects/      # ⚠️ À créer
│       ├── Prefabs/                # ⚠️ À créer
│       ├── Art/                    # ⚠️ À créer
│       ├── Audio/                  # ⚠️ À créer
│       └── Scenes/                 # ⚠️ À créer (MainMenu, ClanDomain, TacticalCombat, WorldMap)
│
├── ProjectSettings/                # ⚠️ MANQUANT — le projet n'est pas un vrai projet Unity
├── Packages/                       # ⚠️ MANQUANT — pas de manifest.json
├── Library/                        # (auto-généré au 1er open Unity)
│
├── BRAIN_CLAUDE/                   # Documentation projet (ce dossier)
│   ├── BRAINSTORMING.md            # Point d'entrée session
│   ├── MEMORY.md                   # Ce fichier
│   ├── PLAN.md                     # Plan complet du jeu
│   ├── DONE.md                     # Tâches complétées
│   ├── NOT_DONE.md                 # Tâches restantes
│   ├── WORKED_LESSON.md            # Leçons apprises
│   ├── PACKAGE.md                  # Registre packages Unity
│   └── TEST.md                     # Règles de tests
│
├── BRAIN_QWEN/                     # Docs d'un autre projet (HOMECI) — à ignorer
│
└── [Legacy web]                    # À supprimer ou archiver après décision finale
    ├── src/                        # Port React (obsolète)
    ├── package.json, vite.config.ts, tsconfig.json
    ├── index.html, metadata.json
    └── .env.example
```

---

## 🎮 Pitch du jeu (résumé)

Le joueur incarne une **conscience piégée dans un miroir de bronze ancestral**. Il ne contrôle aucun personnage — il observe, guide et influence une famille de mortels sur **des dizaines de générations** pour en faire le clan de cultivateurs le plus puissant de **Mount Dali**.

Le jeu mêle :
- **Gestion de clan multigénérationnelle** (naissances, mariages, morts, trahisons)
- **Cultivation Xianxia** (6 royaumes : Embryonnaire → Embryon Dao)
- **Combat tactique tour par tour** sur grille 10x10
- **Diplomatie, espionnage, alliances** entre familles rivales
- **Héritage de savoirs** entre générations (Legacy System)

---

## 🏗️ Design patterns utilisés

| Pattern | Utilisation actuelle |
|---|---|
| **Singleton (MonoBehaviour lazy)** | Tous les Managers (GameManager, TimeManager, ClanManager, etc.) |
| **Observer (C# Events)** | `GameEvents` static class — bus global. Systèmes s'abonnent OnEnable/OnDisable |
| **State Machine** | `GameState` (MainMenu/Loading/Playing/GameOver), `GamePhase` (4 phases), `CombatState` |
| **Strategy** | `IAIStrategy` + `AggressiveStrategy`, `DefensiveStrategy` (autres à implémenter) |
| **Command** | `ICombatAction` + `AttackAction`, `MoveAction`, `DefendAction` |
| **MVC** | ⚠️ En théorie — Data (POCOs `CharacterData`, `GameData`) séparée des Managers. View UI **non implémentée**. |

---

## 📊 État d'avancement global

```
Scripts C# (architecture)  : ███████████████████████████░░░░  ~70%
  - Core (GameManager, TimeManager, SaveSystem)          ✅
  - Clan (Registry, Manager, Genetics, Legacy)           ✅ (Legacy a un bug)
  - Characters (Cultivation, Breakthrough, Aging, etc.)  ✅
  - Combat tactique (Grid, Turn, AI, Actions)            ✅ (partiel — 2 AI strategies sur 5)
  - Mirror (MirrorSystem, DeductionEngine)               ✅ (bugs compile)
  - Diplomacy (Factions, Marriage, Alliance)             ✅ (base)
  - Economy (Resources, Tasks)                           ✅ (base)
  - Events (Manager + Bus)                               ✅ (4 events sur 10+)

Projet Unity (infra)       : ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░   0%
  - ProjectSettings/                                     ❌
  - Packages/manifest.json                               ❌
  - Scènes .unity                                        ❌
  - .asmdef                                              ❌
  - .meta files                                          ❌

ScriptableObjects          : ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░   0%
UI (Shuimo thème)          : ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░   0%
Audio                      : ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░   0%
Art (sprites, VFX)         : ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░   0%
Tests (Edit/Play Mode)     : █░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  ~5% (1 script de simu, pas un test unitaire)
```

**Voir `DONE.md` pour le détail des items complétés.**
**Voir `NOT_DONE.md` pour le détail des items restants.**

---

## 🔴 Problèmes CRITIQUES à résoudre

1. **Le projet n'est pas un projet Unity valide** — aucun `ProjectSettings/`, aucun `Packages/manifest.json`, aucune scène. Unity ne peut pas l'ouvrir.
2. **Erreurs de compilation** (code ne compile pas en l'état) :
   - `LegacySystem.cs:50` → `deceased.RootElement` n'existe pas (doit être `Affinity`)
   - `DeductionEngine.cs:65` → `MirrorSystem.Instance.ConsumeMirrorPower(...)` n'existe pas (doit être `ConsumePower(...)`)
   - `DeductionEngine.cs:162` → `Element.Ice` n'existe pas dans l'enum `Element`
   - `GameSimulationTest.cs:75` → `EventManager.Instance.GenerateYearlyEvent()` n'existe pas et la méthode existante `TriggerYearlyEvent()` est privée
   - `GameSimulationTest.cs:88` → `FactionManager.Instance.ProcessYearlyFactionAI()` est privée
3. **`JsonUtility` ne sérialise pas les auto-properties** — `CharacterData` utilise `public int Age { get; set; }` qui ne sera **pas** sauvegardé. Deux options : (a) remplacer par des champs publics `public int Age;` (b) passer à Newtonsoft.Json (`com.unity.nuget.newtonsoft-json`).
4. **Aucun asmdef** — recompilation très lente dès que le projet grandit.
5. **Aucun ScriptableObject** alors que le master prompt l'exige pour toutes les données statiques (techniques, items, events, factions).

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

---

## 📐 Règles métier clés (rappel)

| Règle | Valeur |
|---|---|
| **Nombre de royaumes** | 6 (Embryonic → DaoEmbryo) |
| **Min Spiritual Root par royaume** | 0, 10, 30, 50, 70, 90 |
| **Taux d'échec percée par royaume** | 5%, 15%, 30%, 50%, 70% |
| **Clan de départ** | 5 membres (Patriarche, Matriarche, Fils, Fille, Oncle) |
| **Mariage âge min** | 18 ans |
| **Mariage inceste** | Interdit ≤ 3 générations |
| **Phases par année** | 4 (Management → Events → Breakthrough → Inheritance) |
| **Qi en combat** | Ne régénère pas (sauf case ConcentratedQi +5%/tour) |
| **Sauvegarde** | Ironman, auto à fin d'année, JSON |
| **Grille combat** | 10x10 (extensible 12x12 batailles majeures) |
| **Relations factions** | -100 (Guerre) à +100 (Alliance Scellée) |

---

## 🚧 Prochaines étapes recommandées (dans l'ordre)

1. **Bootstrapper Unity** — créer `ProjectSettings/` + `Packages/manifest.json` (URP ou Built-in, TextMeshPro, Input System).
2. **Créer `.asmdef`** — `MirrorChronicles.Runtime.asmdef` sous `Assets/_Project/Scripts/`.
3. **Corriger les 5 erreurs de compilation** (voir WL-001 à WL-005 dans `WORKED_LESSON.md`).
4. **Créer scène `ClanDomain.unity`** avec GameObject `[Systems]` portant tous les Singletons.
5. **Passer à Newtonsoft.Json** pour la sérialisation (sinon auto-properties non sauvegardées).
6. **Jouer la simulation 10 ans** (`GameSimulationTest`) et valider les logs.
7. **Créer les premiers ScriptableObjects** : `CultivationTechniqueData`, `FactionData.asset` (remplacer les hardcoded de `FactionManager.InitializeWorldFactions`).
8. **Commencer l'UI placeholder** de la Phase 1 (BloodRegistry basique, TaskAssignment basique).

---

## 📝 Conventions de code (rappel)

- **Namespaces** : `MirrorChronicles.<Module>` (Core, Clan, Characters, Combat, Mirror, Diplomacy, Economy, Events, Data, Tests)
- **Singletons** : `public static <T> Instance { get; private set; }` + Awake standard (destroy duplicate)
- **Events** : S'abonner dans `OnEnable`, se désabonner dans `OnDisable`. **JAMAIS dans Awake/Start.**
- **Commentaires XML** : Sur tous les membres publics + classes
- **Debug.Log** : Préfixer avec `[NomDuSystem]`, utiliser les couleurs rich text pour les événements marquants (`<color=red>[Death]</color>`)
- **Nommage** : PascalCase pour types/méthodes/propriétés, camelCase pour champs privés avec underscore (`_turnQueue`)
- **POCOs** : `[Serializable]` obligatoire, constructeur sans paramètres requis, IDs via `Guid.NewGuid().ToString()`

---

## 📋 Fichiers de suivi

| Fichier | Usage |
|---|---|
| `PLAN.md` | Plan complet du jeu — architecture, systèmes, phases, paliers |
| `DONE.md` | Tâches complétées — **mis à jour à chaque étape** |
| `NOT_DONE.md` | Tâches restantes — **mis à jour à chaque étape** |
| `WORKED_LESSON.md` | Bugs identifiés & leçons — **mis à jour à chaque session** |
| `MEMORY.md` | Ce fichier — contexte et résumé pour reprises de session |
| `PACKAGE.md` | Registre des packages Unity — **mis à jour à chaque ajout/suppression** |
| `TEST.md` | Règles de tests Edit Mode / Play Mode |
| `BRAINSTORMING.md` | Point d'entrée — ordre de lecture optimal |

---

*Ce fichier doit être lu au début de chaque session pour reprendre le contexte rapidement.*
*Toujours mettre à jour `DONE.md`, `NOT_DONE.md`, `WORKED_LESSON.md` après chaque tâche.*
