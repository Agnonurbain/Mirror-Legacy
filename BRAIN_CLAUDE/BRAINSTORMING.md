# 🧠 BRAINSTORMING.md — Point d'entrée central

> **Ce fichier est le premier et dernier fichier .md à lire à chaque session.**
> Il orchestre la lecture de tous les autres fichiers de contexte **Reflets de Lignée : Les Chroniques du Miroir**.

---

## 📖 Ordre de lecture optimal (début de session)

Lire **dans cet ordre** pour une compréhension maximale avec un minimum de lectures :

| Ordre | Fichier | Chemin | Pourquoi | Temps de lecture |
|---|---|---|---|---|
| **1** | **BRAINSTORMING.md** | `BRAIN_CLAUDE/BRAINSTORMING.md` | Ce fichier — sait quoi lire ensuite | 30s |
| **2** | **CLAUDE.md** (ou master prompt) | `CLAUDE.md` (racine) ou prompt chat | Règles de codage, conventions Unity/C#, règles métier Xianxia | 2 min |
| **3** | **MEMORY.md** | `BRAIN_CLAUDE/MEMORY.md` | Contexte projet résumé, état actuel, bugs critiques, prochaines étapes | 2 min |
| **4** | **NOT_DONE.md** | `BRAIN_CLAUDE/NOT_DONE.md` | Tâches restantes numérotées, priorisées, détaillées | 1 min |
| **5** | **WORKED_LESSON.md** | `BRAIN_CLAUDE/WORKED_LESSON.md` | Pièges à éviter, bugs déjà identifiés | 1 min |

### En résumé :

```
BRAINSTORMING.md → CLAUDE.md → MEMORY.md → NOT_DONE.md → WORKED_LESSON.md
       (30s)         (2 min)     (2 min)       (1 min)         (1 min)
                                       Total : ~6-7 min
```

---

## 📖 Ordre de lecture optionnel (selon le besoin)

| Besoin | Lire ensuite | Chemin |
|---|---|---|
| **Vision complète du jeu** | `PLAN.md` | `BRAIN_CLAUDE/PLAN.md` |
| **Vérifier ce qui a été fait** | `DONE.md` | `BRAIN_CLAUDE/DONE.md` |
| **Résoudre un bug connu** | `WORKED_LESSON.md` | `BRAIN_CLAUDE/WORKED_LESSON.md` |
| **Gérer les packages Unity** | `PACKAGE.md` | `BRAIN_CLAUDE/PACKAGE.md` |
| **Mettre à jour un test** | `TEST.md` | `BRAIN_CLAUDE/TEST.md` |

---

## 🎯 État ultra-court du projet

- **Type** : RPG de gestion de clan + tactique 2D, thème Xianxia (cultivation chinoise)
- **Moteur cible** : Unity 6.4 LTS (6000.4.x) + C#
- **Phase actuelle** : **Phase 1 en cours** — le code Phase 1-3 existe déjà sous forme de scripts, mais **le projet n'est PAS encore un vrai projet Unity** (pas de `ProjectSettings/`, pas de `Packages/manifest.json`, pas de scène `.unity`). Plusieurs **erreurs de compilation** à corriger avant que quoi que ce soit tourne.
- **Source de vérité** : le master prompt (également dans chat / CLAUDE.md racine)
- **Langue** : FR pour docs + logs Debug en FR possible, **EN strict** pour code (classes, variables, commentaires XML)

---

## ⚠️ 5 priorités absolues (avant toute nouvelle feature)

1. **Bootstrapper le projet Unity** — créer `ProjectSettings/`, `Packages/manifest.json`, une scène `ClanDomain.unity` avec tous les `MonoBehaviour` Singletons attachés à un GameObject `[Systems]`.
2. **Corriger les erreurs de compilation** (voir WORKED_LESSON.md) — le code ne compile pas en l'état.
3. **Créer l'asmdef** `MirrorChronicles.Runtime.asmdef` pour isoler les scripts et activer la compilation incrémentale.
4. **Vérifier la sérialisation JSON** — `JsonUtility` ne sérialise pas les auto-properties `{ get; set; }` → `CharacterData` ne se sauvegardera pas correctement en l'état.
5. **Supprimer ou archiver le code web** (`src/`, `package.json`, `vite.config.ts`, etc.) — le pivot C# rend ce code obsolète.

---

## 📝 En fin de session — Mettre à jour

**Après chaque tâche complétée**, mettre à jour **dans cet ordre** :

| Ordre | Fichier | Action |
|---|---|---|
| **1** | `DONE.md` | Ajouter la tâche complétée avec détails |
| **2** | `NOT_DONE.md` | Marquer la tâche comme faite, ajuster la progression |
| **3** | `PLAN.md` | Mettre à jour les statuts ✅/❌, ajouter au changelog |
| **4** | `WORKED_LESSON.md` | Ajouter toute difficulté rencontrée avec sa leçon |
| **5** | `TEST.md` | Vérifier que les tests (Edit Mode / Play Mode) associés sont à jour |
| **6** | `MEMORY.md` | Mettre à jour le résumé si l'état global a changé |
| **7** | `BRAINSTORMING.md` (ce fichier) | Mettre à jour les priorités absolues si nécessaire |
| **8** | `PACKAGE.md` | Si un package Unity a été ajouté/supprimé |

---

## 🔑 Règles d'or (rappel)

1. **Code anglais strict, docs FR** — pas de mélange.
2. **Design patterns obligatoires** : Singleton, Observer (C# Events), Strategy, Command, State Machine, MVC.
3. **Pas de `SendMessage`, pas de `FindObjectOfType` en boucle, pas de `StartCoroutine` pour les timers** (utiliser async/await ou GameEvents).
4. **ScriptableObjects pour toute donnée statique** (Techniques, Fragments, Factions, Events, Items).
5. **Null checks + try/catch** sur les opérations critiques (Save/Load).
6. **Tests Play Mode** pour chaque système lourd après implémentation.
7. **Un système à la fois** — proposer l'architecture, valider avec l'utilisateur, puis coder.

---

*Ce fichier doit être lu au début de chaque session pour reprendre le contexte rapidement.*
*Toujours mettre à jour `DONE.md`, `NOT_DONE.md`, `WORKED_LESSON.md` après chaque tâche complétée.*
