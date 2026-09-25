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
- **Phase actuelle** : **Phases 1-3 terminées, Phase 4 en cours** — 60+ scripts, 6 actions combat, 5 stratégies IA, A*, terrain procédural, 11 events + 6 story events, 8 factions, 8 bâtiments, 5 ressources, espionnage, WorldMap, victoire/défaite, save slots, object pool. 17 tests EditMode passent.
- **Source de vérité** : le master prompt (également dans chat / CLAUDE.md racine)
- **Langue** : FR pour docs + logs Debug en FR possible, **EN strict** pour code (classes, variables, commentaires XML)

---

## ⚠️ 5 priorités absolues (Phase 2)

1. ✅ ~~Bootstrapper le projet Unity~~ — fait (2026-04-15)
2. ✅ ~~Corriger les erreurs de compilation~~ — fait (WL-001..005)
3. ✅ ~~Créer les asmdef~~ — Runtime + Editor + Tests
4. ✅ ~~Sérialisation JSON → Newtonsoft~~ — SaveSystem migré
5. ✅ ~~Archiver le code web~~ — dans `.archive/web-port/`

**Nouvelles priorités Phase 2 :**
1. ✅ ~~TechniqueAction~~ (#20) — fait (2026-04-18)
2. ✅ ~~ItemAction + FleeAction~~ (#21) — fait (2026-04-18)
3. ✅ ~~3 stratégies IA~~ (#22) — fait (2026-04-18)
4. ✅ ~~Terrain procédural~~ (#23) — fait (2026-04-18)
5. ✅ ~~Table d'événements~~ (#25) — fait (2026-04-18)

6. ✅ ~~Pathfinding A*~~ (#24) — fait (2026-04-18)
7. ✅ ~~Événements scénarisés~~ (#26) — fait (2026-04-18)
8. ✅ ~~Intervention Divine combat~~ (#28) — fait (2026-04-18)
9. ✅ ~~Intervention Divine percée~~ (#29) — fait (2026-04-18)

10. ✅ ~~Résolution UI events~~ (#27) — fait (2026-04-18)
11. ✅ ~~FactionData SO~~ (#40) — fait (2026-04-18), 8 factions par défaut
12. ✅ ~~BuildingSystem~~ (#43) — fait (2026-04-18), 8 bâtiments × 5 niveaux
13. ✅ ~~4 ressources~~ (#44) — fait (2026-04-18)
14. ✅ ~~Succession Patriarche~~ (#45) — fait (2026-04-18)
15. ✅ ~~Karma du Clan~~ (#46) — fait (2026-04-18)

16. ✅ ~~EspionageSystem~~ (#41) — fait (2026-04-18)

17. ✅ ~~WorldMap scene~~ (#42) — fait (2026-04-18)
18. ✅ ~~Condition de victoire~~ (#66) — fait (2026-04-18)
19. ✅ ~~Object Pool~~ (#100) — fait (2026-04-18)
20. ✅ ~~Memento save slots~~ (#101) — fait (2026-04-18)

**Prochaines priorités (Phase 4 — Art & Polish) :**
1. **Direction artistique Shuimo** (#60) — nécessite assets visuels
2. **UI thématique** (#61) — remplacement UI placeholder
3. **VFX percée/combat** (#62-63) — Particle Systems
4. **Audio** (#64) — musique + SFX
5. **MVC strict pour UI** (#102)

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
2. **Design patterns obligatoires** : Singleton, Observer (C# Events), Strategy, Command, State Machine, MVC.
3. **Pas de `SendMessage`, pas de `FindObjectOfType` en boucle, pas de `StartCoroutine` pour les timers** (utiliser async/await ou GameEvents).
4. **Données JSON (`game/data/`) pour tout contenu statique** (clan, noms, équilibrage, factions, événements ; techniques et Fruitions à venir).
5. **Null checks + try/catch** sur les opérations critiques (Save/Load).
6. **Tests Play Mode** pour chaque système lourd après implémentation.
7. **Un système à la fois** — proposer l'architecture, valider avec l'utilisateur, puis coder.

---

*Ce fichier doit être lu au début de chaque session pour reprendre le contexte rapidement.*
*Toujours mettre à jour `DONE.md`, `NOT_DONE.md`, `WORKED_LESSON.md` après chaque tâche complétée.*
