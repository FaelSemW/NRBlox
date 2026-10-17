#!/usr/bin/env bash
# ============================================================================
# NRBlox (Linux Native Edition) — One-Command CLI Installer & Auto-Launcher
# Author: FaelSemW (https://github.com/FaelSemW/NRBlox)
# Community Discord: https://discord.gg/M8j8JnQSCx
# Usage (1-Line CLI):
#   curl -fsSL https://raw.githubusercontent.com/FaelSemW/NRBlox/main/linux/install-linux.sh | bash
# ============================================================================

set -euo pipefail

REPO_RAW_BASE="https://raw.githubusercontent.com/FaelSemW/NRBlox/main"
INSTALL_DIR="${HOME}/.local/share/nrblox-linux"
BIN_DIR="${HOME}/.local/bin"
APPS_DIR="${HOME}/.local/share/applications"

echo "============================================================"
echo "  ⚡ NRBlox (Linux Native Edition) — CLI Auto-Installer"
echo "  GitHub: https://github.com/FaelSemW/NRBlox"
echo "  Discord: https://discord.gg/M8j8JnQSCx"
echo "============================================================"

# 1. Check & install system dependencies (python3, tkinter, flatpak, xdg-utils, curl)
install_deps() {
    if python3 -c "import tkinter" >/dev/null 2>&1 && command -v curl >/dev/null 2>&1; then
        return 0
    fi
    echo "[NRBlox-CLI] Verificando dependências do sistema (python3-tk, flatpak, xdg-utils)..."
    if command -v apt-get >/dev/null 2>&1; then
        sudo apt-get update -y && sudo apt-get install -y python3 python3-tk curl xdg-utils flatpak
    elif command -v pacman >/dev/null 2>&1; then
        sudo pacman -Sy --noconfirm python tk curl xdg-utils flatpak
    elif command -v dnf >/dev/null 2>&1; then
        sudo dnf install -y python3 python3-tkinter curl xdg-utils flatpak
    elif command -v zypper >/dev/null 2>&1; then
        sudo zypper install -y python3 python3-tk curl xdg-utils flatpak
    fi
}

install_deps || true

# 2. Ensure Sober (Flatpak) or Wine runner is ready
if command -v flatpak >/dev/null 2>&1; then
    flatpak remote-add --user --if-not-exists flathub https://dl.flathub.org/repo/flathub.flatpakrepo >/dev/null 2>&1 || true
    if ! flatpak list --app 2>/dev/null | grep -q "org.vinegarhq.Sober"; then
        echo "[NRBlox-CLI] Instalando runtime Sober (org.vinegarhq.Sober) via Flatpak..."
        flatpak install --user -y flathub org.vinegarhq.Sober || true
    fi
fi

# 3. Prepare directories
mkdir -p "${INSTALL_DIR}/src" "${INSTALL_DIR}/assets/icons" "${BIN_DIR}" "${APPS_DIR}"

# 4. Copy from local clone if available, otherwise download directly from GitHub
SCRIPT_DIR=""
if [ -n "${BASH_SOURCE[0]:-}" ] && [ -f "${BASH_SOURCE[0]}" ]; then
    SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
fi

if [ -n "${SCRIPT_DIR}" ] && [ -f "${SCRIPT_DIR}/src/nrblox_linux_app.py" ]; then
    echo "[NRBlox-CLI] Copiando arquivos locais para ${INSTALL_DIR}..."
    cp -r "${SCRIPT_DIR}/src/"* "${INSTALL_DIR}/src/"
    if [ -d "${SCRIPT_DIR}/../assets" ]; then
        cp -r "${SCRIPT_DIR}/../assets" "${INSTALL_DIR}/"
    fi
else
    echo "[NRBlox-CLI] Baixando motor nativo NRBlox Linux do GitHub..."
    curl -fsSL "${REPO_RAW_BASE}/linux/src/nrblox_linux_app.py" -o "${INSTALL_DIR}/src/nrblox_linux_app.py"
    curl -fsSL "${REPO_RAW_BASE}/assets/icons/IconNRBlox.ico" -o "${INSTALL_DIR}/assets/icons/IconNRBlox.ico" || true
fi

chmod +x "${INSTALL_DIR}/src/nrblox_linux_app.py"

# 5. Create executable wrapper in ~/.local/bin/nrblox and ~/.local/bin/nrblox-linux
cat > "${INSTALL_DIR}/nrblox-linux" << 'EOF'
#!/usr/bin/env bash
exec python3 "${HOME}/.local/share/nrblox-linux/src/nrblox_linux_app.py" "$@"
EOF
chmod +x "${INSTALL_DIR}/nrblox-linux"

ln -sf "${INSTALL_DIR}/nrblox-linux" "${BIN_DIR}/nrblox-linux"
ln -sf "${INSTALL_DIR}/nrblox-linux" "${BIN_DIR}/nrblox"

# Ensure ~/.local/bin is in PATH
for rc in "${HOME}/.bashrc" "${HOME}/.zshrc"; do
    if [ -f "${rc}" ] && ! grep -q ".local/bin" "${rc}"; then
        echo 'export PATH="${HOME}/.local/bin:${PATH}"' >> "${rc}"
    fi
done
export PATH="${BIN_DIR}:${PATH}"

# 6. Create Desktop entry & register roblox-player:// protocol
cat > "${APPS_DIR}/nrblox-linux.desktop" << EOF
[Desktop Entry]
Name=NRBlox
Comment=NRBlox — Native Roblox Launcher, FastFlag & Mod Controller
Exec=${BIN_DIR}/nrblox-linux %u
Terminal=false
Type=Application
Categories=Game;Utility;
MimeType=x-scheme-handler/roblox-player;x-scheme-handler/roblox;x-scheme-handler/nrblox;
StartupWMClass=NRBlox
EOF

if [ -d "${HOME}/Desktop" ]; then
    cp "${APPS_DIR}/nrblox-linux.desktop" "${HOME}/Desktop/NRBlox.desktop" || true
    chmod +x "${HOME}/Desktop/NRBlox.desktop" || true
fi

if command -v xdg-mime >/dev/null 2>&1; then
    xdg-mime default nrblox-linux.desktop x-scheme-handler/roblox-player || true
    xdg-mime default nrblox-linux.desktop x-scheme-handler/roblox || true
    xdg-mime default nrblox-linux.desktop x-scheme-handler/nrblox || true
fi

if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database "${APPS_DIR}" >/dev/null 2>&1 || true
fi

echo ""
echo "✅ [NRBlox-CLI] Instalação concluída com sucesso!"
echo "   • Comando global no terminal: nrblox (ou nrblox-linux)"
echo "   • Protocolos roblox-player:// e roblox:// registrados!"
echo ""

# 7. Auto-launch NRBlox GUI if an X11 or Wayland display is active
if [ -n "${DISPLAY:-}" ] || [ -n "${WAYLAND_DISPLAY:-}" ]; then
    echo "🚀 [NRBlox-CLI] Abrindo o launcher NRBlox..."
    nohup "${BIN_DIR}/nrblox-linux" >/dev/null 2>&1 &
fi
