// ============================================================================
// NRBlox (Windows Native Edition) — Safe Native WPF Bootstrapper Window
// Author: FaelSemW (https://github.com/FaelSemW/NRBlox)
// Community Discord: https://discord.gg/M8j8JnQSCx
// License: MIT (100% Open Source)
// ============================================================================

using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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
        private readonly bool _isPreviewOnly;

        private TextBlock _statusText;
        private TextBlock _subStatusText;
        private Border _progressFill;
        private Border _progressTrack;
        private DispatcherTimer _stepTimer;
        private int _stepIndex;
        private bool _cancelled;

        public NRBloxBootstrapperWindow(NRBloxSettings settings, FastFlagEngine fastFlags, ModEngine mods, string launchArgs, bool isStudio, bool isPreviewOnly = false)
        {
            _settings = settings;
            _fastFlags = fastFlags;
            _mods = mods;
            _launchArgs = launchArgs;
            _isStudio = isStudio;
            _isPreviewOnly = isPreviewOnly;

            Title = string.IsNullOrEmpty(_settings.BootstrapperTitle) ? "NRBlox" : _settings.BootstrapperTitle;
            Width = 480;
            Height = 240;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.SingleBorderWindow;
            AllowsTransparency = false;
            ResizeMode = ResizeMode.NoResize;

            NRBloxTheme.ApplyTheme(Resources, _settings.ThemeMode);
            SetResourceReference(Window.BackgroundProperty, "BgWindowBrush");

            BuildLayout();
            Loaded += OnWindowLoaded;
        }

        private void BuildLayout()
        {
            var rootGrid = new Grid { Margin = new Thickness(24, 20, 24, 18) };
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Row 0: Brand Header
            var brandStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var logo = NRBloxTheme.CreateNRBloxLogo(32);
            logo.Margin = new Thickness(0, 0, 14, 0);
            brandStack.Children.Add(logo);

            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            titleStack.Children.Add(NRBloxTheme.CreateText(Title, 17, FontWeights.SemiBold, "TextPrimaryBrush"));
            titleStack.Children.Add(NRBloxTheme.CreateText("Style: " + (_settings.BootstrapperStyle ?? "NRBlox"), 11.5, FontWeights.Normal, "TextSecondaryBrush"));
            brandStack.Children.Add(titleStack);

            Grid.SetRow(brandStack, 0);
            rootGrid.Children.Add(brandStack);

            // Row 1: Status
            var statusStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 12, 0, 10) };
            _statusText = NRBloxTheme.CreateText(_isPreviewOnly ? "Bootstrapper Preview Mode" : "Starting Roblox...", 14, FontWeights.Normal, "TextPrimaryBrush");
            _subStatusText = NRBloxTheme.CreateText("Applying FastFlags and client modifications...", 12, FontWeights.Normal, "TextSecondaryBrush", new Thickness(0, 3, 0, 0));
            statusStack.Children.Add(_statusText);
            statusStack.Children.Add(_subStatusText);
            Grid.SetRow(statusStack, 1);
            rootGrid.Children.Add(statusStack);

            // Row 2: Progress Bar
            _progressTrack = new Border
            {
                Height = 6,
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 0, 0, 14),
                ClipToBounds = true
            };
            _progressTrack.SetResourceReference(Border.BackgroundProperty, "BorderSubtleBrush");

            _progressFill = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = _isPreviewOnly ? 220 : 40,
                Height = 6,
                CornerRadius = new CornerRadius(3)
            };
            _progressFill.SetResourceReference(Border.BackgroundProperty, "AccentPrimaryBrush");
            _progressTrack.Child = _progressFill;

            Grid.SetRow(_progressTrack, 2);
            rootGrid.Children.Add(_progressTrack);

            // Row 3: Cancel Button
            var footerGrid = new Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var verLabel = NRBloxTheme.CreateText(NRBloxPaths.TargetVersionGuid, 11.5, FontWeights.Normal, "TextSecondaryBrush");
            verLabel.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(verLabel, 0);
            footerGrid.Children.Add(verLabel);

            var cancelBtn = NRBloxTheme.CreateStyledButton("Cancel", false, (s, e) =>
            {
                _cancelled = true;
                if (_stepTimer != null) _stepTimer.Stop();
                Close();
            });
            Grid.SetColumn(cancelBtn, 1);
            footerGrid.Children.Add(cancelBtn);

            Grid.SetRow(footerGrid, 3);
            rootGrid.Children.Add(footerGrid);

            Content = rootGrid;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (_isPreviewOnly) return;

            _stepIndex = 0;
            _stepTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(220) };
            _stepTimer.Tick += OnStepTick;
            _stepTimer.Start();
        }

        private void SetProgress(double ratio, string status, string sub)
        {
            double maxW = _progressTrack.ActualWidth > 0 ? _progressTrack.ActualWidth : 410;
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
                SetProgress(0.30, "Verifying client installation...", "Checking " + NRBloxPaths.TargetVersionGuid);
                SystemIntegrationService.VerifyClientInstallation();
            }
            else if (_stepIndex == 2)
            {
                SetProgress(0.65, "Applying Engine Settings & FastFlags...", string.Format("Syncing {0} FastFlags to ClientAppSettings.json", _fastFlags.Flags.Count));
                _fastFlags.ApplySettingsToFlags();
                _fastFlags.SaveAndSync();
            }
            else if (_stepIndex == 3)
            {
                SetProgress(0.90, "Applying Mods & Integrations...", "Synchronizing client modifications...");
                _mods.ApplyAndSyncMods();
                if (_settings.MultiInstanceLaunching)
                    SystemIntegrationService.SetMultiInstanceMutex(true);
            }
            else if (_stepIndex >= 4)
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
                    "Could not find Roblox client executable at:\n" + targetExe,
                    "NRBlox",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            try
            {
                if (_settings.ConfirmLaunches)
                {
                    Process[] existing = Process.GetProcessesByName("RobloxPlayerBeta");
                    if (existing != null && existing.Length > 0)
                    {
                        var res = MessageBox.Show(
                            "Roblox is already running. Are you sure you want to launch another instance?",
                            "NRBlox",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question
                        );
                        if (res != MessageBoxResult.Yes) return;
                    }
                }

                var psi = new ProcessStartInfo
                {
                    FileName = targetExe,
                    Arguments = string.IsNullOrEmpty(_launchArgs) ? "--app" : _launchArgs,
                    WorkingDirectory = versionDir,
                    UseShellExecute = false
                };
                Process.Start(psi);

                // Launch custom integrations
                if (_settings.CustomIntegrationItems != null)
                {
                    foreach (var item in _settings.CustomIntegrationItems)
                    {
                        if (!string.IsNullOrEmpty(item.Location) && File.Exists(item.Location))
                        {
                            try
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = item.Location,
                                    Arguments = item.LaunchArgs ?? "",
                                    UseShellExecute = true
                                });
                            }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error launching Roblox:\n" + ex.Message, "NRBlox", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
