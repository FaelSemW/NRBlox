#!/usr/bin/env bash
# ============================================================================
# NRBlox (Linux Native Edition) — System & XDG Protocol Installer
# Author: FaelSemW (https://github.com/FaelSemW/NRBlox)
# Community Discord: https://discord.gg/M8j8JnQSCx
# ============================================================================

set -euo pipefail

LINUX_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${LINUX_DIR}/.." && pwd)"
INSTALL_DIR="${HOME}/.local/share/nrblox-linux"
BIN_DIR="${HOME}/.local/bin"
APPS_DIR="${HOME}/.local/share/applications"

echo "============================================================"
echo "  NRBlox (Linux Native Edition) — Instalador XDG"
echo "============================================================"

mkdir -p "${INSTALL_DIR}" "${BIN_DIR}" "${APPS_DIR}"

cp -r "${LINUX_DIR}/src" "${INSTALL_DIR}/"
cp -r "${REPO_ROOT}/assets" "${INSTALL_DIR}/"
cp "${LINUX_DIR}/nrblox-linux" "${INSTALL_DIR}/nrblox-linux"
chmod +x "${INSTALL_DIR}/nrblox-linux"

ln -sf "${INSTALL_DIR}/nrblox-linux" "${BIN_DIR}/nrblox-linux"

sed "s|Exec=nrblox-linux %u|Exec=${BIN_DIR}/nrblox-linux %u|g" \
    "${LINUX_DIR}/nrblox-linux.desktop" > "${APPS_DIR}/nrblox-linux.desktop"

if command -v xdg-mime >/dev/null 2>&1; then
    xdg-mime default nrblox-linux.desktop x-scheme-handler/roblox-player
    xdg-mime default nrblox-linux.desktop x-scheme-handler/roblox
    xdg-mime default nrblox-linux.desktop x-scheme-handler/nrblox
    echo "[NRBlox-Linux] Protocolos roblox-player:// e roblox:// registrados no XDG!"
fi

if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database "${APPS_DIR}" || true
fi

echo "[NRBlox-Linux] Instalação concluída! Execute 'nrblox-linux' para abrir."
