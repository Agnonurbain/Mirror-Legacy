# 🧠 BRAINSTORMING.md — Point d'entrée central

> **Ce fichier est le premier et dernier fichier .md à lire à chaque session.**
> Il orchestre la lecture de tous les autres fichiers de contexte **Reflets de Lignée : Les Chroniques du Miroir**.

---

## 📖 Ordre de lecture optimal (début de session)

Lire **dans cet ordre** pour une compréhension maximale avec un minimum de lectures :

| Ordre | Fichier | Chemin | Pourquoi | Temps de lecture |
|---|---|---|---|---|
| **1** | **BRAINSTORMING.md** | `BRAIN_CLAUDE/BRAINSTORMING.md` | Ce fichier — sait quoi lire ensuite | 30s |
| **2** | **CLAUDE.md** (ou master prompt) | `CLAUDE.md` (racine) ou prompt chat | Règles de codage, conventions C#/Godot, règles métier Xianxia | 2 min |
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
| **Gérer les dépendances (Godot, .NET, NuGet)** | `PACKAGE.md` | `BRAIN_CLAUDE/PACKAGE.md` |
| **Mettre à jour un test** | `TEST.md` | `BRAIN_CLAUDE/TEST.md` |

---

## 🎯 État ultra-court du projet

- **Type** : RPG de gestion de clan + tactique 2D, thème Xianxia (cultivation chinoise)
- **Moteur** : Godot 4.7.2 .NET + C# (depuis le 2026-09-25 ; Unity abandonné)
- **Phase actuelle** (2026-10-10) : migration Godot (G0-G5) et restructuration du lore L0-L5 faites ; simulation complète dans `src/Core` (~27 000 lignes), 10 écrans Godot, contenu dans 22 fichiers `game/data/*.json`. **2 092 tests NUnit passent** (`./Scripts/dev.sh test`), fumée headless OK (`./Scripts/dev.sh smoke`).
- **Source de vérité** : `CLAUDE.md` (racine) pour le code, `LORE.md` pour le monde
- **Langue** : FR pour docs + logs Debug en FR possible, **EN strict** pour code (classes, variables, commentaires XML)
- **Outillage** : Godot dans `~/code/Godot` (lien `~/Godot`), plugin godot-ai 4.3.0 dans `game/addons/godot_ai`

---

## ⚠️ Priorités actuelles (détails dans `NOT_DONE.md`)

1. 🟠 **G6 — Écrans à venir** : brancher la bataille sur les guerres, embuscades et chasses ; chaque étape vérifiée dans le jeu lancé depuis l'éditeur (godot-ai).
2. 🟠 **B1 — Équilibrage des longues parties** : population et pierres qui s'emballent après un siècle ; un clan passif n'attire aucun complot.
3. 🟡 **P1 — Parité du monde** : tout ce qui arrive au clan arrive aussi au reste du monde.
4. 🟡 **L6 — Monde vivant** puis **L7 — autres voies de cultivation**.
5. 🟢 **Polish** : sprites peints (#60-61), VFX (#62-63), audio (#64).

L'historique Unity (phases 1-3, avril 2026) est dans `DONE.md` et dans git.

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
| **8** | `PACKAGE.md` | Si une dépendance a été ajoutée, retirée ou mise à jour |

---

## 🔑 Règles d'or (rappel)

1. **Code anglais strict, docs FR** — pas de mélange.
2. **Simulation sans moteur** : toute règle vit dans `src/Core` (aucun type Godot, dépendances par constructeur, `GameContext` partagé, **pas de singleton**) ; la couche `game/` ne fait que lier les modèles de `Presentation`. Patterns : Observer (bus par partie), Strategy (IA), Command (actions de combat), MVC.
3. **Hasard à graine** : toujours `ctx.Rng` — une graine rejoue la même partie.
4. **Données JSON (`game/data/`) pour tout contenu statique**, validées par `GameContentLoader` ; jamais de nom propre du roman (`LORE.md` §13).
5. **Null checks + try/catch** sur les opérations critiques (Save/Load) ; ne jamais renommer un champ sauvegardé.
6. **TDD NUnit** (`dotnet test`) pour chaque règle, puis **vérification dans le jeu lancé depuis l'éditeur** (godot-ai) pour chaque changement de gameplay ou d'interface.
7. **Un système à la fois** — proposer l'architecture, valider avec l'utilisateur, puis coder.

---

*Ce fichier doit être lu au début de chaque session pour reprendre le contexte rapidement.*
*Toujours mettre à jour `DONE.md`, `NOT_DONE.md`, `WORKED_LESSON.md` après chaque tâche complétée.*
