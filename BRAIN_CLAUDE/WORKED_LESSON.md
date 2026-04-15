# 📚 WORKED_LESSON.md — Bugs identifiés & Leçons apprises

> **Mis à jour à chaque session.** Dernière mise à jour : 2026-04-14 (audit initial du code C# existant).
> **Objectif :** Capitaliser sur les erreurs passées pour ne pas les répéter.

---

## 📖 Comment utiliser ce fichier

Chaque entrée suit ce format :

| Champ | Description |
|---|---|
| **ID** | Identifiant unique (WL-001, WL-002, etc.) |
| **Date** | Date de découverte |
| **Catégorie** | Compilation / Runtime / Architecture / Unity / Sérialisation / Design |
| **Problème** | Description du problème rencontré |
| **Cause racine** | Pourquoi c'est arrivé |
| **Solution** | Comment ça a été / doit être résolu |
| **Impact** | 🔴 Bloquant / 🟠 Fort / 🟡 Moyen / 🟢 Faible |
| **Prévention** | Comment éviter que ça se reproduise |

---

## 🐛 Bugs identifiés dans le code existant (audit 2026-04-14)

### WL-001 — `deceased.RootElement` n'existe pas
| Champ | Valeur |
|---|---|
| **ID** | WL-001 |
| **Date** | 2026-04-14 |
| **Catégorie** | Compilation |
| **Fichier** | `Assets/_Project/Scripts/Clan/LegacySystem.cs:50` |
| **Problème** | Le code appelle `deceased.RootElement` mais `CharacterData` n'a pas cette propriété. La propriété existante s'appelle `Affinity` (de type `Element`). |
| **Cause racine** | Incohérence entre la génération initiale par l'IA : un premier prompt a peut-être utilisé `RootElement`, un second prompt l'a renommé `Affinity` dans `CharacterData` mais pas dans `LegacySystem`. |
| **Solution** | Remplacer `deceased.RootElement` par `deceased.Affinity`. |
| **Impact** | 🔴 Bloquant — le projet entier ne compile pas. |
| **Statut** | ✅ Résolu 2026-04-15. |
| **Prévention** | Systématiquement faire `Refactor → Rename` de Rider/VS au lieu de renommer à la main. Activer les warnings "CS0103 Name does not exist" en Error dans `.editorconfig`. |

---

### WL-002 — `MirrorSystem.ConsumeMirrorPower` n'existe pas
| Champ | Valeur |
|---|---|
| **ID** | WL-002 |
| **Date** | 2026-04-14 |
| **Catégorie** | Compilation |
| **Fichier** | `Assets/_Project/Scripts/Mirror/DeductionEngine.cs:65` |
| **Problème** | Le code appelle `MirrorSystem.Instance.ConsumeMirrorPower(mirrorPowerCost)` mais la méthode s'appelle `ConsumePower(int amount)`. |
| **Cause racine** | Même que WL-001 — incohérence de nommage entre fichiers. |
| **Solution** | Remplacer par `MirrorSystem.Instance.ConsumePower(mirrorPowerCost)`. |
| **Impact** | 🔴 Bloquant. |
| **Statut** | ✅ Résolu 2026-04-15. |
| **Prévention** | Idem WL-001. De plus, ne jamais redoubler le préfixe dans le nom de méthode si le type porte déjà le préfixe (`MirrorSystem.ConsumeMirrorPower` → redondant, `MirrorSystem.ConsumePower` suffit). |

---

### WL-003 — `Element.Ice` absent de l'enum `Element`
| Champ | Valeur |
|---|---|
| **ID** | WL-003 |
| **Date** | 2026-04-14 |
| **Catégorie** | Compilation |
| **Fichier** | `Assets/_Project/Scripts/Mirror/DeductionEngine.cs:162` |
| **Problème** | `DeductionEngine.GenerateProceduralName` utilise `Element.Ice` dans un switch expression, mais l'enum `Element` ne contient pas cette valeur. L'enum actuel : `None, Fire, Water, Wood, Metal, Earth, Lightning, Darkness, Light`. |
| **Cause racine** | Le master prompt liste 8 éléments (Feu, Eau, Bois, Métal, Terre, Foudre, Ténèbres, Lumière). L'IA a inventé "Ice" en rédigeant le switch de noms procéduraux. |
| **Solution** | Deux options : (a) retirer le case `Element.Ice` du switch dans `DeductionEngine`, (b) ajouter `Ice` à l'enum Element si l'on veut ce 9e élément — dans ce cas, l'ajouter aussi à `GeneticSystem.GenerateRandomAffinity` pour cohérence. Recommandation : option (a). |
| **Impact** | 🔴 Bloquant. |
| **Statut** | ✅ Résolu 2026-04-15. |
| **Prévention** | Ne pas écrire de switch sur un enum dans des fichiers différents. Si on a besoin de mapping enum → string, utiliser une méthode d'extension centralisée qui couvre toutes les valeurs via un `Dictionary<Element, string>` initialisé depuis l'enum — les valeurs manquantes deviennent visibles immédiatement. |

---

### WL-004 — `EventManager.GenerateYearlyEvent` n'existe pas + `TriggerYearlyEvent` est privée
| Champ | Valeur |
|---|---|
| **ID** | WL-004 |
| **Date** | 2026-04-14 |
| **Catégorie** | Compilation / Encapsulation |
| **Fichier** | `Assets/_Project/Scripts/Tests/GameSimulationTest.cs:75` |
| **Problème** | Le test appelle `EventManager.Instance.GenerateYearlyEvent()` qui n'existe pas. La méthode existante s'appelle `TriggerYearlyEvent()` et est `private`. |
| **Cause racine** | Test écrit avant / après la signature finale d'`EventManager`, sans recompilation. Faute d'un Unity qui tourne, aucune erreur visible. |
| **Solution** | Soit (a) passer `TriggerYearlyEvent` en `public` si on veut permettre aux tests de la déclencher manuellement, soit (b) laisser le trigger automatique via `OnPhaseChanged` et supprimer l'appel dans le test. Recommandation : (a) avec un commentaire XML "Exposed for tests only — normally triggered by OnPhaseChanged". |
| **Impact** | 🔴 Bloquant. |
| **Statut** | ✅ Résolu 2026-04-15. |
| **Prévention** | Écrire un vrai test NUnit qui peut se lancer dès qu'on sauvegarde. Le Test Runner d'Unity signale ces erreurs immédiatement. |

---

### WL-005 — `FactionManager.ProcessYearlyFactionAI` privée appelée depuis test
| Champ | Valeur |
|---|---|
| **ID** | WL-005 |
| **Date** | 2026-04-14 |
| **Catégorie** | Compilation / Encapsulation |
| **Fichier** | `Assets/_Project/Scripts/Tests/GameSimulationTest.cs:88` |
| **Problème** | Même pattern que WL-004 : test appelle `FactionManager.Instance.ProcessYearlyFactionAI()` qui est `private`. |
| **Cause racine** | Idem WL-004. |
| **Solution** | Passer la méthode en `public` avec commentaire XML "Exposed for testing" ou utiliser `[assembly: InternalsVisibleTo("MirrorChronicles.Tests")]` pour exposer l'internal aux tests uniquement. |
| **Impact** | 🔴 Bloquant. |
| **Statut** | ✅ Résolu 2026-04-15. |
| **Prévention** | Idem WL-004. En plus, définir dès le départ la stratégie : tests dans assembly séparée + `InternalsVisibleTo`. |

---

### WL-006 — `JsonUtility` ne sérialise pas les auto-properties
| Champ | Valeur |
|---|---|
| **ID** | WL-006 |
| **Date** | 2026-04-14 |
| **Catégorie** | Sérialisation / Runtime |
| **Fichier** | `Assets/_Project/Scripts/Data/CharacterData.cs` (et autres POCOs) + `Assets/_Project/Scripts/Core/SaveSystem.cs` |
| **Problème** | `CharacterData` utilise des auto-properties `public int Age { get; set; }`. `JsonUtility.ToJson` ne les sérialise pas — seuls les champs publics (ou privés avec `[SerializeField]`) sont supportés. Résultat : la sauvegarde produit un JSON vide pour chaque personnage. |
| **Cause racine** | Le sérialiseur d'Unity est conçu pour les champs style `public int age;` (style Unity MonoBehaviour), pas pour des POCOs modernes. |
| **Solution** | Deux options : (a) convertir toutes les auto-properties en champs publics (`public int Age;` au lieu de `public int Age { get; set; }`) — simple mais casse l'encapsulation ; (b) utiliser Newtonsoft.Json via le package `com.unity.nuget.newtonsoft-json` — respecte properties, inheritance, converters custom. **Recommandation : (b).** |
| **Impact** | 🔴 Bloquant pour le mode Ironman (la save ne persiste rien). Silencieux au compile, cassé au runtime. |
| **Prévention** | Par défaut, utiliser Newtonsoft.Json dans tout projet Unity ayant une logique de sauvegarde non-triviale. `JsonUtility` seulement pour des petits DTO Unity-native (Vector3, etc.). |

---

### WL-007 — Wounds non persistés sur CharacterData
| Champ | Valeur |
|---|---|
| **ID** | WL-007 |
| **Date** | 2026-04-14 |
| **Catégorie** | Design / Runtime |
| **Fichier** | `Assets/_Project/Scripts/Characters/WoundSystem.cs` |
| **Problème** | `WoundSystem.ApplyLightWound/Moderate/Severe/Critical` écrivent uniquement un Debug.Log. Aucune blessure n'est effectivement stockée sur `CharacterData`. Le Dao Wound applique bien une pénalité de lifespan et mental, mais il n'est pas marqué comme "Dao Wound" sur le personnage — donc impossible de vérifier plus tard si un personnage a une blessure permanente. |
| **Cause racine** | TODO non finalisé dans le code généré (`"In a full implementation, we would add a 'Wound' object..."`). |
| **Solution** | Ajouter à `CharacterData` : `public List<WoundData> ActiveWounds { get; set; }` et `public bool HasDaoWound { get; set; }`. Créer `WoundData` POCO avec `Severity`, `MonthsRemaining`, `StatPenalty`. `WoundSystem` doit effectivement ajouter à la liste et un autre système (à créer) doit décrémenter `MonthsRemaining` à chaque tick de temps. |
| **Impact** | 🟠 Fort — le système de combat létal n'a pas d'effet persistant. |
| **Prévention** | Ne jamais laisser de commentaire "In a full implementation..." dans le code merged. Soit l'implémenter, soit créer un ticket NOT_DONE explicite. |

---

### WL-008 — Le projet n'est pas un vrai projet Unity
| Champ | Valeur |
|---|---|
| **ID** | WL-008 |
| **Date** | 2026-04-14 |
| **Catégorie** | Unity / Infra |
| **Fichier** | Racine du repo |
| **Problème** | Pas de `ProjectSettings/`, pas de `Packages/manifest.json`, pas de scènes `.unity`, pas de `.meta` files. Unity ne peut pas ouvrir le repo actuel comme projet. |
| **Cause racine** | Le code a été généré à plat (scripts C# uniquement) par l'IA, sans bootstrap Unity Hub. |
| **Solution** | Créer un nouveau projet Unity 2022 LTS avec le template "2D Core", puis déplacer les scripts existants dans `Assets/_Project/Scripts/`. Laisser Unity générer les `.meta`. |
| **Impact** | 🔴 Bloquant — rien ne tourne. |
| **Prévention** | Toujours bootstrapper via Unity Hub avant d'écrire du code, même pour du prototypage. |

---

### WL-009 — Ordre d'initialisation Singleton non garanti
| Champ | Valeur |
|---|---|
| **ID** | WL-009 |
| **Date** | 2026-04-14 |
| **Catégorie** | Architecture / Runtime |
| **Fichier** | `Assets/_Project/Scripts/Core/GameManager.cs:42-48`, `Core/SaveSystem.cs:55-61`, etc. |
| **Problème** | `GameManager.Start()` appelle `TimeManager.Instance.StartGameLoop()` — si `TimeManager` n'est pas attaché au même GameObject ou si son Awake n'a pas encore tourné, `Instance` est null. Problème similaire dans `SaveSystem.SaveGame()` qui lit `ClanManager.Instance.ClanName` sans vérifier. |
| **Cause racine** | Unity ne garantit pas un ordre d'Awake entre MonoBehaviour sur GameObjects différents. Le pattern Singleton lazy-via-Awake fonctionne seulement si tous les Singletons sont sur le même GameObject (Awake dans l'ordre des Components) ou avec Script Execution Order configuré. |
| **Solution** | Deux options : (a) mettre tous les Managers sur un seul GameObject `[Systems]` avec `GameManager` en haut (premier Awake) ; (b) configurer `Edit → Project Settings → Script Execution Order` pour imposer `GameManager` → `TimeManager` → autres ; (c) utiliser un Bootstrapper `[RuntimeInitializeOnLoadMethod]` qui crée les Singletons programmatiquement dans l'ordre. |
| **Impact** | 🟠 Fort — NullReferenceException au démarrage dans 90% des configs de scène. |
| **Prévention** | Script Execution Order défini dès l'ouverture du projet. Null checks systématiques avant `.Instance.Method()` pour les appels inter-Singleton. |

---

### WL-010 — `TimeManager.LoadState(year, phase)` manquant
| Champ | Valeur |
|---|---|
| **ID** | WL-010 |
| **Date** | 2026-04-14 |
| **Catégorie** | Runtime / Save-Load |
| **Fichier** | `Assets/_Project/Scripts/Core/SaveSystem.cs:120` (commentaire TODO) |
| **Problème** | `SaveSystem.RestoreGameState` ne restaure pas `CurrentYear` ni `CurrentPhase` dans `TimeManager` — le TODO explique qu'il faudrait une méthode `LoadState(year, phase)` qui set les valeurs sans déclencher les events. |
| **Cause racine** | Tension entre "réhydrater l'état" et "ne pas re-publier OnYearStarted lors d'un load". |
| **Solution** | Ajouter dans `TimeManager` : `public void LoadState(int year, GamePhase phase) { CurrentYear = year; CurrentPhase = phase; /* no events */ }` + appeler depuis `SaveSystem.RestoreGameState`. |
| **Impact** | 🟠 Fort — un load remet l'année à 1. |
| **Prévention** | Pour chaque champ `{ get; private set; }` public, se demander : "Comment ça se charge depuis une sauvegarde ?" et fournir un setter alternatif ou une méthode de load si nécessaire. |

---

## 📐 Leçons d'architecture & design

### WL-100 — Pivot Unity → Web → Unity : cause de la dette
| Champ | Valeur |
|---|---|
| **ID** | WL-100 |
| **Date** | 2026-04-14 |
| **Catégorie** | Design / Méta-projet |
| **Problème** | Le master prompt visait Unity/C#. L'implémentation s'est faite sur Google AI Studio en React/TS, avec conservation en parallèle d'une génération C# par l'IA. Résultat : 2 bases de code, aucune des deux complète, et le code C# n'a jamais été exécuté (d'où WL-001 à WL-005). |
| **Cause racine** | AI Studio a forcé un format web ; l'outil a généré du C# "à côté" sans jamais pouvoir le tester. |
| **Solution** | Retour complet à Unity. Le TypeScript devient legacy, à supprimer ou archiver. Doc `BRAIN_CLAUDE/` créée pour tracer. |
| **Impact** | 🟡 Moyen — perte de temps sur le port TS, mais l'architecture C# est déjà posée ce qui est un bon point de départ. |
| **Prévention** | Choisir UN SEUL framework dès le départ et l'exécuter immédiatement après chaque prompt IA pour détecter les divergences tôt. |

---

### WL-101 — Factions hardcodées au lieu de ScriptableObjects
| Champ | Valeur |
|---|---|
| **ID** | WL-101 |
| **Date** | 2026-04-14 |
| **Catégorie** | Design |
| **Fichier** | `Assets/_Project/Scripts/Diplomacy/FactionManager.cs:45-75` |
| **Problème** | `InitializeWorldFactions` crée les 3 factions en dur dans le code (Wang, Zhao, Azure Cloud). Viole la règle du master prompt "ScriptableObjects pour TOUTES les données statiques". |
| **Solution** | Créer `FactionDefinition.cs` (ScriptableObject) avec les mêmes champs que `FactionData`. Puis `FactionManager` charge via `Resources.LoadAll<FactionDefinition>("Factions")` et convertit en `FactionData` runtime. |
| **Impact** | 🟡 Moyen — pas bloquant mais bloque l'extensibilité (5-8 factions attendues dans le master prompt). |
| **Prévention** | Toute donnée initiale (factions, techniques, events, items) doit passer par des .asset ScriptableObjects dès la première version. |

---

## 📝 Template pour nouvelles entrées

```markdown
### WL-XXX — Titre court du problème
| Champ | Valeur |
|---|---|
| **ID** | WL-XXX |
| **Date** | YYYY-MM-DD |
| **Catégorie** | Compilation / Runtime / Architecture / Unity / Sérialisation / Design |
| **Fichier** | Chemin:ligne |
| **Problème** | Description |
| **Cause racine** | Pourquoi |
| **Solution** | Comment résolu |
| **Impact** | 🔴 / 🟠 / 🟡 / 🟢 |
| **Prévention** | Comment éviter |
```

---

*À chaque nouveau bug ou difficulté, créer une entrée WL-XXX.*
*Revoir ce fichier avant chaque grosse refonte pour ne pas reproduire les erreurs passées.*
