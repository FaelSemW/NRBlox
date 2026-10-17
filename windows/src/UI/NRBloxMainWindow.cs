// ============================================================================
// NRBlox (Windows Native Edition) — Settings Window (Matches Screenshots 2–13)
// Author: FaelSemW (https://github.com/FaelSemW/NRBlox)
// Community Discord: https://discord.gg/M8j8JnQSCx
// License: MIT (100% Open Source)
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using NRBlox.Windows.Core;

namespace NRBlox.Windows.UI
{
    public sealed class NRBloxMainWindow : Window
    {
        private readonly NRBloxSettings _settings;
        private readonly FastFlagEngine _fastFlags;
        private readonly ModEngine _mods;

        private readonly Dictionary<string, UIElement> _pages = new Dictionary<string, UIElement>();
        private readonly Dictionary<string, Border> _navItems = new Dictionary<string, Border>();
        private readonly Dictionary<string, Border> _navIndicators = new Dictionary<string, Border>();

        private ContentControl _pageHost;
        private ListBox _customIntListBox;
        private ContentControl _customIntDetailHost;
        private StackPanel _fastFlagRowsPanel;

        public NRBloxMainWindow(string initialTab = "integrations")
        {
            NRBloxPaths.EnsureDirectories();
            _settings = JsonHelper.LoadOrCreate<NRBloxSettings>(NRBloxPaths.SettingsFile);
            _fastFlags = new FastFlagEngine(_settings);
            _mods = new ModEngine(_settings);

            Title = "NRBlox Settings";
            Width = 980;
            Height = 620;
            MinWidth = 820;
            MinHeight = 520;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.SingleBorderWindow;
            AllowsTransparency = false;

            NRBloxTheme.ApplyTheme(Resources, _settings.ThemeMode);
            SetResourceReference(Window.BackgroundProperty, "BgWindowBrush");

            BuildWindowLayout();
            NavigateTo(string.IsNullOrEmpty(initialTab) ? "integrations" : initialTab);
        }

