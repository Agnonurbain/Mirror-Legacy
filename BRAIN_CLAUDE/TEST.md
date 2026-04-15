# 🧪 TEST.md — Règles de tests Unity

> **⚠️ RAPPEL IMPÉRATIF** — À chaque fois que tu modifies du code, tu DOIS mettre à jour les tests associés.
> **Ce fichier DOIT être lu avant chaque commit.**

---

## 📋 Règle d'or

> **Code modifié = Tests mis à jour dans le MÊME commit.**
>
> Pas d'exception. Pas de "je le ferai plus tard".
> Un test qui échoue après un changement de code est un bug de test — **à corriger immédiatement**.

---

## 🧬 Deux niveaux de tests

### 1. Edit Mode Tests (NUnit pur, rapides)

Pour tout ce qui est **logique métier pure** sans dépendance à Unity Runtime :
- `GeneticSystem` (static class → facile à tester)
- `BreakthroughSystem.CalculateSuccessRate` (pure function)
- `DeductionEngine.GenerateTechnique` (via extraction dans une static helper)
- Sérialisation/désérialisation d'un `CharacterData` ou d'un `GameData`
- Validation `MarriageSystem.CanMarry`
- Calculs de damage dans `AttackAction`

**Emplacement** : `Assets/_Project/Tests/EditMode/`
**Assembly** : `MirrorChronicles.Tests.EditMode.asmdef`
**Exécution** : `Window → General → Test Runner → EditMode → Run All` (quelques ms)

### 2. Play Mode Tests (UnityTest, plus lents)

Pour tout ce qui a besoin de **runtime Unity** :
- Comportement de `TimeManager` avec coroutines et events
- Écoute/publish d'événements via `GameEvents`
- `SaveSystem.SaveGame` + `LoadGame` cycle complet (écrit sur disque)
- Simulation complète de 10 années (`GameSimulationTest`)
- Ordre d'Awake/Start des Singletons sur une scène test

**Emplacement** : `Assets/_Project/Tests/PlayMode/`
**Assembly** : `MirrorChronicles.Tests.PlayMode.asmdef`
**Exécution** : `Window → General → Test Runner → PlayMode → Run All` (plusieurs secondes)

---

## ✅ Checklist avant commit

Après **CHAQUE** modification de code, se poser ces questions :

### 1. Ai-je modifié une static class / pure function ?
- [ ] Ajouter/mettre à jour un test Edit Mode dans `Tests/EditMode/`
- [ ] Couvrir cas nominaux + cas limites (null, valeurs extrêmes, boundaries)
- [ ] Exemple : après modif de `GeneticSystem.GenerateSpiritualRoot`, vérifier : clamp [1,100], moyenne quand mutation = 0, variance quand parents identiques

### 2. Ai-je modifié un MonoBehaviour ?
- [ ] Si logique pure extractible → déplacer dans une static class et tester en Edit Mode
- [ ] Sinon → test Play Mode qui instancie un GameObject avec le component
- [ ] Vérifier le cycle Awake → OnEnable → Start → OnDestroy
- [ ] Vérifier la souscription/désabonnement aux events

### 3. Ai-je modifié un event `GameEvents` ?
- [ ] Vérifier tous les listeners dans tout le projet (Grep sur le nom de l'event)
- [ ] Mettre à jour les signatures si l'event a changé (ex : ajout d'un paramètre)
- [ ] Test Play Mode : subscribe → trigger → assert callback reçu

### 4. Ai-je modifié un POCO (`CharacterData`, `GameData`, etc.) ?
- [ ] Test Edit Mode de sérialisation Newtonsoft.Json : round-trip toJson/fromJson
- [ ] Vérifier les champs ajoutés sont marqués `[JsonProperty]` si Newtonsoft est custom-configuré
- [ ] Vérifier les factories / constructeurs par défaut

### 5. Ai-je modifié un `Singleton` Manager ?
- [ ] Vérifier Script Execution Order encore correct (voir PACKAGE.md)
- [ ] Vérifier l'ordre Awake → autres Singletons
- [ ] Test Play Mode avec scène de test qui instancie tous les Managers

### 6. Ai-je modifié un ScriptableObject ?
- [ ] Vérifier tous les `.asset` existants sont toujours valides (pas de champ disparu)
- [ ] Si breaking change : script d'upgrade ou Unity Editor migration
- [ ] Test Edit Mode : charger le SO depuis Resources, assert les champs

### 7. Ai-je modifié le SaveSystem ou le format JSON ?
- [ ] Test Play Mode : save une partie, kill le jeu, reload, assert que tout est identique
- [ ] Tester un upgrade de save : save avec v1.0, charger avec v2.0 (si on implémente versioning)
- [ ] Vérifier `SaveVersion` dans `GameData`

