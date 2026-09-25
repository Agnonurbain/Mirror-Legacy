# 🚧 NOT_DONE.md — Ce qui reste à faire

> **Mis à jour à chaque étape du projet.** Dernière mise à jour : 2026-09-24 (mariages annuels + règle de consanguinité, tâches Phase 1 cochées).
> Priorités : 🔴 Bloquant · 🟠 Haute · 🟡 Moyenne · 🟢 Basse

---

## 🔴 BLOQUANT — Infra Unity & Compilation

| # | Tâche | Priorité | Détails |
|---|---|---|---|
| 1 | ~~Bootstrapper le projet Unity~~ | ✅ | Fait 2026-04-15. Projet temporaire créé dans `/tmp/` (Universal 2D, Unity 6.4.2f1), fusion `ProjectSettings/` + `Packages/manifest.json` dans `Mirror-Legacy/`, Newtonsoft.Json ajouté au manifest. Unity a scanné et généré 39 `.meta` pour les scripts. Compile sans erreur côté code projet (seulement 2 erreurs ShaderGraph dans le package cache, bug connu Unity 6.4, non bloquant). |
| 2 | ~~Corriger erreurs de compilation~~ | ✅ | Fait 2026-04-15. WL-001 à WL-005 résolus : `RootElement→Affinity`, `ConsumeMirrorPower→ConsumePower`, `Element.Ice` remplacé par `Darkness/Light`, `TriggerYearlyEvent/ProcessYearlyFactionAI` passées en public + appel test corrigé. Sera re-vérifiable via Unity Test Runner après bootstrap (tâche 1). |
| 3 | ~~Créer l'asmdef~~ | ✅ | Fait 2026-04-15 (commit `89d9e32`). — `Assets/_Project/Scripts/MirrorChronicles.Runtime.asmdef`. Séparer un `MirrorChronicles.Editor.asmdef` pour les custom editors à venir et un `MirrorChronicles.Tests.asmdef` pour les tests NUnit (avec references `nunit.framework.dll` + `UnityEngine.TestRunner` + `UnityEditor.TestRunner`). |
| 4 | ~~Créer la scène `ClanDomain.unity`~~ | ✅ | Fait 2026-04-15 (commit `89d9e32`). — Scène principale avec GameObject `[Systems]` portant tous les Singletons (GameManager, TimeManager, SaveSystem, ClanManager, BloodRegistry, tous les Characters systems, MirrorSystem, DeductionEngine, FactionManager, MarriageSystem, AllianceSystem, ResourceManager, TaskAssignmentSystem, EventManager). |
| 5 | ~~Passer à Newtonsoft.Json~~ | ✅ | Fait 2026-04-15 (commit `89d9e32`, WL-006). — `JsonUtility` ne sérialise pas les auto-properties `{ get; set; }` → `CharacterData` ne se sauvegarde pas. Installer `com.unity.nuget.newtonsoft-json` et remplacer `JsonUtility.ToJson/FromJson` dans `SaveSystem.cs`. |
| 6 | **Réinstaller l'éditeur Unity 6000.4.2f1** | 🔴 | Absent de la machine depuis le déplacement du repo vers `~/Games/Mirror-Legacy` : `unity-claude.sh` ne compile plus et ne lance plus aucun test. Ensuite : `RunEditModeTests` (valider les 2 tests `CanMarry`) et `RunPlayModeTests`. |

---

## 🟠 Priorité HAUTE — Finir Phase 1

