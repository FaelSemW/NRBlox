// ============================================================================
// NRBlox (Windows Native Edition) — Fluent Windows 11 Light/Dark Theme Engine
// Author: FaelSemW (https://github.com/FaelSemW/NRBlox)
// Community Discord: https://discord.gg/M8j8JnQSCx
// License: MIT (100% Open Source)
// ============================================================================

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NRBlox.Windows.UI
{
    public static class NRBloxTheme
    {
        public static bool IsDark { get; private set; }

        public static void ApplyTheme(ResourceDictionary resources, string themeSetting)
        {
            bool useDark = string.Equals(themeSetting, "Dark", StringComparison.OrdinalIgnoreCase);
            IsDark = useDark;

            if (useDark)
            {
                // Fluent Windows 11 Dark Mode Palette
                resources["BgWindowBrush"] = BrushFromHex("#1F1F1F");
                resources["BgSidebarBrush"] = BrushFromHex("#1F1F1F");
                resources["BgCardBrush"] = BrushFromHex("#2B2B2B");
                resources["BgCardHoverBrush"] = BrushFromHex("#323232");
                resources["BgInputBrush"] = BrushFromHex("#242424");
                resources["BorderSubtleBrush"] = BrushFromHex("#3A3A3A");
                resources["BorderStrongBrush"] = BrushFromHex("#505050");
                resources["TextPrimaryBrush"] = BrushFromHex("#FFFFFF");
                resources["TextSecondaryBrush"] = BrushFromHex("#A6A6A6");
                resources["TextMutedBrush"] = BrushFromHex("#7E7E7E");
                resources["AccentPrimaryBrush"] = BrushFromHex("#0067C0");
                resources["AccentLinkBrush"] = BrushFromHex("#4CC2FF");
                resources["AccentTextOnFillBrush"] = BrushFromHex("#FFFFFF");
                resources["NavSelectedBgBrush"] = BrushFromHex("#2D2D2D");
                resources["SwitchOffBgBrush"] = BrushFromHex("#2B2B2B");
                resources["SwitchOffBorderBrush"] = BrushFromHex("#8A8A8A");
                resources["SwitchOffThumbBrush"] = BrushFromHex("#C5C5C5");
            }
            else
            {
                // Fluent Windows 11 Light Mode Palette (Matches the 13 Screenshots)
                resources["BgWindowBrush"] = BrushFromHex("#F8F9FA");
                resources["BgSidebarBrush"] = BrushFromHex("#F8F9FA");
                resources["BgCardBrush"] = BrushFromHex("#FFFFFF");
                resources["BgCardHoverBrush"] = BrushFromHex("#F3F4F6");
                resources["BgInputBrush"] = BrushFromHex("#FFFFFF");
                resources["BorderSubtleBrush"] = BrushFromHex("#E3E5E8");
                resources["BorderStrongBrush"] = BrushFromHex("#C4C8CE");
                resources["TextPrimaryBrush"] = BrushFromHex("#1B1B1B");
                resources["TextSecondaryBrush"] = BrushFromHex("#6B6B6B");
                resources["TextMutedBrush"] = BrushFromHex("#8A8A8A");
                resources["AccentPrimaryBrush"] = BrushFromHex("#0067C0");
                resources["AccentLinkBrush"] = BrushFromHex("#0067C0");
                resources["AccentTextOnFillBrush"] = BrushFromHex("#FFFFFF");
                resources["NavSelectedBgBrush"] = BrushFromHex("#FFFFFF");
                resources["SwitchOffBgBrush"] = BrushFromHex("#FFFFFF");
                resources["SwitchOffBorderBrush"] = BrushFromHex("#757575");
                resources["SwitchOffThumbBrush"] = BrushFromHex("#5C5C5C");
            }
        }

        public static SolidColorBrush BrushFromHex(string hex)
        {
            Color c = (Color)ColorConverter.ConvertFromString(hex);
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        /// <summary>
        /// Draws the signature tilted 4-tile NRBlox geometric logo (cyan-to-indigo gradient).
        /// </summary>
        public static FrameworkElement CreateNRBloxLogo(double size)
        {
            var container = new Grid
            {
                Width = size,
                Height = size,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform(14)
            };

            container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(size * 0.10) });
            container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(size * 0.10) });
            container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var grad = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#00D2FF"),
                (Color)ColorConverter.ConvertFromString("#6366F1"),
                45.0
            );
            grad.Freeze();

            double r = Math.Max(2, size * 0.08);
            int[,] positions = new int[,] { { 0, 0 }, { 0, 2 }, { 2, 0 }, { 2, 2 } };
            for (int i = 0; i < 4; i++)
            {
                var tile = new Border
                {
                    Background = grad,
                    CornerRadius = new CornerRadius(r)
                };
                Grid.SetRow(tile, positions[i, 0]);
                Grid.SetColumn(tile, positions[i, 1]);
                container.Children.Add(tile);
            }

            return container;
        }

        public static Border CreateCard(Thickness margin, Thickness padding)
        {
            var card = new Border
            {
                CornerRadius = new CornerRadius(6),
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

        /// <summary>
        /// Creates a clickable Fluent Action Card with left icon, title, optional subtitle, and right chevron "›"
        /// (Matches Launch Roblox, Launch Studio, Configure Settings, Open Mods Folder, Help, Fast Flag Editor).
        /// </summary>
        public static Border CreateActionCard(string iconGlyph, string title, string subtitle, Action onClick, Thickness margin = default(Thickness))
        {
            var card = CreateCard(margin, new Thickness(16, 14, 16, 14));
            card.Cursor = Cursors.Hand;

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var iconTb = CreateText(iconGlyph, 17, FontWeights.Normal, "TextPrimaryBrush", new Thickness(0, 0, 14, 0));
            iconTb.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(iconTb, 0);
            grid.Children.Add(iconTb);

            var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            textStack.Children.Add(CreateText(title, 14, FontWeights.Normal, "TextPrimaryBrush"));
            if (!string.IsNullOrEmpty(subtitle))
            {
                textStack.Children.Add(CreateText(subtitle, 12, FontWeights.Normal, "TextSecondaryBrush", new Thickness(0, 2, 0, 0)));
            }
            Grid.SetColumn(textStack, 1);
            grid.Children.Add(textStack);

            var chevron = CreateText("›", 20, FontWeights.Light, "TextSecondaryBrush", new Thickness(10, -2, 0, 0));
            chevron.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(chevron, 2);
            grid.Children.Add(chevron);

            card.Child = grid;

            card.MouseEnter += (s, e) => card.SetResourceReference(Border.BackgroundProperty, "BgCardHoverBrush");
            card.MouseLeave += (s, e) => card.SetResourceReference(Border.BackgroundProperty, "BgCardBrush");
            card.MouseLeftButtonUp += (s, e) =>
            {
                if (onClick != null) onClick();
            };

            return card;
        }

        public static Button CreateStyledButton(string label, bool isPrimary, RoutedEventHandler onClick, Thickness margin = default(Thickness))
        {
            var btn = new Button
            {
                Content = label,
                Margin = margin,
                Padding = new Thickness(18, 7, 18, 7),
                FontSize = 13,
                FontWeight = isPrimary ? FontWeights.SemiBold : FontWeights.Normal,
                Cursor = Cursors.Hand,
                FontFamily = new FontFamily("Segoe UI")
            };

            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "BtnBorder";
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
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
                borderFactory.SetResourceReference(Border.BackgroundProperty, "BgCardBrush");
                borderFactory.SetResourceReference(Border.BorderBrushProperty, "BorderStrongBrush");
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

        /// <summary>
        /// Creates a Windows 11 Fluent pill toggle switch (blue #0067C0 when On, white/dark with border when Off).
        /// </summary>
        public static FrameworkElement CreateToggleSwitch(bool initialChecked, Action<bool> onChanged)
        {
            bool state = initialChecked;

            var track = new Border
            {
                Width = 42,
                Height = 21,
                CornerRadius = new CornerRadius(10.5),
                BorderThickness = new Thickness(1.2),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };

            var thumb = new Ellipse
            {
                Width = 13,
                Height = 13,
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
                    track.SetResourceReference(Border.BackgroundProperty, "SwitchOffBgBrush");
                    track.SetResourceReference(Border.BorderBrushProperty, "SwitchOffBorderBrush");
                    thumb.HorizontalAlignment = HorizontalAlignment.Left;
                    thumb.SetResourceReference(Shape.FillProperty, "SwitchOffThumbBrush");
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

        public static Border CreateSettingRow(string title, string subtitle, FrameworkElement rightControl, Thickness margin = default(Thickness))
        {
            if (margin == default(Thickness))
                margin = new Thickness(0, 0, 0, 10);

            var row = CreateCard(margin, new Thickness(16, 14, 16, 14));
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 16, 0) };
            stack.Children.Add(CreateText(title, 13.5, FontWeights.Normal, "TextPrimaryBrush"));
            if (!string.IsNullOrEmpty(subtitle))
            {
                stack.Children.Add(CreateText(subtitle, 12, FontWeights.Normal, "TextSecondaryBrush", new Thickness(0, 2, 0, 0)));
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

        public static Border CreateExpandableCard(string title, string subtitle, FrameworkElement headerRightElement, UIElement bodyContent, bool initiallyExpanded = true)
        {
            var outer = CreateCard(new Thickness(0, 0, 0, 10), new Thickness(0));
            var stack = new StackPanel();

            var headerBorder = new Border
            {
                Padding = new Thickness(16, 14, 16, 14),
                Cursor = Cursors.Hand,
                Background = Brushes.Transparent
            };

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            textStack.Children.Add(CreateText(title, 13.5, FontWeights.Normal, "TextPrimaryBrush"));
            if (!string.IsNullOrEmpty(subtitle))
                textStack.Children.Add(CreateText(subtitle, 12, FontWeights.Normal, "TextSecondaryBrush", new Thickness(0, 2, 0, 0)));

            Grid.SetColumn(textStack, 0);
            headerGrid.Children.Add(textStack);

            if (headerRightElement != null)
            {
                headerRightElement.VerticalAlignment = VerticalAlignment.Center;
                headerRightElement.Margin = new Thickness(8, 0, 12, 0);
                Grid.SetColumn(headerRightElement, 1);
                headerGrid.Children.Add(headerRightElement);
            }

            var arrowTb = CreateText(initiallyExpanded ? "∧" : "∨", 13, FontWeights.Bold, "TextSecondaryBrush");
            arrowTb.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(arrowTb, 2);
            headerGrid.Children.Add(arrowTb);

            headerBorder.Child = headerGrid;
            stack.Children.Add(headerBorder);

            var bodyContainer = new Border
            {
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(16, 14, 16, 14),
                Visibility = initiallyExpanded ? Visibility.Visible : Visibility.Collapsed
            };
            bodyContainer.SetResourceReference(Border.BorderBrushProperty, "BorderSubtleBrush");
            bodyContainer.Child = bodyContent;
            stack.Children.Add(bodyContainer);

            headerBorder.MouseLeftButtonUp += (s, e) =>
            {
                bool expand = bodyContainer.Visibility != Visibility.Visible;
                bodyContainer.Visibility = expand ? Visibility.Visible : Visibility.Collapsed;
                arrowTb.Text = expand ? "∧" : "∨";
            };

            outer.Child = stack;
            return outer;
        }

        public static ComboBox CreateStyledComboBox(string[] options, string currentValue, Action<string> onSelected)
        {
            var combo = new ComboBox
            {
                MinWidth = 185,
                Padding = new Thickness(10, 6, 10, 6),
                FontSize = 13,
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