### 8. Ai-je modifié la combat scene ?
- [ ] Test Play Mode : `TacticalCombatManager` full cycle (Init → Deploy → PlayerTurn → EnemyTurn → Victory/Defeat)
- [ ] Vérifier que les unités mortes sont bien retirées de la `TurnQueue`
- [ ] Vérifier post-combat : `WoundSystem.EvaluatePostCombatWounds` appelé

---

## 📐 Conventions de naming

```csharp
// Classe de test
public class GeneticSystemTests  // Suffix "Tests" obligatoire
{
    // Pattern : Methode_Scenario_ResultatAttendu
    [Test]
    public void GenerateSpiritualRoot_WithNullParents_ReturnsValueInValidRange()
    {
        // Arrange
        // Act
        int root = GeneticSystem.GenerateSpiritualRoot(null, null);
        // Assert
        Assert.That(root, Is.InRange(1, 100));
    }

    [Test]
    public void GenerateSpiritualRoot_WithHighRootParents_HasHighChanceOfHighRoot()
    {
        var father = new CharacterData { SpiritualRoot = 90 };
        var mother = new CharacterData { SpiritualRoot = 90 };

        int sum = 0;
        for (int i = 0; i < 1000; i++)
        {
            sum += GeneticSystem.GenerateSpiritualRoot(father, mother);
        }
        int avg = sum / 1000;

        Assert.That(avg, Is.InRange(75, 105)); // 90 ± mutation (en pratique clampé à 100)
    }
}
```

---

## 🎯 Objectifs de couverture

| Domaine | Cible Phase 1 | Cible Phase 4 |
|---|---|---|
| **Data (POCOs, sérialisation)** | 100% | 100% |
| **Static / pure functions** (GeneticSystem, calculs) | 80% | 100% |
| **Managers (Singleton)** | 40% | 80% |
| **Combat actions** | 60% | 90% |
| **IA strategies** | 50% | 80% |
| **UI** | 0% | 30% (tests manuels privilégiés) |

---

## 🚫 Ce qu'on NE teste PAS

- Visuels (sprites, VFX, shaders) — tests manuels en Play Mode
- Audio (mix, fadeout) — tests manuels
- Performance (FPS, mémoire) — Unity Profiler en session dédiée
- Accessibility / responsive — tests manuels sur appareils

---

## 🔁 Commandes utiles

### Lancer tous les tests Edit Mode en CLI (pour CI)

```bash
Unity -batchmode -quit \
  -projectPath "/home/aymeric/Mirror-Legacy" \
  -runTests \
  -testPlatform EditMode \
  -testResults editmode-results.xml \
  -logFile editmode.log
```

### Lancer un seul test par nom

```bash
Unity -batchmode -quit \
  -projectPath "/home/aymeric/Mirror-Legacy" \
  -runTests \
  -testPlatform EditMode \
  -testFilter "MirrorChronicles.Tests.EditMode.GeneticSystemTests" \
  -logFile test.log
```

### Regarder les résultats

```bash
cat editmode-results.xml  # XML NUnit format
```

---

## 🪝 Pre-commit hook (suggéré)

Ajouter dans `.git/hooks/pre-commit` :

```bash
#!/bin/bash
# Run Edit Mode tests before every commit
Unity -batchmode -quit \
  -projectPath "$(pwd)" \
  -runTests \
  -testPlatform EditMode \
  -testResults .git/hooks/editmode-results.xml \
  -logFile .git/hooks/editmode.log

if [ $? -ne 0 ]; then
  echo "❌ Edit Mode tests failed. Commit aborted."
  echo "   See .git/hooks/editmode.log for details."
  exit 1
fi
```

(À activer après bootstrap Unity, puisque le projet doit d'abord compiler.)

---

## 📝 Checklist pré-merge PR (plus tard, quand on aura git remote)

- [ ] Tous les tests Edit Mode passent (100%)
- [ ] Tous les tests Play Mode passent (100%)
- [ ] Pas de `Debug.Log` temporaire oublié
- [ ] Pas de `TODO` sans numéro de ticket
- [ ] `NOT_DONE.md` mis à jour si une tâche a été complétée
- [ ] `DONE.md` mis à jour avec la nouvelle tâche
- [ ] `WORKED_LESSON.md` mis à jour si un bug non trivial a été rencontré
- [ ] `PACKAGE.md` mis à jour si un package a été ajouté/retiré

---

*Respecter cette discipline = économiser des heures de debug plus tard.*
*Le Test Runner d'Unity est ton meilleur ami : `Ctrl+R T T` pour l'ouvrir rapidement.*
