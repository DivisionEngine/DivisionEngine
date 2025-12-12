using Avalonia;
using Avalonia.Threading;
using System;

namespace DivisionEngine.Editor;

/// <summary>
/// Window responsible for displaying the Silk.NET player in-editor, sized correctly.
/// </summary>
public partial class EnvironmentWindow : EditorWindow
{
    private readonly DispatcherTimer? renderWindowUpdate;

    public EnvironmentWindow()
    {
        InitializeComponent();

        // Initialize the render window update timer
        renderWindowUpdate = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100) // Approximately 10 FPS
        };
        renderWindowUpdate.Tick += (_, _) => UpdateRendererPosition();
        renderWindowUpdate.Start(); // Start the render window tracking update loop

        this.GetObservable(IsVisibleProperty).Subscribe(SetVisible);
    }

    private void UserControl_Unloaded(object? sender, VisualTreeAttachmentEventArgs e) => SetVisible(false);

    // Fully implement this so environment window can be added and removed
    public void SetVisible(bool visible)
    {
        if (visible)
        {
            //App.SetEditorRendering(true);
            renderWindowUpdate?.Start();
            UpdateRendererPosition();
            Debug.Info("Environment Window: Activated");
        }
        else
        {
            renderWindowUpdate?.Stop();
            //App.SetEditorRendering(false);
            Debug.Info("Environment Window: Deactivated");
        }
    }

    /// <summary>
    /// Updates the position, size, and visibility of the renderer's window to match the current position and
    /// dimensions of the <see cref="RenderBackground"/> element.
    /// </summary>
    private void UpdateRendererPosition()
    {
        try
        {
            if (RenderVisualizerFrame == null || App.Renderer?.RendererWindow == null)
                return;
            if (RenderVisualizerFrame.Bounds.Width <= 0 || RenderVisualizerFrame.Bounds.Height <= 0)
                return;

            PixelPoint screenPoint = RenderVisualizerFrame.PointToScreen(new Point(0, 0));
            Size size = RenderVisualizerFrame.Bounds.Size;

            App.Renderer.RendererWindow!.Position = new Silk.NET.Maths.Vector2D<int>(screenPoint.X, screenPoint.Y);
            App.Renderer.RendererWindow.Size = new Silk.NET.Maths.Vector2D<int>((int)size.Width, (int)size.Height);
        }
        catch
        {
            Debug.Error("Failed to update renderer window position.");
        }
    }
}