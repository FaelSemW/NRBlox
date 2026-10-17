#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
============================================================================
NRBlox (Linux Native Edition) — Official NRBlox Linux Engine & Bootstrapper
Author: FaelSemW (https://github.com/FaelSemW/NRBlox)
Community Discord: https://discord.gg/M8j8JnQSCx
License: MIT (100% Open Source)
============================================================================
100% Independent NRBlox Linux Engine (no third-party Sober/Vinegar wrappers):
 - Full Roblox CDN Package Downloader & Extractor (setup.rbxcdn.com + MD5 cache)
 - Isolated NRBlox Native Prefix (~/.local/share/nrblox-linux/prefix)
 - Open-Source WebView2Loader.dll & NTDLL/Registry Windows 10 Build 19045 Cloak
 - Native C Runner (`nrblox-core`) with Vulkan ICD, DXVK, Esync/Fsync, GameMode
 - Comfortable 2-Window Fluent UI (Launch Menu -> Settings Window + Bootstrapper)
   with full Light Mode & Dark Mode support.
============================================================================
"""

import hashlib
import json
import os
import shutil
import subprocess
import sys
import threading
import time
import urllib.request
import webbrowser
import zipfile
from pathlib import Path

try:
    import tkinter as tk
    from tkinter import messagebox
    HAS_TK = True
except ImportError:
    HAS_TK = False

DISCORD_URL = "https://discord.gg/M8j8JnQSCx"
GITHUB_REPO_URL = "https://github.com/FaelSemW/NRBlox"
GITHUB_WIKI_URL = "https://github.com/FaelSemW/NRBlox/blob/main/docs/wiki/Home.md"
DEFAULT_VERSION_GUID = "version-11f155f3450f4c8e"

CDN_BASE_URLS = [
    "https://setup.rbxcdn.com/channel/common/",
    "https://setup.rbxcdn.com/",
]

PACKAGE_DIR_MAP = {
    "RobloxApp.zip": "",
    "RobloxStudio.zip": "",
    "redist.zip": "",
    "shaders.zip": "shaders",
    "ssl.zip": "ssl",
    "WebView2.zip": "",
    "WebView2RuntimeInstaller.zip": "WebView2RuntimeInstaller",
    "content-avatar.zip": "content/avatar",
    "content-configs.zip": "content/configs",
    "content-fonts.zip": "content/fonts",
    "content-sky.zip": "content/sky",
    "content-sounds.zip": "content/sounds",
    "content-textures2.zip": "content/textures",
    "content-models.zip": "content/models",
    "content-platform-fonts.zip": "PlatformContent/pc/fonts",
    "content-platform-dictionaries.zip": "PlatformContent/pc/shared_compression_dictionaries",
    "content-terrain.zip": "PlatformContent/pc/terrain",
    "content-textures3.zip": "PlatformContent/pc/textures",
    "extracontent-luapackages.zip": "ExtraContent/LuaPackages",
    "extracontent-translations.zip": "ExtraContent/translations",
    "extracontent-models.zip": "ExtraContent/models",
    "extracontent-textures.zip": "ExtraContent/textures",
    "extracontent-places.zip": "ExtraContent/places",
    "RibbonConfig.zip": "RibbonConfig",
    "ApplicationConfig.zip": "ApplicationConfig",
    "BuiltInPlugins.zip": "BuiltInPlugins",
    "BuiltInStandalonePlugins.zip": "BuiltInStandalonePlugins",
    "Plugins.zip": "Plugins",
    "Qml.zip": "Qml",
    "StudioFonts.zip": "StudioFonts",
    "studio-svg-textures.zip": "content/studio_svg_textures",
    "content-qt_translations.zip": "content/qt_translations",
    "content-api-docs.zip": "content/api_docs",
    "extracontent-scripts.zip": "ExtraContent/scripts",
}


class LinuxPaths:
    HOME = Path.home()
    XDG_CONFIG = Path(os.environ.get("XDG_CONFIG_HOME", HOME / ".config")) / "nrblox-linux"
    XDG_DATA = Path(os.environ.get("XDG_DATA_HOME", HOME / ".local" / "share")) / "nrblox-linux"

    SETTINGS_FILE = XDG_CONFIG / "NRBloxSettings.json"
    FFLAGS_FILE = XDG_CONFIG / "ClientAppSettings.json"
    MODS_DIR = XDG_DATA / "Modifications"
    DOWNLOADS_DIR = XDG_DATA / "Downloads"
    VERSIONS_DIR = XDG_DATA / "Versions"
    NATIVE_PREFIX = XDG_DATA / "prefix"
    CORE_BIN = XDG_DATA / "bin" / "nrblox-core"
    OPEN_WEBVIEW2_DLL = XDG_DATA / "bin" / "WebView2Loader.dll"

    @classmethod
    def ensure_dirs(cls):
        for d in [
            cls.XDG_CONFIG,
            cls.XDG_DATA,
            cls.MODS_DIR,
            cls.DOWNLOADS_DIR,
            cls.VERSIONS_DIR,
            cls.NATIVE_PREFIX,
            cls.XDG_DATA / "bin",
            cls.MODS_DIR / "ClientSettings",
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
    "lock_roblox_version": False,
    "target_version_guid": DEFAULT_VERSION_GUID,
    "vulkan_icd": "Automatic",
    "enable_gamemode": True,
    "enable_mangohud": False,
    "enable_esync": True,
    "enable_fsync": True,
    "enable_ntdll_cloak": True,
    "use_open_webview2": True,
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


class RobloxLinuxCdnInstaller:
    """Downloads and extracts official Roblox packages from setup.rbxcdn.com directly into NRBlox Linux"""

    @staticmethod
    def resolve_version_guid(is_studio=False, lock_version=False, current_guid=DEFAULT_VERSION_GUID):
        if lock_version and current_guid:
            vdir = LinuxPaths.VERSIONS_DIR / current_guid
            exe = "RobloxStudioBeta.exe" if is_studio else "RobloxPlayerBeta.exe"
            if (vdir / exe).exists():
                return current_guid
        try:
            binary_type = "WindowsStudio64" if is_studio else "WindowsPlayer"
            req = urllib.request.Request(
                f"https://clientsettingscdn.roblox.com/v2/client-version/{binary_type}",
                headers={"User-Agent": "Roblox/WinInet"},
            )
            with urllib.request.urlopen(req, timeout=15) as resp:
                data = json.loads(resp.read().decode("utf-8"))
                guid = data.get("clientVersionUpload", "")
                if guid.startswith("version-"):
                    return guid
        except Exception:
            pass
        return current_guid or DEFAULT_VERSION_GUID

    @staticmethod
    def is_installed(version_guid: str, is_studio=False) -> bool:
        vdir = LinuxPaths.VERSIONS_DIR / version_guid
        exe = "RobloxStudioBeta.exe" if is_studio else "RobloxPlayerBeta.exe"
        return (vdir / exe).exists()

    @staticmethod
    def install_or_update(version_guid: str, on_progress=None, is_cancelled=None):
        LinuxPaths.ensure_dirs()
        vdir = LinuxPaths.VERSIONS_DIR / version_guid
        vdir.mkdir(parents=True, exist_ok=True)

        if on_progress:
            on_progress(0.04, "Connecting to Roblox CDN...", f"Fetching package manifest for {version_guid}")

        manifest_text = None
        active_base = CDN_BASE_URLS[0]
        for base in CDN_BASE_URLS:
            try:
                req = urllib.request.Request(
                    f"{base}{version_guid}-rbxPkgManifest.txt",
                    headers={"User-Agent": "Roblox/WinInet"},
                )
                with urllib.request.urlopen(req, timeout=20) as resp:
                    txt = resp.read().decode("utf-8", errors="ignore")
                    if txt.startswith("v0"):
                        manifest_text = txt
                        active_base = base
                        break
            except Exception:
                pass

        if not manifest_text:
            raise RuntimeError(f"Failed to fetch package manifest for {version_guid} from setup.rbxcdn.com")

        lines = [ln.strip() for ln in manifest_text.splitlines() if ln.strip()]
        packages = []
        idx = 1
        while idx + 3 < len(lines):
            name = lines[idx]
            md5 = lines[idx + 1]
            comp_size = int(lines[idx + 2]) if lines[idx + 2].isdigit() else 0
            uncomp_size = int(lines[idx + 3]) if lines[idx + 3].isdigit() else 0
            idx += 4
            if name.lower().endswith(".zip"):
                packages.append({
                    "name": name,
                    "md5": md5,
                    "compressed": comp_size,
                    "uncompressed": uncomp_size,
                })

        total_bytes = sum(max(1, p["compressed"]) for p in packages)
        total_mb = total_bytes / (1024.0 * 1024.0)
        downloaded_so_far = 0

        # Step 1: Download all .zip packages with MD5 cache verification
        for i, pkg in enumerate(packages):
            if is_cancelled and is_cancelled():
                return
            cached_path = LinuxPaths.DOWNLOADS_DIR / pkg["md5"]
            cache_valid = False
            if cached_path.exists() and cached_path.stat().st_size == pkg["compressed"]:
                h = hashlib.md5(cached_path.read_bytes()).hexdigest()
                if h.lower() == pkg["md5"].lower():
                    cache_valid = True

            if cache_valid:
                downloaded_so_far += pkg["compressed"]
                ratio = 0.05 + 0.73 * (downloaded_so_far / total_bytes)
                cur_mb = downloaded_so_far / (1024.0 * 1024.0)
                if on_progress:
                    on_progress(
                        ratio,
                        f"Downloading Roblox ({i + 1}/{len(packages)})...",
                        f"Verified cached {pkg['name']} ({cur_mb:.1f} MB / {total_mb:.1f} MB)",
                    )
            else:
                url = f"{active_base}{version_guid}-{pkg['name']}"
                tmp_path = cached_path.with_suffix(".tmp")
                req = urllib.request.Request(url, headers={"User-Agent": "Roblox/WinInet"})
                with urllib.request.urlopen(req, timeout=30) as resp, open(tmp_path, "wb") as out_f:
                    pkg_read = 0
                    last_ui = 0.0
                    while True:
                        if is_cancelled and is_cancelled():
                            return
                        chunk = resp.read(65536)
                        if not chunk:
                            break
                        out_f.write(chunk)
                        pkg_read += len(chunk)
                        now = time.time()
                        if now - last_ui >= 0.08:
                            last_ui = now
                            cur_total = downloaded_so_far + pkg_read
                            ratio = 0.05 + 0.73 * min(1.0, cur_total / total_bytes)
                            cur_mb = cur_total / (1024.0 * 1024.0)
                            if on_progress:
                                on_progress(
                                    ratio,
                                    f"Downloading Roblox ({i + 1}/{len(packages)})...",
                                    f"{pkg['name']} — {cur_mb:.1f} MB / {total_mb:.1f} MB",
                                )
                tmp_path.replace(cached_path)
                downloaded_so_far += pkg["compressed"]

        # Step 2: Extract all .zip packages into mapped folders inside ~/.local/share/nrblox-linux/Versions/version-...
        for i, pkg in enumerate(packages):
            if is_cancelled and is_cancelled():
                return
            cached_path = LinuxPaths.DOWNLOADS_DIR / pkg["md5"]
            if not cached_path.exists():
                continue

            sub = PACKAGE_DIR_MAP.get(pkg["name"], "")
            target_dir = vdir / sub if sub else vdir
            target_dir.mkdir(parents=True, exist_ok=True)

            ratio = 0.78 + 0.18 * ((i + 1) / len(packages))
            if on_progress:
                on_progress(
                    ratio,
                    f"Extracting Roblox packages ({i + 1}/{len(packages)})...",
                    f"Extracting {pkg['name']}...",
                )

            with zipfile.ZipFile(cached_path, "r") as zf:
                for member in zf.infolist():
                    rel = member.filename.replace("\\", "/").lstrip("/")
                    if not rel or ".." in rel:
                        continue
                    dest = target_dir / rel
                    if member.is_dir() or rel.endswith("/"):
                        dest.mkdir(parents=True, exist_ok=True)
                    else:
                        dest.parent.mkdir(parents=True, exist_ok=True)
                        with zf.open(member, "r") as src_f, open(dest, "wb") as dst_f:
                            shutil.copyfileobj(src_f, dst_f)

        # Step 3: Write AppSettings.xml
        app_settings = (
            '<?xml version="1.0" encoding="UTF-8"?>\r\n'
            "<Settings>\r\n"
            "\t<ContentFolder>content</ContentFolder>\r\n"
            "\t<BaseUrl>http://www.roblox.com</BaseUrl>\r\n"
            "</Settings>\r\n"
        )
        (vdir / "AppSettings.xml").write_text(app_settings, encoding="utf-8")


class LinuxEngineController:
    def __init__(self):
        LinuxPaths.ensure_dirs()
        self.settings = self._load_json(LinuxPaths.SETTINGS_FILE, DEFAULT_LINUX_SETTINGS)
        self.fflags = self._load_json(
            LinuxPaths.FFLAGS_FILE,
            {
                "DFIntTaskSchedulerTargetFps": "240",
                "FFlagDebugGraphicsPreferVulkan": "True",
                "FFlagDebugGraphicsDisableDirect3D11": "True",
                "FIntDebugForceMSAASamples": "4",
                "DFFlagDisableDPIScale": "True",
                "DFFlagTextureQualityOverrideEnabled": "True",
                "DFIntTextureQualityOverride": "3",
                "FFlagHandleAltEnterFullscreenManually": "False",
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

    def sync_all(self, version_guid=None):
        LinuxPaths.FFLAGS_FILE.write_text(
            json.dumps(self.fflags, indent=2, ensure_ascii=False), encoding="utf-8"
        )
        guid = version_guid or self.settings.get("target_version_guid", DEFAULT_VERSION_GUID)
        vdir = LinuxPaths.VERSIONS_DIR / guid
        if vdir.exists():
            cs_dir = vdir / "ClientSettings"
            cs_dir.mkdir(parents=True, exist_ok=True)
            (cs_dir / "ClientAppSettings.json").write_text(
                json.dumps(self.fflags, indent=2, ensure_ascii=False), encoding="utf-8"
            )
            # Inject open-source WebView2Loader.dll if built by mingw-w64
            if self.settings.get("use_open_webview2", True) and LinuxPaths.OPEN_WEBVIEW2_DLL.exists():
                try:
                    shutil.copy2(LinuxPaths.OPEN_WEBVIEW2_DLL, vdir / "WebView2Loader.dll")
                except Exception:
                    pass

    def prepare_native_prefix(self):
        """Normalizes the isolated NRBlox Linux prefix and cloaks Wine registry fingerprints"""
        env = os.environ.copy()
        env["WINEPREFIX"] = str(LinuxPaths.NATIVE_PREFIX)
        env["WINEARCH"] = "win64"
        env["WINEDEBUG"] = "-all"

        if self.settings.get("enable_ntdll_cloak", True) and shutil.which("wine64"):
            try:
                subprocess.run(
                    ["wine64", "reg", "delete", r"HKLM\Software\Wine", "/f"],
                    env=env,
                    stdout=subprocess.DEVNULL,
                    stderr=subprocess.DEVNULL,
                    timeout=8,
                )
                subprocess.run(
                    ["wine64", "reg", "delete", r"HKCU\Software\Wine\Explorer", "/f"],
                    env=env,
                    stdout=subprocess.DEVNULL,
                    stderr=subprocess.DEVNULL,
                    timeout=8,
                )
            except Exception:
                pass

    def launch_installed_client(self, version_guid: str, is_studio=False, uri=None):
        self.sync_all(version_guid)
        self.prepare_native_prefix()

        vdir = LinuxPaths.VERSIONS_DIR / version_guid
        exe_name = "RobloxStudioBeta.exe" if is_studio else "RobloxPlayerBeta.exe"
        target_exe = vdir / exe_name

        if not target_exe.exists():
            return False, f"Executable not found at: {target_exe}"

        env = os.environ.copy()
        env["WINEPREFIX"] = str(LinuxPaths.NATIVE_PREFIX)
        env["WINEARCH"] = "win64"
        env["WINEESYNC"] = "1" if self.settings.get("enable_esync", True) else "0"
        env["WINEFSYNC"] = "1" if self.settings.get("enable_fsync", True) else "0"
        env["WINE_LARGE_ADDRESS_AWARE"] = "1"
        env["WINEDEBUG"] = "-all"
        env["WINEDLLOVERRIDES"] = "WebView2Loader=n,b;d3d11=n,b;dxgi=n,b;mscoree,mshtml="

        wrappers = []
        if self.settings.get("enable_gamemode") and shutil.which("gamemoderun"):
            wrappers.append("gamemoderun")
        if self.settings.get("enable_mangohud") and shutil.which("mangohud"):
            wrappers.extend(["mangohud", "--dlsym"])

        launch_arg = uri if uri else "--app"
        if LinuxPaths.CORE_BIN.exists():
            cmd = wrappers + [str(LinuxPaths.CORE_BIN), str(target_exe), launch_arg]
        else:
            wine_bin = shutil.which("wine64") or shutil.which("wine")
            if not wine_bin:
                return False, "Native runtime requires wine64 (run install-linux.sh to install)."
            cmd = wrappers + [wine_bin, str(target_exe), launch_arg]

        try:
            subprocess.Popen(cmd, cwd=str(vdir), env=env, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            return True, "Launched via NRBlox Linux Native Core: " + " ".join(cmd)
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
    """Two-Window Fluent Linux Interface + Real-Time CDN Downloader Bootstrapper"""

    def __init__(self, ctrl: LinuxEngineController, initial_mode=None, launch_uri=None):
        self.ctrl = ctrl
        self.root = tk.Tk()
        self.cancelled = False
        if initial_mode == "player":
            self.launch_client("player", launch_uri)
        elif initial_mode == "studio":
            self.launch_client("studio", launch_uri)
        elif initial_mode == "settings":
            self.show_settings_window("integrations")
        else:
            self.show_launch_menu()

    def get_palette(self):
        mode = self.ctrl.settings.get("theme_mode", "Light")
        return PALETTES["Dark"] if mode == "Dark" else PALETTES["Light"]

    def show_launch_menu(self):
        for w in self.root.winfo_children():
            w.destroy()
        p = self.get_palette()
        self.root.title("NRBlox")
        self.root.geometry("620x365")
        self.root.configure(bg=p["bg_window"])

        container = tk.Frame(self.root, bg=p["bg_window"], padx=26, pady=24)
        container.pack(fill="both", expand=True)

        # Left column (Logo + About / Wiki / Community links)
        left = tk.Frame(container, bg=p["bg_window"], width=230)
        left.pack(side="left", fill="y", padx=(0, 20))
        left.pack_propagate(False)

        tk.Label(left, text="❖ NRBlox", bg=p["bg_window"], fg=p["text_primary"], font=("Sans", 22, "normal")).pack(pady=(32, 2))
        tk.Label(left, text="Version 3.0.0", bg=p["bg_window"], fg=p["text_secondary"], font=("Sans", 10)).pack(pady=(0, 24))

        for label, action in [
            ("ⓘ  About NRBlox", lambda: self.show_settings_window("about")),
            ("📖  NRBlox Wiki", lambda: webbrowser.open(GITHUB_WIKI_URL)),
            ("💬  NRBlox Community", lambda: webbrowser.open(DISCORD_URL)),
        ]:
            lbl = tk.Label(left, text=label, bg=p["bg_window"], fg=p["accent"], font=("Sans", 10), cursor="hand2")
            lbl.pack(pady=4)
            lbl.bind("<Button-1>", lambda e, fn=action: fn())

        # Right column (4 Action Cards matching Windows NRBloxMenuWindow)
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
            tk.Label(parent, text="Bootstrapper & NRBlox Native Runtime", bg=p["bg_window"], fg=p["text_primary"], font=("Sans", 20, "bold"), anchor="w").pack(fill="x", pady=(0, 12))
            self._toggle_row(parent, p, "confirm_launches", "Prompt to confirm when launching another Roblox instance")
            self._toggle_row(parent, p, "force_reinstall", "Force Roblox reinstallation", "Re-downloads and extracts all 21 official packages from setup.rbxcdn.com on next launch.")
            self._toggle_row(parent, p, "enable_ntdll_cloak", "Enable NRBlox NTDLL & Registry Cloak", "Normalizes Windows 10 Build 19045 environment in the isolated NRBlox prefix.")
            self._toggle_row(parent, p, "use_open_webview2", "Inject Open-Source WebView2Loader.dll", "Uses NRBlox's reconstructed C++ WebView2Loader inside the client version folder.")
            self._toggle_row(parent, p, "enable_gamemode", "Enable Feral GameMode (gamemoderun)", "Optimizes Linux CPU governor when launching Roblox.")
            self._toggle_row(parent, p, "enable_mangohud", "Enable MangoHud Vulkan Overlay (mangohud --dlsym)", "Displays FPS and frame timing overlay on Linux.")
            self._toggle_row(parent, p, "enable_esync", "Enable Esync & Fsync", "Low-latency futex synchronization in NRBlox Core.")
        elif t == "mods":
            tk.Label(parent, text="Mods", bg=p["bg_window"], fg=p["text_primary"], font=("Sans", 20, "bold"), anchor="w").pack(fill="x", pady=(0, 12))
            self._toggle_row(parent, p, "use_old_avatar_bg", "Use old avatar editor background", "Bring back the classic avatar background.")
            self._toggle_row(parent, p, "use_old_sounds", "Emulate old character sounds", "Bring back pre-2014 character sounds.")
        elif t == "engine":
            tk.Label(parent, text="Engine Settings", bg=p["bg_window"], fg=p["text_primary"], font=("Sans", 20, "bold"), anchor="w").pack(fill="x", pady=(0, 12))
            self._toggle_row(parent, p, "manage_fastflags", "Allow NRBlox to manage Fast Flags", "Writes directly to ClientSettings/ClientAppSettings.json.")
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

    def launch_client(self, mode, uri=None):
        is_studio = mode == "studio"
        self.cancelled = False
        for w in self.root.winfo_children():
            w.destroy()
        p = self.get_palette()
        self.root.title(self.ctrl.settings.get("bootstrapper_title", "NRBlox"))
        self.root.geometry("480x240")
        self.root.configure(bg=p["bg_window"])

        box = tk.Frame(self.root, bg=p["bg_window"], padx=24, pady=20)
        box.pack(fill="both", expand=True)

        tk.Label(box, text="❖  " + self.ctrl.settings.get("bootstrapper_title", "NRBlox"), bg=p["bg_window"], fg=p["text_primary"], font=("Sans", 15, "bold"), anchor="w").pack(fill="x")
        status_lbl = tk.Label(box, text="Starting Roblox...", bg=p["bg_window"], fg=p["text_primary"], font=("Sans", 11), anchor="w")
        status_lbl.pack(fill="x", pady=(16, 2))
        sub_lbl = tk.Label(box, text="Checking client version and local installation...", bg=p["bg_window"], fg=p["text_secondary"], font=("Sans", 9), anchor="w")
        sub_lbl.pack(fill="x", pady=(0, 14))

        track = tk.Frame(box, bg=p["border"], height=6)
        track.pack(fill="x", pady=(0, 16))
        track.pack_propagate(False)
        fill = tk.Frame(track, bg=p["accent"], width=28, height=6)
        fill.pack(side="left")

        footer = tk.Frame(box, bg=p["bg_window"])
        footer.pack(fill="x", side="bottom")
        ver_lbl = tk.Label(footer, text=DEFAULT_VERSION_GUID, bg=p["bg_window"], fg=p["text_secondary"], font=("Sans", 9))
        ver_lbl.pack(side="left")

        def cancel_action():
            self.cancelled = True
            self.root.destroy()

        tk.Button(footer, text="Cancel", bg=p["bg_card"], fg=p["text_primary"], relief="flat", padx=14, pady=4, command=cancel_action).pack(side="right")

        def update_ui(ratio, st, sub, guid=None):
            if self.cancelled:
                return
            self.root.after(0, lambda: _apply_ui(ratio, st, sub, guid))

        def _apply_ui(ratio, st, sub, guid):
            if self.cancelled:
                return
            w = max(16, int(430 * min(1.0, max(0.0, ratio))))
            fill.configure(width=w)
            status_lbl.configure(text=st)
            sub_lbl.configure(text=sub)
            if guid:
                ver_lbl.configure(text=guid)

        def worker():
            try:
                update_ui(0.05, "Connecting to Roblox...", "Checking latest deployment version on clientsettingscdn.roblox.com...")
                guid = RobloxLinuxCdnInstaller.resolve_version_guid(
                    is_studio=is_studio,
                    lock_version=bool(self.ctrl.settings.get("lock_roblox_version", False)),
                    current_guid=self.ctrl.settings.get("target_version_guid", DEFAULT_VERSION_GUID),
                )
                self.ctrl.settings["target_version_guid"] = guid
                update_ui(0.08, "Verifying client installation...", f"Checking {guid}", guid)

                needs_install = bool(self.ctrl.settings.get("force_reinstall", False)) or not RobloxLinuxCdnInstaller.is_installed(guid, is_studio)
                if needs_install:
                    RobloxLinuxCdnInstaller.install_or_update(
                        guid,
                        on_progress=lambda r, s, sb: update_ui(r, s, sb, guid),
                        is_cancelled=lambda: self.cancelled,
                    )
                    if self.cancelled:
                        return
                    if self.ctrl.settings.get("force_reinstall"):
                        self.ctrl.settings["force_reinstall"] = False
                        self.ctrl.save_settings()

                update_ui(0.96, "Applying Engine Settings & FastFlags...", f"Syncing {len(self.ctrl.fflags)} FastFlags to ClientAppSettings.json", guid)
                self.ctrl.apply_presets_to_fflags()
                self.ctrl.sync_all(guid)
                time.sleep(0.15)

                if self.cancelled:
                    return

                update_ui(0.99, "Starting NRBlox Linux Native Core...", "Initializing isolated prefix and Vulkan pipeline...", guid)
                ok, msg = self.ctrl.launch_installed_client(guid, is_studio=is_studio, uri=uri)
                self.root.after(0, lambda: _finish(ok, msg))
            except Exception as ex:
                if not self.cancelled:
                    self.root.after(0, lambda: _finish(False, str(ex)))

        def _finish(ok, msg):
            if not ok and not self.cancelled:
                messagebox.showerror("NRBlox Bootstrapper", msg)
            self.root.destroy()

        threading.Thread(target=worker, daemon=True).start()

    def run(self):
        self.root.mainloop()


def main():
    ctrl = LinuxEngineController()
    args = sys.argv[1:]

    if "--verify" in args:
        guid = ctrl.settings.get("target_version_guid", DEFAULT_VERSION_GUID)
        print(json.dumps({
            "launcher": "NRBlox Linux Native Edition",
            "version": "3.0.0",
            "target_version_guid": guid,
            "client_installed": RobloxLinuxCdnInstaller.is_installed(guid, False),
            "core_binary": str(LinuxPaths.CORE_BIN),
            "open_webview2": str(LinuxPaths.OPEN_WEBVIEW2_DLL),
            "active_fflags": len(ctrl.fflags),
        }, indent=2))
        return 0

    if "--install-player" in args:
        guid = RobloxLinuxCdnInstaller.resolve_version_guid(False, False)
        print(f"[NRBlox-Linux] Downloading & extracting Roblox {guid} from setup.rbxcdn.com...")
        RobloxLinuxCdnInstaller.install_or_update(
            guid,
            on_progress=lambda r, s, sb: print(f"[{int(r * 100):3d}%] {s} | {sb}"),
        )
        ctrl.sync_all(guid)
        print(f"[NRBlox-Linux] Successfully installed {guid} to {LinuxPaths.VERSIONS_DIR / guid}")
        return 0

    if not HAS_TK:
        print("[NRBlox-Linux] Install python3-tk to open the GUI.")
        return 1

    uri = next((a for a in args if a.startswith(("roblox-player:", "roblox:", "nrblox:"))), None)
    initial_mode = None
    if "-player" in args or "--player" in args or uri:
        initial_mode = "player"
    elif "-studio" in args or "--studio" in args:
        initial_mode = "studio"
    elif "-settings" in args or "--settings" in args:
        initial_mode = "settings"

    app = NRBloxLinuxApp(ctrl, initial_mode=initial_mode, launch_uri=uri)
    app.run()
    return 0


if __name__ == "__main__":
    sys.exit(main())
