# 📦 PACKAGE.md — Dépendances

> **Dernière mise à jour :** 2026-09-25 (migration Unity → Godot 4.7.2 .NET).
> **Mettre à jour ce fichier à chaque ajout, suppression ou montée de version.**

---

## Outils

| Outil | Version | Où | Remarque |
|---|---|---|---|
| **Godot .NET** | 4.7.2-stable | `~/Godot/Godot_v4.7.2-stable_mono_linux_x86_64/` | Archive officielle, somme SHA-512 vérifiée ; `GODOT_BIN` pour un autre chemin |
| **SDK .NET** | 8.0.x | système | Godot 4.7.2 cible `net8.0` |
| **graphify** | — | `graphify` dans le PATH | Graphe de connaissances du dépôt |

## Paquets NuGet

| Projet | Paquet | Version | Rôle |
|---|---|---|---|
| `game/MirrorLegacy.csproj` | `Godot.NET.Sdk` | 4.7.2 | SDK du projet Godot |
| `src/Core` | `Newtonsoft.Json` | 13.0.3 | Sauvegardes et contenu JSON (énumérations par nom) |
| `tests/Core.Tests` | `NUnit` | 3.14.0 | Tests (assertions classiques `Assert.AreEqual`) |
| `tests/Core.Tests` | `NUnit3TestAdapter` | 4.6.0 | Exécution par `dotnet test` |
| `tests/Core.Tests` | `Microsoft.NET.Test.Sdk` | 17.11.1 | Hôte de tests |

## Règles

- **Core ne dépend d'aucun moteur** : pas de paquet Godot dans `src/Core` ni dans les tests.
- NUnit reste en 3.x : la 4.x déplace les assertions classiques (`ClassicAssert`).
- Avant d'ajouter un paquet, vérifier qu'il existe pour .NET 8 et qu'il ne tire pas de dépendance au moteur.
- **Exports (plus tard)** : installer `Godot_v4.7.2-stable_mono_export_templates.tpz` et inclure `data/*.json` dans le filtre d'export des préréglages.
