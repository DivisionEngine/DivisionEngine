//
// Copyright (C) 2026 Rex Woodfield
//
// This file is part of Division Engine.
//
// Division Engine is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Division Engine is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Division Engine.  If not, see <https://www.gnu.org/licenses/>.
//
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DivisionEngine.MathLib;
using Material.Icons;
using Material.Icons.Avalonia;
using System;
using System.Linq;
using Math = DivisionEngine.MathLib.Math;

namespace DivisionEngine.Editor;

/// <summary>
/// Class that represents a console window in Division.
/// </summary>
public partial class ConsoleWindow : EditorWindow
{
    public const int MaxDisplayedLogEntries = 1000;

    private readonly StackPanel logList;
    private readonly StackPanel controlsPanel;
    private readonly ScrollViewer scrollViewer;
    private readonly CheckBox autoscrollCheckbox;
    private readonly ComboBox filterLogTypeBox;
    private readonly Button clearButton;
    private readonly TextBox searchBox;
    private readonly MaterialIcon searchIcon;
    private bool autoScroll;
    private string searchFilter = string.Empty;

    /// <summary>
    /// Builds a new console window.
    /// </summary>
    public ConsoleWindow()
    {
        InitializeComponent();

        // Create header controls
        autoScroll = true;
        clearButton = new Button
        {
            Content = "Clear",
            FontSize = 12,
            Height = 25,
            FontStretch = FontStretch.SemiExpanded,
            Background = EditorColor.FromRGB(17, 17, 17),
            Foreground = EditorColor.FromColor(ColorPalette.White),
            BorderThickness = new Thickness(0),
            Margin = new Thickness(4, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        clearButton.Click += ClearButton_Click;
        autoscrollCheckbox = new CheckBox
        {
            Content = "Auto Scroll",
            Foreground = Brushes.White,
            IsChecked = autoScroll,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };
        autoscrollCheckbox.IsCheckedChanged += (s, e) => { autoScroll = autoscrollCheckbox.IsChecked.Value; };
        searchIcon = new MaterialIcon
        {
            Kind = MaterialIconKind.Search,
            Foreground = EditorColor.FromRGB(128, 128, 128),
            Margin = new Thickness(6, 0, 0, 0),
            Width = 12,
            Height = 12,
        };
        searchBox = new TextBox
        {
            InnerLeftContent = searchIcon,
            Text = "",
            Watermark = "Search Logs...",
            FontSize = 12,
            Foreground = EditorColor.FromRGB(220, 220, 220),
            Background = EditorColor.FromRGB(17, 17, 17),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(0),
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            MinWidth = 150,
            Margin = new Thickness(8, 0, 0, 0),
        };
        searchBox.TextChanged += SearchBox_TextChanged;

        StackPanel allLogTypes = new StackPanel {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
        };
        StackPanel debugLogType = new StackPanel {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
        };
        StackPanel infoLogType = new StackPanel {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
        };
        StackPanel warnLogType = new StackPanel {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
        };
        StackPanel errorLogType = new StackPanel {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
        };

        allLogTypes.Children.Add(new MaterialIcon { Kind = MaterialIconKind.AllInclusive });
        allLogTypes.Children.Add(new TextBlock { Text = "All" });
        infoLogType.Children.Add(new MaterialIcon { Kind = MaterialIconKind.Info });
        infoLogType.Children.Add(new TextBlock { Text = "Info" });
        debugLogType.Children.Add(new MaterialIcon { Kind = MaterialIconKind.DebugStepOver });
        debugLogType.Children.Add(new TextBlock { Text = "Debug" });
        warnLogType.Children.Add(new MaterialIcon { Kind = MaterialIconKind.Warning, Foreground = EditorColor.FromRGB(200, 200, 0) });
        warnLogType.Children.Add(new TextBlock { Text = "Warning" });
        errorLogType.Children.Add(new MaterialIcon { Kind = MaterialIconKind.Error, Foreground = EditorColor.FromRGB(200, 0, 0) });
        errorLogType.Children.Add(new TextBlock { Text = "Error" });
        filterLogTypeBox = new ComboBox
        {
            Items =
            {
                new ComboBoxItem { Content = allLogTypes, },
                new ComboBoxItem { Content = infoLogType, },
                new ComboBoxItem { Content = debugLogType, },
                new ComboBoxItem { Content = warnLogType, },
                new ComboBoxItem { Content = errorLogType, },
            },
            SelectedIndex = 0,
            Foreground = Brushes.White,
            Background = EditorColor.FromRGB(17, 17, 17),
            BorderThickness = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0),
        };
        filterLogTypeBox.SelectionChanged += (s, e) => LoadAllCurrentLogs();

        // Create panels
        logList = new StackPanel
        {
            Orientation = Orientation.Vertical
        };
        controlsPanel = new StackPanel
        {
            Background = EditorColor.FromRGB(28, 28, 28),
            Orientation = Orientation.Horizontal,
            Spacing = 0,
            Height = 30,
            VerticalAlignment = VerticalAlignment.Top
        };
        scrollViewer = new ScrollViewer
        {
            Content = logList,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        controlsPanel.Children.Add(clearButton);
        controlsPanel.Children.Add(autoscrollCheckbox);
        controlsPanel.Children.Add(searchBox);
        controlsPanel.Children.Add(filterLogTypeBox);

        DockPanel mainPanel = new DockPanel { Background = EditorColor.FromRGB(45, 45, 45) };
        DockPanel.SetDock(controlsPanel, Dock.Top);
        mainPanel.Children.Add(controlsPanel);
        mainPanel.Children.Add(scrollViewer);

        Debug.OnLogUpdate += Debug_OnLogUpdate;

        LoadAllCurrentLogs();
        Border? border = this.FindControl<Border>("MainBorder");
        if (border != null) border.Child = mainPanel;
    }

    /// <summary>
    /// Called when the clear button is clicked.
    /// </summary>
    private void ClearButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        logList.Children.Clear();
        Debug.ClearLogs();
    }

    /// <summary>
    /// Called when debug log is updated.
    /// </summary>
    /// <param name="obj">Log entry from update</param>
    private void Debug_OnLogUpdate(LogEntry obj) => Dispatcher.UIThread.Post(() => CreateLogEntry(obj, autoScroll));

    /// <summary>
    /// Called when the search field is edited.
    /// </summary>
    private void SearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        searchFilter = searchBox.Text?.Trim() ?? string.Empty;
        ApplySearchFilter();
    }

