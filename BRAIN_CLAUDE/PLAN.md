# 📋 PLAN — Reflets de Lignée : Les Chroniques du Miroir

> **Vision** : Un RPG de gestion de clan multigénérationnel + combat tactique 2D, dans un univers Xianxia, où le joueur est une conscience dans un miroir de bronze qui guide une famille de mortels sur des siècles pour en faire le clan de cultivateurs le plus puissant de Mount Dali.
> **Dernière mise à jour** : 2026-04-14 — Pivot complet Unity/C# acté.

---

## 🏗️ Architecture Globale

```
┌──────────────────────────────────────────────────────────────┐
│                  UNITY 6.4 LTS (6000.4.x) + C#               │
│                                                              │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │              Scenes (4)                                 │ │
│  │  MainMenu.unity → ClanDomain.unity ↔ TacticalCombat    │ │
│  │                          ↕                              │ │
│  │                    WorldMap.unity                       │ │
│  └─────────────────────────────────────────────────────────┘ │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐    │
│  │  GameObject [Systems] (DontDestroyOnLoad)            │    │
│  │  ├─ GameManager (State Machine)                      │    │
│  │  ├─ TimeManager (4 phases/an)                        │    │
│  │  ├─ SaveSystem (Ironman JSON)                        │    │
│  │  ├─ ClanManager, BloodRegistry                       │    │
│  │  ├─ Cultivation/Breakthrough/Aging/MentalStability   │    │
│  │  ├─ MirrorSystem, DeductionEngine                    │    │
│  │  ├─ FactionManager, Marriage, Alliance               │    │
│  │  ├─ ResourceManager, TaskAssignmentSystem            │    │
│  │  └─ EventManager                                     │    │
│  └──────────────────────────────────────────────────────┘    │
│                                                              │
│  GameEvents (static bus) ←─── tous les systèmes ───          │
│                                                              │
│  ScriptableObjects/  : Techniques, Items, Events, Factions  │
│  Prefabs/            : CombatUnit, GridCell, UI components  │
│  Art/Audio/          : Assets visuels et sonores (Shuimo)   │
└──────────────────────────────────────────────────────────────┘
```

---

## 🧩 Systèmes du jeu (détail)

### 1. Core — Boucle principale

| Système | Rôle | Fichier | Statut |
|---|---|---|---|
| **GameManager** | State machine principale (MainMenu/Loading/Playing/GameOver) | `Core/GameManager.cs` | ✅ |
| **TimeManager** | Cycle annuel en 4 phases + compteur d'années | `Core/TimeManager.cs` | ✅ |
| **SaveSystem** | Auto-save à fin d'année, Ironman, JSON | `Core/SaveSystem.cs` | ⚠️ (JsonUtility limité) |
| **GameEvents** | Bus global d'événements C# | `Events/GameEvents.cs` | ✅ |

### 2. Mirror — Interface joueur

| Système | Rôle | Fichier | Statut |
|---|---|---|---|
| **MirrorSystem** | Jauge MirrorPower 0-100, 3 interventions divines | `Mirror/MirrorSystem.cs` | ✅ (2 interventions manquantes) |
| **DeductionEngine** | Fusion de 2-5 fragments → Technique | `Mirror/DeductionEngine.cs` | ⚠️ (2 bugs compile) |
| **BloodRegistry UI** | Arbre généalogique rouleau de soie | ❌ Non fait | ❌ |
| **Deduction UI** | Table alchimique | ❌ Non fait | ❌ |

### 3. Clan — Lignée

| Système | Rôle | Fichier | Statut |
|---|---|---|---|
| **ClanManager** | Membres vivants, ajout/suppression | `Clan/ClanManager.cs` | ✅ |
| **BloodRegistry** | Base de données historique (morts+vivants) | `Clan/BloodRegistry.cs` | ✅ |
| **GeneticSystem** | Racine Spirituelle + Affinité élémentaire à la naissance | `Clan/GeneticSystem.cs` | ✅ |
| **LegacySystem** | Transfert d'héritage à la mort | `Clan/LegacySystem.cs` | ⚠️ (1 bug compile) |
| **Karma du Clan** | Bonus passif par génération | ❌ Non implémenté | ❌ |

### 4. Characters — Individus

