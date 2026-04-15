#!/usr/bin/env bash
set -euo pipefail

# Couleurs
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# 🔍 Auto-détection Unity
detect_unity() {
    if [[ -n "${UNITY_PATH:-}" ]]; then 
        echo "$UNITY_PATH"
        return
    fi
    
    # Chemins courants Unity 6 sur Ubuntu
    for dir in \
        "/home/aymeric/Unity/Hub/Editor/6000.4.2f1/Editor/Unity" \
        "$HOME/Unity/Hub/Editor/6000.4.2f1/Editor/Unity" \
        "$HOME/Unity/Hub/Editor/*/Editor/Unity" \
        "/opt/unity/Editor/Unity"
    do
        if [[ -x "$dir" ]]; then
            echo "$dir"
            return
        fi
    done
    
    echo "ERROR: Unity non trouvé. Définissez UNITY_PATH" >&2
    echo "Exemple: export UNITY_PATH=/home/aymeric/Unity/Hub/Editor/6000.4.2f1/Editor/Unity" >&2
    exit 1
}

UNITY_BIN=$(detect_unity)
PROJECT_DIR="$(pwd)"
COMMAND="${1:-Compile}"

# Resolve fully-qualified method path.
# - Input with a dot is treated as a full path (e.g. ClaudeBridge.ClaudeSceneBootstrapper.BootstrapClanDomain).
# - Bare names map to methods on ClaudeBridge.ClaudeCodeBridge.
# - Known aliases route to their owning class so callers don't need to remember layout.
case "$COMMAND" in
    BootstrapClanDomain) METHOD_PATH="ClaudeBridge.ClaudeSceneBootstrapper.BootstrapClanDomain" ;;
    BuildUIPlaceholder) METHOD_PATH="ClaudeBridge.ClaudeUIBuilder.BuildUIPlaceholder" ;;
    ImportTMPEssentials) METHOD_PATH="ClaudeBridge.ClaudeUIBuilder.ImportTMPEssentials" ;;
    BuildLinux64|BuildWindows64) METHOD_PATH="ClaudeBridge.ClaudeBuildManager.${COMMAND}" ;;
    RunEditModeTests|RunPlayModeTests) METHOD_PATH="ClaudeBridge.ClaudeTestRunner.${COMMAND}" ;;
    ScanLegacyPackages|AutoMigrate) METHOD_PATH="ClaudeBridge.UpmMigrator.${COMMAND}" ;;
    *.*) METHOD_PATH="$COMMAND" ;;
    *) METHOD_PATH="ClaudeBridge.ClaudeCodeBridge.${COMMAND}" ;;
esac

LOG_FILE=$(mktemp /tmp/unity_claude_XXXX.log)
OUTPUT_FILE="$PROJECT_DIR/claude-output/result.json"
TIMEOUT="${TIMEOUT:-600}" # 10 min max par défaut
LOCK_FILE="$PROJECT_DIR/.unity.lock"

cleanup() {
    rm -f "$LOG_FILE"
    rm -f "$LOCK_FILE"
}
trap cleanup EXIT

# Open lock file on fd 200 so flock -n 200 actually works
exec 200>"$LOCK_FILE"

echo -e "${BLUE}╔════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║   Unity 6 + Claude Code Bridge        ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════╝${NC}"
echo ""
echo -e "${GREEN}🎮 Unity:${NC} $($UNITY_BIN -version 2>/dev/null | head -1 || echo 'batch mode')"
echo -e "${GREEN}📂 Projet:${NC} $PROJECT_DIR"
echo -e "${GREEN}⚙️ Commande:${NC} $COMMAND"
echo -e "${GREEN}⏱️ Timeout:${NC} ${TIMEOUT}s"
echo ""

# Créer output dir
mkdir -p "$PROJECT_DIR/claude-output"

# Lancement batch sécurisé avec lock
echo -e "${YELLOW}▶️ Démarrage Unity en batch mode...${NC}"
if flock -n 200 2>/dev/null; then
    # -quit omitted on purpose: async work (ImportPackage callbacks, etc.) must
    # complete before termination. WriteJson inside the bridge always calls
    # EditorApplication.Exit, so Unity terminates deterministically on success.
    # The `timeout` wrapper guards against hangs.
    timeout "$TIMEOUT" "$UNITY_BIN" \
        -batchmode \
        -nographics \
        -projectPath "$PROJECT_DIR" \
        -executeMethod "$METHOD_PATH" \
        -logFile "$LOG_FILE" 2>/dev/null || true
else
    echo -e "${RED}⚠️  Une instance Unity est déjà en cours${NC}"
    exit 1
fi

# Vérification sortie
echo ""
if [[ -f "$OUTPUT_FILE" ]]; then
    echo -e "${GREEN}✅ Résultat:${NC}"
    echo "─────────────────────────────────────"
    
    if command -v jq &> /dev/null; then
        cat "$OUTPUT_FILE" | jq '.'
        STATUS=$(cat "$OUTPUT_FILE" | jq -r '.status')
    else
        cat "$OUTPUT_FILE"
        STATUS=$(grep -o '"status":"[^"]*"' "$OUTPUT_FILE" | cut -d'"' -f4)
    fi
    
    echo "─────────────────────────────────────"
    
    # Cleanup
    rm -f "$OUTPUT_FILE"
    
    if [[ "$STATUS" == "success" ]]; then
        echo -e "${GREEN}✓ Commande réussie${NC}"
        exit 0
    else
        echo -e "${RED}✗ Commande échouée${NC}"
        exit 1
    fi
else
    echo -e "${RED}❌ Aucune sortie générée${NC}"
    echo ""
    echo -e "${YELLOW}Dernières lignes du log:${NC}"
    tail -c 2000 "$LOG_FILE" 2>/dev/null || echo "Log vide"
    
    # Créer un JSON d'erreur
    echo "{\"status\":\"error\",\"message\":\"Timeout ou échec batch\",\"log\":\"$(tail -c 500 "$LOG_FILE" | sed 's/"/\\"/g' | tr '\n' ' ')\"}" | jq '.' 2>/dev/null || true
    
    exit 1
fi