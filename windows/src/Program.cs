// ============================================================================
// NRBlox (Windows Native Edition) — Application Entry Point
// Author: FaelSemW (https://github.com/FaelSemW/NRBlox)
// Community Discord: https://discord.gg/M8j8JnQSCx
// License: MIT (100% Open Source)
// ============================================================================

using System;
using System.Reflection;
using System.Windows;
using NRBlox.Windows.Core;
using NRBlox.Windows.UI;

[assembly: AssemblyTitle("NRBlox")]
[assembly: AssemblyDescription("NRBlox — Native Windows Roblox Engine Controller & FastFlag Studio")]
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
            NRBloxPaths.EnsureDirectories();
            var settings = JsonHelper.LoadOrCreate<NRBloxSettings>(NRBloxPaths.SettingsFile);
            var fastFlags = new FastFlagEngine(settings);
            var mods = new ModEngine(settings);

            // CLI Mode: --verify
            foreach (string arg in args)
            {
                if (string.Equals(arg, "--verify", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(SystemIntegrationService.VerifyClientInstallation());
                    return 0;
                }
                if (string.Equals(arg, "--register-protocols", StringComparison.OrdinalIgnoreCase))
                {
                    SystemIntegrationService.RegisterWindowsProtocols();
                    return 0;
                }
            }

            // Check if launched as Bootstrapper (-player, -studio, or roblox-player:// URI)
            bool isPlayerLaunch = false;
            bool isStudioLaunch = false;
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
                var bootstrapperWin = new NRBloxBootstrapperWindow(settings, fastFlags, mods, launchUri, isStudioLaunch);
                app.Run(bootstrapperWin);
                return 0;
            }

            var mainWin = new NRBloxMainWindow();
            app.Run(mainWin);
            return 0;
        }
    }
}
