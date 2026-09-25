# ✅ DONE.md — Ce qui a été fait

> **Mis à jour à chaque étape du projet.** Dernière mise à jour : 2026-09-25 (L3 : techniques graduées).

---

## 🏗️ Architecture & Structure

| # | Tâche | Détails |
|---|---|---|
| 1 | **Arborescence C#** | Création de `Assets/_Project/Scripts/` avec sous-dossiers par domaine (Core, Clan, Characters, Combat, Mirror, Diplomacy, Economy, Events, Data, Tests). |
| 2 | **Namespaces homogènes** | Tous les scripts utilisent `MirrorChronicles.<Module>` (10 modules). |
| 3 | **Pattern Singleton** | Implémenté sur tous les Managers : `Instance` + Awake standard (destroy duplicate + DontDestroyOnLoad sur GameManager). |
| 4 | **Pattern Observer** | `GameEvents` static class — bus d'événements C# (`OnYearStarted`, `OnPhaseChanged`, `OnCharacterBorn`, `OnCharacterDied`, `OnBreakthroughSuccess`, `OnBreakthroughFailed`, `OnSpiritStonesChanged`). |

---

## 🎯 Core — Boucle principale

| # | Tâche | Détails |
|---|---|---|
| 5 | **GameManager** | State Machine principale : MainMenu / Loading / Playing / GameOver. Singleton. Démarre en `Playing` pour les tests Phase 1. |
| 6 | **TimeManager** | Cycle annuel en 4 phases : Management → Events → Breakthrough → Inheritance. Méthode `AdvancePhase()` qui loop sur `AdvanceYear()`. Publie `OnYearStarted` + `OnPhaseChanged`. |
| 7 | **SaveSystem** | Auto-save à fin d'année (écoute `OnYearStarted` avec `newYear > 1`). Format JSON via `JsonUtility`. Chemin : `Application.persistentDataPath/ironman_save.json`. Try/catch sur Save et Load. ⚠️ Limitation sur auto-properties — voir WL-006. |
| 8 | **GameEvents bus** | 7 events globaux + méthodes `Trigger*` pour invoquer en toute sécurité (null-safe). |

---

## 👥 Clan — Lignée

| # | Tâche | Détails |
|---|---|---|
| 9 | **ClanManager** | Gère `List<CharacterData> LivingMembers`. Initialise 5 membres fondateurs (Patriarche Wei, Matriarche Xue, Fils Jian avec racine 75 Foudre, Fille Mei, Oncle Shan). `AddMember` + `HandleCharacterDeath`. |
| 10 | **BloodRegistry** | Base de données historique `List<CharacterData> HistoricalRecords` (vivants + morts). `GetCharacterByID`, `GetChildrenOf`. Écoute `OnCharacterBorn` pour enregistrer. |
| 11 | **GeneticSystem** | Classe statique. `GenerateSpiritualRoot` = moyenne parents + mutation [-15,+15], clamp [1,100]. `GenerateAffinity` = 70% héritée, 30% mutation. Éléments rares (Foudre 16%, Light/Dark 2% chacun). |
| 12 | **LegacySystem** | ⚠️ 1 bug compile (`RootElement` au lieu de `Affinity`). Transfère 50 Spirit Stones à la mort + log Dao Fragment si Royaume ≥ GoldenCore. |

---

## 🧬 Characters — Individus

| # | Tâche | Détails |
|---|---|---|
| 13 | **CultivationSystem** | Gain XP annuel = 10 + (SpiritualRoot / 2), multiplier 0.8 si MentalStability < 50. Seuils XP : 100 / 500 / 2000 / 10000 / 50000. Notifie l'éligibilité à la percée via Debug.Log. |
| 14 | **BreakthroughSystem** | `CalculateSuccessRate` avec modificateurs (Spiritual Root, Mental Stability, âge). Roll 1-100. 3 niveaux d'échec : Mineur (70%), Majeur (25%), Catastrophique (5% → mort par QiDeviation). |
| 15 | **AgingAndDeathSystem** | Écoute `OnYearStarted`. Incrémente Age, compare à MaxLifespan (80/120/250/500/1000/3000 selon Royaume). `Die(character, cause)` publie `OnCharacterDied`. |
| 16 | **MentalStabilitySystem** | Clamp 0-100. `ApplyModifier`. Écoute mort (-15 pour proches : parents, enfants, conjoint), percée réussie (+10), percée échouée (-10). |
| 17 | **WoundSystem** | `EvaluatePostCombatWounds` avec 4 seuils (0-30-60-90%). 20% chance Dao Wound si Severe, 80% si Critical. Dao Wound = -20% lifespan + -20 stability. ⚠️ Wounds non persistés sur CharacterData (juste des logs). |
| 18 | **AscensionSystem** | Écoute `OnBreakthroughSuccess`. Si nouveau royaume = DaoEmbryo → Ascend : compteur `AscendedAncestorsCount++`, retire du clan, log Divine Blessing. |