| # | Tâche | Priorité | Détails |
|---|---|---|---|
| 10 | ~~Tests Edit Mode basiques~~ | ✅ | Fait 2026-04-17 (commit `93e18cf`). — NUnit tests pour : `GeneticSystem.GenerateSpiritualRoot` (bornes, moyenne, mutations), `BreakthroughSystem.CalculateSuccessRate` (modificateurs), `MentalStabilitySystem.ApplyModifier` (clamp 0-100). Créer `Assets/_Project/Tests/EditMode/`. |
| 11 | ~~Test Play Mode de la simulation 10 ans~~ | ✅ | Fait 2026-04-17 (commit `93e18cf`). — Convertir `GameSimulationTest` en vrai test Play Mode : spawn Systems, advance 10 years via TimeManager, assert que le clan a survécu, que des morts ont eu lieu, que Spirit Stones ont été générés. |
| 12 | ~~UI placeholder Phase 1~~ | ✅ | Fait 2026-04-15 (commit `23503d2`). — Minimal Canvas avec : (a) bouton "End Turn" qui call `TimeManager.Instance.AdvancePhase()`, (b) affichage texte CurrentYear + CurrentPhase, (c) liste des LivingMembers avec Nom / Age / Realm / Task / Stability. Utiliser TextMeshPro. |
| 13 | ~~TaskAssignment UI~~ | ✅ | Fait 2026-04-17 (commit `93e18cf`). — Pour chaque membre, un dropdown avec les TaskType valides (filtrés selon Realm). Appeler `TaskAssignmentSystem.AssignTask`. |
| 14 | ~~Child birth~~ | ✅ | Fait 2026-04-17 (commit `93e18cf`). — Pas de logique de procréation. Ajouter dans `ClanManager` une méthode `GenerateChild(father, mother)` qui appelle `GeneticSystem.GenerateSpiritualRoot/Affinity` et créé un nouveau `CharacterData`. Déclenchement : event scénarisé ou annuel aléatoire si couple marié. |
| 15 | ~~Compléter tâches manquantes~~ | ✅ | Fait 2026-04-17 (commit `93e18cf`). — 4 tâches dans `TaskAssignmentSystem` : Study (+chance Deduction), Teaching (mentor buff pour élève), Diplomacy (relation +5/an sur faction ciblée), Espionage (chance vol fragment mais risque capture). |
| 16 | ~~Mariages annuels~~ | ✅ | Fait 2026-09-24. `MarriageMatchmaker` + `MarriageSystem.ProcessAnnualMarriages` (phase Events, avant les naissances) ; conjoints extérieurs ajoutés au clan. Sans cela la lignée s'éteignait après le couple fondateur (WL-012). Intégration à valider en PlayMode (#6). |
| 17 | ~~Règle de consanguinité ≤ 3 générations~~ | ✅ | Fait 2026-09-24. `KinshipRules` utilisé par `CanMarry` (WL-011). |

---

## 🟡 Priorité MOYENNE — Finir Phase 2 (Combat & Events)

| # | Tâche | Priorité | Détails |
|---|---|---|---|
| 20 | ~~TechniqueAction~~ | ✅ | Fait 2026-04-18. ICombatAction avec Qi cost, range variable, dégâts/heal selon TechniqueType. +25% affinity bonus, +20% terrain Water. KnownTechniqueIDs ajouté sur CharacterData. Range ajouté sur TechniqueData. |
| 21 | ~~ItemAction + FleeAction~~ | ✅ | Fait 2026-04-18. ItemAction (HealingPill/QiRestorationPill), FleeAction (Agility roll vs nearest enemy). ItemData POCO créé. Heal() + RestoreQi() ajoutés sur CombatUnit. |
| 22 | ~~3 stratégies IA~~ | ✅ | Fait 2026-04-18. StrategicStrategy (cible supports, terrain), BerserkerStrategy (nearest + all Qi in techniques), CautiousStrategy (flee <40%, defend, retreat). |
| 23 | ~~Terrain procédural~~ | ✅ | Fait 2026-04-18. InitializeGrid avec clusters (Forest, Mountain, Water) par croissance voisinage + ConcentratedQi spots (1-3). |
| 24 | ~~Pathfinding A*~~ | ✅ | Fait 2026-04-18. `GridSystem.FindPath(start, goal)` avec A*, coûts terrain, skip occupied cells. Retourne chemin sans start, avec goal. |
| 25 | ~~Table d'événements~~ | ✅ | Fait 2026-04-18. RandomEventData ScriptableObject avec Weight, MinYear, MinPatriarchRealm. EventManager refactorisé avec weighted random + 11 event types. Fallback BuildDefaultEventTable() si pas de SO assignés. |
| 26 | ~~Événements scénarisés~~ | ✅ | Fait 2026-04-18. StoryEventData SO + StoryEventManager. 6 triggers : FirstFoundation, FirstGoldenCore, PatriarchBetrayal, FirstAscension, ClanExtinctionThreat. Choix avec StoryOutcome (MS, stones, relation). Defaults hardcodés si pas de SO. |
| 27 | ~~Résolution UI des events~~ | ✅ | Fait 2026-04-18. EventResolutionUI modal panel + ChoiceButtonTemplate. Bloque EndTurn tant que PendingEvent actif. UIBuilder génère le panel dans la scène. |
| 28 | ~~Intervention Divine combat~~ | ✅ | Fait 2026-04-18. `MirrorSystem.UseQiPulse(CombatUnit)` : +30% MaxQi + 15% MaxVit sur la cible. Coût 10 MirrorPower. UI button à créer. |
| 29 | ~~Intervention Divine percée~~ | ✅ | Fait 2026-04-18. `BreakthroughSystem.ActivateAncestralShield()` consume 25 MirrorPower, flag +30% succès sur prochain AttemptBreakthrough. UI séquence à créer. |

