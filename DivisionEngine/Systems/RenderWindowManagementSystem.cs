//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using DivisionEngine.Rendering;
using DivisionEngine.Systems;
using System;
using System.Threading.Tasks;

namespace DivisionEngine.Editor.Systems
{
    /// <summary>
    /// Manages render window state (visible, focus, position, etc).
    /// </summary>
    public class RenderWindowManagementSystem : SystemBase
    {
        private double prevWidth, prevHeight; 
        private int prevX, prevY;
        private int forceGrabWindowTimer;
        private int initializeTimer;
        private bool updatingFocus;
        private double lastDpiScale = 1;

        /// <summary>
        /// Editor window is in focus.
        /// </summary>
        public static bool EditorFocused { get; set; }

        /// <summary>
        /// Renderer window is in focus.
        /// </summary>
        public static bool RendererFocused { get; set; }

        public override void AppStart()
        {
            prevWidth = 0;
            prevHeight = 0;
            prevX = 0;
            prevY = 0;
            forceGrabWindowTimer = 0;
            initializeTimer = 60;

            EditorFocused = false;
            RendererFocused = true;
            RenderPipeline.RenderWindowFocusd += (f) => RendererFocused = f;
            RenderPipeline.RenderWindowFocusd += async (_) => await FocusUpdate();
            App.AppFocused += (f) => EditorFocused = f;
            App.AppFocused += async (_) => await FocusUpdate();

            // Make sure object selection is enabled and getting called
            EntitySelectionSystem.CanSelect = true;
            EntitySelectionSystem.OnEntitySelected += Selection.SelectEntity;
            EntitySelectionSystem.OnNoEntityFound += () =>
            {
                Selection.Clear();
                PropertiesWindow.LoadWorldData(WorldManager.CurrentWorld);
            };
        }

        /// <summary>
        /// Gets the current DPI scaling factor from the TopLevel.
        /// </summary>
        private double GetCurrentDpiScale(EnvironmentWindow win)
        {
            if (win.renderVisualizerFrame == null) return 1.0;
            TopLevel? topLevel = TopLevel.GetTopLevel(win.renderVisualizerFrame);
            return topLevel?.RenderScaling ?? 1.0;
        }

        public override void EditorUpdate()
        {
            if (initializeTimer > 0) initializeTimer--;
        }

        public async Task FocusUpdate()
        {
            if (updatingFocus) return;
            updatingFocus = true;
            await Task.Delay(300); // Wait to see if other window is immediately focused

            Dispatcher.UIThread.Post(() =>
            {
                if (initializeTimer == 0 && !EditorFocused && !RendererFocused && App.RendererVisible)
                {
                    _ = App.SetEditorRenderingAsync(false);
                    Debug.Log("Set rendering false");
                }
                else if ((EditorFocused || RendererFocused) && !App.RendererVisible)
                {
                    EnvironmentWindow? win = EnvironmentWindow.GetFirstActiveWindow();
                    if (win != null && win.IsLoaded)
                    {
                        initializeTimer = 60;
                        _ = App.SetEditorRenderingAsync(true);
                        Debug.Log("Set rendering true");
                    }
                }
            });
            updatingFocus = false;
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
        public static void SetVisible(bool visible)
        {
            if (visible != App.RendererVisible)
                _ = App.SetEditorRenderingAsync(visible);
        }

        /// <summary>
        /// Updates the render position, size, and visibility.
        /// </summary>
        private void UpdateRenderer(bool forceGrab)
        {
            try
            {
                if (App.Renderer == null || App.Renderer.RendererWindow == null) return;

                EnvironmentWindow? win = EnvironmentWindow.GetFirstActiveWindow();
                if (win != null && win.IsLoaded && App.RendererVisible)
                {
                    // Check frame is active
                    if (win.renderVisualizerFrame == null || App.Renderer?.RendererWindow == null ||
                        win.renderVisualizerFrame.Bounds.Width <= 0 || win.renderVisualizerFrame.Bounds.Height <= 0)
                        return;

                    // Get DPI scaling
                    double dpiScale = GetCurrentDpiScale(win);

                    // PointToScreen already returns physical pixels! Don't multiply again.
                    PixelPoint screenPointPhysical = win.renderVisualizerFrame.PointToScreen(new Point(0, 0));
                    Size sizeDPI = win.renderVisualizerFrame.Bounds.Size;

                    // Only convert the size from DPI to physical pixels
                    int physicalWidth = (int)(sizeDPI.Width * dpiScale);
                    int physicalHeight = (int)(sizeDPI.Height * dpiScale);

                    // Update RenderPipeline's DPI scale for input handling
                    if (App.Renderer != null && Math.Abs(dpiScale - lastDpiScale) > 0.01)
                    {
                        App.Renderer.UpdateDpiScale((float)dpiScale);
                        lastDpiScale = dpiScale;
                    }

                    // Check if frame changed shape using physical coordinates
                    if (!forceGrab && physicalWidth == prevWidth && physicalHeight == prevHeight
                        && screenPointPhysical.X == prevX && screenPointPhysical.Y == prevY) return;

                    // Update bounds with physical pixels
                    prevWidth = physicalWidth;
                    prevHeight = physicalHeight;
                    prevX = screenPointPhysical.X;
                    prevY = screenPointPhysical.Y;

                    // Apply physical pixel coordinates to Silk.NET window
                    App.Renderer!.RendererWindow.Position = new Silk.NET.Maths.Vector2D<int>(screenPointPhysical.X, screenPointPhysical.Y);
                    App.Renderer.RendererWindow.Size = new Silk.NET.Maths.Vector2D<int>(physicalWidth, physicalHeight);

                    // Update window text (show DPI size for reference)
                    win.widthHeightText.Text = $"(Width {(int)sizeDPI.Width}px,  Height {(int)sizeDPI.Height}px,  FPS {TimeSystem.FPS})";
                }
                else if (App.RendererVisible) SetVisible(false);
            }
            catch (Exception ex)
            {
                Debug.Error("Failed to update renderer window", ex);
            }
        }
    }
}