| Système | Rôle | Fichier | Statut |
|---|---|---|---|
| **CultivationSystem** | Gain d'XP annuel selon tâche | `Characters/CultivationSystem.cs` | ✅ |
| **BreakthroughSystem** | Calcul réussite + conséquences | `Characters/BreakthroughSystem.cs` | ✅ |
| **AgingAndDeathSystem** | Vieillissement annuel + mort vieillesse | `Characters/AgingAndDeathSystem.cs` | ✅ |
| **MentalStabilitySystem** | Gestion stabilité 0-100 + modificateurs | `Characters/MentalStabilitySystem.cs` | ✅ |
| **WoundSystem** | Blessures post-combat + Blessures de Dao | `Characters/WoundSystem.cs` | ⚠️ (wounds non persistés) |
| **AscensionSystem** | Ascension finale au royaume immortel | `Characters/AscensionSystem.cs` | ✅ |

### 5. Combat — Tactique tour par tour

| Système | Rôle | Fichier | Statut |
|---|---|---|---|
| **TacticalCombatManager** | State machine combat | `Combat/TacticalCombatManager.cs` | ✅ |
| **GridSystem** | Grille 10x10 + distance Manhattan | `Combat/GridSystem.cs` | ✅ |
| **GridCell** | Case + terrain + occupant | `Combat/GridCell.cs` | ✅ |
| **TurnManager** | Queue d'initiative + tours | `Combat/TurnManager.cs` | ✅ |
| **CombatUnit** | Wrapper CharacterData pour combat | `Combat/CombatUnit.cs` | ✅ |
| **ICombatAction** | Interface Command Pattern | `Combat/ICombatAction.cs` | ✅ |
| **AttackAction** | Attaque physique adjacente | `Combat/AttackAction.cs` | ✅ |
| **MoveAction** | Déplacement | `Combat/MoveAction.cs` | ✅ (pas de A*) |
| **DefendAction** | Défense -50% dégâts | `Combat/DefendAction.cs` | ✅ |
| **TechniqueAction** | Utilisation de technique | ❌ Non fait | ❌ |
| **ItemAction** | Utilisation d'objet | ❌ Non fait | ❌ |
| **FleeAction** | Fuite | ❌ Non fait | ❌ |
| **CombatAI** | Strategy Pattern | `Combat/CombatAI.cs` | ⚠️ (2 strats sur 5) |
| **Terrain procédural** | Génération grille variée | ❌ Non fait | ❌ |

### 6. Diplomacy — Monde extérieur

| Système | Rôle | Fichier | Statut |
|---|---|---|---|
| **FactionManager** | 3 familles rivales + IA annuelle | `Diplomacy/FactionManager.cs` | ✅ (base, 3 factions hardcodées) |
| **MarriageSystem** | Mariage d'amour + mariage arrangé | `Diplomacy/MarriageSystem.cs` | ✅ |
| **AllianceSystem** | Tribut, non-agression, déclaration de guerre | `Diplomacy/AllianceSystem.cs` | ✅ |
| **EspionageSystem** | Vol de fragments | ❌ Non fait | ❌ |
| **WorldMap** | Carte du monde visuelle | ❌ Non fait | ❌ |

### 7. Economy — Ressources & tâches

| Système | Rôle | Fichier | Statut |
|---|---|---|---|
| **ResourceManager** | Pierres Spirituelles (autres à ajouter) | `Economy/ResourceManager.cs` | ⚠️ (1 ressource sur 5) |
| **TaskAssignmentSystem** | Attribution + résolution tâches | `Economy/TaskAssignmentSystem.cs` | ⚠️ (4 tâches sur 8) |
| **BuildingSystem** | 8 bâtiments × 5 niveaux | ❌ Non fait | ❌ |
| **Herbes / Minerais / Prestige / Fragments** | Ressources additionnelles | ❌ Non fait | ❌ |

### 8. Events — Aléatoire + scénarisé

| Système | Rôle | Fichier | Statut |
|---|---|---|---|
| **EventManager** | Roll annuel pondéré | `Events/EventManager.cs` | ⚠️ (4 events sur 10+) |
| **RandomEventData** (SO) | Données d'événements | ❌ Non fait | ❌ |
| **StoryEventData** (SO) | Événements scénarisés | ❌ Non fait | ❌ |

---

