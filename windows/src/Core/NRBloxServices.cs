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
}
