// ============================================================================
// NRBlox (Windows Native Edition) — Safe Application Entry Point
// Author: FaelSemW (https://github.com/FaelSemW/NRBlox)
// Community Discord: https://discord.gg/M8j8JnQSCx
// License: MIT (100% Open Source)
// ============================================================================

using System;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using NRBlox.Windows.Core;
using NRBlox.Windows.UI;

[assembly: AssemblyTitle("NRBlox")]
[assembly: AssemblyDescription("NRBlox — Native Windows Roblox Engine Controller & Bootstrapper")]
[assembly: AssemblyCompany("FaelSemW")]
[assembly: AssemblyProduct("NRBlox")]
[assembly: AssemblyCopyright("Copyright © FaelSemW - MIT License")]
[assembly: AssemblyVersion("3.0.0.0")]
[assembly: AssemblyFileVersion("3.0.0.0")]

namespace NRBlox.Windows
{
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            // Force safe CPU/GDI rendering to prevent any DirectX9/DWM GPU driver TDR or BSOD
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

            NRBloxPaths.EnsureDirectories();
            var settings = JsonHelper.LoadOrCreate<NRBloxSettings>(NRBloxPaths.SettingsFile);
            var fastFlags = new FastFlagEngine(settings);
            var mods = new ModEngine(settings);

            foreach (string arg in args)
            {
                if (string.Equals(arg, "--verify", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(SystemIntegrationService.VerifyClientInstallation());
                    return 0;
                }
                if (string.Equals(arg, "--install-player", StringComparison.OrdinalIgnoreCase))
                {
                    string guid = RobloxPackageInstaller.ResolveTargetVersionGuid(false, false);
                    string vDir = System.IO.Path.Combine(NRBloxPaths.SystemRobloxRoot, "Versions", guid);
                    Console.WriteLine("[NRBlox] Installing/Verifying Roblox Player " + guid + " to: " + vDir);
                    RobloxPackageInstaller.InstallOrUpdateClient(
                        guid,
                        vDir,
                        (ratio, status, sub) => Console.WriteLine(string.Format("[{0:P0}] {1} | {2}", ratio, status, sub)),
                        () => false
                    );
                    fastFlags.ApplySettingsToFlags();
                    fastFlags.SaveAndSync();
                    mods.ApplyAndSyncMods();
                    Console.WriteLine(SystemIntegrationService.VerifyClientInstallation());
                    return 0;
                }
                if (string.Equals(arg, "--register-protocols", StringComparison.OrdinalIgnoreCase))
                {
                    SystemIntegrationService.RegisterWindowsProtocols();
                    return 0;
                }
            }

            bool isPlayerLaunch = false;
            bool isStudioLaunch = false;
            bool isSettingsDirect = false;
            string launchUri = null;

            foreach (string arg in args)
            {
                if (string.Equals(arg, "-player", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "--player", StringComparison.OrdinalIgnoreCase))
                {
                    isPlayerLaunch = true;
                }
                else if (string.Equals(arg, "-studio", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(arg, "--studio", StringComparison.OrdinalIgnoreCase))
                {
                    isStudioLaunch = true;
                }
                else if (string.Equals(arg, "-settings", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(arg, "--settings", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(arg, "-preferences", StringComparison.OrdinalIgnoreCase))
                {
                    isSettingsDirect = true;
                }
                else if (arg.StartsWith("roblox-player:", StringComparison.OrdinalIgnoreCase) ||
                         arg.StartsWith("roblox:", StringComparison.OrdinalIgnoreCase) ||
                         arg.StartsWith("nrblox:", StringComparison.OrdinalIgnoreCase))
                {
                    isPlayerLaunch = true;
                    launchUri = arg;
                }
            }

            var app = new Application();

            if (isPlayerLaunch || isStudioLaunch)
            {
                var bootstrapperWin = new NRBloxBootstrapperWindow(settings, fastFlags, mods, launchUri, isStudioLaunch, false);
                app.Run(bootstrapperWin);
                return 0;
            }

            if (isSettingsDirect)
            {
                var settingsWin = new NRBloxMainWindow("integrations");
                app.Run(settingsWin);
                return 0;
            }

            // Default: Open the comfortable 2-column Launch Menu Window (Screenshot 1)
            var menuWin = new NRBloxMenuWindow();
            app.Run(menuWin);
            return 0;
        }
    }
}