## 🎮 Boucle de gameplay annuelle (4 phases)

```
┌──────────────────────────────────────────────────────────────┐
│ PHASE 1 — MANAGEMENT (ManagementPhase)                       │
│ Le joueur assigne chaque membre à une tâche :                │
│ Cultivation · Mine · Patrouille · Étude · Enseignement       │
│ Diplomatie · Espionnage · Repos                              │
│ → TaskAssignmentSystem résout à la transition vers phase 2   │
└───────────────────────────────┬──────────────────────────────┘
                                ↓
┌──────────────────────────────────────────────────────────────┐
│ PHASE 2 — EVENTS (EventPhase)                                │
│ EventManager roll 1-100 sur table pondérée :                 │
│ Attaque monstres (20) · Visite diplomatique (15) ·           │
│ Ruines (10) · Génie (5) · Trahison (8) · Catastrophe (7) ·   │
│ Marchand (12) · Défi clan (10) · Épidémie (5) · Mariage (8)  │
│ → FactionManager roule son IA autonome aussi                 │
└───────────────────────────────┬──────────────────────────────┘
                                ↓
┌──────────────────────────────────────────────────────────────┐
│ PHASE 3 — BREAKTHROUGH (BreakthroughPhase)                   │
│ Pour chaque membre éligible :                                │
│ 1. Notification joueur                                       │
│ 2. Choix tenter ou attendre                                  │
│ 3. Calcul taux de réussite                                   │
│ 4. Séquence animée + choix Intervention Divine (temps limité)│
│ 5. Résultat : Succès / Échec mineur / Majeur / Catastrophique│
└───────────────────────────────┬──────────────────────────────┘
                                ↓
┌──────────────────────────────────────────────────────────────┐
│ PHASE 4 — INHERITANCE (InheritancePhase)                     │
│ Pour chaque mort de l'année :                                │
│ - Transfert techniques (70%/technique) aux enfants/élèves    │
│ - XP cultivation transféré (10%) aux enfants directs         │
│ - Patriarche → succession auto ou vote                       │
│ - +1 génération → Karma du Clan progresse                    │
└───────────────────────────────┬──────────────────────────────┘
                                ↓
                       AdvanceYear → PHASE 1
```

---

## 🏯 Les 6 Royaumes de Cultivation

| Royaume | Enum | Min Root | Max Age | Risque Percée | Power | Unlock |
|---|---|---|---|---|---|---|
| Embryonnaire | `Embryonic` | 0 | 80 | 5% | 1 | (départ) |
| Raffinement du Qi | `QiRefinement` | 10 | 120 | 15% | 20 | Combat, méditation |
| Fondation | `Foundation` | 30 | 250 | 30% | 100 | Spécialisation élémentaire, Vol d'Épée |
| Manoir Pourpre | `PurpleMansion` | 50 | 500 | 50% | 500 | Sens Spirituel, Patriarche |
| Noyau d'Or | `GoldenCore` | 70 | 1000 | 70% | 2500 | Domaines de Lois, ressources haut rang |
| Embryon Dao | `DaoEmbryo` | 90 | 3000 | — | 10000+ | Quasi-immortel, changement de corps, Ascension |

**Note** : Le master prompt mentionne un 7e palier "Immortel" avec `maxAge: 999999`. Dans le code actuel, c'est confondu avec `DaoEmbryo`. À clarifier si on veut aligner sur le prompt original ou garder 6 royaumes.

---

## 💀 Système de Blessures (Létalité)

```
Vitalité Max = Constitution × 10 × (1 + Realm × 0.5)

Dégâts reçus :
  0-30%   → Blessure Légère (1-3 mois)
  30-60%  → Blessure Moyenne (6-12 mois, -10% stats)
  60-90%  → Blessure Grave (1-3 ans, 20% séquelle)
  90-100% → Blessure Critique (1-5 ans, 80% séquelle)
  = 100%  → MORT

Blessure de Dao (permanente) :
  - Déclenchée par échec percée majeur ou coup critique
  - -10 à -30% sur 1 stat définitivement
  - -20% lifespan
  - Guérit uniquement avec item légendaire très rare
```

---

## 🎨 Direction Artistique — Shuimo

### Palette

