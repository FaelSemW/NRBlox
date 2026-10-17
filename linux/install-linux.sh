#!/usr/bin/env bash
# ============================================================================
# NRBlox (Linux Native Edition) — Official One-Command CLI Installer
# Author: FaelSemW (https://github.com/FaelSemW/NRBlox)
# Community Discord: https://discord.gg/M8j8JnQSCx
# License: MIT (100% Open Source)
# ============================================================================
# Usage (1-Line CLI):
#   curl -fsSL https://raw.githubusercontent.com/FaelSemW/NRBlox/main/linux/install-linux.sh | bash
# ============================================================================

set -uo pipefail

REPO_RAW_BASE="https://raw.githubusercontent.com/FaelSemW/NRBlox/main"
INSTALL_DIR="${HOME}/.local/share/nrblox-linux"
BIN_DIR="${HOME}/.local/bin"
APPS_DIR="${HOME}/.local/share/applications"

if [ "$(id -u)" -eq 0 ]; then
    SUDO=""
else
    SUDO="sudo"
fi

echo "============================================================"
echo "  ⚡ NRBlox (Linux Native Edition) — Official CLI Installer"
echo "  GitHub: https://github.com/FaelSemW/NRBlox"
echo "  Discord: https://discord.gg/M8j8JnQSCx"
echo "============================================================"

# 1. Install native Linux dependencies (even if third-party apt repos like Chrome have GPG warnings)
install_deps() {
    echo "[NRBlox-CLI] Instalando dependências nativas do Linux (python3-tk, gcc, wine64, xdg-utils)..."
    if command -v apt-get >/dev/null 2>&1; then
        # Ignore GPG errors from unrelated third-party repos (e.g., dl.google.com/linux/chrome)
        ${SUDO} apt-get update -o Acquire::AllowInsecureRepositories=true >/dev/null 2>&1 || true
        DEBIAN_FRONTEND=noninteractive ${SUDO} apt-get install -y python3 python3-tk gcc g++-mingw-w64-x86-64 wine64 curl xdg-utils || \
        DEBIAN_FRONTEND=noninteractive ${SUDO} apt-get install -y python3 python3-tk gcc wine64 curl xdg-utils || \
        DEBIAN_FRONTEND=noninteractive ${SUDO} apt-get install -y python3 python3-tk gcc curl || true
    elif command -v pacman >/dev/null 2>&1; then
        ${SUDO} pacman -Sy --noconfirm python tk gcc mingw-w64-gcc wine curl xdg-utils || true
    elif command -v dnf >/dev/null 2>&1; then
        ${SUDO} dnf install -y python3 python3-tkinter gcc mingw64-gcc-c++ wine curl xdg-utils || true
    elif command -v zypper >/dev/null 2>&1; then
        ${SUDO} zypper install -y python3 python3-tk gcc cross-x86_64-w64-mingw32-gcc-c++ wine curl xdg-utils || true
    fi
}

install_deps

# 2. Prepare NRBlox directories
mkdir -p "${INSTALL_DIR}/src" "${INSTALL_DIR}/runtime" "${INSTALL_DIR}/bin" "${INSTALL_DIR}/assets/icons" "${BIN_DIR}" "${APPS_DIR}"

# 3. Copy from local clone or download directly from GitHub
SCRIPT_DIR=""
if [ -n "${BASH_SOURCE[0]:-}" ] && [ -f "${BASH_SOURCE[0]}" ]; then
    SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
fi

if [ -n "${SCRIPT_DIR}" ] && [ -f "${SCRIPT_DIR}/src/nrblox_linux_app.py" ]; then
    echo "[NRBlox-CLI] Copiando código-fonte local para ${INSTALL_DIR}..."
    cp -r "${SCRIPT_DIR}/src/"* "${INSTALL_DIR}/src/"
    cp -r "${SCRIPT_DIR}/runtime/"* "${INSTALL_DIR}/runtime/"
    if [ -d "${SCRIPT_DIR}/../assets" ]; then
        cp -r "${SCRIPT_DIR}/../assets" "${INSTALL_DIR}/"
    fi
else
    echo "[NRBlox-CLI] Baixando motor nativo NRBlox Linux e código-fonte C/C++ do GitHub..."
    curl -fsSL "${REPO_RAW_BASE}/linux/src/nrblox_linux_app.py" -o "${INSTALL_DIR}/src/nrblox_linux_app.py"
    curl -fsSL "${REPO_RAW_BASE}/linux/runtime/nrblox_linux_core.c" -o "${INSTALL_DIR}/runtime/nrblox_linux_core.c"
    curl -fsSL "${REPO_RAW_BASE}/linux/runtime/nrblox_wine_cloak.cpp" -o "${INSTALL_DIR}/runtime/nrblox_wine_cloak.cpp"
    curl -fsSL "${REPO_RAW_BASE}/assets/icons/IconNRBlox.ico" -o "${INSTALL_DIR}/assets/icons/IconNRBlox.ico" || true
