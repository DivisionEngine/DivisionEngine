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
using Avalonia.Layout;
using Avalonia.Media;
using DivisionEngine.Editor.Systems;
using DivisionEngine.Rendering;
using DivisionEngine.Systems;
using System.Collections.Generic;

namespace DivisionEngine.Editor;

/// <summary>
/// Window responsible for displaying the Silk.NET player in-editor, sized correctly.
/// </summary>
public partial class EnvironmentWindow : EditorWindow
{
    private static readonly List<EnvironmentWindow?> currentWindows = [];

    private readonly DockPanel mainPanel;
    private readonly StackPanel headerPanel;
    private readonly ComboBox debugMode;

    public readonly Panel renderVisualizerFrame;
    public readonly TextBlock widthHeightText;

    public EnvironmentWindow()
    {
        InitializeComponent();

        // Create main dock panel
        mainPanel = new DockPanel
        {
            Background = Brushes.Transparent,
        };
        headerPanel = new StackPanel
        {
            Background = EditorColor.FromRGB(28, 28, 28),
            Orientation = Orientation.Horizontal,
            Height = 25,
            VerticalAlignment = VerticalAlignment.Center,
        };
        TextBlock debugModeText = new TextBlock
        {
            Text = "Debug Mode",
            FontSize = 12,
            FontWeight = FontWeight.Regular,
            Foreground = EditorColor.FromRGB(128, 128, 128),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(4, 2, 4, 2),
        };
        debugMode = new ComboBox
        {
            ItemsSource = new[] { "None", "Depth", "World Normals", "Object ID", "Ray Steps", "Shadows", "BRDF" },
            SelectedIndex = 0,
            FontSize = 12,
            FontWeight = FontWeight.Regular,
            BorderThickness = new Thickness(0),
            Background = EditorColor.FromRGB(17, 17, 17),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(4, 2, 4, 2),
        };
        debugMode.SelectionChanged += (e, s) => UpdateRendererDebugMode();

        int width = 0, height = 0;
        if (App.Renderer != null && App.Renderer.RendererWindow != null)
        {
            width = App.Renderer.RendererWindow.Size.X;
            height = App.Renderer.RendererWindow.Size.Y;
        }
        widthHeightText = new TextBlock
        {
            Text = $"(Width {width}px,  Height {height}px,  FPS {TimeSystem.FPS})",
            FontSize = 12,
            FontWeight = FontWeight.Regular,
            Foreground = EditorColor.FromRGB(128, 128, 128),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(4, 2, 4, 2),
        };

        // Add controls to header
        headerPanel.Children.Add(debugModeText);
        headerPanel.Children.Add(debugMode);
        headerPanel.Children.Add(widthHeightText);
        DockPanel.SetDock(headerPanel, Dock.Top);
        mainPanel.Children.Add(headerPanel);
        Border separator = new Border
        {
            Background = EditorColor.FromRGB(68, 68, 68),
            Height = 1,
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
                    HorizontalAlignment = HorizontalAlignment.Center,
                }
            },
            Background = EditorColor.FromRGB(12, 12, 12),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        mainPanel.Children.Add(renderVisualizerFrame);

        // Re-enable render window if just focused
        if (!App.RendererVisible) RenderWindowManagementSystem.SetVisible(true);

        this.FindControl<Border>("MainBorder")!.Child = mainPanel;
        currentWindows.Add(this);
    }

    /// <summary>
    /// Syncs the environment window tool values to the renderer window.
    /// </summary>
    public static void SyncToolValuesToRenderer()
    {
        if (App.RendererVisible)
        {
            for (int i = 0; i < currentWindows.Count; i++)
                currentWindows[i]?.UpdateRendererDebugMode();
        }
    }

    /// <summary>
    /// Updates the render window's debug mode.
    /// </summary>
    private void UpdateRendererDebugMode()
    {
        RenderPipeline.DebugMode mode = (RenderPipeline.DebugMode)debugMode.SelectedIndex;
        if (App.Renderer != null) App.Renderer!.debugMode = mode;
    }

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