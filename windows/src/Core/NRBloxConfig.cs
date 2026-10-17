// ============================================================================
// NRBlox (Windows Native Edition) — Core Configuration, FastFlags & Mod Engine
// Author: FaelSemW (https://github.com/FaelSemW/NRBlox)
// Community Discord: https://discord.gg/M8j8JnQSCx
// License: MIT (100% Open Source)
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Web.Script.Serialization;

namespace NRBlox.Windows.Core
{
    public static class NRBloxPaths
    {
        public static readonly string DiscordUrl = "https://discord.gg/M8j8JnQSCx";
        public static readonly string GitHubRepoUrl = "https://github.com/FaelSemW/NRBlox";
        public static readonly string GitHubWikiUrl = "https://github.com/FaelSemW/NRBlox/blob/main/docs/wiki/Home.md";
        public static string TargetVersionGuid = "version-11f155f3450f4c8e";

        public static string BaseDir
        {
            get
            {
                string asmLoc = typeof(NRBloxPaths).Assembly.Location;
                string exeDir = !string.IsNullOrEmpty(asmLoc)
                    ? Path.GetDirectoryName(asmLoc)
                    : AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');

                if (Directory.Exists(Path.Combine(exeDir, "assets")) || Directory.Exists(Path.Combine(exeDir, "windows")))
                    return exeDir;
                string parent = Path.GetFullPath(Path.Combine(exeDir, "..", ".."));
                if (Directory.Exists(Path.Combine(parent, "assets")))
                    return parent;

                // Standalone single-file mode: install/store into %LocalAppData%\NRBlox
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "NRBlox"
                );
            }
        }