    /// <summary>
    /// Applies the search field and log level filters to the log list.
    /// </summary>
    private void ApplySearchFilter()
    {
        if (string.IsNullOrWhiteSpace(searchFilter))
        {
            foreach (Control? child in logList.Children) // Show all logs that match the current level filter
            {
                if (child is Border border)
                {
                    LogEntry? logEntry = GetLogEntryFromControl(border);
                    if (logEntry != null)
                        border.IsVisible = filterLogTypeBox.SelectedIndex == 0 ||
                            logEntry.Level == (LogLevel)(filterLogTypeBox.SelectedIndex - 1);
                }
            }
        }
        else
        {
            foreach (Control? child in logList.Children) // Filter by both level and search text
            {
                if (child is Border border)
                {
                    LogEntry? logEntry = GetLogEntryFromControl(border);
                    if (logEntry != null)
                    {
                        bool matchesLevel = filterLogTypeBox.SelectedIndex == 0 ||
                                           logEntry.Level == (LogLevel)(filterLogTypeBox.SelectedIndex - 1);
                        bool matchesSearch = logEntry.Message.Contains(searchFilter, StringComparison.OrdinalIgnoreCase) ||
                                            logEntry.Timestamp.ToString().Contains(searchFilter, StringComparison.OrdinalIgnoreCase) ||
                                            logEntry.Level.ToString().Contains(searchFilter, StringComparison.OrdinalIgnoreCase);
                        border.IsVisible = matchesLevel && matchesSearch;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gets a log entry from a log control in avalonia.
    /// </summary>
    /// <param name="logControl">Log control to sift through</param>
    /// <returns>LogEntry value associated with control</returns>
    private static LogEntry? GetLogEntryFromControl(Border logControl)
    {
        // Try to find the log entry in Debug.Logs that matches this control
        if (logControl.Child is StackPanel mainPanel && mainPanel.Children.Count > 0 && mainPanel.Children[0] is Grid headerGrid)
        {
            foreach (Control child in headerGrid.Children)
            {
                if (child is SelectableTextBlock messageText)
                    return Debug.Logs.FirstOrDefault(log => // Primitive log search, modify this in the future to improve search algorithm
                        messageText.Text!.Contains(log.Message) ||
                        log.Message.Contains(messageText.Text));
            }
        }
        return null;
    }

    /// <summary>
    /// Loads all the current logs in the Debug.Logs list.
    /// </summary>
    private void LoadAllCurrentLogs()
    {
        logList.Children.Clear();
        foreach (LogEntry log in Debug.Logs)
            Dispatcher.UIThread.Post(() => CreateLogEntry(log, false));
    }

    /// <summary>
    /// Creates a log entry in the log list view.
    /// </summary>
    /// <param name="log">Log entry to build</param>
    /// <param name="scrollToEnd">Whether to scroll to the end when done</param>
    private void CreateLogEntry(LogEntry log, bool scrollToEnd)
    {
        if (filterLogTypeBox.SelectedIndex == 0 || log.Level == (LogLevel)(filterLogTypeBox.SelectedIndex - 1))
        {
            Border logContainer = CreateLogControl(log);
            logList.Children.Add(logContainer);
            if (scrollToEnd) scrollViewer.ScrollToEnd();
            if (logList.Children.Count > MaxDisplayedLogEntries)
                logList.Children.RemoveAt(0);
        }
    }

    /// <summary>
    /// Builds the control for a long entry in the console window.
    /// </summary>
    /// <param name="log">Log entry to build</param>
    /// <returns>Log entry container element</returns>
    private Border CreateLogControl(LogEntry log)
    {
        // Check if the message contains newlines or is too long
        bool isMultiLine = log.Message.Contains('\n') || log.Message.Length > 100;
        Border logBorder = new Border()
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(10, 10, 10)),
            BorderThickness = new Thickness(0, 0, 2, 2),
            Padding = new Thickness(4),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(6, 2, 6, 2),
            Background = EditorColor.FromRGB(17, 17, 17),
        };
        StackPanel mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 4,
        };
        Grid headerGrid = new Grid // Header row
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                new ColumnDefinition(GridLength.Auto),
            },
        };

        // Expand/collapse button (only visible for multi-line logs)
        Button? expandButton = null;
        if (isMultiLine)
        {
            expandButton = new Button
            {
                Content = new MaterialIcon
                {
                    Kind = MaterialIconKind.ChevronRight,
                    Width = 12,
                    Height = 12,
                    Foreground = Brushes.Gray,
                },
                Background = Brushes.Transparent,
                Padding = new Thickness(2),
                Margin = new Thickness(0, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center,
                BorderThickness = new Thickness(0),
                Width = 20,
                Height = 20,
            };
            headerGrid.Children.Add(expandButton);
            Grid.SetColumn(expandButton, 0);
        }

        // Timestamp
        int columnOffset = isMultiLine ? 1 : 0;
        headerGrid.Children.Add(new TextBlock
        {
            Text = $"[{log.Timestamp.TimeOfDay:hh':'mm':'ss'.'fff}]",
            FontSize = 11,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Center,
        });
        Grid.SetColumn(headerGrid.Children[^1], columnOffset);

        // Log level
        headerGrid.Children.Add(new TextBlock
        {
            Text = $"[{log.Level}]",
            FontSize = 11,
            Foreground = GetLogColor(log.Level),
            Margin = new Thickness(0, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Center,
        });
        Grid.SetColumn(headerGrid.Children[^1], columnOffset + 1);

        // Truncated message
        string displayMessage = log.Message;
        if (isMultiLine)
        {
            int firstNewline = log.Message.IndexOf('\n'); // Get first line or truncate
            if (firstNewline >= 0)
                displayMessage = string.Concat(log.Message.AsSpan(0, Math.Min(firstNewline, 100)), "...");
            else if (log.Message.Length > 100)
                displayMessage = string.Concat(log.Message.AsSpan(0, 100), "...");
        }

        SelectableTextBlock messageText = new SelectableTextBlock
        {
            Text = displayMessage,
            FontSize = 11,
            Foreground = Brushes.White,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.NoWrap,
        };
        headerGrid.Children.Add(messageText);
        Grid.SetColumn(headerGrid.Children[^1], columnOffset + 2);

        // Delete button
        Button deleteButton = new Button
        {
            Content = new MaterialIcon
            {
                Kind = MaterialIconKind.Delete,
                Width = 12,
                Height = 12,
                Foreground = Brushes.White,
            },
            Background = EditorColor.FromRGB(68, 68, 68),
            Padding = new Thickness(2),
            Margin = new Thickness(4, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            Width = 24,
            Height = 24,
        };
        deleteButton.Click += (e, s) => ClickDeleteButton(log);
        headerGrid.Children.Add(deleteButton);
        Grid.SetColumn(headerGrid.Children[^1], columnOffset + 3);

        mainPanel.Children.Add(headerGrid);

        // Expanded content area
        Border? expandedContent = null;
        if (isMultiLine)
        {
            expandedContent = new Border
            {
                Background = EditorColor.FromRGB(6, 6, 6),
                BorderBrush = EditorColor.FromRGB(28, 28, 28),
                BorderThickness = new Thickness(1, 1, 0, 0),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 6),
                Margin = new Thickness(isMultiLine ? 24 : 0, 4, 0, 0),
                IsVisible = false,
            };

            // Full message with proper formatting
            SelectableTextBlock fullMessage = new SelectableTextBlock
            {
                Text = log.Message,
                FontSize = 11,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = FontFamily.Parse("Consolas, Courier New, monospace"),
            };

            // If there's a stack trace, format it nicely
            if (log.Message.Contains("StackTrace:") || log.Message.Contains("at "))
            {
                // You could parse and format stack traces here
                // For now, just use the full message
            }

            expandedContent.Child = fullMessage;
            mainPanel.Children.Add(expandedContent);

            // Set up expand/collapse functionality
            bool isExpanded = false;
            expandButton!.Click += (s, e) =>
            {
                isExpanded = !isExpanded;
                expandedContent.IsVisible = isExpanded;

                if (expandButton.Content is MaterialIcon icon)
                    icon.Kind = isExpanded ? MaterialIconKind.ChevronDown : MaterialIconKind.ChevronRight;

                // Work on improving auto scroll when expanded for a less-janky experience

                // Adjust auto-scroll if enabled
                /*if (isExpanded && autoScroll)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        scrollViewer.ScrollToEnd();
                    }, DispatcherPriority.Background);
                }*/
            };
        }

        logBorder.Child = mainPanel;
        return logBorder;
    }

    /// <summary>
    /// Called when a log delete button is clicked.
    /// </summary>
    /// <param name="logEntry">Log entry to delete</param>
    private void ClickDeleteButton(LogEntry logEntry)
    {
        Debug.ClearLog(logEntry);
        LoadAllCurrentLogs();
    }

    /// <summary>
    /// Gets the correct log level color.
    /// </summary>
    /// <param name="level">Log level</param>
    /// <returns>Color for log level</returns>
    private static IBrush GetLogColor(LogLevel level) => level switch
    {
        LogLevel.Debug => Brushes.White,
        LogLevel.Info => Brushes.White,
        LogLevel.Warning => Brushes.Yellow,
        LogLevel.Error => Brushes.Red,
        _ => Brushes.Green
    };
}