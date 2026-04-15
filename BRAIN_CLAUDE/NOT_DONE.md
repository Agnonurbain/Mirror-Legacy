# 🚧 NOT_DONE.md — Ce qui reste à faire

> **Mis à jour à chaque étape du projet.** Dernière mise à jour : 2026-04-14 (audit initial après pivot web → Unity/C#).
> Priorités : 🔴 Bloquant · 🟠 Haute · 🟡 Moyenne · 🟢 Basse

---

## 🔴 BLOQUANT — Infra Unity & Compilation

| # | Tâche | Priorité | Détails |
|---|---|---|---|
| 1 | **Bootstrapper le projet Unity** | 🔴 | Créer `ProjectSettings/` (via Unity Hub : New Project → 2D Core Template → Unity 2022.3 LTS). Créer `Packages/manifest.json` avec les packages minimum (TextMeshPro, Input System, Newtonsoft.Json). Le code ne peut tourner sans ça. |
| 2 | ~~Corriger erreurs de compilation~~ | ✅ | Fait 2026-04-15. WL-001 à WL-005 résolus : `RootElement→Affinity`, `ConsumeMirrorPower→ConsumePower`, `Element.Ice` remplacé par `Darkness/Light`, `TriggerYearlyEvent/ProcessYearlyFactionAI` passées en public + appel test corrigé. Sera re-vérifiable via Unity Test Runner après bootstrap (tâche 1). |
| 3 | **Créer l'asmdef** | 🔴 | `Assets/_Project/Scripts/MirrorChronicles.Runtime.asmdef`. Séparer un `MirrorChronicles.Editor.asmdef` pour les custom editors à venir et un `MirrorChronicles.Tests.asmdef` pour les tests NUnit (avec references `nunit.framework.dll` + `UnityEngine.TestRunner` + `UnityEditor.TestRunner`). |
| 4 | **Créer la scène `ClanDomain.unity`** | 🔴 | Scène principale avec GameObject `[Systems]` portant tous les Singletons (GameManager, TimeManager, SaveSystem, ClanManager, BloodRegistry, tous les Characters systems, MirrorSystem, DeductionEngine, FactionManager, MarriageSystem, AllianceSystem, ResourceManager, TaskAssignmentSystem, EventManager). |
| 5 | **Passer à Newtonsoft.Json** | 🔴 | `JsonUtility` ne sérialise pas les auto-properties `{ get; set; }` → `CharacterData` ne se sauvegarde pas. Installer `com.unity.nuget.newtonsoft-json` et remplacer `JsonUtility.ToJson/FromJson` dans `SaveSystem.cs`. |

---

## 🟠 Priorité HAUTE — Finir Phase 1

| # | Tâche | Priorité | Détails |
|---|---|---|---|
| 10 | **Tests Edit Mode basiques** | 🟠 | NUnit tests pour : `GeneticSystem.GenerateSpiritualRoot` (bornes, moyenne, mutations), `BreakthroughSystem.CalculateSuccessRate` (modificateurs), `MentalStabilitySystem.ApplyModifier` (clamp 0-100). Créer `Assets/_Project/Tests/EditMode/`. |
| 11 | **Test Play Mode de la simulation 10 ans** | 🟠 | Convertir `GameSimulationTest` en vrai test Play Mode : spawn Systems, advance 10 years via TimeManager, assert que le clan a survécu, que des morts ont eu lieu, que Spirit Stones ont été générés. |
| 12 | **UI placeholder Phase 1** | 🟠 | Minimal Canvas avec : (a) bouton "End Turn" qui call `TimeManager.Instance.AdvancePhase()`, (b) affichage texte CurrentYear + CurrentPhase, (c) liste des LivingMembers avec Nom / Age / Realm / Task / Stability. Utiliser TextMeshPro. |
| 13 | **TaskAssignment UI** | 🟠 | Pour chaque membre, un dropdown avec les TaskType valides (filtrés selon Realm). Appeler `TaskAssignmentSystem.AssignTask`. |
| 14 | **Child birth** | 🟠 | Pas de logique de procréation. Ajouter dans `ClanManager` une méthode `GenerateChild(father, mother)` qui appelle `GeneticSystem.GenerateSpiritualRoot/Affinity` et créé un nouveau `CharacterData`. Déclenchement : event scénarisé ou annuel aléatoire si couple marié. |
| 15 | **Compléter tâches manquantes** | 🟠 | 4 tâches dans `TaskAssignmentSystem` : Study (+chance Deduction), Teaching (mentor buff pour élève), Diplomacy (relation +5/an sur faction ciblée), Espionage (chance vol fragment mais risque capture). |

---

## 🟡 Priorité MOYENNE — Finir Phase 2 (Combat & Events)

| # | Tâche | Priorité | Détails |
|---|---|---|---|
| 20 | **TechniqueAction** | 🟡 | Nouvelle `ICombatAction` : consomme Qi (`GetQiCost` > 0), range variable, effets selon `TechniqueData`. Liée aux techniques mémorisées de `CharacterData` (à ajouter comme `List<string> KnownTechniqueIDs`). |
| 21 | **ItemAction + FleeAction** | 🟡 | `ItemAction` : utilise une pilule (soin, Qi restore). `FleeAction` : jet Agility vs AgilityEnnemie, tour perdu si échec. |
| 22 | **3 stratégies IA manquantes** | 🟡 | `StrategicStrategy` (cible les supports d'abord, utilise terrain), `BerserkerStrategy` (plus proche, ignore défense, tout le Qi), `CautiousStrategy` (fuit si Vit < 40%, économise Qi). |
| 23 | **Terrain procédural** | 🟡 | `GridSystem.InitializeGrid` : génération avec mix de 5 terrains. Params : % forêt, % montagne, % eau, spots de ConcentratedQi (1-3). |
| 24 | **Pathfinding A*** | 🟡 | `MoveAction` ne tient pas compte des coûts de mouvement ni des obstacles. Implémenter A* dans `GridSystem.FindPath`. |
| 25 | **Table d'événements complète** | 🟡 | Passer `EventManager` d'un switch hardcodé à un `List<RandomEventData>` ScriptableObject avec poids. 10+ events : Monster Attack, Diplomatic Visit, Ruins Discovery, Genius Birth, Internal Betrayal, Natural Disaster, Wandering Merchant, Rival Challenge, Epidemic, Marriage Opportunity. |
| 26 | **Événements scénarisés** | 🟡 | `StoryEventData` SO déclenchés par conditions : First Foundation, First Golden Core, Patriarch Betrayal, 10 Generations Retrospective. |
| 27 | **Résolution UI des events** | 🟡 | Quand un event se déclenche, afficher une fenêtre de choix et bloquer `TimeManager.AdvancePhase` jusqu'à décision joueur. |
| 28 | **Intervention Divine en combat** | 🟡 | Bouton "Qi Pulse" (10 MirrorPower) pendant `PlayerTurn` — buff temporaire à un allié. |
| 29 | **Intervention Divine en percée** | 🟡 | Séquence animée de percée avec timer 5s et bouton "Ancestral Shield" (25 MirrorPower) qui ajoute +30% chance succès. |

---

## 🟡 Priorité MOYENNE — Finir Phase 3 (Monde & Diplomatie)

| # | Tâche | Priorité | Détails |
|---|---|---|---|
| 40 | **FactionData en ScriptableObjects** | 🟡 | Sortir les 3 factions hardcodées de `FactionManager.InitializeWorldFactions` vers des `.asset`. Créer 5-8 factions initiales (master prompt). |
| 41 | **EspionageSystem** | 🟡 | Nouvelle classe. Permet de tenter un vol de FragmentData depuis une faction. Chance succès = SpiritualRoot × 0.5 − FactionPowerLevel/100. Échec = relation -20, possible combat. |
| 42 | **WorldMap scene** | 🟡 | Scène `WorldMap.unity` avec carte peinte à l'encre. Factions positionnées sur territoires. Click sur une faction → panneau relation + actions diplomatiques. |
| 43 | **BuildingSystem** | 🟡 | 8 bâtiments × 5 niveaux (master prompt) : Training Hall, Forge, Library, Herb Garden, Mine, Council Room, Meditation Pagoda (Purple Mansion min), Protective Formation. Coût croissant en ressources. |
| 44 | **4 ressources additionnelles** | 🟡 | Compléter `ResourceManager` : Herbes Médicinales, Minerais Spirituels, Prestige (non-matériel), Fragments de Techniques. |
| 45 | **Succession Patriarche** | 🟡 | Quand le Patriarche meurt, `LegacySystem` doit choisir successeur automatiquement (membre le plus qualifié : Realm puis Age puis SpiritualRoot). Si plusieurs candidats équivalents → événement de vote/schisme. |
| 46 | **Karma du Clan** | 🟡 | Compteur de générations. Bonus passifs cumulatifs (+2% vitesse culture à 5 gen, +5% à 10 gen + techniques ancestrales). |

---

## 🟢 Priorité BASSE — Phase 4 (Polish & Art)

| # | Tâche | Priorité | Détails |
|---|---|---|---|
| 60 | **Direction artistique Shuimo** | 🟢 | Sprites parchemin, textures bronze, bordures nuages/dragons. 5 tranches d'âge × 2 genres = 10 sprites par archétype. |
| 61 | **UI thématique** | 🟢 | Remplacer toute l'UI placeholder par la version parchemin/bronze/encre. Transitions d'encre entre écrans. |
| 62 | **VFX percée** | 🟢 | Particle System avec explosion ascendante + flash + camera shake. |
| 63 | **VFX combat** | 🟢 | Traînées de Qi colorées par élément, shockwave à l'impact, Déviation de Qi = particules noires/rouges chaotiques. |
| 64 | **Audio complet** | 🟢 | Musique Guqin (domaine), Taiko+Dizi (combat), montée orchestrale (percée). SFX parchemin, bronze, goutte d'encre. |
| 65 | **Responsive UI mobile** | 🟢 | Canvas avec Reference Resolution + Scale With Screen Size. Testé 16:9 (PC) + 9:16 (mobile portrait) + 18:9. |
| 66 | **Condition de victoire** | 🟢 | Écran narratif quand le clan atteint 10 générations + 1 Ascension. Cinématique finale. |
| 67 | **Équilibrage complet** | 🟢 | Passes d'équilibrage après premiers playtest : courbes XP, taux de percée, drop rate fragments, prix bâtiments. |
| 68 | **Optimisation mobile** | 🟢 | Unity Profiler avec 100+ membres dans BloodRegistry et 10+ unités en combat. Object Pool pour VFX. |
| 69 | **Build Steam** | 🟢 | Config Steamworks SDK, achievements, cloud saves. |
| 70 | **Build iOS/Android** | 🟢 | Export, signing, store assets, monetization (à discuter : one-time vs F2P). |

---

## 🧹 Nettoyage

| # | Tâche | Priorité | Détails |
|---|---|---|---|
| 90 | ~~Archiver le portage web~~ | ✅ | Fait 2026-04-15. Déplacé vers `.archive/web-port/` via `git mv` (historique préservé). |
| 91 | ~~Réécrire README.md~~ | ✅ | Fait 2026-04-15. Nouveau README décrit le projet Unity + pointe vers `BRAIN_CLAUDE/`. |
| 92 | **Ignorer `BRAIN_QWEN/`** | 🟢 | Dossier d'un autre projet (HOMECI). Ne pas toucher. |
| 93 | ~~.gitignore Unity~~ | ✅ | Fait 2026-04-15. Remplacé par gitignore Unity standard (Library/, Temp/, *.csproj, *.sln, etc.) + conservation node_modules pour l'archive web. |

---

## 📐 Design patterns non encore implémentés

| # | Tâche | Priorité | Détails |
|---|---|---|---|
| 100 | **Object Pool** | 🟡 | Pour VFX combat, projectiles, éléments UI dynamiques. Évite les allocations en Update(). |
| 101 | **Memento (save snapshot)** | 🟡 | Extension du SaveSystem pour snapshot d'état complet — permet mode Casual avec 3 slots + save manuelle. |
| 102 | **MVC strict pour UI** | 🟡 | Séparation Controller UI / ViewBinder / Model (ScriptableObject ou POCO). Pour l'instant tout est dans les MonoBehaviours. |

---

## 📝 Liste de vérification pour passer à Phase 2

- [ ] Tâches 1-5 terminées (infra Unity + compile clean)
- [ ] Tâches 10-15 terminées (Phase 1 jouable bout-en-bout)
- [ ] Simulation 10 ans passe sans erreur en Play Mode
- [ ] Une save existe sur disque après 1 an et est rechargeable
- [ ] Au moins 2 naissances + 1 mort + 1 percée observées dans une partie test

---

*Voir `DONE.md` pour la liste de ce qui est déjà fait.*
*Voir `WORKED_LESSON.md` pour les bugs à corriger en priorité.*