---

## 🟡 Priorité MOYENNE — Finir Phase 3 (Monde & Diplomatie)

| # | Tâche | Priorité | Détails |
|---|---|---|---|
| 40 | ~~FactionData en ScriptableObjects~~ | ✅ | Fait 2026-04-18. FactionTemplate SO + FactionManager accepte SO ou fallback 8 factions hardcodées. IA par personnalité (Aggressive, Merchant, Manipulative, Expansionist, Isolationist). |
| 41 | ~~EspionageSystem~~ | ✅ | Fait 2026-04-18. `AttemptEspionage(spy, target)` : succès = SpiritualRoot×0.5%−Power/100 clampé [5%,60%]. Succès = fragment volé. Échec = -20 relation, -10 MS, 10% chance combat. Retourne `EspionageResult` struct. |
| 42 | ~~WorldMap scene~~ | ✅ | Fait 2026-04-18. WorldMapUI controller + ClaudeWorldMapBuilder. 8 factions en cercle, panneau diplomatie (tribute, pact, spy, war). Navigation ClanDomain↔WorldMap. |
| 43 | ~~BuildingSystem~~ | ✅ | Fait 2026-04-18. 8 bâtiments × 5 niveaux. Coûts croissants (200→8000). Bonus passifs annuels (XP, herbs, stones, MS). MeditationPagoda nécessite PurpleMansion, ProtectiveFormation nécessite Foundation. |
| 44 | ~~4 ressources additionnelles~~ | ✅ | Fait 2026-04-18. ResourceManager étendu : MedicinalHerbs (50 init), SpiritualOres (30), Prestige (10), TechniqueFragments (0). Add/Consume pour chaque. |
| 45 | ~~Succession Patriarche~~ | ✅ | Fait 2026-04-18. `PatriarchID` sur ClanManager, `ElectNewPatriarch()` auto à la mort (tri Realm→Age→SpiritualRoot). `GetPatriarch()` helper. |
| 46 | ~~Karma du Clan~~ | ✅ | Fait 2026-04-18. ClanKarmaSystem : compteur générationnel (incrémenté au changement de patriarche), +2% cultivation/gen, bonus XP (+5 à 5 gen, +10 à 10 gen), unlock technique ancestrale à 10 gen. |

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
| 66 | ~~Condition de victoire~~ | ✅ | Fait 2026-04-18. VictoryConditionSystem : victoire si 10 gen + 1 ascension, défaite si clan éteint. Transition GameOver. |
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
| 100 | ~~Object Pool~~ | ✅ | Fait 2026-04-18. Generic ObjectPool avec RegisterPrefab, Get, Return (immédiat ou delayed). Singleton. |
| 101 | ~~Memento (save snapshot)~~ | ✅ | Fait 2026-04-18. SaveSystem étendu avec SaveToSlot(1-3), LoadFromSlot, SlotExists. BuildGameData extrait en méthode partagée. |
| 102 | **MVC strict pour UI** | 🟡 | Séparation Controller UI / ViewBinder / Model (ScriptableObject ou POCO). Pour l'instant tout est dans les MonoBehaviours. |

---

## 📜 Restructuration selon le lore (`LORE.md`, décidée le 2026-09-24)