---

## 🪞 Mirror — Interface joueur

| # | Tâche | Détails |
|---|---|---|
| 19 | **MirrorSystem** | Jauge `MirrorPower` 0-100 (démarre à 50). Recharge passive +1/an, +5 par percée réussie. Qi Pulse (10, +30% Qi +15% HP sur CombatUnit), Ancestral Shield (25, +30% percée), Mirror Judgment (50 → QiDeviation). |
| 20 | **DeductionEngine** | Accepte 2-5 FragmentData, coût MirrorPower = inputs.Count × 10. Détermine élément dominant par count, calcule RiskFactor avec conflits (Water+Fire +30, Metal+Wood +30, synergie Wood+Fire -10). Génération procédurale de nom avec préfixes + suffixes. ⚠️ 2 bugs compile. |

---

## ⚔️ Combat — Tactique tour par tour

| # | Tâche | Détails |
|---|---|---|
| 21 | **TacticalCombatManager** | State Machine : Initialization / Deployment / PlayerTurn / EnemyTurn / Victory / Defeat. `InitializeCombat` prend les 3 premiers membres du clan + génère 3 bandits. `ProcessPostCombat` appelle WoundSystem. |
| 22 | **GridSystem** | Grille 10x10 (extensible). `GetCellAt`, `GetDistance` (Manhattan), `GetAdjacentCells` (4 directions). Init avec terrain procédural : clusters Forest/Mountain/Water + spots ConcentratedQi (1-3). A* pathfinding via `FindPath(start, goal)` avec coûts terrain. |
| 23 | **GridCell** | POCO X, Y, Terrain, Occupant. `GetMovementCost` (Mountain=2, Water=2, Plain=1). 5 types de terrain : Plain, Forest, Mountain, Water, ConcentratedQi. |
| 24 | **TurnManager** | Queue d'initiative recalculée chaque round. Formule : `Agility + (Realm × 10)`. Nettoie les unités mortes. Notifie `TacticalCombatManager` du changement de tour. |
| 25 | **CombatUnit** | Wrapper MonoBehaviour autour de CharacterData. MaxVitality = Constitution × 10 × (1 + Realm × 0.5). MaxQi = SpiritualRoot × (Realm+1). Défend = -50% dégâts. Régen +5% Qi sur ConcentratedQi. |
| 26 | **ICombatAction** | Interface Command Pattern avec `Type`, `GetQiCost`, `IsValid`, `Execute`. |
| 27 | **AttackAction** | Dégâts = Strength × (1 + Realm × 0.2) − TargetDefense (min 1). Range 1. Coût Qi 0. |
| 28 | **MoveAction** | Distance max = max(1, Agility / 10). Pas de A*, juste check range Manhattan + case non-occupée. |
| 29 | **DefendAction** | Met `IsDefending = true` jusqu'à `ResetTurnState`. |
| 30 | **CombatAI (Strategy)** | Interface `IAIStrategy`. 5 stratégies implémentées : Aggressive, Defensive, Strategic (cible supports, utilise terrain), Berserker (plus proche, tout le Qi en techniques), Cautious (fuit si Vit<40%, économise Qi). |
| 30b | **TechniqueAction** | `ICombatAction` consommant Qi. Range variable depuis `TechniqueData.Range`. MartialArt = offensif (dégâts = PowerModifier × realmMult − defense, +25% affinity bonus, +20% terrain Water). SupportArt = heal allié. CultivationMethod = pas utilisable en combat. |
| 30c | **ItemAction** | `ICombatAction` pour pilules. HealingPill → `Heal()`, QiRestorationPill → `RestoreQi()`. Range 1, cible allié uniquement, consomme `ItemData.Quantity`. |
| 30d | **FleeAction** | `ICombatAction` fuite. Chance succès = userAgility / (userAgility + enemyAgility + 1). Échec = tour perdu. Succès = retrait du combat. |

---

## 🌍 Diplomacy — Monde extérieur