| Nom | Hex | Usage |
|---|---|---|
| Parchemin | `#F5E6C8` | Fond principal |
| Encre profonde | `#1A1A2E` | Texte, contours |
| Or/Bronze | `#C9A959` | Accents chauds, UI boutons |
| Jade | `#3D8B8B` | Accents froids, info |
| Cinabre | `#C62828` | Danger, combat, sang |
| Violet impérial | `#6A0DAD` | Qi, spirituel |

### Règles UI

- L'UI entière = **ARTEFACT ANCIEN**
- ❌ Pas de flat design, pas de Material Design
- Panneaux = texture parchemin avec bords usés
- Boutons = plaques de bronze avec reflets
- Texte : calligraphique pour titres, serif lisible pour corps
- Bordures : motifs nuages stylisés / dragons
- Transitions : effet d'encre qui se répand

### Sprites personnages

5 tranches d'âge × 2 genres = 10 sprites par personnage-type :
- Enfant (0-11)
- Jeune Adulte (12-25)
- Adulte (26-60)
- Ancien (61-80% lifespan)
- Vénérable (80%+ lifespan)

+ Aura colorée selon Royaume et Élément.

### VFX (Particle System)

- Aura de Qi (halo translucide)
- Percée (explosion particules ascendantes + flash)
- Combat (traînées Qi + shockwave)
- Déviation de Qi (particules noires/rouges chaotiques)
- Vol d'Épée (traînée lumineuse sous le sprite)

---

## 🎵 Audio (3 ambiances)

| Scène | Musique | SFX |
|---|---|---|
| **Domaine** | Guqin (cithare), rivière, vent bambous | Oiseaux, cloches lointaines, soie |
| **Combat** | Taiko, Dizi, chants gutturaux | Impacts métal, whoosh Qi, craquements |
| **Percée** | Montée orchestrale, climax | Bourdonnement Qi, écho miroir, tonnerre |
| **UI/Menus** | — | Parchemin qui se déroule, clic bronze, goutte d'encre |

Dynamique : musique s'intensifie à l'approche d'un événement ou quand Vitalité < 40%.

---

## 💾 Système de Sauvegarde

- **Type** : Ironman (1 slot unique par défaut)
- **Moment** : Auto à la fin de chaque année
- **Format** : JSON sérialisé (+ compression à étudier)
- **Données** :
  - `BloodRegistry` complet (morts + vivants)
  - `ResourceManager.SpiritStones` (+ autres ressources à venir)
  - `FactionManager.Factions` (relations)
  - `MirrorSystem.MirrorPower`
  - `DeductionEngine.ClanTechniques`
  - `TimeManager.CurrentYear + CurrentPhase`
  - `AscensionSystem.AscendedAncestorsCount`
  - Seed du générateur aléatoire (reproductibilité)
- **Mode Casual (optionnel)** : 3 slots + save manuelle possible

---

## 🗺️ Phases de développement

### ✅ PHASE 1 — Fondations (Prototype Jouable)
**Objectif :** 1 cycle complet, 1 clan de 5 membres, limité à Fondation.
Statut : **~80% — le code existe mais ne compile pas & pas de projet Unity.**

### 🚧 PHASE 2 — Combat & Événements
Statut : **~50% — combat fonctionnel en code, EventManager minimal (4 events).**

### 🚧 PHASE 3 — Monde & Diplomatie
Statut : **~40% — Factions + Marriage + Alliance en code, pas d'UI, pas de WorldMap, pas d'Espionage.**

### ❌ PHASE 4 — Royaumes Avancés & Polish
Statut : **0% — aucune UI Shuimo, aucun audio, aucun sprite, aucune animation, pas d'équilibrage.**

---

## 🎯 Condition de victoire narrative

Le master prompt mentionne : **"Après 10 générations + ascension d'un membre au royaume Embryon Dao → le clan devient une légende."**

À confirmer : garder cette condition stricte ou proposer plusieurs fins (domination militaire, domination diplomatique, domination spirituelle, etc.).

---

## 📌 Changelog

| Date | Modification |
|---|---|
| 2026-04-14 | Création de `BRAIN_CLAUDE/`. Pivot web → Unity/C# acté. État initial documenté : 34 fichiers C# (3432 lignes), 5 bugs compile, projet Unity pas bootstrapped. |
| — | *Prochain changelog ici* |
