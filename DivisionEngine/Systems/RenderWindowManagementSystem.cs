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
        private bool prevVisible;

        public override void Awake()
        {
            prevWidth = 0;
            prevHeight = 0;
            prevX = 0;
            prevY = 0;
            prevVisible = false;
        }

        public override void Render()
        {
            Dispatcher.UIThread.Post(UpdateRenderer, DispatcherPriority.Render);
        }

        /// <summary>
        /// Sets whether the Silk.NET physical render window is enabled.
        /// </summary>
        /// <param name="visible">Whether the OpenGL renderer is visible</param>
        public static void SetVisible(bool visible) => _ = App.SetEditorRenderingAsync(visible);

        /// <summary>
        /// Updates the render position, size, and visibility.
        /// </summary>
        private void UpdateRenderer()
        {
            try
            {
                EnvironmentWindow? win = EnvironmentWindow.GetFirstActiveWindow();
                if (win != null && win.IsLoaded && App.RendererVisible)
                {
                    // Check frame is active
                    if (win.renderVisualizerFrame == null || App.Renderer?.RendererWindow == null ||
                        win.renderVisualizerFrame.Bounds.Width <= 0 || win.renderVisualizerFrame.Bounds.Height <= 0)
                    {
                        prevVisible = false;
                        return;
                    }

                    PixelPoint screenPoint = win.renderVisualizerFrame.PointToScreen(new Point(0, 0));
                    Size size = win.renderVisualizerFrame.Bounds.Size;

                    // Check if frame changed shape
                    if (size.Width == prevWidth && size.Height == prevHeight
                        && prevX == screenPoint.X && prevY == screenPoint.Y && prevVisible) return;

                    // Update bounds
                    prevWidth = size.Width;
                    prevHeight = size.Height;
                    prevX = screenPoint.X;
                    prevY = screenPoint.Y;
                    prevVisible = true;

                    App.Renderer.RendererWindow!.Position = new Silk.NET.Maths.Vector2D<int>(screenPoint.X, screenPoint.Y);
                    App.Renderer.RendererWindow.Size = new Silk.NET.Maths.Vector2D<int>((int)size.Width, (int)size.Height);
                }
                else
                {
                    if (App.RendererVisible) SetVisible(false);
                    prevVisible = false;
                }
            }
            catch
            {
                Debug.Error("Failed to update renderer window");
            }
        }
    }
}
