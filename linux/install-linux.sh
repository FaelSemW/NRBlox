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

# Clean up any stuck Wine configuration popups from previous runs
pkill -9 -f wine 2>/dev/null || true

# 0. On Live RAM systems (Slax), mount the 32GB virtual disk (/dev/sda) if available so disk never fills up
if [ -d /run/initramfs/memory ] && [ -b /dev/sda ] && [ "$(id -u)" -eq 0 ]; then
    mkdir -p /mnt/nrblox-disk
    if ! mountpoint -q /mnt/nrblox-disk 2>/dev/null; then
        if ! blkid /dev/sda >/dev/null 2>&1; then
            echo "[NRBlox-CLI] Formatando disco virtual de 32 GB (/dev/sda) para armazenamento nativo..."
            mkfs.ext4 -F -q /dev/sda >/dev/null 2>&1 || true
        fi
        mount /dev/sda /mnt/nrblox-disk 2>/dev/null || true
    fi
    if mountpoint -q /mnt/nrblox-disk 2>/dev/null; then
        mkdir -p /mnt/nrblox-disk/nrblox-linux "${HOME}/.local/share"
        if [ ! -L "${INSTALL_DIR}" ]; then
            rm -rf "${INSTALL_DIR}" 2>/dev/null || true
            ln -s /mnt/nrblox-disk/nrblox-linux "${INSTALL_DIR}"
        fi
    fi
fi

# 1. Install native Linux dependencies (skip on immutable distros like Bazzite/SteamOS where python3 & flatpak are built-in)
install_deps() {
    if [ -f /run/ostree-booted ] || command -v rpm-ostree >/dev/null 2>&1; then
        echo "[NRBlox-CLI] Sistema imutável (Bazzite / Fedora Atomic) detectado — usando Python3 e Flatpak nativos!"
        return 0
    fi
    if python3 -c "import tkinter" >/dev/null 2>&1 && command -v gcc >/dev/null 2>&1 && [ -f /usr/include/stdio.h ] && command -v flatpak >/dev/null 2>&1; then
        return 0
    fi
    echo "[NRBlox-CLI] Instalando dependências nativas do Linux (python3-tk, gcc, libc6-dev, curl, xdg-utils)..."
    if command -v apt-get >/dev/null 2>&1; then
        ${SUDO} apt-get update -o Acquire::AllowInsecureRepositories=true >/dev/null 2>&1 || true
        if [ -d /run/initramfs/memory ] || grep -q "slax" /etc/os-release 2>/dev/null || [ "$(hostname 2>/dev/null)" = "slax" ]; then
            echo "[NRBlox-CLI] Sistema Debian Live (Slax) detectado — instalando pacotes essenciais leves..."
            DEBIAN_FRONTEND=noninteractive ${SUDO} apt-get install -y python3 python3-tk gcc libc6-dev curl ca-certificates xdg-utils || true
        else
            DEBIAN_FRONTEND=noninteractive ${SUDO} apt-get install -y python3 python3-tk gcc libc6-dev g++-mingw-w64-x86-64 wine64 flatpak curl ca-certificates xdg-utils || \
            DEBIAN_FRONTEND=noninteractive ${SUDO} apt-get install -y python3 python3-tk gcc libc6-dev wine64 flatpak curl ca-certificates xdg-utils || \
            DEBIAN_FRONTEND=noninteractive ${SUDO} apt-get install -y python3 python3-tk gcc libc6-dev curl ca-certificates || true
        fi
    elif command -v pacman >/dev/null 2>&1; then
        ${SUDO} pacman -Sy --noconfirm python tk gcc mingw-w64-gcc wine flatpak curl xdg-utils || true
    elif command -v dnf >/dev/null 2>&1; then
        ${SUDO} dnf install -y python3 python3-tkinter gcc glibc-devel mingw64-gcc-c++ wine flatpak curl xdg-utils || true
    elif command -v zypper >/dev/null 2>&1; then
        ${SUDO} zypper install -y python3 python3-tk gcc glibc-devel cross-x86_64-w64-mingw32-gcc-c++ wine flatpak curl xdg-utils || true
    fi
}

install_deps

# 1b. Configure Bionic ELF backend for Roblox Player (skip on Live RAM systems to avoid filling RAM disk)
if command -v flatpak >/dev/null 2>&1 && [ ! -f /.dockerenv ] && [ ! -d /run/initramfs/memory ]; then
    flatpak remote-add --user --if-not-exists flathub https://dl.flathub.org/repo/flathub.flatpakrepo >/dev/null 2>&1 || true
    if ! flatpak list --app 2>/dev/null | grep -q "org.vinegarhq.Sober"; then
        echo "[NRBlox-CLI] Configurando motor ELF Linux para o Roblox Player..."
        flatpak install --user -y flathub org.vinegarhq.Sober >/dev/null 2>&1 || true
    fi
fi

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
    fetch_url() {
        if command -v curl >/dev/null 2>&1; then
            curl -fsSL "$1" -o "$2"
        else
            wget -qO "$2" "$1"
        fi
    }
    fetch_url "${REPO_RAW_BASE}/linux/src/nrblox_linux_app.py" "${INSTALL_DIR}/src/nrblox_linux_app.py"
    fetch_url "${REPO_RAW_BASE}/linux/runtime/nrblox_linux_core.c" "${INSTALL_DIR}/runtime/nrblox_linux_core.c"
    fetch_url "${REPO_RAW_BASE}/linux/runtime/nrblox_wine_cloak.cpp" "${INSTALL_DIR}/runtime/nrblox_wine_cloak.cpp"
    fetch_url "${REPO_RAW_BASE}/assets/icons/IconNRBlox.ico" "${INSTALL_DIR}/assets/icons/IconNRBlox.ico" || true
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
ln -sf "${INSTALL_DIR}/src/nrblox_linux_app.py" "${INSTALL_DIR}/nrblox-linux"
ln -sf "${INSTALL_DIR}/src/nrblox_linux_app.py" "${BIN_DIR}/nrblox-linux"
ln -sf "${INSTALL_DIR}/src/nrblox_linux_app.py" "${BIN_DIR}/nrblox"
if [ -w /usr/local/bin ]; then
    ln -sf "${INSTALL_DIR}/src/nrblox_linux_app.py" /usr/local/bin/nrblox-linux || true
    ln -sf "${INSTALL_DIR}/src/nrblox_linux_app.py" /usr/local/bin/nrblox || true
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
Exec=${INSTALL_DIR}/src/nrblox_linux_app.py %u
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
    if command -v setsid >/dev/null 2>&1; then
        setsid python3 "${INSTALL_DIR}/src/nrblox_linux_app.py" >/tmp/nrblox-gui.log 2>&1 < /dev/null &
    else
        nohup python3 "${INSTALL_DIR}/src/nrblox_linux_app.py" >/tmp/nrblox-gui.log 2>&1 < /dev/null &
    fi
    sleep 1
else
    echo "⚠️ Nenhum monitor X11/Wayland detectado nesta sessão. Execute 'nrblox' para abrir o menu!"
fi
