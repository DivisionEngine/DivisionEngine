//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DivisionEngine.MathLib;
using Material.Icons;
using Material.Icons.Avalonia;
using System;
using System.Linq;
using System.Reflection;

namespace DivisionEngine.Editor;

/// <summary>
/// Developer utility window showing ColorPalette colors and Material Icons.
/// </summary>
public partial class DeveloperWindow : EditorWindow
{
    private readonly TabControl? tabControl;
    private ScrollViewer? colorScrollViewer;
    private ScrollViewer? iconScrollViewer;
    private StackPanel? colorPanel;
    private WrapPanel? iconPanel;
    private TextBox? iconSearchBox;
    private TextBox? colorSearchBox;
    private ComboBox? iconSizeComboBox;
    private string iconSearchFilter = string.Empty;
    private string colorSearchFilter = string.Empty;
    private int currentIconSize = 24;

    public DeveloperWindow()
    {
        InitializeComponent();

        DockPanel mainPanel = new DockPanel();
        tabControl = new TabControl
        {
            Margin = new Thickness(4),
            Background = EditorColor.FromRGB(45, 45, 45)
        };

        // Colors Tab
        Grid colorsTab = CreateColorsTab();
        tabControl.Items.Add(new TabItem
        {
            Header = "🎨 Colors",
            Content = colorsTab
        });

        // Icons Tab
        Grid iconsTab = CreateIconsTab();
        tabControl.Items.Add(new TabItem
        {
            Header = "🔤 Material Icons",
            Content = iconsTab
        });

        mainPanel.Children.Add(tabControl);

        Border? border = this.FindControl<Border>("MainBorder");
        if (border != null) border.Child = mainPanel;
    }

