using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DivisionEngine.MathLib;
using Material.Icons;
using Material.Icons.Avalonia;

namespace DivisionEngine.Editor;

public partial class ConsoleWindow : EditorWindow
{
    public const int MaxDisplayedLogEntries = 1000;

    private readonly StackPanel logList;
    private readonly StackPanel controlsPanel;
    private readonly ScrollViewer scrollViewer;
    private readonly CheckBox autoscrollCheckbox;
    private readonly ComboBox filterLogTypeBox;
    private readonly Button clearButton;
    private bool autoScroll;

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

    private void ClearButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        logList.Children.Clear();
        Debug.ClearLogs();
    }

    private void Debug_OnLogUpdate(LogEntry obj) => Dispatcher.UIThread.Post(() => CreateLogEntry(obj, autoScroll));

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
        Border logBorder = new Border()
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(68, 68, 68)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(6, 2, 6, 2),
        };

        Grid grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto), // Timestamp
                new ColumnDefinition(GridLength.Auto), // Level
                new ColumnDefinition(new GridLength(1, GridUnitType.Star)), // Message
                new ColumnDefinition(GridLength.Auto), // Delete button
            }
        };

        // Timestamp
        grid.Children.Add(new TextBlock
        {
            Text = $"[{log.Timestamp.TimeOfDay:hh':'mm':'ss':'fff}]",
            FontSize = 12,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
        Grid.SetColumn(grid.Children[^1], 0);

        // Level
        grid.Children.Add(new TextBlock
        {
            Text = $"[{log.Level}]",
            FontSize = 12,
            Foreground = GetLogColor(log.Level),
            Margin = new Thickness(0, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
        Grid.SetColumn(grid.Children[^1], 1);

        // Message
        grid.Children.Add(new TextBlock
        {
            Text = log.Message,
            FontSize = 12,
            Foreground = Brushes.White,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        });
        Grid.SetColumn(grid.Children[^1], 2);

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
        };
        deleteButton.Click += (e, s) => ClickDeleteButton(log);
        grid.Children.Add(deleteButton);
        Grid.SetColumn(grid.Children[^1], 3);

        logBorder.Child = grid;
        return logBorder;
    }

    private void ClickDeleteButton(LogEntry logEntry)
    {
        Debug.ClearLog(logEntry);
        LoadAllCurrentLogs();
    }

    private static IBrush GetLogColor(LogLevel level) => level switch
    {
        LogLevel.Debug => Brushes.White,
        LogLevel.Info => Brushes.White,
        LogLevel.Warning => Brushes.Yellow,
        LogLevel.Error => Brushes.Red,
        _ => Brushes.Green
    };
}