# 🚧 NOT_DONE.md — Ce qui reste à faire

> **Mis à jour à chaque étape du projet.** Dernière mise à jour : 2026-09-25 (L4 : lignées, fondations, Manoir Pourpre).
> Priorités : 🔴 Bloquant · 🟠 Haute · 🟡 Moyenne · 🟢 Basse

---

## ✅ Infra & compilation (historique Unity, caduc depuis la migration Godot)

| # | Tâche | Priorité | Détails |
|---|---|---|---|
| 1 | ~~Bootstrapper le projet Unity~~ | ✅ | Fait 2026-04-15. Projet temporaire créé dans `/tmp/` (Universal 2D, Unity 6.4.2f1), fusion `ProjectSettings/` + `Packages/manifest.json` dans `Mirror-Legacy/`, Newtonsoft.Json ajouté au manifest. Unity a scanné et généré 39 `.meta` pour les scripts. Compile sans erreur côté code projet (seulement 2 erreurs ShaderGraph dans le package cache, bug connu Unity 6.4, non bloquant). |
| 2 | ~~Corriger erreurs de compilation~~ | ✅ | Fait 2026-04-15. WL-001 à WL-005 résolus : `RootElement→Affinity`, `ConsumeMirrorPower→ConsumePower`, `Element.Ice` remplacé par `Darkness/Light`, `TriggerYearlyEvent/ProcessYearlyFactionAI` passées en public + appel test corrigé. Sera re-vérifiable via Unity Test Runner après bootstrap (tâche 1). |
| 3 | ~~Créer l'asmdef~~ | ✅ | Fait 2026-04-15 (commit `89d9e32`). — `Assets/_Project/Scripts/MirrorChronicles.Runtime.asmdef`. Séparer un `MirrorChronicles.Editor.asmdef` pour les custom editors à venir et un `MirrorChronicles.Tests.asmdef` pour les tests NUnit (avec references `nunit.framework.dll` + `UnityEngine.TestRunner` + `UnityEditor.TestRunner`). |
| 4 | ~~Créer la scène `ClanDomain.unity`~~ | ✅ | Fait 2026-04-15 (commit `89d9e32`). — Scène principale avec GameObject `[Systems]` portant tous les Singletons (GameManager, TimeManager, SaveSystem, ClanManager, BloodRegistry, tous les Characters systems, MirrorSystem, DeductionEngine, FactionManager, MarriageSystem, AllianceSystem, ResourceManager, TaskAssignmentSystem, EventManager). |
| 5 | ~~Passer à Newtonsoft.Json~~ | ✅ | Fait 2026-04-15 (commit `89d9e32`, WL-006). — `JsonUtility` ne sérialise pas les auto-properties `{ get; set; }` → `CharacterData` ne se sauvegarde pas. Installer `com.unity.nuget.newtonsoft-json` et remplacer `JsonUtility.ToJson/FromJson` dans `SaveSystem.cs`. |
| 6 | ~~Réinstaller l'éditeur Unity 6000.4.2f1~~ | ✅ | Caduc 2026-09-25 : le jeu passe sous Godot 4.7.2 .NET (installé dans `~/Godot`). Les tests qui dépendaient d'Unity (`CanMarry`, stabilité mentale, génétique, simulations) sont réécrits dans Core en G1. |

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
| 68 | **Optimisation** | 🟢 | Profileur Godot avec 100+ membres dans BloodRegistry et 10+ unités en combat. Object Pool pour VFX. |
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
| 102 | ~~MVC strict pour UI~~ | ✅ | Fait 2026-09-25 (G4) : les écrans lient des modèles sans moteur et testés (`src/Core/Presentation`). |

---

## 📜 Restructuration selon le lore (`LORE.md`, décidée le 2026-09-24)

> Règles : structure fidèle + noms renommés (D1), **rien de simplifié** (D2), orifice héréditaire (D3), **infinité de possibilités** (D4). Chaque phase : graphify → TDD (RED/GREEN) → revue ECC → commit.

