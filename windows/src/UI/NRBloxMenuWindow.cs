// ============================================================================
// NRBlox (Windows Native Edition) — Launch Menu Window (Matches Screenshot 1)
// Author: FaelSemW (https://github.com/FaelSemW/NRBlox)
// Community Discord: https://discord.gg/M8j8JnQSCx
// License: MIT (100% Open Source)
// ============================================================================

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NRBlox.Windows.Core;

namespace NRBlox.Windows.UI
{
    public sealed class NRBloxMenuWindow : Window
    {
        private readonly NRBloxSettings _settings;
        private readonly FastFlagEngine _fastFlags;
        private readonly ModEngine _mods;

        public NRBloxMenuWindow()
        {
            NRBloxPaths.EnsureDirectories();
            _settings = JsonHelper.LoadOrCreate<NRBloxSettings>(NRBloxPaths.SettingsFile);
            _fastFlags = new FastFlagEngine(_settings);
            _mods = new ModEngine(_settings);

            Title = "NRBlox";
            Width = 620;
            Height = 365;
            ResizeMode = ResizeMode.CanMinimize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.SingleBorderWindow;
            AllowsTransparency = false;

            NRBloxTheme.ApplyTheme(Resources, _settings.ThemeMode);
            SetResourceReference(Window.BackgroundProperty, "BgWindowBrush");

            BuildMenuLayout();
        }

        private void BuildMenuLayout()
        {
            var mainGrid = new Grid { Margin = new Thickness(26, 22, 26, 22) };
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // =================================================================
            // LEFT COLUMN: Logo, Version, and Links (About, Wiki, Community)
            // =================================================================
            var leftPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 18, 0)
            };

            var brandRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 26)
            };

            var logo = NRBloxTheme.CreateNRBloxLogo(52);
            logo.Margin = new Thickness(0, 0, 16, 0);
            brandRow.Children.Add(logo);

            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            titleStack.Children.Add(NRBloxTheme.CreateText("NRBlox", 25, FontWeights.Normal, "TextPrimaryBrush"));
            titleStack.Children.Add(NRBloxTheme.CreateText("Version 3.0.0", 13, FontWeights.Normal, "TextSecondaryBrush", new Thickness(0, 2, 0, 0)));
            brandRow.Children.Add(titleStack);

            leftPanel.Children.Add(brandRow);

            // Links
            var linksStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            linksStack.Children.Add(CreateLinkRow("ⓘ", "About NRBlox", () => OpenSettingsWindow("about")));
            linksStack.Children.Add(CreateLinkRow("📖", "NRBlox Wiki", () => OpenUrl(NRBloxPaths.GitHubWikiUrl)));
            linksStack.Children.Add(CreateLinkRow("💬", "NRBlox Community", () => OpenUrl(NRBloxPaths.DiscordUrl)));
            leftPanel.Children.Add(linksStack);

            Grid.SetColumn(leftPanel, 0);
            mainGrid.Children.Add(leftPanel);

            // =================================================================
            // RIGHT COLUMN: 4 Action Cards with Right Chevron
            // =================================================================
            var rightGrid = new Grid();
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var launchRobloxCard = NRBloxTheme.CreateActionCard(
                "➔",
                "Launch Roblox",
                null,
                () => LaunchClient(false),
                new Thickness(0, 0, 0, 10)
            );
            Grid.SetRow(launchRobloxCard, 0);
            rightGrid.Children.Add(launchRobloxCard);

            var launchStudioCard = NRBloxTheme.CreateActionCard(
                "🔧",
                "Launch Roblox Studio",
                null,
                () => LaunchClient(true),
                new Thickness(0, 0, 0, 10)
            );
            Grid.SetRow(launchStudioCard, 1);
            rightGrid.Children.Add(launchStudioCard);

            var configSettingsCard = NRBloxTheme.CreateActionCard(
                "⚙️",
                "Configure settings",
                null,
                () => OpenSettingsWindow("integrations"),
                new Thickness(0, 0, 0, 10)
            );
            Grid.SetRow(configSettingsCard, 2);
            rightGrid.Children.Add(configSettingsCard);

            var issueWikiCard = NRBloxTheme.CreateActionCard(
                "📓",
                "Having an issue?",
                "See the Wiki for help",
                () => OpenUrl(NRBloxPaths.GitHubWikiUrl),
                new Thickness(0, 14, 0, 0)
            );
            Grid.SetRow(issueWikiCard, 4);
            rightGrid.Children.Add(issueWikiCard);

            Grid.SetColumn(rightGrid, 1);
            mainGrid.Children.Add(rightGrid);

            Content = mainGrid;
        }

        private FrameworkElement CreateLinkRow(string icon, string text, Action onClick)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 6, 0, 6),
                Cursor = Cursors.Hand
            };

            var iconTb = NRBloxTheme.CreateText(icon, 13.5, FontWeights.Normal, "AccentLinkBrush", new Thickness(0, 0, 8, 0));
            var labelTb = NRBloxTheme.CreateText(text, 13.5, FontWeights.Normal, "AccentLinkBrush");
            row.Children.Add(iconTb);
            row.Children.Add(labelTb);

            row.MouseEnter += (s, e) => labelTb.TextDecorations = TextDecorations.Underline;
            row.MouseLeave += (s, e) => labelTb.TextDecorations = null;
            row.MouseLeftButtonUp += (s, e) =>
            {
                if (onClick != null) onClick();
            };

            return row;
        }

        private void LaunchClient(bool isStudio)
        {
            Hide();
            var boot = new NRBloxBootstrapperWindow(_settings, _fastFlags, _mods, null, isStudio, false);
            boot.ShowDialog();
            Close();
        }

        private void OpenSettingsWindow(string initialTab)
        {
            Hide();
            var settingsWin = new NRBloxMainWindow(initialTab);
            settingsWin.ShowDialog();
            Close();
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch { }
        }
    }
}
