# ✅ DONE.md — Ce qui a été fait

> **Mis à jour à chaque étape du projet.** Dernière mise à jour : 2026-09-25 (L4 : lignées, fondations, Manoir Pourpre).

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
| 43a6 | **Lignées, fondations, Manoir Pourpre (L4, 2026-09-25)** | Registre des 63 lignées (`fruitions.json`) et état du monde tiré de la graine ; fondation formée depuis le Qi de sa méthode (liens du wiki) ; Partenaires Dao et leur consommation ; Cœur Dao (tempérament héréditaire qui s'aligne) ; percée du Manoir Pourpre en 4 épreuves (Montée, Manifestation, Grand Vide, Illusions) avec retraites ; capacités divines 2 à 5 (cultivation alignée, ressources, Greffe du Dao, Seuil d'Immortalité) ; écran. Sauvegarde 2.2. 698 tests verts. |
| 43a7 | **Routes du Noyau d'Or (L4b, 2026-09-26)** | `GoldenCoreSystem` et `GoldenCoreRules` : forge de l'essence métallique puis demande de position (Réalisation, Surplus, Intercalaire 4+1 et 3+2), Vrai Monarque sans position, permission du détenteur, Démon d'Essence Métallique, Main Gauche vraie et fausse, méthodes de recherche d'or et voies de la Main Gauche comme savoir déchiffré par le miroir, position au domaine ; sauvegarde 2.5. `GoldenCoreTests`, `LeftHandTests`, `ClanDomainViewTests` : RED de compilation puis GREEN, revue ECC traitée (le registre refuse de prendre une Réalisation tenue), 803 tests verts. |
| 43a8 | **Carte et factions renommées (L5, 2026-09-26)** | `regions.json` et `RegionDefinition` (52 lieux, voisinages symétriques vérifiés au chargement), `factions.json` réécrit (35 puissances du lore, `FactionData` : type, région, voie, royaume le plus haut, techniques, provenance), `WorldMapView` et écran Godot `WorldMap` (`MapCanvas` : encre sur papier, placement glouton des étiquettes). `RegionContentTests`, `FactionContentTests`, `WorldMapViewTests` : RED puis GREEN ; revue faite à la main (la revue ECC a buté sur la limite d'usage) : quatre validations ajoutées. 853 tests verts. |
| 43a9 | **Suites L4c et analyse du wiki (2026-09-26)** | Constantes des épreuves de L1-L2 dans `balance.json` (`TrialSettings`) ; chronique du savoir (`KnowledgeView`) ; carte recalée sur la carte du wiki (37 lieux, provenance Wiki) ; Qi de talisman (`TalismanSystem`, prières, rituel, offre, bond, traits actifs ; sauvegarde 2.6) ; quatre fondations nommées d'après les fiches ; 20 figures des puissances (`figures.json`). RED puis GREEN à chaque étape ; le garde-fou des noms propres a intercepté deux noms de la source. 914 tests verts. |
| 43a10 | **Suites L4c, L4e, L5b (2026-09-26)** | `KnowledgeExchange` (achat, lecture des Partenaires) ; Imagerie ; corps inhumains ; emprunt de lumière ; Dao mûr ; stades 2-4 du Noyau d'Or ; Transfert et Transformation ; Reprise de la Fruition ; lignée transformée ; voisins ; vol de manuels ; zoom de la carte. Revue ECC : offre de talisman bloquante corrigée. RED puis GREEN à chaque étape, 959 tests verts. |
| 43a11 | **Rituel des bêtes (2026-09-26)** | Décisions de l'utilisateur : sacrifice de bêtes chassées (tâche `HuntBeast`, terrain de chasse, bêtes solitaires ou d'une puissance, rancune si découvert), qualité selon la puissance de la bête ; revue ECC (lumière empruntée séparée du verrou définitif). Sauvegarde 2.7. 972 tests verts. |
| 43a12 | **Chasse rituelle, L2c.1-L2c.3 (2026-09-26)** | D7 « tout est complot » ; calendrier du rituel (20 ans, chasse 3 ans avant) ; bêtes du monde et repérage ; chasse comme opération planifiée ; soupçons et méfiances cachés. Revue ECC traitée. Sauvegarde 2.9. 1009 tests verts. |
| 43a13 | **Complots, L2c.4 (2026-09-26)** | `PlotSystem` (enquêtes, preuves, frappe avec ou sans preuve, méfiance des témoins), `SecretSystem` (fuites, serment du secret, indices sur le miroir, brouillage, fausse preuve, confrontation, saisie = défaite, vente du secret), `SuspicionLedger` étendu. 1033 tests verts. |
| 43a14 | **Écran des opérations secrètes, L2c.5 (2026-09-26)** | `OperationsView` (rituel, cibles, candidats, aperçu d'un plan, signes perçus sans chiffres, dépositaires) et scène Godot `Operations` en trois onglets ; raisons de refus en français. 1045 tests verts. |
| 43a15 | **Complots des puissances et captifs, L6a (2026-09-26)** | `SchemeSystem` (embuscades par profit, une par an au plus), `CaptiveSystem` (captifs des deux côtés : interrogatoire un an après la capture, exécution passé la patience, rançon, sauvetage, échange, effacement par le miroir ; agents revendus, relâchés, interrogés, dénoncés, exécutés), `SchemeRules`, `SchemeSettings` et `Prisoner` (sauvegarde 2.10), `DeathCause.Executed`, captifs exclus des tâches, opérations, mariages, percées et de la succession ; onglet « Captifs » de l'écran des opérations, chronique des enlèvements. Relecture appliquée : fausse preuve refusée par la règle du miroir elle-même, choix périmés remis à zéro, chasse découpée. |
| 43a16 | **Qi des lieux et carte peinte, L5b (2026-09-26)** | `RegionalQi`, `RegionalQiRules`, `atmospheres.json` (4 atmosphères du lore), `regionalQi` (éléments et densité par nature de lieu, fortune des lignées), champs `qi`, `qiDensity`, `atmosphereId` des lieux vérifiés au chargement ; cultivation et percées du clan selon son lieu ; `WorldMapView.QiOf` ; carte peinte (grain, lavis, brumes d'atmosphère, glyphes au pinceau, noms mineurs au zoom). `ContentGaps` couvre atmosphères et talismans. |
| 43a17 | **Écrans G6 : bibliothèque et arbre généalogique (2026-09-26)** | `LibraryView` (arts connus, Qi en réserve, pratiquants ; marché du savoir avec prix et raison du refus, `KnowledgeExchange.PurchaseRefusal`) et scène `Library` ; `GenealogyView` (générations, couples côte à côte, morts, captifs) et scènes `Genealogy`/`GenealogyCanvas` ; navigation du domaine sur sa propre ligne ; smoke `--lib --tree`. |
| 43a18 | **Connaisseurs du miroir (2026-09-27)** | `MirrorLore` et `MirrorLoreRules` (seules les figures du Noyau d'Or et au-delà, selon royaume et âge ; ancien anonyme ; tirage fixé par la graine), « mirrorLore » dans `balance.json` ; `SecretSystem` : une puissance ordinaire ne soupçonne qu'un trésor et peut vendre la rumeur à la puissance d'un connaisseur ; confrontation et saisie par un connaisseur seulement, qui ne parle qu'à un pair ; deux connaisseurs à la fois : le plus fort confronte. |
| 43a19 | **Traités et diplomatie (2026-09-27)** | `TreatySystem`, `TreatyRules`, `Treaty`/`TreatySettings` (sauvegarde 2.11) : non-agression, commerce, défense, vassalité dans les deux sens (emprise du suzerain), secret ou scellé par serment, acceptation par le profit, trahison annuelle, découverte d'un traité secret, rupture par le clan ; effets sur les embuscades, les frappes et le marché du savoir ; `DiplomacyView` et écran `Diplomacy` ; l'ancien bonus de non-agression retiré. |
| 43a20 | **Politique des puissances (2026-09-27)** | `PowerPoliticsSystem`, `PoliticsRules`, `PowerBond`/`Coalition`/`CallToArms`/`PoliticsSettings` (sauvegarde 2.12) : alliances entre puissances, querelles, soumission et absorption des vassaux, coalition contre le clan (complote deux fois plus), appel aux armes (répondre, refuser, silence = refus), clan vassal absorbé (défaite, `OnClanAbsorbed`) ; `FactionManager.AreNeighbours`/`RemoveFaction` ; chronique ; écran Diplomatie (alliés publics, suzerain, coalition, appel, absorption à venir). |
| 43a21 | **Intrigues (2026-09-27)** | `IntrigueSystem`, `IntrigueRules`, `Demand`/`IntrigueTarget`/`IntrigueSettings` (sauvegarde 2.13) : chantage (payer, refuser, silence = refus), vol (bête, manuel copié, pierres, Qi ; patrouilles, voleur pris), espions envoyés en mariage arrangé (`FromFaction`, `SpyFor`), sonde du miroir, agent double, exécution (`DeathCause.ExecutedAsSpy`) ; chronique des vols ; Diplomatie (chantage) et Opérations/Secrets (conjoints). |
| 43a22 | **Secrets et sondages, 5a (2026-09-27)** | `SecretBook` (secrets par rang, nés des actes du clan via `OnDeed`, tirés pour les puissances, indices pelés du moins grave, preuve d'un secret du clan percé, absorption), `ProbeSystem` et `ProbeRules` (cinq méthodes, facteurs, partenaires, alliés à temps ou qui traînent, éventé, désastre, IA des puissances, vigilance), `secrets.json`, « secrets » dans `balance.json`, sauvegarde 2.14, chronique des sondeurs surpris, relevé des interprétations. |
| 43a23 | **Méfiance du clan et usages des secrets, 5b (2026-09-27)** | `ClanWatch` (mémoire des offenses, qui s'efface ; sondages plus durs pour les puissances dont le clan se méfie ; signe dans la Diplomatie), `SecretDealings` (chantage, révélation, vente ; les puissances révèlent aussi), `SecretsView`, onglet Sondages ; sauvegarde 2.15 (`ClanDistrust`, `SpentSecrets`). |
| 43a24 | **Alliance matrimoniale (2026-09-27)** | `MarriageAlliance` (proposition, refus expliqué, valeur du membre, répudiation), `TreatyKind.Marriage` et `Treaty.SpouseId`, `ClanManager.Depart` et `CharacterData.Departed`, lien qui épargne, réchauffe et trahit moins, fin avec le couple ; Diplomatie (membre à marier, « Répudier ») ; arbre (« retourné(e) auprès des siens »). |
| 43a25 | **Pactes de très haut niveau (2026-09-27)** | `PatronSystem`, `patrons.json` (Renarde des Monts Qingyan du lore, deux partenaires interprétés), tribut, faveur, protection contre les embuscades, fragments, regard sur les sondages, colère ; Diplomatie (grands partenaires) ; chronique ; sauvegarde 2.16. |
| 43a26 | **Guerres ouvertes (2026-09-27)** | `WarSystem`, `WarRules`, `War`/`ClanWar`/`WarSettings` (sauvegarde 2.17) : coalitions d'alliés, batailles, reddition et vassalité, hégémon, appel aux armes, guerre du clan et paix ; chronique ; Diplomatie (guerres, « Demander la paix »). |
| 43a27 | **Relecture des guerres (2026-09-27)** | Pas de guerre contre un partenaire de traité (le rompre d'abord) ; toute guerre finit par une paix racontée (lassitude comprise) ; une puissance absorbée quitte ses guerres ; camps modifiés par `with` ; le clan n'est pas appelé quand ses alliés sont des deux côtés ; un allié ne répond qu'une fois par an ; « fait la guerre au clan ». |
| 43a28 | **Écran du miroir (2026-09-27, G6)** | `MirrorView` + `MirrorScreen` (bouton « Miroir » du domaine, smoke `--mir`, `MIR_TAB`) : puissance et Graines de Sceau, interventions avec coût, effet et refus ; Graine proposée seulement aux mortels examinés (un orifice non examiné n'est jamais deviné) ; Jugement confirmé en deux temps, origine des alliés par mariage ; déduction avec choix des fragments et coût. Noms des fragments traduits. |
| 43a29 | **Écran des bâtiments (2026-09-27, G6)** | `BuildingsView` + `BuildingsScreen` (bouton « Bâtiments », smoke `--bld`) : les huit bâtiments, niveau, effet actuel et au niveau suivant (tirés des constantes qu'appliquent les systèmes), coût ; `BuildingSystem.UpgradeRefusal` dit pourquoi, et l'écran montre le refus même de l'action ; un cultivateur captif n'élève rien au domaine. |
| 43a30 | **Défi d'un rival et écran de bataille (2026-09-29, G6)** | L'événement « Défi d'un rival » (jusqu'ici sans effet) ouvre un `ChallengeSystem` : rivaux du rang du meilleur combattant libre, tirés d'une graine ; le clan choisit ses combattants (cultivateurs libres et connus), la bataille se joue sur la grille ; l'enjeu va au vainqueur, les morts tombent, les survivants gardent leurs blessures ; se dérober, ou se taire jusqu'à l'année suivante, coûte la face (réglages `challenges`, sauvegarde 2.18, chronique). `BattleView` + `BattleScreen`/`BattleCanvas` (grille à l'encre, actions et cibles, fin du tour, combat automatique, Pulsation de Qi du miroir ; bouton « Défi d'un rival » du domaine ; smoke `--bat`). Commentaires du bus d'événements remis à leur place. |
| 43a31 | **Longues parties automatiques (2026-09-29, piste 5)** | `BalanceRun` : une partie sans affichage, un clan passif ou mis au travail par un pilote (cultiver, récolter le Qi du mur de la Fondation, sinon miner), compte ce qui arrive au clan et aux puissances (coups, captures, coalitions, guerres, paix, absorptions, trahisons, chantages, vols, sondages surpris, défis, morts) ; `./Scripts/dev.sh balance` (BALANCE_SEEDS, BALANCE_YEARS, BALANCE_AUTOPILOT=0) imprime le tableau. |
| 43a32 | **Entretien du clan (2026-09-29, décision : « entretien par membre »)** | `UpkeepSystem` : chaque membre coûte chaque année (mortel 5, cultivateur 4 + 6 par royaume) ; une réserve de moins de 8 ans d'entretien réduit les naissances, une année de misère (impossible de payer) secoue les membres et n'en laisse naître que 10 % ; la mine n'a que 8 filons (+4 par niveau du bâtiment Mine), les mineurs au-delà rendent 10 %. Sauvegarde 2.19. Le domaine montre l'entretien et la misère. Payé à la fin de la phase des événements, une fois les revenus de l'année rentrés. Longues parties (8 graines × 150 ans) : ~250 membres au lieu de ~7 600, ~0,5 année de misère, ~3 900 pierres, ~15 cultivateurs. |
| 43a33 | **Convoitise (2026-09-29, décision : « la richesse attire »)** | Une puissance avide (agressive, expansionniste ; manipulatrice ou marchande moins ; isolationniste jamais), plus forte que le clan, convoite un trésor d'au moins 6 000 pierres (tentation croissante jusqu'à 30 000) et exige 30 % « pour sa protection » (`DemandKind.Protection`, une exigence par an avec le chantage). Payée, elle se tient tranquille 5 ans ; refusée — ou sans réponse — elle fait la guerre au clan (`OnExtortionRefused` → `WarSystem`). Pas d'exigence d'une puissance en confrontation ou déjà en guerre, pas de second front ; le clan se souvient de l'extorsion (`clanWatch.extortion`). Diplomatie et chronique. Longues parties, clan muet : ~2,5 extorsions et guerres en 150 ans. |
| 43a34 | **Pilote actif et réglages (2026-09-29, décision : « pilote actif »)** | `BalanceRun.Act` : le pilote paie une exigence s'il garde deux ans d'entretien (sinon refuse), relève les défis de son rang avec ses meilleurs combattants, demande la paix après deux ans de guerre, plante une Graine de Sceau dans un jeune mortel examiné, cherche un pacte de non-agression tous les 5 ans (3 traités au plus). Réglages : trahison de base 3 % → 1 %/an (le soupçon et la force en restent les moteurs) ; défi « dans les règles » : le tombé cède, grièvement blessé, sauf un coup mal retenu (`challenges.deathChance` 25 %, interprétation). Longues parties (10 × 150 ans) : ~4 trahisons au lieu de ~22, ~8 défis pour ~3,6 morts, ~0,1 guerre du clan, ~7 années de misère, ~257 membres. |
| 43a35 | **La chasse au Dao mûr, un complot (2026-09-29)** | `DaoHuntSystem` remplace le tirage annuel : seules les puissances d'au moins le Manoir Pourpre chassent (jamais un partenaire de traité) ; la rumeur d'un Dao mûr atteint un chasseur par an au plus (×2 si le membre voyage, ×0,2 en **réclusion**, nouvelle tâche qui ne fait rien d'autre) ; qui sait frappe l'année suivante ou plus tard (50 %/an) ; gardien du Manoir Pourpre, patrouilles, Formation protectrice, allié de défense et réclusion réduisent la réussite ; un chasseur repoussé est nommé dans la chronique, retenu par le registre de méfiance, et doit tout réapprendre. Ce que savent les chasseurs reste caché (sauvegarde 2.20) ; le domaine signale « Dao mûr : une proie ». Longues parties, pilote prudent : ~1,3 Dao dévoré au lieu de ~5,5. |
| 43a36 | **La lignée cultivatrice (2026-09-29)** | Aux mariages annuels, un cultivateur cherche d'abord un cultivateur ; le clan peut **unir** deux de ses membres (`MarriageSystem.Arrange`, refus : parenté, âge, captivité…) et **chercher au dehors** un conjoint cultivateur (300 pierres payées quel que soit le résultat ; 30 % + 10 % par royaume ; un cultivateur errant de la Culture du Qi, examiné, lié à aucune puissance). L'arbre généalogique montre les « Mariages de la lignée ». Le fondateur « Kin » est le frère du patriarche (interprétation de clan.json) : leurs aïeux sont retenus pour la parenté, sans fiche. Le pilote unit ses cultivateurs et envoie des cultivateurs à la mine quand les caisses baissent. Une seule règle dit qui peut s'unir ; les fiches sont indexées par identifiant (les vérifications de parenté restent rapides). Longues parties (20 × 150 ans) : ~47 cultivateurs au lieu de ~7, aucune défaite, ~0,1 année de misère, ~342 membres, ~0,3 Dao dévoré. |
| 43a37 | **Le défi à mort (2026-09-29)** | Une puissance qui hait le clan (relation ≤ −50 ou soupçon ≥ 70) lance, une fois sur deux, un défi **à mort** : qui tombe meurt, et se dérober coûte trois fois la face (`challenges.deathGrudgeRelation`, `deathGrudgeSuspicion`, `deathChallengeChance`, `deathDeclineFactor` ; `Challenge.ToTheDeath`, sauvegardé). L'écran de bataille le dit ; le pilote ne l'accepte que s'il a l'avantage. Le défi à mort peut laisser deviner une rancune cachée : la haine se montre par l'acte, jamais par un chiffre (D7), comme la guerre déclarée sur soupçon. |
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