| # | Phase | Priorité | Détails |
|---|---|---|---|
| L0 | ~~Bible du monde + lexique~~ | ✅ | Fait et validé le 2026-09-24 : `LORE.md` (lore complet + passe du wiki), lexique validé (Trois Profondeurs : Qingxuan, Douxuan, Tongxuan), miroir comme second axe de progression, bac à sable dynastique (Annales, grande fin optionnelle, deux défaites), mortels 60-80 ans. |
| L1 | ~~Échelle de puissance multi-voies, sous-niveaux, états du Noyau d'Or~~ | ✅ | Fait 2026-09-25 (branche `feature/l1-power-ladder`) : modèle multi-voies, `PowerLadder`, `RankCatalog`, `BreakthroughRules`, branchement (cultivation, percées en phase Percée, vieillissement, sauvegardes, interface). 113 tests purs verts ; intégration Unity non compilée (#6). |
| L2 | ~~Orifice spirituel héréditaire~~ | ✅ | Fait 2026-09-25 (branche `feature/l2-spiritual-orifice`) : `SpiritualOrificeRules` (D3 : 0,3 % / 35 % / 50 %, détection dès l'Œil du Sommet, mortels 60-80 ans, Graines de Sceau 2 + éclats, normalisation des sauvegardes), `TaskRules` (mortels : mine, patrouille, diplomatie, repos), examen du clan à chaque phase Héritage, vieillissement sur la durée de vie individuelle, blessures du Dao persistantes après percée (`DaoWounds`), `MirrorSystem.GrantTalismanSeed` (40 Puissance). 164 tests purs verts ; intégration Unity non compilée (#6). |
| L2b | **Suites de L2** | 🟡 | Bouton d'interface pour offrir une Graine de Sceau (l'API existe) ; taux D3 dans `BalanceConfig` (asset aujourd'hui lu par personne) ; perception du miroir pour examiner un orifice sans cultivateur confirmé et Qi de talisman *Prolonger la vie* (+40 ans) avec l'axe du miroir (L6) ; autres blessures persistantes (WL-007, seules les blessures du Dao sont enregistrées). |
| G0 | ~~Migration Godot — outillage~~ | ✅ | Fait 2026-09-25 : Godot 4.7.2 .NET dans `~/Godot` (somme SHA-512 vérifiée), solution `MirrorLegacy.sln` (`src/Core`, `tests/Core.Tests`, `game/`), 19 fichiers purs et 7 fichiers de tests déplacés avec leur historique, `Scripts/dev.sh` (build, test, smoke headless). 164 tests verts via `dotnet test`. |
| G1 | ~~Migration Godot — simulation Core~~ | ✅ | Fait 2026-09-25 (G1a-G1e, TDD) : les 30 systèmes Unity portés en classes C# sans moteur sur un `GameContext` (bus par session, journal, hasard à graine, horloge) ; `GameSession` câble tout dans un ordre fixe et joue les phases ; `ClanManager.Kill` seule porte de la mort ; sauvegarde v2 complète (WL-010) et compatible v1 ; simulations de 100 ans sur 5 graines. 383 tests verts. Correctifs en route : karma, Forge/Bibliothèque/Conseil/Formation, espionnage dédoublé, royaume de déduction invalide, événements d'histoire écrasés, héritage des étrangers. |
| B1 | **Équilibrage : démographie** | 🟠 | Première simulation headless (G1e) : 600 à 1 500 membres vivants après un siècle (mariage 30 %/an, naissances 25 %/an, aucun frein). Ajouter une pression : capacité du domaine, mortalité infantile, branches cadettes qui essaiment, fertilité décroissante. |
| B2 | **Équilibrage : économie** | 🟡 | 300 000 à 800 000 pierres après un siècle : chaque mortel mine et rien ne coûte chaque année. Ajouter l'entretien du domaine, les ressources de cultivation, le coût des techniques. |
| B3 | **Équilibrage : générations** | 🟡 | 1 à 3 générations par siècle (un patriarche de Fondation vit 300 ans) : la victoire « 10 générations » est hors de portée ; à reprendre avec les fins dynastiques (L6, §11.9). |
| G2 | ~~Migration Godot — données JSON~~ | ✅ | Fait 2026-09-25 : `game/data/*.json` (clan, noms, équilibrage D3, factions, événements, histoire ; textes en français), `GameContentLoader` validé (erreur nommant le fichier), plus aucune table dans le code, `FactionData.FamilyName`, garde contre les noms du roman. |
| G3 | ~~Migration Godot — logique du combat~~ | ✅ | Fait 2026-09-25 (G3a-G3b) : grille à graine, A*, unités, 6 actions, initiative, 5 IA enfin appelées, `Battle` (fuite, retrait après 100 tours, suites via `ClanManager.Kill` et blessures), Pulsation de Qi du miroir. Rendu à faire (G6). |
| G4 | ~~Migration Godot — couche Godot~~ | ✅ | Fait 2026-09-25 : autoload `GameRoot` (contenu, reprise, sauvegarde annuelle), écran `ClanDomain` (en-tête, membres et tâches, choix d'histoire, chronique), fumée et capture d'écran ; règles d'âge des tâches et prénoms uniques. |
| G5 | ~~Migration Godot — nettoyage~~ | ✅ | Fait 2026-09-25 : arbre Unity supprimé (historique dans git), CI `dotnet test` + Godot headless, `MEMORY.md`/README réécrits, graphify reconstruit, revue ECC des G2-G4 vérifiée, fusion dans `main`. |
| G6 | **Écrans à venir** | 🟠 | Interventions du miroir (dont la Graine de Sceau, L2b), bâtiments, diplomatie, déduction (choix des fragments, technique obtenue), bibliothèque du clan (apprendre un art, détails d'une technique et de son Qi), bataille (grille, actions, IA), arbre généalogique, nouvelle partie et emplacements de sauvegarde, thème Shuimo. Si les grilles dépassent 10×10 : calculer les cases atteignables par un seul Dijkstra borné, car l'IA lance aujourd'hui un A* par case candidate (revue G2-G4). |
| L3 | ~~Techniques graduées~~ | ✅ | Fait 2026-09-25 (L3a-L3e, branche `feature/l3-graded-techniques`) : `techniques.json` (26 méthodes du §2.4 + 3 du lore) et `qi.json`, `TechniqueRules` (vitesse et plafond par grade, secret du Manoir Pourpre, Qi requis), `TechniqueLibrary`, stock et récolte de Qi, entrée en Culture du Qi au prix d'une portion, déduction graduée et nommée par les données, contre du *Veilleur du Sentier* et arts de déplacement en combat, méthode et Qi à l'écran, sauvegarde 2.1. Interprétations : `LORE.md` §2.5. 610 tests verts. |
| L3b | **Suites de L3** | 🟡 | Boutiques des techniques communes (avec L5-L6) ; Qi de la fondation immortelle (L4) ; raffinage du *Qi Profond du Bélier Variant* par un démon (L6) ; *Sutra de la respiration du Yin Suprême* avec le premier éclat (L6) ; techniques perdues, volées, transmises et réécrites par le miroir (§11.5-11.6) ; mêmes règles pour les cultivateurs des factions (P1, §11). |
| B4 | **Équilibrage : Qi** | 🟡 | Siècle headless (L3) : sans récolteur, tous les enfants attendent au 6e chakra une fois les deux portions de départ absorbées ; avec deux récolteurs permanents, 140 à 190 portions dorment en réserve. À reprendre avec le Qi par région (L5-L6) : richesse de la région, lignées actives, neutres ou néfastes (§2.5). |
| L4 | ~~Voies, lignées, fondations, capacités, Fruitions~~ | ✅ | Fait 2026-09-25 (L4.1-L4.5, branche `feature/l4-paths-lineages-fruitions`) : `fruitions.json` (63 lignées du §6, 334 capacités typées ou non révélées), état du monde par partie (statuts non précisés tirés de la graine), fondation formée depuis le Qi de sa méthode (liens du wiki) au prix d'une portion, Partenaires Dao et leur consommation, Cœur Dao (tempérament héréditaire), percée du Manoir Pourpre en 4 épreuves avec retraites, capacités divines 2 à 5 (cultivation alignée, ressources, Greffe du Dao, Seuil d'Immortalité), écran. Les voies et sous-voies existent depuis L1 (conversions : L7). Interprétations : `LORE.md` §5.3, §5.4, §6. L4.6 (2026-09-26) : décisions de l'utilisateur (Partenaire dévoré mort, échec de Manifestation mortel, donneur de Greffe sans cultivation et 1 à 5 ans à vivre), provenance et champs interprétés dans les données (`dev.sh gaps`), valeurs d'interprétation dans `balance.json`, savoir modulaire (`World/KnowledgeBase`). 716 tests verts. |
| L4c | **Suites de L4** | 🟡 | Imagerie et Mandat de Vie ; lien à un trésor, emprunt de lumière ; « Dao mûr » convoité par les plus forts ; corps inhumains ; phénomènes de fondation, à la mort et à l'échec (L6) ; révéler les capacités inconnues (miroir, ruines, échanges) ; mêmes règles pour les cultivateurs des factions (P1) ; migrer vers `balance.json` les constantes d'interprétation de L1-L2 encore dans le code (`BreakthroughRules`, `PowerLadder` : chances des épreuves des chakras et du mur, dissolution ; `SpiritualOrificeRules`) ; brancher `KnowledgeBase.Revealed` sur la chronique. |
| L4b | **Routes du Noyau d'Or** (§5.9) | 🟠 | R1-R9 (Réalisation, Surplus ×2, Intercalaire ×3, Noyau d'Or sans position, changements de position, réincarnation), R18-R19 (Main Gauche vraie / fausse). À prévoir : capacités d'une autre lignée pour l'Intercalaire, poids des capacités superficielles, greffées ou consommées (« chemin inévitablement périlleux »), permission du détenteur, Démon d'Essence Métallique. |
| L5 | **Carte et factions renommées** | 🟠 | `RegionDefinition`, 3 sectes + portes + familles en `FactionTemplate`, nouvelle carte Shuimo, `WorldMapUI` par régions. **Nécessite la couche Godot (G4).** |
| L6 | **Chronologie, monde vivant, rejouabilité** | 🟡 | Introduction an 0, faits historiques, statuts initiaux des Fruitions, phénomènes régionaux, graine de monde, conditions de victoire. |
| L7 | **Autres voies de cultivation** (§3, §5.9 B) | 🟡 | R10-R17 et R20 : Nature Spirituelle, Démon Pourpre-Or, Démon Embryon Céleste, bouddhisme ancien et moderne, Dao Divin, Dao Démoniaque (membres non humains), chamanisme, Voie Impériale ; conversions entre voies. |

---

## 📝 Liste de vérification pour passer à Phase 2

- [x] Tâches 1-5 terminées (caduques : migration Godot, phase G)
- [ ] Tâches 10-15 terminées (Phase 1 jouable bout-en-bout)
- [x] Simulation de 100 ans sans erreur (`SimulationTests`, 2026-09-25)
- [x] Sauvegarde annuelle rechargeable (`GameRoot`, `SaveSerializerTests`)
- [x] Naissances, morts et percées observées (`SimulationTests`)

---

*Voir `DONE.md` pour la liste de ce qui est déjà fait.*
*Voir `WORKED_LESSON.md` pour les bugs à corriger en priorité.*
