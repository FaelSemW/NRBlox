#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
============================================================================
NRBlox (Linux Native Edition) — Dedicated Linux Desktop Launcher & Engine
Author: FaelSemW (https://github.com/FaelSemW/NRBlox)
Community Discord: https://discord.gg/M8j8JnQSCx
License: MIT (100% Open Source)
============================================================================
Matches the comfortable 2-Window Fluent layout (Launch Menu -> Settings Window)
while providing dedicated Linux runners (Sober Flatpak, Vinegar, Wine64/Proton),
Vulkan ICD selection, GameMode, MangoHud, Esync/Fsync, and Light/Dark Mode.
============================================================================
"""

import json
import os
import shutil
import socket
import struct
import subprocess
import sys
import time
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
    MODS_DIR = XDG_DATA / "modifications"
    DEFAULT_WINE_PREFIX = XDG_DATA / "wine-prefix"
    SOBER_CONFIG_FILE = HOME / ".var" / "app" / "org.vinegarhq.Sober" / "config" / "sober" / "config.json"

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
    "theme_mode": "Light",
    "language": "System default",
    "test_mode": False,
    "enable_activity_tracking": True,
    "query_server_location": True,
    "disable_desktop_app": False,
    "enable_discord_rpc": True,
    "allow_activity_joining": True,
    "show_roblox_account": True,
    "confirm_launches": False,
    "background_updates": False,
    "force_reinstall": False,
    "linux_runner": "Auto (Sober -> Vinegar -> Wine64)",
    "wine_prefix": str(LinuxPaths.DEFAULT_WINE_PREFIX),
    "vulkan_icd": "Automatic",
    "enable_gamemode": True,
    "enable_mangohud": False,
    "enable_esync": True,
    "enable_fsync": True,
    "prime_render_offload": False,
    "cursor_type": "Default",
    "use_old_avatar_bg": False,
    "use_old_sounds": False,
    "preferred_emoji": "Default (Twemoji)",
    "custom_font_path": "",
    "manage_fastflags": True,
    "msaa_samples": "4x",
    "disable_dpi_scale": True,
    "texture_quality": "Level 3 (Highest)",
    "target_fps": 240,
    "rendering_api": "Vulkan",
    "lighting_mode": "Chosen by game",
    "bootstrapper_style": "NRBlox",
    "bootstrapper_icon": "NRBlox",
    "bootstrapper_title": "NRBlox",
    "shortcut_desktop": True,
    "shortcut_app_menu": True,
    "auto_update_nrblox": True,
    "enable_analytics": False,
}


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
        return dict(default)

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

        msaa = str(self.settings.get("msaa_samples", "4x")).replace("x", "").strip()
        if msaa in ("1", "2", "4", "8"):
            self.fflags["FIntDebugForceMSAASamples"] = msaa
        else:
            self.fflags.pop("FIntDebugForceMSAASamples", None)

        if self.settings.get("disable_dpi_scale", True):
            self.fflags["DFFlagDisableDPIScale"] = "True"
        else:
            self.fflags.pop("DFFlagDisableDPIScale", None)

    def sync_all(self):
        LinuxPaths.FFLAGS_FILE.write_text(
            json.dumps(self.fflags, indent=2, ensure_ascii=False), encoding="utf-8"
        )
        try:
            LinuxPaths.SOBER_CONFIG_FILE.parent.mkdir(parents=True, exist_ok=True)
            sober_data = {}
            if LinuxPaths.SOBER_CONFIG_FILE.exists():
                sober_data = json.loads(LinuxPaths.SOBER_CONFIG_FILE.read_text(encoding="utf-8"))
            sober_data["fflags"] = dict(self.fflags)
            sober_data["discord_rpc_enabled"] = bool(self.settings.get("enable_discord_rpc", True))
            LinuxPaths.SOBER_CONFIG_FILE.write_text(
                json.dumps(sober_data, indent=2, ensure_ascii=False), encoding="utf-8"
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
        }
        if shutil.which("flatpak"):
            try:
                out = subprocess.check_output(["flatpak", "list", "--app"], text=True, stderr=subprocess.DEVNULL)
                runners["sober_flatpak"] = "org.vinegarhq.Sober" in out
            except Exception:
                pass
        return runners

    def launch_roblox(self, mode="player", uri=None):
        self.sync_all()
        env = os.environ.copy()
        env["WINEPREFIX"] = os.path.expanduser(self.settings.get("wine_prefix", str(LinuxPaths.DEFAULT_WINE_PREFIX)))
        env["WINEESYNC"] = "1" if self.settings.get("enable_esync", True) else "0"
        env["WINEFSYNC"] = "1" if self.settings.get("enable_fsync", True) else "0"

        wrappers = []
        if self.settings.get("enable_gamemode") and shutil.which("gamemoderun"):
            wrappers.append("gamemoderun")
        if self.settings.get("enable_mangohud") and shutil.which("mangohud"):
            wrappers.extend(["mangohud", "--dlsym"])

        runners = self.detect_installed_runners()
        if not runners["sober_flatpak"] and not runners["vinegar"] and shutil.which("flatpak"):
            try:
                subprocess.run(
                    ["flatpak", "remote-add", "--user", "--if-not-exists", "flathub", "https://dl.flathub.org/repo/flathub.flatpakrepo"],
                    stdout=subprocess.DEVNULL,
                    stderr=subprocess.DEVNULL,
                )
                subprocess.run(
                    ["flatpak", "install", "--user", "-y", "flathub", "org.vinegarhq.Sober"],
                    stdout=subprocess.DEVNULL,
                    stderr=subprocess.DEVNULL,
                )
                runners = self.detect_installed_runners()
            except Exception:
                pass

        cmd = None
        if runners["sober_flatpak"] and mode == "player":
            cmd = wrappers + ["flatpak", "run", "org.vinegarhq.Sober"]
            if uri:
                cmd.append(uri)
        elif runners["vinegar"]:
            cmd = wrappers + ["vinegar", "studio" if mode == "studio" else "player"]
            if uri:
                cmd.append(uri)
        elif runners["wine64"]:
            wine_bin = shutil.which("wine64") or shutil.which("wine")
            exe_name = "RobloxStudioBeta.exe" if mode == "studio" else "RobloxPlayerBeta.exe"
            cmd = wrappers + [wine_bin, exe_name, uri or "--app"]

        if not cmd:
            return False, "No Linux runner found (run install-linux.sh to auto-install Sober/Flatpak or Wine64)."

        try:
            subprocess.Popen(cmd, env=env, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            return True, "Launched via: " + " ".join(cmd)
        except Exception as e:
            return False, str(e)


PALETTES = {
    "Light": {
        "bg_window": "#F8F9FA",
        "bg_card": "#FFFFFF",
        "border": "#E3E5E8",
        "text_primary": "#1B1B1B",
        "text_secondary": "#6B6B6B",
        "accent": "#0067C0",
        "accent_fg": "#FFFFFF",
    },
    "Dark": {
        "bg_window": "#1F1F1F",
        "bg_card": "#2B2B2B",
        "border": "#3A3A3A",
        "text_primary": "#FFFFFF",
        "text_secondary": "#A6A6A6",
        "accent": "#0067C0",
        "accent_fg": "#FFFFFF",
    },
}


class NRBloxLinuxApp:
    """Two-Window Fluent Linux Interface: Launch Menu Window -> Settings Window"""

    def __init__(self, ctrl: LinuxEngineController):
        self.ctrl = ctrl
        self.root = tk.Tk()
        self.show_launch_menu()

    def get_palette(self):
        mode = self.ctrl.settings.get("theme_mode", "Light")
        return PALETTES["Dark"] if mode == "Dark" else PALETTES["Light"]

    def show_launch_menu(self):
        for w in self.root.winfo_children():
            w.destroy()
        p = self.get_palette()
        self.root.title("NRBlox")
        self.root.geometry("620x350")
        self.root.configure(bg=p["bg_window"])

        container = tk.Frame(self.root, bg=p["bg_window"], padx=26, pady=24)
        container.pack(fill="both", expand=True)

        # Left column (Logo + About / Wiki / Community links)
        left = tk.Frame(container, bg=p["bg_window"], width=230)
        left.pack(side="left", fill="y", padx=(0, 20))
        left.pack_propagate(False)

        tk.Label(left, text="❖ NRBlox", bg=p["bg_window"], fg=p["text_primary"], font=("Sans", 22, "normal")).pack(pady=(32, 2))
        tk.Label(left, text="Version 3.0.0 (Linux)", bg=p["bg_window"], fg=p["text_secondary"], font=("Sans", 10)).pack(pady=(0, 24))

        for label, action in [
            ("ⓘ  About NRBlox", lambda: self.show_settings_window("about")),
            ("📖  NRBlox Wiki", lambda: webbrowser.open(GITHUB_WIKI_URL)),
            ("💬  NRBlox Community", lambda: webbrowser.open(DISCORD_URL)),
        ]:
            lbl = tk.Label(left, text=label, bg=p["bg_window"], fg=p["accent"], font=("Sans", 10), cursor="hand2")
            lbl.pack(pady=4)
            lbl.bind("<Button-1>", lambda e, fn=action: fn())

        # Right column (Action Cards)
        right = tk.Frame(container, bg=p["bg_window"])
        right.pack(side="left", fill="both", expand=True)

        for icon, title, subtitle, fn in [
            ("➔", "Launch Roblox", None, lambda: self.launch_client("player")),
            ("🔧", "Launch Roblox Studio", None, lambda: self.launch_client("studio")),
            ("⚙️", "Configure settings", None, lambda: self.show_settings_window("integrations")),
            ("📓", "Having an issue?", "See the Wiki for help", lambda: webbrowser.open(GITHUB_WIKI_URL)),
        ]:
            card = tk.Frame(right, bg=p["bg_card"], padx=16, pady=12, highlightthickness=1, highlightbackground=p["border"], cursor="hand2")
            card.pack(fill="x", pady=5)
            tk.Label(card, text=icon, bg=p["bg_card"], fg=p["text_primary"], font=("Sans", 13)).pack(side="left", padx=(0, 12))
            txt_frame = tk.Frame(card, bg=p["bg_card"])
            txt_frame.pack(side="left", fill="x", expand=True)
            tk.Label(txt_frame, text=title, bg=p["bg_card"], fg=p["text_primary"], font=("Sans", 11), anchor="w").pack(fill="x")
            if subtitle:
                tk.Label(txt_frame, text=subtitle, bg=p["bg_card"], fg=p["text_secondary"], font=("Sans", 9), anchor="w").pack(fill="x")
            tk.Label(card, text="›", bg=p["bg_card"], fg=p["text_secondary"], font=("Sans", 14)).pack(side="right")
            for widget in (card, txt_frame) + tuple(card.winfo_children()) + tuple(txt_frame.winfo_children()):
                widget.bind("<Button-1>", lambda e, f=fn: f())

    def show_settings_window(self, active_tab="integrations"):
        self.active_tab = active_tab
        for w in self.root.winfo_children():
            w.destroy()
        p = self.get_palette()
        self.root.title("NRBlox Settings")
        self.root.geometry("980x620")
        self.root.configure(bg=p["bg_window"])

        body = tk.Frame(self.root, bg=p["bg_window"])
        body.pack(fill="both", expand=True)

        # Left Sidebar
        sidebar = tk.Frame(body, bg=p["bg_window"], width=220, padx=12, pady=14)
        sidebar.pack(side="left", fill="y")
        sidebar.pack_propagate(False)

        nav_items = [
            ("integrations", "＋  Integrations"),
            ("bootstrapper", "▷  Bootstrapper"),
            ("mods", "🛠  Mods"),
            ("engine", "⚑  Engine Settings"),
            ("appearance", "🖌  Appearance"),
            ("shortcuts", "⊞  Shortcuts"),
            ("nrblox", "⚙  NRBlox"),
            ("about", "ⓘ  About"),
        ]
        for key, text in nav_items:
            is_sel = key == self.active_tab
            btn = tk.Button(
                sidebar,
                text=text,
                anchor="w",
                bg=p["bg_card"] if is_sel else p["bg_window"],
                fg=p["text_primary"],
                relief="flat",
                font=("Sans", 10, "bold" if is_sel else "normal"),
                padx=12,
                pady=8,
                cursor="hand2",
                command=lambda k=key: self.show_settings_window(k),
            )
            btn.pack(fill="x", pady=2)

        # Right Page Area
        content = tk.Frame(body, bg=p["bg_window"], padx=22, pady=16)
        content.pack(side="left", fill="both", expand=True)
        self.render_page(content, p)

        # Bottom Bar (Test mode | Save | Close)
        footer = tk.Frame(self.root, bg=p["bg_window"], padx=18, pady=12)
        footer.pack(fill="x", side="bottom")

        test_val = bool(self.ctrl.settings.get("test_mode", False))
        tk.Button(
            footer,
            text=f"Test mode: {'ON' if test_val else 'OFF'}",
            bg=p["accent"] if test_val else p["bg_card"],
            fg=p["accent_fg"] if test_val else p["text_primary"],
            relief="flat",
            padx=12,
            pady=6,
            command=lambda: self.toggle_bool("test_mode"),
        ).pack(side="left")

        tk.Button(
            footer,
            text="Close",
            bg=p["bg_card"],
            fg=p["text_primary"],
            relief="flat",
            padx=18,
            pady=6,
            command=self.root.destroy,
        ).pack(side="right", padx=(8, 0))

        tk.Button(
            footer,
            text="Save",
            bg=p["accent"],
            fg=p["accent_fg"],
            relief="flat",
            padx=18,
            pady=6,
            command=self.on_save,
        ).pack(side="right")

    def _row(self, parent, p, title, subtitle):
        card = tk.Frame(parent, bg=p["bg_card"], padx=16, pady=12, highlightthickness=1, highlightbackground=p["border"])
        card.pack(fill="x", pady=5)
        left = tk.Frame(card, bg=p["bg_card"])
        left.pack(side="left", fill="x", expand=True)
        tk.Label(left, text=title, bg=p["bg_card"], fg=p["text_primary"], font=("Sans", 11), anchor="w").pack(fill="x")
        if subtitle:
            tk.Label(left, text=subtitle, bg=p["bg_card"], fg=p["text_secondary"], font=("Sans", 9), anchor="w").pack(fill="x")
        return card

    def _toggle_row(self, parent, p, key, title, subtitle=None):
        card = self._row(parent, p, title, subtitle)
        val = bool(self.ctrl.settings.get(key, False))
        tk.Button(
            card,
            text="ON" if val else "OFF",
            bg=p["accent"] if val else p["bg_window"],
            fg=p["accent_fg"] if val else p["text_secondary"],
            relief="flat",
            width=8,
            command=lambda k=key: self.toggle_bool(k),
        ).pack(side="right")

    def render_page(self, parent, p):
        t = self.active_tab
        if t == "integrations":
            tk.Label(parent, text="Integrations", bg=p["bg_window"], fg=p["text_primary"], font=("Sans", 20, "bold"), anchor="w").pack(fill="x", pady=(0, 12))
            self._toggle_row(parent, p, "enable_activity_tracking", "Enable activity tracking", "Allows NRBlox to detect what Roblox game you're playing.")
            self._toggle_row(parent, p, "query_server_location", "Query server location", "See where your server is located via ipinfo.io.")
            self._toggle_row(parent, p, "enable_discord_rpc", "Show game activity (Discord Rich Presence)", "Connects via Linux AF_UNIX socket.")
            self._toggle_row(parent, p, "allow_activity_joining", "Allow activity joining", "Allows joining through your Discord profile.")
            self._toggle_row(parent, p, "show_roblox_account", "Show Roblox account", "Shows the Roblox account on your Discord profile.")
        elif t == "bootstrapper":
            tk.Label(parent, text="Bootstrapper & Linux Runner", bg=p["bg_window"], fg=p["text_primary"], font=("Sans", 20, "bold"), anchor="w").pack(fill="x", pady=(0, 12))
            self._toggle_row(parent, p, "confirm_launches", "Prompt to confirm when launching another Roblox instance")
            self._toggle_row(parent, p, "background_updates", "Background updates")
            self._toggle_row(parent, p, "enable_gamemode", "Enable Feral GameMode (gamemoderun)", "Optimizes Linux CPU governor when launching Roblox.")
            self._toggle_row(parent, p, "enable_mangohud", "Enable MangoHud Vulkan Overlay (mangohud --dlsym)", "Displays FPS and frame timing overlay on Linux.")
            self._toggle_row(parent, p, "enable_esync", "Enable Wine Esync & Fsync", "Low-latency futex synchronization for Wine/Proton.")
        elif t == "mods":
            tk.Label(parent, text="Mods", bg=p["bg_window"], fg=p["text_primary"], font=("Sans", 20, "bold"), anchor="w").pack(fill="x", pady=(0, 12))
            self._toggle_row(parent, p, "use_old_avatar_bg", "Use old avatar editor background", "Bring back the classic avatar background.")
            self._toggle_row(parent, p, "use_old_sounds", "Emulate old character sounds", "Bring back pre-2014 character sounds.")
        elif t == "engine":
            tk.Label(parent, text="Engine Settings", bg=p["bg_window"], fg=p["text_primary"], font=("Sans", 20, "bold"), anchor="w").pack(fill="x", pady=(0, 12))
            self._toggle_row(parent, p, "manage_fastflags", "Allow NRBlox to manage Fast Flags", "Syncs directly to Sober config.json and Wine ClientAppSettings.json.")
            self._toggle_row(parent, p, "disable_dpi_scale", "Preserve rendering quality with display scaling", "Prevents display scaling blur.")
        elif t == "appearance":
            tk.Label(parent, text="Appearance", bg=p["bg_window"], fg=p["text_primary"], font=("Sans", 20, "bold"), anchor="w").pack(fill="x", pady=(0, 12))
            c = self._row(parent, p, "Global Theme", "Switch between Light Mode and Dark Mode.")
            curr_theme = self.ctrl.settings.get("theme_mode", "Light")
            tk.Button(
                c,
                text=f"Theme: {curr_theme} (Click to switch)",
                bg=p["accent"],
                fg=p["accent_fg"],
                relief="flat",
                padx=12,
                pady=6,
                command=self.switch_theme,
            ).pack(side="right")
        elif t == "shortcuts":
            tk.Label(parent, text="Shortcuts", bg=p["bg_window"], fg=p["text_primary"], font=("Sans", 20, "bold"), anchor="w").pack(fill="x", pady=(0, 12))
            self._toggle_row(parent, p, "shortcut_desktop", "Desktop icon")
            self._toggle_row(parent, p, "shortcut_app_menu", "Application Menu (.desktop) icon")
        elif t == "nrblox":
            tk.Label(parent, text="NRBlox", bg=p["bg_window"], fg=p["text_primary"], font=("Sans", 20, "bold"), anchor="w").pack(fill="x", pady=(0, 12))
            self._toggle_row(parent, p, "auto_update_nrblox", "Automatically update NRBlox")
            self._toggle_row(parent, p, "enable_analytics", "Enable sending of analytics")
        elif t == "about":
            tk.Label(parent, text="About NRBlox", bg=p["bg_window"], fg=p["text_primary"], font=("Sans", 20, "bold"), anchor="w").pack(fill="x", pady=(0, 12))
            self._row(parent, p, "NRBlox Version 3.0.0 — Created by FaelSemW", "100% Open-Source (MIT License)")
            c_disc = self._row(parent, p, "Official Discord Community", DISCORD_URL)
            tk.Button(c_disc, text="Join Discord", bg=p["accent"], fg=p["accent_fg"], relief="flat", command=lambda: webbrowser.open(DISCORD_URL)).pack(side="right")
            c_wiki = self._row(parent, p, "Official NRBlox Wiki", GITHUB_WIKI_URL)
            tk.Button(c_wiki, text="Open Wiki", bg=p["accent"], fg=p["accent_fg"], relief="flat", command=lambda: webbrowser.open(GITHUB_WIKI_URL)).pack(side="right")

    def toggle_bool(self, key):
        self.ctrl.settings[key] = not bool(self.ctrl.settings.get(key, False))
        self.ctrl.save_settings()
        self.show_settings_window(self.active_tab)

    def switch_theme(self):
        curr = self.ctrl.settings.get("theme_mode", "Light")
        self.ctrl.settings["theme_mode"] = "Dark" if curr == "Light" else "Light"
        self.ctrl.save_settings()
        self.show_settings_window(self.active_tab)

    def on_save(self):
        self.ctrl.save_settings()
        if self.ctrl.settings.get("test_mode"):
            self.launch_client("player")
        else:
            self.root.destroy()

    def launch_client(self, mode):
        for w in self.root.winfo_children():
            w.destroy()
        p = self.get_palette()
        self.root.title(self.ctrl.settings.get("bootstrapper_title", "NRBlox"))
        self.root.geometry("480x230")
        self.root.configure(bg=p["bg_window"])

        box = tk.Frame(self.root, bg=p["bg_window"], padx=24, pady=20)
        box.pack(fill="both", expand=True)

        tk.Label(box, text="❖  " + self.ctrl.settings.get("bootstrapper_title", "NRBlox"), bg=p["bg_window"], fg=p["text_primary"], font=("Sans", 15, "bold"), anchor="w").pack(fill="x")
        status_lbl = tk.Label(box, text="Starting Roblox...", bg=p["bg_window"], fg=p["text_primary"], font=("Sans", 11), anchor="w")
        status_lbl.pack(fill="x", pady=(16, 2))
        sub_lbl = tk.Label(box, text="Verifying Linux runner and syncing FastFlags...", bg=p["bg_window"], fg=p["text_secondary"], font=("Sans", 9), anchor="w")
        sub_lbl.pack(fill="x", pady=(0, 14))

        track = tk.Frame(box, bg=p["border"], height=6)
        track.pack(fill="x", pady=(0, 16))
        track.pack_propagate(False)
        fill = tk.Frame(track, bg=p["accent"], width=80, height=6)
        fill.pack(side="left")

        footer = tk.Frame(box, bg=p["bg_window"])
        footer.pack(fill="x", side="bottom")
        tk.Label(footer, text=TARGET_VERSION_GUID, bg=p["bg_window"], fg=p["text_secondary"], font=("Sans", 9)).pack(side="left")
        tk.Button(footer, text="Cancel", bg=p["bg_card"], fg=p["text_primary"], relief="flat", padx=14, pady=4, command=self.root.destroy).pack(side="right")

        def step1():
            fill.configure(width=200)
            status_lbl.configure(text="Applying Engine Settings & FastFlags...")
            sub_lbl.configure(text=f"Syncing {len(self.ctrl.fflags)} FastFlags to Sober & Wine...")
            self.root.after(260, step2)

        def step2():
            fill.configure(width=360)
            status_lbl.configure(text="Launching Roblox...")
            sub_lbl.configure(text="Starting Linux graphics pipeline...")
            self.root.after(220, finish)

        def finish():
            ok, msg = self.ctrl.launch_roblox(mode=mode)
            if not ok:
                messagebox.showwarning("NRBlox Linux", msg)
            self.root.destroy()

        self.root.after(220, step1)

    def run(self):
        self.root.mainloop()


def main():
    ctrl = LinuxEngineController()
    args = sys.argv[1:]

    if "--verify" in args:
        print(json.dumps({
            "launcher": "NRBlox Linux Native Edition",
            "version": "3.0.0",
            "runners": ctrl.detect_installed_runners(),
            "active_fflags": len(ctrl.fflags),
        }, indent=2))
        return 0

    uri = next((a for a in args if a.startswith(("roblox-player:", "roblox:", "nrblox:"))), None)
    if "-player" in args or "--player" in args or uri:
        ok, msg = ctrl.launch_roblox(mode="player", uri=uri)
        print(f"[NRBlox-Linux] {msg}")
        return 0 if ok else 1

    if not HAS_TK:
        print("[NRBlox-Linux] Install python3-tk to open the GUI.")
        return 1

    app = NRBloxLinuxApp(ctrl)
    app.run()
    return 0


if __name__ == "__main__":
    sys.exit(main())
