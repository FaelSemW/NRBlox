// ============================================================================
// NRBlox (Windows Native Edition) — Main Desktop Window (Dark & Light Mode)
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
        private readonly NativeDiscordRpcClient _discord;
        private readonly ActivityWatcherService _watcher;

        private readonly Dictionary<string, UIElement> _tabViews = new Dictionary<string, UIElement>();
        private readonly Dictionary<string, Button> _navButtons = new Dictionary<string, Button>();
        private ContentControl _pageHost;
        private Button _themeToggleBtn;
        private TextBlock _statusFooterText;

        // Live UI Labels
        private TextBlock _flagCountMetric;
        private TextBlock _fpsMetric;
        private TextBlock _serverLocationLabel;
        private TextBlock _serverIpLabel;
        private TextBlock _serverPlaceJobLabel;
        private StackPanel _flagListPanel;
        private StackPanel _historyListPanel;
        private StackPanel _customIntegrationsPanel;

        public NRBloxMainWindow()
        {
            NRBloxPaths.EnsureDirectories();
            _settings = JsonHelper.LoadOrCreate<NRBloxSettings>(NRBloxPaths.SettingsFile);
            _fastFlags = new FastFlagEngine(_settings);
            _mods = new ModEngine(_settings);
            _discord = new NativeDiscordRpcClient();
            _watcher = new ActivityWatcherService(_settings, _discord);

            // Sync on startup
            _fastFlags.SaveAndSync();
            _mods.ApplyAndSyncMods();
            SystemIntegrationService.SetMultiInstanceMutex(_settings.MultiInstanceLaunching);
            _watcher.OnSessionUpdated += OnServerSessionUpdated;
            _watcher.Start();

            Title = "NRBlox — Windows Engine Controller";
            Width = 1140;
            Height = 740;
            MinWidth = 980;
            MinHeight = 640;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;

            bool isDark = !string.Equals(_settings.ThemeMode, "Light", StringComparison.OrdinalIgnoreCase);
            NRBloxTheme.ApplyTheme(Resources, isDark);

            BuildWindowChrome();
            SwitchTab("home");
        }

        private void BuildWindowChrome()
        {
            var outerShell = new Border
            {
                CornerRadius = new CornerRadius(14),
                BorderThickness = new Thickness(1.5),
                Margin = new Thickness(8),
                ClipToBounds = true
            };
            outerShell.SetResourceReference(Border.BackgroundProperty, "BgWindowBrush");
            outerShell.SetResourceReference(Border.BorderBrushProperty, "BorderSubtleBrush");

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(68) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(38) });

            // =================================================================
            // ROW 0: Top Command Bar (Horizontal Navigation + Theme + Launch)
            // =================================================================
            var topBar = new Border
            {
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(20, 0, 16, 0)
            };
            topBar.SetResourceReference(Border.BackgroundProperty, "BgHeaderBrush");
            topBar.SetResourceReference(Border.BorderBrushProperty, "BorderSubtleBrush");
            topBar.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                    WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
                else if (e.LeftButton == MouseButtonState.Pressed)
                    DragMove();
            };

            var topGrid = new Grid();
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Brand Logo
            var brandPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var logoBox = new Border
            {
                Width = 36,
                Height = 36,
                CornerRadius = new CornerRadius(9),
                Margin = new Thickness(0, 0, 11, 0),
                Child = new TextBlock
                {
                    Text = "⚡",
                    FontSize = 18,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            logoBox.SetResourceReference(Border.BackgroundProperty, "AccentPrimaryBrush");
            brandPanel.Children.Add(logoBox);

            var brandTexts = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            brandTexts.Children.Add(NRBloxTheme.CreateText("NRBLOX", 17, FontWeights.ExtraBold, "TextPrimaryBrush"));
            brandTexts.Children.Add(NRBloxTheme.CreateText("WINDOWS ENGINE v3.0", 9.5, FontWeights.Bold, "AccentPrimaryBrush"));
            brandPanel.Children.Add(brandTexts);
            Grid.SetColumn(brandPanel, 0);
            topGrid.Children.Add(brandPanel);

            // Center Horizontal Pill Navigation Bar
            var navBar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            navBar.Children.Add(CreateNavPill("home", "⚡ Início"));
            navBar.Children.Add(CreateNavPill("fastflags", "🚀 FastFlags"));
            navBar.Children.Add(CreateNavPill("mods", "🎨 Mods & Visuais"));
            navBar.Children.Add(CreateNavPill("server", "📡 Servidor & RPC"));
            navBar.Children.Add(CreateNavPill("system", "🛠️ Sistema & Sobre"));
            Grid.SetColumn(navBar, 1);
            topGrid.Children.Add(navBar);

            // Right Controls: Theme Toggle, Discord, JOGAR, Window Chrome Buttons
            var rightActions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            _themeToggleBtn = NRBloxTheme.CreateStyledButton(
                NRBloxTheme.IsDark ? "☀️ Modo Claro" : "🌙 Modo Escuro",
                false,
                (s, e) => ToggleTheme(),
                new Thickness(0, 0, 8, 0)
            );
            _themeToggleBtn.Padding = new Thickness(12, 7, 12, 7);
            rightActions.Children.Add(_themeToggleBtn);

            var discordBtn = NRBloxTheme.CreateStyledButton("💬 Discord", false, (s, e) => OpenUrl(NRBloxPaths.DiscordUrl), new Thickness(0, 0, 8, 0));
            discordBtn.Padding = new Thickness(12, 7, 12, 7);
            rightActions.Children.Add(discordBtn);

            var playBtn = NRBloxTheme.CreateStyledButton("▶ JOGAR", true, (s, e) => LaunchRoblox(false, null), new Thickness(0, 0, 14, 0));
            playBtn.Padding = new Thickness(16, 7, 16, 7);
            rightActions.Children.Add(playBtn);

            // Window Min / Max / Close
            rightActions.Children.Add(CreateWindowCtrlBtn("—", (s, e) => WindowState = WindowState.Minimized));
            rightActions.Children.Add(CreateWindowCtrlBtn("▢", (s, e) => WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized));
            rightActions.Children.Add(CreateWindowCtrlBtn("✕", (s, e) => Close()));

            Grid.SetColumn(rightActions, 2);
            topGrid.Children.Add(rightActions);
            topBar.Child = topGrid;
            Grid.SetRow(topBar, 0);
            mainGrid.Children.Add(topBar);

            // =================================================================
            // ROW 1: Main Scrollable Content Host
            // =================================================================
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(24, 20, 24, 20)
            };
            _pageHost = new ContentControl();
            scrollViewer.Content = _pageHost;

            // Pre-build all 5 tab views
            _tabViews["home"] = BuildHomeTab();
            _tabViews["fastflags"] = BuildFastFlagsTab();
            _tabViews["mods"] = BuildModsTab();
            _tabViews["server"] = BuildServerTab();
            _tabViews["system"] = BuildSystemTab();

            Grid.SetRow(scrollViewer, 1);
            mainGrid.Children.Add(scrollViewer);

            // =================================================================
            // ROW 2: Bottom Status Bar
            // =================================================================
            var footerBar = new Border
            {
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(22, 0, 22, 0)
            };
            footerBar.SetResourceReference(Border.BackgroundProperty, "BgHeaderBrush");
            footerBar.SetResourceReference(Border.BorderBrushProperty, "BorderSubtleBrush");

            var footerGrid = new Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _statusFooterText = NRBloxTheme.CreateText(
                string.Format("Pronto • Alvo: {0} • {1} FastFlags sincronizadas", NRBloxPaths.TargetVersionGuid, _fastFlags.Flags.Count),
                11.5, FontWeights.Medium, "TextSecondaryBrush"
            );
            _statusFooterText.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(_statusFooterText, 0);
            footerGrid.Children.Add(_statusFooterText);

            var wikiLinkBtn = new TextBlock
            {
                Text = "📖 Documentação & Wiki Oficial (GitHub)  |  💬 Comunidade Discord",
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            wikiLinkBtn.SetResourceReference(TextBlock.ForegroundProperty, "AccentPrimaryBrush");
            wikiLinkBtn.MouseLeftButtonUp += (s, e) => OpenUrl(NRBloxPaths.GitHubWikiUrl);
            Grid.SetColumn(wikiLinkBtn, 1);
            footerGrid.Children.Add(wikiLinkBtn);

            footerBar.Child = footerGrid;
            Grid.SetRow(footerBar, 2);
            mainGrid.Children.Add(footerBar);

            outerShell.Child = mainGrid;
            Content = outerShell;
        }

        private Button CreateNavPill(string tabKey, string label)
        {
            var btn = NRBloxTheme.CreateStyledButton(label, false, (s, e) => SwitchTab(tabKey), new Thickness(3, 0, 3, 0));
            btn.Padding = new Thickness(14, 7, 14, 7);
            _navButtons[tabKey] = btn;
            return btn;
        }

        private Button CreateWindowCtrlBtn(string symbol, RoutedEventHandler onClick)
        {
            var btn = NRBloxTheme.CreateStyledButton(symbol, false, onClick, new Thickness(3, 0, 0, 0));
            btn.Padding = new Thickness(10, 5, 10, 5);
            return btn;
        }

        private void SwitchTab(string tabKey)
        {
            if (!_tabViews.ContainsKey(tabKey)) return;
            _pageHost.Content = _tabViews[tabKey];

            foreach (var kv in _navButtons)
            {
                kv.Value.Opacity = (kv.Key == tabKey) ? 1.0 : 0.68;
                kv.Value.FontWeight = (kv.Key == tabKey) ? FontWeights.Bold : FontWeights.Medium;
            }
        }

        private void ToggleTheme()
        {
            bool nextDark = !NRBloxTheme.IsDark;
            _settings.ThemeMode = nextDark ? "Dark" : "Light";
            NRBloxTheme.ApplyTheme(Resources, nextDark);
            _themeToggleBtn.Content = nextDark ? "☀️ Modo Claro" : "🌙 Modo Escuro";
            SaveAllSettings("Tema alterado para " + (nextDark ? "Modo Escuro" : "Modo Claro"));
        }

        private void SaveAllSettings(string statusMsg = null)
        {
            JsonHelper.Save(NRBloxPaths.SettingsFile, _settings);
            _fastFlags.ApplySettingsToFlags();
            _fastFlags.SaveAndSync();
            _mods.ApplyAndSyncMods();

            if (_flagCountMetric != null)
                _flagCountMetric.Text = _fastFlags.Flags.Count.ToString();
            if (_fpsMetric != null)
                _fpsMetric.Text = _settings.TargetFps > 0 ? (_settings.TargetFps + " FPS") : "Padrão";
            if (_statusFooterText != null && !string.IsNullOrEmpty(statusMsg))
                _statusFooterText.Text = statusMsg + " • Sincronizado com " + NRBloxPaths.TargetVersionGuid;
        }

        // =====================================================================
        // TAB 1: INÍCIO (Bento Dashboard & Launchpad)
        // =====================================================================
        private UIElement BuildHomeTab()
        {
            var root = new StackPanel();

            // Hero Launch Card
            var heroCard = NRBloxTheme.CreateBentoCard(new Thickness(0, 0, 0, 16), new Thickness(24, 22, 24, 22));
            heroCard.SetResourceReference(Border.BorderBrushProperty, "BorderAccentBrush");

            var heroGrid = new Grid();
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var heroLeft = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            heroLeft.Children.Add(NRBloxTheme.CreateText("Central de Controle NRBlox (Windows)", 22, FontWeights.ExtraBold, "TextPrimaryBrush"));
            heroLeft.Children.Add(NRBloxTheme.CreateText(
                "Launcher nativo em WPF para Windows com controle total de FastFlags, Mods, Multi-Instância, Geolocalização de Servidores e Discord Rich Presence.",
                13, FontWeights.Normal, "TextSecondaryBrush", new Thickness(0, 6, 0, 14)
            ));

            var heroBtns = new StackPanel { Orientation = Orientation.Horizontal };
            heroBtns.Children.Add(NRBloxTheme.CreateStyledButton("▶ Iniciar Roblox Player", true, (s, e) => LaunchRoblox(false, null), new Thickness(0, 0, 10, 0)));
            heroBtns.Children.Add(NRBloxTheme.CreateStyledButton("🛠️ Iniciar Roblox Studio", false, (s, e) => LaunchRoblox(true, null), new Thickness(0, 0, 10, 0)));
            heroBtns.Children.Add(NRBloxTheme.CreateStyledButton("💬 Comunidade Discord", false, (s, e) => OpenUrl(NRBloxPaths.DiscordUrl), new Thickness(0, 0, 10, 0)));
            heroBtns.Children.Add(NRBloxTheme.CreateStyledButton("📖 Abrir Wiki", false, (s, e) => OpenUrl(NRBloxPaths.GitHubWikiUrl)));
            heroLeft.Children.Add(heroBtns);

            Grid.SetColumn(heroLeft, 0);
            heroGrid.Children.Add(heroLeft);
            heroCard.Child = heroGrid;
            root.Children.Add(heroCard);

            // 3 Metric Bento Cards
            var metricsGrid = new UniformGrid { Columns = 3, Margin = new Thickness(0, 0, 0, 16) };

            var cardFps = NRBloxTheme.CreateBentoCard(new Thickness(0, 0, 8, 0), new Thickness(18, 16, 18, 16));
            var stackFps = new StackPanel();
            stackFps.Children.Add(NRBloxTheme.CreateText("TAXA DE QUADROS ALVO", 11, FontWeights.Bold, "TextSecondaryBrush"));
            _fpsMetric = NRBloxTheme.CreateText(_settings.TargetFps > 0 ? (_settings.TargetFps + " FPS") : "Padrão", 24, FontWeights.ExtraBold, "AccentPrimaryBrush", new Thickness(0, 4, 0, 2));
            stackFps.Children.Add(_fpsMetric);
            stackFps.Children.Add(NRBloxTheme.CreateText("API: " + _settings.RenderingMode + " • MSAA: " + _settings.MsaaMode + "x", 12, FontWeights.Normal, "TextSecondaryBrush"));
            cardFps.Child = stackFps;
            metricsGrid.Children.Add(cardFps);

            var cardFlags = NRBloxTheme.CreateBentoCard(new Thickness(4, 0, 4, 0), new Thickness(18, 16, 18, 16));
            var stackFlags = new StackPanel();
            stackFlags.Children.Add(NRBloxTheme.CreateText("FASTFLAGS ATIVAS", 11, FontWeights.Bold, "TextSecondaryBrush"));
            _flagCountMetric = NRBloxTheme.CreateText(_fastFlags.Flags.Count.ToString(), 24, FontWeights.ExtraBold, "SuccessBrush", new Thickness(0, 4, 0, 2));
            stackFlags.Children.Add(_flagCountMetric);
            stackFlags.Children.Add(NRBloxTheme.CreateText("Sincronizadas em ClientAppSettings.json", 12, FontWeights.Normal, "TextSecondaryBrush"));
            cardFlags.Child = stackFlags;
            metricsGrid.Children.Add(cardFlags);

            var cardVer = NRBloxTheme.CreateBentoCard(new Thickness(8, 0, 0, 0), new Thickness(18, 16, 18, 16));
            var stackVer = new StackPanel();
            stackVer.Children.Add(NRBloxTheme.CreateText("CLIENTE MONITORADO", 11, FontWeights.Bold, "TextSecondaryBrush"));
            stackVer.Children.Add(NRBloxTheme.CreateText(NRBloxPaths.TargetVersionGuid, 17, FontWeights.ExtraBold, "TextPrimaryBrush", new Thickness(0, 6, 0, 4)));
            stackVer.Children.Add(NRBloxTheme.CreateText("Canal: " + _settings.Channel, 12, FontWeights.Normal, "TextSecondaryBrush"));
            cardVer.Child = stackVer;
            metricsGrid.Children.Add(cardVer);

            root.Children.Add(metricsGrid);

            // Quick Behaviour Settings
            root.Children.Add(NRBloxTheme.CreateText("Configurações Rápidas do Bootstrapper", 16, FontWeights.Bold, "TextPrimaryBrush", new Thickness(0, 4, 0, 10)));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Permitir Múltiplas Instâncias (Multi-Instance Launching)",
                "Mantém o mutex ROBLOX_singletonMutex ativo para permitir abrir várias contas/janelas do Roblox simultaneamente no Windows.",
                NRBloxTheme.CreateToggleSwitch(_settings.MultiInstanceLaunching, val =>
                {
                    _settings.MultiInstanceLaunching = val;
                    SystemIntegrationService.SetMultiInstanceMutex(val);
                    SaveAllSettings("Multi-Instância " + (val ? "ativada" : "desativada"));
                })
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Estilo Visual da Janela de Inicialização (Bootstrapper)",
                "Define o visual da janela exibida ao iniciar o Roblox pelo navegador ou atalho.",
                NRBloxTheme.CreateStyledComboBox(
                    new string[] { "NRBlox Cyber Glass", "Minimal Pulse", "Neon Void", "Classic Retro" },
                    _settings.BootstrapperStyle,
                    val => { _settings.BootstrapperStyle = val; SaveAllSettings("Estilo do Bootstrapper atualizado"); }
                )
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Canal de Distribuição do Cliente (Deployment Channel)",
                "Selecione o canal oficial do cliente Roblox monitorado pelo NRBlox.",
                NRBloxTheme.CreateStyledComboBox(
                    new string[] { "production", "zflag", "zintegration", "zcanary" },
                    _settings.Channel,
                    val => { _settings.Channel = val; SaveAllSettings("Canal atualizado para " + val); }
                )
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Travar Versão Local (version-11f155f3450f4c8e)",
                "Preserva as customizações e impede que o lançador padrão sobrescreva os arquivos locais.",
                NRBloxTheme.CreateToggleSwitch(_settings.LockRobloxVersion, val =>
                {
                    _settings.LockRobloxVersion = val;
                    SaveAllSettings("Trava de versão atualizada");
                })
            ));

            return root;
        }

        // =====================================================================
        // TAB 2: FASTFLAGS & ENGINE STUDIO
        // =====================================================================
        private UIElement BuildFastFlagsTab()
        {
            var root = new StackPanel();

            root.Children.Add(NRBloxTheme.CreateText("Motor Gráfico & Presets de FastFlags", 18, FontWeights.ExtraBold, "TextPrimaryBrush", new Thickness(0, 0, 0, 4)));
            root.Children.Add(NRBloxTheme.CreateText(
                "Todas as alterações abaixo são gravadas instantaneamente em ClientSettings\\ClientAppSettings.json na pasta da versão ativa.",
                12.5, FontWeights.Normal, "TextSecondaryBrush", new Thickness(0, 0, 0, 14)
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Gerenciador de FastFlags Ativo",
                "Habilita a injeção automática do arquivo ClientAppSettings.json no cliente Roblox.",
                NRBloxTheme.CreateToggleSwitch(_settings.UseFastFlagManager, val =>
                {
                    _settings.UseFastFlagManager = val;
                    SaveAllSettings("Gerenciador de FastFlags " + (val ? "ativado" : "desativado"));
                    RefreshFlagListUi();
                })
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Desbloqueador de FPS (DFIntTaskSchedulerTargetFps)",
                "Define o limite máximo de quadros por segundo do agendador de tarefas do motor.",
                NRBloxTheme.CreateStyledComboBox(
                    new string[] { "60", "120", "144", "165", "240", "360", "999", "0" },
                    _settings.TargetFps.ToString(),
                    val =>
                    {
                        int parsed;
                        if (int.TryParse(val, out parsed)) _settings.TargetFps = parsed;
                        SaveAllSettings("Limite de FPS ajustado para " + val);
                        RefreshFlagListUi();
                    }
                )
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "API de Renderização Gráfica",
                "Escolhe o backend gráfico utilizado pelo RobloxPlayerBeta.exe no Windows.",
                NRBloxTheme.CreateStyledComboBox(
                    new string[] { "D3D11", "D3D10", "Vulkan", "OpenGL", "Automático" },
                    _settings.RenderingMode,
                    val =>
                    {
                        _settings.RenderingMode = val;
                        SaveAllSettings("API de renderização alterada para " + val);
                        RefreshFlagListUi();
                    }
                )
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Tecnologia de Iluminação Forçada",
                "Substitui o modo de iluminação do cenário (Voxel Fase 1, ShadowMap Fase 2 ou Future Is Bright Fase 3).",
                NRBloxTheme.CreateStyledComboBox(
                    new string[] { "Padrão", "Voxel", "ShadowMap", "Future" },
                    _settings.LightingMode,
                    val =>
                    {
                        _settings.LightingMode = val;
                        SaveAllSettings("Tecnologia de iluminação alterada para " + val);
                        RefreshFlagListUi();
                    }
                )
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Suavização de Serrilhado (MSAA)",
                "Força o nível de Multisample Anti-Aliasing (FIntDebugForceMSAASamples).",
                NRBloxTheme.CreateStyledComboBox(
                    new string[] { "Automático", "1", "2", "4", "8" },
                    _settings.MsaaMode,
                    val =>
                    {
                        _settings.MsaaMode = val;
                        SaveAllSettings("MSAA ajustado para " + val);
                        RefreshFlagListUi();
                    }
                )
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Qualidade de Texturas (DFIntTextureQualityOverride)",
                "Força a resolução de texturas (3 = Ultra, 2 = Alta, 1 = Média, 0 = Baixa).",
                NRBloxTheme.CreateStyledComboBox(
                    new string[] { "Automático", "0", "1", "2", "3" },
                    _settings.TextureQuality,
                    val =>
                    {
                        _settings.TextureQuality = val;
                        SaveAllSettings("Qualidade de textura ajustada para " + val);
                        RefreshFlagListUi();
                    }
                )
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Desativar Escalonamento de DPI do Windows (DFFlagDisableDPIScale)",
                "Evita que o cliente fique borrado em monitores com escala de 125% ou 150%.",
                NRBloxTheme.CreateToggleSwitch(_settings.DisableDpiScale, val =>
                {
                    _settings.DisableDpiScale = val;
                    SaveAllSettings("Escalonamento de DPI atualizado");
                    RefreshFlagListUi();
                })
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Tela Cheia Exclusiva com Alt+Enter (FFlagHandleAltEnterFullscreenManually)",
                "Melhora a latência de quadros ao alternar para tela cheia.",
                NRBloxTheme.CreateToggleSwitch(_settings.ManualFullscreen, val =>
                {
                    _settings.ManualFullscreen = val;
                    SaveAllSettings("Modo Alt+Enter atualizado");
                    RefreshFlagListUi();
                })
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Bloquear Telemetria e Contadores Efêmeros",
                "Desativa o envio de métricas internas de telemetria do cliente.",
                NRBloxTheme.CreateToggleSwitch(_settings.DisableTelemetry, val =>
                {
                    _settings.DisableTelemetry = val;
                    SaveAllSettings("Bloqueio de telemetria atualizado");
                    RefreshFlagListUi();
                })
            ));

            // Custom FastFlag Add Bar + Raw JSON Editor
            var editorCard = NRBloxTheme.CreateBentoCard(new Thickness(0, 12, 0, 12), new Thickness(18, 16, 18, 16));
            var editorStack = new StackPanel();
            editorStack.Children.Add(NRBloxTheme.CreateText("Editor Avançado de FastFlags (ClientAppSettings.json)", 15, FontWeights.Bold, "TextPrimaryBrush", new Thickness(0, 0, 0, 10)));

            var addGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var flagNameBox = new TextBox
            {
                Padding = new Thickness(10, 7, 10, 7),
                Margin = new Thickness(0, 0, 8, 0),
                FontSize = 13,
                Text = "FFlagDebugDisplayFPS"
            };
            var flagValBox = new TextBox
            {
                Padding = new Thickness(10, 7, 10, 7),
                Margin = new Thickness(0, 0, 8, 0),
                FontSize = 13,
                Text = "True"
            };

            Grid.SetColumn(flagNameBox, 0);
            Grid.SetColumn(flagValBox, 1);
            addGrid.Children.Add(flagNameBox);
            addGrid.Children.Add(flagValBox);

            var addBtn = NRBloxTheme.CreateStyledButton("+ Adicionar / Atualizar Flag", true, (s, e) =>
            {
                string k = (flagNameBox.Text ?? "").Trim();
                string v = (flagValBox.Text ?? "").Trim();
                if (!string.IsNullOrEmpty(k))
                {
                    _fastFlags.SetOrRemove(k, v);
                    _fastFlags.SaveAndSync();
                    RefreshFlagListUi();
                    SaveAllSettings("FastFlag " + k + " gravada");
                }
            }, new Thickness(0, 0, 8, 0));
            Grid.SetColumn(addBtn, 2);
            addGrid.Children.Add(addBtn);

            var openJsonBtn = NRBloxTheme.CreateStyledButton("📄 Abrir ClientAppSettings.json", false, (s, e) =>
            {
                string file = Path.Combine(NRBloxPaths.GetRobloxVersionDir(), "ClientSettings", "ClientAppSettings.json");
                if (File.Exists(file))
                    Process.Start(new ProcessStartInfo { FileName = "notepad.exe", Arguments = "\"" + file + "\"", UseShellExecute = true });
            });
            Grid.SetColumn(openJsonBtn, 3);
            addGrid.Children.Add(openJsonBtn);

            editorStack.Children.Add(addGrid);

            _flagListPanel = new StackPanel();
            RefreshFlagListUi();
            editorStack.Children.Add(_flagListPanel);

            editorCard.Child = editorStack;
            root.Children.Add(editorCard);

            return root;
        }

        private void RefreshFlagListUi()
        {
            if (_flagListPanel == null) return;
            _flagListPanel.Children.Clear();

            foreach (var kv in new Dictionary<string, string>(_fastFlags.Flags))
            {
                string key = kv.Key;
                string val = kv.Value;

                var row = new Border
                {
                    CornerRadius = new CornerRadius(7),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(12, 7, 12, 7),
                    Margin = new Thickness(0, 0, 0, 6)
                };
                row.SetResourceReference(Border.BackgroundProperty, "BgCardAltBrush");
                row.SetResourceReference(Border.BorderBrushProperty, "BorderSubtleBrush");

                var g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var kText = NRBloxTheme.CreateText(key, 12.5, FontWeights.SemiBold, "TextPrimaryBrush");
                kText.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(kText, 0);
                g.Children.Add(kText);

                var vText = NRBloxTheme.CreateText(val, 12.5, FontWeights.Bold, "AccentPrimaryBrush", new Thickness(12, 0, 14, 0));
                vText.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(vText, 1);
                g.Children.Add(vText);

                var delBtn = NRBloxTheme.CreateStyledButton("Remover", false, (s, e) =>
                {
                    _fastFlags.SetOrRemove(key, null);
                    _fastFlags.SaveAndSync();
                    RefreshFlagListUi();
                    if (_flagCountMetric != null) _flagCountMetric.Text = _fastFlags.Flags.Count.ToString();
                });
                delBtn.Padding = new Thickness(10, 3, 10, 3);
                Grid.SetColumn(delBtn, 2);
                g.Children.Add(delBtn);

                row.Child = g;
                _flagListPanel.Children.Add(row);
            }
        }

        // =====================================================================
        // TAB 3: MODS & VISUAIS
        // =====================================================================
        private UIElement BuildModsTab()
        {
            var root = new StackPanel();

            root.Children.Add(NRBloxTheme.CreateText("Mods & Customizações Visuais", 18, FontWeights.ExtraBold, "TextPrimaryBrush", new Thickness(0, 0, 0, 4)));
            root.Children.Add(NRBloxTheme.CreateText(
                "Aplique cursores clássicos, sons antigos, cenário clássico de avatar e fontes customizadas com backup automático (.nrblox_orig).",
                12.5, FontWeights.Normal, "TextSecondaryBrush", new Thickness(0, 0, 0, 14)
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Estilo do Cursor do Mouse",
                "Substitui as texturas ArrowCursor.png e ArrowFarCursor.png do cliente.",
                NRBloxTheme.CreateStyledComboBox(
                    new string[] { "Default", "From2006", "From2013" },
                    _settings.CursorType,
                    val =>
                    {
                        _settings.CursorType = val;
                        SaveAllSettings("Cursor alterado para " + val);
                    }
                )
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Sons Clássicos de Personagem (2013)",
                "Restaura os sons clássicos de passos, pulo e levantar na pasta content\\sounds.",
                NRBloxTheme.CreateToggleSwitch(_settings.UseOldCharacterSounds, val =>
                {
                    _settings.UseOldCharacterSounds = val;
                    SaveAllSettings("Sons clássicos " + (val ? "ativados" : "restaurados ao original"));
                })
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Cenário Clássico do Editor de Avatar (OldAvatarBackground.rbxl)",
                "Substitui o fundo escuro do editor de avatar pelo cenário clássico.",
                NRBloxTheme.CreateToggleSwitch(_settings.UseOldAvatarBackground, val =>
                {
                    _settings.UseOldAvatarBackground = val;
                    SaveAllSettings("Cenário do editor de avatar atualizado");
                })
            ));

            // Custom Font Picker Row
            var fontBtns = new StackPanel { Orientation = Orientation.Horizontal };
            fontBtns.Children.Add(NRBloxTheme.CreateStyledButton("Selecionar Fonte (.ttf/.otf)", false, (s, e) =>
            {
                var dlg = new OpenFileDialog
                {
                    Title = "Selecionar Fonte para o Cliente Roblox",
                    Filter = "Fontes TrueType/OpenType (*.ttf;*.otf)|*.ttf;*.otf"
                };
                if (dlg.ShowDialog() == true)
                {
                    _settings.CustomFontPath = dlg.FileName;
                    SaveAllSettings("Fonte customizada aplicada: " + Path.GetFileName(dlg.FileName));
                }
            }, new Thickness(0, 0, 8, 0)));

            fontBtns.Children.Add(NRBloxTheme.CreateStyledButton("Restaurar Padrão", false, (s, e) =>
            {
                _settings.CustomFontPath = "";
                SaveAllSettings("Fonte padrão restaurada");
            }));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Fonte Tipográfica Customizada",
                "Aplica uma fonte .ttf ou .otf personalizada em toda a interface do cliente.",
                fontBtns
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Pasta de Modificações Customizadas (data\\Modifications)",
                "Qualquer arquivo colocado nesta pasta espelha automaticamente a estrutura da versão do Roblox.",
                NRBloxTheme.CreateStyledButton("📂 Abrir Pasta de Mods", false, (s, e) =>
                {
                    Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = "\"" + NRBloxPaths.ModificationsDir + "\"", UseShellExecute = true });
                })
            ));

            return root;
        }

        // =====================================================================
        // TAB 4: SERVIDOR, ACTIVITY WATCHER & DISCORD RPC
        // =====================================================================
        private UIElement BuildServerTab()
        {
            var root = new StackPanel();

            root.Children.Add(NRBloxTheme.CreateText("Monitor de Servidor em Tempo Real & Discord RPC", 18, FontWeights.ExtraBold, "TextPrimaryBrush", new Thickness(0, 0, 0, 4)));
            root.Children.Add(NRBloxTheme.CreateText(
                "Inspeciona os logs do cliente em tempo real para identificar o IP UDMUX, geolocalização do servidor e atualizar seu status no Discord.",
                12.5, FontWeights.Normal, "TextSecondaryBrush", new Thickness(0, 0, 0, 14)
            ));

            var liveCard = NRBloxTheme.CreateBentoCard(new Thickness(0, 0, 0, 16), new Thickness(20, 18, 20, 18));
            liveCard.SetResourceReference(Border.BorderBrushProperty, "BorderAccentBrush");
            var liveStack = new StackPanel();
            liveStack.Children.Add(NRBloxTheme.CreateText("SESSÃO DE JOGO ATUAL", 11, FontWeights.Bold, "AccentPrimaryBrush"));
            _serverLocationLabel = NRBloxTheme.CreateText(_watcher.CurrentSession.Location, 20, FontWeights.ExtraBold, "TextPrimaryBrush", new Thickness(0, 4, 0, 4));
            _serverIpLabel = NRBloxTheme.CreateText("IP do Servidor (UDMUX): " + _watcher.CurrentSession.ServerIp, 13, FontWeights.Medium, "TextSecondaryBrush");
            _serverPlaceJobLabel = NRBloxTheme.CreateText(
                string.Format("PlaceId: {0}  •  JobId: {1}", _watcher.CurrentSession.PlaceId, _watcher.CurrentSession.JobId),
                12, FontWeights.Normal, "TextMutedBrush", new Thickness(0, 3, 0, 0)
            );
            liveStack.Children.Add(_serverLocationLabel);
            liveStack.Children.Add(_serverIpLabel);
            liveStack.Children.Add(_serverPlaceJobLabel);
            liveCard.Child = liveStack;
            root.Children.Add(liveCard);

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Monitoramento de Logs e Geolocalização de Servidor",
                "Identifica automaticamente a cidade/país do servidor do Roblox ao entrar em uma partida.",
                NRBloxTheme.CreateToggleSwitch(_settings.EnableActivityTracking, val =>
                {
                    _settings.EnableActivityTracking = val;
                    SaveAllSettings("Monitoramento de atividade " + (val ? "ativado" : "desativado"));
                })
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Discord Rich Presence Nativo (IPC Named Pipe)",
                "Exibe o jogo atual e o tempo de sessão diretamente no seu perfil do Discord sem precisar de plugins externos.",
                NRBloxTheme.CreateToggleSwitch(_settings.EnableDiscordRichPresence, val =>
                {
                    _settings.EnableDiscordRichPresence = val;
                    if (!val) _discord.ClearPresence();
                    SaveAllSettings("Discord Rich Presence " + (val ? "ativado" : "desativado"));
                })
            ));

            // Server History Card
            var histCard = NRBloxTheme.CreateBentoCard(new Thickness(0, 10, 0, 12), new Thickness(18, 16, 18, 16));
            var histStack = new StackPanel();
            histStack.Children.Add(NRBloxTheme.CreateText("Histórico de Servidores Recentes (Reconexão Rápida)", 15, FontWeights.Bold, "TextPrimaryBrush", new Thickness(0, 0, 0, 10)));
            _historyListPanel = new StackPanel();
            RefreshHistoryUi();
            histStack.Children.Add(_historyListPanel);
            histCard.Child = histStack;
            root.Children.Add(histCard);

            // Custom Integrations Card
            var intCard = NRBloxTheme.CreateBentoCard(new Thickness(0, 4, 0, 12), new Thickness(18, 16, 18, 16));
            var intStack = new StackPanel();
            var intHeader = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            intHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            intHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            intHeader.Children.Add(NRBloxTheme.CreateText("Integrações Externas (Auto-Iniciar junto com o Roblox)", 15, FontWeights.Bold, "TextPrimaryBrush"));
            var addExeBtn = NRBloxTheme.CreateStyledButton("+ Adicionar Programa (.exe)", false, (s, e) =>
            {
                var dlg = new OpenFileDialog { Title = "Adicionar Programa para Iniciar com o Roblox", Filter = "Executáveis (*.exe)|*.exe" };
                if (dlg.ShowDialog() == true)
                {
                    if (_settings.CustomIntegrations == null) _settings.CustomIntegrations = new List<string>();
                    if (!_settings.CustomIntegrations.Contains(dlg.FileName))
                    {
                        _settings.CustomIntegrations.Add(dlg.FileName);
                        SaveAllSettings("Integração adicionada");
                        RefreshCustomIntegrationsUi();
                    }
                }
            });
            Grid.SetColumn(addExeBtn, 1);
            intHeader.Children.Add(addExeBtn);
            intStack.Children.Add(intHeader);

            _customIntegrationsPanel = new StackPanel();
            RefreshCustomIntegrationsUi();
            intStack.Children.Add(_customIntegrationsPanel);
            intCard.Child = intStack;
            root.Children.Add(intCard);

            return root;
        }

        private void OnServerSessionUpdated(ServerSessionInfo info)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_serverLocationLabel != null) _serverLocationLabel.Text = info.Location;
                if (_serverIpLabel != null) _serverIpLabel.Text = "IP do Servidor (UDMUX): " + info.ServerIp + ":" + info.ServerPort;
                if (_serverPlaceJobLabel != null) _serverPlaceJobLabel.Text = string.Format("PlaceId: {0}  •  JobId: {1}", info.PlaceId, info.JobId);
                RefreshHistoryUi();
            }));
        }

        private void RefreshHistoryUi()
        {
            if (_historyListPanel == null) return;
            _historyListPanel.Children.Clear();

            if (_watcher.History == null || _watcher.History.Count == 0)
            {
                _historyListPanel.Children.Add(NRBloxTheme.CreateText("Nenhum servidor registrado ainda. Entre em um jogo para gravar o histórico de reconexão.", 12.5, FontWeights.Normal, "TextSecondaryBrush"));
                return;
            }

            foreach (var item in _watcher.History)
            {
                var row = new Border
                {
                    CornerRadius = new CornerRadius(7),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 6)
                };
                row.SetResourceReference(Border.BackgroundProperty, "BgCardAltBrush");
                row.SetResourceReference(Border.BorderBrushProperty, "BorderSubtleBrush");

                var g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var txt = NRBloxTheme.CreateText(
                    string.Format("Place {0} • {1} ({2}) • {3}", item.PlaceId, item.Location, item.ServerIp, item.JoinedTime),
                    12.5, FontWeights.SemiBold, "TextPrimaryBrush"
                );
                txt.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(txt, 0);
                g.Children.Add(txt);

                string uri = string.Format("roblox://experiences/start?placeId={0}&gameInstanceId={1}", item.PlaceId, item.JobId);
                var rejoinBtn = NRBloxTheme.CreateStyledButton("Reentrar", true, (s, e) => LaunchRoblox(false, uri));
                rejoinBtn.Padding = new Thickness(12, 4, 12, 4);
                Grid.SetColumn(rejoinBtn, 1);
                g.Children.Add(rejoinBtn);

                row.Child = g;
                _historyListPanel.Children.Add(row);
            }
        }

        private void RefreshCustomIntegrationsUi()
        {
            if (_customIntegrationsPanel == null) return;
            _customIntegrationsPanel.Children.Clear();

            if (_settings.CustomIntegrations == null || _settings.CustomIntegrations.Count == 0)
            {
                _customIntegrationsPanel.Children.Add(NRBloxTheme.CreateText("Nenhum aplicativo externo configurado.", 12.5, FontWeights.Normal, "TextSecondaryBrush"));
                return;
            }

            foreach (string exePath in new List<string>(_settings.CustomIntegrations))
            {
                var row = new Border
                {
                    CornerRadius = new CornerRadius(7),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(12, 7, 12, 7),
                    Margin = new Thickness(0, 0, 0, 6)
                };
                row.SetResourceReference(Border.BackgroundProperty, "BgCardAltBrush");
                row.SetResourceReference(Border.BorderBrushProperty, "BorderSubtleBrush");

                var g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var t = NRBloxTheme.CreateText(exePath, 12.5, FontWeights.Medium, "TextPrimaryBrush");
                t.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(t, 0);
                g.Children.Add(t);

                var remBtn = NRBloxTheme.CreateStyledButton("Remover", false, (s, e) =>
                {
                    _settings.CustomIntegrations.Remove(exePath);
                    SaveAllSettings("Integração removida");
                    RefreshCustomIntegrationsUi();
                });
                remBtn.Padding = new Thickness(10, 3, 10, 3);
                Grid.SetColumn(remBtn, 1);
                g.Children.Add(remBtn);

                row.Child = g;
                _customIntegrationsPanel.Children.Add(row);
            }
        }

        // =====================================================================
        // TAB 5: SISTEMA, ATALHOS & SOBRE (Criador + Discord + Wiki)
        // =====================================================================
        private UIElement BuildSystemTab()
        {
            var root = new StackPanel();

            root.Children.Add(NRBloxTheme.CreateText("Sistema, Integração Windows & Comunidade NRBlox", 18, FontWeights.ExtraBold, "TextPrimaryBrush", new Thickness(0, 0, 0, 12)));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Registrar Protocolos do Windows (roblox-player://, roblox://, nrblox://)",
                "Faz com que o botão 'Play' no site do Roblox inicie automaticamente através do NRBlox.",
                NRBloxTheme.CreateStyledButton("Registrar Protocolos", true, (s, e) =>
                {
                    SystemIntegrationService.RegisterWindowsProtocols();
                    MessageBox.Show("Protocolos roblox-player://, roblox:// e nrblox:// registrados com sucesso para o NRBlox!", "NRBlox", MessageBoxButton.OK, MessageBoxImage.Information);
                })
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Criar Atalhos na Área de Trabalho",
                "Cria os atalhos 'NRBlox' e 'Jogar Roblox (NRBlox)' no seu Desktop.",
                NRBloxTheme.CreateStyledButton("Criar Atalhos", false, (s, e) =>
                {
                    bool ok = SystemIntegrationService.CreateDesktopShortcuts();
                    MessageBox.Show(ok ? "Atalhos criados na Área de Trabalho com sucesso!" : "Não foi possível criar os atalhos.", "NRBlox", MessageBoxButton.OK, MessageBoxImage.Information);
                })
            ));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Verificar Integridade do Cliente (" + NRBloxPaths.TargetVersionGuid + ")",
                "Valida os binários do cliente, AppSettings.xml e ClientAppSettings.json.",
                NRBloxTheme.CreateStyledButton("Executar Diagnóstico", false, (s, e) =>
                {
                    string rep = SystemIntegrationService.VerifyClientInstallation();
                    MessageBox.Show(rep, "Diagnóstico NRBlox", MessageBoxButton.OK, MessageBoxImage.Information);
                })
            ));

            var dirsBtns = new StackPanel { Orientation = Orientation.Horizontal };
            dirsBtns.Children.Add(NRBloxTheme.CreateStyledButton("Pasta da Versão", false, (s, e) => OpenFolder(NRBloxPaths.GetRobloxVersionDir()), new Thickness(0, 0, 8, 0)));
            dirsBtns.Children.Add(NRBloxTheme.CreateStyledButton("Pasta de Logs", false, (s, e) => OpenFolder(NRBloxPaths.GetRobloxLogsDir()), new Thickness(0, 0, 8, 0)));
            dirsBtns.Children.Add(NRBloxTheme.CreateStyledButton("Pasta NRBlox", false, (s, e) => OpenFolder(NRBloxPaths.BaseDir)));

            root.Children.Add(NRBloxTheme.CreateSettingRow(
                "Diretórios do Sistema",
                "Acesso rápido às pastas de instalação, logs e configurações do NRBlox.",
                dirsBtns
            ));

            // About / Official Community Card (Only Creator FaelSemW, Discord & GitHub Wiki)
            var aboutCard = NRBloxTheme.CreateBentoCard(new Thickness(0, 14, 0, 0), new Thickness(22, 20, 22, 20));
            aboutCard.SetResourceReference(Border.BorderBrushProperty, "BorderAccentBrush");

            var aboutStack = new StackPanel();
            aboutStack.Children.Add(NRBloxTheme.CreateText("⚡ NRBlox — Projeto 100% Open-Source", 18, FontWeights.ExtraBold, "TextPrimaryBrush"));
            aboutStack.Children.Add(NRBloxTheme.CreateText(
                "Desenvolvido por FaelSemW • Arquitetura dedicada para Windows (WPF Nativo) e Linux (GTK/Tkinter + Sober/Vinegar/Wine).",
                13, FontWeights.Medium, "TextSecondaryBrush", new Thickness(0, 6, 0, 14)
            ));

            var communityBtns = new StackPanel { Orientation = Orientation.Horizontal };
            communityBtns.Children.Add(NRBloxTheme.CreateStyledButton("💬 Entrar na Comunidade Discord Oficial", true, (s, e) => OpenUrl(NRBloxPaths.DiscordUrl), new Thickness(0, 0, 10, 0)));
            communityBtns.Children.Add(NRBloxTheme.CreateStyledButton("📖 Acessar Wiki Completa no GitHub", false, (s, e) => OpenUrl(NRBloxPaths.GitHubWikiUrl), new Thickness(0, 0, 10, 0)));
            communityBtns.Children.Add(NRBloxTheme.CreateStyledButton("⭐ Repositório GitHub", false, (s, e) => OpenUrl(NRBloxPaths.GitHubRepoUrl)));
            aboutStack.Children.Add(communityBtns);

            aboutCard.Child = aboutStack;
            root.Children.Add(aboutCard);

            return root;
        }

        private void LaunchRoblox(bool isStudio, string launchUri)
        {
            var dlg = new NRBloxBootstrapperWindow(_settings, _fastFlags, _mods, launchUri, isStudio);
            dlg.ShowDialog();
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

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _watcher.Stop();
            _discord.Dispose();
        }
    }
}