| # | Tâche | Détails |
|---|---|---|
| 31 | **FactionManager** | Initialise 3 factions hardcodées : Wang Family (Aggressive, 500 power, relation -20), Zhao Merchant (Merchant, 200 power, +10), Azure Cloud Sect (Isolationist, 5000 power, 0). IA annuelle simple déclenchée sur `OnPhaseChanged → Events`. |
| 32 | **MarriageSystem** | `CanMarry` valide Age ≥ 18, pas déjà marié, aucun ancêtre commun sur 3 générations (`KinshipRules`). `HandleLoveMarriage` +10 stability, le conjoint extérieur rejoint le clan. `HandleArrangedMarriage` crée un vrai conjoint (sexe opposé, adulte, QiRefinement) qui rejoint le clan, boost relation +25 ; forcé = -15 stability, volontaire = +5. |
| 32b | **KinshipRules** (2026-09-24) | Règle "mariage interdit ≤ 3 générations" : remonte FatherID/MotherID sur N générations via BloodRegistry, refuse tout ancêtre commun (frères/sœurs, parent/enfant, oncle/nièce, cousins germains et issus de germains). Voir WL-011. |
| 32c | **Mariages annuels** (2026-09-24) | `MarriageMatchmaker` (logique pure) + `MarriageSystem.ProcessAnnualMarriages` en phase Events (toujours avant les naissances de la phase Inheritance) : chaque membre éligible (vivant, célibataire, 18-40 ans) a 30 %/an de se marier avec le membre non apparenté du sexe opposé le plus proche en âge, sinon avec un cultivateur errant qui rejoint le clan. Débloque les naissances au-delà du couple fondateur. Voir WL-012. |
| 33 | **AllianceSystem** | `OfferTribute` (5 + stones/100, ×1.5 si Merchant). `ProposeNonAggression` accepté si relation ≥ 0 (+10). `DeclareWar` met la relation à -100. |

---

## 💰 Economy — Ressources & tâches

| # | Tâche | Détails |
|---|---|---|
| 34 | **ResourceManager** | 1 seule ressource : Spirit Stones (départ 1000). `Add`, `Consume`, `Set`. Publie `OnSpiritStonesChanged`. |
| 35 | **TaskAssignmentSystem** | `AssignTask` avec validation (Embryonic ne peut que Cultivation/Rest/None). `ProcessYearlyTasks` sur transition Management → Events. 4 tâches implémentées : Cultivation, Mine (yield 50 + Realm × 25), Patrol (compteur), Rest (+5 stability). 4 tâches manquantes : Study, Teaching, Diplomacy, Espionage. |

---

## 🎲 Events — Aléatoire

| # | Tâche | Détails |
|---|---|---|
| 36 | **EventManager** | Refactorisé avec table pondérée `List<RandomEventData>` (ScriptableObject). 11 events avec effets. Filtrage par `MinYear` + `MinPatriarchRealm`. Fallback hardcodé si pas de SO assignés. |
| 36b | **StoryEventManager** | Événements scénarisés one-time. 6 triggers (FirstFoundation, FirstGoldenCore, PatriarchBetrayal, FirstAscension, ClanExtinctionThreat). Choix avec StoryOutcome (MS/stones/relation). `PendingEvent` + `ResolveChoice(index)` pour la future UI. |

---

## 📦 Data — POCOs

| # | Tâche | Détails |
|---|---|---|
| 37 | **CharacterData** | POCO `[Serializable]` avec Guid ID, identité, genetics, cultivation, stats, famille (IDs string). FullName = `$"{LastName} {FirstName}"`. ⚠️ Auto-properties non sérialisées par JsonUtility — voir WL-006. |
| 38 | **GameData** | Root de la sauvegarde : SaveVersion, ClanName, CurrentYear, CurrentPhase, SpiritStones, HistoricalRecords. |
| 39 | **TechniqueData + FragmentData** | POCOs `[Serializable]`. Technique avec Type, Element, RequiredRealm, Power, QiCost, Range, RiskFactor. Fragment avec Element + Quality 1-5. |
| 39b | **ItemData** | POCO `[Serializable]` avec ItemType (HealingPill, QiRestorationPill), Power, Quantity. |
| 39c | **CharacterData.KnownTechniqueIDs** | `List<string>` référençant les techniques connues du personnage. |
| 40 | **FactionData** | POCO avec Name, Personality (5 types), PowerLevel, Wealth, RelationWithPlayer (-100 à +100). |
| 41 | **Enums** | GameState, GamePhase, CultivationRealm (6 valeurs), TaskType (9 valeurs), Element (9 valeurs), DeathCause (6 valeurs). |
| 42 | **CombatEnums** | CombatState (7), TerrainType (5), AIStrategyType (5), ActionType (6). |

---

## 🧪 Tests

