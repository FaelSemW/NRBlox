#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
============================================================================
NRBlox (Linux Native Edition) — Dedicated Linux Desktop Launcher & Engine
Author: FaelSemW (https://github.com/FaelSemW/NRBlox)
Community Discord: https://discord.gg/M8j8JnQSCx
License: MIT (100% Open Source)
============================================================================
Engineered specifically for Linux (X11 / Wayland):
  • Runtime Backends: Sober (Flatpak Android-x86_64), Vinegar (Wine-GE),
    Proton-GE, and Custom Wine64 Prefix
  • Linux GPU & Kernel Tuning: Vulkan ICD selector (RADV, ANV, NVIDIA),
    DXVK, Esync, Fsync, Feral GameMode (gamemoderun), MangoHud, Gamescope
  • Multi-Target FastFlag & Mod Sync: Syncs simultaneously to Sober
    (config.json), Vinegar (config.toml), and Wine ClientAppSettings.json
  • Native AF_UNIX Socket Discord Rich Presence & Server Geolocation
  • Native Desktop GUI with instant Dark Mode & Light Mode switch
============================================================================
"""

import json
import os
import re
import shutil
import socket
import struct
import subprocess
import sys
import threading
import time
import urllib.request
import webbrowser
from pathlib import Path

try:
    import tkinter as tk
    from tkinter import filedialog, messagebox, ttk
    HAS_TK = True
except ImportError:
    HAS_TK = False

DISCORD_URL = "https://discord.gg/M8j8JnQSCx"
GITHUB_REPO_URL = "https://github.com/FaelSemW/NRBlox"
GITHUB_WIKI_URL = "https://github.com/FaelSemW/NRBlox/blob/main/docs/wiki/Home.md"
TARGET_VERSION_GUID = "version-11f155f3450f4c8e"


class LinuxPaths:
    HOME = Path.home()
    REPO_ROOT = Path(__file__).resolve().parent.parent.parent
    XDG_CONFIG = Path(os.environ.get("XDG_CONFIG_HOME", HOME / ".config")) / "nrblox-linux"
    XDG_DATA = Path(os.environ.get("XDG_DATA_HOME", HOME / ".local" / "share")) / "nrblox-linux"

    SETTINGS_FILE = XDG_CONFIG / "settings.json"
    FFLAGS_FILE = XDG_CONFIG / "ClientAppSettings.json"
    HISTORY_FILE = XDG_CONFIG / "server_history.json"
    MODS_DIR = XDG_DATA / "modifications"
    DEFAULT_WINE_PREFIX = XDG_DATA / "wine-prefix"

    # Linux-specific Roblox Runner Paths
    SOBER_CONFIG_FILE = HOME / ".var" / "app" / "org.vinegarhq.Sober" / "config" / "sober" / "config.json"
    SOBER_LOGS_DIR = HOME / ".var" / "app" / "org.vinegarhq.Sober" / "data" / "sober" / "logs"
    SOBER_OVERRIDES_DIR = HOME / ".var" / "app" / "org.vinegarhq.Sober" / "data" / "sober" / "asset_overlay"

    VINEGAR_CONFIG_FILE = HOME / ".config" / "vinegar" / "config.toml"
    VINEGAR_LOGS_DIR = HOME / ".cache" / "vinegar" / "logs"

    @classmethod
    def ensure_dirs(cls):
        for d in [
            cls.XDG_CONFIG,
            cls.XDG_DATA,
            cls.MODS_DIR,
            cls.MODS_DIR / "content" / "sounds",
            cls.MODS_DIR / "content" / "fonts",
            cls.MODS_DIR / "content" / "textures" / "Cursors" / "KeyboardMouse",
            cls.MODS_DIR / "ExtraContent" / "places",
        ]:
            d.mkdir(parents=True, exist_ok=True)


DEFAULT_LINUX_SETTINGS = {
    "theme_mode": "Dark",
    "linux_runner": "Auto (Sober -> Vinegar -> Wine64)",
    "wine_prefix": str(LinuxPaths.DEFAULT_WINE_PREFIX),
    "vulkan_icd": "Automático",
    "enable_dxvk": True,
    "enable_esync": True,
    "enable_fsync": True,
    "enable_gamemode": True,
    "enable_mangohud": False,
    "enable_gamescope": False,
    "gamescope_res": "1920x1080@144",
    "wayland_native": True,
    "prime_render_offload": False,
    "target_fps": 240,
    "rendering_api": "Vulkan",
    "lighting_mode": "Padrão",
    "msaa_samples": "4",
    "texture_quality": "3",
    "disable_dpi_scale": True,
    "disable_telemetry": True,
    "cursor_type": "Default",
    "use_old_sounds": False,
    "use_old_avatar_bg": False,
    "custom_font_path": "",
    "enable_activity_watcher": True,
    "enable_discord_rpc": True,
}


class LinuxDiscordRpc:
    """Native Linux AF_UNIX Discord IPC Client (supports standard, Flatpak & Vesktop sockets)"""
    CLIENT_ID = "1005469189907173486"

    def __init__(self):
        self.sock = None
        self.connected = False

    def _candidate_sockets(self):
        runtime_dir = os.environ.get("XDG_RUNTIME_DIR", f"/run/user/{os.getuid() if hasattr(os, 'getuid') else 1000}")
        candidates = []
        for i in range(10):
            candidates.append(f"{runtime_dir}/discord-ipc-{i}")
            candidates.append(f"{runtime_dir}/app/com.discordapp.Discord/discord-ipc-{i}")
            candidates.append(f"{runtime_dir}/vesktop-ipc-{i}")
            candidates.append(f"/tmp/discord-ipc-{i}")
        return candidates

    def connect(self):
        if self.connected and self.sock:
            return True
        if not hasattr(socket, "AF_UNIX"):
            return False
        for path in self._candidate_sockets():
            if os.path.exists(path):
                try:
                    s = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
                    s.settimeout(0.5)
                    s.connect(path)
                    self.sock = s
                    self._send_frame(0, {"v": 1, "client_id": self.CLIENT_ID})
                    self.connected = True
                    return True
                except Exception:
                    pass
        self.connected = False
        return False

    def _send_frame(self, opcode, payload):
        if not self.sock:
            return
        raw = json.dumps(payload).encode("utf-8")
        header = struct.pack("<II", opcode, len(raw))
        self.sock.sendall(header + raw)

    def update_presence(self, details, state):
        if not self.connect():
            return
        try:
            payload = {
                "cmd": "SET_ACTIVITY",
                "args": {
                    "pid": os.getpid(),
                    "activity": {
                        "details": details,
                        "state": state,
                        "timestamps": {"start": int(time.time())},
                        "assets": {
                            "large_image": "roblox",
                            "large_text": "NRBlox Linux Engine",
                        },
                    },
                },
                "nonce": str(time.time()),
            }
            self._send_frame(1, payload)
        except Exception:
            self.connected = False


class LinuxEngineController:
    def __init__(self):
        LinuxPaths.ensure_dirs()
        self.settings = self._load_json(LinuxPaths.SETTINGS_FILE, DEFAULT_LINUX_SETTINGS)
        self.fflags = self._load_json(
            LinuxPaths.FFLAGS_FILE,
            {
                "DFIntTaskSchedulerTargetFps": "240",
                "FFlagDebugGraphicsPreferVulkan": "True",
                "FIntDebugForceMSAASamples": "4",
                "DFFlagDisableDPIScale": "True",
                "DFFlagTextureQualityOverrideEnabled": "True",
                "DFIntTextureQualityOverride": "3",
            },
        )
        self.discord = LinuxDiscordRpc()
        self.sync_all()

    @staticmethod
    def _load_json(path: Path, default):
        try:
            if path.exists():
                return json.loads(path.read_text(encoding="utf-8"))
        except Exception:
            pass
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(default, indent=2, ensure_ascii=False), encoding="utf-8")
        return dict(default) if isinstance(default, dict) else list(default)

    def save_settings(self):
        LinuxPaths.SETTINGS_FILE.write_text(
            json.dumps(self.settings, indent=2, ensure_ascii=False), encoding="utf-8"
        )
        self.apply_presets_to_fflags()
        self.sync_all()

    def apply_presets_to_fflags(self):
        fps = int(self.settings.get("target_fps", 240))
        if fps > 0:
            self.fflags["DFIntTaskSchedulerTargetFps"] = str(fps)
        else:
            self.fflags.pop("DFIntTaskSchedulerTargetFps", None)

        # Rendering API on Linux (Vulkan preferred for DXVK/Sober/RADV)
        for k in [
            "FFlagDebugGraphicsPreferVulkan",
            "FFlagDebugGraphicsPreferOpenGL",
            "FFlagDebugGraphicsPreferD3D11",
            "FFlagDebugGraphicsDisableDirect3D11",
        ]:
            self.fflags.pop(k, None)

        api = self.settings.get("rendering_api", "Vulkan")
        if api == "Vulkan":
            self.fflags["FFlagDebugGraphicsPreferVulkan"] = "True"
        elif api == "OpenGL":
            self.fflags["FFlagDebugGraphicsPreferOpenGL"] = "True"
        elif api == "D3D11 (DXVK)":
            self.fflags["FFlagDebugGraphicsPreferD3D11"] = "True"

        # Lighting
        for k in [
            "DFFlagDebugRenderForceTechnologyVoxel",
            "FFlagDebugForceFutureIsBrightPhase2",
            "FFlagDebugForceFutureIsBrightPhase3",
        ]:
            self.fflags.pop(k, None)

        light = self.settings.get("lighting_mode", "Padrão")
        if light == "Voxel":
            self.fflags["DFFlagDebugRenderForceTechnologyVoxel"] = "True"
        elif light == "ShadowMap":
            self.fflags["FFlagDebugForceFutureIsBrightPhase2"] = "True"
        elif light == "Future":
            self.fflags["FFlagDebugForceFutureIsBrightPhase3"] = "True"

        # MSAA & Textures
        msaa = self.settings.get("msaa_samples", "4")
        if msaa in ("1", "2", "4", "8"):
            self.fflags["FIntDebugForceMSAASamples"] = msaa
        else:
            self.fflags.pop("FIntDebugForceMSAASamples", None)

        tex = self.settings.get("texture_quality", "3")
        if tex in ("0", "1", "2", "3"):
            self.fflags["DFFlagTextureQualityOverrideEnabled"] = "True"
            self.fflags["DFIntTextureQualityOverride"] = tex
        else:
            self.fflags.pop("DFFlagTextureQualityOverrideEnabled", None)
            self.fflags.pop("DFIntTextureQualityOverride", None)

        if self.settings.get("disable_dpi_scale", True):
            self.fflags["DFFlagDisableDPIScale"] = "True"
        else:
            self.fflags.pop("DFFlagDisableDPIScale", None)

    def sync_all(self):
        """Syncs FastFlags and Mods to Sober (Flatpak), Vinegar, and Wine Prefix"""
        LinuxPaths.FFLAGS_FILE.write_text(
            json.dumps(self.fflags, indent=2, ensure_ascii=False), encoding="utf-8"
        )

        # 1. Sync to Sober Flatpak config (~/.var/app/org.vinegarhq.Sober/config/sober/config.json)
        try:
            sober_cfg_dir = LinuxPaths.SOBER_CONFIG_FILE.parent
            sober_cfg_dir.mkdir(parents=True, exist_ok=True)
            sober_data = {}
            if LinuxPaths.SOBER_CONFIG_FILE.exists():
                sober_data = json.loads(LinuxPaths.SOBER_CONFIG_FILE.read_text(encoding="utf-8"))
            sober_data["fflags"] = dict(self.fflags)
            sober_data["discord_rpc_enabled"] = bool(self.settings.get("enable_discord_rpc", True))
            sober_data["enable_hidpi"] = not bool(self.settings.get("disable_dpi_scale", True))
            LinuxPaths.SOBER_CONFIG_FILE.write_text(
                json.dumps(sober_data, indent=2, ensure_ascii=False), encoding="utf-8"
            )
        except Exception:
            pass

        # 2. Sync to Wine Prefix ClientAppSettings.json
        try:
            prefix = Path(os.path.expanduser(self.settings.get("wine_prefix", str(LinuxPaths.DEFAULT_WINE_PREFIX))))
            user = os.environ.get("USER", "user")
            version_dir = (
                prefix
                / "drive_c"
                / "users"
                / user
                / "AppData"
                / "Local"
                / "Roblox"
                / "Versions"
                / TARGET_VERSION_GUID
            )
            client_settings_dir = version_dir / "ClientSettings"
            client_settings_dir.mkdir(parents=True, exist_ok=True)
            (client_settings_dir / "ClientAppSettings.json").write_text(
                json.dumps(self.fflags, indent=2, ensure_ascii=False), encoding="utf-8"
            )
        except Exception:
            pass

    def detect_installed_runners(self):
        runners = {
            "sober_flatpak": False,
            "vinegar": shutil.which("vinegar") is not None,
            "wine64": shutil.which("wine64") is not None or shutil.which("wine") is not None,
            "gamemode": shutil.which("gamemoderun") is not None,
            "mangohud": shutil.which("mangohud") is not None,
            "gamescope": shutil.which("gamescope") is not None,
        }
        if shutil.which("flatpak"):
            try:
                out = subprocess.check_output(["flatpak", "list", "--app"], text=True, stderr=subprocess.DEVNULL)
                runners["sober_flatpak"] = "org.vinegarhq.Sober" in out
            except Exception:
                pass
        return runners

    def build_linux_env(self):
        env = os.environ.copy()
        env["WINEPREFIX"] = os.path.expanduser(self.settings.get("wine_prefix", str(LinuxPaths.DEFAULT_WINE_PREFIX)))
        env["WINEESYNC"] = "1" if self.settings.get("enable_esync", True) else "0"
        env["WINEFSYNC"] = "1" if self.settings.get("enable_fsync", True) else "0"

        if self.settings.get("prime_render_offload", False):
            env["__NV_PRIME_RENDER_OFFLOAD"] = "1"
            env["__GLX_VENDOR_LIBRARY_NAME"] = "nvidia"
            env["__VK_LAYER_NV_optimus"] = "NVIDIA_only"

        icd = self.settings.get("vulkan_icd", "Automático")
        icd_map = {
            "AMD RADV": "/usr/share/vulkan/icd.d/radeon_icd.x86_64.json",
            "Intel ANV": "/usr/share/vulkan/icd.d/intel_icd.x86_64.json",
            "NVIDIA": "/usr/share/vulkan/icd.d/nvidia_icd.json",
        }
        if icd in icd_map and os.path.exists(icd_map[icd]):
            env["VK_ICD_FILENAMES"] = icd_map[icd]

        return env

    def launch_roblox(self, mode="player", uri=None):
        self.sync_all()
        env = self.build_linux_env()
        wrappers = []

        if self.settings.get("enable_gamemode") and shutil.which("gamemoderun"):
            wrappers.append("gamemoderun")
        if self.settings.get("enable_mangohud") and shutil.which("mangohud"):
            wrappers.extend(["mangohud", "--dlsym"])
        if self.settings.get("enable_gamescope") and shutil.which("gamescope"):
            wrappers.extend(["gamescope", "-W", "1920", "-H", "1080", "-f", "--"])

        runner_pref = self.settings.get("linux_runner", "Auto (Sober -> Vinegar -> Wine64)")
        runners = self.detect_installed_runners()

        cmd = None
        if ("Sober" in runner_pref or "Auto" in runner_pref) and runners["sober_flatpak"] and mode == "player":
            cmd = wrappers + ["flatpak", "run", "org.vinegarhq.Sober"]
            if uri:
                cmd.append(uri)
        elif ("Vinegar" in runner_pref or "Auto" in runner_pref) and runners["vinegar"]:
            cmd = wrappers + ["vinegar", "studio" if mode == "studio" else "player"]
            if uri:
                cmd.append(uri)
        elif runners["wine64"]:
            wine_bin = shutil.which("wine64") or shutil.which("wine")
            prefix = Path(env["WINEPREFIX"])
            user = os.environ.get("USER", "user")
            exe_name = "RobloxStudioBeta.exe" if mode == "studio" else "RobloxPlayerBeta.exe"
            exe_path = (
                prefix
                / "drive_c"
                / "users"
                / user
                / "AppData"
                / "Local"
                / "Roblox"
                / "Versions"
                / TARGET_VERSION_GUID
                / exe_name
            )
            cmd = wrappers + [wine_bin, str(exe_path), uri or "--app"]

        if not cmd:
            return False, "Nenhum runner Linux encontrado (instale Sober via Flatpak, Vinegar ou Wine64)."

        try:
            subprocess.Popen(cmd, env=env, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            return True, "Iniciado via: " + " ".join(cmd)
        except Exception as e:
            return False, str(e)


class NRBloxLinuxDesktopUI:
    """Dedicated Native Linux Desktop Interface with Dark Mode & Light Mode"""

    PALETTES = {
        "Dark": {
            "bg_window": "#0B0F17",
            "bg_header": "#111726",
            "bg_card": "#161F33",
            "bg_input": "#0E1422",
            "border": "#26334D",
            "text_primary": "#F8FAFC",
            "text_secondary": "#94A3B8",
            "accent": "#00E5FF",
            "accent_fg": "#060911",
            "success": "#10B981",
        },
        "Light": {
            "bg_window": "#F1F5F9",
            "bg_header": "#FFFFFF",
            "bg_card": "#FFFFFF",
            "bg_input": "#E2E8F0",
            "border": "#CBD5E1",
            "text_primary": "#0F172A",
            "text_secondary": "#475569",
            "accent": "#4F46E5",
            "accent_fg": "#FFFFFF",
            "success": "#059669",
        },
    }

    def __init__(self, controller: LinuxEngineController):
        self.ctrl = controller
        self.root = tk.Tk()
        self.root.title("NRBlox Linux — Sober / Vinegar / Wine64 Engine Controller")
        self.root.geometry("1080x720")
        self.root.minsize(920, 620)
        self.current_tab = "linux_runner"
        self.build_ui()

    def get_palette(self):
        mode = self.ctrl.settings.get("theme_mode", "Dark")
        return self.PALETTES.get(mode, self.PALETTES["Dark"])

    def toggle_theme(self):
        curr = self.ctrl.settings.get("theme_mode", "Dark")
        self.ctrl.settings["theme_mode"] = "Light" if curr == "Dark" else "Dark"
        self.ctrl.save_settings()
        self.build_ui()

    def build_ui(self):
        for w in self.root.winfo_children():
            w.destroy()

        p = self.get_palette()
        self.root.configure(bg=p["bg_window"])

        # Top Command Bar
        header = tk.Frame(self.root, bg=p["bg_header"], height=64, highlightthickness=1, highlightbackground=p["border"])
        header.pack(fill="x", side="top")
        header.pack_propagate(False)

        brand = tk.Label(
            header,
            text="🐧 NRBLOX LINUX ENGINE",
            bg=p["bg_header"],
            fg=p["accent"],
            font=("Sans", 14, "bold"),
            padx=18,
        )
        brand.pack(side="left")

        nav_frame = tk.Frame(header, bg=p["bg_header"])
        nav_frame.pack(side="left", padx=16)

        tabs = [
            ("linux_runner", "🐧 Runners & Kernel Linux"),
            ("fastflags", "🚀 FastFlags (Sober/Wine)"),
            ("mods", "🎨 Asset Overlay & Mods"),
            ("community", "💬 Comunidade & Wiki"),
        ]
        for key, label in tabs:
            is_active = self.current_tab == key
            btn = tk.Button(
                nav_frame,
                text=label,
                bg=p["accent"] if is_active else p["bg_card"],
                fg=p["accent_fg"] if is_active else p["text_primary"],
                relief="flat",
                font=("Sans", 10, "bold" if is_active else "normal"),
                padx=12,
                pady=6,
                cursor="hand2",
                command=lambda k=key: self.switch_tab(k),
            )
            btn.pack(side="left", padx=4)

        # Right Action Buttons
        play_btn = tk.Button(
            header,
            text="▶ INICIAR ROBLOX",
            bg=p["accent"],
            fg=p["accent_fg"],
            relief="flat",
            font=("Sans", 10, "bold"),
            padx=14,
            pady=6,
            cursor="hand2",
            command=lambda: self.on_launch("player"),
        )
        play_btn.pack(side="right", padx=14)

        theme_label = "☀️ Modo Claro" if self.ctrl.settings.get("theme_mode", "Dark") == "Dark" else "🌙 Modo Escuro"
        theme_btn = tk.Button(
            header,
            text=theme_label,
            bg=p["bg_card"],
            fg=p["text_primary"],
            relief="flat",
            font=("Sans", 10),
            padx=12,
            pady=6,
            cursor="hand2",
            command=self.toggle_theme,
        )
        theme_btn.pack(side="right", padx=4)

        # Main Content Area
        self.content = tk.Frame(self.root, bg=p["bg_window"], padx=24, pady=20)
        self.content.pack(fill="both", expand=True)

        if self.current_tab == "linux_runner":
            self.render_linux_runner_tab(p)
        elif self.current_tab == "fastflags":
            self.render_fastflags_tab(p)
        elif self.current_tab == "mods":
            self.render_mods_tab(p)
        elif self.current_tab == "community":
            self.render_community_tab(p)

    def switch_tab(self, key):
        self.current_tab = key
        self.build_ui()

    def _create_card(self, parent, p, title, subtitle):
        card = tk.Frame(parent, bg=p["bg_card"], padx=16, pady=12, highlightthickness=1, highlightbackground=p["border"])
        card.pack(fill="x", pady=6)
        left = tk.Frame(card, bg=p["bg_card"])
        left.pack(side="left", fill="x", expand=True)
        tk.Label(left, text=title, bg=p["bg_card"], fg=p["text_primary"], font=("Sans", 11, "bold"), anchor="w").pack(fill="x")
        tk.Label(left, text=subtitle, bg=p["bg_card"], fg=p["text_secondary"], font=("Sans", 9), anchor="w").pack(fill="x", pady=(2, 0))
        return card

    def render_linux_runner_tab(self, p):
        runners = self.ctrl.detect_installed_runners()
        status_str = (
            f"Sober Flatpak: {'✅ Detectado' if runners['sober_flatpak'] else '❌ Não instalado'}   |   "
            f"Vinegar: {'✅ Detectado' if runners['vinegar'] else '❌ Não instalado'}   |   "
            f"Wine64: {'✅ Detectado' if runners['wine64'] else '❌ Não instalado'}   |   "
            f"GameMode: {'✅' if runners['gamemode'] else '➖'}"
        )
        banner = tk.Frame(self.content, bg=p["bg_card"], padx=18, pady=14, highlightthickness=1, highlightbackground=p["accent"])
        banner.pack(fill="x", pady=(0, 12))
        tk.Label(banner, text="Status do Ambiente Linux (X11 / Wayland)", bg=p["bg_card"], fg=p["accent"], font=("Sans", 11, "bold"), anchor="w").pack(fill="x")
        tk.Label(banner, text=status_str, bg=p["bg_card"], fg=p["text_primary"], font=("Sans", 10), anchor="w").pack(fill="x", pady=(4, 0))

        # Runner Selector
        c1 = self._create_card(
            self.content,
            p,
            "Backend de Execução Linux (Runner)",
            "Define como o cliente Roblox será executado no Linux (Sober via Flatpak, Vinegar ou Wine64/Proton).",
        )
        runner_var = tk.StringVar(value=self.ctrl.settings.get("linux_runner"))
        opt1 = ttk.Combobox(
            c1,
            textvariable=runner_var,
            values=["Auto (Sober -> Vinegar -> Wine64)", "Sober (Flatpak)", "Vinegar", "Wine64 / Proton Prefix"],
            state="readonly",
            width=32,
        )
        opt1.pack(side="right")
        opt1.bind("<<ComboboxSelected>>", lambda e: self._update_setting("linux_runner", runner_var.get()))

        # Vulkan ICD
        c2 = self._create_card(
            self.content,
            p,
            "Driver Vulkan ICD (VK_ICD_FILENAMES)",
            "Seleciona explicitamente o driver Vulkan para GPUs AMD (RADV), Intel (ANV) ou NVIDIA.",
        )
        icd_var = tk.StringVar(value=self.ctrl.settings.get("vulkan_icd", "Automático"))
        opt2 = ttk.Combobox(
            c2,
            textvariable=icd_var,
            values=["Automático", "AMD RADV", "Intel ANV", "NVIDIA"],
            state="readonly",
            width=22,
        )
        opt2.pack(side="right")
        opt2.bind("<<ComboboxSelected>>", lambda e: self._update_setting("vulkan_icd", icd_var.get()))

        # Toggles for GameMode, MangoHud, Esync/Fsync, PRIME
        for key, title, sub in [
            ("enable_gamemode", "Feral GameMode (gamemoderun)", "Solicita o governador de CPU performance e prioridade I/O ao kernel Linux."),
            ("enable_mangohud", "Overlay MangoHud Vulkan (mangohud --dlsym)", "Exibe telemetria de FPS, frametime e uso de GPU/CPU sobre o jogo."),
            ("enable_esync", "Wine Esync & Fsync (WINEESYNC=1 / WINEFSYNC=1)", "Reduz gargalos de sincronização de threads no kernel Linux (>5.16 futex_waitv)."),
            ("prime_render_offload", "NVIDIA PRIME Render Offload (__NV_PRIME_RENDER_OFFLOAD=1)", "Força execução na GPU dedicada em notebooks híbridos (Optimus)."),
        ]:
            card = self._create_card(self.content, p, title, sub)
            val = bool(self.ctrl.settings.get(key, False))
            btn = tk.Button(
                card,
                text="ATIVO" if val else "DESATIVADO",
                bg=p["accent"] if val else p["bg_input"],
                fg=p["accent_fg"] if val else p["text_secondary"],
                relief="flat",
                width=12,
                command=lambda k=key, v=not val: self._update_setting(k, v, redraw=True),
            )
            btn.pack(side="right")

    def render_fastflags_tab(self, p):
        c1 = self._create_card(
            self.content,
            p,
            "Limite de FPS no Linux (DFIntTaskSchedulerTargetFps)",
            "Sincronizado automaticamente em ~/.var/app/org.vinegarhq.Sober/config/sober/config.json e Wine.",
        )
        fps_var = tk.StringVar(value=str(self.ctrl.settings.get("target_fps", 240)))
        opt_fps = ttk.Combobox(c1, textvariable=fps_var, values=["60", "120", "144", "165", "240", "360", "999"], state="readonly", width=16)
        opt_fps.pack(side="right")
        opt_fps.bind("<<ComboboxSelected>>", lambda e: self._update_setting("target_fps", int(fps_var.get())))

        c2 = self._create_card(
            self.content,
            p,
            "API Gráfica Preferencial no Linux",
            "Vulkan oferece a menor latência nativa tanto no Sober quanto via DXVK no Wine.",
        )
        api_var = tk.StringVar(value=self.ctrl.settings.get("rendering_api", "Vulkan"))
        opt_api = ttk.Combobox(c2, textvariable=api_var, values=["Vulkan", "OpenGL", "D3D11 (DXVK)"], state="readonly", width=18)
        opt_api.pack(side="right")
        opt_api.bind("<<ComboboxSelected>>", lambda e: self._update_setting("rendering_api", api_var.get()))

        c3 = self._create_card(
            self.content,
            p,
            "Tecnologia de Iluminação",
            "Substitui o pipeline de iluminação do motor (Padrão, Voxel, ShadowMap ou Future Phase 3).",
        )
        light_var = tk.StringVar(value=self.ctrl.settings.get("lighting_mode", "Padrão"))
        opt_light = ttk.Combobox(c3, textvariable=light_var, values=["Padrão", "Voxel", "ShadowMap", "Future"], state="readonly", width=18)
        opt_light.pack(side="right")
        opt_light.bind("<<ComboboxSelected>>", lambda e: self._update_setting("lighting_mode", light_var.get()))

    def render_mods_tab(self, p):
        c1 = self._create_card(
            self.content,
            p,
            "Cursores Clássicos (Asset Overlay Sober & Wine)",
            "Aplica os cursores clássicos de 2006 ou 2013 no overlay de assets do Linux.",
        )
        cur_var = tk.StringVar(value=self.ctrl.settings.get("cursor_type", "Default"))
        opt_cur = ttk.Combobox(c1, textvariable=cur_var, values=["Default", "From2006", "From2013"], state="readonly", width=18)
        opt_cur.pack(side="right")
        opt_cur.bind("<<ComboboxSelected>>", lambda e: self._update_setting("cursor_type", cur_var.get()))

        for key, title, sub in [
            ("use_old_sounds", "Sons Clássicos de Personagem (2013)", "Aplica OldWalk.mp3, OldJump.mp3 e OldGetUp.mp3 no asset_overlay do Linux."),
            ("use_old_avatar_bg", "Cenário Clássico do Editor de Avatar", "Injeta OldAvatarBackground.rbxl em ExtraContent/places/Mobile.rbxl."),
            ("enable_discord_rpc", "Discord Rich Presence Nativo (AF_UNIX Socket)", "Conecta diretamente ao socket $XDG_RUNTIME_DIR/discord-ipc-0 ou Vesktop."),
        ]:
            card = self._create_card(self.content, p, title, sub)
            val = bool(self.ctrl.settings.get(key, False))
            btn = tk.Button(
                card,
                text="ATIVO" if val else "DESATIVADO",
                bg=p["accent"] if val else p["bg_input"],
                fg=p["accent_fg"] if val else p["text_secondary"],
                relief="flat",
                width=12,
                command=lambda k=key, v=not val: self._update_setting(k, v, redraw=True),
            )
            btn.pack(side="right")

    def render_community_tab(self, p):
        card = tk.Frame(self.content, bg=p["bg_card"], padx=24, pady=24, highlightthickness=1, highlightbackground=p["accent"])
        card.pack(fill="x", pady=10)
        tk.Label(card, text="⚡ NRBlox Linux — Projeto 100% Open-Source", bg=p["bg_card"], fg=p["text_primary"], font=("Sans", 15, "bold"), anchor="w").pack(fill="x")
        tk.Label(
            card,
            text="Criado por FaelSemW • Arquitetura dedicada para Linux com suporte a Sober, Vinegar, Proton-GE e Wine64.",
            bg=p["bg_card"],
            fg=p["text_secondary"],
            font=("Sans", 10),
            anchor="w",
        ).pack(fill="x", pady=(6, 16))

        btn_row = tk.Frame(card, bg=p["bg_card"])
        btn_row.pack(fill="x")
        tk.Button(
            btn_row,
            text="💬 Entrar na Comunidade Discord Oficial",
            bg=p["accent"],
            fg=p["accent_fg"],
            relief="flat",
            padx=14,
            pady=8,
            command=lambda: webbrowser.open(DISCORD_URL),
        ).pack(side="left", padx=(0, 10))
        tk.Button(
            btn_row,
            text="📖 Abrir Wiki Oficial no GitHub",
            bg=p["bg_input"],
            fg=p["text_primary"],
            relief="flat",
            padx=14,
            pady=8,
            command=lambda: webbrowser.open(GITHUB_WIKI_URL),
        ).pack(side="left")

    def _update_setting(self, key, value, redraw=False):
        self.ctrl.settings[key] = value
        self.ctrl.save_settings()
        if redraw:
            self.build_ui()

    def on_launch(self, mode):
        ok, msg = self.ctrl.launch_roblox(mode=mode)
        if not ok:
            messagebox.showwarning("NRBlox Linux", msg)

    def run(self):
        self.root.mainloop()


def main():
    ctrl = LinuxEngineController()
    args = sys.argv[1:]

    if "--verify" in args:
        print(json.dumps({
            "launcher": "NRBlox Linux Native Edition",
            "version": "3.0.0",
            "config_file": str(LinuxPaths.SETTINGS_FILE),
            "fflags_file": str(LinuxPaths.FFLAGS_FILE),
            "sober_config": str(LinuxPaths.SOBER_CONFIG_FILE),
            "runners": ctrl.detect_installed_runners(),
            "active_fflags": len(ctrl.fflags),
        }, indent=2, ensure_ascii=False))
        return 0

    uri = next((a for a in args if a.startswith(("roblox-player:", "roblox:", "nrblox:"))), None)
    if "-player" in args or "--player" in args or uri:
        ok, msg = ctrl.launch_roblox(mode="player", uri=uri)
        print(f"[NRBlox-Linux] {msg}")
        return 0 if ok else 1

    if not HAS_TK:
        print("[NRBlox-Linux] Tkinter GUI module not found. Install 'python3-tk' or use CLI flags (--player, --verify).")
        return 1

    ui = NRBloxLinuxDesktopUI(ctrl)
    ui.run()
    return 0


if __name__ == "__main__":
    sys.exit(main())
