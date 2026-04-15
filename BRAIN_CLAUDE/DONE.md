# ✅ DONE.md — Ce qui a été fait

> **Mis à jour à chaque étape du projet.** Dernière mise à jour : 2026-04-14 (audit initial après pivot web → Unity/C#).

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
| 19 | **MirrorSystem** | Jauge `MirrorPower` 0-100 (démarre à 50). Recharge passive +1/an, +5 par percée réussie. 3 Interventions Divines implémentées : Qi Pulse (10), Ancestral Shield (25), Mirror Judgment (50 → QiDeviation sur cible). |
| 20 | **DeductionEngine** | Accepte 2-5 FragmentData, coût MirrorPower = inputs.Count × 10. Détermine élément dominant par count, calcule RiskFactor avec conflits (Water+Fire +30, Metal+Wood +30, synergie Wood+Fire -10). Génération procédurale de nom avec préfixes + suffixes. ⚠️ 2 bugs compile. |

---

## ⚔️ Combat — Tactique tour par tour

| # | Tâche | Détails |
|---|---|---|
| 21 | **TacticalCombatManager** | State Machine : Initialization / Deployment / PlayerTurn / EnemyTurn / Victory / Defeat. `InitializeCombat` prend les 3 premiers membres du clan + génère 3 bandits. `ProcessPostCombat` appelle WoundSystem. |
| 22 | **GridSystem** | Grille 10x10 (extensible). `GetCellAt`, `GetDistance` (Manhattan), `GetAdjacentCells` (4 directions). Init avec terrain Plain partout. |
| 23 | **GridCell** | POCO X, Y, Terrain, Occupant. `GetMovementCost` (Mountain=2, Water=2, Plain=1). 5 types de terrain : Plain, Forest, Mountain, Water, ConcentratedQi. |
| 24 | **TurnManager** | Queue d'initiative recalculée chaque round. Formule : `Agility + (Realm × 10)`. Nettoie les unités mortes. Notifie `TacticalCombatManager` du changement de tour. |
| 25 | **CombatUnit** | Wrapper MonoBehaviour autour de CharacterData. MaxVitality = Constitution × 10 × (1 + Realm × 0.5). MaxQi = SpiritualRoot × (Realm+1). Défend = -50% dégâts. Régen +5% Qi sur ConcentratedQi. |
| 26 | **ICombatAction** | Interface Command Pattern avec `Type`, `GetQiCost`, `IsValid`, `Execute`. |
| 27 | **AttackAction** | Dégâts = Strength × (1 + Realm × 0.2) − TargetDefense (min 1). Range 1. Coût Qi 0. |
| 28 | **MoveAction** | Distance max = max(1, Agility / 10). Pas de A*, juste check range Manhattan + case non-occupée. |
| 29 | **DefendAction** | Met `IsDefending = true` jusqu'à `ResetTurnState`. |
| 30 | **CombatAI (Strategy)** | Interface `IAIStrategy`. 2 stratégies implémentées : Aggressive (bouge vers plus proche + attaque), Defensive (juste Defend). 3 stratégies à faire : Strategic, Berserker, Cautious. |

---

## 🌍 Diplomacy — Monde extérieur

| # | Tâche | Détails |
|---|---|---|
| 31 | **FactionManager** | Initialise 3 factions hardcodées : Wang Family (Aggressive, 500 power, relation -20), Zhao Merchant (Merchant, 200 power, +10), Azure Cloud Sect (Isolationist, 5000 power, 0). IA annuelle simple déclenchée sur `OnPhaseChanged → Events`. |
| 32 | **MarriageSystem** | `CanMarry` valide Age ≥ 18, pas déjà marié, pas frères/sœurs directs. `HandleLoveMarriage` +10 stability. `HandleArrangedMarriage` avec boost relation +25 ; forcé = -15 stability, volontaire = +5. |
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
| 36 | **EventManager** | Écoute `OnPhaseChanged → Events`. Roll 1-100 avec 4 events basiques : MonsterAttack (20%), WanderingMerchant (15%), RuinsDiscovery (10%), InternalBetrayal (10%), PeacefulYear (45%). Pas encore de résolution d'événement (juste logs). |

---

## 📦 Data — POCOs

| # | Tâche | Détails |
|---|---|---|
| 37 | **CharacterData** | POCO `[Serializable]` avec Guid ID, identité, genetics, cultivation, stats, famille (IDs string). FullName = `$"{LastName} {FirstName}"`. ⚠️ Auto-properties non sérialisées par JsonUtility — voir WL-006. |
| 38 | **GameData** | Root de la sauvegarde : SaveVersion, ClanName, CurrentYear, CurrentPhase, SpiritStones, HistoricalRecords. |
| 39 | **TechniqueData + FragmentData** | POCOs `[Serializable]`. Technique avec Type, Element, RequiredRealm, Power, QiCost, RiskFactor. Fragment avec Element + Quality 1-5. |
| 40 | **FactionData** | POCO avec Name, Personality (5 types), PowerLevel, Wealth, RelationWithPlayer (-100 à +100). |
| 41 | **Enums** | GameState, GamePhase, CultivationRealm (6 valeurs), TaskType (9 valeurs), Element (9 valeurs), DeathCause (6 valeurs). |
| 42 | **CombatEnums** | CombatState (7), TerrainType (5), AIStrategyType (5), ActionType (6). |

---

## 🧪 Tests

| # | Tâche | Détails |
|---|---|---|
| 43 | **GameSimulationTest** | Coroutine qui simule 10 ans en ~10 secondes. Triggers : Deduction en année 3, Mariage politique en année 5, Ascension en année 7, Mort en année 9. ⚠️ 2 bugs compile (méthodes privées appelées). Pas un vrai test Edit/Play Mode NUnit. |

---

## 📊 Métriques

- **Fichiers C#** : 34
- **Lignes de code** : 3432
- **Namespaces** : 10
- **Singletons** : 19
- **Events globaux** : 7
- **Royaumes implémentés** : 6 sur 6
- **Design patterns** : 4 sur 8 attendus (Singleton, Observer, State Machine, Strategy, Command — tous implémentés ; MVC, Memento, Object Pool pas encore)

---

*Voir `NOT_DONE.md` pour la liste complète des tâches restantes.*
*Voir `WORKED_LESSON.md` pour les bugs identifiés dans le code existant.*