| # | Tâche | Détails |
|---|---|---|
| 43 | **GameSimulationTest** | Coroutine qui simule 10 ans en ~10 secondes. Triggers : Deduction en année 3, Mariage politique en année 5, Ascension en année 7, Mort en année 9. ⚠️ 2 bugs compile (méthodes privées appelées). Pas un vrai test Edit/Play Mode NUnit. |
| 43a | **Échelle de puissance (L1, 2026-09-25)** | `PowerLadder` (6 chakras / 3 épreuves, 9 niveaux de Qi, 4 stades, durées de vie du lore, XP par sous-niveau, mur de la Fondation, dissolution spirituelle), `RankCatalog` (noms de rangs par voie : immortelle, bouddhiste, diable, divine ; équivalences asymétriques), `BreakthroughRules` (chances et issues). Modèle multi-voies : `CultivationPath` (6 Dao), `CultivationSubPath`, `Species`, `GoldenCoreState`. Percées tentées en phase Percée (jamais appelées avant). `Scripts/run-pure-tests.sh` : 113 tests purs verts sans Unity. |
| 43a2 | **Orifice spirituel héréditaire (L2, 2026-09-25)** | `SpiritualOrificeRules` : orifice tiré à la naissance (0,3 % sans parent doté, 35 % avec un, 50 % avec deux), seuls les cultivateurs à l'Œil du Sommet ou au-delà le détectent (examen du clan en phase Héritage), mortels 60-80 ans, Graines de Sceau du miroir (40 Puissance, 2 actives + éclats restaurés). `TaskRules` partagé par l'assignation, la résolution annuelle et l'interface. Affichage « Orifice non examiné » / « Mortel » avant le premier chakra. Vieillissement sur la durée de vie propre à chacun ; `DaoWounds` garde la perte d'une blessure du Dao après les percées. 164 tests purs verts. |
| 43a3 | **Simulation sans moteur (G1, 2026-09-25)** | Tout le jeu tourne hors d'Unity et de Godot dans `src/Core` : `GameSession` (fondation du clan Mo, phases, sauvegarde v2), 30 systèmes portés, 383 tests `dotnet test` dont des parties de 100 ans sur plusieurs graines (invariants, mariages, naissances, percées, déterminisme). |
| 43a4 | **Migration Godot terminée (G2-G5, 2026-09-25)** | Contenu en `game/data/*.json` validé au chargement ; logique du combat (grille à graine, A*, 6 actions, 5 IA, `Battle`) ; couche Godot (autoload `GameRoot`, écran `ClanDomain`, fumée headless et capture) ; arbre Unity supprimé, CI `dotnet test` + Godot headless. Revue ECC des G2-G4 : 6 constats (1 élevé, 4 moyens, 1 faible) corrigés en RED → GREEN, vérifiés et approuvés. 501 tests verts. |
| 43a5 | **Techniques graduées (L3, 2026-09-25)** | Catalogue `techniques.json` (26 méthodes de Qi du lore + *Lotus Blanc*, *Sutra de l'Aîné*, *Dialogue de Gongye Shu*) et `qi.json`, validés au chargement. Grade 1-7+ : vitesse (`balance.json`) et plafond de royaume ; secret du Manoir Pourpre ; entrer en Culture du Qi absorbe une portion du Qi de la méthode (aucune pour le *Souffle Commun*) ; récolte du Qi dès l'Œil du Sommet ; un Qi cultivateur reste lié à son Qi ; défauts du *Veilleur du Sentier* (vitesse, durée de vie, impuissance face au sutra d'origine). Déduction du miroir → technique secrète graduée, nommée par les données. Méthode et Qi à l'écran. Sauvegarde 2.1, anciennes sauvegardes mises à niveau. 610 tests verts. |
| 43b | **KinshipRulesTests** (2026-09-24) | 14 tests EditMode. Les 12 tests de logique pure passent dans un harnais `dotnet test` NUnit ; les 2 tests `CanMarry` attendent l'éditeur Unity (non installé). |
| 43c | **MarriageMatchmakerTests** (2026-09-24) | 16 tests EditMode purs (éligibilité, partenaire non apparenté, conjoint extérieur, planification annuelle). RED 8 échecs → GREEN 28/28 avec les tests de parenté. |

---

## 📊 Métriques

> Au 2026-09-25, après L3 (techniques graduées).

- **Fichiers C#** : 68 dans `src/Core`, 3 scripts Godot minces, 51 fichiers de tests
- **Lignes de code** : 5 940 (simulation) + 380 (Godot) ; 5 740 de tests (610 tests)
- **Namespaces** : 10
- **Singletons** : 0 — un `GameContext` par partie ; côté Godot, le seul autoload `GameRoot`
- **Événements du bus** : 11 (`GameEventBus`, un par partie)
- **Données** : 8 fichiers `game/data/*.json` (dont 29 techniques et 28 Qi), validés au chargement
- **Royaumes implémentés** : 6 sur 6
- **Design patterns** : Observer (bus par partie), State Machine (horloge des phases), Strategy (5 IA de combat), Command (6 actions), racine de composition (`GameSession`), instantané détaché pour les sauvegardes

---

*Voir `NOT_DONE.md` pour la liste complète des tâches restantes.*
*Voir `WORKED_LESSON.md` pour les bugs identifiés dans le code existant.*
