// ============================================================================
// NRBlox (Windows Native Edition) — Dynamic Dark/Light Theme & Control Factory
// Author: FaelSemW (https://github.com/FaelSemW/NRBlox)
// Community Discord: https://discord.gg/M8j8JnQSCx
// License: MIT (100% Open Source)
// ============================================================================

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace NRBlox.Windows.UI
{
    public static class NRBloxTheme
    {
        public static bool IsDark { get; private set; }

        public static void ApplyTheme(ResourceDictionary resources, bool isDark)
        {
            IsDark = isDark;
            if (isDark)
            {
                // Obsidian Cyber Dark Palette
                resources["BgWindowBrush"] = BrushFromHex("#0A0D14");
                resources["BgHeaderBrush"] = BrushFromHex("#0F1420");
                resources["BgCardBrush"] = BrushFromHex("#141B2D");
                resources["BgCardAltBrush"] = BrushFromHex("#192238");
                resources["BgInputBrush"] = BrushFromHex("#0D121F");
                resources["BorderSubtleBrush"] = BrushFromHex("#243049");
                resources["BorderAccentBrush"] = BrushFromHex("#00E5FF");
                resources["TextPrimaryBrush"] = BrushFromHex("#F8FAFC");
                resources["TextSecondaryBrush"] = BrushFromHex("#94A3B8");
                resources["TextMutedBrush"] = BrushFromHex("#64748B");
                resources["AccentPrimaryBrush"] = BrushFromHex("#00E5FF");
                resources["AccentSecondaryBrush"] = BrushFromHex("#6366F1");
                resources["AccentTextOnFillBrush"] = BrushFromHex("#060911");
                resources["SuccessBrush"] = BrushFromHex("#10B981");
                resources["DangerBrush"] = BrushFromHex("#EF4444");
                resources["NavPillActiveBgBrush"] = BrushFromHex("#1E293B");
            }
            else
            {
                // Alabaster Studio Light Palette
                resources["BgWindowBrush"] = BrushFromHex("#F1F5F9");
                resources["BgHeaderBrush"] = BrushFromHex("#FFFFFF");
                resources["BgCardBrush"] = BrushFromHex("#FFFFFF");
                resources["BgCardAltBrush"] = BrushFromHex("#F8FAFC");
                resources["BgInputBrush"] = BrushFromHex("#F1F5F9");
                resources["BorderSubtleBrush"] = BrushFromHex("#CBD5E1");
                resources["BorderAccentBrush"] = BrushFromHex("#4F46E5");
                resources["TextPrimaryBrush"] = BrushFromHex("#0F172A");
                resources["TextSecondaryBrush"] = BrushFromHex("#475569");
                resources["TextMutedBrush"] = BrushFromHex("#64748B");
                resources["AccentPrimaryBrush"] = BrushFromHex("#4F46E5");
                resources["AccentSecondaryBrush"] = BrushFromHex("#0284C7");
                resources["AccentTextOnFillBrush"] = BrushFromHex("#FFFFFF");
                resources["SuccessBrush"] = BrushFromHex("#059669");
                resources["DangerBrush"] = BrushFromHex("#DC2626");
                resources["NavPillActiveBgBrush"] = BrushFromHex("#E2E8F0");
            }
        }

        public static SolidColorBrush BrushFromHex(string hex)
        {
            Color c = (Color)ColorConverter.ConvertFromString(hex);
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        public static Border CreateBentoCard(Thickness margin, Thickness padding)
        {
            var card = new Border
            {
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(1),
                Margin = margin,
                Padding = padding
            };
            card.SetResourceReference(Border.BackgroundProperty, "BgCardBrush");
            card.SetResourceReference(Border.BorderBrushProperty, "BorderSubtleBrush");
            return card;
        }

        public static TextBlock CreateText(string text, double fontSize, FontWeight weight, string brushKey = "TextPrimaryBrush", Thickness margin = default(Thickness))
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = weight,
                Margin = margin,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Segoe UI")
            };
            tb.SetResourceReference(TextBlock.ForegroundProperty, brushKey);
            return tb;
        }

        public static Button CreateStyledButton(string label, bool isPrimary, RoutedEventHandler onClick, Thickness margin = default(Thickness))
        {
            var btn = new Button
            {
                Content = label,
                Margin = margin,
                Padding = new Thickness(16, 9, 16, 9),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                FontFamily = new FontFamily("Segoe UI")
            };

            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "BtnBorder";
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));

            if (isPrimary)
            {
                borderFactory.SetResourceReference(Border.BackgroundProperty, "AccentPrimaryBrush");
                borderFactory.SetResourceReference(Border.BorderBrushProperty, "AccentPrimaryBrush");
                btn.SetResourceReference(Control.ForegroundProperty, "AccentTextOnFillBrush");
            }
            else
            {
                borderFactory.SetResourceReference(Border.BackgroundProperty, "BgCardAltBrush");
                borderFactory.SetResourceReference(Border.BorderBrushProperty, "BorderSubtleBrush");
                btn.SetResourceReference(Control.ForegroundProperty, "TextPrimaryBrush");
            }

            var presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            presenterFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenterFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(presenterFactory);

            template.VisualTree = borderFactory;

            var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(UIElement.OpacityProperty, 0.88, "BtnBorder"));
            template.Triggers.Add(hoverTrigger);

            btn.Template = template;
            if (onClick != null)
                btn.Click += onClick;
            return btn;
        }

        public static FrameworkElement CreateToggleSwitch(bool initialChecked, Action<bool> onChanged)
        {
            bool state = initialChecked;

            var track = new Border
            {
                Width = 46,
                Height = 24,
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };

            var thumb = new Ellipse
            {
                Width = 16,
                Height = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(3, 0, 3, 0)
            };

            track.Child = thumb;

            Action applyVisualState = () =>
            {
                if (state)
                {
                    track.SetResourceReference(Border.BackgroundProperty, "AccentPrimaryBrush");
                    track.SetResourceReference(Border.BorderBrushProperty, "AccentPrimaryBrush");
                    thumb.HorizontalAlignment = HorizontalAlignment.Right;
                    thumb.SetResourceReference(Shape.FillProperty, "AccentTextOnFillBrush");
                }
                else
                {
                    track.SetResourceReference(Border.BackgroundProperty, "BgInputBrush");
                    track.SetResourceReference(Border.BorderBrushProperty, "BorderSubtleBrush");
                    thumb.HorizontalAlignment = HorizontalAlignment.Left;
                    thumb.SetResourceReference(Shape.FillProperty, "TextSecondaryBrush");
                }
            };

            applyVisualState();

            track.MouseLeftButtonUp += (s, e) =>
            {
                state = !state;
                applyVisualState();
                if (onChanged != null)
                    onChanged(state);
            };

            return track;
        }

        public static Border CreateSettingRow(string title, string subtitle, FrameworkElement rightControl)
        {
            var row = CreateBentoCard(new Thickness(0, 0, 0, 10), new Thickness(16, 13, 16, 13));
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 16, 0) };
            stack.Children.Add(CreateText(title, 14, FontWeights.SemiBold, "TextPrimaryBrush"));
            if (!string.IsNullOrEmpty(subtitle))
            {
                stack.Children.Add(CreateText(subtitle, 12, FontWeights.Normal, "TextSecondaryBrush", new Thickness(0, 3, 0, 0)));
            }

            Grid.SetColumn(stack, 0);
            grid.Children.Add(stack);

            if (rightControl != null)
            {
                rightControl.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(rightControl, 1);
                grid.Children.Add(rightControl);
            }

            row.Child = grid;
            return row;
        }

        public static ComboBox CreateStyledComboBox(string[] options, string currentValue, Action<string> onSelected)
        {
            var combo = new ComboBox
            {
                MinWidth = 175,
                Padding = new Thickness(10, 6, 10, 6),
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                Cursor = Cursors.Hand,
                FontFamily = new FontFamily("Segoe UI")
            };

            int selectedIdx = 0;
            for (int i = 0; i < options.Length; i++)
            {
                combo.Items.Add(options[i]);
                if (string.Equals(options[i], currentValue, StringComparison.OrdinalIgnoreCase))
                    selectedIdx = i;
            }
            combo.SelectedIndex = selectedIdx;

            if (onSelected != null)
            {
                combo.SelectionChanged += (s, e) =>
                {
                    if (combo.SelectedItem != null)
                        onSelected(combo.SelectedItem.ToString());
                };
            }
            return combo;
        }
    }
}