fi

chmod +x "${INSTALL_DIR}/src/nrblox_linux_app.py"

# 4. Compile NRBlox Native Linux Core (`nrblox-core`) and Open-Source `WebView2Loader.dll`
if command -v gcc >/dev/null 2>&1; then
    echo "[NRBlox-CLI] Compilando binário nativo Linux: nrblox-core..."
    gcc -O2 "${INSTALL_DIR}/runtime/nrblox_linux_core.c" -o "${INSTALL_DIR}/bin/nrblox-core" || true
    chmod +x "${INSTALL_DIR}/bin/nrblox-core" 2>/dev/null || true
fi

if command -v x86_64-w64-mingw32-g++ >/dev/null 2>&1; then
    echo "[NRBlox-CLI] Compilando WebView2Loader.dll open-source e NTDLL Cloak..."
    x86_64-w64-mingw32-g++ -shared -O2 "${INSTALL_DIR}/runtime/nrblox_wine_cloak.cpp" \
        -o "${INSTALL_DIR}/bin/WebView2Loader.dll" -lole32 -static-libgcc -static-libstdc++ || true
fi

# 5. Create global CLI command `nrblox` and `nrblox-linux` (also in /usr/local/bin so root & all users have it in PATH immediately)
cat > "${INSTALL_DIR}/nrblox-linux" << 'EOF'
#!/usr/bin/env bash
# Auto-detect active X11 display if DISPLAY is not set (common in VNC / root shells)
if [ -z "${DISPLAY:-}" ] && [ -z "${WAYLAND_DISPLAY:-}" ] && [ -d /tmp/.X11-unix ]; then
    for sock in /tmp/.X11-unix/X*; do
        if [ -e "$sock" ]; then
            disp_num="${sock#/tmp/.X11-unix/X}"
            export DISPLAY=":${disp_num}"
            break
        fi
    done
fi
exec python3 "${HOME}/.local/share/nrblox-linux/src/nrblox_linux_app.py" "$@"
EOF
chmod +x "${INSTALL_DIR}/nrblox-linux"

ln -sf "${INSTALL_DIR}/nrblox-linux" "${BIN_DIR}/nrblox-linux"
ln -sf "${INSTALL_DIR}/nrblox-linux" "${BIN_DIR}/nrblox"
if [ -w /usr/local/bin ]; then
    ln -sf "${INSTALL_DIR}/nrblox-linux" /usr/local/bin/nrblox-linux || true
    ln -sf "${INSTALL_DIR}/nrblox-linux" /usr/local/bin/nrblox || true
fi

for rc in "${HOME}/.bashrc" "${HOME}/.zshrc"; do
    if [ -f "${rc}" ] && ! grep -q ".local/bin" "${rc}"; then
        echo 'export PATH="${HOME}/.local/bin:${PATH}"' >> "${rc}"
    fi
done
export PATH="${BIN_DIR}:/usr/local/bin:${PATH}"

# 6. Register Desktop Entry & XDG URI Protocols (roblox-player://, roblox://, nrblox://)
cat > "${APPS_DIR}/nrblox-linux.desktop" << EOF
[Desktop Entry]
Name=NRBlox
Comment=NRBlox — Official Native Roblox Launcher, FastFlag & Mod Controller
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

# Auto-detect active X11 display if DISPLAY is unset in current shell
if [ -z "${DISPLAY:-}" ] && [ -z "${WAYLAND_DISPLAY:-}" ] && [ -d /tmp/.X11-unix ]; then
    for sock in /tmp/.X11-unix/X*; do
        if [ -e "$sock" ]; then
            disp_num="${sock#/tmp/.X11-unix/X}"
            export DISPLAY=":${disp_num}"
            break
        fi
    done
fi

echo ""
echo "✅ [NRBlox-CLI] Instalação concluída com sucesso!"
echo "   • Motor Nativo Compilado: ${INSTALL_DIR}/bin/nrblox-core"
echo "   • Comando Global: digite 'nrblox' no terminal a qualquer momento"
echo ""

# 7. Auto-launch NRBlox GUI right after CLI installation
if [ -n "${DISPLAY:-}" ] || [ -n "${WAYLAND_DISPLAY:-}" ]; then
    echo "🚀 [NRBlox-CLI] Abrindo a janela gráfica do NRBlox em DISPLAY=${DISPLAY:-wayland}..."
    nohup "${BIN_DIR}/nrblox-linux" >/tmp/nrblox-gui.log 2>&1 &
    sleep 1
else
    echo "⚠️ Nenhum monitor X11/Wayland detectado nesta sessão. Execute 'nrblox' para abrir o menu!"
fi
