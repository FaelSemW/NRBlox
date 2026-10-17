// ============================================================================
// NRBlox (Windows Native Edition) — Runtime Services, ActivityWatcher & RPC
// Author: FaelSemW (https://github.com/FaelSemW/NRBlox)
// Community Discord: https://discord.gg/M8j8JnQSCx
// License: MIT (100% Open Source)
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace NRBlox.Windows.Core
{
    public sealed class ServerSessionInfo
    {
        public string PlaceId { get; set; }
        public string JobId { get; set; }
        public string ServerIp { get; set; }
        public string ServerPort { get; set; }
        public string Location { get; set; }
        public string JoinedTime { get; set; }
        public bool InGame { get; set; }

        public ServerSessionInfo()
        {
            PlaceId = "—";
            JobId = "—";
            ServerIp = "—";
            ServerPort = "—";
            Location = "Aguardando conexão...";
            JoinedTime = "—";
            InGame = false;
        }
    }

    public sealed class NativeDiscordRpcClient : IDisposable
    {
        private const string DefaultClientId = "1005469189907173486";
        private NamedPipeClientStream _pipe;
        public bool IsConnected { get; private set; }

        public bool Connect()
        {
            if (IsConnected && _pipe != null && _pipe.IsConnected)
                return true;

            for (int i = 0; i <= 9; i++)
            {
                try
                {
                    var pipe = new NamedPipeClientStream(".", "discord-ipc-" + i, PipeDirection.InOut, PipeOptions.Asynchronous);
                    pipe.Connect(250);
                    _pipe = pipe;
                    SendFrame(0, "{\"v\":1,\"client_id\":\"" + DefaultClientId + "\"}");
                    IsConnected = true;
                    return true;
                }
                catch { }
            }
            IsConnected = false;
            return false;
        }

        public void UpdatePresence(string details, string state, string placeId, string jobId)
        {
            if (!Connect()) return;
            try
            {
                long startEpoch = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                string safeDetails = Escape(details);
                string safeState = Escape(state);
                string payload = "{" +
                    "\"cmd\":\"SET_ACTIVITY\"," +
                    "\"args\":{" +
                        "\"pid\":" + Process.GetCurrentProcess().Id + "," +
                        "\"activity\":{" +
                            "\"details\":\"" + safeDetails + "\"," +
                            "\"state\":\"" + safeState + "\"," +
                            "\"timestamps\":{\"start\":" + startEpoch + "}," +
                            "\"assets\":{\"large_image\":\"roblox\",\"large_text\":\"NRBlox Windows Engine\"}" +
                        "}" +
                    "}," +
                    "\"nonce\":\"" + Guid.NewGuid().ToString("N") + "\"" +
                "}";
                SendFrame(1, payload);
            }
            catch
            {
                IsConnected = false;
            }
        }

        public void ClearPresence()
        {
            if (!IsConnected || _pipe == null) return;
            try
            {
                string payload = "{\"cmd\":\"SET_ACTIVITY\",\"args\":{\"pid\":" + Process.GetCurrentProcess().Id + ",\"activity\":null},\"nonce\":\"" + Guid.NewGuid().ToString("N") + "\"}";
                SendFrame(1, payload);
            }
            catch { }
        }

        private void SendFrame(int opcode, string json)
        {
            if (_pipe == null || !_pipe.IsConnected) return;
            byte[] data = Encoding.UTF8.GetBytes(json);
            byte[] header = new byte[8];
            BitConverter.GetBytes(opcode).CopyTo(header, 0);
            BitConverter.GetBytes(data.Length).CopyTo(header, 4);
            _pipe.Write(header, 0, 8);
            _pipe.Write(data, 0, data.Length);
            _pipe.Flush();
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        public void Dispose()
        {
            try
            {
                if (_pipe != null) _pipe.Dispose();
            }
            catch { }
            IsConnected = false;
        }
    }

    public sealed class ActivityWatcherService
    {
        private readonly NRBloxSettings _settings;
        private readonly NativeDiscordRpcClient _discord;
        private Timer _pollTimer;
        private string _currentLogFile;
        private long _lastOffset;

        public ServerSessionInfo CurrentSession { get; private set; }
        public List<ServerSessionInfo> History { get; private set; }
        public event Action<ServerSessionInfo> OnSessionUpdated;

        private static readonly Regex JoinGameRegex = new Regex(@"! Joining game '([0-9a-f\-]{36})' place ([0-9]+) at ([0-9\.]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex UdmuxRegex = new Regex(@"UDMUX Address = ([0-9\.]+), Port = ([0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DisconnectRegex = new Regex(@"Disconnected from game|Time to disconnect|Client:Disconnect", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public ActivityWatcherService(NRBloxSettings settings, NativeDiscordRpcClient discord)
        {
            _settings = settings;
            _discord = discord;
            CurrentSession = new ServerSessionInfo();
            History = JsonHelper.LoadOrCreate<List<ServerSessionInfo>>(NRBloxPaths.ServerHistoryFile);
        }

        public void Start()
        {
            if (_pollTimer != null) return;
            _pollTimer = new Timer(PollLogsCallback, null, 1000, 2000);
        }

        public void Stop()
        {
            if (_pollTimer != null)
            {
                _pollTimer.Dispose();
                _pollTimer = null;
            }
        }

        private void PollLogsCallback(object state)
        {
            if (!_settings.EnableActivityTracking) return;
            try
            {
                string logsDir = NRBloxPaths.GetRobloxLogsDir();
                if (!Directory.Exists(logsDir)) return;

                string newestFile = null;
                DateTime newestTime = DateTime.MinValue;

                foreach (string f in Directory.GetFiles(logsDir, "*Player*.log"))
                {
                    DateTime wt = File.GetLastWriteTimeUtc(f);
                    if (wt > newestTime)
                    {
                        newestTime = wt;
                        newestFile = f;
                    }
                }

                if (string.IsNullOrEmpty(newestFile)) return;

                if (newestFile != _currentLogFile)
                {
                    _currentLogFile = newestFile;
                    _lastOffset = 0;
                }

                using (var fs = new FileStream(_currentLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    if (fs.Length <= _lastOffset) return;
                    fs.Seek(_lastOffset, SeekOrigin.Begin);
                    using (var sr = new StreamReader(fs, Encoding.UTF8))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            ProcessLogLine(line);
                        }
                        _lastOffset = fs.Position;
                    }
                }
            }
            catch { }
        }

        private void ProcessLogLine(string line)
        {
            Match mJoin = JoinGameRegex.Match(line);
            if (mJoin.Success)
            {
                CurrentSession.JobId = mJoin.Groups[1].Value;
                CurrentSession.PlaceId = mJoin.Groups[2].Value;
                CurrentSession.ServerIp = mJoin.Groups[3].Value;
                CurrentSession.JoinedTime = DateTime.Now.ToString("HH:mm:ss");
                CurrentSession.InGame = true;
                ResolveIpLocationAsync(CurrentSession.ServerIp);
                RecordHistory(CurrentSession);
                NotifyUpdate();
                return;
            }

            Match mUdmux = UdmuxRegex.Match(line);
            if (mUdmux.Success)
            {
                CurrentSession.ServerIp = mUdmux.Groups[1].Value;
                CurrentSession.ServerPort = mUdmux.Groups[2].Value;
                CurrentSession.InGame = true;
                ResolveIpLocationAsync(CurrentSession.ServerIp);
                NotifyUpdate();
                return;
            }

            if (DisconnectRegex.IsMatch(line) && CurrentSession.InGame)
            {
                CurrentSession.InGame = false;
                CurrentSession.Location = "Sessão encerrada";
                if (_settings.EnableDiscordRichPresence)
                    _discord.ClearPresence();
                NotifyUpdate();
            }
        }

        private void ResolveIpLocationAsync(string ip)
        {
            if (string.IsNullOrEmpty(ip) || ip == "—" || ip.StartsWith("10.") || ip.StartsWith("127."))
                return;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    using (var wc = new WebClient())
                    {
                        wc.Encoding = Encoding.UTF8;
                        wc.Headers.Add("User-Agent", "NRBlox/3.0");
                        string json = wc.DownloadString("https://ipinfo.io/" + ip + "/json");
                        var ser = new JavaScriptSerializer();
                        var dict = ser.Deserialize<Dictionary<string, object>>(json);
                        if (dict != null)
                        {
                            string city = dict.ContainsKey("city") ? Convert.ToString(dict["city"]) : "";
                            string region = dict.ContainsKey("region") ? Convert.ToString(dict["region"]) : "";
                            string country = dict.ContainsKey("country") ? Convert.ToString(dict["country"]) : "";
                            CurrentSession.Location = string.Format("{0}, {1} ({2})", city, region, country).Trim(' ', ',');
                            NotifyUpdate();
                        }
                    }
                }
                catch { }
            });
        }

        private void RecordHistory(ServerSessionInfo info)
        {
            try
            {
                History.RemoveAll(x => x.JobId == info.JobId && x.PlaceId == info.PlaceId);
                History.Insert(0, new ServerSessionInfo
                {
                    PlaceId = info.PlaceId,
                    JobId = info.JobId,
                    ServerIp = info.ServerIp,
                    ServerPort = info.ServerPort,
                    Location = info.Location,
                    JoinedTime = info.JoinedTime,
                    InGame = true
                });
                if (History.Count > 25)
                    History.RemoveRange(25, History.Count - 25);
                JsonHelper.Save(NRBloxPaths.ServerHistoryFile, History);
            }
            catch { }
        }

        private void NotifyUpdate()
        {
            if (_settings.EnableDiscordRichPresence && CurrentSession.InGame)
            {
                string details = "Jogando Roblox (Place " + CurrentSession.PlaceId + ")";
                string state = _settings.ShowServerDetails ? ("Servidor: " + CurrentSession.Location) : "No jogo via NRBlox";
                _discord.UpdatePresence(details, state, CurrentSession.PlaceId, CurrentSession.JobId);
            }
            var handler = OnSessionUpdated;
            if (handler != null) handler(CurrentSession);
        }
    }

    public static class SystemIntegrationService
    {
        private static Mutex _multiInstanceMutex;

        public static bool SetMultiInstanceMutex(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    if (_multiInstanceMutex == null)
                    {
                        bool createdNew;
                        _multiInstanceMutex = new Mutex(true, "ROBLOX_singletonMutex", out createdNew);
                    }
                    return true;
                }
                else
                {
                    if (_multiInstanceMutex != null)
                    {
                        try { _multiInstanceMutex.ReleaseMutex(); } catch { }
                        _multiInstanceMutex.Dispose();
                        _multiInstanceMutex = null;
                    }
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void RegisterWindowsProtocols()
        {
            string exePath = Path.Combine(NRBloxPaths.BaseDir, "NRBlox.exe");
            string[] schemes = new string[] { "roblox-player", "roblox", "nrblox" };
            foreach (string scheme in schemes)
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + scheme))
                {
                    if (key == null) continue;
                    key.SetValue("", "URL:NRBlox Protocol (" + scheme + ")");
                    key.SetValue("URL Protocol", "");
                    using (RegistryKey iconKey = key.CreateSubKey("DefaultIcon"))
                    {
                        iconKey.SetValue("", "\"" + exePath + "\",0");
                    }
                    using (RegistryKey cmdKey = key.CreateSubKey(@"shell\open\command"))
                    {
                        cmdKey.SetValue("", "\"" + exePath + "\" -player \"%1\"");
                    }
                }
            }
        }

        public static bool CreateDesktopShortcuts()
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string exePath = Path.Combine(NRBloxPaths.BaseDir, "NRBlox.exe");
                string iconPath = Path.Combine(NRBloxPaths.IconsDir, "IconNRBlox.ico");

                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return false;
                dynamic shell = Activator.CreateInstance(shellType);

                dynamic s1 = shell.CreateShortcut(Path.Combine(desktop, "NRBlox.lnk"));
                s1.TargetPath = exePath;
                s1.WorkingDirectory = NRBloxPaths.BaseDir;
                if (File.Exists(iconPath)) s1.IconLocation = iconPath;
                s1.Description = "NRBlox — Windows Engine & FastFlag Controller";
                s1.Save();

                dynamic s2 = shell.CreateShortcut(Path.Combine(desktop, "Jogar Roblox (NRBlox).lnk"));
                s2.TargetPath = exePath;
                s2.Arguments = "-player";
                s2.WorkingDirectory = NRBloxPaths.BaseDir;
                if (File.Exists(iconPath)) s2.IconLocation = iconPath;
                s2.Description = "Iniciar Roblox diretamente via NRBlox";
                s2.Save();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string VerifyClientInstallation()
        {
            string versionDir = NRBloxPaths.GetRobloxVersionDir();
            var sb = new StringBuilder();
            sb.AppendLine("=== Verificação de Integridade NRBlox ===");
            sb.AppendLine("Diretório Alvo: " + versionDir);

            if (!Directory.Exists(versionDir))
            {
                sb.AppendLine("Status: Diretório da versão não encontrado.");
                return sb.ToString();
            }

            // Ensure AppSettings.xml exists
            string appSettings = Path.Combine(versionDir, "AppSettings.xml");
            if (!File.Exists(appSettings))
            {
                File.WriteAllText(appSettings,
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<Settings>\r\n\t<ContentFolder>content</ContentFolder>\r\n\t<BaseUrl>http://www.roblox.com</BaseUrl>\r\n</Settings>\r\n",
                    Encoding.UTF8);
            }

            string[] keyFiles = new string[]
            {
                "RobloxPlayerBeta.exe",
                "RobloxPlayerBeta.dll",
                "WebView2Loader.dll",
                "AppSettings.xml",
                @"ClientSettings\ClientAppSettings.json"
            };

            foreach (string rel in keyFiles)
            {
                string full = Path.Combine(versionDir, rel);
                if (File.Exists(full))
                {
                    var fi = new FileInfo(full);
                    sb.AppendLine(string.Format("[OK] {0} ({1:N0} bytes)", rel, fi.Length));
                }
                else
                {
                    sb.AppendLine(string.Format("[AUSENTE] {0}", rel));
                }
            }
            return sb.ToString();
        }
    }

    public sealed class RobloxPackageEntry
    {
        public string Name { get; set; }
        public string Md5 { get; set; }
        public long CompressedSize { get; set; }
        public long UncompressedSize { get; set; }
    }

    public static class RobloxPackageInstaller
    {
        private static readonly string[] CdnBaseUrls = new string[]
        {
            "https://setup.rbxcdn.com/channel/common/",
            "https://setup.rbxcdn.com/"
        };

        private static readonly Dictionary<string, string> PackageDirectoryMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "RobloxApp.zip", "" },
            { "RobloxStudio.zip", "" },
            { "redist.zip", "" },
            { "shaders.zip", "shaders" },
            { "ssl.zip", "ssl" },
            { "WebView2.zip", "" },
            { "WebView2RuntimeInstaller.zip", "WebView2RuntimeInstaller" },
            { "content-avatar.zip", @"content\avatar" },
            { "content-configs.zip", @"content\configs" },
            { "content-fonts.zip", @"content\fonts" },
            { "content-sky.zip", @"content\sky" },
            { "content-sounds.zip", @"content\sounds" },
            { "content-textures2.zip", @"content\textures" },
            { "content-models.zip", @"content\models" },
            { "content-platform-fonts.zip", @"PlatformContent\pc\fonts" },
            { "content-platform-dictionaries.zip", @"PlatformContent\pc\shared_compression_dictionaries" },
            { "content-terrain.zip", @"PlatformContent\pc\terrain" },
            { "content-textures3.zip", @"PlatformContent\pc\textures" },
            { "extracontent-luapackages.zip", @"ExtraContent\LuaPackages" },
            { "extracontent-translations.zip", @"ExtraContent\translations" },
            { "extracontent-models.zip", @"ExtraContent\models" },
            { "extracontent-textures.zip", @"ExtraContent\textures" },
            { "extracontent-places.zip", @"ExtraContent\places" },
            // Studio packages
            { "RibbonConfig.zip", "RibbonConfig" },
            { "ApplicationConfig.zip", "ApplicationConfig" },
            { "BuiltInPlugins.zip", "BuiltInPlugins" },
            { "BuiltInStandalonePlugins.zip", "BuiltInStandalonePlugins" },
            { "Plugins.zip", "Plugins" },
            { "Qml.zip", "Qml" },
            { "StudioFonts.zip", "StudioFonts" },
            { "studio-svg-textures.zip", @"content\studio_svg_textures" },
            { "content-qt_translations.zip", @"content\qt_translations" },
            { "content-api-docs.zip", @"content\api_docs" },
            { "extracontent-scripts.zip", @"ExtraContent\scripts" }
        };

        public static string ResolveTargetVersionGuid(bool isStudio, bool lockVersion)
        {
            if (lockVersion && !string.IsNullOrEmpty(NRBloxPaths.TargetVersionGuid))
            {
                string lockedDir = Path.Combine(NRBloxPaths.SystemRobloxRoot, "Versions", NRBloxPaths.TargetVersionGuid);
                string exeName = isStudio ? "RobloxStudioBeta.exe" : "RobloxPlayerBeta.exe";
                if (File.Exists(Path.Combine(lockedDir, exeName)))
                    return NRBloxPaths.TargetVersionGuid;
            }

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string binaryType = isStudio ? "WindowsStudio64" : "WindowsPlayer";
                string url = "https://clientsettingscdn.roblox.com/v2/client-version/" + binaryType;
                using (var wc = new WebClient())
                {
                    wc.Encoding = Encoding.UTF8;
                    wc.Headers.Add("User-Agent", "Roblox/WinInet");
                    string json = wc.DownloadString(url);
                    var ser = new JavaScriptSerializer();
                    var dict = ser.Deserialize<Dictionary<string, object>>(json);
                    if (dict != null && dict.ContainsKey("clientVersionUpload"))
                    {
                        string guid = Convert.ToString(dict["clientVersionUpload"]);
                        if (!string.IsNullOrEmpty(guid) && guid.StartsWith("version-"))
                        {
                            if (!isStudio)
                                NRBloxPaths.TargetVersionGuid = guid;
                            return guid;
                        }
                    }
                }
            }
            catch { }

            return NRBloxPaths.TargetVersionGuid;
        }

        public static bool IsClientInstalled(string versionDir, bool isStudio)
        {
            if (string.IsNullOrEmpty(versionDir) || !Directory.Exists(versionDir))
                return false;
            string exeName = isStudio ? "RobloxStudioBeta.exe" : "RobloxPlayerBeta.exe";
            return File.Exists(Path.Combine(versionDir, exeName));
        }

        public static void InstallOrUpdateClient(
            string versionGuid,
            string versionDir,
            Action<double, string, string> onProgress,
            Func<bool> isCancelled)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.DefaultConnectionLimit = 16;

            if (onProgress != null)
                onProgress(0.04, "Connecting to Roblox CDN...", "Fetching package manifest for " + versionGuid);

            string manifestText = null;
            string activeBaseUrl = CdnBaseUrls[0];

            foreach (string baseUrl in CdnBaseUrls)
            {
                try
                {
                    using (var wc = new WebClient())
                    {
                        wc.Encoding = Encoding.UTF8;
                        wc.Headers.Add("User-Agent", "Roblox/WinInet");
                        manifestText = wc.DownloadString(baseUrl + versionGuid + "-rbxPkgManifest.txt");
                        if (!string.IsNullOrEmpty(manifestText) && manifestText.StartsWith("v0"))
                        {
                            activeBaseUrl = baseUrl;
                            break;
                        }
                    }
                }
                catch { }
            }

            if (string.IsNullOrEmpty(manifestText))
                throw new InvalidOperationException("Failed to download package manifest for " + versionGuid + " from setup.rbxcdn.com.");

            List<RobloxPackageEntry> packages = ParseManifest(manifestText);
            if (packages.Count == 0)
                throw new InvalidOperationException("No valid .zip packages found in manifest for " + versionGuid + ".");

            string cacheDir = NRBloxPaths.DownloadsCacheDir;
            if (!Directory.Exists(cacheDir))
                Directory.CreateDirectory(cacheDir);
            if (!Directory.Exists(versionDir))
                Directory.CreateDirectory(versionDir);

            long totalCompressedBytes = 0;
            foreach (var p in packages)
                totalCompressedBytes += Math.Max(1, p.CompressedSize);

            long downloadedBytesSoFar = 0;
            double totalMb = totalCompressedBytes / (1024.0 * 1024.0);

            // Step 1: Download all .zip packages (progress 0.05 -> 0.78)
            for (int i = 0; i < packages.Count; i++)
            {
                if (isCancelled != null && isCancelled())
                    return;

                RobloxPackageEntry pkg = packages[i];
                string cachedFile = Path.Combine(cacheDir, pkg.Md5);

                bool cacheValid = false;
                if (File.Exists(cachedFile))
                {
                    try
                    {
                        var fi = new FileInfo(cachedFile);
                        if (fi.Length == pkg.CompressedSize && VerifyMd5(cachedFile, pkg.Md5))
                            cacheValid = true;
                    }
                    catch { }
                }

                if (cacheValid)
                {
                    downloadedBytesSoFar += pkg.CompressedSize;
                    double ratio = 0.05 + 0.73 * ((double)downloadedBytesSoFar / totalCompressedBytes);
                    double curMb = downloadedBytesSoFar / (1024.0 * 1024.0);
                    if (onProgress != null)
                        onProgress(ratio, "Downloading Roblox (" + (i + 1) + "/" + packages.Count + ")...",
                            string.Format("Verified cached {0} ({1:F1} MB / {2:F1} MB)", pkg.Name, curMb, totalMb));
                }
                else
                {
                    string pkgUrl = activeBaseUrl + versionGuid + "-" + pkg.Name;
                    string tempFile = cachedFile + ".tmp";
                    if (File.Exists(tempFile))
                    {
                        try { File.Delete(tempFile); } catch { }
                    }

                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(pkgUrl);
                    req.UserAgent = "Roblox/WinInet";
                    req.Timeout = 30000;
                    req.ReadWriteTimeout = 30000;

                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    using (Stream netStream = resp.GetResponseStream())
                    using (FileStream fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 65536))
                    {
                        byte[] buffer = new byte[65536];
                        int read;
                        long pkgRead = 0;
                        DateTime lastUiUpdate = DateTime.MinValue;

                        while ((read = netStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            if (isCancelled != null && isCancelled())
                                return;

                            fs.Write(buffer, 0, read);
                            pkgRead += read;
                            long currentTotal = downloadedBytesSoFar + pkgRead;

                            if ((DateTime.UtcNow - lastUiUpdate).TotalMilliseconds >= 60)
                            {
                                lastUiUpdate = DateTime.UtcNow;
                                double ratio = 0.05 + 0.73 * Math.Min(1.0, (double)currentTotal / totalCompressedBytes);
                                double curMb = currentTotal / (1024.0 * 1024.0);
                                if (onProgress != null)
                                    onProgress(ratio, "Downloading Roblox (" + (i + 1) + "/" + packages.Count + ")...",
                                        string.Format("{0} — {1:F1} MB / {2:F1} MB", pkg.Name, curMb, totalMb));
                            }
                        }
                    }

                    if (File.Exists(cachedFile))
                        File.Delete(cachedFile);
                    File.Move(tempFile, cachedFile);
                    downloadedBytesSoFar += pkg.CompressedSize;
                }
            }

            // Step 2: Extract all .zip packages into their mapped folders inside versionDir (progress 0.78 -> 0.96)
            for (int i = 0; i < packages.Count; i++)
            {
                if (isCancelled != null && isCancelled())
                    return;

                RobloxPackageEntry pkg = packages[i];
                string cachedFile = Path.Combine(cacheDir, pkg.Md5);
                if (!File.Exists(cachedFile))
                    continue;

                string subFolder = "";
                if (PackageDirectoryMap.ContainsKey(pkg.Name))
                    subFolder = PackageDirectoryMap[pkg.Name];

                string targetExtractDir = string.IsNullOrEmpty(subFolder)
                    ? versionDir
                    : Path.Combine(versionDir, subFolder);

                if (!Directory.Exists(targetExtractDir))
                    Directory.CreateDirectory(targetExtractDir);

                double ratio = 0.78 + 0.18 * ((double)(i + 1) / packages.Count);
                if (onProgress != null)
                    onProgress(ratio, "Extracting Roblox packages (" + (i + 1) + "/" + packages.Count + ")...",
                        "Extracting " + pkg.Name + "...");

                ExtractZipArchiveSafe(cachedFile, targetExtractDir);
            }

            // Step 3: Write AppSettings.xml & Register URI protocols
            string appSettingsPath = Path.Combine(versionDir, "AppSettings.xml");
            File.WriteAllText(appSettingsPath,
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<Settings>\r\n\t<ContentFolder>content</ContentFolder>\r\n\t<BaseUrl>http://www.roblox.com</BaseUrl>\r\n</Settings>\r\n",
                Encoding.UTF8);

            try
            {
                SystemIntegrationService.RegisterWindowsProtocols();
            }
            catch { }
        }

        private static List<RobloxPackageEntry> ParseManifest(string manifestText)
        {
            var list = new List<RobloxPackageEntry>();
            string[] lines = manifestText.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 5)
                return list;

            int idx = 1; // skip "v0"
            while (idx + 3 < lines.Length)
            {
                string name = lines[idx].Trim();
                string md5 = lines[idx + 1].Trim();
                long compSize = 0;
                long uncompSize = 0;
                long.TryParse(lines[idx + 2].Trim(), out compSize);
                long.TryParse(lines[idx + 3].Trim(), out uncompSize);
                idx += 4;

                if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(new RobloxPackageEntry
                    {
                        Name = name,
                        Md5 = md5,
                        CompressedSize = compSize,
                        UncompressedSize = uncompSize
                    });
                }
            }
            return list;
        }

        private static void ExtractZipArchiveSafe(string zipFilePath, string targetFolder)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name) && entry.FullName.EndsWith("/"))
                        continue;

                    string relativePath = entry.FullName.Replace('/', '\\').TrimStart('\\');
                    if (relativePath.Contains(".."))
                        continue;

                    string destFile = Path.Combine(targetFolder, relativePath);
                    string destDir = Path.GetDirectoryName(destFile);
                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir);

                    entry.ExtractToFile(destFile, true);
                }
            }
        }

        private static bool VerifyMd5(string filePath, string expectedMd5)
        {
            if (string.IsNullOrEmpty(expectedMd5)) return true;
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                var sb = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                    sb.Append(hash[i].ToString("x2"));
                return string.Equals(sb.ToString(), expectedMd5, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
