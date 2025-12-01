using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace DivisionEngine.Editor;

public partial class ConsoleWindow : EditorWindow
{
    public const int MaxDisplayedLogEntries = 1000;

    private readonly StackPanel logList;
    private readonly StackPanel controlsPanel;
    private readonly ScrollViewer scrollViewer;
    private readonly CheckBox autoscrollCheckbox;
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
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(34, 34, 34)),
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

        DockPanel mainPanel = new DockPanel
        {
            Background = EditorColor.FromRGB(45, 45, 45)
        };

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

    private void LoadAllCurrentLogs()
    {
        foreach (LogEntry log in Debug.Logs)
            Dispatcher.UIThread.Post(() => CreateLogEntry(log, false));
    }

    private void Debug_OnLogUpdate(LogEntry obj) => Dispatcher.UIThread.Post(() => CreateLogEntry(obj, autoScroll));

    private void CreateLogEntry(LogEntry log, bool scrollToEnd)
    {
        Border logContainer = CreateLogControl(log);
        logList.Children.Add(logContainer);

        if (scrollToEnd) scrollViewer.ScrollToEnd();

        if (logList.Children.Count > MaxDisplayedLogEntries)
            logList.Children.RemoveAt(0);
    }

    private static Border CreateLogControl(LogEntry log)
    {
        Border logBorder = new Border()
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(68, 68, 68)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(6, 2, 6, 2),
        };

        StackPanel logElements = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 0,
        };

        logElements.Children.AddRange(
        [
            new TextBlock
            {
                Text = $"[{log.Timestamp.TimeOfDay:hh':'mm':'ss':'fff}]",
                FontSize = 12,
                Foreground = Brushes.Gray,
            },
            new TextBlock
            {
                Text = $"[{log.Level}]",
                FontSize = 12,
                Foreground = GetLogColor(log.Level),
            },
            new TextBlock
            {
                Text = log.Message,
                FontSize = 12,
                Foreground = Brushes.White,
            },
        ]);

        logBorder.Child = logElements;
        return logBorder;
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