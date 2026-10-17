// ============================================================================
// NRBlox (Windows Native Edition) — Native WPF Bootstrapper Launch Window
// Author: FaelSemW (https://github.com/FaelSemW/NRBlox)
// Community Discord: https://discord.gg/M8j8JnQSCx
// License: MIT (100% Open Source)
// ============================================================================

using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NRBlox.Windows.Core;

namespace NRBlox.Windows.UI
{
    public sealed class NRBloxBootstrapperWindow : Window
    {
        private readonly NRBloxSettings _settings;
        private readonly FastFlagEngine _fastFlags;
        private readonly ModEngine _mods;
        private readonly string _launchArgs;
        private readonly bool _isStudio;

        private TextBlock _statusText;
        private TextBlock _subStatusText;
        private Border _progressFill;
        private Border _progressTrack;
        private DispatcherTimer _stepTimer;
        private int _stepIndex;
        private bool _cancelled;

        public NRBloxBootstrapperWindow(NRBloxSettings settings, FastFlagEngine fastFlags, ModEngine mods, string launchArgs, bool isStudio)
        {
            _settings = settings;
            _fastFlags = fastFlags;
            _mods = mods;
            _launchArgs = launchArgs;
            _isStudio = isStudio;

            Title = "NRBlox Bootstrapper";
            Width = 500;
            Height = 255;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;

            bool isDark = !string.Equals(_settings.ThemeMode, "Light", StringComparison.OrdinalIgnoreCase);
            NRBloxTheme.ApplyTheme(Resources, isDark);

            BuildLayout();
            Loaded += OnWindowLoaded;
        }

        private void BuildLayout()
        {
            var outerBorder = new Border
            {
                CornerRadius = new CornerRadius(14),
                BorderThickness = new Thickness(1.5),
                Margin = new Thickness(10),
                Padding = new Thickness(26, 22, 26, 22)
            };
            outerBorder.SetResourceReference(Border.BackgroundProperty, "BgWindowBrush");
            outerBorder.SetResourceReference(Border.BorderBrushProperty, "BorderAccentBrush");
            outerBorder.MouseLeftButtonDown += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed) DragMove();
            };

            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Row 0: Brand Header + Badge
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var brandStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var logoBadge = new Border
            {
                Width = 34,
                Height = 34,
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 12, 0),
                Child = new TextBlock
                {
                    Text = "⚡",
                    FontSize = 18,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            logoBadge.SetResourceReference(Border.BackgroundProperty, "AccentPrimaryBrush");
            brandStack.Children.Add(logoBadge);

            var titleStack = new StackPanel();
            titleStack.Children.Add(NRBloxTheme.CreateText("NRBLOX ENGINE", 17, FontWeights.ExtraBold, "TextPrimaryBrush"));
            titleStack.Children.Add(NRBloxTheme.CreateText(_settings.BootstrapperStyle ?? "NRBlox Cyber Glass", 11, FontWeights.Medium, "AccentPrimaryBrush"));
            brandStack.Children.Add(titleStack);

            Grid.SetColumn(brandStack, 0);
            headerGrid.Children.Add(brandStack);

            var channelBadge = new Border
            {
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 4, 10, 4),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                Child = NRBloxTheme.CreateText("CANAL: " + (_settings.Channel ?? "production").ToUpperInvariant(), 10.5, FontWeights.Bold, "TextSecondaryBrush")
            };
            channelBadge.SetResourceReference(Border.BackgroundProperty, "BgCardBrush");
            channelBadge.SetResourceReference(Border.BorderBrushProperty, "BorderSubtleBrush");
            Grid.SetColumn(channelBadge, 1);
            headerGrid.Children.Add(channelBadge);

            Grid.SetRow(headerGrid, 0);
            rootGrid.Children.Add(headerGrid);

