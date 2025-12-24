using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using DivisionEngine.Editor.Systems;
using System.Collections.Generic;

namespace DivisionEngine.Editor;

/// <summary>
/// Window responsible for displaying the Silk.NET player in-editor, sized correctly.
/// </summary>
public partial class EnvironmentWindow : EditorWindow
{
    private static readonly List<EnvironmentWindow?> currentWindows = [];

    private readonly DockPanel mainPanel;
    public readonly Panel renderVisualizerFrame;
    private readonly StackPanel headerPanel;
    private readonly TextBlock headerText;

    //public Panel RenderVisualizerFrame => renderVisualizerFrame;

    public EnvironmentWindow()
    {
        InitializeComponent();

        // Create main dock panel
        mainPanel = new DockPanel
        {
            Background = Brushes.Transparent
        };
        headerPanel = new StackPanel
        {
            Background = EditorColor.FromRGB(28, 28, 28),
            Orientation = Orientation.Horizontal,
            Height = 25,
            VerticalAlignment = VerticalAlignment.Center,
        };
        headerText = new TextBlock
        {
            Text = "Environment tools area",
            FontSize = 12,
            FontWeight = FontWeight.Regular,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(4, 0, 4, 0)
        };

        // Add controls to header
        headerPanel.Children.Add(headerText);
        DockPanel.SetDock(headerPanel, Dock.Top);
        mainPanel.Children.Add(headerPanel);
        Border separator = new Border
        {
            Background = EditorColor.FromRGB(68, 68, 68),
            Height = 1
        };
        DockPanel.SetDock(separator, Dock.Top);
        mainPanel.Children.Add(separator);

        renderVisualizerFrame = new Panel
        {
            Children = {
                new TextBlock
                {
                    Text = "Cannot have multiple environment windows",
                    Foreground = Brushes.LightGray,
                    FontSize = 14,
                    FontWeight = FontWeight.Light,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            },
            Background = EditorColor.FromRGB(12, 12, 12),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        mainPanel.Children.Add(renderVisualizerFrame);

        // Re-enable render window if just focused
        if (!App.RendererVisible) RenderWindowManagementSystem.SetVisible(true);

        this.FindControl<Border>("MainBorder")!.Child = mainPanel;
        currentWindows.Add(this);
    }

    /// <summary>
    /// Updates the window title/header text.
    /// </summary>
    /// <param name="text">New header text</param>
    public void SetHeaderText(string text) => headerText.Text = text;

    /// <summary>
    /// Sets the render frame size.
    /// </summary>
    /// <param name="width">Width in pixels</param>
    /// <param name="height">Height in pixels</param>
    public void SetRenderFrameSize(int width, int height)
    {
        renderVisualizerFrame.Width = width;
        renderVisualizerFrame.Height = height;
    }

    /// <summary>
    /// Makes sure all environment windows in current list are active.
    /// </summary>
    private static void ValidateEnvironmentWindows()
    {
        foreach (EnvironmentWindow? window in currentWindows.ToArray()) // Don't forget to create iterator copy
        {
            if (window == null || !window.IsLoaded)
                currentWindows.Remove(window);
        }
    }

    /// <summary>
    /// Gets the first active environment window.
    /// </summary>
    public static EnvironmentWindow? GetFirstActiveWindow()
    {
        ValidateEnvironmentWindows();
        return currentWindows.Count > 0 ? currentWindows[0] : null;
    }

    /// <summary>
    /// Gets all active environment windows.
    /// </summary>
    public static EnvironmentWindow?[] GetActiveWindows()
    {
        ValidateEnvironmentWindows();
        return [.. currentWindows];
    }
}