> Règles : structure fidèle + noms renommés (D1), **rien de simplifié** (D2), orifice héréditaire (D3), **infinité de possibilités** (D4). Chaque phase : graphify → TDD (RED/GREEN) → revue ECC → commit.

| # | Phase | Priorité | Détails |
|---|---|---|---|
| L0 | ~~Bible du monde + lexique~~ | ✅ | Fait et validé le 2026-09-24 : `LORE.md` (lore complet + passe du wiki), lexique validé (Trois Profondeurs : Qingxuan, Douxuan, Tongxuan), miroir comme second axe de progression, bac à sable dynastique (Annales, grande fin optionnelle, deux défaites), mortels 60-80 ans. |
| L1 | ~~Échelle de puissance multi-voies, sous-niveaux, états du Noyau d'Or~~ | ✅ | Fait 2026-09-25 (branche `feature/l1-power-ladder`) : modèle multi-voies, `PowerLadder`, `RankCatalog`, `BreakthroughRules`, branchement (cultivation, percées en phase Percée, vieillissement, sauvegardes, interface). 113 tests purs verts ; intégration Unity non compilée (#6). |
| L2 | ~~Orifice spirituel héréditaire~~ | ✅ | Fait 2026-09-25 (branche `feature/l2-spiritual-orifice`) : `SpiritualOrificeRules` (D3 : 0,3 % / 35 % / 50 %, détection dès l'Œil du Sommet, mortels 60-80 ans, Graines de Sceau 2 + éclats, normalisation des sauvegardes), `TaskRules` (mortels : mine, patrouille, diplomatie, repos), examen du clan à chaque phase Héritage, vieillissement sur la durée de vie individuelle, blessures du Dao persistantes après percée (`DaoWounds`), `MirrorSystem.GrantTalismanSeed` (40 Puissance). 164 tests purs verts ; intégration Unity non compilée (#6). |
| L2b | **Suites de L2** | 🟡 | Bouton d'interface pour offrir une Graine de Sceau (l'API existe) ; taux D3 dans `BalanceConfig` (asset aujourd'hui lu par personne) ; perception du miroir pour examiner un orifice sans cultivateur confirmé et Qi de talisman *Prolonger la vie* (+40 ans) avec l'axe du miroir (L6) ; autres blessures persistantes (WL-007, seules les blessures du Dao sont enregistrées). |
| L3 | **Techniques graduées** | 🟠 | Grade 1-7+, catégorie commune/ancestrale/secrète, type, Qi requis, secret du Manoir Pourpre ; 26 méthodes de Qi en données ; déduction du miroir → grade. |
| L4 | **Voies, lignées, fondations, capacités, Fruitions** | 🟠 | 5 voies / 9 sous-voies ; ≈60 Fruitions en ScriptableObjects (emplacements non nommés inclus) ; Partenaires Dao ; 5 capacités divines typées ; percée du Manoir Pourpre en 4 épreuves ; positions Réalisation / Surplus / Intercalaire ; Démon d'Essence Métallique. |
| L4b | **Routes du Noyau d'Or** (§5.9) | 🟠 | R1-R9 (Réalisation, Surplus ×2, Intercalaire ×3, Noyau d'Or sans position, changements de position, réincarnation), R18-R19 (Main Gauche vraie / fausse). |
| L5 | **Carte et factions renommées** | 🟠 | `RegionDefinition`, 3 sectes + portes + familles en `FactionTemplate`, nouvelle carte Shuimo, `WorldMapUI` par régions. **Nécessite l'éditeur Unity (#6).** |
| L6 | **Chronologie, monde vivant, rejouabilité** | 🟡 | Introduction an 0, faits historiques, statuts initiaux des Fruitions, phénomènes régionaux, graine de monde, conditions de victoire. |
| L7 | **Autres voies de cultivation** (§3, §5.9 B) | 🟡 | R10-R17 et R20 : Nature Spirituelle, Démon Pourpre-Or, Démon Embryon Céleste, bouddhisme ancien et moderne, Dao Divin, Dao Démoniaque (membres non humains), chamanisme, Voie Impériale ; conversions entre voies. |

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