    private Grid CreateColorsTab()
    {
        Grid mainGrid = new Grid
        {
            Margin = new Thickness(8),
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            }
        };
        StackPanel searchPanel = new StackPanel // Search bar
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 0, 0, 8)
        };
        MaterialIcon searchIcon = new MaterialIcon
        {
            Kind = MaterialIconKind.Magnify,
            Foreground = EditorColor.FromRGB(128, 128, 128),
            Width = 16,
            Height = 16,
            VerticalAlignment = VerticalAlignment.Center
        };

        colorSearchBox = new TextBox
        {
            InnerLeftContent = searchIcon,
            PlaceholderText = "Search colors...",
            Width = 250,
            Background = EditorColor.FromRGB(30, 30, 30),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(1),
            BorderBrush = EditorColor.FromRGB(60, 60, 60)
        };
        colorSearchBox.TextChanged += ColorSearchBox_TextChanged;

        TextBlock statsText = new TextBlock
        {
            Text = "Loading...",
            Foreground = Brushes.Gray,
            VerticalAlignment = VerticalAlignment.Center
        };

        searchPanel.Children.Add(colorSearchBox);
        searchPanel.Children.Add(statsText);

        colorScrollViewer = new ScrollViewer // Color grid
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        colorPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 2
        };
        colorScrollViewer.Content = colorPanel;

        Grid.SetRow(searchPanel, 0); // Add to grid
        Grid.SetRow(colorScrollViewer, 1);

        mainGrid.Children.Add(searchPanel);
        mainGrid.Children.Add(colorScrollViewer);

        LoadAllColors((text) => // Load colors
        {
            Dispatcher.UIThread.Post(() => statsText.Text = text);
        });
        return mainGrid;
    }

    private Grid CreateIconsTab()
    {
        Grid mainGrid = new Grid
        {
            Margin = new Thickness(8),
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            }
        };

        StackPanel controlsPanel = new StackPanel // Controls panel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Margin = new Thickness(0, 0, 0, 8)
        };
        MaterialIcon searchIcon = new MaterialIcon
        {
            Kind = MaterialIconKind.Magnify,
            Foreground = EditorColor.FromRGB(128, 128, 128),
            Width = 16,
            Height = 16,
            VerticalAlignment = VerticalAlignment.Center
        };

        iconSearchBox = new TextBox
        {
            InnerLeftContent = searchIcon,
            PlaceholderText = "Search icons...",
            Width = 300,
            Background = EditorColor.FromRGB(30, 30, 30),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(1),
            BorderBrush = EditorColor.FromRGB(60, 60, 60)
        };
        iconSearchBox.TextChanged += IconSearchBox_TextChanged;

        StackPanel sizePanel = new StackPanel // Size selector
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };
        sizePanel.Children.Add(new TextBlock
        {
            Text = "Size:",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        iconSizeComboBox = new ComboBox
        {
            Width = 80,
            Background = EditorColor.FromRGB(30, 30, 30),
            Foreground = Brushes.White,
            SelectedIndex = 2
        };
        iconSizeComboBox.Items.Add("16");
        iconSizeComboBox.Items.Add("24");
        iconSizeComboBox.Items.Add("32");
        iconSizeComboBox.Items.Add("48");
        iconSizeComboBox.Items.Add("64");
        iconSizeComboBox.SelectionChanged += IconSize_SelectionChanged;
        sizePanel.Children.Add(iconSizeComboBox);

        TextBlock statsText = new TextBlock
        {
            Text = "Loading...",
            Foreground = Brushes.Gray,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0)
        };

        controlsPanel.Children.Add(iconSearchBox);
        controlsPanel.Children.Add(sizePanel);
        controlsPanel.Children.Add(statsText);

        iconScrollViewer = new ScrollViewer // Icon grid
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        iconPanel = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemSpacing = 8,
        };
        iconScrollViewer.Content = iconPanel;

        Grid.SetRow(controlsPanel, 0); // Add to grid
        Grid.SetRow(iconScrollViewer, 1);

        mainGrid.Children.Add(controlsPanel);
        mainGrid.Children.Add(iconScrollViewer);

        LoadAllIcons((count) => // Load icons
        {
            Dispatcher.UIThread.Post(() => statsText.Text = $"{count} icons");
        });
        return mainGrid;
    }

    private void LoadAllColors(Action<string> updateStats)
    {
        Dispatcher.UIThread.Post(() =>
        {
            colorPanel?.Children.Clear();

            var colorProperties = typeof(ColorPalette)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(float4))
                .ToList();
            updateStats($"Total colors: {colorProperties.Count}");

            foreach (FieldInfo prop in colorProperties)
            {
                if (colorPanel == null) break;
                float4 colorValue = (float4)prop.GetValue(null)!;
                Border colorControl = CreateColorDisplayControl(prop.Name, colorValue);
                colorPanel.Children.Add(colorControl);
            }
            ApplyColorFilter();
        });
    }

    private Border CreateColorDisplayControl(string name, float4 color)
    {
        Border border = new Border
        {
            Margin = new Thickness(0, 2),
            Padding = new Thickness(8, 6),
            CornerRadius = new CornerRadius(4),
            Background = EditorColor.FromRGB(30, 30, 30),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        Grid panel = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                new ColumnDefinition(GridLength.Auto)
            },
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto)
            }
        };
        Border swatch = new Border // Color swatch
        {
            Width = 60,
            Height = 30,
            CornerRadius = new CornerRadius(4),
            Background = new SolidColorBrush(Color.FromRgb(
                (byte)(color.X * 255),
                (byte)(color.Y * 255),
                (byte)(color.Z * 255))),
            BorderThickness = new Thickness(1),
            BorderBrush = EditorColor.FromRGB(80, 80, 80),
            Margin = new Thickness(0, 0, 12, 0)
        };
        TextBlock nameText = new TextBlock // Color name
        {
            Text = name,
            Foreground = Brushes.White,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeight.Medium
        };
        TextBlock rgbText = new TextBlock // RGB values
        {
            Text = $"RGB({(byte)(color.X * 255)}, {(byte)(color.Y * 255)}, {(byte)(color.Z * 255)})",
            Foreground = Brushes.Gray,
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0),
            FontFamily = "Consolas, monospace"
        };

        Grid.SetColumn(swatch, 0);
        Grid.SetColumn(nameText, 1);
        Grid.SetColumn(rgbText, 2);
        panel.Children.Add(swatch);
        panel.Children.Add(nameText);
        panel.Children.Add(rgbText);
        
        border.PointerPressed += (s, e) => // Copy to clipboard on click
        {
            IClipboard? clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                DataTransfer clipboardData = new DataTransfer(); // This new clipboard thing is hella annoying wtf
                clipboardData.Add(DataTransferItem.CreateText(name));
                TopLevel.GetTopLevel(this)?.Clipboard?.SetDataAsync(clipboardData);
                Debug.Info($"Copying color name: {name}");

                // Visual feedback
                IBrush originalBg = border.Background;
                border.Background = EditorColor.FromRGB(50, 50, 80);
                Dispatcher.UIThread.Post(() =>
                {
                    border.Background = originalBg;
                }, DispatcherPriority.Background);
            }
        };

        border.Child = panel;
        return border;
    }

    private void ApplyColorFilter()
    {
        if (colorPanel == null) return;
        foreach (Control child in colorPanel.Children)
        {
            if (child is Border border && border.Child is Grid grid && grid.Children.Count > 1)
            {
                if (grid.Children[1] is TextBlock nameText)
                {
                    bool matches = string.IsNullOrWhiteSpace(colorSearchFilter) ||
                                  nameText.Text!.Contains(colorSearchFilter, StringComparison.OrdinalIgnoreCase);
                    border.IsVisible = matches;
                }
            }
        }
    }

    private void ColorSearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        colorSearchFilter = colorSearchBox?.Text?.Trim() ?? string.Empty;
        ApplyColorFilter();
    }

    private void LoadAllIcons(Action<int> updateStats)
    {
        Dispatcher.UIThread.Post(() =>
        {
            iconPanel?.Children.Clear();
            var iconValues = Enum.GetValues<MaterialIconKind>();
            var iconCount = iconValues.Length;
            updateStats(iconCount);

            foreach (MaterialIconKind kind in iconValues)
            {
                if (iconPanel == null) break;

                var iconControl = CreateIconDisplayControl(kind);
                iconPanel.Children.Add(iconControl);
            }

            ApplyIconFilter();
        });
    }

    private Border CreateIconDisplayControl(MaterialIconKind kind)
    {
        Border border = new Border
        {
            Margin = new Thickness(2),
            Padding = new Thickness(8, 6),
            CornerRadius = new CornerRadius(4),
            Background = EditorColor.FromRGB(35, 35, 35),
            Cursor = new Cursor(StandardCursorType.Hand),
            MinWidth = 150
        };
        StackPanel panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        MaterialIcon icon = new MaterialIcon // Icon preview
        {
            Kind = kind,
            Width = currentIconSize,
            Height = currentIconSize,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        TextBlock nameText = new TextBlock // Icon name (formatted for readability)
        {
            Text = FormatIconName(kind.ToString()),
            Foreground = Brushes.White,
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 130,
            TextAlignment = TextAlignment.Center
        };
        ToolTip toolTip = new ToolTip // Raw name tooltip
        {
            Content = kind.ToString()
        };
        ToolTip.SetTip(border, toolTip);

        panel.Children.Add(icon);
        panel.Children.Add(nameText);

        border.PointerPressed += (s, e) => // Copy name to clipboard on click
        {
            IClipboard? clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                DataTransfer clipboardData = new DataTransfer();
                clipboardData.Add(DataTransferItem.CreateText(kind.ToString()));
                TopLevel.GetTopLevel(this)?.Clipboard?.SetDataAsync(clipboardData);
                Debug.Info($"Copying material icon kind: {kind}");

                // Visual feedback
                IBrush originalBg = border.Background;
                border.Background = EditorColor.FromRGB(55, 55, 75);
                Dispatcher.UIThread.Post(() =>
                {
                    border.Background = originalBg;
                }, DispatcherPriority.Background);
            }
        };

        border.Child = panel;
        return border;
    }

    private static string FormatIconName(string name) => IconNameRegex().Replace(name, " $1");

    private void ApplyIconFilter()
    {
        if (iconPanel == null) return;
        foreach (Control child in iconPanel.Children)
        {
            if (child is Border border && border.Child is StackPanel panel && panel.Children.Count > 1)
            {
                if (panel.Children[1] is TextBlock nameText)
                {
                    bool matches = string.IsNullOrWhiteSpace(iconSearchFilter) ||
                                  nameText.Text!.Contains(iconSearchFilter, StringComparison.OrdinalIgnoreCase) ||
                                  (ToolTip.GetTip(border) as string)?.Contains(iconSearchFilter, StringComparison.OrdinalIgnoreCase) == true;
                    border.IsVisible = matches;
                }
            }
        }
    }

    private void IconSearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        iconSearchFilter = iconSearchBox?.Text?.Trim() ?? string.Empty;
        ApplyIconFilter();
    }

    private void IconSize_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (iconSizeComboBox?.SelectedItem != null && int.TryParse(iconSizeComboBox.SelectedItem.ToString(), out int newSize))
        {
            currentIconSize = newSize;
            if (iconPanel != null) // Update all icon sizes
            {
                foreach (Control child in iconPanel.Children)
                {
                    if (child is Border border && border.Child is StackPanel panel && panel.Children.Count > 0)
                    {
                        if (panel.Children[0] is MaterialIcon icon)
                        {
                            icon.Width = currentIconSize;
                            icon.Height = currentIconSize;
                        }
                    }
                }
            }
        }
    }

    [System.Text.RegularExpressions.GeneratedRegex("(\\B[A-Z])")]
    private static partial System.Text.RegularExpressions.Regex IconNameRegex();
}