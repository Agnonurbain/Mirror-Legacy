# 🎮 Unity 6 + Claude Code (Ubuntu/Linux)

## 📋 Configuration
- **Unity Version**: 6000.4.2f1 (Unity 6)
- **Plateforme**: Ubuntu 22.04+
- **Wrapper**: `./Scripts/unity-claude.sh <Commande>`

## 📋 Commandes disponibles

| Commande             | Description                               | Exit Code | Temps estimé |
| -------------------- | ----------------------------------------- | --------- | ------------ |
| `Compile`            | Compile C#, retourne erreurs/warnings     | 0/1       | 30-60s       |
| `BuildLinux64`       | Build standalone Linux64 dans `Builds/`   | 0/1       | 2-5min       |
| `BuildWindows64`     | Build standalone Windows64                | 0/1       | 2-5min       |
| `RunEditModeTests`   | Lance tests EditMode (rapide, sans rendu) | 0/1       | 1-3min       |
| `RunPlayModeTests`   | Lance tests PlayMode (headless avec Xvfb) | 0/1       | 3-10min      |
| `FindAssets`         | Liste tous les assets (exclut bridge)     | 0         | 5-10s        |
| `ClearCache`         | Vide console & refresh AssetDatabase      | 0         | 2-5s         |
| `ScanLegacyPackages` | Détecte packages non-UPM                  | 0         | 10-30s       |
| `AutoMigrate`        | Migre vers UPM automatiquement            | 0/1       | 1-5min       |

## 🛡️ Règles de sécurité impératives

### ✅ TOUJOURS
1. Exécuter `Compile` après modification de `.cs`
2. Versionner via Git avant chaque build/test important
3. Vérifier `claude-output/result.json` après chaque commande
4. Utiliser `TIMEOUT=900` pour les builds complexes

### ❌ JAMAIS
1. Modifier manuellement `.unity`, `.prefab`, `.asset` (YAML sérialisé)
2. Modifier/supprimer `.meta` sans utiliser `AssetDatabase`
3. Éditer `Library/` ou `Temp/`
4. Lancer plusieurs instances Unity en parallèle (lock file)

## 🔄 Workflow recommandé

### Développement quotidien
```bash
# 1. Modifier scripts C#
# 2. Compiler
./Scripts/unity-claude.sh Compile

# 3. Si erreurs → lire JSON → corriger → recompiler
# 4. Tester
./Scripts/unity-claude.sh RunEditModeTests

# 5. Commit
git add -A && git commit -m "feat: description"