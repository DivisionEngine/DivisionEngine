using Avalonia;
using Avalonia.Threading;

namespace DivisionEngine.Editor.Systems
{
    /// <summary>
    /// Manages render window state (visible, invisible, position, etc).
    /// </summary>
    public class RenderWindowManagementSystem : SystemBase
    {
        private double prevWidth, prevHeight; 
        private int prevX, prevY;
        private int forceGrabWindowTimer;

        public override void Awake()
        {
            prevWidth = 0;
            prevHeight = 0;
            prevX = 0;
            prevY = 0;
            forceGrabWindowTimer = 0;
        }

        public override void Render()
        {
            bool forceGrab = false;
            forceGrabWindowTimer++; // Force window grab every 20 rendered frames.
            if (forceGrabWindowTimer > 20)
            {
                forceGrab = true;
                forceGrabWindowTimer = 0;
            }
            Dispatcher.UIThread.Post(() => UpdateRenderer(forceGrab), DispatcherPriority.Render);
        }

        /// <summary>
        /// Sets whether the Silk.NET physical render window is enabled.
        /// </summary>
        /// <param name="visible">Whether the OpenGL renderer is visible</param>
        public static void SetVisible(bool visible) => _ = App.SetEditorRenderingAsync(visible);

        /// <summary>
        /// Updates the render position, size, and visibility.
        /// </summary>
        private void UpdateRenderer(bool forceGrab)
        {
            try
            {
                EnvironmentWindow? win = EnvironmentWindow.GetFirstActiveWindow();
                if (win != null && win.IsLoaded && App.RendererVisible)
                {
                    // Check frame is active
                    if (win.renderVisualizerFrame == null || App.Renderer?.RendererWindow == null ||
                        win.renderVisualizerFrame.Bounds.Width <= 0 || win.renderVisualizerFrame.Bounds.Height <= 0)
                        return;

                    PixelPoint screenPoint = win.renderVisualizerFrame.PointToScreen(new Point(0, 0));
                    Size size = win.renderVisualizerFrame.Bounds.Size;

                    // Check if frame changed shape
                    if (!forceGrab && size.Width == prevWidth && size.Height == prevHeight
                        && prevX == screenPoint.X && prevY == screenPoint.Y) return;

                    // Update bounds
                    prevWidth = size.Width;
                    prevHeight = size.Height;
                    prevX = screenPoint.X;
                    prevY = screenPoint.Y;

                    App.Renderer.RendererWindow!.Position = new Silk.NET.Maths.Vector2D<int>(screenPoint.X, screenPoint.Y);
                    App.Renderer.RendererWindow.Size = new Silk.NET.Maths.Vector2D<int>((int)size.Width, (int)size.Height);
                }
                else if (App.RendererVisible)
                    SetVisible(false);
            }
            catch
            {
                Debug.Error("Failed to update renderer window");
            }
        }
    }
}