            // Row 1: Status texts
            var statusStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 16, 0, 12) };
            _statusText = NRBloxTheme.CreateText("Inicializando cliente Roblox...", 14.5, FontWeights.SemiBold, "TextPrimaryBrush");
            _subStatusText = NRBloxTheme.CreateText("Preparando FastFlags e verificando integridade da versão...", 12, FontWeights.Normal, "TextSecondaryBrush", new Thickness(0, 4, 0, 0));
            statusStack.Children.Add(_statusText);
            statusStack.Children.Add(_subStatusText);
            Grid.SetRow(statusStack, 1);
            rootGrid.Children.Add(statusStack);

            // Row 2: Custom Progress Bar
            _progressTrack = new Border
            {
                Height = 10,
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(0, 0, 0, 16),
                ClipToBounds = true
            };
            _progressTrack.SetResourceReference(Border.BackgroundProperty, "BgCardBrush");

            _progressFill = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 30,
                Height = 10,
                CornerRadius = new CornerRadius(5)
            };
            _progressFill.SetResourceReference(Border.BackgroundProperty, "AccentPrimaryBrush");
            _progressTrack.Child = _progressFill;

            Grid.SetRow(_progressTrack, 2);
            rootGrid.Children.Add(_progressTrack);

            // Row 3: Footer with Version & Cancel button
            var footerGrid = new Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var verLabel = NRBloxTheme.CreateText(NRBloxPaths.TargetVersionGuid, 11.5, FontWeights.Medium, "TextMutedBrush");
            verLabel.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(verLabel, 0);
            footerGrid.Children.Add(verLabel);

            var cancelBtn = NRBloxTheme.CreateStyledButton("Cancelar", false, (s, e) =>
            {
                _cancelled = true;
                if (_stepTimer != null) _stepTimer.Stop();
                Close();
            });
            cancelBtn.Padding = new Thickness(14, 5, 14, 5);
            Grid.SetColumn(cancelBtn, 1);
            footerGrid.Children.Add(cancelBtn);

            Grid.SetRow(footerGrid, 3);
            rootGrid.Children.Add(footerGrid);

            outerBorder.Child = rootGrid;
            Content = outerBorder;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            _stepIndex = 0;
            _stepTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(260) };
            _stepTimer.Tick += OnStepTick;
            _stepTimer.Start();
        }

        private void SetProgress(double ratio, string status, string sub)
        {
            double maxW = _progressTrack.ActualWidth > 0 ? _progressTrack.ActualWidth : 420;
            _progressFill.Width = Math.Max(16, maxW * Math.Min(1.0, Math.Max(0.0, ratio)));
            _statusText.Text = status;
            _subStatusText.Text = sub;
        }

        private void OnStepTick(object sender, EventArgs e)
        {
            if (_cancelled) return;
            _stepIndex++;

            if (_stepIndex == 1)
            {
                SetProgress(0.25, "Verificando instalação do Roblox...", "Validando AppSettings.xml e binários em " + NRBloxPaths.TargetVersionGuid);
                SystemIntegrationService.VerifyClientInstallation();
            }
            else if (_stepIndex == 2)
            {
                SetProgress(0.55, "Sincronizando FastFlags...", string.Format("Injetando {0} flags ativas em ClientAppSettings.json (FPS: {1})", _fastFlags.Flags.Count, _settings.TargetFps));
                _fastFlags.ApplySettingsToFlags();
                _fastFlags.SaveAndSync();
            }
            else if (_stepIndex == 3)
            {
                SetProgress(0.80, "Aplicando Mods e Integrações...", "Sincronizando cursores, sons e verificando Multi-Instance Mutex...");
                _mods.ApplyAndSyncMods();
                SystemIntegrationService.SetMultiInstanceMutex(_settings.MultiInstanceLaunching);
            }
            else if (_stepIndex == 4)
            {
                SetProgress(1.0, "Iniciando Roblox...", "Executando cliente nativo...");
            }
            else if (_stepIndex >= 5)
            {
                _stepTimer.Stop();
                ExecuteRobloxProcess();
                Close();
            }
        }

        private void ExecuteRobloxProcess()
        {
            string versionDir = NRBloxPaths.GetRobloxVersionDir();
            string exeName = _isStudio ? "RobloxStudioBeta.exe" : "RobloxPlayerBeta.exe";
            string targetExe = Path.Combine(versionDir, exeName);

            if (!File.Exists(targetExe))
            {
                MessageBox.Show(
                    "Não foi possível localizar o executável do cliente em:\n" + targetExe,
                    "NRBlox — Executável não encontrado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = targetExe,
                    Arguments = string.IsNullOrEmpty(_launchArgs) ? "--app" : _launchArgs,
                    WorkingDirectory = versionDir,
                    UseShellExecute = false
                };
                Process.Start(psi);

                // Launch any configured custom integrations
                if (_settings.CustomIntegrations != null)
                {
                    foreach (string customExe in _settings.CustomIntegrations)
                    {
                        if (!string.IsNullOrEmpty(customExe) && File.Exists(customExe))
                        {
                            try { Process.Start(new ProcessStartInfo { FileName = customExe, UseShellExecute = true }); }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao iniciar o Roblox:\n" + ex.Message, "NRBlox", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
