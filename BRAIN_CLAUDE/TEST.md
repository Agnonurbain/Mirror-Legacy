# 🧪 TEST.md — Règles de tests

> **Code modifié = tests mis à jour dans le même commit.** Pas d'exception. Un test qui casse après un changement de code est à corriger tout de suite (le code, ou le test s'il était faux).

---

## Commandes

```bash
./Scripts/dev.sh test                  # tests de la simulation (NUnit, dotnet test) — ~1 s, sans moteur
./Scripts/dev.sh smoke                 # le jeu sous Godot, sans affichage : 5 ans joués par l'écran, échoue à la moindre erreur
./Scripts/dev.sh screenshot out.png    # idem dans une vraie fenêtre, puis capture PNG (vérification visuelle)
./Scripts/dev.sh all                   # build + test + smoke
```

Ne jamais se fier à un ancien fichier de résultat : relancer la commande et lire sa sortie.

---

## Les niveaux de tests

| Niveau | Où | Ce qu'il garantit |
|---|---|---|
| **Règles pures** | `tests/Core.Tests/<Domaine>/*RulesTests.cs` | Formules et seuils du lore (échelle de puissance, percées, orifice, tâches…) |
| **Systèmes** | `tests/Core.Tests/<Domaine>/*Tests.cs` | Chaque système sur un `GameContext` (`Fixtures.Context`) ou un monde complet (`TestWorld`) |
| **Session** | `tests/Core.Tests/Session/` | Ordre des phases, sauvegardes (instantané, v1 → v2), fin de partie |
| **Simulation** | `SimulationTests` | Parties de 100 ans sur plusieurs graines : invariants, mariages, naissances, percées, déterminisme |
| **Contenu** | `tests/Core.Tests/Data/` | Le chargeur refuse le contenu injouable ; le contenu livré respecte le lore (D3, clan Mo, aucun nom du roman) |
| **Présentation** | `tests/Core.Tests/Presentation/` | Ce que montrent les écrans (en-tête, liste, chronique) |
| **Fumée Godot** | `dev.sh smoke` | Le jeu démarre, l'écran joue plusieurs années sans erreur du moteur |

---

## Écrire un test

- **TDD** : écrire le test, le lancer et le voir échouer pour la bonne raison (RED, commit), puis implémenter (GREEN, commit).
- **Un seul assert** par test ; nom `Méthode_Résultat_QuandCondition`.
- **Hasard maîtrisé** : `FixedRandom(x)` (toujours le même tirage), `SequenceRandom(a, b, …)` (tirages successifs), `new Random(graine)` pour les statistiques. Ne jamais dépendre d'un hasard non semé.
- **Données** : `Fixtures.Content` (le contenu livré), `Fixtures.QuietContent` (sans événements aléatoires), `Fixtures.Cultivator(...)`, `Fixtures.Mortal(...)`.
- **Un test qui passe avant le correctif n'est pas un reproducteur** : le rendre discriminant (cf. le test des prénoms, 2026-09-25).

---

## Revue

Après toute modification C# : revue ECC (`ecc:csharp-reviewer`), correction des points CRITIQUE et ÉLEVÉ, puis nouvelle vérification.
