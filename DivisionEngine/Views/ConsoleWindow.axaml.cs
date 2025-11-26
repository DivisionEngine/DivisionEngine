using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace DivisionEngine.Editor;

public partial class ConsoleWindow : EditorWindow
{
    public const int MaxDisplayedLogEntries = 1000;

    private readonly StackPanel logList;
    private readonly ScrollViewer scrollViewer;
    private readonly bool autoScroll;

    public ConsoleWindow()
    {
        InitializeComponent();

        autoScroll = true;
        logList = new StackPanel { Orientation = Orientation.Vertical };
        scrollViewer = new ScrollViewer { Content = logList };

        Debug.OnLogUpdate += Debug_OnLogUpdate;

        LoadAllCurrentLogs();
        AddDynamicContent();
    }

    private void LoadAllCurrentLogs()
    {
        foreach (LogEntry log in Debug.Logs)
            Dispatcher.UIThread.Post(() => CreateLogEntry(log, false));
    }

    private void Debug_OnLogUpdate(LogEntry obj)
    {
        Dispatcher.UIThread.Post(() => CreateLogEntry(obj, autoScroll));
    }

    private void AddDynamicContent()
    {
        Border? border = this.FindControl<Border>("MainBorder");
        if (border == null)
        {
            Content = new Border
            {
                Child = scrollViewer,
            };
        }
        else border.Child = scrollViewer;
    }

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

    private static IBrush GetLogColor(LogLevel level)
    {
        return level switch
        { 
            LogLevel.Debug => Brushes.White,
            LogLevel.Info => Brushes.White,
            LogLevel.Warning => Brushes.Yellow,
            LogLevel.Error => Brushes.Red,
            _ => Brushes.Green
        };
    }
}