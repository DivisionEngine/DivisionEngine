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
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DivisionEngine.MathLib;
using DivisionEngine.MathUtilities;
using Material.Icons;
using Material.Icons.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
    private readonly CheckBox collapseCheckbox;
    private readonly ComboBox filterLogTypeBox;
    private readonly Button clearButton;
    private readonly TextBox searchBox;
    private readonly MaterialIcon searchIcon;
    private bool autoScroll;
    private bool collapseEnabled;
    private string searchFilter = string.Empty;
    private readonly Lock threadLock;

    // Grouped log entries for collapse feature
    private readonly Dictionary<string, GroupedLogEntry> groupedLogs = [];

    /// <summary>
    /// Represents a group of identical log entries.
    /// </summary>
    private class GroupedLogEntry
    {
        public LogEntry FirstLog { get; set; } = null!;
        public int Count { get; set; } = 1;
        public Border? Control { get; set; }
    }

    /// <summary>
    /// Builds a new console window.
    /// </summary>
    public ConsoleWindow()
    {
        InitializeComponent();

        // Create header controls
        autoScroll = true;
        collapseEnabled = false;
        threadLock = new Lock();

        clearButton = new Button
        {
            Content = "Clear",
            FontSize = 12,
            Height = 25,
            FontStretch = FontStretch.SemiExpanded,
            Background = EditorColor.FromRGB(17, 17, 17),
            Foreground = EditorColor.FromColor(ColorPalette.White),
            BorderBrush = EditorColor.FromRGB(28, 28, 28),
            BorderThickness = new Thickness(1, 1, 0, 0),
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
            Margin = new Thickness(8, 0, 0, 0),
        };
        autoscrollCheckbox.IsCheckedChanged += (s, e) => { autoScroll = autoscrollCheckbox.IsChecked.Value; };

        // Collapse checkbox (like Unity)
        collapseCheckbox = new CheckBox
        {
            Content = "Collapse",
            Foreground = Brushes.White,
            IsChecked = collapseEnabled,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0),
        };
        collapseCheckbox.IsCheckedChanged += (s, e) =>
        {
            collapseEnabled = collapseCheckbox.IsChecked.Value;
            ReloadLogs();
        };

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
            PlaceholderText = "Search Logs...",
            FontSize = 12,
            Foreground = EditorColor.FromRGB(220, 220, 220),
            Background = EditorColor.FromRGB(17, 17, 17),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(4),
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            MinWidth = 150,
            Margin = new Thickness(8, 0, 0, 0),
        };
        searchBox.TextChanged += SearchBox_TextChanged;

        // Log type filter dropdown
        StackPanel allLogTypes = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
        };
        StackPanel debugLogType = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
        };
        StackPanel infoLogType = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
        };
        StackPanel warnLogType = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
        };
        StackPanel errorLogType = new StackPanel
        {
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
        filterLogTypeBox.SelectionChanged += (s, e) => ReloadLogs();

        // Create panels
        logList = new StackPanel
        {
            Orientation = Orientation.Vertical,
        };
        controlsPanel = new StackPanel
        {
            Background = EditorColor.FromRGB(28, 28, 28),
            Orientation = Orientation.Horizontal,
            Spacing = 0,
            Height = 30,
            VerticalAlignment = VerticalAlignment.Top,
        };
        scrollViewer = new ScrollViewer
        {
            Content = logList,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };

        controlsPanel.Children.Add(clearButton);
        controlsPanel.Children.Add(autoscrollCheckbox);
        controlsPanel.Children.Add(collapseCheckbox);
        controlsPanel.Children.Add(searchBox);
        controlsPanel.Children.Add(filterLogTypeBox);

        DockPanel mainPanel = new DockPanel { Background = EditorColor.FromRGB(45, 45, 45) };
        DockPanel.SetDock(controlsPanel, Dock.Top);
        mainPanel.Children.Add(controlsPanel);
        mainPanel.Children.Add(scrollViewer);

        // Attach background context menu
        AttachBackgroundContextMenu();

        Debug.OnLogUpdate += Debug_OnLogUpdate;

        ReloadLogs();
        Border? border = this.FindControl<Border>("MainBorder");
        if (border != null) border.Child = mainPanel;
    }

    /// <summary>
    /// Attaches a context menu to the background of the console window.
    /// </summary>
    private void AttachBackgroundContextMenu()
    {
        ContextMenu backgroundContextMenu = new ContextMenu
        {
            Background = EditorColor.FromRGB(68, 68, 68),
            BorderBrush = EditorColor.FromRGB(128, 128, 128),
        };

        // Clear logs
        MenuItem clearItem = new MenuItem
        {
            Header = "Clear All",
            Icon = new MaterialIcon { Kind = MaterialIconKind.Delete, Width = 16, Height = 16 },
            Foreground = EditorColor.FromRGB(220, 68, 68),
        };
        clearItem.Click += (s, e) => ClearButton_Click(s, e);
        backgroundContextMenu.Items.Add(clearItem);

        // Separator
        backgroundContextMenu.Items.Add(new Separator());

        // Copy all
        MenuItem copyAllItem = new MenuItem
        {
            Header = "Copy All",
            Icon = new MaterialIcon { Kind = MaterialIconKind.ContentCopy, Width = 16, Height = 16 },
            Foreground = Brushes.White,
        };
        copyAllItem.Click += (s, e) => CopyAllLogs();
        backgroundContextMenu.Items.Add(copyAllItem);

        // Open log file
        MenuItem openLogFileItem = new MenuItem
        {
            Header = "Open Log File",
            Icon = new MaterialIcon { Kind = MaterialIconKind.FileDocument, Width = 16, Height = 16 },
            Foreground = Brushes.White,
        };
        openLogFileItem.Click += (s, e) => Debug.OpenLogFile();
        backgroundContextMenu.Items.Add(openLogFileItem);

        // Open log directory
        MenuItem openLogDirItem = new MenuItem
        {
            Header = "Open Log Directory",
            Icon = new MaterialIcon { Kind = MaterialIconKind.FolderOpen, Width = 16, Height = 16 },
            Foreground = Brushes.White,
        };
        openLogDirItem.Click += (s, e) => Debug.OpenLogDirectory();
        backgroundContextMenu.Items.Add(openLogDirItem);

        scrollViewer.ContextMenu = backgroundContextMenu;
    }

    /// <summary>
    /// Copies all logs to the clipboard.
    /// </summary>
    private async void CopyAllLogs()
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard == null) return;

            var logs = Debug.Logs.Select(log => log.ToFileString());
            string allLogs = string.Join(Environment.NewLine, logs);

            var data = new Avalonia.Input.DataTransfer();
            data.Add(Avalonia.Input.DataTransferItem.CreateText(allLogs));
            await clipboard.SetDataAsync(data);

            Debug.Info($"Copied {Debug.Logs.Count} logs to clipboard");
        }
        catch (Exception ex)
        {
            Debug.Error($"Failed to copy logs: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the collapse key for a log entry (based on caller info and message).
    /// </summary>
    private static string GetCollapseKey(LogEntry log)
    {
        // Use the caller info + message as the key for collapsing
        // This matches Unity's behavior of grouping by identical call stack + message
        return $"{log.CallerInfo}|{log.Message}|{log.Level}";
    }

    /// <summary>
    /// Reloads all logs with current filters and collapse settings.
    /// </summary>
    private void ReloadLogs()
    {
        logList.Children.Clear();
        groupedLogs.Clear();

        if (collapseEnabled)
        {
            // First, group all logs by their collapse key
            IReadOnlyList<LogEntry> logs = Debug.Logs;
            foreach (LogEntry log in logs)
            {
                string key = GetCollapseKey(log);
                if (!groupedLogs.TryGetValue(key, out GroupedLogEntry? group))
                {
                    group = new GroupedLogEntry { FirstLog = log, Count = 1 };
                    groupedLogs[key] = group;
                }
                else group.Count++;
            }

            // Then display each group
            foreach (GroupedLogEntry group in groupedLogs.Values)
            {
                if (ShouldShowLog(group.FirstLog))
                {
                    Border control = CreateLogControl(group.FirstLog, group.Count);
                    group.Control = control;
                    logList.Children.Add(control);
                }
            }
        }
        else
        {
            // Normal display - one entry per log
            lock (threadLock)
            {
                List<LogEntry> logs = [.. Debug.Logs];
                foreach (LogEntry log in logs)
                    if (ShouldShowLog(log))
                        logList.Children.Add(CreateLogControl(log, 1));
            }
        }

        // Auto-scroll to end if enabled
        if (autoScroll) Dispatcher.UIThread.Post(scrollViewer.ScrollToEnd, DispatcherPriority.Background);
    }

    /// <summary>
    /// Determines if a log should be shown based on filters.
    /// </summary>
    private bool ShouldShowLog(LogEntry log)
    {
        // Level filter
        bool matchesLevel = filterLogTypeBox.SelectedIndex == 0 ||
                           log.Level == (LogLevel)(filterLogTypeBox.SelectedIndex - 1);

        if (!matchesLevel) return false;

        // Search filter
        if (!string.IsNullOrWhiteSpace(searchFilter))
        {
            return log.Message.Contains(searchFilter, StringComparison.OrdinalIgnoreCase) ||
                   log.Timestamp.ToString().Contains(searchFilter, StringComparison.OrdinalIgnoreCase) ||
                   log.Level.ToString().Contains(searchFilter, StringComparison.OrdinalIgnoreCase) ||
                   log.CallerInfo.Contains(searchFilter, StringComparison.OrdinalIgnoreCase);
        }
        return true;
    }

    /// <summary>
    /// Called when the clear button is clicked.
    /// </summary>
    private void ClearButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        logList.Children.Clear();
        groupedLogs.Clear();
        Debug.ClearLogs();
    }

    /// <summary>
    /// Called when debug log is updated.
    /// </summary>
    private void Debug_OnLogUpdate(LogEntry obj)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (collapseEnabled)
            {
                // Update the grouped log entry
                string key = GetCollapseKey(obj);
                if (groupedLogs.TryGetValue(key, out GroupedLogEntry? group))
                {
                    // Update count
                    group.Count++;

                    // Refresh the control if it exists
                    if (group.Control != null && ShouldShowLog(obj)) UpdateLogCount(group.Control, group.Count);
                }
                else if (ShouldShowLog(obj))
                {
                    // New group - create control
                    GroupedLogEntry newGroup = new GroupedLogEntry { FirstLog = obj, Count = 1 };
                    Border control = CreateLogControl(obj, 1);
                    newGroup.Control = control;
                    groupedLogs[key] = newGroup;
                    logList.Children.Add(control);

                    if (autoScroll) scrollViewer.ScrollToEnd();
                }
            }
            else
            {
                // Normal mode - just add the log
                if (ShouldShowLog(obj))
                {
                    Border control = CreateLogControl(obj, 1);
                    logList.Children.Add(control);

                    if (autoScroll) scrollViewer.ScrollToEnd();
                }
            }
        });
    }

    /// <summary>
    /// Updates the count display on a grouped log entry.
    /// </summary>
    private static void UpdateLogCount(Border control, int count)
    {
        if (control.Child is StackPanel mainPanel && mainPanel.Children.Count > 0)
        {
            // Find the count badge in the header
            foreach (Control child in mainPanel.Children)
            {
                if (child is Grid headerGrid)
                {
                    foreach (Control gridChild in headerGrid.Children)
                    {
                        if (gridChild is Border badgeBorder && badgeBorder.Classes.Contains("count-badge"))
                        {
                            if (badgeBorder.Child is TextBlock badgeText) badgeText.Text = count.ToString();
                            break;
                        }
                    }
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Called when the search field is edited.
    /// </summary>
    private void SearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        searchFilter = searchBox.Text?.Trim() ?? string.Empty;
        ReloadLogs();
    }

    /// <summary>
    /// Creates a log entry control with optional count badge for collapsed logs.
    /// </summary>
    private Border CreateLogControl(LogEntry log, int count)
    {
        bool isMultiLine = log.Message.Contains('\n') || log.Message.Length > 100;
        bool hasCallerInfo = !string.IsNullOrEmpty(log.CallerInfo);
        bool isGrouped = count > 1;

        Border logBorder = new Border()
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(10, 10, 10)),
            BorderThickness = new Thickness(0, 0, 2, 2),
            Padding = new Thickness(4),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(6, 2, 6, 2),
            Background = EditorColor.FromRGB(17, 17, 17),
            Tag = log,
        };

        StackPanel mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 2,
        };

        // Header row
        Grid headerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
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

        int columnOffset = isMultiLine ? 1 : 0;

        // Timestamp
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

        // Count badge for collapsed logs (like Unity)
        if (isGrouped)
        {
            Border countBadge = new Border
            {
                Classes = { "count-badge" },
                Background = EditorColor.FromRGB(68, 68, 68),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(6, 1),
                Margin = new Thickness(0, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = count.ToString(),
                    FontSize = 10,
                    Foreground = Brushes.White,
                    FontWeight = FontWeight.Medium,
                }
            };
            headerGrid.Children.Add(countBadge);
            Grid.SetColumn(headerGrid.Children[^1], columnOffset + 2);
        }

        // Truncated message
        string displayMessage = log.Message;
        if (isMultiLine)
        {
            int firstNewline = log.Message.IndexOf('\n');
            if (firstNewline >= 0)
                displayMessage = string.Concat(log.Message.AsSpan(0, math.min(firstNewline, 100)), "...");
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
        Grid.SetColumn(headerGrid.Children[^1], columnOffset + 3);

        // Delete button
        Button deleteButton = new Button
        {
            Content = new MaterialIcon
            {
                Kind = MaterialIconKind.Delete,
                Width = 12,
                Height = 12,
                Foreground = EditorColor.FromRGB(200, 200, 200),
            },
            Background = EditorColor.FromRGB(10, 10, 10),
            Padding = new Thickness(2),
            Margin = new Thickness(4, 0, 0, 0),
            BorderBrush = EditorColor.FromRGB(28, 28, 28),
            BorderThickness = new Thickness(1, 1, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            Width = 24,
            Height = 24,
        };
        deleteButton.Click += (e, s) => ClickDeleteButton(log);
        headerGrid.Children.Add(deleteButton);
        Grid.SetColumn(headerGrid.Children[^1], columnOffset + 4);

        mainPanel.Children.Add(headerGrid);

        // Caller info as small gray text underneath
        if (hasCallerInfo)
        {
            TextBlock callerBlock = new TextBlock
            {
                Text = $"└─ {log.CallerInfo}",
                FontSize = 10,
                Foreground = EditorColor.FromRGB(80, 80, 80),
                Margin = new Thickness(isMultiLine ? 24 : 0, 0, 0, 2),
                FontFamily = FontFamily.Parse("Consolas, Courier New, monospace"),
            };
            mainPanel.Children.Add(callerBlock);
        }

        // Expanded content area for multi-line messages
        if (isMultiLine)
        {
            Border expandedContent = new Border
            {
                Background = EditorColor.FromRGB(6, 6, 6),
                BorderBrush = EditorColor.FromRGB(28, 28, 28),
                BorderThickness = new Thickness(1, 1, 0, 0),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 6),
                Margin = new Thickness(isMultiLine ? 24 : 0, 4, 0, 0),
                IsVisible = false,
            };

            StackPanel expandedPanel = new StackPanel { Spacing = 4 };

            // Full message
            SelectableTextBlock fullMessage = new SelectableTextBlock
            {
                Text = log.Message,
                FontSize = 11,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = FontFamily.Parse("Consolas, Courier New, monospace"),
            };
            expandedPanel.Children.Add(fullMessage);

            expandedContent.Child = expandedPanel;
            mainPanel.Children.Add(expandedContent);

            // Expand/collapse functionality
            bool isExpanded = false;
            expandButton!.Click += (_, _) =>
            {
                isExpanded = !isExpanded;
                expandedContent.IsVisible = isExpanded;

                if (expandButton.Content is MaterialIcon icon)
                    icon.Kind = isExpanded ? MaterialIconKind.ChevronDown : MaterialIconKind.ChevronRight;
            };
        }

        logBorder.Child = mainPanel;
        return logBorder;
    }

    /// <summary>
    /// Called when a log delete button is clicked.
    /// </summary>
    private void ClickDeleteButton(LogEntry logEntry)
    {
        if (collapseEnabled)
        {
            // Find and remove the group
            string key = GetCollapseKey(logEntry);
            if (groupedLogs.TryGetValue(key, out GroupedLogEntry? group))
            {
                if (group.Control != null) logList.Children.Remove(group.Control);
                groupedLogs.Remove(key);
            }

            // Remove all matching logs from the debug list
            List<LogEntry> logsToRemove = [.. Debug.Logs.Where(l => GetCollapseKey(l) == key)];
            foreach (LogEntry log in logsToRemove) Debug.ClearLog(log);
        }
        else
        {
            // Normal mode - remove single log
            Debug.ClearLog(logEntry);

            // Find and remove the corresponding UI element
            foreach (Control? child in logList.Children)
            {
                if (child is Border border && border.Tag == logEntry)
                {
                    logList.Children.Remove(border);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Gets the correct log level color.
    /// </summary>
    private static IBrush GetLogColor(LogLevel level) => level switch
    {
        LogLevel.Debug => Brushes.White,
        LogLevel.Info => Brushes.White,
        LogLevel.Warning => Brushes.Yellow,
        LogLevel.Error => Brushes.Red,
        _ => Brushes.Green
    };
}
