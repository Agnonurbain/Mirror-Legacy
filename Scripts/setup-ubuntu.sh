#!/usr/bin/env bash
set -euo pipefail

echo "🔧 Configuration Ubuntu pour Unity 6 + Claude Code..."
echo "======================================================"

# Couleurs
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Dépendances système
echo -e "${YELLOW}Installation des dépendances système...${NC}"
sudo apt update && sudo apt install -y \
    jq timeout flock xvfb git git-lfs \
    python3 python3-pip python3-venv \
    libgconf-2-4 libsecret-1-0 libgtk-3-0 \
    libnss3 libxss1 libasound2 \
    libgl1-mesa-dri libgl1-mesa-glx \
    || { echo -e "${RED}Échec installation dépendances${NC}"; exit 1; }

# Installer pip packages
echo -e "${YELLOW}Installation des outils Python...${NC}"
pip3 install --user regex || true

# Config Git LFS
if command -v git-lfs &> /dev/null; then
    git lfs install
    echo -e "${GREEN}✓ Git LFS configuré${NC}"
fi

# Créer dossiers
mkdir -p claude-output .claude

# Vérifier Unity Hub
if command -v unityhub &> /dev/null; then
    echo -e "${GREEN}✓ Unity Hub détecté${NC}"
else
    echo -e "${YELLOW}⚠ Unity Hub non détecté${NC}"
    echo "Installez-le depuis https://unity.com/download"
fi

# Vérifier Unity
UNITY_PATH=""
if [[ -n "${UNITY_PATH:-}" ]]; then
    echo -e "${GREEN}✓ UNITY_PATH défini: $UNITY_PATH${NC}"
elif [[ -x "/home/aymeric/Unity/Hub/Editor/6000.4.2f1/Editor/Unity" ]]; then
    UNITY_PATH="/home/aymeric/Unity/Hub/Editor/6000.4.2f1/Editor/Unity"
    echo -e "${GREEN}✓ Unity 6.4 détecté automatiquement${NC}"
else
    echo -e "${YELLOW}⚠ Unity non trouvé. Définissez UNITY_PATH${NC}"
fi

# Rendre les scripts exécutables
chmod +x Scripts/unity-claude.sh 2>/dev/null || true

echo ""
echo -e "${GREEN}✅ Setup terminé !${NC}"
echo ""
echo "Prochaines étapes:"
echo "1. Lancez ./Scripts/unity-claude.sh Compile"
echo "2. Installez Newtonsoft.Json via Package Manager"
echo "3. Lisez CLAUDE.md pour les commandes disponibles"
echo ""