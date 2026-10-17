// ============================================================================
// NRBloxPlayerLauncher.cs
// 100% Open-Source Replacement for RobloxPlayerLauncher.exe
// Targets: C:\Users\Faelzinhown\AppData\Local\Roblox\Versions\version-11f155f3450f4c8e
// Reconstructed from REA analysis of RobloxPlayerBeta.exe & WebView2Loader.dll
// License: MIT
// ============================================================================

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

[assembly: AssemblyTitle("NRBloxPlayerLauncher")]
[assembly: AssemblyDescription("Open-Source Launcher & ClientSettings Injector for version-11f155f3450f4c8e")]
[assembly: AssemblyProduct("NRBlox")]
[assembly: AssemblyVersion("1.0.0.0")]

namespace NRBlox.Version11f155f3450f4c8e
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            string defaultVersionDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Roblox",
                "Versions",
                "version-11f155f3450f4c8e"
            );

            string targetVersionDir = Environment.GetEnvironmentVariable("NRBLOX_ROBLOX_VERSION_DIR");
            if (string.IsNullOrEmpty(targetVersionDir) || !Directory.Exists(targetVersionDir))
            {
                targetVersionDir = defaultVersionDir;
            }

            if (!Directory.Exists(targetVersionDir))
            {
                Console.Error.WriteLine("[NRBloxPlayerLauncher] Target version directory not found: " + targetVersionDir);
                return 1;
            }

            // 1. Ensure AppSettings.xml exists (required by RobloxPlayerBeta.dll on startup)
            string appSettingsXml = Path.Combine(targetVersionDir, "AppSettings.xml");
            if (!File.Exists(appSettingsXml))
            {
                string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" +
                             "<Settings>\r\n" +
                             "\t<ContentFolder>content</ContentFolder>\r\n" +
                             "\t<BaseUrl>http://www.roblox.com</BaseUrl>\r\n" +
                             "</Settings>\r\n";
                File.WriteAllText(appSettingsXml, xml);
            }

            // 2. Ensure ClientSettings\ClientAppSettings.json exists
            string clientSettingsDir = Path.Combine(targetVersionDir, "ClientSettings");
            if (!Directory.Exists(clientSettingsDir))
            {
                Directory.CreateDirectory(clientSettingsDir);
            }

            string clientAppSettings = Path.Combine(clientSettingsDir, "ClientAppSettings.json");
            if (!File.Exists(clientAppSettings))
            {
                File.WriteAllText(clientAppSettings, "{\r\n  \"DFIntTaskSchedulerTargetFps\": \"240\"\r\n}\r\n");
            }

            // 3. Check if --verify-only was passed
            foreach (string a in args)
            {
                if (string.Equals(a, "--verify-only", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("[NRBloxPlayerLauncher] Verified version-11f155f3450f4c8e at: " + targetVersionDir);
                    Console.WriteLine("[NRBloxPlayerLauncher] AppSettings.xml: OK");
                    Console.WriteLine("[NRBloxPlayerLauncher] ClientSettings\\ClientAppSettings.json: OK");
                    return 0;
                }
            }

            // 4. Launch RobloxPlayerBeta.exe with passed protocol/CLI arguments
            string playerBetaExe = Path.Combine(targetVersionDir, "RobloxPlayerBeta.exe");
            if (!File.Exists(playerBetaExe))
            {
                Console.Error.WriteLine("[NRBloxPlayerLauncher] RobloxPlayerBeta.exe not found in: " + targetVersionDir);
                return 2;
            }

            string joinedArgs = args.Length > 0 ? string.Join(" ", args) : "--app";
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = playerBetaExe,
                Arguments = joinedArgs,
                WorkingDirectory = targetVersionDir,
                UseShellExecute = false
            };

            Process proc = Process.Start(psi);
            Console.WriteLine("[NRBloxPlayerLauncher] Launched RobloxPlayerBeta.exe (PID: " + (proc != null ? proc.Id.ToString() : "unknown") + ")");
            return 0;
        }
    }
}
