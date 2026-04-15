# 📦 PACKAGE.md — Registre des packages Unity

> **Dernière mise à jour :** 2026-04-14 (projet Unity pas encore bootstrapped — liste = packages cibles).
> **Ce fichier DOIT être mis à jour à chaque ajout/suppression de package.**

---

## 📋 Table des matières

1. [Bootstrap du projet Unity](#bootstrap-du-projet-unity)
2. [Packages Unity requis](#packages-unity-requis)
3. [Packages Unity recommandés](#packages-unity-recommandés)
4. [Packages optionnels (plus tard)](#packages-optionnels)
5. [Configuration manuelle](#configuration-manuelle)
6. [Commandes de référence](#commandes-de-référence)

---

## Bootstrap du projet Unity

Une seule fois, pour créer le projet :

1. Unity Hub → **New Project**
2. Editor Version : **Unity 2022.3 LTS** (ou plus récent LTS)
3. Template : **2D Core** (inclut 2D Sprite + 2D Tilemap + Universal Render Pipeline 2D)
4. Project Name : `MirrorChronicles`
5. Location : `/home/aymeric/Mirror-Legacy/`
6. ⚠️ Cocher **Connect to Version Control** = Off (on gère git à la main)

Après création :
- Fermer Unity
- Déplacer les scripts existants de `Assets/_Project/Scripts/` dans le nouveau projet (s'ils ne sont pas déjà au bon endroit)
- Rouvrir Unity et laisser générer les `.meta` files
- Add/commit tout sauf `Library/`, `Temp/`, `Obj/`, `Build/`, `Logs/`, `UserSettings/`, `*.csproj`, `*.sln`

---

## Packages Unity requis

Ces packages sont **obligatoires** pour que le code existant fonctionne.

| Package | Version cible | Identifiant | Usage |
|---|---|---|---|
| **TextMeshPro** | Built-in | `com.unity.textmeshpro` | Tout affichage texte (UI) |
| **Newtonsoft JSON** | 3.2.1+ | `com.unity.nuget.newtonsoft-json` | Sérialisation JSON robuste (remplace JsonUtility — voir WL-006) |
| **Input System** | 1.7+ | `com.unity.inputsystem` | Entrées clavier/souris/tactile unifiées (PC + mobile) |
| **Test Framework** | 1.1+ | `com.unity.test-framework` | Tests NUnit Edit Mode + Play Mode |

### Installation

```
Window → Package Manager → Unity Registry
  - TextMeshPro (Essentials importés automatiquement)
  - Newtonsoft JSON
  - Input System (redémarrer Unity après install, choisir "New Input System (Preview)")
  - Test Framework
```

Ou directement dans `Packages/manifest.json` :

```json
{
  "dependencies": {
    "com.unity.textmeshpro": "3.0.6",
    "com.unity.nuget.newtonsoft-json": "3.2.1",
    "com.unity.inputsystem": "1.7.0",
    "com.unity.test-framework": "1.3.9",
    "com.unity.2d.sprite": "1.0.0",
    "com.unity.2d.tilemap": "1.0.0",
    "com.unity.render-pipelines.universal": "14.0.11"
  }
}
```

---

## Packages Unity recommandés

À ajouter **avant Phase 4** (polish/UI/audio).

| Package | Identifiant | Usage |
|---|---|---|
| **Cinemachine** | `com.unity.cinemachine` | Caméra procédurale (zoom, secousses, suivi) pour scènes combat et percée |
| **Post Processing** | `com.unity.postprocessing` | Effets vignette, chromatic aberration pour les séquences de percée |
| **Addressables** | `com.unity.addressables` | Chargement asynchrone des assets (sprites par âge, 10 × 2 × N archétypes) |
| **Localization** | `com.unity.localization` | Multi-langue (FR par défaut, EN plus tard) |
| **2D Animation** | `com.unity.2d.animation` | Animation squelettale 2D pour les sprites de personnages |
| **Timeline** | `com.unity.timeline` | Cinématiques (percée, ascension, fin de jeu) |

---

## Packages optionnels

À considérer selon besoin.

| Package | Identifiant | Usage | Quand ? |
|---|---|---|---|
| **Analytics** | `com.unity.services.analytics` | Télémétrie anonymisée des parties | Avant release |
| **In-App Purchasing** | `com.unity.purchasing` | Monétisation mobile (si F2P) | Phase 4 si monétisation |
| **Unity Cloud Build** | (service) | CI/CD Steam + Mobile | Phase 4 release |
| **DOTween** (Asset Store) | Demigiant | Animations tween propres (UI, transitions) | Phase 4 polish |
| **Odin Inspector** (payant) | Sirenix | Inspector avancé pour ScriptableObjects complexes | Si besoin de beaucoup de SO |

---

## Configuration manuelle

### Script Execution Order (WL-009)

`Edit → Project Settings → Script Execution Order` :

1. `MirrorChronicles.Core.GameManager` (−100)
2. `MirrorChronicles.Core.TimeManager` (−90)
3. `MirrorChronicles.Events.GameEvents` (−80, static — pas nécessaire mais clarifie)
4. `MirrorChronicles.Clan.ClanManager` (−70)
5. `MirrorChronicles.Clan.BloodRegistry` (−60)
6. Tous les autres Singletons (0 — ordre par défaut)

### Quality Settings

`Edit → Project Settings → Quality` :
- PC : "High" par défaut
- Mobile : "Low" avec shadows = Disabled, anisotropic = Disabled

### Player Settings

- Company Name : à définir
- Product Name : `Mirror Chronicles` (ou nom final du jeu)
- Default Icon : à définir (Phase 4)
- Scripting Backend : **IL2CPP** pour release (performance + anti-reverse)
- Api Compatibility Level : **.NET Standard 2.1**

### Build Settings — Scenes in Build

(Une fois les scènes créées)
1. `Scenes/MainMenu.unity`
2. `Scenes/ClanDomain.unity`
3. `Scenes/TacticalCombat.unity`
4. `Scenes/WorldMap.unity`

---

## Assembly Definitions

À créer :

| Fichier | Chemin | References |
|---|---|---|
| `MirrorChronicles.Runtime.asmdef` | `Assets/_Project/Scripts/` | (auto : Unity.TextMeshPro, Newtonsoft.Json, Unity.InputSystem) |
| `MirrorChronicles.Editor.asmdef` | `Assets/_Project/Editor/` | `MirrorChronicles.Runtime`, `UnityEditor` (Include Platforms : Editor only) |
| `MirrorChronicles.Tests.EditMode.asmdef` | `Assets/_Project/Tests/EditMode/` | `MirrorChronicles.Runtime`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner` |
| `MirrorChronicles.Tests.PlayMode.asmdef` | `Assets/_Project/Tests/PlayMode/` | idem EditMode |

---

## Commandes de référence

### Build en ligne de commande (Linux/Mac/Windows)

```bash
# Build PC Windows (sur machine Windows ou via Unity Cloud Build)
Unity -batchmode -quit \
  -projectPath "$(pwd)" \
  -buildTarget StandaloneWindows64 \
  -executeMethod BuildScripts.BuildWindows \
  -logFile build.log

# Tests Edit Mode
Unity -batchmode -quit \
  -projectPath "$(pwd)" \
  -runTests \
  -testPlatform EditMode \
  -testResults editmode-results.xml \
  -logFile editmode.log

# Tests Play Mode
Unity -batchmode -quit \
  -projectPath "$(pwd)" \
  -runTests \
  -testPlatform PlayMode \
  -testResults playmode-results.xml \
  -logFile playmode.log
```

---

## Changelog des packages

| Date | Action | Package | Raison |
|---|---|---|---|
| — | (Projet Unity pas encore bootstrapped) | — | — |
| *Prochains ajouts à logger ici* | | | |

---

*Toujours mettre à jour ce fichier après installation/désinstallation d'un package.*
*Vérifier `Packages/manifest.json` et committer les changements explicitement.*