        public static string DataDir { get { return Path.Combine(BaseDir, "data"); } }
        public static string DownloadsCacheDir
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Roblox",
                    "Downloads"
                );
            }
        }
        public static string AssetsDir { get { return Path.Combine(BaseDir, "assets"); } }
        public static string ModsAssetsDir { get { return Path.Combine(AssetsDir, "mods"); } }
        public static string IconsDir { get { return Path.Combine(AssetsDir, "icons"); } }
        public static string ModificationsDir { get { return Path.Combine(DataDir, "Modifications"); } }
        public static string SettingsFile { get { return Path.Combine(DataDir, "NRBloxSettings.json"); } }
        public static string ServerHistoryFile { get { return Path.Combine(DataDir, "ServerHistory.json"); } }
        public static string LocalClientAppSettingsFile
        {
            get { return Path.Combine(ModificationsDir, "ClientSettings", "ClientAppSettings.json"); }
        }

        public static string SystemRobloxRoot
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Roblox"
                );
            }
        }

        public static string GetRobloxVersionDir()
        {
            string preferred = Path.Combine(SystemRobloxRoot, "Versions", TargetVersionGuid);
            if (Directory.Exists(preferred) && File.Exists(Path.Combine(preferred, "RobloxPlayerBeta.exe")))
                return preferred;

            string versionsRoot = Path.Combine(SystemRobloxRoot, "Versions");
            if (Directory.Exists(versionsRoot))
            {
                foreach (string dir in Directory.GetDirectories(versionsRoot, "version-*"))
                {
                    if (File.Exists(Path.Combine(dir, "RobloxPlayerBeta.exe")))
                        return dir;
                }
            }
            return preferred;
        }

        public static string GetRobloxLogsDir()
        {
            return Path.Combine(SystemRobloxRoot, "logs");
        }

        public static void EnsureDirectories()
        {
            try
            {
                string[] dirs = new string[]
                {
                    BaseDir,
                    DataDir,
                    AssetsDir,
                    ModificationsDir,
                    Path.Combine(ModificationsDir, "ClientSettings"),
                    Path.Combine(ModificationsDir, "content", "sounds"),
                    Path.Combine(ModificationsDir, "content", "fonts"),
                    Path.Combine(ModificationsDir, "content", "textures", "Cursors", "KeyboardMouse"),
                    Path.Combine(ModificationsDir, "ExtraContent", "places")
                };
                foreach (string d in dirs)
                {
                    if (!Directory.Exists(d))
                        Directory.CreateDirectory(d);
                }

                // If running as a standalone downloaded .exe, copy NRBlox.exe into BaseDir for protocol/shortcut persistence
                string currentExe = typeof(NRBloxPaths).Assembly.Location;
                string targetExe = Path.Combine(BaseDir, "NRBlox.exe");
                if (!string.IsNullOrEmpty(currentExe) &&
                    File.Exists(currentExe) &&
                    !string.Equals(Path.GetFullPath(currentExe), Path.GetFullPath(targetExe), StringComparison.OrdinalIgnoreCase))
                {
                    try { File.Copy(currentExe, targetExe, true); } catch { }
                }

                // Auto-extract embedded NRBlox.Assets.zip if assets are not yet extracted
                string markerIcon = Path.Combine(IconsDir, "IconNRBlox.ico");
                string markerPlace = Path.Combine(ModsAssetsDir, "places", "OldAvatarBackground.rbxl");
                if (!File.Exists(markerIcon) || !File.Exists(markerPlace))
                {
                    using (Stream resStream = typeof(NRBloxPaths).Assembly.GetManifestResourceStream("NRBlox.Assets.zip"))
                    {
                        if (resStream != null)
                        {
                            using (ZipArchive archive = new ZipArchive(resStream, ZipArchiveMode.Read))
                            {
                                foreach (ZipArchiveEntry entry in archive.Entries)
                                {
                                    string rel = entry.FullName.Replace('/', '\\').TrimStart('\\');
                                    if (string.IsNullOrEmpty(rel) || rel.Contains(".."))
                                        continue;
                                    if (string.IsNullOrEmpty(entry.Name) || rel.EndsWith("\\"))
                                    {
                                        string subDir = Path.Combine(AssetsDir, rel.TrimEnd('\\'));
                                        if (!Directory.Exists(subDir)) Directory.CreateDirectory(subDir);
                                        continue;
                                    }
                                    string destFile = Path.Combine(AssetsDir, rel);
                                    string destParent = Path.GetDirectoryName(destFile);
                                    if (!string.IsNullOrEmpty(destParent) && !Directory.Exists(destParent))
                                        Directory.CreateDirectory(destParent);
                                    entry.ExtractToFile(destFile, true);
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }

    public sealed class CustomIntegrationItem
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public string LaunchArgs { get; set; }
        public bool AutoClose { get; set; }

        public CustomIntegrationItem()
        {
            Name = "New Integration";
            Location = "";
            LaunchArgs = "";
            AutoClose = true;
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? "New Integration" : Name;
        }
    }

    public sealed class NRBloxSettings
    {
        // Appearance & Global
        public string ThemeMode { get; set; } // "Light", "Dark", "System default"
        public string Language { get; set; }
        public string BootstrapperStyle { get; set; }
        public string BootstrapperIcon { get; set; }
        public string BootstrapperTitle { get; set; }
        public string BootstrapperCustomIconPath { get; set; }
        public bool TestMode { get; set; }

        // Integrations
        public bool EnableActivityTracking { get; set; }
        public bool QueryServerLocation { get; set; }
        public bool DisableDesktopAppPatch { get; set; }
        public bool EnableDiscordRichPresence { get; set; }
        public bool AllowActivityJoining { get; set; }
        public bool ShowAccountOnRichPresence { get; set; }
        public bool ShowServerDetails { get; set; }
        public List<CustomIntegrationItem> CustomIntegrationItems { get; set; }
        public List<string> CustomIntegrations { get; set; }

        // Bootstrapper
        public bool ConfirmLaunches { get; set; }
        public bool BackgroundUpdates { get; set; }
        public bool ForceRobloxReinstall { get; set; }
        public bool MultiInstanceLaunching { get; set; }
        public bool LockRobloxVersion { get; set; }
        public string Channel { get; set; }

        // Mods
        public string CursorType { get; set; }
        public bool UseOldAvatarBackground { get; set; }
        public bool UseOldCharacterSounds { get; set; }
        public bool UseClassicDeathSound { get; set; }
        public string PreferredEmojiType { get; set; }
        public string CustomFontPath { get; set; }
        public List<string> AppliedModManifest { get; set; }

        // Engine Settings (FastFlags)
        public bool UseFastFlagManager { get; set; }
        public int TargetFps { get; set; }
        public string RenderingMode { get; set; }
        public string LightingMode { get; set; }
        public string MsaaMode { get; set; }
        public bool DisableDpiScale { get; set; }
        public string TextureQuality { get; set; }
        public bool ManualFullscreen { get; set; }
        public bool DisablePostFx { get; set; }
        public bool DisableTelemetry { get; set; }
        public int MtuSize { get; set; }
        public bool ResetEverythingToDefaults { get; set; }

        // Shortcuts
        public bool ExtractIconsToFolder { get; set; }
        public bool ShortcutDesktopIcon { get; set; }
        public bool ShortcutStartMenuIcon { get; set; }
        public bool ShortcutLaunchRoblox { get; set; }
        public bool ShortcutLaunchStudio { get; set; }
        public bool ShortcutSettings { get; set; }

        // NRBlox Behaviour
        public bool AutoUpdateNRBlox { get; set; }
        public bool EnableAnalytics { get; set; }
        public bool ExportConfigCheck { get; set; }
        public bool ExportLogsCheck { get; set; }

        public NRBloxSettings()
        {
            ThemeMode = "Light";
            Language = "System default";
            BootstrapperStyle = "NRBlox";
            BootstrapperIcon = "NRBlox";
            BootstrapperTitle = "NRBlox";
            BootstrapperCustomIconPath = "";
            TestMode = false;

            EnableActivityTracking = true;
            QueryServerLocation = true;
            DisableDesktopAppPatch = false;
            EnableDiscordRichPresence = true;
            AllowActivityJoining = true;
            ShowAccountOnRichPresence = true;
            ShowServerDetails = true;
            CustomIntegrationItems = new List<CustomIntegrationItem>();
            CustomIntegrations = new List<string>();

            ConfirmLaunches = false;
            BackgroundUpdates = false;
            ForceRobloxReinstall = false;
            MultiInstanceLaunching = false;
            LockRobloxVersion = true;
            Channel = "production";

            CursorType = "Default";
            UseOldAvatarBackground = false;
            UseOldCharacterSounds = false;
            UseClassicDeathSound = false;
            PreferredEmojiType = "Default (Twemoji)";
            CustomFontPath = "";
            AppliedModManifest = new List<string>();

            UseFastFlagManager = true;
            TargetFps = 240;
            RenderingMode = "D3D11";
            LightingMode = "Chosen by game";
            MsaaMode = "4x";
            DisableDpiScale = true;
            TextureQuality = "Level 3 (Highest)";
            ManualFullscreen = true;
            DisablePostFx = false;
            DisableTelemetry = true;
            MtuSize = 0;
            ResetEverythingToDefaults = false;

            ExtractIconsToFolder = false;
            ShortcutDesktopIcon = true;
            ShortcutStartMenuIcon = true;
            ShortcutLaunchRoblox = false;
            ShortcutLaunchStudio = false;
            ShortcutSettings = false;

            AutoUpdateNRBlox = true;
            EnableAnalytics = false;
            ExportConfigCheck = true;
            ExportLogsCheck = true;
        }
    }

    public static class JsonHelper
    {
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        public static T LoadOrCreate<T>(string path) where T : new()
        {
            try
            {
                if (File.Exists(path))
                {
                    string raw = File.ReadAllText(path, Encoding.UTF8);
                    T obj = Serializer.Deserialize<T>(raw);
                    if (obj != null) return obj;
                }
            }
            catch { }
            T def = new T();
            Save(path, def);
            return def;
        }

        public static void Save(string path, object data)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                string raw = Serializer.Serialize(data);
                File.WriteAllText(path, FormatJson(raw), Encoding.UTF8);
            }
            catch { }
        }

        public static Dictionary<string, string> LoadStringDict(string path)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                if (File.Exists(path))
                {
                    string raw = File.ReadAllText(path, Encoding.UTF8);
                    var parsed = Serializer.Deserialize<Dictionary<string, object>>(raw);
                    if (parsed != null)
                    {
                        foreach (var kv in parsed)
                        {
                            result[kv.Key] = kv.Value != null ? kv.Value.ToString() : "";
                        }
                    }
                }
            }
            catch { }
            return result;
        }

        public static void SaveStringDict(string path, Dictionary<string, string> dict)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var sb = new StringBuilder();
                sb.AppendLine("{");
                var keys = new List<string>(dict.Keys);
                for (int i = 0; i < keys.Count; i++)
                {
                    string k = EscapeJson(keys[i]);
                    string v = EscapeJson(dict[keys[i]] ?? "");
                    sb.Append("  \"").Append(k).Append("\": \"").Append(v).Append("\"");
                    if (i < keys.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.AppendLine("}");
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        public static string FormatJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return "{}";
            var sb = new StringBuilder();
            int indent = 0;
            bool inQuotes = false;
            for (int i = 0; i < json.Length; i++)
            {
                char ch = json[i];
                if (ch == '"' && (i == 0 || json[i - 1] != '\\'))
                {
                    inQuotes = !inQuotes;
                    sb.Append(ch);
                }
                else if (!inQuotes)
                {
                    if (ch == '{' || ch == '[')
                    {
                        sb.Append(ch);
                        sb.AppendLine();
                        indent++;
                        sb.Append(new string(' ', indent * 2));
                    }
                    else if (ch == '}' || ch == ']')
                    {
                        sb.AppendLine();
                        indent = Math.Max(0, indent - 1);
                        sb.Append(new string(' ', indent * 2));
                        sb.Append(ch);
                    }
                    else if (ch == ',')
                    {
                        sb.Append(ch);
                        sb.AppendLine();
                        sb.Append(new string(' ', indent * 2));
                    }
                    else if (ch == ':')
                    {
                        sb.Append(": ");
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }
    }

    public sealed class FastFlagEngine
    {
        public Dictionary<string, string> Flags { get; private set; }
        private readonly NRBloxSettings _settings;

        public FastFlagEngine(NRBloxSettings settings)
        {
            _settings = settings;
            Flags = new Dictionary<string, string>(StringComparer.Ordinal);
            Load();
        }

        public void Load()
        {
            NRBloxPaths.EnsureDirectories();
            string versionClientSettings = Path.Combine(NRBloxPaths.GetRobloxVersionDir(), "ClientSettings", "ClientAppSettings.json");

            if (File.Exists(NRBloxPaths.LocalClientAppSettingsFile))
            {
                Flags = JsonHelper.LoadStringDict(NRBloxPaths.LocalClientAppSettingsFile);
            }
            else if (File.Exists(versionClientSettings))
            {
                Flags = JsonHelper.LoadStringDict(versionClientSettings);
                SaveAndSync();
            }
            else
            {
                ApplySettingsToFlags();
                SaveAndSync();
            }
        }

        public void ApplySettingsToFlags()
        {
            if (_settings.ResetEverythingToDefaults)
            {
                Flags.Clear();
                Flags["FFlagHandleAltEnterFullscreenManually"] = "False";
                Flags["FIntDebugForceMSAASamples"] = "4";
                Flags["DFFlagDisableDPIScale"] = "True";
                Flags["DFFlagTextureQualityOverrideEnabled"] = "True";
                Flags["DFIntTextureQualityOverride"] = "3";
                _settings.ResetEverythingToDefaults = false;
                return;
            }

            // FPS Unlocker
            SetOrRemove("DFIntTaskSchedulerTargetFps", _settings.TargetFps > 0 ? _settings.TargetFps.ToString() : null);

            // Exclusive Fullscreen & Display Scaling
            SetOrRemove("FFlagHandleAltEnterFullscreenManually", _settings.ManualFullscreen ? "False" : null);
            SetOrRemove("DFFlagDisableDPIScale", _settings.DisableDpiScale ? "True" : null);

            // MSAA ("Automatic", "1x", "2x", "4x")
            string msaaClean = (_settings.MsaaMode ?? "").Replace("x", "").Trim();
            if (msaaClean == "1" || msaaClean == "2" || msaaClean == "4" || msaaClean == "8")
                SetOrRemove("FIntDebugForceMSAASamples", msaaClean);
            else
                SetOrRemove("FIntDebugForceMSAASamples", null);

            // Texture Quality ("Automatic", "Level 0 (Lowest)", "Level 1 (Low)", "Level 2 (Medium)", "Level 3 (Highest)")
            string tex = _settings.TextureQuality ?? "";
            string texLevel = null;
            if (tex.Contains("0") || tex == "0") texLevel = "0";
            else if (tex.Contains("1") || tex == "1") texLevel = "1";
            else if (tex.Contains("2") || tex == "2") texLevel = "2";
            else if (tex.Contains("3") || tex == "3") texLevel = "3";

            if (texLevel == null)
            {
                SetOrRemove("DFFlagTextureQualityOverrideEnabled", null);
                SetOrRemove("DFIntTextureQualityOverride", null);
            }
            else
            {
                SetOrRemove("DFFlagTextureQualityOverrideEnabled", "True");
                SetOrRemove("DFIntTextureQualityOverride", texLevel);
            }

            // Rendering API
            SetOrRemove("FFlagDebugGraphicsPreferD3D11", null);
            SetOrRemove("FFlagDebugGraphicsPreferD3D11FL10", null);
            SetOrRemove("FFlagDebugGraphicsPreferVulkan", null);
            SetOrRemove("FFlagDebugGraphicsPreferOpenGL", null);
            SetOrRemove("FFlagDebugGraphicsDisableDirect3D11", null);

            string rMode = _settings.RenderingMode ?? "";
            if (rMode.Contains("D3D11") || rMode.Contains("Direct3D 11"))
                SetOrRemove("FFlagDebugGraphicsPreferD3D11", "True");
            else if (rMode.Contains("D3D10") || rMode.Contains("Direct3D 10"))
                SetOrRemove("FFlagDebugGraphicsPreferD3D11FL10", "True");
            else if (rMode.Contains("Vulkan"))
            {
                SetOrRemove("FFlagDebugGraphicsPreferVulkan", "True");
                SetOrRemove("FFlagDebugGraphicsDisableDirect3D11", "True");
            }
            else if (rMode.Contains("OpenGL"))
            {
                SetOrRemove("FFlagDebugGraphicsPreferOpenGL", "True");
                SetOrRemove("FFlagDebugGraphicsDisableDirect3D11", "True");
            }

            // Lighting Mode
            SetOrRemove("DFFlagDebugRenderForceTechnologyVoxel", null);
            SetOrRemove("FFlagDebugForceFutureIsBrightPhase2", null);
            SetOrRemove("FFlagDebugForceFutureIsBrightPhase3", null);

            string lMode = _settings.LightingMode ?? "";
            if (lMode.Contains("Voxel"))
                SetOrRemove("DFFlagDebugRenderForceTechnologyVoxel", "True");
            else if (lMode.Contains("Shadow"))
                SetOrRemove("FFlagDebugForceFutureIsBrightPhase2", "True");
            else if (lMode.Contains("Future"))
                SetOrRemove("FFlagDebugForceFutureIsBrightPhase3", "True");

            // PostFX & Telemetry
            SetOrRemove("FFlagDisablePostFx", _settings.DisablePostFx ? "True" : null);
            string tel = _settings.DisableTelemetry ? "True" : null;
            SetOrRemove("FFlagDebugDisableTelemetryEphemeralCounter", tel);
            SetOrRemove("FFlagDebugDisableTelemetryEphemeralStat", tel);
            SetOrRemove("FFlagDebugDisableTelemetryEventIngest", tel);
            SetOrRemove("FFlagDebugDisableTelemetryPoint", tel);
            SetOrRemove("FFlagDebugDisableTelemetryV2Counter", tel);
        }

        public void SetOrRemove(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
                Flags.Remove(key);
            else
                Flags[key] = value;
        }

        public bool SaveAndSync()
        {
            JsonHelper.SaveStringDict(NRBloxPaths.LocalClientAppSettingsFile, Flags);

            string versionDir = NRBloxPaths.GetRobloxVersionDir();
            if (!Directory.Exists(versionDir))
                return false;

            string clientSettingsDir = Path.Combine(versionDir, "ClientSettings");
            string targetFile = Path.Combine(clientSettingsDir, "ClientAppSettings.json");
            try
            {
                if (!Directory.Exists(clientSettingsDir))
                    Directory.CreateDirectory(clientSettingsDir);

                if (!_settings.UseFastFlagManager)
                {
                    if (File.Exists(targetFile))
                        File.Delete(targetFile);
                    return true;
                }

                JsonHelper.SaveStringDict(targetFile, Flags);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public sealed class ModEngine
    {
        private readonly NRBloxSettings _settings;

        public ModEngine(NRBloxSettings settings)
        {
            _settings = settings;
        }

        public int ApplyAndSyncMods()
        {
            NRBloxPaths.EnsureDirectories();
            string modDir = NRBloxPaths.ModificationsDir;

            // 1. Cursors ("Default", "From 2006 (Cartoon)", "From 2013 (Angular)")
            string cursorTargetDir = Path.Combine(modDir, "content", "textures", "Cursors", "KeyboardMouse");
            string arrowDst = Path.Combine(cursorTargetDir, "ArrowCursor.png");
            string arrowFarDst = Path.Combine(cursorTargetDir, "ArrowFarCursor.png");

            string folderKey = null;
            if ((_settings.CursorType ?? "").Contains("2006")) folderKey = "From2006";
            else if ((_settings.CursorType ?? "").Contains("2013")) folderKey = "From2013";

            if (folderKey != null)
            {
                string srcFolder = Path.Combine(NRBloxPaths.ModsAssetsDir, "cursors", folderKey);
                CopyIfExists(Path.Combine(srcFolder, "ArrowCursor.png"), arrowDst);
                CopyIfExists(Path.Combine(srcFolder, "ArrowFarCursor.png"), arrowFarDst);
            }
            else
            {
                DeleteIfExists(arrowDst);
                DeleteIfExists(arrowFarDst);
            }

            // 2. Classic Character Sounds
            string soundsTargetDir = Path.Combine(modDir, "content", "sounds");
            string[,] soundMap = new string[,]
            {
                { "OldWalk.mp3", "action_footsteps_plastic.mp3" },
                { "OldJump.mp3", "action_jump.mp3" },
                { "OldGetUp.mp3", "action_get_up.mp3" },
                { "Empty.mp3", "action_falling.mp3" },
                { "Empty.mp3", "action_jump_land.mp3" },
                { "Empty.mp3", "action_swim.mp3" },
                { "Empty.mp3", "impact_water.mp3" }
            };

            for (int i = 0; i < soundMap.GetLength(0); i++)
            {
                string src = Path.Combine(NRBloxPaths.ModsAssetsDir, "sounds", soundMap[i, 0]);
                string dst = Path.Combine(soundsTargetDir, soundMap[i, 1]);
                if (_settings.UseOldCharacterSounds && File.Exists(src))
                    CopyIfExists(src, dst);
                else
                    DeleteIfExists(dst);
            }

            // 3. Old Avatar Background
            string mobileRbxlDst = Path.Combine(modDir, "ExtraContent", "places", "Mobile.rbxl");
            string avatarBgSrc = Path.Combine(NRBloxPaths.ModsAssetsDir, "places", "OldAvatarBackground.rbxl");
            if (_settings.UseOldAvatarBackground && File.Exists(avatarBgSrc))
                CopyIfExists(avatarBgSrc, mobileRbxlDst);
            else
                DeleteIfExists(mobileRbxlDst);

            // 4. Custom Font
            string customFontDst = Path.Combine(modDir, "content", "fonts", "CustomFont.ttf");
            if (!string.IsNullOrEmpty(_settings.CustomFontPath) && File.Exists(_settings.CustomFontPath))
                CopyIfExists(_settings.CustomFontPath, customFontDst);
            else if (string.IsNullOrEmpty(_settings.CustomFontPath))
                DeleteIfExists(customFontDst);

            return SyncModificationsToRobloxVersion();
        }

        public int SyncModificationsToRobloxVersion()
        {
            string versionDir = NRBloxPaths.GetRobloxVersionDir();
            if (!Directory.Exists(versionDir))
                return 0;

            string modDir = NRBloxPaths.ModificationsDir;
            var oldManifest = new HashSet<string>(_settings.AppliedModManifest ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            var newManifest = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int count = 0;

            foreach (string file in Directory.GetFiles(modDir, "*", SearchOption.AllDirectories))
            {
                string rel = file.Substring(modDir.Length).TrimStart('\\', '/');
                newManifest.Add(rel);

                string targetPath = Path.Combine(versionDir, rel);
                string backupPath = targetPath + ".nrblox_orig";
                try
                {
                    string parent = Path.GetDirectoryName(targetPath);
                    if (!Directory.Exists(parent))
                        Directory.CreateDirectory(parent);

                    if (File.Exists(targetPath) && !File.Exists(backupPath) && !rel.StartsWith("ClientSettings", StringComparison.OrdinalIgnoreCase))
                        File.Copy(targetPath, backupPath, false);

                    File.Copy(file, targetPath, true);
                    count++;
                }
                catch { }
            }

            foreach (string oldRel in oldManifest)
            {
                if (!newManifest.Contains(oldRel))
                {
                    string targetPath = Path.Combine(versionDir, oldRel);
                    string backupPath = targetPath + ".nrblox_orig";
                    try
                    {
                        if (File.Exists(backupPath))
                        {
                            File.Copy(backupPath, targetPath, true);
                            File.Delete(backupPath);
                        }
                    }
                    catch { }
                }
            }

            _settings.AppliedModManifest = new List<string>(newManifest);
            JsonHelper.Save(NRBloxPaths.SettingsFile, _settings);
            return count;
        }

        private static void CopyIfExists(string src, string dst)
        {
            try
            {
                if (!File.Exists(src)) return;
                string dir = Path.GetDirectoryName(dst);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.Copy(src, dst, true);
            }
            catch { }
        }

        private static void DeleteIfExists(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch { }
        }
    }
}