        private void BuildWindowLayout()
        {
            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // =================================================================
            // ROW 0: Main Body (Left Sidebar + Right Scrollable Page)
            // =================================================================
            var bodyGrid = new Grid();
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) });
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Left Sidebar
            var sidebarGrid = new Grid { Margin = new Thickness(12, 14, 12, 8) };
            sidebarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            sidebarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            sidebarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var topNavStack = new StackPanel();
            topNavStack.Children.Add(CreateSidebarNavItem("integrations", "＋", "Integrations"));
            topNavStack.Children.Add(CreateSidebarNavItem("bootstrapper", "▷", "Bootstrapper"));
            topNavStack.Children.Add(CreateSidebarNavItem("mods", "🛠", "Mods"));
            topNavStack.Children.Add(CreateSidebarNavItem("engine", "⚑", "Engine Settings"));
            topNavStack.Children.Add(CreateSidebarNavItem("appearance", "🖌", "Appearance"));
            topNavStack.Children.Add(CreateSidebarNavItem("shortcuts", "⊞", "Shortcuts"));
            Grid.SetRow(topNavStack, 0);
            sidebarGrid.Children.Add(topNavStack);

            var bottomNavStack = new StackPanel();
            bottomNavStack.Children.Add(CreateSidebarNavItem("nrblox", "⚙", "NRBlox"));
            bottomNavStack.Children.Add(CreateSidebarNavItem("about", "ⓘ", "About"));
            Grid.SetRow(bottomNavStack, 2);
            sidebarGrid.Children.Add(bottomNavStack);

            Grid.SetColumn(sidebarGrid, 0);
            bodyGrid.Children.Add(sidebarGrid);

            // Right Content ScrollViewer
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(18, 12, 24, 18)
            };
            _pageHost = new ContentControl();
            scrollViewer.Content = _pageHost;

            // Build all pages
            _pages["integrations"] = BuildIntegrationsPage();
            _pages["bootstrapper"] = BuildBootstrapperPage();
            _pages["mods"] = BuildModsPage();
            _pages["engine"] = BuildEngineSettingsPage();
            _pages["fflag_editor"] = BuildFastFlagEditorPage();
            _pages["appearance"] = BuildAppearancePage();
            _pages["shortcuts"] = BuildShortcutsPage();
            _pages["nrblox"] = BuildNRBloxBehaviourPage();
            _pages["about"] = BuildAboutPage();

            Grid.SetColumn(scrollViewer, 1);
            bodyGrid.Children.Add(scrollViewer);

            Grid.SetRow(bodyGrid, 0);
            rootGrid.Children.Add(bodyGrid);

            // =================================================================
            // ROW 1: Bottom Bar (Test mode toggle on left, Save & Close on right)
            // =================================================================
            var bottomBar = new Border
            {
                Padding = new Thickness(18, 12, 18, 14)
            };
            var bottomGrid = new Grid();
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var testModePanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var testToggle = NRBloxTheme.CreateToggleSwitch(_settings.TestMode, val => _settings.TestMode = val);
            testToggle.Margin = new Thickness(0, 0, 10, 0);
            testModePanel.Children.Add(testToggle);
            var testLabel = NRBloxTheme.CreateText("Test mode", 13.5, FontWeights.Normal, "TextPrimaryBrush");
            testLabel.VerticalAlignment = VerticalAlignment.Center;
            testModePanel.Children.Add(testLabel);

            Grid.SetColumn(testModePanel, 0);
            bottomGrid.Children.Add(testModePanel);

            var actionButtons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var saveBtn = NRBloxTheme.CreateStyledButton("Save", true, (s, e) => OnSaveClicked(), new Thickness(0, 0, 8, 0));
            var closeBtn = NRBloxTheme.CreateStyledButton("Close", false, (s, e) => Close());
            actionButtons.Children.Add(saveBtn);
            actionButtons.Children.Add(closeBtn);

            Grid.SetColumn(actionButtons, 1);
            bottomGrid.Children.Add(actionButtons);

            bottomBar.Child = bottomGrid;
            Grid.SetRow(bottomBar, 1);
            rootGrid.Children.Add(bottomBar);

            Content = rootGrid;
        }

        private Border CreateSidebarNavItem(string key, string iconGlyph, string label)
        {
            var itemBorder = new Border
            {
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Transparent,
                Background = Brushes.Transparent,
                Padding = new Thickness(0, 9, 12, 9),
                Margin = new Thickness(0, 2, 0, 2),
                Cursor = Cursors.Hand
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var indicator = new Border
            {
                Width = 3.5,
                Height = 18,
                CornerRadius = new CornerRadius(2),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Hidden
            };
            indicator.SetResourceReference(Border.BackgroundProperty, "AccentPrimaryBrush");
            Grid.SetColumn(indicator, 0);
            grid.Children.Add(indicator);

            var iconTb = NRBloxTheme.CreateText(iconGlyph, 14, FontWeights.Normal, "TextPrimaryBrush");
            iconTb.HorizontalAlignment = HorizontalAlignment.Center;
            iconTb.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(iconTb, 1);
            grid.Children.Add(iconTb);

            var labelTb = NRBloxTheme.CreateText(label, 13.5, FontWeights.Normal, "TextPrimaryBrush");
            labelTb.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(labelTb, 2);
            grid.Children.Add(labelTb);

            itemBorder.Child = grid;
            itemBorder.MouseLeftButtonUp += (s, e) => NavigateTo(key);

            _navItems[key] = itemBorder;
            _navIndicators[key] = indicator;
            return itemBorder;
        }

        public void NavigateTo(string key)
        {
            if (!_pages.ContainsKey(key)) return;
            _pageHost.Content = _pages[key];

            string activeNavKey = (key == "fflag_editor") ? "engine" : key;
            foreach (var kv in _navItems)
            {
                bool isSel = (kv.Key == activeNavKey);
                if (isSel)
                {
                    kv.Value.SetResourceReference(Border.BackgroundProperty, "NavSelectedBgBrush");
                    kv.Value.SetResourceReference(Border.BorderBrushProperty, "BorderSubtleBrush");
                    _navIndicators[kv.Key].Visibility = Visibility.Visible;
                }
                else
                {
                    kv.Value.Background = Brushes.Transparent;
                    kv.Value.BorderBrush = Brushes.Transparent;
                    _navIndicators[kv.Key].Visibility = Visibility.Hidden;
                }
            }
        }

        private void OnSaveClicked()
        {
            JsonHelper.Save(NRBloxPaths.SettingsFile, _settings);
            _fastFlags.ApplySettingsToFlags();
            _fastFlags.SaveAndSync();
            _mods.ApplyAndSyncMods();

            if (_settings.ShortcutDesktopIcon)
                SystemIntegrationService.CreateDesktopShortcuts();

            if (_settings.TestMode)
            {
                var boot = new NRBloxBootstrapperWindow(_settings, _fastFlags, _mods, null, false, false);
                boot.ShowDialog();
            }
            else
            {
                Close();
            }
        }

        private static StackPanel CreatePageHeader(string title, string subtitle)
        {
            var stack = new StackPanel { Margin = new Thickness(0, 0, 0, 18) };
            stack.Children.Add(NRBloxTheme.CreateText(title, 26, FontWeights.SemiBold, "TextPrimaryBrush"));
            if (!string.IsNullOrEmpty(subtitle))
                stack.Children.Add(NRBloxTheme.CreateText(subtitle, 13.5, FontWeights.Normal, "TextSecondaryBrush", new Thickness(0, 4, 0, 0)));
            return stack;
        }

        private static TextBlock CreateSectionHeader(string title, string subtitle = null)
        {
            var tb = NRBloxTheme.CreateText(title, 18, FontWeights.SemiBold, "TextPrimaryBrush", new Thickness(0, 14, 0, subtitle == null ? 10 : 2));
            return tb;
        }

        // =====================================================================
        // PAGE 1: INTEGRATIONS (Screenshots 2, 3, 4)
        // =====================================================================
        private UIElement BuildIntegrationsPage()
        {
            var root = new StackPanel();
            root.Children.Add(CreatePageHeader("Integrations", "Configure additional functionality to go alongside Roblox."));

            root.Children.Add(CreateSectionHeader("Activity tracking"));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Enable activity tracking",
                "Allows for NRBlox to detect what Roblox game you're playing. Certain features may require this.",
                NRBloxTheme.CreateToggleSwitch(_settings.EnableActivityTracking, v => _settings.EnableActivityTracking = v)
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Query server location",
                "When in-game, you'll be able to see where your server is located via ipinfo.io.",
                NRBloxTheme.CreateToggleSwitch(_settings.QueryServerLocation, v => _settings.QueryServerLocation = v)
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Don't exit to desktop app",
                "Roblox will fully close when you leave a game instead of going back to the app.",
                NRBloxTheme.CreateToggleSwitch(_settings.DisableDesktopAppPatch, v => _settings.DisableDesktopAppPatch = v)
            ));

            // Discord Rich Presence
            root.Children.Add(CreateSectionHeader("Discord Rich Presence", "This feature requires activity tracking to be enabled and the Discord desktop app to be installed and running."));
            root.Children.Add(NRBloxTheme.CreateText(
                "This feature requires activity tracking to be enabled and the Discord desktop app to be installed and running.",
                12.5, FontWeights.Normal, "TextSecondaryBrush", new Thickness(0, 0, 0, 10)
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Show game activity",
                "The Roblox game you're playing will be shown on your Discord profile.",
                NRBloxTheme.CreateToggleSwitch(_settings.EnableDiscordRichPresence, v => _settings.EnableDiscordRichPresence = v)
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Allow activity joining",
                "Allows for anybody to join the game you're currently in through your Discord profile.",
                NRBloxTheme.CreateToggleSwitch(_settings.AllowActivityJoining, v => _settings.AllowActivityJoining = v)
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Show Roblox account",
                "Shows the Roblox account you're playing with on your Discord profile.",
                NRBloxTheme.CreateToggleSwitch(_settings.ShowAccountOnRichPresence, v => _settings.ShowAccountOnRichPresence = v)
            ));

            // Custom Integrations (Master-Detail layout matching Screenshot 4)
            root.Children.Add(CreateSectionHeader("Custom Integrations", "Here, you can have other programs launch with Roblox automatically."));
            root.Children.Add(NRBloxTheme.CreateText(
                "Here, you can have other programs launch with Roblox automatically.",
                12.5, FontWeights.Normal, "TextSecondaryBrush", new Thickness(0, 0, 0, 10)
            ));

            var ciGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            ciGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
            ciGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var leftStack = new StackPanel();
            _customIntListBox = new ListBox
            {
                Height = 175,
                Margin = new Thickness(0, 0, 0, 8),
                FontSize = 13
            };
            _customIntListBox.SetResourceReference(Control.BackgroundProperty, "BgCardBrush");
            _customIntListBox.SetResourceReference(Control.BorderBrushProperty, "BorderSubtleBrush");
            _customIntListBox.SetResourceReference(Control.ForegroundProperty, "TextPrimaryBrush");

            if (_settings.CustomIntegrationItems != null)
            {
                foreach (var item in _settings.CustomIntegrationItems)
                    _customIntListBox.Items.Add(item);
            }
            _customIntListBox.SelectionChanged += (s, e) => RenderCustomIntegrationDetail();
            leftStack.Children.Add(_customIntListBox);

            var ciButtonsGrid = new UniformGrid { Columns = 2 };
            var newBtn = NRBloxTheme.CreateStyledButton("＋ New", false, (s, e) =>
            {
                if (_settings.CustomIntegrationItems == null)
                    _settings.CustomIntegrationItems = new List<CustomIntegrationItem>();
                var item = new CustomIntegrationItem { Name = "New Integration" };
                _settings.CustomIntegrationItems.Add(item);
                _customIntListBox.Items.Add(item);
                _customIntListBox.SelectedItem = item;
            }, new Thickness(0, 0, 4, 0));

            var delBtn = NRBloxTheme.CreateStyledButton("🗑 Delete", false, (s, e) =>
            {
                var sel = _customIntListBox.SelectedItem as CustomIntegrationItem;
                if (sel != null)
                {
                    _settings.CustomIntegrationItems.Remove(sel);
                    _customIntListBox.Items.Remove(sel);
                    RenderCustomIntegrationDetail();
                }
            }, new Thickness(4, 0, 0, 0));

            ciButtonsGrid.Children.Add(newBtn);
            ciButtonsGrid.Children.Add(delBtn);
            leftStack.Children.Add(ciButtonsGrid);

            Grid.SetColumn(leftStack, 0);
            ciGrid.Children.Add(leftStack);

            _customIntDetailHost = new ContentControl { Margin = new Thickness(16, 0, 0, 0) };
            RenderCustomIntegrationDetail();
            Grid.SetColumn(_customIntDetailHost, 1);
            ciGrid.Children.Add(_customIntDetailHost);

            root.Children.Add(ciGrid);
            return root;
        }

        private void RenderCustomIntegrationDetail()
        {
            if (_customIntDetailHost == null) return;
            var sel = _customIntListBox != null ? (_customIntListBox.SelectedItem as CustomIntegrationItem) : null;

            if (sel == null)
            {
                var emptyText = NRBloxTheme.CreateText("No integration selected, please select or add a new one", 13, FontWeights.Normal, "TextSecondaryBrush");
                emptyText.HorizontalAlignment = HorizontalAlignment.Center;
                emptyText.VerticalAlignment = VerticalAlignment.Center;
                _customIntDetailHost.Content = emptyText;
                return;
            }

            var detailCard = NRBloxTheme.CreateCard(new Thickness(0), new Thickness(16));
            var stack = new StackPanel();

            stack.Children.Add(NRBloxTheme.CreateText("Name", 12.5, FontWeights.SemiBold, "TextPrimaryBrush", new Thickness(0, 0, 0, 4)));
            var nameBox = new TextBox { Text = sel.Name, Padding = new Thickness(8, 6, 8, 6), Margin = new Thickness(0, 0, 0, 10) };
            nameBox.TextChanged += (s, e) => { sel.Name = nameBox.Text; _customIntListBox.Items.Refresh(); };
            stack.Children.Add(nameBox);

            stack.Children.Add(NRBloxTheme.CreateText("Application Location", 12.5, FontWeights.SemiBold, "TextPrimaryBrush", new Thickness(0, 0, 0, 4)));
            var locGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            locGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            locGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var locBox = new TextBox { Text = sel.Location, Padding = new Thickness(8, 6, 8, 6), Margin = new Thickness(0, 0, 8, 0) };
            locBox.TextChanged += (s, e) => sel.Location = locBox.Text;
            Grid.SetColumn(locBox, 0);
            locGrid.Children.Add(locBox);

            var browseBtn = NRBloxTheme.CreateStyledButton("📁 Browse", false, (s, e) =>
            {
                var dlg = new OpenFileDialog { Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*" };
                if (dlg.ShowDialog() == true)
                {
                    locBox.Text = dlg.FileName;
                    if (sel.Name == "New Integration")
                    {
                        nameBox.Text = Path.GetFileNameWithoutExtension(dlg.FileName);
                    }
                }
            });
            Grid.SetColumn(browseBtn, 1);
            locGrid.Children.Add(browseBtn);
            stack.Children.Add(locGrid);

            stack.Children.Add(NRBloxTheme.CreateText("Launch Arguments", 12.5, FontWeights.SemiBold, "TextPrimaryBrush", new Thickness(0, 0, 0, 4)));
            var argsBox = new TextBox { Text = sel.LaunchArgs, Padding = new Thickness(8, 6, 8, 6) };
            argsBox.TextChanged += (s, e) => sel.LaunchArgs = argsBox.Text;
            stack.Children.Add(argsBox);

            detailCard.Child = stack;
            _customIntDetailHost.Content = detailCard;
        }

        // =====================================================================
        // PAGE 2: BOOTSTRAPPER (Screenshot 5 + NRBlox Multi-Instance)
        // =====================================================================
        private UIElement BuildBootstrapperPage()
        {
            var root = new StackPanel();
            root.Children.Add(CreatePageHeader("Bootstrapper", "Configure what NRBlox should do when launching Roblox."));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Prompt to confirm when launching another Roblox instance",
                "Prevent against closures of your existing game from accidentally launching another one.",
                NRBloxTheme.CreateToggleSwitch(_settings.ConfirmLaunches, v => _settings.ConfirmLaunches = v)
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Background updates",
                "Update Roblox in the background instead of blocking launch when an update is available. At least 5GB of free storage space is required for this feature.",
                NRBloxTheme.CreateToggleSwitch(_settings.BackgroundUpdates, v => _settings.BackgroundUpdates = v)
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Force Roblox reinstallation",
                "Roblox will be installed fresh on next launch.",
                NRBloxTheme.CreateToggleSwitch(_settings.ForceRobloxReinstall, v => _settings.ForceRobloxReinstall = v)
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Allow multiple Roblox instances (Multi-Instance)",
                "Holds ROBLOX_singletonMutex so you can run multiple Roblox game windows simultaneously.",
                NRBloxTheme.CreateToggleSwitch(_settings.MultiInstanceLaunching, v =>
                {
                    _settings.MultiInstanceLaunching = v;
                    SystemIntegrationService.SetMultiInstanceMutex(v);
                })
            ));

            return root;
        }

        // =====================================================================
        // PAGE 3: MODS (Screenshots 6 & 7)
        // =====================================================================
        private UIElement BuildModsPage()
        {
            var root = new StackPanel();
            root.Children.Add(CreatePageHeader("Mods", "Manage and apply file mods to the Roblox game client."));

            // Top 2-column cards: Open Mods Folder | Help
            var topGrid = new UniformGrid { Columns = 2, Margin = new Thickness(0, 0, 0, 10) };
            topGrid.Children.Add(NRBloxTheme.CreateActionCard(
                "📁",
                "Open Mods Folder",
                "Manage custom Roblox mods here.",
                () => OpenFolder(NRBloxPaths.ModificationsDir),
                new Thickness(0, 0, 5, 0)
            ));
            topGrid.Children.Add(NRBloxTheme.CreateActionCard(
                "📓",
                "Help",
                "See info about managing and creating mods.",
                () => OpenUrl(NRBloxPaths.GitHubWikiUrl),
                new Thickness(5, 0, 0, 0)
            ));
            root.Children.Add(topGrid);

            root.Children.Add(NRBloxTheme.CreateActionCard(
                "🛠️",
                "Manage compatibility settings",
                "Configure application parameters such as DPI scaling behaviour and fullscreen optimizations.",
                () => OpenCompatibilityProperties(),
                new Thickness(0, 0, 0, 10)
            ));

            root.Children.Add(CreateSectionHeader("Presets"));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Mouse cursor",
                "Choose between using two classic Roblox cursor styles.",
                NRBloxTheme.CreateStyledComboBox(
                    new string[] { "Default", "From 2006 (Cartoon)", "From 2013 (Angular)" },
                    _settings.CursorType,
                    v => _settings.CursorType = v
                )
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Use old avatar editor background",
                "Bring back the old avatar editor background used in the Roblox app prior to 2020.",
                NRBloxTheme.CreateToggleSwitch(_settings.UseOldAvatarBackground, v => _settings.UseOldAvatarBackground = v)
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Emulate old character sounds",
                "An attempt to roughly bring back the character sounds used prior to 2014.",
                NRBloxTheme.CreateToggleSwitch(_settings.UseOldCharacterSounds, v => _settings.UseOldCharacterSounds = v)
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Preferred emoji type",
                "Choose which type of emoji should Roblox use.",
                NRBloxTheme.CreateStyledComboBox(
                    new string[] { "Default (Twemoji)", "Catmoji", "Windows 11", "Windows 10", "Windows 8.1" },
                    _settings.PreferredEmojiType,
                    v => _settings.PreferredEmojiType = v
                )
            ));

            root.Children.Add(CreateSectionHeader("Miscellaneous"));

            var chooseFontBtn = NRBloxTheme.CreateStyledButton(
                string.IsNullOrEmpty(_settings.CustomFontPath) ? "🔤 Choose font..." : "🗑 Remove custom font",
                false,
                null
            );
            chooseFontBtn.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(_settings.CustomFontPath))
                {
                    _settings.CustomFontPath = "";
                    chooseFontBtn.Content = "🔤 Choose font...";
                    return;
                }
                var dlg = new OpenFileDialog
                {
                    Title = "Select Custom Font",
                    Filter = "Font files (*.ttf;*.otf)|*.ttf;*.otf"
                };
                if (dlg.ShowDialog() == true)
                {
                    _settings.CustomFontPath = dlg.FileName;
                    chooseFontBtn.Content = "🗑 Remove custom font (" + Path.GetFileName(dlg.FileName) + ")";
                }
            };

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Use custom font",
                null,
                chooseFontBtn
            ));

            return root;
        }

        // =====================================================================
        // PAGE 4: ENGINE SETTINGS (Screenshots 8 & 9 + Extra NRBlox Presets)
        // =====================================================================
        private UIElement BuildEngineSettingsPage()
        {
            var root = new StackPanel();
            root.Children.Add(CreatePageHeader("Engine Settings", "Control how specific Roblox engine parameters and features are configured."));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Allow NRBlox to manage Fast Flags",
                "Disabling this will prevent anything configured here from being applied to Roblox.",
                NRBloxTheme.CreateToggleSwitch(_settings.UseFastFlagManager, v => _settings.UseFastFlagManager = v)
            ));

            root.Children.Add(CreateSectionHeader("Presets"));
            root.Children.Add(NRBloxTheme.CreateText("Rendering and Graphics", 14.5, FontWeights.SemiBold, "TextPrimaryBrush", new Thickness(0, 0, 0, 2)));
            root.Children.Add(NRBloxTheme.CreateText("Direct3D exclusive fullscreen using Alt+Enter is enabled by default.", 12, FontWeights.Normal, "TextSecondaryBrush", new Thickness(0, 0, 0, 10)));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Anti-aliasing quality (MSAA)",
                null,
                NRBloxTheme.CreateStyledComboBox(
                    new string[] { "Automatic", "1x", "2x", "4x" },
                    _settings.MsaaMode,
                    v => _settings.MsaaMode = v
                )
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Preserve rendering quality with display scaling",
                "Roblox reduces your rendering quality depending on how your display is scaled in Windows.",
                NRBloxTheme.CreateToggleSwitch(_settings.DisableDpiScale, v => _settings.DisableDpiScale = v)
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Texture quality",
                null,
                NRBloxTheme.CreateStyledComboBox(
                    new string[] { "Automatic", "Level 0 (Lowest)", "Level 1 (Low)", "Level 2 (Medium)", "Level 3 (Highest)" },
                    _settings.TextureQuality,
                    v => _settings.TextureQuality = v
                )
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Framerate limit (FPS Unlocker)",
                "Set the target framerate limit for the Roblox task scheduler.",
                NRBloxTheme.CreateStyledComboBox(
                    new string[] { "60", "120", "144", "165", "240", "360", "999", "0" },
                    _settings.TargetFps.ToString(),
                    v =>
                    {
                        int fps;
                        if (int.TryParse(v, out fps)) _settings.TargetFps = fps;
                    }
                )
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Rendering mode (Graphics API)",
                "Choose the graphics API used by the Roblox engine.",
                NRBloxTheme.CreateStyledComboBox(
                    new string[] { "Automatic", "Direct3D 11", "Direct3D 10", "Vulkan", "OpenGL" },
                    _settings.RenderingMode,
                    v => _settings.RenderingMode = v
                )
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Preferred lighting technology",
                "Force a specific lighting phase across games.",
                NRBloxTheme.CreateStyledComboBox(
                    new string[] { "Chosen by game", "Voxel (Phase 1)", "Shadow Map (Phase 2)", "Future (Phase 3)" },
                    _settings.LightingMode,
                    v => _settings.LightingMode = v
                )
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Reset everything to defaults",
                null,
                NRBloxTheme.CreateToggleSwitch(_settings.ResetEverythingToDefaults, v => _settings.ResetEverythingToDefaults = v),
                new Thickness(0, 12, 0, 10)
            ));

            root.Children.Add(NRBloxTheme.CreateActionCard(
                "✏️",
                "Fast Flag Editor",
                "Manage your own Fast Flags. Use with caution.",
                () =>
                {
                    RefreshFastFlagEditorRows();
                    NavigateTo("fflag_editor");
                },
                new Thickness(0, 0, 0, 10)
            ));

            return root;
        }

        // =====================================================================
        // SUB-PAGE: FAST FLAG EDITOR
        // =====================================================================
        private UIElement BuildFastFlagEditorPage()
        {
            var root = new StackPanel();

            var topRow = new Grid { Margin = new Thickness(0, 0, 0, 14) };
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel();
            titleStack.Children.Add(NRBloxTheme.CreateText("Fast Flag Editor", 24, FontWeights.SemiBold, "TextPrimaryBrush"));
            titleStack.Children.Add(NRBloxTheme.CreateText("Manage your own Fast Flags in ClientAppSettings.json.", 13, FontWeights.Normal, "TextSecondaryBrush", new Thickness(0, 3, 0, 0)));
            Grid.SetColumn(titleStack, 0);
            topRow.Children.Add(titleStack);

            var backBtn = NRBloxTheme.CreateStyledButton("← Back to Engine Settings", false, (s, e) => NavigateTo("engine"));
            backBtn.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(backBtn, 1);
            topRow.Children.Add(backBtn);
            root.Children.Add(topRow);

            // Add Bar
            var addCard = NRBloxTheme.CreateCard(new Thickness(0, 0, 0, 12), new Thickness(14));
            var addGrid = new Grid();
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var nameBox = new TextBox { Text = "FFlagDebugDisplayFPS", Padding = new Thickness(8, 6, 8, 6), Margin = new Thickness(0, 0, 8, 0) };
            var valBox = new TextBox { Text = "True", Padding = new Thickness(8, 6, 8, 6), Margin = new Thickness(0, 0, 8, 0) };
            var addBtn = NRBloxTheme.CreateStyledButton("＋ Add New", true, (s, e) =>
            {
                string k = (nameBox.Text ?? "").Trim();
                string v = (valBox.Text ?? "").Trim();
                if (!string.IsNullOrEmpty(k))
                {
                    _fastFlags.SetOrRemove(k, v);
                    _fastFlags.SaveAndSync();
                    RefreshFastFlagEditorRows();
                }
            });

            Grid.SetColumn(nameBox, 0);
            Grid.SetColumn(valBox, 1);
            Grid.SetColumn(addBtn, 2);
            addGrid.Children.Add(nameBox);
            addGrid.Children.Add(valBox);
            addGrid.Children.Add(addBtn);
            addCard.Child = addGrid;
            root.Children.Add(addCard);

            _fastFlagRowsPanel = new StackPanel();
            RefreshFastFlagEditorRows();
            root.Children.Add(_fastFlagRowsPanel);

            return root;
        }

        private void RefreshFastFlagEditorRows()
        {
            if (_fastFlagRowsPanel == null) return;
            _fastFlagRowsPanel.Children.Clear();

            foreach (var kv in new Dictionary<string, string>(_fastFlags.Flags))
            {
                string key = kv.Key;
                string val = kv.Value;

                var card = NRBloxTheme.CreateCard(new Thickness(0, 0, 0, 6), new Thickness(14, 10, 14, 10));
                var g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var kTb = NRBloxTheme.CreateText(key, 13, FontWeights.SemiBold, "TextPrimaryBrush");
                kTb.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(kTb, 0);
                g.Children.Add(kTb);

                var vTb = NRBloxTheme.CreateText(val, 13, FontWeights.Normal, "AccentLinkBrush", new Thickness(12, 0, 14, 0));
                vTb.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(vTb, 1);
                g.Children.Add(vTb);

                var delBtn = NRBloxTheme.CreateStyledButton("Delete", false, (s, e) =>
                {
                    _fastFlags.SetOrRemove(key, null);
                    _fastFlags.SaveAndSync();
                    RefreshFastFlagEditorRows();
                });
                delBtn.Padding = new Thickness(12, 4, 12, 4);
                Grid.SetColumn(delBtn, 2);
                g.Children.Add(delBtn);

                card.Child = g;
                _fastFlagRowsPanel.Children.Add(card);
            }
        }

        // =====================================================================
        // PAGE 5: APPEARANCE (Screenshots 10 & 11)
        // =====================================================================
        private UIElement BuildAppearancePage()
        {
            var root = new StackPanel();
            root.Children.Add(CreatePageHeader("Appearance", "Configure how NRBlox should look."));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Global Theme",
                null,
                NRBloxTheme.CreateStyledComboBox(
                    new string[] { "System default", "Light", "Dark" },
                    _settings.ThemeMode,
                    v =>
                    {
                        _settings.ThemeMode = v;
                        NRBloxTheme.ApplyTheme(Resources, v);
                    }
                )
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Language",
                "A relaunch is required for changes to take effect.",
                NRBloxTheme.CreateStyledComboBox(
                    new string[] { "System default", "Português (Brasil)", "English" },
                    _settings.Language,
                    v => _settings.Language = v
                )
            ));

            // Bootstrapper header with [Preview] button on the right
            var bootHeaderGrid = new Grid { Margin = new Thickness(0, 14, 0, 10) };
            bootHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bootHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var bootTexts = new StackPanel();
            bootTexts.Children.Add(NRBloxTheme.CreateText("Bootstrapper", 18, FontWeights.SemiBold, "TextPrimaryBrush"));
            bootTexts.Children.Add(NRBloxTheme.CreateText("You can make it look different, retro, or even just like Roblox.", 12.5, FontWeights.Normal, "TextSecondaryBrush", new Thickness(0, 2, 0, 0)));
            Grid.SetColumn(bootTexts, 0);
            bootHeaderGrid.Children.Add(bootTexts);

            var previewBtn = NRBloxTheme.CreateStyledButton("Preview", false, (s, e) =>
            {
                var prev = new NRBloxBootstrapperWindow(_settings, _fastFlags, _mods, null, false, true);
                prev.ShowDialog();
            });
            previewBtn.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(previewBtn, 1);
            bootHeaderGrid.Children.Add(previewBtn);
            root.Children.Add(bootHeaderGrid);

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Style",
                "Choose how it should look. Dark theme does not apply to Legacy/Vista.",
                NRBloxTheme.CreateStyledComboBox(
                    new string[] { "NRBlox", "Fluent Aero", "Vista", "Legacy (2008)", "Legacy (2011)", "Progress" },
                    _settings.BootstrapperStyle,
                    v => _settings.BootstrapperStyle = v
                )
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Icon",
                "Choose what icon it should use.",
                NRBloxTheme.CreateStyledComboBox(
                    new string[] { "NRBlox", "2008", "2011", "Early 2015", "Late 2015", "2017", "2019", "2022", "Custom" },
                    _settings.BootstrapperIcon,
                    v => _settings.BootstrapperIcon = v
                )
            ));

            // Expandable Bootstrapper customization (Matches Screenshot 11)
            var custBody = new StackPanel();

            var titleGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var titleLbl = NRBloxTheme.CreateText("Title", 13, FontWeights.Normal, "TextSecondaryBrush");
            titleLbl.VerticalAlignment = VerticalAlignment.Top;
            titleLbl.Margin = new Thickness(0, 6, 0, 0);
            Grid.SetColumn(titleLbl, 0);
            titleGrid.Children.Add(titleLbl);

            var titleRight = new StackPanel();
            var titleBox = new TextBox { Text = _settings.BootstrapperTitle ?? "NRBlox", Padding = new Thickness(10, 6, 10, 6), FontSize = 13 };
            titleBox.TextChanged += (s, e) => _settings.BootstrapperTitle = titleBox.Text;
            titleRight.Children.Add(titleBox);
            titleRight.Children.Add(NRBloxTheme.CreateText("The text that shows as the title of the bootstrapper.", 11.5, FontWeights.Normal, "TextSecondaryBrush", new Thickness(0, 4, 0, 0)));
            Grid.SetColumn(titleRight, 1);
            titleGrid.Children.Add(titleRight);
            custBody.Children.Add(titleGrid);

            var iconGrid = new Grid();
            iconGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            iconGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var iconLbl = NRBloxTheme.CreateText("Custom Icon", 13, FontWeights.Normal, "TextSecondaryBrush");
            iconLbl.VerticalAlignment = VerticalAlignment.Top;
            iconLbl.Margin = new Thickness(0, 6, 0, 0);
            Grid.SetColumn(iconLbl, 0);
            iconGrid.Children.Add(iconLbl);

            var iconRight = new StackPanel();
            var iconRowGrid = new Grid();
            iconRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            iconRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var customIconBox = new TextBox { Text = _settings.BootstrapperCustomIconPath ?? "", Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 8, 0), FontSize = 13 };
            customIconBox.TextChanged += (s, e) => _settings.BootstrapperCustomIconPath = customIconBox.Text;
            Grid.SetColumn(customIconBox, 0);
            iconRowGrid.Children.Add(customIconBox);

            var browseIconBtn = NRBloxTheme.CreateStyledButton("📁 Browse", false, (s, e) =>
            {
                var dlg = new OpenFileDialog { Filter = "Icon files (*.ico)|*.ico" };
                if (dlg.ShowDialog() == true) customIconBox.Text = dlg.FileName;
            });
            Grid.SetColumn(browseIconBtn, 1);
            iconRowGrid.Children.Add(browseIconBtn);

            iconRight.Children.Add(iconRowGrid);
            iconRight.Children.Add(NRBloxTheme.CreateText("Must be a multi-size .ico file with sizes 16px to 128px. Set Icon as 'Custom' to use it.", 11.5, FontWeights.Normal, "TextSecondaryBrush", new Thickness(0, 4, 0, 0)));
            Grid.SetColumn(iconRight, 1);
            iconGrid.Children.Add(iconGrid.Children.Count > 0 ? iconRight : iconRight);
            custBody.Children.Add(iconGrid);

            root.Children.Add(NRBloxTheme.CreateExpandableCard(
                "Bootstrapper customization",
                "Configure other customizable Bootstrapper options.",
                null,
                custBody,
                true
            ));

            return root;
        }

        // =====================================================================
        // PAGE 6: SHORTCUTS (Screenshot 12)
        // =====================================================================
        private UIElement BuildShortcutsPage()
        {
            var root = new StackPanel();
            root.Children.Add(CreatePageHeader("Shortcuts", "Configure how NRBlox can be readily launched."));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Extract Roblox icons to folder",
                "Allows you to use NRBlox's range of Roblox icons for your shortcuts.",
                NRBloxTheme.CreateToggleSwitch(_settings.ExtractIconsToFolder, v => _settings.ExtractIconsToFolder = v)
            ));

            root.Children.Add(CreateSectionHeader("General"));
            root.Children.Add(NRBloxTheme.CreateText("These are the shortcuts that bring up the multi-choice launch menu.", 12.5, FontWeights.Normal, "TextSecondaryBrush", new Thickness(0, 0, 0, 10)));

            var genGrid = new UniformGrid { Columns = 2, Margin = new Thickness(0, 0, 0, 10) };
            genGrid.Children.Add(NRBloxTheme.CreateSettingRow(
                "Desktop icon",
                null,
                NRBloxTheme.CreateToggleSwitch(_settings.ShortcutDesktopIcon, v => _settings.ShortcutDesktopIcon = v),
                new Thickness(0, 0, 5, 0)
            ));
            genGrid.Children.Add(NRBloxTheme.CreateSettingRow(
                "Start Menu icon",
                null,
                NRBloxTheme.CreateToggleSwitch(_settings.ShortcutStartMenuIcon, v => _settings.ShortcutStartMenuIcon = v),
                new Thickness(5, 0, 0, 0)
            ));
            root.Children.Add(genGrid);

            root.Children.Add(CreateSectionHeader("Function"));
            root.Children.Add(NRBloxTheme.CreateText("Create shortcuts for quick access to specific functions. These will all be placed on the Desktop.", 12.5, FontWeights.Normal, "TextSecondaryBrush", new Thickness(0, 0, 0, 10)));

            var fnGrid = new UniformGrid { Columns = 3, Margin = new Thickness(0, 0, 0, 10) };
            fnGrid.Children.Add(NRBloxTheme.CreateSettingRow(
                "Launch Roblox",
                null,
                NRBloxTheme.CreateToggleSwitch(_settings.ShortcutLaunchRoblox, v => _settings.ShortcutLaunchRoblox = v),
                new Thickness(0, 0, 5, 0)
            ));
            fnGrid.Children.Add(NRBloxTheme.CreateSettingRow(
                "Launch Roblox Studio",
                null,
                NRBloxTheme.CreateToggleSwitch(_settings.ShortcutLaunchStudio, v => _settings.ShortcutLaunchStudio = v),
                new Thickness(5, 0, 5, 0)
            ));
            fnGrid.Children.Add(NRBloxTheme.CreateSettingRow(
                "NRBlox Settings",
                null,
                NRBloxTheme.CreateToggleSwitch(_settings.ShortcutSettings, v => _settings.ShortcutSettings = v),
                new Thickness(5, 0, 0, 0)
            ));
            root.Children.Add(fnGrid);

            return root;
        }

        // =====================================================================
        // PAGE 7: NRBLOX BEHAVIOUR (Screenshot 13)
        // =====================================================================
        private UIElement BuildNRBloxBehaviourPage()
        {
            var root = new StackPanel();
            root.Children.Add(CreatePageHeader("NRBlox", "Configure options related to NRBlox's behaviour itself."));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Automatically update NRBlox",
                "NRBlox will automatically check and update itself when launching Roblox.",
                NRBloxTheme.CreateToggleSwitch(_settings.AutoUpdateNRBlox, v => _settings.AutoUpdateNRBlox = v)
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Enable sending of analytics",
                "Anonymous diagnostic telemetry.",
                NRBloxTheme.CreateToggleSwitch(_settings.EnableAnalytics, v => _settings.EnableAnalytics = v)
            ));

            var exportBtn = NRBloxTheme.CreateStyledButton("📋 Export", false, (s, e) =>
            {
                string diag = SystemIntegrationService.VerifyClientInstallation();
                Clipboard.SetText(diag);
                MessageBox.Show("Diagnostic data copied to clipboard!", "NRBlox", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            var exportChecksGrid = new UniformGrid { Columns = 2 };
            var cb1 = new CheckBox
            {
                Content = NRBloxTheme.CreateText("NRBlox configuration", 13, FontWeights.Normal, "TextPrimaryBrush"),
                IsChecked = _settings.ExportConfigCheck,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            cb1.Checked += (s, e) => _settings.ExportConfigCheck = true;
            cb1.Unchecked += (s, e) => _settings.ExportConfigCheck = false;

            var cb2 = new CheckBox
            {
                Content = NRBloxTheme.CreateText("All NRBlox logs", 13, FontWeights.Normal, "TextPrimaryBrush"),
                IsChecked = _settings.ExportLogsCheck,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            cb2.Checked += (s, e) => _settings.ExportLogsCheck = true;
            cb2.Unchecked += (s, e) => _settings.ExportLogsCheck = false;

            exportChecksGrid.Children.Add(cb1);
            exportChecksGrid.Children.Add(cb2);

            root.Children.Add(NRBloxTheme.CreateExpandableCard(
                "Export diagnostic data",
                "Gather information that can be uploaded online to troubleshoot a problem you're having.",
                exportBtn,
                exportChecksGrid,
                true
            ));

            return root;
        }

        // =====================================================================
        // PAGE 8: ABOUT (FaelSemW + Discord + Wiki)
        // =====================================================================
        private UIElement BuildAboutPage()
        {
            var root = new StackPanel();
            root.Children.Add(CreatePageHeader("About NRBlox", "100% Open-Source Roblox Engine Controller & Bootstrapper."));

            var heroCard = NRBloxTheme.CreateCard(new Thickness(0, 0, 0, 14), new Thickness(20));
            var heroStack = new StackPanel { Orientation = Orientation.Horizontal };
            var logo = NRBloxTheme.CreateNRBloxLogo(48);
            logo.Margin = new Thickness(0, 0, 18, 0);
            heroStack.Children.Add(logo);

            var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            infoStack.Children.Add(NRBloxTheme.CreateText("NRBlox Version 3.0.0", 18, FontWeights.SemiBold, "TextPrimaryBrush"));
            infoStack.Children.Add(NRBloxTheme.CreateText("Created & Maintained by FaelSemW • Licensed under MIT", 13, FontWeights.Normal, "TextSecondaryBrush", new Thickness(0, 3, 0, 0)));
            heroStack.Children.Add(infoStack);
            heroCard.Child = heroStack;
            root.Children.Add(heroCard);

            root.Children.Add(NRBloxTheme.CreateActionCard(
                "💬",
                "Official Discord Community",
                "Join the official NRBlox community server on Discord.",
                () => OpenUrl(NRBloxPaths.DiscordUrl),
                new Thickness(0, 0, 0, 10)
            ));

            root.Children.Add(NRBloxTheme.CreateActionCard(
                "📖",
                "Official NRBlox Wiki",
                "Read the complete documentation, FastFlag guides, and troubleshooting tips on GitHub.",
                () => OpenUrl(NRBloxPaths.GitHubWikiUrl),
                new Thickness(0, 0, 0, 10)
            ));

            root.Children.Add(NRBloxTheme.CreateActionCard(
                "⭐",
                "GitHub Repository",
                "View source code and releases at github.com/FaelSemW/NRBlox.",
                () => OpenUrl(NRBloxPaths.GitHubRepoUrl),
                new Thickness(0, 0, 0, 10)
            ));

            return root;
        }

        private static void OpenCompatibilityProperties()
        {
            try
            {
                string exe = Path.Combine(NRBloxPaths.GetRobloxVersionDir(), "RobloxPlayerBeta.exe");
                if (File.Exists(exe))
                {
                    Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = "/select,\"" + exe + "\"", UseShellExecute = true });
                }
            }
            catch { }
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch { }
        }

        private static void OpenFolder(string folder)
        {
            try
            {
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = "\"" + folder + "\"", UseShellExecute = true });
            }
            catch { }
        }
    }
}
